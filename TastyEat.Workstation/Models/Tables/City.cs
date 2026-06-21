namespace TastyEat.Workstation.Models.Tables;

public sealed class City
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public ICollection<Client> Clients { get; set; } = [];
}
