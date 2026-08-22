using System.Data;
using System.Windows;

namespace SeanTool.CSharp.WPFTool.Test
{
    /// <summary>
    /// ModelEditorDataTableTestWindow.xaml 的互動邏輯
    /// </summary>
    /// <remarks>驗證 ModelEditor 直接繫結 DataTable 時，會自動轉為第一列(DataRowView)，欄位皆能依 DataTable 的 Columns 自動掃描並編輯</remarks>
    public partial class ModelEditorDataTableTestWindow : Window
    {
        // 資料來源：DataTable，直接繫結給 ModelEditor.TargetObject，由 ModelEditor 自動轉為第一列(DataRowView)進行編輯
        public DataTable PersonTable { get; }

        public ModelEditorDataTableTestWindow()
        {
            PersonTable = CreateSampleTable();

            this.DataContext = this;

            InitializeComponent();
        }

        private static DataTable CreateSampleTable()
        {
            var table = new DataTable();
            table.Columns.Add("Name", typeof(string));
            table.Columns.Add("Age", typeof(int));
            table.Columns.Add("BirthDate", typeof(DateTime));
            table.Columns.Add("IsEnabled", typeof(bool));
            DataColumn idColumn = table.Columns.Add("ID", typeof(int));
            idColumn.ReadOnly = true; // 唯讀欄位：驗證 ModelEditor 能正確顯示但不可編輯

            table.Rows.Add("User 000001", 31, new DateTime(2000, 1, 1), true, 1);

            return table;
        }

        private void CheckModelValue(object sender, RoutedEventArgs e)
        {
            DataRow personRow = PersonTable.Rows[0];
            // 此處下中斷點檢查 personRow 內容
            MessageBox.Show($"ID: {personRow["ID"]}, Name: {personRow["Name"]}, Age: {personRow["Age"]}");
        }
    }
}
