using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Markup.Declarative;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TastyEat.Workstation.Models.Dto;
using TastyEat.Workstation.Models.Tables;
using TastyEat.Workstation.Options;
using TastyEat.Workstation.Services.Interfaces;
using TastyEat.Workstation.Ui;
using TastyEat.Workstation.Views.Utils;

namespace TastyEat.Workstation.Components.Dialogs;

public sealed record ClientEditResult(Client Client, bool IsNew);

public sealed partial class ClientEditDialog : Window
{
    public sealed partial class State(
        ClientValidationOptions validationOptions,
        IPhoneValidationService phoneValidation,
        ObservableCollection<Client> existingClients) : ObservableObject
    {
        [ObservableProperty]
        public partial string FullName { get; set; } = string.Empty;

        [ObservableProperty]
        public partial string PhoneNumber { get; set; } = string.Empty;

        [ObservableProperty]
        public partial bool IsInTelegramChannel { get; set; }

        [ObservableProperty]
        public partial City? SelectedCity { get; set; }

        [ObservableProperty]
        public partial Client? SelectedReferrer { get; set; }

        [ObservableProperty]
        public partial string? ReferrerSearchText { get; set; }

        [ObservableProperty]
        public partial string WindowTitle { get; set; } = "Добавить клиента";

        public ObservableCollection<City> Cities { get; } = [];

        public ObservableCollection<Client> Referrers { get; } = [];

        public ObservableCollection<Client> ExistingClients => existingClients;

        public bool IsNew => Id == 0;

        public int Id { get; set; }

        public string? FullNameError =>
            string.IsNullOrWhiteSpace(FullName)
                ? "ФИО обязательно"
                : FullName.Trim().Length < validationOptions.FullNameMinLength || FullName.Trim().Length > validationOptions.FullNameMaxLength
                    ? $"ФИО должно содержать от {validationOptions.FullNameMinLength} до {validationOptions.FullNameMaxLength} символов"
                    : null;

        public string? PhoneNumberError
        {
            get
            {
                if (!phoneValidation.IsValid(PhoneNumber))
                    return "Телефон должен начинаться с +7 или 8 и содержать 11 цифр";

                var normalized = phoneValidation.Normalize(PhoneNumber);
                return ExistingClients.Any(c => phoneValidation.Normalize(c.PhoneNumber) == normalized && c.Id != Id)
                    ? "Клиент с таким номером телефона уже существует"
                    : null;
            }
        }

        public string? CityError => SelectedCity is null ? "Необходимо выбрать город" : null;

        public string? ReferrerError
        {
            get
            {
                var trimmed = ReferrerSearchText?.Trim();
                if (string.IsNullOrEmpty(trimmed))
                    return null;
                return Referrers.Any(r => r.FullName == trimmed) ? null : "Клиента с таким ФИО не существует";
            }
        }

        public bool CanSave => FullNameError is null && PhoneNumberError is null && CityError is null && ReferrerError is null;

        partial void OnFullNameChanged(string value) => RaiseValidation();

        partial void OnPhoneNumberChanged(string value) => RaiseValidation();

        partial void OnSelectedCityChanged(City? value) => RaiseValidation();

        partial void OnSelectedReferrerChanged(Client? value) => RaiseValidation();

        partial void OnReferrerSearchTextChanged(string? value) => RaiseValidation();

        private void RaiseValidation()
        {
            OnPropertyChanged(nameof(FullNameError));
            OnPropertyChanged(nameof(PhoneNumberError));
            OnPropertyChanged(nameof(CityError));
            OnPropertyChanged(nameof(ReferrerError));
            OnPropertyChanged(nameof(CanSave));
        }
    }

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IPhoneValidationService _phoneValidationService;
    private readonly State _state;

    public ClientEditResult? Result { get; private set; }

