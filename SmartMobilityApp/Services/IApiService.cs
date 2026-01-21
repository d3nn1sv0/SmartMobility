using SmartMobilityApp.Models;

namespace SmartMobilityApp.Services;

public interface IApiService
{
    void SetAuthToken(string? token);
    Task<T?> GetAsync<T>(string endpoint);
    Task<TResponse?> PostAsync<TRequest, TResponse>(string endpoint, TRequest data);
    Task<bool> PostAsync<TRequest>(string endpoint, TRequest data);
}
