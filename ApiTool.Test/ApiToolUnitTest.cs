using System.Net;
using System.Reflection;
using System.Text.Json;
using FluentAssertions;
using RichardSzalay.MockHttp;

namespace SeanTool.CSharp.Net8.Test
{
    // 為了避免並行測試導致 Static 欄位互相干擾，設定為循序執行
    [Collection("Sequential")]
    public class ApiToolUnitTest : IDisposable
    {
        private readonly MockHttpMessageHandler _mockHttp;

        // 用來模擬回傳的資料模型
        private class TestResponse
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
        }

        public ApiToolUnitTest()
        {
            // 初始化 Mock Http Handler
            _mockHttp = new MockHttpMessageHandler();

            // 建立帶有 Mock Handler 的 HttpClient
            var mockClient = _mockHttp.ToHttpClient();
            mockClient.BaseAddress = new Uri("http://test.com/");

            // 【關鍵步驟】使用 Reflection 強制替換 ApiTool 內部的 _Client
            // 注意：因為是 static readonly，這是一種 Hack，但在測試 legacy 或 static code 時很有用
            var field = typeof(ApiTool).GetField("_Client", BindingFlags.Static | BindingFlags.NonPublic);
            if (field != null)
            {
                field.SetValue(null, mockClient);
            }
        }

        public void Dispose()
        {
            // 測試結束後，最好把 _Client 重置回一個新的 HttpClient，避免影響其他測試
            // (雖然在這裡每次測試都會 new 一個新的 mockClient 覆蓋過去，但保持習慣較好)
            _mockHttp.Dispose();
        }

        [Fact(DisplayName = "GetAsync 應能正確解析 JSON 並回傳物件")]
        public async Task GetAsync_ShouldReturnObject_WhenResponseIsSuccess()
        {
            // Arrange
            var expectedUrl = "http://test.com/api/users/1";
            var expectedResponse = new TestResponse { Id = 1, Name = "Sean" };

            // 設定 Mock：當呼叫此 URL 時，回傳 200 OK 與 JSON
            _mockHttp.When(expectedUrl)
                     .Respond("application/json", JsonSerializer.Serialize(expectedResponse));

            // Act
            var result = await ApiTool.GetAsync<TestResponse>(expectedUrl);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(1);
            result.Name.Should().Be("Sean");
        }

        [Fact(DisplayName = "GetAsync 應能正確處理 QueryString 與 Header")]
        public async Task GetAsync_ShouldIncludeQueryStringAndHeaders()
        {
            // Arrange
            var baseUrl = "http://test.com/api/search";
            var queryParams = new Dictionary<string, string>
            {
                { "keyword", "csharp" },
                { "page", "1" }
            };
            var headers = new Dictionary<string, string>
            {
                { "X-Custom-Header", "TestValue" }
            };
            var token = "secret-token";

            // 設定 Mock：預期會收到帶有參數的 URL 和特定的 Header
            _mockHttp.Expect("http://test.com/api/search?keyword=csharp&page=1")
                     .WithHeaders("X-Custom-Header", "TestValue")
                     .WithHeaders("Authorization", "Bearer secret-token")
                     .Respond("application/json", "{}");

            // Act
            await ApiTool.GetAsync<object>(baseUrl, headers, queryParams, token);

            // Assert
            // 驗證是否有符合預期的請求被發送 (沒有符合會拋出例外)
            _mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact(DisplayName = "GetAsync 遇到 404 時應拋出 HttpRequestException")]
        public async Task GetAsync_ShouldThrowException_WhenStatusCodeIs404()
        {
            // Arrange
            var url = "http://test.com/api/notfound";

            _mockHttp.When(url)
                     .Respond(HttpStatusCode.NotFound);

            // Act
            Func<Task> act = async () => await ApiTool.GetAsync<object>(url);

            // Assert
            await act.Should().ThrowAsync<HttpRequestException>();
        }

        [Fact(DisplayName = "PostAsync 應正確序列化 Payload 並回傳結果")]
        public async Task PostAsync_ShouldSerializePayload_AndReturnResponse()
        {
            // Arrange
            var url = "http://test.com/api/users";
            var payload = new TestResponse { Id = 99, Name = "New User" };
            var responseObj = new { Success = true, Id = 100 };

            // 設定 Mock：攔截 POST 請求，並檢查傳送出去的 JSON 內容
            _mockHttp.Expect(HttpMethod.Post, url)
                     .WithContent(JsonSerializer.Serialize(payload, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }))
                     .Respond("application/json", JsonSerializer.Serialize(responseObj));

            // Act
            // 這裡使用 dynamic 或是另外定義一個 Response Class 都可以
            var result = await ApiTool.PostAsync<TestResponse, JsonElement>(url, payload);

            // Assert
            _mockHttp.VerifyNoOutstandingExpectation();
            result.GetProperty("Success").GetBoolean().Should().BeTrue();
        }

        [Fact(DisplayName = "GetAsync 遇到 204 No Content 應回傳 default")]
        public async Task GetAsync_ShouldReturnDefault_WhenResponseIs204()
        {
            // Arrange
            var url = "http://test.com/api/empty";

            _mockHttp.When(url)
                     .Respond(HttpStatusCode.NoContent);

            // Act
            var result = await ApiTool.GetAsync<TestResponse>(url);

            // Assert
            result.Should().BeNull();
        }
    }
}