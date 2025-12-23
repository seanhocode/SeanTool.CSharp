using System.Text.Json;

namespace SeanTool.CSharp
{
    public interface IApiTool
    {
        /// <summary>
        /// 呼叫 GET API
        /// </summary>
        /// <typeparam name="T">回傳資料型態</typeparam>
        /// <param name="url">API網址</param>
        /// <param name="headers">API Header</param>
        /// <param name="queryParams">網址參數</param>
        /// <param name="bearerToken">API BearerToken</param>
        /// <param name="options">Json序列化選項</param>
        /// <param name="cancellationToken">取消權杖</param>
        /// <returns>API呼叫結果</returns>
        Task<T?> GetAsync<T>(string url, Dictionary<string, string>? headers = null, Dictionary<string, string>? queryParams = null, string? bearerToken = null, JsonSerializerOptions? options = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// 呼叫 POST API
        /// </summary>
        /// <typeparam name="TRequest">Content資料型態</typeparam>
        /// <typeparam name="TResponse">回傳資料型態</typeparam>
        /// <param name="url">API網址</param>
        /// <param name="payload">Content</param>
        /// <param name="headers">API Header</param>
        /// <param name="queryParams">網址參數</param>
        /// <param name="bearerToken">API BearerToken</param>
        /// <param name="options">Json序列化選項</param>
        /// <param name="cancellationToken">取消權杖</param>
        /// <returns>API呼叫結果</returns>
        Task<TResponse?> PostAsync<TRequest, TResponse>(string url, TRequest payload, Dictionary<string, string>? headers = null, Dictionary<string, string>? queryParams = null, string? bearerToken = null, JsonSerializerOptions? options = null, CancellationToken cancellationToken = default);
    }
}