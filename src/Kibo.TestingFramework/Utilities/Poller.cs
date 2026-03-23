using System.Diagnostics;
using System.Text.Json;

namespace Kibo.TestingFramework.Utilities;

public static class Poller
{
    /// <summary>
    /// Polls an async operation until condition is met or timeout occurs.
    /// Replaces Thread.Sleep() -- returns as soon as condition is true.
    /// </summary>
    public static async Task<T> WaitUntilAsync<T>(
        Func<Task<T>> operation,
        Func<T, bool> condition,
        TimeSpan? interval = null,
        TimeSpan? timeout = null)
    {
        interval ??= TimeSpan.FromMilliseconds(500);
        timeout ??= TimeSpan.FromSeconds(15);

        var stopwatch = Stopwatch.StartNew();
        T? lastResult = default;

        while (stopwatch.Elapsed < timeout)
        {
            lastResult = await operation();
            
            if (condition(lastResult))
                return lastResult;

            await Task.Delay(interval.Value);
        }

        var lastStateJson = JsonSerializer.Serialize(lastResult);
        throw new TimeoutException($"Polling failed after {timeout}. Last state: {lastStateJson}");
    }
}