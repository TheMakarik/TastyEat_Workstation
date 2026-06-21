namespace TastyEat.Workstation.Models.Tables;

public sealed class ProductionBatch
{
    public int Id { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public ICollection<ProductionBatchItem> Items { get; set; } = [];
}
