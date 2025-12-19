using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace SeanTool.CSharp.Net8.WPF
{
    public class ModelEditorViewModel : INotifyPropertyChanged
    {
        // 這取代了 WinForms 的 TableLayoutPanel，直接給 UI 一個清單
        public ObservableCollection<PropertyItem> Properties { get; set; }

        // 用來控制是否唯讀 (對應你的 Viewer/Editor 模式)
        private bool _isEditing = true;
        public bool IsEditing
        {
            get => _isEditing;
            set { _isEditing = value; OnPropertyChanged(nameof(IsEditing)); }
        }

        // 檔案瀏覽命令
        public ICommand BrowseCommand { get; }

        // 編輯子物件的命令
        public ICommand EditObjectCommand { get; }

        // 儲存命令
        public ICommand SaveCommand { get; }

        // 如果你是用在彈出視窗，可能需要一個 Action 來關閉視窗
        private readonly Action _closeAction;

        public ModelEditorViewModel(object model, Action closeAction = null)
        {
            _closeAction = closeAction;

            BrowseCommand = new RelayCommand<PropertyItem>(OnBrowseFile);
            EditObjectCommand = new RelayCommand<PropertyItem>(OnEditObject);
            SaveCommand = new RelayCommand<object>(OnSave);

            Properties = new ObservableCollection<PropertyItem>();
            if (model == null) return;

            // 掃描屬性
            var props = model.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (PropertyInfo p in props)
            {
                if (!p.CanRead || !p.CanWrite) continue;

                // 建立包裝器並加入清單
                Properties.Add(new PropertyItem(model, p));
            }
        }

        private void OnSave(object parameter)
        {
            // 跑迴圈，把所有暫存的值寫入真正的 Model
            foreach (var item in Properties)
            {
                item.ApplyChange();
            }

            MessageBox.Show("設定已儲存！");

            // 如果有設定關閉視窗的動作，就執行它
            _closeAction?.Invoke();
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

            // 用自訂的 EditorModelWindow
            var window = new EditorModelWindow(item.Value)
            {
                // 如果想要針對屬性名稱顯示更詳細的標題
                Title = $"編輯屬性: {item.DisplayName}",
                Owner = Application.Current.MainWindow // (可選) 設定擁有者，讓視窗不會亂跑
            };

            // 處理視窗關閉後的更新
            // 因為 EditorModelWindow 裡面是用 DataBinding，
            // 我們需要監聽視窗關閉事件來做最後的 Refresh
            window.Closed += (s, e) =>
            {
                // 視窗關閉後，強制更新 UI 顯示 (例如 ToString 結果可能變了)
                item.Refresh();
            };

            window.ShowDialog();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
