namespace TastyEat.Workstation.Models.Tables;

public sealed class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public ProductType ProductType { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public ICollection<ProductPrice> Prices { get; set; } = [];
    public ICollection<ProductionBatchItem> ProductionBatchItems { get; set; } = [];
    public ICollection<DistributionItem> DistributionItems { get; set; } = [];
}
