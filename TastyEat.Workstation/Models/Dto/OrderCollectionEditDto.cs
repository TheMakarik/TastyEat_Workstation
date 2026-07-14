namespace TastyEat.Workstation.Models.Dto;

public sealed record OrderCollectionEditDto
{
    public int Id { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

public sealed record OrderCollectionClientEditDto
{
    public int Id { get; set; }
    public int ClientId { get; set; }
    public List<OrderCollectionItemEditDto> Items { get; set; } = [];
}

public sealed record OrderCollectionItemEditDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
}
