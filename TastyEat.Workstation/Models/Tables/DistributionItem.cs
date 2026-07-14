namespace TastyEat.Workstation.Models.Tables;

public sealed class DistributionItem
{
    public int Id { get; set; }

    public DistributionClient DistributionClient { get; set; } = null!;
    public Product Product { get; set; } = null!;

    public int Quantity { get; set; }
}
