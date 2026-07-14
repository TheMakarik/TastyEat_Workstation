namespace TastyEat.Workstation.Models.Tables;

public sealed class OrderCollectionClient
{
    public int Id { get; set; }

    public OrderCollection OrderCollection { get; set; } = null!;
    public Client Client { get; set; } = null!;

    public ICollection<OrderCollectionItem> Items { get; set; } = [];
}
