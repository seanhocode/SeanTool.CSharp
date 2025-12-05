using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace SeanTool.CSharp.Net8
{
    public static class ApiTool
    {
        // readonly 確保不會有人在程式其他地方不小心把 _client 換掉
        // _Client 會存在 Heap 記憶體，全體共用
        private static HttpClient _Client = new HttpClient();

        // 預設 JSON 選項 (Web 預設通常不分大小寫)
        private static readonly JsonSerializerOptions DefaultJsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        // Static Class 的建構子只會執行一次 (第一次呼叫此類別時)
        static ApiTool()
        {
            // 設定預設 timeout 為 30 秒
            _Client.Timeout = TimeSpan.FromSeconds(30);
        }

        public static async Task<T?> GetAsync<T>(
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

                // Step.1 建立獨立的請求 (HttpRequestMessage)
                // 這裡的 request 是區域變數，只屬於這次呼叫
                using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, GenRequestQueryString(url, queryParams));

                // Step.2 處理 Headers (含 Token)
                AddHeaders(request, headers, bearerToken);

                // Step.4 發送請求
                // ResponseHeadersRead : 只要伺服器回傳了標頭 (Headers)，就立刻把控制權交還給程式，不等內容 (Body) 下載完
                // ConfigureAwait(false) : 不強制回到原本的執行緒（Thread）繼續執行
                using HttpResponseMessage response = await _Client.SendAsync(
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
            catch (HttpRequestException )
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

        public static async Task<TResponse?> PostAsync<TRequest, TResponse>(
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

                // Step.1 建立 RequestMessage
                using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, GenRequestQueryString(url, queryParams));

                // Step.2 處理 Headers (含 Token)
                AddHeaders(request, headers, bearerToken);

                // Step.3 序列化 (直接轉為 JsonContent，內部實作為 Stream)
                // .NET 5+ 推薦使用 JsonContent.Create，它內部優化了記憶體與編碼
                request.Content = JsonContent.Create(payload, mediaType: null, options);

                // Step.4 發送請求
                // ResponseHeadersRead : 只要伺服器回傳了標頭 (Headers)，就立刻把控制權交還給程式，不等內容 (Body) 下載完
                using HttpResponseMessage response = await _Client.SendAsync(
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
        /// 設定 Header
        /// </summary>
        private static void AddHeaders(HttpRequestMessage request, Dictionary<string, string>? headers, string? bearerToken)
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
        private static string GenRequestQueryString(string url, Dictionary<string, string>? parameters)
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
