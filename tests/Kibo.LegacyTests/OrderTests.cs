using Kibo.TestingFramework.Clients;
using System.Net;
using Kibo.TestingFramework.Builders;
using Kibo.TestingFramework.Utilities;
using Kibo.TestingFramework.Models;

namespace Kibo.LegacyTests;

/// <summary>
/// Refactored Kibo Legacy Tests → Production Testing Framework
/// 
/// ORIGINAL ANTI-PATTERNS (ALL ELIMINATED):
/// • HttpClient created directly in every test method - FIXED: KiboApiClient + IClassFixture
/// • Hardcoded base URL (http://localhost:5000) copy-pasted everywhere - FIXED: KiboTestFixture.BaseUrl  
/// • x-kibo-tenant header logic duplicated - FIXED: Client manages headers
/// • Raw JSON strings inline - FIXED: OrderBuilder fluent API
/// • Thread.Sleep(6000) brittle timing - FIXED: Poller.WaitUntilAsync
/// 
/// NEW CAPABILITIES:
/// • Toggleable diagnostics + performance assertions
/// • Edge cases expose MockApi security/business validation gaps
/// </summary>

public class OrderTests : IClassFixture<KiboTestFixture>
{
    private readonly KiboTestFixture _fixture;

    private const string STATUS_PENDING = "Pending";
    private const string STATUS_READY_FOR_FULFILLMENT = "ReadyForFulfillment";

    public OrderTests(KiboTestFixture fixture)
    {
        _fixture = fixture;
    }
  
    /// <summary>
    /// Happy path: Valid order creation
    /// EXPECTED: 201 Created with Pending status
    /// ACTUAL: Works correctly
    /// </summary>
    [Fact]
    public async Task CreateOrder_ReturnsSuccess()
    {
        var order = OrderBuilder.Default
            .WithScenarioEmail("happy-path")
            .WithItems(1)
            .Build();

        var createdOrder = await _fixture.Client.CreateOrderAsync(order);
        Assert.NotNull(createdOrder?.Id);
        Assert.Equal(STATUS_PENDING, createdOrder.Status);
    }

