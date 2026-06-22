namespace TastyEat.Workstation.Models.Analytics;

public sealed class ProductionStatsDto
{
    public string ProductName { get; set; } = string.Empty;
    public string ProductTypeName { get; set; } = string.Empty;
    public double TotalProduced { get; set; }
    public double TotalSold { get; set; }
    public double Remaining { get; set; }
}
