using FluentAssertions;
using Microsoft.Extensions.DependencyInjection; // 用於 IServiceCollection 模擬 (如果需要測 Extension)
using RichardSzalay.MockHttp;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SeanTool.CSharp.Net8.Test
{
    public class ApiToolExtensionsTest
    {
        [Fact(DisplayName = "AddApiTool 應成功註冊 IApiTool 為 Singleton")]
        public void AddApiTool_ShouldRegisterService_AsSingleton()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddApiTool();
            var provider = services.BuildServiceProvider();

            // Assert 1: 檢查是否能解析介面
            var apiTool = provider.GetService<IApiTool>();
            apiTool.Should().NotBeNull("因為 AddApiTool 應該要註冊 IApiTool");
            apiTool.Should().BeOfType<ApiTool>("實作型別應為 ApiTool");

            // Assert 2: 檢查生命週期 (Singleton)
            var apiTool2 = provider.GetService<IApiTool>();
            apiTool.Should().BeSameAs(apiTool2, "因為註冊為 Singleton，多次解析應得到同一個執行個體");
        }

        [Fact(DisplayName = "AddApiTool 應正確設定 HttpClient 的 Timeout 為 30 秒")]
        public void AddApiTool_ShouldConfigureHttpClient_WithCorrectTimeout()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddApiTool();
            var provider = services.BuildServiceProvider();

            // 取得 IHttpClientFactory 來產生具名 Client
            var factory = provider.GetRequiredService<IHttpClientFactory>();
            var client = factory.CreateClient("ApiTool");

            // Assert
            // 驗證擴充方法內的 client.Timeout = TimeSpan.FromSeconds(30); 是否生效
            client.Timeout.Should().Be(TimeSpan.FromSeconds(30));
        }
    }

    public class ApiToolUnitTest_Online
    {
        private readonly ApiTool _apiTool;

        public class PostPayloadTest
        {
            [JsonPropertyName("title")]
            public string Title { get; set; } = string.Empty;

            [JsonPropertyName("body")]
            public string Body { get; set; } = string.Empty;

            [JsonPropertyName("userId")]
            public int UserId { get; set; }
        }

        public ApiToolUnitTest_Online()
        {
            // 1. 建立一個簡單的 Factory，回傳 "真實" 的 HttpClient
            IHttpClientFactory realFactory = new RealHttpClientFactory();

            // 2. 注入 Factory 建立 ApiTool 實體
            _apiTool = new ApiTool(realFactory);
        }

        [Fact(DisplayName = "線上測試 - GetAsync 應能取得真實資料")]
        public async Task GetAsyncOnlineTest()
        {
            // Arrange
            var url = "https://jsonplaceholder.typicode.com/posts/1";

            // Act
            // 使用實體方法 _apiTool
            var result = await _apiTool.GetAsync<JsonElement>(url);

            // Assert
            // 使用 FluentAssertions 風格 (或保持 Assert.Equal 亦可)
            result.GetProperty("id").GetInt32().Should().Be(1);
        }

        [Fact(DisplayName = "線上測試 - PostAsync 應能成功發送資料")]
        public async Task PostAsyncOnlineTest()
        {
            // Arrange
            var url = "https://jsonplaceholder.typicode.com/posts";

            PostPayloadTest payload = new PostPayloadTest
            {
                Title = "foo",
                Body = "bar",
                UserId = 1
            };

            // Act
            // 預期回傳型別為 JsonElement
            var result = await _apiTool.PostAsync<PostPayloadTest, JsonElement>(url, payload);

            // Assert
            // JsonPlaceholder 對於成功的 POST 請求，通常會回傳固定的 ID 101
            result.GetProperty("id").GetInt32().Should().Be(101);

            // 驗證回傳的內容是否包含剛剛傳送的 title
            result.GetProperty("title").GetString().Should().Be("foo");
        }

        // ==========================================
        // 輔助類別：真實的 HttpClientFactory
        // 作用：回傳一個真的能上網的 HttpClient
        // ==========================================
        private class RealHttpClientFactory : IHttpClientFactory
        {
            public HttpClient CreateClient(string name)
            {
                // 每次都回傳一個新的真實 Client
                // 這裡沒有設定 BaseAddress，因為測試案例中的 URL 都是完整的絕對路徑
                return new HttpClient();
            }
        }
    }

    public class ApiToolUnitTest_Local : IDisposable
    {
        private readonly MockHttpMessageHandler _MockHttp;
        private readonly HttpClient _MockHttpClient;
        private readonly ApiTool _ApiTool;

        // 用來模擬回傳的資料模型
        private class TestResponse
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
        }

        public ApiToolUnitTest_Local()
        {
            // 1. 初始化 Mock Http Handler
            _MockHttp = new MockHttpMessageHandler();

            // 2. 建立帶有 Mock Handler 的 HttpClient
            _MockHttpClient = _MockHttp.ToHttpClient();
            // 重要：必須設定 BaseAddress，否則 ApiTool 若只傳入相對路徑會報錯
            _MockHttpClient.BaseAddress = new Uri("http://test.com/");

            // 3. 建立一個假的 Factory，讓它永遠回傳上面那個 Mock 的 Client
            IHttpClientFactory fakeFactory = new FakeHttpClientFactory(_MockHttpClient);

            // 4. 正常實例化 ApiTool (Dependency Injection)
            _ApiTool = new ApiTool(fakeFactory);
        }

        public void Dispose()
        {
            _MockHttpClient.Dispose();
            _MockHttp.Dispose();
        }

        [Fact(DisplayName = "GetAsync 應能正確解析 JSON 並回傳物件")]
        public async Task GetAsync_ShouldReturnObject_WhenResponseIsSuccess()
        {
            // Arrange
            var relativeUrl = "/api/users/1"; // 使用相對路徑測試
            var expectedResponse = new TestResponse { Id = 1, Name = "Sean" };

            // 設定 Mock：當呼叫此 URL 時，回傳 200 OK 與 JSON
            _MockHttp.When("http://test.com/api/users/1")
                     .Respond("application/json", JsonSerializer.Serialize(expectedResponse));

            // Act (使用實體方法呼叫)
            var result = await _ApiTool.GetAsync<TestResponse>(relativeUrl);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(1);
            result.Name.Should().Be("Sean");
        }

        [Fact(DisplayName = "GetAsync 應能正確處理 QueryString 與 Header")]
        public async Task GetAsync_ShouldIncludeQueryStringAndHeaders()
        {
            // Arrange
            var url = "http://test.com/api/search";
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
            // 注意：MockHttp 會比對完整的 QueryString
            _MockHttp.Expect("http://test.com/api/search?keyword=csharp&page=1")
                     .WithHeaders("X-Custom-Header", "TestValue")
                     .WithHeaders("Authorization", "Bearer secret-token")
                     .Respond("application/json", "{}");

            // Act
            await _ApiTool.GetAsync<object>(url, headers, queryParams, token);

            // Assert
            _MockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact(DisplayName = "GetAsync 遇到 404 時應拋出 HttpRequestException")]
        public async Task GetAsync_ShouldThrowException_WhenStatusCodeIs404()
        {
            // Arrange
            var url = "http://test.com/api/notfound";

            _MockHttp.When(url)
                     .Respond(HttpStatusCode.NotFound);

            // Act
            // 使用實體方法
            Func<Task> act = async () => await _ApiTool.GetAsync<object>(url);

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
            _MockHttp.Expect(HttpMethod.Post, url)
                     .WithContent(JsonSerializer.Serialize(payload, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }))
                     .Respond("application/json", JsonSerializer.Serialize(responseObj));

            // Act
            var result = await _ApiTool.PostAsync<TestResponse, JsonElement>(url, payload);

            // Assert
            _MockHttp.VerifyNoOutstandingExpectation();
            result.GetProperty("Success").GetBoolean().Should().BeTrue();
        }

        [Fact(DisplayName = "GetAsync 遇到 204 No Content 應回傳 default")]
        public async Task GetAsync_ShouldReturnDefault_WhenResponseIs204()
        {
            // Arrange
            var url = "http://test.com/api/empty";

            _MockHttp.When(url)
                     .Respond(HttpStatusCode.NoContent);

            // Act
            var result = await _ApiTool.GetAsync<TestResponse>(url);

            // Assert
            result.Should().BeNull();
        }

        // ==========================================
        // 輔助類別：偽造的 IHttpClientFactory
        // 作用：不管呼叫 CreateClient 傳什麼名稱，永遠回傳我們設定好的 MockClient
        // ==========================================
        private class FakeHttpClientFactory : IHttpClientFactory
        {
            private readonly HttpClient _client;

            public FakeHttpClientFactory(HttpClient client)
            {
                _client = client;
            }

            public HttpClient CreateClient(string name)
            {
                // 直接回傳同一個 Mock Client
                return _client;
            }
        }
    }
}