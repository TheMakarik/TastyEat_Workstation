namespace TastyEat.Workstation.Models.Analytics;

public sealed class SalesByClientDto
{
    public string ClientName { get; set; } = string.Empty;
    public string CityName { get; set; } = string.Empty;
    public int TotalQuantity { get; set; }
    public int TotalRevenue { get; set; }
}
