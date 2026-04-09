using System.Diagnostics;
using System.Text;

namespace Kibo.TestingFramework.Observability;

/// <summary>
/// HttpClient DelegatingHandler for observability requirements.
/// Captures timing, correlation ID, request/response logs for CI/CD diagnostics.
/// Logging toggleable via constructor parameter.
/// </summary>
public class ObservabilityHandler(bool enableLogging = false) : DelegatingHandler
{
    private readonly bool _enableLogging = enableLogging;

    public new HttpMessageHandler InnerHandler 
    { 
        get => base.InnerHandler!; 
        set => base.InnerHandler = value; 
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // 1. CORRELATION ID 
        var correlationId = Guid.NewGuid().ToString("N")[..8];
        request.Headers.TryAddWithoutValidation("x-correlation-id", correlationId);

        // 2. TIMING 
        var stopwatch = Stopwatch.StartNew();

        // 3. CAPTURE REQUEST
        var requestLog = await FormatRequestAsync(request, cancellationToken);

        // 4. EXECUTE
        var response = await base.SendAsync(request, cancellationToken);
        stopwatch.Stop();
        var elapsedMs = stopwatch.ElapsedMilliseconds;

        // 5. CAPTURE RESPONSE
        var responseLog = await FormatResponseAsync(response, cancellationToken);

        // 6. ATTACH DIAGNOSTICS (HttpResponseMessage.Content.Headers)
        response.Content.Headers.Add("X-Kibo-Correlation-Id", correlationId);
        response.Content.Headers.Add("X-Kibo-Elapsed-Ms", elapsedMs.ToString());
        response.Content.Headers.Add("X-Kibo-Request-Log", requestLog);
        response.Content.Headers.Add("X-Kibo-Response-Log", responseLog);

        // 7. CONSOLE LOGGING (toggleable)
        if (_enableLogging)
        {
            Console.WriteLine($"[{correlationId}] {requestLog}");
            Console.WriteLine($"[{correlationId}] {responseLog} ({elapsedMs}ms)");
        }

        return response;
    }

    private static async Task<string> FormatRequestAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var sb = new StringBuilder();
        sb.Append($"{request.Method} {request.RequestUri?.PathAndQuery}");

        if (request.Content != null)
        {
            var body = await request.Content.ReadAsStringAsync(ct);
            sb.Append($" | Body: {Truncate(body, 100)}");
        }

        return sb.ToString();
    }

    private static async Task<string> FormatResponseAsync(HttpResponseMessage response, CancellationToken ct)
    {
        var sb = new StringBuilder();
        sb.Append($"{(int)response.StatusCode} {response.StatusCode}");

        if (response.Content != null)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            sb.Append($" | Body: {Truncate(body, 100)}");
        }

        return sb.ToString();
    }

    private static string Truncate(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value)) return "";
        return value.Length <= maxLength ? value : value[..maxLength] + "...";
    }
}