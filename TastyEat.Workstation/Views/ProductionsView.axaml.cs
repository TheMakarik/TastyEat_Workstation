using System.Reactive;
using System.Reactive.Disposables;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.ReactiveUI;
using Material.Icons;
using Material.Icons.Avalonia;
using ReactiveUI;
using TastyEat.Workstation.Models.Dto;
using TastyEat.Workstation.ViewModels;
using TastyEat.Workstation.Views.Utils;

namespace TastyEat.Workstation.Views;

public partial class ProductionsView : ReactiveUserControl<ProductionsViewModel>
{
    public ProductionsView()
    {
        InitializeComponent();
        this.WhenActivated(disposables =>
        {
            ViewModel?.AddProductionInteraction.RegisterHandler(async interaction => await DoAddProductionAsync(interaction)).DisposeWith(disposables);
            ViewModel?.EditProductionItemInteraction.RegisterHandler(async interaction => await DoEditItemAsync(interaction)).DisposeWith(disposables);
            ViewModel?.AddDistributionInteraction.RegisterHandler(async interaction => await DoAddDistributionAsync(interaction)).DisposeWith(disposables);
            ViewModel?.EditDistributionClientInteraction.RegisterHandler(async interaction => await DoEditDistributionClientAsync(interaction)).DisposeWith(disposables);
            ViewModel?.ConfirmDeleteInteraction.RegisterHandler(async interaction => await DoConfirmDeleteAsync(interaction)).DisposeWith(disposables);
        });
    }

    private async Task DoAddProductionAsync(IInteractionContext<ProductionEditViewModel, bool> interaction) =>
        await interaction.ShowDialogAsync(this, vm => new ProductionEditWindow { DataContext = vm });

    private async Task DoEditItemAsync(IInteractionContext<ProductionItemEditViewModel, ProductionItemEditDto?> interaction) =>
        await interaction.ShowDialogAsync(this, vm => new ProductionItemEditWindow { DataContext = vm });

    private async Task DoAddDistributionAsync(IInteractionContext<DistributionDateViewModel, DateTimeOffset?> interaction) =>
        await interaction.ShowDialogAsync(this, vm => new DistributionDateWindow { DataContext = vm });

    private async Task DoEditDistributionClientAsync(IInteractionContext<DistributionEditViewModel, DistributionClientEditDto?> interaction) =>
        await interaction.ShowDialogAsync(this, vm => new DistributionEditWindow { DataContext = vm });

    private async Task DoConfirmDeleteAsync(IInteractionContext<ProductionNodeViewModel, bool> interaction)
    {
        var owner = this.GetOwnerWindow();
        var node = interaction.Input;
        var entityName = node.Kind switch
        {
            ProductionNodeKind.Batch => $"производство \"{node.Name}\"",
            ProductionNodeKind.Item => $"позицию \"{node.Name}\"",
            ProductionNodeKind.Distribution => $"развоз \"{node.Name}\"",
            ProductionNodeKind.DistributionClient => $"клиента \"{node.Name}\"",
            _ => $"\"{node.Name}\""
        };
        var message = $"Удалить {entityName}?";
        var result = await DeleteConfirmationDialog.ShowAsync(owner, message);
        interaction.SetOutput(result);
    }

    private void OnActionsButtonClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not ProductionNodeViewModel node || ViewModel is null)
            return;

        var flyout = new MenuFlyout();

        if (node.IsBatch || node.IsItem || node.IsDistributionClient)
        {
            flyout.Items.Add(new MenuItem
            {
                Header = "Изменить",
                Icon = new MaterialIcon { Kind = MaterialIconKind.Pencil },
                Command = ViewModel.EditNodeCommand,
                CommandParameter = node
            });
        }

        if (node.IsDistribution)
        {
            flyout.Items.Add(new MenuItem
            {
                Header = "Добавить клиента",
                Icon = new MaterialIcon { Kind = MaterialIconKind.AccountPlus },
                Command = ViewModel.AddDistributionClientCommand,
                CommandParameter = node
            });
        }

        flyout.Items.Add(new MenuItem
        {
            Header = "Удалить",
            Icon = new MaterialIcon { Kind = MaterialIconKind.Delete },
            Command = ViewModel.DeleteNodeCommand,
            CommandParameter = node
        });

        flyout.ShowAt(button);
    }
}
