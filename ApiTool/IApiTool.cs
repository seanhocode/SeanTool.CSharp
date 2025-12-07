using System.Text.Json;

namespace SeanTool.CSharp.Net8
{
    public interface IApiTool
    {
        Task<T?> GetAsync<T>(string url, Dictionary<string, string>? headers = null, Dictionary<string, string>? queryParams = null, string? bearerToken = null, JsonSerializerOptions? options = null, CancellationToken cancellationToken = default);
        Task<TResponse?> PostAsync<TRequest, TResponse>(string url, TRequest payload, Dictionary<string, string>? headers = null, Dictionary<string, string>? queryParams = null, string? bearerToken = null, JsonSerializerOptions? options = null, CancellationToken cancellationToken = default);
    }
}