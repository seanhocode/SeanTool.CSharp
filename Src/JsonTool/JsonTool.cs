using System.Text.Json;

namespace SeanTool.CSharp
{
    public static class JsonTool
    {
        /// <summary>
        /// 取得Json的SubPropertyList
        /// </summary>
        /// <param name="jsonFilePath"></param>
        /// <param name="rootProperty"></param>
        /// <returns>SubPropertyNameList</returns>
        public static List<string> GetJsonSubPropertyList(string jsonFilePath, string rootProperty)
        {
            string jsonString = File.ReadAllText(jsonFilePath);

            using JsonDocument doc = JsonDocument.Parse(jsonString);
            JsonElement root = doc.RootElement;

            List<string> result = new List<string>();

            if (root.TryGetProperty(rootProperty, out JsonElement targetElement) && targetElement.ValueKind == JsonValueKind.Object)
            {
                foreach (JsonProperty property in targetElement.EnumerateObject())
                {
                    result.Add(property.Name);
                }
            }

            return result;
        }

        /// <summary>
        /// 將SubProperty的值帶入Model
        /// </summary>
        /// <typeparam name="T">SubProperty對應的Model</typeparam>
        /// <param name="jsonFilePath"></param>
        /// <param name="rootProperty"></param>
        /// <param name="subProperty"></param>
        /// <returns>new SubProperty對應的Model</returns>
        public static T GetSinglePropertyByListJson<T>(string jsonFilePath, string rootProperty, string subProperty) where T : new()
        {
            string jsonString = File.ReadAllText(jsonFilePath);
            T result = new T();

            using JsonDocument doc = JsonDocument.Parse(jsonString);
            JsonElement root = doc.RootElement;

            if (root.TryGetProperty(rootProperty, out JsonElement jsonElement))
            {
                if (jsonElement.TryGetProperty(subProperty, out JsonElement subElement))
                {
                    result = JsonSerializer.Deserialize<T>(subElement.GetRawText()) ?? new T();
                }
            }

            return result;
        }

        /// <summary>
        /// 將Model存入Json
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="jsonFilePath"></param>
        /// <param name="rootProperty"></param>
        /// <param name="subPropertyKey"></param>
        /// <param name="subPropertyValue"></param>
        public static void SaveSinglePropertyToListJson<T>(string jsonFilePath, string rootProperty, string subPropertyKey, T subPropertyValue)
        {
            string jsonString = File.ReadAllText(jsonFilePath);

            JsonSerializerOptions options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            // 反序列化成 Dictionary
            Dictionary<string, object> dict = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonString, options)
                       ?? new Dictionary<string, object>();

            if (dict.TryGetValue(rootProperty, out object? rootObj))
            {
                // rootProperty 本身也是個 Dictionary
                Dictionary<string, object> rootDict = JsonSerializer.Deserialize<Dictionary<string, object>>(rootObj.ToString()!)
                              ?? new Dictionary<string, object>();

                // 更新或新增子屬性
                rootDict[subPropertyKey] = subPropertyValue;

                // 放回去
                dict[rootProperty] = rootDict;
            }
            else
            {
                // 如果根屬性不存在 → 建立一個
                dict[rootProperty] = new Dictionary<string, object>
                {
                    { subPropertyKey, subPropertyValue }
                };
            }

            // 轉回 JSON
            string updatedJson = JsonSerializer.Serialize(dict, options);

            // 寫回檔案
            File.WriteAllText(jsonFilePath, updatedJson);
        }
    }
}
