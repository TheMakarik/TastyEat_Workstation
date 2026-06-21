namespace TastyEat.Workstation.Models.Tables;

public sealed class Client
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public City City { get; set; } = null!;
    public bool IsInTelegramChannel { get; set; }
    public Client? Referrer { get; set; }
    public ICollection<Client> Referrals { get; set; } = [];
    public DateTime CreatedAt { get; set; }
    public ICollection<DistributionItem> DistributionItems { get; set; } = [];
}
