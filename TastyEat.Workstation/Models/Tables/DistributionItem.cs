namespace TastyEat.Workstation.Models.Tables;

public sealed class DistributionItem
{
    public int Id { get; set; }
    public Distribution Distribution { get; set; } = null!;
    public Client Client { get; set; } = null!;
    public Product Product { get; set; } = null!;
    public int Quantity { get; set; }
    public int? PriceAtDistribution { get; set; }
}
