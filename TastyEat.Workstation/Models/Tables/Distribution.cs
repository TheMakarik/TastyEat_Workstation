namespace TastyEat.Workstation.Models.Tables;

public sealed class Distribution
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public ICollection<DistributionItem> Items { get; set; } = [];
}
