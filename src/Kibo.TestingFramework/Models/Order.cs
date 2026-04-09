namespace Kibo.TestingFramework.Models;

public class Order
{
    public Guid Id { get; set; } // Make Guid for better test data uniqueness
    public string TenantId { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public List<LineItem> LineItems { get; set; } = [];
    public string Status { get; set; } = string.Empty;
}

public class LineItem
{
    public string ProductCode { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}