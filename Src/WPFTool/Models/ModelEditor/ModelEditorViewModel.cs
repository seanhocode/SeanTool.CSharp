using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Windows;
using System.Windows.Input;

namespace SeanTool.CSharp.WPF
{
    public class ModelEditorViewModel : ViewModelBase
    {
        // 這取代了 WinForms 的 TableLayoutPanel，直接給 UI 一個清單
        public ObservableCollection<PropertyItem> Properties { get; set; }

        // 用來控制是否唯讀 (對應你的 Viewer/Editor 模式)
        private bool _isEditing = true;
        public bool IsEditing
        {
            get => _isEditing;
            set
            {
                _isEditing = value;
                foreach (var property in Properties)
                {
                    property.IsEditing = value;
                }
                OnPropertyChanged();
            }
        }

        // 檔案瀏覽命令
        public ICommand BrowseCommand { get; }

        // 編輯子物件的命令
        public ICommand EditObjectCommand { get; }

        // 儲存命令
        public ICommand SaveCommand { get; }

        public ICommand CancelCommand { get; }

        private readonly Action? _savedAction;
        private readonly Action? _canceledAction;

        public ModelEditorViewModel(object model, Action? savedAction = null, Action? canceledAction = null)
        {
            _savedAction = savedAction;
            _canceledAction = canceledAction;

            BrowseCommand = new RelayCommand<PropertyItem>(OnBrowseFile);
            EditObjectCommand = new RelayCommand<PropertyItem>(OnEditObject);
            SaveCommand = new RelayCommand<object>(OnSave);
            CancelCommand = new RelayCommand<object>(OnCancel);

            Properties = new ObservableCollection<PropertyItem>();
            if (model == null) return;

            // 掃描屬性
            var props = model.GetType()
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(property => property.CanRead && property.GetIndexParameters().Length == 0)
                .OrderBy(property => property.Name, StringComparer.Ordinal);
            foreach (PropertyInfo p in props)
            {
                var propertyItem = new PropertyItem(model, p)
                {
                    IsEditing = IsEditing
                };
                Properties.Add(propertyItem);
            }
        }

        private void OnSave(object parameter)
        {
            if (Properties.Any(item => !item.Validate()))
            {
                MessageBox.Show("請先修正欄位錯誤後再儲存。");
                return;
            }

            var appliedItems = new List<PropertyItem>();
            foreach (var item in Properties.Where(item => !item.IsReadOnly))
            {
                try
                {
                    item.ApplyChange();
                    appliedItems.Add(item);
                }
                catch (Exception ex)
                {
                    item.SetError(ex.Message);
                    foreach (var appliedItem in appliedItems.AsEnumerable().Reverse())
                    {
                        try
                        {
                            appliedItem.RestoreOriginalValue();
                        }
                        catch
                        {
                        }
                    }

                    MessageBox.Show("儲存失敗，已取消已寫入的欄位變更。");
                    return;
                }
            }

            foreach (var item in Properties.Where(item => !item.IsReadOnly))
            {
                item.Commit();
            }

            MessageBox.Show("設定已儲存！");

            _savedAction?.Invoke();
        }

        private void OnCancel(object parameter)
        {
            foreach (var item in Properties)
            {
                item.Reset();
            }

            _canceledAction?.Invoke();
        }

        private void OnBrowseFile(PropertyItem item)
        {
            if (item == null) return;

            // 根據 InputType 決定是選檔案還是選資料夾
            if (item.InputType == EditorInputType.FilePath)
            {
                var dialog = new OpenFileDialog
                {
                    Filter = string.IsNullOrEmpty(item.FileFilter) ? "All files (*.*)|*.*" : item.FileFilter,
                    Title = $"Select file for {item.DisplayName}"
                };

                if (dialog.ShowDialog() == true)
                {
                    item.Value = dialog.FileName;
                }
            }
            else if (item.InputType == EditorInputType.FolderPath)
            {
                // .NET 8 (WPF) 可以直接用 OpenFolderDialog
                var dialog = new OpenFolderDialog
                {
                    Title = $"Select folder for {item.DisplayName}"
                };

                if (dialog.ShowDialog() == true)
                {
                    item.Value = dialog.FolderName;
                }
            }
        }

        private void OnEditObject(PropertyItem item)
        {
            if (item?.Value == null)
            {
                MessageBox.Show("物件為空，無法編輯。");
                return;
            }

            var editableCopy = item.CreateEditableCopy();
            var window = new ModelEditorWindow(editableCopy)
            {
                // 如果想要針對屬性名稱顯示更詳細的標題
                Title = $"編輯屬性: {item.DisplayName}",
                Owner = Application.Current.MainWindow // (可選) 設定擁有者，讓視窗不會亂跑
            };

            if (window.ShowDialog() == true)
            {
                item.Value = editableCopy;
                item.Refresh();
            }
        }
    }
}
