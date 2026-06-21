namespace TastyEat.Workstation.Models.Analytics;

public sealed class ProductionStatsDto
{
    public string ProductName { get; set; } = string.Empty;
    public string ProductTypeName { get; set; } = string.Empty;
    public int TotalProduced { get; set; }
    public int TotalSold { get; set; }
    public int Remaining { get; set; }
}
