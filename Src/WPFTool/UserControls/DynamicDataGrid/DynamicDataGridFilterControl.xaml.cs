using System.Windows.Controls;

namespace SeanTool.CSharp.WPF
{
    public partial class DynamicDataGridFilterControl : UserControl
    {
        public static IReadOnlyList<DynamicDataGridFilterOperator> FilterOperators { get; } =
            Enum.GetValues<DynamicDataGridFilterOperator>();

        public DynamicDataGridFilterControl()
        {
            InitializeComponent();
        }
    }
}