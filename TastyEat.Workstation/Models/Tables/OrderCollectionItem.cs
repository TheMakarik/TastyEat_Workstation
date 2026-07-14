namespace TastyEat.Workstation.Models.Tables;

public sealed class OrderCollectionItem
{
    public int Id { get; set; }

    public OrderCollectionClient OrderCollectionClient { get; set; } = null!;
    public Product Product { get; set; } = null!;

    public int Quantity { get; set; }
}
