namespace TastyEat.Workstation.Models.Dto;

public sealed record ProductTypeEditDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
