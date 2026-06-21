namespace TastyEat.Workstation.Models.Tables;

public sealed class ProductPrice
{
    public int Id { get; set; }
    public Product Product { get; set; } = null!;
    public int Price { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
}
