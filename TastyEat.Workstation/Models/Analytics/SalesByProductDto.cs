namespace TastyEat.Workstation.Models.Analytics;

public sealed class SalesByProductDto
{
    public string ProductName { get; set; } = string.Empty;
    public string ProductTypeName { get; set; } = string.Empty;
    public int TotalQuantity { get; set; }
    public int TotalRevenue { get; set; }
}