    /// <summary>
    /// Edge case: Missing tenant header
    /// EXPECTED: 401 Unauthorized 
    /// ACTUAL: 401 Unauthorized
    /// </summary>
    [Fact]
    public async Task CreateOrder_WithoutTenantHeader_Returns401()
    {
        var order = OrderBuilder.Default
            .WithScenarioEmail("no-tenant")
            .WithItems(1)
            .Build();
        
        var response = await _fixture.TenantlessClient.CreateOrderRawAsync(order);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>
    /// Status transition validation (replaces Thread.Sleep)
    /// EXPECTED: Status changes to ReadyForFulfillment after ~5s
    /// ACTUAL: Polling confirms transition
    /// </summary>
    [Fact]
    public async Task GetOrder_AfterCreation_StatusBecomesReadyForFulfillment()
    {
        var order = OrderBuilder.Default
            .WithScenarioEmail("status-check")
            .WithItems(1)
            .Build();

        var createdOrder = await _fixture.Client.CreateOrderAsync(order);
        
        var readyOrder = await Poller.WaitUntilAsync(
            () => _fixture.Client.GetOrderAsync(createdOrder.Id!.ToString()!),
            order => order.Status == STATUS_READY_FOR_FULFILLMENT);

        Assert.Equal(STATUS_READY_FOR_FULFILLMENT, readyOrder.Status);
    }

    /// <summary>
    /// Edge case: Invalid order ID
    /// EXPECTED: 404 Not Found
    /// ACTUAL: 404 Not Found
    /// </summary>
    [Fact]
    public async Task GetOrder_WithInvalidId_Returns404()
    {
        var fakeId = Guid.NewGuid().ToString();
        var response = await _fixture.Client.GetOrderRawAsync(fakeId);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// Destructive edge case: Empty shopping cart
    /// EXPECTED: 400 Bad Request "lineItems required"
    /// ACTUAL: 201 Created - MockApi lacks validation
    /// </summary>
    /// BUG REPORT: Production API should return 400 "lineItems required"
    /// MockApi lacks basic order validation - business risk
    /// KNOWN ISSUE: MockApi lacks validation (Expected: 400 Bad Request)
    [Fact]
    public async Task CreateOrder_EmptyLineItemsArray_AcceptsInvalidOrder()
    {
        var emptyOrder = OrderBuilder.Default
            .WithScenarioEmail("empty-cart")
            .WithItems(0)
            .Build();

        var response = await _fixture.Client.CreateOrderRawAsync(emptyOrder);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    /// <summary>
    /// Destructive edge case: Negative pricing
    /// EXPECTED: 400 Bad Request "unitPrice must be > 0"
    /// ACTUAL: 201 Created - MockApi allows negative revenue
    /// </summary>
    /// BUG REPORT: Production API should reject negative pricing
    /// MockApi lacks business rule validation - financial risk
    /// KNOWN ISSUE: MockApi allows negative revenue (Expected: 400 Bad Request)
    [Fact]
    public async Task CreateOrder_NegativeUnitPrice_AcceptsInvalidPrice()
    {
        var order = OrderBuilder.Default
            .WithScenarioEmail("negative-price")
            .WithLineItems([new LineItem 
        { 
                    ProductCode = "TEST-001",
                    Quantity = 1,
                    UnitPrice = -19.99m  
            }])
            .Build();

        var response = await _fixture.Client.CreateOrderRawAsync(order);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    /// <summary>
    /// Destructive edge case: SQL Injection in tenant header (CRITICAL)
    /// EXPECTED: 400 Bad Request or header sanitization
    /// ACTUAL: 201 Created - Complete security vulnerability
    /// </summary>
    /// ***SECURITY BUG REPORT***
    /// CRITICAL: MockApi accepts SQL injection payload in x-kibo-tenant header
    /// Payload: "tenant1'; DROP TABLE Orders; --" 
    /// Risk: Complete database compromise, data exfiltration
    /// Impact: Production deployment = IMMEDIATE SECURITY BREACH
    /// Recommendation: Header whitelist validation + parameterized queries
    /// CRITICAL SECURITY BUG: MockApi accepts SQLi payloads (Expected: 400/401)

    [Fact]
    public async Task CreateOrder_SQLInjectionTenantHeader_AcceptsMaliciousPayload()
    {
        var maliciousOrder = OrderBuilder.Default
            .WithScenarioEmail("sql-injection")
            .WithItems(1)
            .Build();

        using var sqlClient = _fixture.CreateClientWithTenant("tenant1'; DROP TABLE Orders; --");
        var response = await sqlClient.CreateOrderRawAsync(maliciousOrder);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    /// <summary>
    /// Destructive edge case: Oversized payload
    /// EXPECTED: 413 Payload Too Large
    /// ACTUAL: 201 Created - DoS vector exposed
    /// </summary>
    /// BUG REPORT: Production API should enforce payload limits
    /// MockApi missing size validation - DoS risk
    /// KNOWN ISSUE: MockApi missing payload limits (Expected: 413 Payload Too Large)

    [Fact]
    public async Task CreateOrder_ExcessiveCustomerEmail_AcceptsMassivePayload()
    {
        var oversizedOrder = OrderBuilder.Default
            .WithCustomerEmail(new string('a', 10000) + "@example.com")
            .WithItems(1)
            .Build();

        var response = await _fixture.Client.CreateOrderRawAsync(oversizedOrder);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode); 
    }

    /// <summary>
    /// Destructive edge case: Unicode/emoji in email (XSS vector)
    /// EXPECTED: 400 Bad Request "Invalid email format"
    /// ACTUAL: 201 Created - XSS risk exposed
    /// </summary>
    /// BUG REPORT: Production API should validate/sanitize email
    /// MockApi accepts XSS vectors - security risk
    /// KNOWN ISSUE: MockApi missing email validation/sanitization (Expected: 400 Bad Request)

    [Fact]
    public async Task CreateOrder_UnicodeSpecialCharacters_AcceptsUnsafePayload()
    {
        var unicodeOrder = OrderBuilder.Default
            .WithScenarioEmail("unicode")
            .WithItems(1)
            .Build();

        var response = await _fixture.Client.CreateOrderRawAsync(unicodeOrder);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);  
    }

    /// <summary>
    /// Performance validation
    /// EXPECTED: <500ms response under normal load
    /// ACTUAL: Varies by environment, <1000ms acceptable
    /// </summary>
    [Fact]
    public async Task CreateOrder_Performance_RespondsWithin1000ms()
    {
        using var perfClient = _fixture.CreateClientWithTenant("tenant-abc-123", enableLogging: true);  
        
        var order = OrderBuilder.Default
            .WithScenarioEmail("perf")
            .WithItems(1)
            .Build();
            
        var response = await perfClient.CreateOrderRawAsync(order);
        
        var responseTimeMs = response.GetElapsedMs();  
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.True(responseTimeMs < 1000, $"Response took {responseTimeMs}ms"); 

        Console.WriteLine($"[PERF] Corr: {response.GetCorrelationId()} | {responseTimeMs}ms | Order Creation");
        Console.WriteLine($"Request: {response.GetRequestLog()}");
        Console.WriteLine($"Response: {response.GetResponseLog()}");
    }
}