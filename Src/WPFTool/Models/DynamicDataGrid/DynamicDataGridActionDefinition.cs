using System.Windows.Controls;

namespace SeanTool.CSharp.WPFTool.Models.DynamicDataGrid
{
    public class DynamicDataGridActionDefinition
    {
        public string Header { get; set; } = string.Empty;

        public string Content { get; set; } = "執行";

        public DataGridLength Width { get; set; } = DataGridLength.Auto;

        public Action<object> Action { get; set; } = _ => { };
    }
}
