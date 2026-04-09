using Kibo.TestingFramework.Clients;

namespace Kibo.LegacyTests;

public class KiboTestFixture : IDisposable
{
    public string BaseUrl { get; } = "http://localhost:5000";
    public KiboApiClient Client { get; private set; }
    public KiboApiClient TenantlessClient { get; private set; }

    // Environment variable toggle for logging
    private static bool EnableLogging => Environment.GetEnvironmentVariable("KIBO_TEST_LOGGING") == "true";

    public KiboTestFixture()
    {
        // Default clients (logging OFF for clean test output)
        Client = new KiboApiClient(BaseUrl, "t1", EnableLogging);
        TenantlessClient = new KiboApiClient(BaseUrl, "", EnableLogging);
    }

    /// <summary>
    /// Factory method with logging toggle
    /// </summary>
    public KiboApiClient CreateClientWithTenant(string tenantId, bool enableLogging = false)
    {
        return new KiboApiClient(BaseUrl, tenantId, enableLogging);
    }

    public void Dispose()
    {
        Client?.Dispose();
        TenantlessClient?.Dispose();
        GC.SuppressFinalize(this);
    }
}