namespace TastyEat.Workstation.Models.Dto;

public sealed record ProductEditDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int ProductTypeId { get; set; }
    public int Price { get; set; }
}
