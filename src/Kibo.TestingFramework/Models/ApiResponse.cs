using System.Net;

namespace Kibo.TestingFramework.Models;


public record ApiResponse<T>(
    HttpResponseMessage RawResponse,
    T? Data,
    long ElapsedMilliseconds,
    string CorrelationId,
    string RequestLog,
    string ResponseLog)
{
    public bool IsSuccess => RawResponse.IsSuccessStatusCode;
    public HttpStatusCode StatusCode => RawResponse.StatusCode;
   
    // Backwards compatibility for existing tests
    public T? Order => Data;
    public string? Id => (Data as dynamic)?.Id?.ToString();
    public string? Status => (Data as dynamic)?.Status?.ToString();
   
    // Raw response access
    public HttpResponseMessage Response => RawResponse;
   
    public override string ToString() =>
        $"Status: {StatusCode} | Corr: {CorrelationId} | {ElapsedMilliseconds}ms";
}