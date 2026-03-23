using Kibo.TestingFramework.Models;

namespace Kibo.TestingFramework.Builders;

public class OrderBuilder
{
    private readonly Order _order = new()
    {
        CustomerEmail = "test@kibo.com",
        LineItems = new List<LineItem>()
    };

    private readonly Random _random = new();

    /// <summary>
    /// Entry point for fluent order building with sensible defaults applied
    /// </summary>
    public static OrderBuilder Default => new();

    /// <summary>
    /// Sets custom customer email
    /// </summary>
    public OrderBuilder WithCustomerEmail(string email)
    {
        _order.CustomerEmail = email;
        return this;
    }

    /// <summary>
    /// Generates N random line items
    /// </summary>
    public OrderBuilder WithItems(int count)
    {
        _order.LineItems.Clear();
        for (int i = 0; i < count; i++)
        {
            _order.LineItems.Add(new LineItem
            {
                ProductCode = $"SKU-{_random.Next(100, 999)}",
                Quantity = _random.Next(1, 5),
                UnitPrice = (decimal)(_random.NextDouble() * 100)
            });
        }
        return this;
    }

    /// <summary>
    /// Sets completely custom line items (builder-idiomatic alternative to post-Build mutation)
    /// Usage: .WithLineItems(new[] { new LineItem { UnitPrice = -19.99m } })
    /// </summary>
    public OrderBuilder WithLineItems(IEnumerable<LineItem> lineItems)
    {
        _order.LineItems.Clear();
        _order.LineItems.AddRange(lineItems);
        return this;
    }

    /// <summary>
    /// Sets scenario-specific test emails (self-documenting test data)
    /// Usage: .WithScenarioEmail("sql-injection") → "sql-injection@example.com"
    /// </summary>
    public OrderBuilder WithScenarioEmail(string scenario)
    {
        _order.CustomerEmail = scenario switch
        {
            "happy-path" => "john.doe@example.com",
            "no-tenant" => "no-tenant@example.com",
            "status-check" => "status-check@example.com",
            "empty-cart" => "empty-cart@example.com",
            "negative-price" => "negative-price@example.com",
            "sql-injection" => "sql-injection@example.com",
            "unicode" => "café👨‍💻@räucher.de",
            "perf" => "perf@test.com",
            _ => $"test-{Guid.NewGuid():N[..8]}@example.com"
        };
        return this;
    }

    /// <summary>
    /// Produces final immutable Order DTO for API calls
    /// </summary>
    public Order Build() => _order;
}