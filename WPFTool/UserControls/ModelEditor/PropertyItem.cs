using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reflection;

namespace SeanTool.CSharp.Net8.WPF
{
    public class PropertyItem : ViewModelBase
    {
        private readonly object _targetInstance; // 原始 Model 實體
        private readonly PropertyInfo _propInfo; // 屬性資訊

        public string PropertyName { get; }
        public string DisplayName { get; }
        public EditorInputType InputType { get; private set; }

        // 給 ComboBox 綁定的選項清單
        public ObservableCollection<string> Options { get; private set; }

        // 用於檔案選擇器的 Filter
        public string FileFilter { get; private set; }

        // 暫存值
        private object? _pendingValue;

        // 建構子：透過 Reflection 初始化
        public PropertyItem(object instance, PropertyInfo prop)
        {
            _targetInstance = instance;
            _propInfo = prop;
            PropertyName = prop.Name;

            // 處理 DisplayName Attribute
            var dispAttr = prop.GetCustomAttribute<DisplayNameAttribute>();
            DisplayName = dispAttr != null ? dispAttr.DisplayName : prop.Name;

            // 初始化時，先從 Model 讀取現有的值到暫存區
            _pendingValue = _propInfo.GetValue(_targetInstance);

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
                if (InputType == EditorInputType.Enum && _pendingValue != null)
                {
                    return _pendingValue.ToString();
                }
                return _pendingValue;
            }
            set
            {
                try
                {
                    object? safeValue = value;
                    Type targetType = Nullable.GetUnderlyingType(_propInfo.PropertyType) ?? _propInfo.PropertyType;

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
                    _pendingValue = safeValue;

                    Refresh();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"轉換錯誤: {ex.Message}");
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
                if (value == null) return;

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
                string oldTimePart = TimePart;
                // 嘗試解析使用者輸入的時間字串
                if (DateTime.TryParse(value, out var tempTime))
                {
                    // 取得原本的日期部分
                    var originalDate = (Value is DateTime dt) ? dt.Date : DateTime.Today;

                    // 合併：舊日期 + 新時間
                    Value = originalDate.Add(tempTime.TimeOfDay);
                }
            }
        }

        public void ApplyChange()
        {
            try
            {
                _propInfo.SetValue(_targetInstance, _pendingValue);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"寫入 Model 失敗: {ex.Message}");
            }
        }

        private void DetermineInputType()
        {
            Type type = Nullable.GetUnderlyingType(_propInfo.PropertyType) ?? _propInfo.PropertyType;

            var pathAttr = _propInfo.GetCustomAttribute<EditorPathAttribute>();
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
