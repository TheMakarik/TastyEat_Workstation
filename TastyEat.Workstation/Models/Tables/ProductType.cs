namespace TastyEat.Workstation.Models.Tables;

public sealed class ProductType
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public ICollection<Product> Products { get; set; } = [];
}
