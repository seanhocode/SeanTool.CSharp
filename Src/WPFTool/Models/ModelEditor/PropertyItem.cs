using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reflection;

namespace SeanTool.CSharp.WPF
{
    public class PropertyItem : ViewModelBase
    {
        private readonly object _TargetInstance; // 原始 Model 實體
        private readonly PropertyInfo _PropInfo; // 屬性資訊

        public string PropertyName { get; }
        public string DisplayName { get; }
        public EditorInputType InputType { get; private set; }
        public bool IsReadOnly => !_PropInfo.CanWrite;

        private bool _isEditing = true;
        public bool IsEditing
        {
            get => _isEditing;
            set
            {
                _isEditing = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanEdit));
            }
        }

        public bool CanEdit => IsEditing && !IsReadOnly;

        // 給 ComboBox 綁定的選項清單
        public ObservableCollection<string> Options { get; private set; }

        // 用於檔案選擇器的 Filter
        public string FileFilter { get; private set; }

        private object? _PendingValue;
        private object? _OriginalValue;
        private string? _ErrorMessage;

        public string? ErrorMessage
        {
            get => _ErrorMessage;
            private set
            {
                _ErrorMessage = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasError));
            }
        }

        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

        // 建構子：透過 Reflection 初始化
        public PropertyItem(object instance, PropertyInfo prop)
        {
            _TargetInstance = instance;
            _PropInfo = prop;
            PropertyName = prop.Name;

            // 處理 DisplayName Attribute
            var dispAttr = prop.GetCustomAttribute<DisplayNameAttribute>();
            DisplayName = dispAttr != null ? dispAttr.DisplayName : prop.Name;

            // 初始化時，先從 Model 讀取現有的值到暫存區
            _OriginalValue = _PropInfo.GetValue(_TargetInstance);
            _PendingValue = _OriginalValue;

            // 判斷 InputType (對應你原本的 CreateEditorControl 邏輯)
            DetermineInputType();
        }

        // 【關鍵】這是 UI 綁定的目標。
        // 當 UI 修改 Value 時，我們直接透過 Reflection 寫回原始 Model
        public object? Value
        {
            // 如果是 Enum，就轉成字串給 UI，這樣才能對應到 Options 裡的字串
            get
            {
                if (InputType == EditorInputType.Enum && _PendingValue != null)
                {
                    return _PendingValue.ToString();
                }
                return _PendingValue;
            }
            set
            {
                try
                {
                    object? safeValue = value;
                    Type targetType = Nullable.GetUnderlyingType(_PropInfo.PropertyType) ?? _PropInfo.PropertyType;

                    if (safeValue is string text && text.Length == 0 && Nullable.GetUnderlyingType(_PropInfo.PropertyType) != null)
                    {
                        safeValue = null;
                    }
                    else if (safeValue == null && targetType.IsValueType && Nullable.GetUnderlyingType(_PropInfo.PropertyType) == null)
                    {
                        throw new InvalidOperationException($"{DisplayName} 不可為空值。");
                    }

                    // 1. Enum 轉換 (UI 傳來字串 -> 轉回 Enum 存入暫存)
                    if (targetType.IsEnum && safeValue is string strEnum)
                    {
                        safeValue = Enum.Parse(targetType, strEnum);
                    }
                    // 2. 一般型別轉換 (字串轉數字等)
                    else if (safeValue != null && !targetType.IsAssignableFrom(safeValue.GetType()))
                    {
                        safeValue = Convert.ChangeType(safeValue, targetType);
                    }

                    // 更新暫存值 (這裡存的是真正的 Enum 物件)
                    _PendingValue = safeValue;
                    ErrorMessage = null;

                    Refresh();
                }
                catch (Exception ex)
                {
                    ErrorMessage = ex.Message;
                }
            }
        }

        // 給 DatePicker 綁定的屬性 (只讀取/寫入 日期部分)
        public DateTime? DatePart
        {
            get
            {
                if (Value is DateTime dt) return dt.Date;
                return null;
            }
            set
            {
                if (value == null)
                {
                    if (Nullable.GetUnderlyingType(_PropInfo.PropertyType) != null)
                    {
                        Value = null;
                    }

                    return;
                }

                // 取得原本的時間部分
                var originalTime = (Value is DateTime dt) ? dt.TimeOfDay : TimeSpan.Zero;

                // 合併：新日期 + 舊時間
                Value = value.Value.Add(originalTime);
            }
        }

        // 給 TextBox 綁定的屬性 (只讀取/寫入 時間字串 HH:mm)
        public string TimePart
        {
            get
            {
                if (Value is DateTime dt) return dt.ToString("HH:mm:ss");
                return "00:00:00";
            }
            set
            {
                if (!DataTimePicker.TryParseTime(value, out TimeSpan time))
                {
                    SetError("時間格式錯誤，請輸入 HH:mm 或 HH:mm:ss。");
                    return;
                }

                var originalDate = (Value is DateTime dt) ? dt.Date : DateTime.Today;
                Value = originalDate.Add(time);
            }
        }

        public bool Validate()
        {
            return !HasError;
        }

        public void SetError(string errorMessage)
        {
            ErrorMessage = errorMessage;
        }

        public void ApplyChange()
        {
            if (IsReadOnly)
            {
                return;
            }

            _PropInfo.SetValue(_TargetInstance, _PendingValue);
        }

        public void RestoreOriginalValue()
        {
            _PropInfo.SetValue(_TargetInstance, _OriginalValue);
        }

        public void Commit()
        {
            _OriginalValue = _PendingValue;
        }

        public void Reset()
        {
            _PendingValue = _OriginalValue;
            ErrorMessage = null;
            Refresh();
        }

        public object CreateEditableCopy()
        {
            if (_PendingValue == null)
            {
                throw new InvalidOperationException($"{DisplayName} 沒有可編輯的物件。");
            }

            return CloneObject(_PendingValue, new Dictionary<object, object>(ReferenceEqualityComparer.Instance));
        }

        private static object CloneObject(object value, IDictionary<object, object> visited)
        {
            Type type = value.GetType();
            if (type.IsValueType || value is string || value is Delegate)
            {
                return value;
            }

            if (visited.TryGetValue(value, out object? existing))
            {
                return existing;
            }

            if (type.IsArray)
            {
                Array source = (Array)value;
                Array clone = Array.CreateInstance(type.GetElementType()!, source.Length);
                visited[value] = clone;
                for (int index = 0; index < source.Length; index++)
                {
                    object? element = source.GetValue(index);
                    clone.SetValue(element is null ? null : CloneObject(element, visited), index);
                }

                return clone;
            }

            var memberwiseClone = typeof(object).GetMethod("MemberwiseClone", BindingFlags.Instance | BindingFlags.NonPublic)!;
            object copy = memberwiseClone.Invoke(value, null)!;
            visited[value] = copy;

            foreach (FieldInfo field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (field.IsStatic)
                {
                    continue;
                }

                object? fieldValue = field.GetValue(value);
                if (fieldValue is not null && !field.FieldType.IsValueType && fieldValue is not string)
                {
                    field.SetValue(copy, CloneObject(fieldValue, visited));
                }
            }

            return copy;
        }

        private void DetermineInputType()
        {
            Type type = Nullable.GetUnderlyingType(_PropInfo.PropertyType) ?? _PropInfo.PropertyType;

            var pathAttr = _PropInfo.GetCustomAttribute<EditorPathAttribute>();
            if (pathAttr != null)
            {
                InputType = pathAttr.Type == PathType.File
                    ? EditorInputType.FilePath
                    : EditorInputType.FolderPath; // 假設你有定義 FolderPath
                FileFilter = pathAttr.Filter;
                return;
            }

            if (type == typeof(bool))
            {
                InputType = EditorInputType.Boolean;
            }
            else if (type.IsEnum)
            {
                InputType = EditorInputType.Enum;
                // ★ 新增：取得 Enum 所有名稱並填入 Options
                var names = Enum.GetNames(type);
                Options = new ObservableCollection<string>(names);
            }
            else if (type == typeof(DateTime))
            {
                InputType = EditorInputType.DateTime;
            }
            else if (IsNumeric(type))
            {
                InputType = EditorInputType.Number;
            }
            else if (IsComplexType(type))
            {
                InputType = EditorInputType.Object;
            }
            else
            {
                InputType = EditorInputType.Text;
            }
        }

        private bool IsComplexType(Type type)
        {
            // 排除 String, 排除實值型別(int, double...), 排除陣列集合
            // 這裡的邏輯可以根據你的需求調整，例如只允許特定的 Namespace
            return type.IsClass && type != typeof(string) && !typeof(IEnumerable).IsAssignableFrom(type);
        }

        private bool IsNumeric(Type type)
        {
            return type == typeof(int) || type == typeof(double) || type == typeof(decimal) ||
                   type == typeof(float) || type == typeof(long) || type == typeof(short);
        }

        // ★ 新增：強制更新 UI 的方法
        public void Refresh()
        {
            // 通知 UI 重新讀取 Value 屬性 (觸發 TextBox 更新顯示)
            OnPropertyChanged(nameof(Value));
            OnPropertyChanged(nameof(DatePart));
            OnPropertyChanged(nameof(TimePart));
        }
    }
}
