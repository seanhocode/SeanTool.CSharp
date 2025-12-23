using System.Windows.Controls;

namespace SeanTool.CSharp.Net8.WPF
{
    public class DynamicDataGridColumnDefinition
    {
        /// <summary>
        /// 顯示的標題
        /// </summary>
        public string Header { get; set; } = string.Empty;

        /// <summary>
        /// 資料來源的屬性名稱 (例如: "Name", "Age")
        /// </summary>
        public string BindingPath { get; set; } = string.Empty;

        /// <summary>
        /// 欄位寬度 (支援 100, *, Auto)
        /// </summary>
        public DataGridLength Width { get; set; } = DataGridLength.Auto;

        /// <summary>
        /// 格式化字串 (選填，例如 "C0", "yyyy-MM-dd")
        /// </summary>
        public string StringFormat { get; set; }

        /// <summary>
        /// 是否唯讀
        /// </summary>
        public bool IsReadOnly { get; set; } = false;
    }
}
