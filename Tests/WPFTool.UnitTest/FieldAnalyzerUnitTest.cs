using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Data;
using SeanTool.CSharp.WPFTool.Models.Fields;
using Xunit;

namespace SeanTool.CSharp.WPFTool.Test
{
    /// <summary>
    /// FieldAnalyzer / FieldDescriptor 單元測試集合
    /// 驗證共用欄位分析器對一般 CLR 物件與 DataTable(DataRowView) 皆能正確掃描欄位、讀寫值與中繼資料
    /// </summary>
    public class FieldAnalyzerUnitTest
    {
        /// <summary>
        /// 測試：一般 CLR 型別 - 掃描出所有可讀屬性，DisplayName/Order 取自 Attribute
        /// </summary>
        [Fact]
        public void Analyze_ClrType_ReturnsAllReadableProperties()
        {
            IReadOnlyList<FieldDescriptor> fields = FieldAnalyzer.Analyze(typeof(Person));

            Assert.Equal(
                new[] { "Name", "Age", "Created", "ReadOnly" },
                fields.OrderBy(f => f.Order).ThenBy(f => f.Name).Select(f => f.Name));

            FieldDescriptor name = fields.Single(f => f.Name == nameof(Person.Name));
            Assert.Equal("顯示名稱", name.DisplayName);
            Assert.Equal(1, name.Order);
            Assert.False(name.IsReadOnly);

            FieldDescriptor readOnly = fields.Single(f => f.Name == nameof(Person.ReadOnly));
            Assert.True(readOnly.IsReadOnly);
        }

        /// <summary>
        /// 測試：Nullable 屬性 - PropertyType 保留 Nullable&lt;T&gt;，NonNullableType 展開為底層型別
        /// </summary>
        [Fact]
        public void Analyze_NullableProperty_ExposesRawAndNonNullableType()
        {
            IReadOnlyList<FieldDescriptor> fields = FieldAnalyzer.Analyze(typeof(Person));

            FieldDescriptor age = fields.Single(f => f.Name == nameof(Person.Age));
            Assert.Equal(typeof(int?), age.PropertyType);
            Assert.Equal(typeof(int), age.NonNullableType);
        }

        /// <summary>
        /// 測試：GetValue/SetValue - 一般 CLR 物件可透過 FieldDescriptor 讀寫實例
        /// </summary>
        [Fact]
        public void GetValueSetValue_ClrInstance_RoundTrips()
        {
            var person = new Person { Name = "Alice", Age = 30 };
            FieldDescriptor name = FieldAnalyzer.Analyze(person).Single(f => f.Name == nameof(Person.Name));

            Assert.Equal("Alice", name.GetValue(person));

            name.SetValue(person, "Bob");

            Assert.Equal("Bob", person.Name);
        }

        /// <summary>
        /// 測試：自訂 Attribute - GetAttribute 可取得屬性上標記的自訂 Attribute
        /// </summary>
        [Fact]
        public void GetAttribute_ReturnsCustomAttribute()
        {
            FieldDescriptor tag = FieldAnalyzer.Analyze(typeof(Person)).Single(f => f.Name == nameof(Person.Name));

            Assert.NotNull(tag.GetAttribute<DisplayNameAttribute>());
            Assert.Null(tag.GetAttribute<RequiredAttribute>());
        }

        /// <summary>
        /// 測試：DataTable 支援 - 傳入 DataRowView 樣本時，欄位掃描結果依 DataTable 的 Columns 而非 DataRowView 的 CLR 屬性
        /// </summary>
        [Fact]
        public void Analyze_DataTableSample_ReflectsDataColumns()
        {
            DataTable table = CreateSampleTable();
            DataView view = table.DefaultView;

            IReadOnlyList<FieldDescriptor> fields = FieldAnalyzer.Analyze(typeof(DataRowView), view[0]);

            Assert.Equal(new[] { "Age", "Fixed", "Name" }, fields.Select(f => f.Name).OrderBy(n => n));
            Assert.Equal(typeof(int), fields.Single(f => f.Name == "Age").PropertyType);
            Assert.True(fields.Single(f => f.Name == "Fixed").IsReadOnly);
            Assert.False(fields.Single(f => f.Name == "Name").IsReadOnly);
        }

        /// <summary>
        /// 測試：DataTable 支援 - 以單一實例(DataRowView)呼叫 Analyze(object) 便利多載，等同傳入型別+樣本
        /// </summary>
        [Fact]
        public void Analyze_SingleDataRowViewInstance_ReturnsSameFieldsAsTypeOverload()
        {
            DataTable table = CreateSampleTable();
            DataRowView row = table.DefaultView[0];

            IReadOnlyList<FieldDescriptor> fields = FieldAnalyzer.Analyze(row);

            Assert.Equal(new[] { "Age", "Fixed", "Name" }, fields.Select(f => f.Name).OrderBy(n => n));
        }

        /// <summary>
        /// 測試：DataTable 支援 - 可透過 FieldDescriptor 直接讀寫 DataRowView 對應的儲存格
        /// </summary>
        [Fact]
        public void GetValueSetValue_DataRowView_RoundTrips()
        {
            DataTable table = CreateSampleTable();
            DataRowView row = table.DefaultView[0];

            FieldDescriptor name = FieldAnalyzer.Analyze(row).Single(f => f.Name == "Name");

            Assert.Equal("Alice", name.GetValue(row));

            name.SetValue(row, "Carol");

            Assert.Equal("Carol", table.Rows[0]["Name"]);
        }

        /// <summary>
        /// 測試：未提供樣本實例時，動態提供屬性的型別(DataRowView)沒有固定欄位可掃描，僅回退為型別本身的 CLR 屬性，不拋例外
        /// </summary>
        [Fact]
        public void Analyze_DataRowViewType_WithoutSample_FallsBackWithoutThrowing()
        {
            IReadOnlyList<FieldDescriptor> fields = FieldAnalyzer.Analyze(typeof(DataRowView));

            Assert.DoesNotContain(fields, f => f.Name == "Age");
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

        /// <summary>
        /// 測試：ReadOnlyAttribute - 標記 [ReadOnly(true)] 的屬性即使有 Setter 也應視為唯讀
        /// </summary>
        [Fact]
        public void Analyze_ReadOnlyAttribute_MarksPropertyAsReadOnly()
        {
            IReadOnlyList<FieldDescriptor> fields = FieldAnalyzer.Analyze(typeof(ReadOnlyAttributeModel));

            Assert.True(fields.Single(f => f.Name == nameof(ReadOnlyAttributeModel.Locked)).IsReadOnly);
            Assert.False(fields.Single(f => f.Name == nameof(ReadOnlyAttributeModel.Unlocked)).IsReadOnly);
        }

        private sealed class ReadOnlyAttributeModel
        {
            [ReadOnly(true)]
            public string Locked { get; set; } = "locked";

            public string Unlocked { get; set; } = "unlocked";
        }

        private sealed class Person
        {
            [DisplayName("顯示名稱")]
            [Display(Order = 1)]
            public string? Name { get; set; }

            [Display(Order = 2)]
            public int? Age { get; set; }

            [Display(Order = 3)]
            public DateTime Created { get; set; }

            [DisplayName("Read only")]
            [Display(Order = 4)]
            public string ReadOnly => "value";
        }
    }
}
