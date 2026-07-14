namespace TastyEat.Workstation.Models.Tables;

public sealed class ApplicationSetting
{
    public int Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}
