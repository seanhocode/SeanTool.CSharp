using SeanTool.CSharp.WPFTool.Models.DynamicDataGrid;
using System.Data;
using System.Windows;
using SeanTool.CSharp.WPFTool.Windows;

namespace SeanTool.CSharp.WPFTool.Test
{
    /// <summary>
    /// DynamicDataGridDataTableTestWindow.xaml 的互動邏輯
    /// </summary>
    /// <remarks>驗證 DynamicDataGrid 繫結 DataTable(DataView) 時，欄位/篩選皆能依 DataTable 的 Columns 自動掃描</remarks>
    public partial class DynamicDataGridDataTableTestWindow : Window
    {
        // 資料來源：DynamicDataGrid 內部會自動將 DataTable 轉為 DefaultView(DataView)，此處可直接繫結 DataTable
        public DataTable PersonTable { get; }

        public List<DynamicDataGridActionDefinition> ActionDefinitions { get; set; }

        public DynamicDataGridDataTableTestWindow()
        {
            PersonTable = LoadDynamicDataGridTestData();

            ActionDefinitions = new List<DynamicDataGridActionDefinition>
            {
                new DynamicDataGridActionDefinition
                {
                    Header = "操作",
                    Content = "編輯",
                    Action = dataTable => {
                        new ModelEditorWindow(dataTable).ShowDialog();
                    }
                }
            };

            this.DataContext = this;

            InitializeComponent();
        }

        private static DataTable LoadDynamicDataGridTestData()
        {
            var table = new DataTable();
            table.Columns.Add("ID", typeof(int));
            table.Columns.Add("Name", typeof(string));
            table.Columns.Add("Age", typeof(short));
            table.Columns.Add("BirthDate", typeof(DateTime));
            table.Columns.Add("IsEnabled", typeof(bool));

            var random = new Random(20260819);
            int dataCount = 1_000;
            for (int i = 0; i < dataCount; i++)
            {
                table.Rows.Add(
                    i,
                    $"User {random.Next(1, 1_000_000):D6}",
                    (short)random.Next(18, 80),
                    DateTime.Today.AddDays(-random.Next(0, 20_000)),
                    random.Next(2) == 1);
            }

            return table;
        }

        private void CheckDataValue(object sender, RoutedEventArgs e)
        {
            DataTable personTable = PersonTable;
            // 此處下中斷點檢查 personTable 內容
            MessageBox.Show(personTable.Rows.Count.ToString());
        }

        private void ShowSelectedItems(object sender, RoutedEventArgs e)
        {
            string names = string.Join(Environment.NewLine,
                PersonDataGrid.SelectedItems
                    .OfType<DataRowView>()
                    .Select(row => row["Name"]?.ToString()));

            MessageBox.Show(string.IsNullOrWhiteSpace(names) ? "目前沒有勾選項目。" : names,
                "選取項目名稱");
        }
    }
}