    public ClientEditDialog(
        IServiceScopeFactory scopeFactory,
        IPhoneValidationService phoneValidationService,
        IOptions<ClientValidationOptions> validationOptions)
    {
        _scopeFactory = scopeFactory;
        _phoneValidationService = phoneValidationService;
        _state = new State(validationOptions.Value, phoneValidationService, []);

        Width = 480;
        Height = 520;
        CanResize = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Title = _state.WindowTitle;
        _state.PropertyChanged += (_, eventArgs) =>
        {
            if (eventArgs.PropertyName == nameof(State.WindowTitle))
                Title = _state.WindowTitle;
        };

        var citySelector = new ComboBox { PlaceholderText = "Выберите город" }
            .ItemsSource(_state, x => x.Cities)
            .SelectedItem(_state, x => x.SelectedCity, Avalonia.Data.BindingMode.TwoWay);
        citySelector.ItemTemplate = new FuncDataTemplate<City>((city, _) => new TextBlock { Text = city?.Name ?? string.Empty });

        var referrerSelector = new AutoCompleteBox
        {
            PlaceholderText = "Начните вводить ФИО",
            MinimumPrefixLength = 1
        }
            .ItemsSource(_state, x => x.Referrers)
            .SelectedItem(_state, x => x.SelectedReferrer, Avalonia.Data.BindingMode.TwoWay)
            .Text(_state, x => x.ReferrerSearchText, Avalonia.Data.BindingMode.TwoWay);

        Content = new Grid { Margin = new Thickness(24) }.Rows("Auto, *, Auto")
            .Children(
                new TextBlock { FontSize = 20, FontWeight = Avalonia.Media.FontWeight.Bold }
                    .Text(_state, x => x.WindowTitle),
                new StackPanel { Margin = new Thickness(0, 20, 0, 0), Spacing = 16 }.Grid_Row(1)
                    .Children(
                        Field("ФИО",
                            new TextBox { PlaceholderText = "Иванов Иван Иванович" }
                                .Text(_state, x => x.FullName, Avalonia.Data.BindingMode.TwoWay),
                            UiFactory.ErrorText(_state, x => x.FullNameError)),
                        Field("Телефон",
                            new TextBox { PlaceholderText = "+7 (999) 123-45-67" }
                                .Text(_state, x => x.PhoneNumber, Avalonia.Data.BindingMode.TwoWay),
                            UiFactory.ErrorText(_state, x => x.PhoneNumberError)),
                        new CheckBox { Margin = new Thickness(0, 8, 0, 0) }
                            .Content("В группе")
                            .IsChecked(_state, x => x.IsInTelegramChannel, Avalonia.Data.BindingMode.TwoWay),
                        Field("Город", citySelector, UiFactory.ErrorText(_state, x => x.CityError)),
                        Field("Приглашён", referrerSelector, UiFactory.ErrorText(_state, x => x.ReferrerError))),
                new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12, HorizontalAlignment = HorizontalAlignment.Right }.Grid_Row(2)
                    .Children(
                        new Button { Content = "Сохранить", IsDefault = true }
                            .IsEnabled(_state, x => x.CanSave)
                            .OnClick(async _ => await SaveAsync()),
                        new Button { Content = "Отмена", IsCancel = true }
                            .OnClick(_ => Close(null))));
    }

    public void Initialize(IReadOnlyList<City> cities, IReadOnlyList<Client> referrers, IReadOnlyList<Client> existingClients, Client? client = null)
    {
        _state.Cities.Clear();
        foreach (var city in cities)
            _state.Cities.Add(city);

        _state.Referrers.Clear();
        foreach (var referrer in referrers)
            _state.Referrers.Add(referrer);

        _state.ExistingClients.Clear();
        foreach (var existing in existingClients)
            _state.ExistingClients.Add(existing);

        if (client is null)
        {
            _state.Id = 0;
            _state.WindowTitle = "Добавить клиента";
            _state.FullName = string.Empty;
            _state.PhoneNumber = string.Empty;
            _state.IsInTelegramChannel = false;
            _state.SelectedCity = cities.FirstOrDefault();
            _state.SelectedReferrer = null;
            _state.ReferrerSearchText = null;
            return;
        }

        _state.Id = client.Id;
        _state.WindowTitle = "Изменить клиента";
        _state.FullName = client.FullName;
        _state.PhoneNumber = client.PhoneNumber;
        _state.IsInTelegramChannel = client.IsInTelegramChannel;
        _state.SelectedCity = cities.FirstOrDefault(c => c.Id == client.City.Id);
        _state.SelectedReferrer = client.Referrer is null
            ? null
            : referrers.FirstOrDefault(r => r.Id == client.Referrer.Id);
        _state.ReferrerSearchText = _state.SelectedReferrer?.FullName;
    }

    private async Task SaveAsync()
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var clientService = scope.ServiceProvider.GetRequiredService<IClientService>();

            var referrerId = _state.SelectedReferrer?.Id;
            if (referrerId is null && !string.IsNullOrWhiteSpace(_state.ReferrerSearchText))
            {
                var referrer = await clientService.GetByFullNameAsync(_state.ReferrerSearchText.Trim());
                referrerId = referrer?.Id;
            }

            var dto = new ClientEditDto
            {
                Id = _state.Id,
                FullName = _state.FullName.Trim(),
                PhoneNumber = _phoneValidationService.Normalize(_state.PhoneNumber),
                CityId = _state.SelectedCity?.Id ?? throw new InvalidOperationException("Город не выбран"),
                IsInTelegramChannel = _state.IsInTelegramChannel,
                ReferrerId = referrerId
            };

            var client = _state.IsNew
                ? await clientService.CreateAsync(dto)
                : await clientService.UpdateAsync(dto);

            Result = new ClientEditResult(client, _state.IsNew);
            Close(Result);
        }
        catch (Exception exception)
        {
            await MessageDialog.ShowInfoAsync(this.GetOwnerWindow(), $"Не удалось сохранить клиента:\n{exception.Message}");
        }
    }

    private static StackPanel Field(string label, Control editor, Control? error = null)
    {
        var field = new StackPanel { Spacing = 4 };
        field.Children.Add(new TextBlock { Text = label });
        field.Children.Add(editor);
        if (error is not null)
            field.Children.Add(error);
        return field;
    }
}
