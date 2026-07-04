using System.Reactive;
using System.Reactive.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using ReactiveUI.Validation.Extensions;
using TastyEat.Workstation.Models.Dto;
using TastyEat.Workstation.Models.Tables;
using TastyEat.Workstation.Options;
using TastyEat.Workstation.Services.Interfaces;

namespace TastyEat.Workstation.ViewModels;

public sealed partial class ClientEditViewModel : ValidatableViewModelBase
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IPhoneValidationService _phoneValidationService;
    private readonly ClientValidationOptions _validationOptions;
    private readonly IObservable<bool> _canExecute;

    [Reactive]
    private int _id;

    [Reactive]
    private string _fullName = string.Empty;

    [Reactive]
    private string _phoneNumber = string.Empty;

    [Reactive]
    private bool _isInTelegramChannel;

    [Reactive]
    private City? _selectedCity;

    [Reactive]
    private Client? _selectedReferrer;

    [Reactive]
    private string? _referrerSearchText;

    [Reactive]
    private IReadOnlyList<City> _cities = [];

    [Reactive]
    private IReadOnlyList<Client> _referrers = [];

    [Reactive]
    private string _title = "Добавить клиента";

    private IReadOnlyList<Client> _existingClients = [];

    public ClientEditViewModel(
        IServiceScopeFactory scopeFactory,
        IPhoneValidationService phoneValidationService,
        IOptions<ClientValidationOptions> validationOptions)
    {
        _scopeFactory = scopeFactory;
        _phoneValidationService = phoneValidationService;
        _validationOptions = validationOptions.Value;

        this.ValidationRule(
            vm => vm.FullName,
            name => !string.IsNullOrWhiteSpace(name)
                    && name.Length >= _validationOptions.FullNameMinLength
                    && name.Length <= _validationOptions.FullNameMaxLength,
            $"ФИО должно содержать от {_validationOptions.FullNameMinLength} до {_validationOptions.FullNameMaxLength} символов");

        this.ValidationRule(
            vm => vm.PhoneNumber,
            phone => _phoneValidationService.IsValid(phone),
            "Телефон должен начинаться с +7 или 8 и содержать 11 цифр");

        this.ValidationRule(
            vm => vm.SelectedCity,
            city => city is not null,
            "Необходимо выбрать город");

        this.ValidationRule(
            vm => vm.PhoneNumber,
            phone =>
            {
                if (!_phoneValidationService.IsValid(phone))
                    return true;

                var normalized = _phoneValidationService.Normalize(phone!);
                return !_existingClients.Any(c =>
                    _phoneValidationService.Normalize(c.PhoneNumber) == normalized && c.Id != Id);
            },
            "Клиент с таким номером телефона уже существует");

        this.ValidationRule(
            vm => vm.ReferrerSearchText,
            text =>
            {
                var trimmed = text?.Trim() ?? string.Empty;
                if (string.IsNullOrEmpty(trimmed))
                    return true;

                return Referrers.Any(r => r.FullName == trimmed);
            },
            "Приглашённый клиент с таким ФИО не существует");

        _canExecute = this.IsValid().ObserveOn(RxApp.MainThreadScheduler);
    }

    public bool IsNew => Id == 0;

    public void Initialize(IReadOnlyList<City> cities, IReadOnlyList<Client> referrers, IReadOnlyList<Client> existingClients, Client? client = null)
    {
        Cities = cities;
        Referrers = referrers;
        _existingClients = existingClients;

        if (client is null)
        {
            Id = 0;
            Title = "Добавить клиента";
            FullName = string.Empty;
            PhoneNumber = string.Empty;
            IsInTelegramChannel = false;
            SelectedCity = cities.FirstOrDefault();
            SelectedReferrer = null;
            ReferrerSearchText = null;
            return;
        }

        Id = client.Id;
        Title = "Изменить клиента";
        FullName = client.FullName;
        PhoneNumber = client.PhoneNumber;
        IsInTelegramChannel = client.IsInTelegramChannel;
        SelectedCity = cities.FirstOrDefault(c => c.Id == client.City.Id);
        SelectedReferrer = client.Referrer is null
            ? null
            : referrers.FirstOrDefault(r => r.Id == client.Referrer.Id);
        ReferrerSearchText = SelectedReferrer?.FullName;
    }

    [ReactiveCommand(CanExecute = nameof(_canExecute), OutputScheduler = "ReactiveUI.RxApp.MainThreadScheduler")]
    private async Task<ClientEditResult?> SaveAsync()
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var clientService = scope.ServiceProvider.GetRequiredService<IClientService>();

        var referrerId = SelectedReferrer?.Id;
        if (referrerId is null && !string.IsNullOrWhiteSpace(ReferrerSearchText))
        {
            var referrer = await clientService.GetByFullNameAsync(ReferrerSearchText.Trim());
            referrerId = referrer?.Id;
        }

        var dto = new ClientEditDto
        {
            Id = Id,
            FullName = FullName.Trim(),
            PhoneNumber = _phoneValidationService.Normalize(PhoneNumber),
            CityId = SelectedCity?.Id ?? throw new InvalidOperationException("Город не выбран"),
            IsInTelegramChannel = IsInTelegramChannel,
            ReferrerId = referrerId
        };

        var client = IsNew
            ? await clientService.CreateAsync(dto)
            : await clientService.UpdateAsync(dto);

        return new ClientEditResult(client, IsNew);
    }
}

public sealed record ClientEditResult(Client Client, bool IsNew);
