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

public partial class OrderCollectionView : ReactiveUserControl<OrderCollectionViewModel>
{
    public OrderCollectionView()
    {
        InitializeComponent();
        this.WhenActivated(disposables =>
        {
            ViewModel?.EditClientOrderInteraction.RegisterHandler(async interaction => await DoEditClientOrderAsync(interaction)).DisposeWith(disposables);
            ViewModel?.ConfirmDeleteInteraction.RegisterHandler(async interaction => await DoConfirmDeleteAsync(interaction)).DisposeWith(disposables);
            ViewModel?.StartCollectionChoiceInteraction.RegisterHandler(async interaction => await DoStartCollectionChoiceAsync(interaction)).DisposeWith(disposables);
            ViewModel?.ShowInfoInteraction.RegisterHandler(async interaction => await DoShowInfoAsync(interaction)).DisposeWith(disposables);
        });
    }

    private async Task DoEditClientOrderAsync(IInteractionContext<OrderCollectionClientEditViewModel, OrderCollectionClientEditDto?> interaction) =>
        await interaction.ShowDialogAsync(this, vm => new OrderCollectionClientEditWindow { DataContext = vm });

    private async Task DoConfirmDeleteAsync(IInteractionContext<OrderCollectionNodeViewModel, bool> interaction)
    {
        var owner = this.GetOwnerWindow();
        var node = interaction.Input;
        var entityName = node.IsCollection
            ? $"сбор \"{node.Name}\""
            : $"клиента \"{node.Name}\"";
        var message = $"Удалить {entityName}?";
        var result = await DeleteConfirmationDialog.ShowAsync(owner, message);
        interaction.SetOutput(result);
    }

    private async Task DoStartCollectionChoiceAsync(IInteractionContext<string, bool?> interaction)
    {
        var owner = this.GetOwnerWindow();
        var result = await MessageDialog.ChoiceAsync(
            owner,
            interaction.Input,
            "Незавершённый сбор",
            "Закрыть и начать новый",
            "Отмена");
        interaction.SetOutput(result);
    }

    private async Task DoShowInfoAsync(IInteractionContext<string, Unit> interaction)
    {
        var owner = this.GetOwnerWindow();
        await MessageDialog.ShowInfoAsync(owner, interaction.Input);
        interaction.SetOutput(Unit.Default);
    }

    private void OnActionsButtonClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not OrderCollectionNodeViewModel node || ViewModel is null)
            return;

        var flyout = new MenuFlyout();

        if (node.IsClient)
        {
            flyout.Items.Add(new MenuItem
            {
                Header = "Изменить",
                Icon = new MaterialIcon { Kind = MaterialIconKind.Pencil },
                Command = ViewModel.EditNodeCommand,
                CommandParameter = node
            });
        }

        if (node.IsCollection)
        {
            flyout.Items.Add(new MenuItem
            {
                Header = "Завершить сбор",
                Icon = new MaterialIcon { Kind = MaterialIconKind.CheckCircle },
                Command = ViewModel.CloseCollectionCommand,
                CommandParameter = node,
                IsEnabled = !node.IsCompleted
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
