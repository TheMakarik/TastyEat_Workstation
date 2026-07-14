namespace TastyEat.Workstation.Models.Tables;

public sealed class OrderCollection
{
    public int Id { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    public ICollection<OrderCollectionClient> Clients { get; set; } = [];
}
