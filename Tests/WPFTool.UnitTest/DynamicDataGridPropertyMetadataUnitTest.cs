using SeanTool.CSharp.WPFTool.Models.DynamicDataGrid;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Data;
using Xunit;

namespace SeanTool.CSharp.WPFTool.Test
{
    public class DynamicDataGridPropertyMetadataUnitTest
    {
        /// <summary>
        /// 測試：元數據讀取 - 支援 DisplayName 和唯讀屬性
        /// 驗證元數據掃描：DisplayNameAttribute 正確讀取、唯讀屬性正確標記
        /// </summary>
        [Fact]
        public void Metadata_UsesDisplayNameAndKeepsReadOnlyProperty()
        {
            IReadOnlyList<DynamicDataGridPropertyMetadata> metadata =
                DynamicDataGridPropertyMetadata.Create(typeof(Person));

            Assert.Equal(new[] { "Name", "Age", "Created", "Read only" }, metadata.Select(item => item.Header));
            Assert.True(metadata.Single(item => item.Name == nameof(Person.ReadOnly)).IsReadOnly);
        }

        /// <summary>
        /// 測試：DataTable 支援 - 透過 DataRowView 樣本掃描出 DataTable 的實際欄位
        /// 驗證 DataTable 資料源：欄位名稱、型別(Nullable 展開)、唯讀狀態均來自 DataColumn 而非 DataRowView 的 CLR 屬性
        /// </summary>
        [Fact]
        public void Metadata_FromDataTableSample_ReflectsDataColumns()
        {
            DataTable table = new DataTable();
            table.Columns.Add("Name", typeof(string));
            table.Columns.Add("Age", typeof(int));
            DataColumn readOnlyColumn = table.Columns.Add("Fixed", typeof(string));
            readOnlyColumn.ReadOnly = true;
            table.Rows.Add("Alice", 31, "x");
            DataView view = table.DefaultView;

            IReadOnlyList<DynamicDataGridPropertyMetadata> metadata =
                DynamicDataGridPropertyMetadata.Create(typeof(DataRowView), view[0]);

            Assert.Equal(new[] { "Age", "Fixed", "Name" }, metadata.Select(item => item.Name).OrderBy(name => name));
            Assert.Equal(typeof(int), metadata.Single(item => item.Name == "Age").PropertyType);
            Assert.True(metadata.Single(item => item.Name == "Fixed").IsReadOnly);
            Assert.False(metadata.Single(item => item.Name == "Name").IsReadOnly);
        }

        /// <summary>
        /// 測試：未提供樣本實例時，DataRowView 型別本身沒有動態欄位可掃描（只會看到其固定 CLR 屬性）
        /// 驗證備援行為：不會拋例外，僅回退為型別本身的屬性
        /// </summary>
        [Fact]
        public void Metadata_FromDataRowViewType_WithoutSample_DoesNotThrow()
        {
            IReadOnlyList<DynamicDataGridPropertyMetadata> metadata =
                DynamicDataGridPropertyMetadata.Create(typeof(DataRowView));

            Assert.DoesNotContain(metadata, item => item.Name == "Age");
        }

        private sealed class Person
        {
            [Display(Order = 2)]
            public int Age { get; set; }

            [Display(Order = 3)]
            public DateTime Created { get; set; }

            [DisplayName("Name")]
            [Display(Order = 1)]
            public string? Name { get; set; }

            [DisplayName("Read only")]
            [Display(Order = 4)]
            public string ReadOnly => "value";
        }
    }
}
