using System.Text.Json;
using System.Text.Json.Serialization;

namespace SeanTool.CSharp.Net8.Test
{
    // 為了避免並行測試導致 Static 欄位互相干擾，設定為循序執行
    [Collection("Sequential")]
    public class ApiToolUnitTest_Online : IDisposable
    {
        private readonly HttpClient _Client = new HttpClient();

        public class PostPayloadTest
        {
            [JsonPropertyName("title")]
            public string Title { get; set; } = string.Empty;

            [JsonPropertyName("body")]
            public string Body { get; set; } = string.Empty;

            [JsonPropertyName("userId")]
            public int UserId { get; set; }
        }

        public void Dispose()
        {
            _Client.Dispose();
        }

        public ApiToolUnitTest_Online()
        {
            _Client = new HttpClient();
            ApiTool.SetClient(_Client);
        }

        [Fact]
        public async Task GetAsyncOnlineTest()
        {
            var url = "https://jsonplaceholder.typicode.com/posts/1";

            var result = await ApiTool.GetAsync<JsonElement>(url);

            Assert.Equal(1, result.GetProperty("id").GetInt32());
        }

        [Fact]
        public async Task PostAsyncOnlineTest()
        {
            var url = "https://jsonplaceholder.typicode.com/posts";

            PostPayloadTest payload = new PostPayloadTest
            {
                Title = "foo",
                Body = "bar",
                UserId = 1
            };

            // 預期回傳型別為 JsonElement，以便稍後驗證
            var result = await ApiTool.PostAsync<PostPayloadTest, JsonElement>(url, payload);

            // JsonPlaceholder 對於成功的 POST 請求，通常會回傳固定的 ID 101
            Assert.Equal(101, result.GetProperty("id").GetInt32());

            // 驗證回傳的內容是否包含剛剛傳送的 title
            Assert.Equal("foo", result.GetProperty("title").GetString());
        }
    }
}
