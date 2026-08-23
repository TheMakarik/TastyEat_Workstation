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
using TastyEat.Workstation.Models.Tables;
using TastyEat.Workstation.Services.Interfaces;
using TastyEat.Workstation.Ui;
using TastyEat.Workstation.Views.Utils;

namespace TastyEat.Workstation.Components.Dialogs;

public sealed partial class ProductTypeManagementDialog : Window
{
    public sealed partial class State : ObservableObject
    {
        public ObservableCollection<ProductType> Types { get; } = [];
    }

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly State _state = new();

    public ProductTypeManagementDialog(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;

        Width = 440;
        Height = 520;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Title = "Управление типами продукции";

        Content = new Grid { Margin = new Thickness(24) }.Rows("Auto, Auto, *")
            .Children(
                new TextBlock { Text = "Типы продукции", FontSize = 20, FontWeight = Avalonia.Media.FontWeight.Bold },
                UiFactory.ActionButton(MaterialIconKind.Plus, "Добавить тип", () => _ = AddTypeAsync())
                    .Grid_Row(1)
                    .Margin(new Thickness(0, 16, 0, 12))
                    .HorizontalAlignment(HorizontalAlignment.Left),
                new ScrollViewer().Grid_Row(2)
                    .Content(new ItemsControl
                    {
                        ItemsSource = _state.Types,
                        ItemTemplate = new FuncDataTemplate<ProductType>((type, _) => type is null ? null : BuildTypeRow(type))
                    }));
    }

    private Control BuildTypeRow(ProductType type) =>
        new Border
        {
            BorderBrush = Avalonia.Media.Brush.Parse("#E0E0E0"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(12, 6),
            Margin = new Thickness(0, 0, 0, 8),
            Child = new Grid().Cols("*, Auto, Auto")
                .Children(
                    new TextBlock { Text = type.Name, VerticalAlignment = VerticalAlignment.Center, FontSize = 15 },
                    new Button
                        {
                            Classes = { "action" },
                            Content = new MaterialIcon { Kind = MaterialIconKind.Pencil }
                        }
                        .Grid_Column(1)
                        .OnClick(args => _ = EditTypeAsync(type)),
                    new Button
                        {
                            Classes = { "action" },
                            Content = new MaterialIcon { Kind = MaterialIconKind.DeleteOutline }
                        }
                        .Grid_Column(2)
                        .OnClick(args => _ = DeleteTypeAsync(type)))
        };

    public async void Initialize() => await ReloadAsync();

    private async Task ReloadAsync()
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var productTypeService = scope.ServiceProvider.GetRequiredService<IProductTypeService>();
        var types = await productTypeService.GetAllAsync();

        _state.Types.Clear();
        foreach (var type in types)
            _state.Types.Add(type);
    }

    private async Task AddTypeAsync()
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var editDialog = scope.ServiceProvider.GetRequiredService<ProductTypeEditDialog>();
        editDialog.Initialize();
        var accepted = await editDialog.ShowDialog<bool?>(this);
        if (accepted == true)
            await ReloadAsync();
    }

    private async Task EditTypeAsync(ProductType type)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var editDialog = scope.ServiceProvider.GetRequiredService<ProductTypeEditDialog>();
        editDialog.Initialize(type);
        var accepted = await editDialog.ShowDialog<bool?>(this);
        if (accepted == true)
            await ReloadAsync();
    }

    private async Task DeleteTypeAsync(ProductType type)
    {
        var confirmed = await DeleteConfirmationDialog.ShowAsync(this, $"Удалить тип \"{type.Name}\"?");
        if (!confirmed)
            return;

        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var productTypeService = scope.ServiceProvider.GetRequiredService<IProductTypeService>();
            await productTypeService.DeleteAsync(type.Id);
            await ReloadAsync();
        }
        catch (Exception exception)
        {
            await MessageDialog.ShowInfoAsync(this, exception.Message);
        }
    }
}
