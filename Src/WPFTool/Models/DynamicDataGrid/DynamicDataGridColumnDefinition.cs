using System.Windows.Controls;
using SeanTool.CSharp.WPFTool.Enums.Filter;

namespace SeanTool.CSharp.WPFTool.Models.DynamicDataGrid
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

        /// <summary>
        /// 篩選值型別；未指定時由欄位型別自動推斷
        /// </summary>
        public FilterValueType? FilterValueType { get; set; }

    }
}
