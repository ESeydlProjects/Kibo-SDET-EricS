using System.Text;
using System.Text.Json;
using Kibo.TestingFramework.Handlers;
using Kibo.TestingFramework.Models;
using System.Net.Http.Json;

namespace Kibo.TestingFramework.Clients;

public class KiboApiClient : IDisposable
{
    private readonly HttpClient _httpClient;

    private static readonly JsonSerializerOptions s_camelCaseOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public KiboApiClient(string baseUrl, string tenantId, bool enableLogging = false)
    {
        var httpHandler = new HttpClientHandler();
        HttpMessageHandler handler;

        if (enableLogging)
        {
            handler = new ObservabilityHandler(enableLogging: true) { InnerHandler = httpHandler };
        }
        else
        {
            handler = httpHandler;
        }

        _httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(baseUrl),
            Timeout = TimeSpan.FromSeconds(30)
        };

        _httpClient.DefaultRequestHeaders.Add("x-kibo-tenant", tenantId);
    }

    /// <summary>
    /// Returns ApiResponse<T> with full observability (timing, correlation, logs)
    /// </summary>
    public async Task<ApiResponse<Order>> CreateOrderAsync(Order order)
    {
        var json = JsonSerializer.Serialize(order, s_camelCaseOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("/v1/orders", content);
        var orderData = await response.Content.ReadFromJsonAsync<Order>();

        return new ApiResponse<Order>(
            response,
            orderData,
            response.GetElapsedMs(),
            response.GetCorrelationId(),
            response.GetRequestLog(),
            response.GetResponseLog()
        );
    }

    /// <summary>
    /// Raw HTTP access for advanced scenarios
    /// </summary>
    public async Task<HttpResponseMessage> CreateOrderRawAsync(Order order)
    {
        var json = JsonSerializer.Serialize(order, s_camelCaseOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        return await _httpClient.PostAsync("/v1/orders", content);
    }

    /// <summary>
    /// Fluent logging toggle
    /// </summary>
    public KiboApiClient WithLogging()
    {
        return this;
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
        GC.SuppressFinalize(this);
    }

    public async Task<ApiResponse<Order>> GetOrderAsync(string orderId)
    {
        var response = await _httpClient.GetAsync($"/v1/orders/{orderId}");
        var orderData = await response.Content.ReadFromJsonAsync<Order>();

        return new ApiResponse<Order>(
            response, orderData,
            response.GetElapsedMs(),
            response.GetCorrelationId(),
            response.GetRequestLog(),
            response.GetResponseLog()
        );
    }

    /// <summary>Raw HTTP access for GET /v1/orders/{id}</summary>
    public async Task<HttpResponseMessage> GetOrderRawAsync(string orderId)
    {
        return await _httpClient.GetAsync($"/v1/orders/{orderId}");
    }
}

/// <summary>
/// Extension methods to extract observability data from responses
/// </summary>
public static class ObservabilityHelpers
{
    public static long GetElapsedMs(this HttpResponseMessage response) =>
        long.TryParse(
            GetHeader(response, "X-Kibo-Elapsed-Ms"),
            out var ms
        ) ? ms : 0;

    public static string GetCorrelationId(this HttpResponseMessage response) =>
        GetHeader(response, "X-Kibo-Correlation-Id");

    public static string GetRequestLog(this HttpResponseMessage response) =>
        GetHeader(response, "X-Kibo-Request-Log");

    public static string GetResponseLog(this HttpResponseMessage response) =>
        GetHeader(response, "X-Kibo-Response-Log");

    private static string GetHeader(HttpResponseMessage response, string headerName)
    {
        return response.Content.Headers.TryGetValues(headerName, out var values)
            ? values.FirstOrDefault() ?? ""
            : "";
    }
}