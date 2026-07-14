namespace TastyEat.Workstation.Models.Tables;

public sealed class DistributionClient
{
    public int Id { get; set; }

    public Distribution Distribution { get; set; } = null!;
    public Client Client { get; set; } = null!;

    public int TotalAmount { get; set; }

    public ICollection<DistributionItem> Items { get; set; } = [];
}
