namespace TastyEat.Workstation.Models.Dto;

public sealed record DistributionClientEditDto
{
    public int Id { get; set; }
    public int DistributionId { get; set; }
    public int ClientId { get; set; }
    public int TotalAmount { get; set; }
    public List<DistributionItemEditDto> Items { get; set; } = [];
}

public sealed record DistributionItemEditDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
}
