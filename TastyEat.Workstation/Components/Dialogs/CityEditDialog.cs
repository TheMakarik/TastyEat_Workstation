using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Markup.Declarative;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TastyEat.Workstation.Options;
using TastyEat.Workstation.Services.Interfaces;
using TastyEat.Workstation.Ui;
using TastyEat.Workstation.Views.Utils;

namespace TastyEat.Workstation.Components.Dialogs;

public sealed partial class CityEditDialog : Window
{
    public sealed partial class State(StringLengthOptions stringLengthOptions) : ObservableObject
    {
        [ObservableProperty]
        public partial string Name { get; set; } = string.Empty;

        [ObservableProperty]
        public partial string? ServerError { get; set; }

        public string? NameError =>
            string.IsNullOrWhiteSpace(Name)
                ? "Название города обязательно"
                : Name.Trim().Length > stringLengthOptions.CityNameMaxLength
                    ? $"Название города не должно превышать {stringLengthOptions.CityNameMaxLength} символов"
                    : ServerError;

        public bool CanSave => NameError is null;

        partial void OnNameChanged(string value)
        {
            ServerError = null;
            RaiseValidation();
        }

        partial void OnServerErrorChanged(string? value) => RaiseValidation();

        private void RaiseValidation()
        {
            OnPropertyChanged(nameof(NameError));
            OnPropertyChanged(nameof(CanSave));
        }
    }

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly State _state;

    public CityEditDialog(IServiceScopeFactory scopeFactory, IOptions<StringLengthOptions> stringLengthOptions)
    {
        _scopeFactory = scopeFactory;
        _state = new State(stringLengthOptions.Value);

        Width = 400;
        Height = 220;
        CanResize = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Title = "Добавить город";

        Content = new Grid { Margin = new Thickness(24) }.Rows("Auto, *, Auto")
            .Children(
                new TextBlock { Text = "Добавить город", FontSize = 20, FontWeight = Avalonia.Media.FontWeight.Bold },
                new StackPanel { Margin = new Thickness(0, 20, 0, 0), Spacing = 4 }.Grid_Row(1)
                    .Children(
                        new TextBlock { Text = "Название города" },
                        new TextBox { PlaceholderText = "Введите название города" }
                            .Text(_state, x => x.Name, Avalonia.Data.BindingMode.TwoWay),
                        UiFactory.ErrorText(_state, x => x.NameError)),
                new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12, HorizontalAlignment = HorizontalAlignment.Right }.Grid_Row(2)
                    .Children(
                        new Button { Content = "Добавить", IsDefault = true }
                            .IsEnabled(_state, x => x.CanSave)
                            .OnClick(async _ => await SaveAsync()),
                        new Button { Content = "Отмена", IsCancel = true }
                            .OnClick(_ => Close(null))));
    }

    private async Task SaveAsync()
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var cityService = scope.ServiceProvider.GetRequiredService<ICityService>();
            var name = _state.Name.Trim();

            if (await cityService.ExistsByNameAsync(name))
            {
                _state.ServerError = "Город с таким названием уже существует";
                return;
            }

            var city = await cityService.CreateAsync(name);
            Close(city);
        }
        catch (Exception exception)
        {
            await MessageDialog.ShowInfoAsync(this.GetOwnerWindow(), $"Не удалось добавить город:\n{exception.Message}");
        }
    }
}
