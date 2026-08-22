using System.Data;
using SeanTool.CSharp.WPFTool.Enums.ModelEditor;
using SeanTool.CSharp.WPFTool.Models.ModelEditor;
using Xunit;

namespace SeanTool.CSharp.WPFTool.Test
{
    /// <summary>
    /// ModelEditorViewModel 單元測試集合
    /// 驗證 ModelEditor 透過共用 FieldAnalyzer 對一般 CLR 物件與 DataTable(DataRowView) 皆能自動分析欄位並編輯
    /// </summary>
    public class ModelEditorViewModelUnitTest
    {
        /// <summary>
        /// 測試：DataTable 支援 - 傳入 DataRowView 時，能依 DataTable 的 Columns 自動產生可編輯的 PropertyItem
        /// </summary>
        [Fact]
        public void Constructor_WithDataRowView_BuildsPropertiesFromDataColumns()
        {
            DataTable table = CreateSampleTable();
            DataRowView row = table.DefaultView[0];

            var viewModel = new ModelEditorViewModel(row);

            Assert.Equal(new[] { "Age", "Fixed", "Name" }, viewModel.Properties.Select(p => p.PropertyName).OrderBy(n => n));

            PropertyItem name = viewModel.Properties.Single(p => p.PropertyName == "Name");
            Assert.Equal("Alice", name.Value);
            Assert.False(name.IsReadOnly);

            PropertyItem fixedField = viewModel.Properties.Single(p => p.PropertyName == "Fixed");
            Assert.True(fixedField.IsReadOnly);
        }

        /// <summary>
        /// 測試：DataTable 支援 - 編輯 DataRowView 欄位後儲存(ApplyChange)可正確寫回 DataTable
        /// </summary>
        [Fact]
        public void ApplyChange_WithDataRowView_WritesBackToDataTable()
        {
            DataTable table = CreateSampleTable();
            DataRowView row = table.DefaultView[0];
            var viewModel = new ModelEditorViewModel(row);

            PropertyItem age = viewModel.Properties.Single(p => p.PropertyName == "Age");
            age.Value = "40";
            age.ApplyChange();

            Assert.Equal(40, table.Rows[0]["Age"]);
        }

        /// <summary>
        /// 測試：唯讀欄位保護 - DataColumn.ReadOnly=true 的欄位即使暫存值改變，ApplyChange 也不會寫回
        /// </summary>
        [Fact]
        public void ApplyChange_ReadOnlyDataColumn_IsNotWrittenBack()
        {
            DataTable table = CreateSampleTable();
            DataRowView row = table.DefaultView[0];
            var viewModel = new ModelEditorViewModel(row);

            PropertyItem fixedField = viewModel.Properties.Single(p => p.PropertyName == "Fixed");
            fixedField.Value = "changed";
            fixedField.ApplyChange();

            Assert.Equal("x", table.Rows[0]["Fixed"]);
        }

        /// <summary>
        /// 測試：一般 CLR 物件 - 既有行為不受共用欄位分析器影響，仍可正常掃描與編輯
        /// </summary>
        [Fact]
        public void Constructor_WithPlainObject_BuildsPropertiesFromClrProperties()
        {
            var person = new Person { Name = "Alice", Age = 30 };

            var viewModel = new ModelEditorViewModel(person);

            Assert.Equal(new[] { "Age", "Name" }, viewModel.Properties.Select(p => p.PropertyName));
            Assert.Equal(EditorInputType.Number, viewModel.Properties.Single(p => p.PropertyName == "Age").InputType);
        }

        private static DataTable CreateSampleTable()
        {
            var table = new DataTable();
            table.Columns.Add("Name", typeof(string));
            table.Columns.Add("Age", typeof(int));
            DataColumn fixedColumn = table.Columns.Add("Fixed", typeof(string));
            fixedColumn.ReadOnly = true;
            table.Rows.Add("Alice", 31, "x");
            return table;
        }

        private sealed class Person
        {
            public string? Name { get; set; }
            public int Age { get; set; }
        }
    }
}
