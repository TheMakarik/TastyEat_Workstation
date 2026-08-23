using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Markup.Declarative;
using CommunityToolkit.Mvvm.ComponentModel;
using Material.Icons;
using Material.Icons.Avalonia;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TastyEat.Workstation.Models.Tables;
using TastyEat.Workstation.Options;
using TastyEat.Workstation.Services.Interfaces;
using TastyEat.Workstation.Ui;
using TastyEat.Workstation.Views.Utils;

namespace TastyEat.Workstation.Components.Dialogs;

public sealed partial class CityManagementDialog : Window
{
    public sealed partial class State : ObservableObject
    {
        public ObservableCollection<City> Cities { get; } = [];
    }

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly State _state = new();

    public CityManagementDialog(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;

        Width = 440;
        Height = 520;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Title = "Управление городами";

        Content = new Grid { Margin = new Thickness(24) }.Rows("Auto, *")
            .Children(
                new TextBlock { Text = "Города", FontSize = 20, FontWeight = Avalonia.Media.FontWeight.Bold },
                new ScrollViewer().Grid_Row(1).Margin(new Thickness(0, 16, 0, 0))
                    .Content(BuildList()));
    }

    private Control BuildList() =>
        new ItemsControl
        {
            ItemsSource = _state.Cities,
            ItemTemplate = new FuncDataTemplate<City>((city, _) => city is null ? null : BuildCityRow(city))
        };

    private Control BuildCityRow(City city) =>
        new Border
        {
            BorderBrush = Avalonia.Media.Brush.Parse("#E0E0E0"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(12, 6),
            Margin = new Thickness(0, 0, 0, 8),
            Child = new Grid().Cols("*, Auto")
                .Children(
                    new TextBlock { Text = city.Name, VerticalAlignment = VerticalAlignment.Center, FontSize = 15 },
                    new Button
                        {
                            Classes = { "action" },
                            Content = new MaterialIcon { Kind = MaterialIconKind.DeleteOutline }
                        }
                        .Grid_Column(1)
                        .OnClick(eventArgs => _ = DeleteCityAsync(eventArgs.Source as Button, city)))
        };

    public async void Initialize()
    {
        await ReloadAsync();
    }

    private async Task ReloadAsync()
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var cityService = scope.ServiceProvider.GetRequiredService<ICityService>();
        var cities = await cityService.GetAllAsync();

        _state.Cities.Clear();
        foreach (var city in cities)
            _state.Cities.Add(city);
    }

    private async Task AddCityAsync()
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var editDialog = scope.ServiceProvider.GetRequiredService<CityEditDialog>();
        var accepted = await editDialog.ShowDialog<bool?>(this);
        if (accepted == true)
            await ReloadAsync();
    }

    private async Task DeleteCityAsync(Button? anchor, City city)
    {
        var confirmed = await DeleteConfirmationDialog.ShowAsync(this, $"Удалить город \"{city.Name}\"?");
        if (!confirmed)
            return;

        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var cityService = scope.ServiceProvider.GetRequiredService<ICityService>();
            await cityService.DeleteAsync(city.Id);
            await ReloadAsync();
        }
        catch (Exception exception)
        {
            await MessageDialog.ShowInfoAsync(this, exception.Message);
        }
    }
}
