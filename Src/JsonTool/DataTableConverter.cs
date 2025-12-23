using System.Data;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SeanTool.CSharp.Test
{
    public class DataTableConverter : JsonConverter<DataTable>
    {
        public override DataTable? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // 確保 JSON 是以 Array 開始 (例如: [{"id":1}, {"id":2}])
            if (reader.TokenType != JsonTokenType.StartArray)
            {
                throw new JsonException("JSON payload must be an array to convert to DataTable.");
            }

            DataTable dataTable = new DataTable();

            // 使用 JsonDocument 解析目前的 JSON 區塊，方便操作
            using (JsonDocument document = JsonDocument.ParseValue(ref reader))
            {
                JsonElement array = document.RootElement;

                // 如果是空陣列，直接回傳空 Table
                if (array.GetArrayLength() == 0)
                {
                    return dataTable;
                }

                // Step.1 根據第一筆資料建立欄位 (Columns)
                JsonElement firstElement = array[0];
                foreach (JsonProperty property in firstElement.EnumerateObject())
                {
                    // 先判斷 ValueKind 來決定欄位型別，若複雜情況建議預設 String
                    Type colType = property.Value.ValueKind switch
                    {
                        JsonValueKind.Number => typeof(decimal), // 或 double/int
                        JsonValueKind.True or JsonValueKind.False => typeof(bool),
                        JsonValueKind.String => typeof(string),
                        _ => typeof(object)
                    };

                    dataTable.Columns.Add(property.Name, colType);
                }

                // Step.2 填入資料 (Rows)
                foreach (JsonElement element in array.EnumerateArray())
                {
                    DataRow row = dataTable.NewRow();
                    foreach (JsonProperty property in element.EnumerateObject())
                    {
                        // 確保欄位存在 (避免 JSON 資料結構不一致)
                        if (dataTable.Columns.Contains(property.Name))
                        {
                            JsonElement value = property.Value;
                            // 處理 Null 與型別轉換
                            row[property.Name] = value.ValueKind == JsonValueKind.Null ? DBNull.Value : GetValue(value);
                        }
                    }
                    dataTable.Rows.Add(row);
                }
            }

            return dataTable;
        }

        // 輔助方法：將 JsonElement 轉為 C# 物件
        private object GetValue(JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.Number => element.GetDecimal(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.String => element.GetString() ?? string.Empty,
                _ => element.ToString()
            };
        }

        public override void Write(Utf8JsonWriter writer, DataTable value, JsonSerializerOptions options)
        {
            // Step.1 JSON 陣列開始符號 '['
            writer.WriteStartArray();

            foreach (DataRow row in value.Rows)
            {
                // Step.2 每一列代表一個 JSON 物件 '{'
                writer.WriteStartObject();

                foreach (DataColumn col in value.Columns)
                {
                    // 檢查 options 是否有設定命名原則 (例如 JsonNamingPolicy.CamelCase)，若有則進行轉換
                    string propertyName = col.ColumnName;
                    if (options.PropertyNamingPolicy != null)
                    {
                        propertyName = options.PropertyNamingPolicy.ConvertName(propertyName);
                    }

                    // 寫入屬性名稱 "key":
                    writer.WritePropertyName(propertyName);

                    // C# 的 null 與 ADO.NET 的 DBNull.Value 是不同的物件
                    // 明確檢查 DBNull，否則序列化可能會出錯或產生非預期的空物件
                    object cellValue = row[col];

                    if (cellValue == DBNull.Value || cellValue == null)
                    {
                        writer.WriteNullValue(); // 寫入 JSON 的 null
                    }
                    else
                    {
                        // Step.3 序列化實際數值
                        // 為了確保內部的 DateTime 或其他格式設定能被繼承使用傳入 options
                        JsonSerializer.Serialize(writer, cellValue, cellValue.GetType(), options);
                    }
                }

                // 該列物件結束 '}'
                writer.WriteEndObject();
            }

            // JSON 陣列結束 ']'
            writer.WriteEndArray();
        }
    }
}
