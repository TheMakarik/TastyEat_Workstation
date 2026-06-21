namespace TastyEat.Workstation.Models.Tables;

public sealed class ProductionBatchItem
{
    public int Id { get; set; }
    public ProductionBatch ProductionBatch { get; set; } = null!;
    public Product Product { get; set; } = null!;
    public int Quantity { get; set; }
}
