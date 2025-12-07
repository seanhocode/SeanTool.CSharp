using System.Data;
using System.Text.Json;

namespace SeanTool.CSharp.Net8.Test.Test
{
    public class DataTableConverterTests
    {
        private readonly JsonSerializerOptions _Options;

        public DataTableConverterTests()
        {
            // 初始化通用的設定，加入 Converter
            _Options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new DataTableConverter() }
            };
        }

        #region 序列化測試 (Write)

        [Fact]
        public void Serialize_DataTable_ShouldReturnCorrectJson()
        {
            // Arrange
            var dt = new DataTable();
            dt.Columns.Add("Id", typeof(int));
            dt.Columns.Add("Name", typeof(string));
            dt.Rows.Add(1, "Sean");

            // Act
            string json = JsonSerializer.Serialize(dt, _Options);

            // Assert
            // 預設沒有 NamingPolicy，欄位名稱維持原樣
            Assert.Contains(@"""Id"":1", json);
            Assert.Contains(@"""Name"":""Sean""", json);
        }

        [Fact]
        public void Serialize_WithCamelCasePolicy_ShouldConvertColumnNames()
        {
            // Arrange
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase, // 設定駝峰式命名
                Converters = { new DataTableConverter() }
            };

            var dt = new DataTable();
            dt.Columns.Add("UserId", typeof(int));
            dt.Columns.Add("UserName", typeof(string));
            dt.Rows.Add(100, "Admin");

            // Act
            string json = JsonSerializer.Serialize(dt, options);

            // Assert
            // 檢查是否轉為小寫開頭 (userId, userName)
            Assert.Contains(@"""userId"":100", json);
            Assert.Contains(@"""userName"":""Admin""", json);
        }

        [Fact]
        public void Serialize_WithDBNull_ShouldWriteJsonNull()
        {
            // Arrange
            var dt = new DataTable();
            dt.Columns.Add("Value", typeof(string));
            dt.Rows.Add(DBNull.Value); // 加入 DBNull

            // Act
            string json = JsonSerializer.Serialize(dt, _Options);

            // Assert
            // DBNull 應該被轉為 JSON 的 null
            Assert.Contains(@"""Value"":null", json);
        }

        [Fact]
        public void Serialize_EmptyDataTable_ShouldReturnEmptyArray()
        {
            // Arrange
            var dt = new DataTable();
            dt.Columns.Add("Col1");

            // Act
            string json = JsonSerializer.Serialize(dt, _Options);

            // Assert
            Assert.Equal("[]", json);
        }

        #endregion

        #region 反序列化測試 (Read)

        [Fact]
        public void Deserialize_ValidJsonArray_ShouldReturnDataTable()
        {
            // Arrange
            string json = @"[
                { ""Id"": 1, ""Name"": ""Test"", ""IsActive"": true },
                { ""Id"": 2, ""Name"": ""Test2"", ""IsActive"": false }
            ]";

            // Act
            var dt = JsonSerializer.Deserialize<DataTable>(json, _Options);

            // Assert
            Assert.NotNull(dt);
            Assert.Equal(2, dt.Rows.Count);

            // 驗證第一筆資料
            Assert.Equal(1m, dt.Rows[0]["Id"]); // 注意：您的 Converter 將 Number 轉為 decimal
            Assert.Equal("Test", dt.Rows[0]["Name"]);
            Assert.Equal(true, dt.Rows[0]["IsActive"]);
        }

        [Fact]
        public void Deserialize_JsonWithNull_ShouldConvertToDBNull()
        {
            // Arrange
            string json = @"[{ ""Id"": 1, ""Description"": null }]";

            // Act
            var dt = JsonSerializer.Deserialize<DataTable>(json, _Options);

            // Assert
            Assert.NotNull(dt);
            var row = dt.Rows[0];

            // 驗證 ID
            Assert.Equal(1m, row["Id"]);

            // 驗證 Description 是否為 DBNull
            Assert.Equal(DBNull.Value, row["Description"]);
        }

        [Fact]
        public void Deserialize_EmptyArray_ShouldReturnEmptyDataTable()
        {
            // Arrange
            string json = "[]";

            // Act
            var dt = JsonSerializer.Deserialize<DataTable>(json, _Options);

            // Assert
            Assert.NotNull(dt);
            Assert.Equal(0, dt.Rows.Count);
        }

        [Fact]
        public void Deserialize_NotArray_ShouldThrowJsonException()
        {
            // Arrange
            string json = "{}"; // 物件而非陣列

            // Act & Assert
            var exception = Assert.Throws<JsonException>(() =>
            {
                JsonSerializer.Deserialize<DataTable>(json, _Options);
            });

            Assert.Equal("JSON payload must be an array to convert to DataTable.", exception.Message);
        }

        [Fact]
        public void Deserialize_InconsistentColumns_ShouldIgnoreExtraColumns()
        {
            // Arrange
            // 第一筆決定欄位結構 (ColA)，第二筆多了一個 ColB
            string json = @"[
                { ""ColA"": ""Value1"" },
                { ""ColA"": ""Value2"", ""ColB"": ""Extra"" }
            ]";

            // Act
            var dt = JsonSerializer.Deserialize<DataTable>(json, _Options);

            // Assert
            Assert.NotNull(dt);
            Assert.True(dt.Columns.Contains("ColA"));
            Assert.False(dt.Columns.Contains("ColB")); // ColB 應該被忽略，因為第一筆沒有它
            Assert.Equal("Value2", dt.Rows[1]["ColA"]);
        }

        #endregion
    }
}
