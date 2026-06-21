using TastyEat.Workstation.Services.Interfaces;

namespace TastyEat.Workstation.Services;

public sealed class PhoneValidationService : IPhoneValidationService
{
    public bool IsValid(string? phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return false;

        var digits = ExtractDigits(phoneNumber);
        if (digits.Length != 11)
            return false;

        return digits[0] == '7' || digits[0] == '8';
    }

    public string Normalize(string phoneNumber)
    {
        var digits = ExtractDigits(phoneNumber);
        if (digits.Length == 11 && digits[0] == '7')
            return '8' + digits[1..];

        return digits;
    }

    private static string ExtractDigits(string phoneNumber) =>
        new(phoneNumber.Where(char.IsDigit).ToArray());
}
