namespace TastyEat.Workstation.Models.Dto;

public sealed class ClientEditDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public int CityId { get; set; }
    public bool IsInTelegramChannel { get; set; }
    public int? ReferrerId { get; set; }
}
