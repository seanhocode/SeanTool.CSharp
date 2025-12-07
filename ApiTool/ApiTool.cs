using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace SeanTool.CSharp.Net8
{
    public static class ApiToolExtensions
    {
        public static IServiceCollection AddApiTool(this IServiceCollection services)
        {
            // Step.1 註冊 HttpClient (並在此設定全域 Timeout)
            services.AddHttpClient("ApiTool", client =>
            {
                // 建議在此處設定 Timeout，預設是 100秒
                client.Timeout = TimeSpan.FromSeconds(30);
                // 也可以設定 User-Agent 等預設標頭
                // client.DefaultRequestHeaders.Add("User-Agent", "SeanTool");
            });

            // Step.2 註冊 ApiTool
            // 注意：因為我們用了具名 Client ("ApiTool")，所以在 ApiTool 建構子需要調整一下拿到正確的 Client
            services.AddSingleton<IApiTool, ApiTool>();

            return services;
        }
    }

    public class ApiTool : IApiTool
    {
        // 在 .NET Core / .NET 5+ 之後，微軟強烈建議使用 IHttpClientFactory 來管理 HttpClient
        // 可以解決 DNS 重新整理的問題 (Socket Exhaustion)，並且更容易配合 Polly 做重試機制
        private readonly IHttpClientFactory _HttpClientFactory;

        // 預設 JSON 選項 (Web 預設通常不分大小寫)
        private static readonly JsonSerializerOptions DefaultJsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public ApiTool(IHttpClientFactory httpClientFactory)
        {
            _HttpClientFactory = httpClientFactory;
        }

        public async Task<T?> GetAsync<T>(
            string url,
            Dictionary<string, string>? headers = null,
            Dictionary<string, string>? queryParams = null,
            string? bearerToken = null,
            JsonSerializerOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                options ??= DefaultJsonOptions;

                // 每次建立新的 Client (由 Factory 管理底層連線池，不用擔心效能)
                using HttpClient client = CreateClient();

                // Step.1 建立獨立的請求 (HttpRequestMessage)
                // 這裡的 request 是區域變數，只屬於這次呼叫
                using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, GenRequestQueryString(url, queryParams));

                // Step.2 處理 Headers (含 Token)
                AddHeaders(request, headers, bearerToken);

                // Step.4 發送請求
                // ResponseHeadersRead : 只要伺服器回傳了標頭 (Headers)，就立刻把控制權交還給程式，不等內容 (Body) 下載完
                // ConfigureAwait(false) : 不強制回到原本的執行緒（Thread）繼續執行
                using HttpResponseMessage response = await client.SendAsync(
                    request,
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken).ConfigureAwait(false);

                response.EnsureSuccessStatusCode();

                // 處理 204 No Content
                if (response.StatusCode == HttpStatusCode.NoContent)
                {
                    return default;
                }

                // Step.5 取得回傳結果
                // 這裡只是拿到一個指向網路資料的「水管」，資料可能還在傳輸中
                using Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);

                // Step.6 轉換成 T 並回傳
                // 如果 JSON 是空的或格式不對，這裡可能會丟出例外或回傳 null
                // 直接對 Stream 進行反序列化
                // 注意：這裡必須用 await，因為它是一邊讀 Stream 一邊轉物件
                return await JsonSerializer.DeserializeAsync<T>(stream, options, cancellationToken).ConfigureAwait(false);
            }
            catch (HttpRequestException)
            {
                throw;
            }
            catch (JsonException ex) // 如果解析失敗
            {
                // 因為 stream 通常不能重讀 (CanSeek 為 false)
                // 所以實務上，如果追求極致效能，這筆 Log 只能紀錄 Url，無法紀錄 Content
                // 如果非要紀錄 Content，則不能用純 Stream 模式，或者要先 Copy 一份 Stream (會犧牲效能)
                ex.Data.Add("RequestUrl", url);
                throw;
            }
        }

        public async Task<TResponse?> PostAsync<TRequest, TResponse>(
            string url,
            TRequest payload,
            Dictionary<string, string>? headers = null,
            Dictionary<string, string>? queryParams = null,
            string? bearerToken = null,
            JsonSerializerOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                options ??= DefaultJsonOptions;

                using HttpClient client = CreateClient();

                // Step.1 建立 RequestMessage
                using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, GenRequestQueryString(url, queryParams));

                // Step.2 處理 Headers (含 Token)
                AddHeaders(request, headers, bearerToken);

                // Step.3 序列化 (直接轉為 JsonContent，內部實作為 Stream)
                // .NET 5+ 推薦使用 JsonContent.Create，它內部優化了記憶體與編碼
                request.Content = JsonContent.Create(payload, mediaType: null, options);

                // Step.4 發送請求
                // ResponseHeadersRead : 只要伺服器回傳了標頭 (Headers)，就立刻把控制權交還給程式，不等內容 (Body) 下載完
                using HttpResponseMessage response = await client.SendAsync(
                    request,
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken).ConfigureAwait(false);

                response.EnsureSuccessStatusCode();

                // 判斷 204 No Content
                if (response.StatusCode == HttpStatusCode.NoContent)
                {
                    return default;
                }

                // Step.5 取得回傳結果
                using Stream responseStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);

                // Step.6 從 Stream 轉物件
                return await JsonSerializer.DeserializeAsync<TResponse>(
                    responseStream,
                    options,
                    cancellationToken).ConfigureAwait(false);
            }
            catch (HttpRequestException)
            {
                throw;
            }
            catch (JsonException ex) // 如果解析失敗
            {
                // 因為 stream 通常不能重讀 (CanSeek 為 false)
                // 所以實務上，如果追求極致效能，這筆 Log 只能紀錄 Url，無法紀錄 Content
                // 如果非要紀錄 Content，則不能用純 Stream 模式，或者要先 Copy 一份 Stream (會犧牲效能)
                ex.Data.Add("RequestUrl", url);
                throw;
            }
        }

        /// <summary>
        /// 取得 Client 的輔助屬性或方法
        /// </summary>
        /// <returns></returns>
        private HttpClient CreateClient()
        {
            // 使用與註冊時相同的名稱，這樣才能吃到 Timeout 設定
            // 如果 AddHttpClient 沒有給名稱，這邊就用 CreateClient() 即可
            return _HttpClientFactory.CreateClient("ApiTool");
        }

        /// <summary>
        /// 設定 Header
        /// </summary>
        private void AddHeaders(HttpRequestMessage request, Dictionary<string, string>? headers, string? bearerToken)
        {
            if (headers != null)
            {
                foreach (KeyValuePair<string, string> header in headers)
                {
                    request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }

            if (!string.IsNullOrWhiteSpace(bearerToken))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
            }
        }

        /// <summary>
        /// 建立帶有查詢字串的完整 URL
        /// </summary>
        /// <param name="url"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        private string GenRequestQueryString(string url, Dictionary<string, string>? parameters)
        {
            if (parameters == null || parameters.Count == 0) return url;

            // 判斷原本 url 是否已經包含 '?'
            bool hasQuery = url.Contains('?');

            // 如果已有 '?'，則後續參數用 '&' 連接；否則用 '?' 開頭
            StringBuilder queryBuilder = new StringBuilder(hasQuery ? "&" : "?");

            bool isFirstParam = true;
            foreach (KeyValuePair<string, string> param in parameters)
            {
                if (!isFirstParam)
                {
                    queryBuilder.Append('&');
                }

                // 處理 null value 的情況，避免當機，改為空字串
                string value = param.Value != null ? Uri.EscapeDataString(param.Value) : string.Empty;
                queryBuilder.Append($"{param.Key}={value}");

                isFirstParam = false;
            }

            return $"{url}{queryBuilder}";
        }
    }
}
