namespace TastyEat.Workstation.Options;

public sealed class ClientValidationOptions
{
    public int FullNameMinLength { get; set; } = 2;
    public int FullNameMaxLength { get; set; } = 200;
    public int PhoneNumberMinLength { get; set; } = 10;
    public int PhoneNumberMaxLength { get; set; } = 20;
}
