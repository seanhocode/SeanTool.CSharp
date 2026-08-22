using System.Data;
using SeanTool.CSharp.WPFTool.Models;
using Xunit;

namespace SeanTool.CSharp.WPFTool.Test
{
    /// <summary>
    /// DataSourceResolver 單元測試集合
    /// 驗證 DynamicDataGrid、ModelEditor 共用的資料來源轉換邏輯(DataTable/DataSet 等 IListSource 轉為可列舉集合)
    /// </summary>
    public class IEnumerableConverterUnitTest
    {
        /// <summary>
        /// 測試：null 資料來源 - 回傳 null
        /// </summary>
        [Fact]
        public void Convert_Null_ReturnsNull()
        {
            Assert.Null(IEnumerableConverter.Convert(null));
        }

        /// <summary>
        /// 測試：一般 IEnumerable - 原樣回傳，不做轉換
        /// </summary>
        [Fact]
        public void Convert_Enumerable_ReturnsSameInstance()
        {
            var list = new List<int> { 1, 2, 3 };

            var result = IEnumerableConverter.Convert(list);

            Assert.Same(list, result);
        }

        /// <summary>
        /// 測試：DataTable(IListSource) - 轉換為其 DefaultView，內容與 Rows 一致
        /// </summary>
        [Fact]
        public void Convert_DataTable_ReturnsDefaultView()
        {
            DataTable table = new DataTable();
            table.Columns.Add("Name", typeof(string));
            table.Rows.Add("Alice");
            table.Rows.Add("Bob");

            var result = IEnumerableConverter.Convert(table);

            var rows = Assert.IsType<DataView>(result).Cast<DataRowView>().ToArray();
            Assert.Equal(new[] { "Alice", "Bob" }, rows.Select(row => row["Name"]));
        }

        /// <summary>
        /// 測試：不支援的型別 - 拋出 ArgumentException
        /// </summary>
        [Fact]
        public void Convert_UnsupportedType_ThrowsArgumentException()
        {
            var ex = Assert.Throws<ArgumentException>(() => IEnumerableConverter.Convert(42));
            Assert.Contains("IEnumerable", ex.Message);
        }
    }
}
