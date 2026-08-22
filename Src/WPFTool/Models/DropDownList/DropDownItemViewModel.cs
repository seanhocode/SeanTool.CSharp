namespace SeanTool.CSharp.WPFTool.Models.DropDownList
{
    /// <summary>
    /// DropDownList 內部使用的項目包裝，提供搜尋用的 DisplayText 與選取狀態。
    /// </summary>
    public class DropDownItemViewModel : ViewModelBase
    {
        private bool _isSelected;

        public DropDownItemViewModel(object? value, string displayText)
        {
            Value = value;
            DisplayText = displayText;
        }

        /// <summary>
        /// 原始來源項目 (ItemsSource 中的物件)
        /// </summary>
        public object? Value { get; }

        /// <summary>
        /// 搜尋/顯示用文字，來自 DisplayMemberPath 或 ToString()
        /// </summary>
        public string DisplayText { get; }

        /// <summary>
        /// 是否已選取
        /// </summary>
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected == value)
                {
                    return;
                }

                _isSelected = value;
                OnPropertyChanged();
            }
        }
    }
}
