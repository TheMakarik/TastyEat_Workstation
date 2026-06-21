namespace TastyEat.Workstation.Services.Interfaces;

public interface IPhoneValidationService
{
    bool IsValid(string? phoneNumber);
    string Normalize(string phoneNumber);
}
