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

    private async Task DoAddProductionAsync(IInteractionContext<ProductionEditViewModel, bool> interaction)
    {
        var window = new ProductionEditWindow { DataContext = interaction.Input };
        var owner = TopLevel.GetTopLevel(this) as Window ?? throw new InvalidOperationException("No top-level window found");
        var result = await window.ShowDialog<bool>(owner);
        interaction.SetOutput(result);
    }

    private async Task DoEditItemAsync(IInteractionContext<ProductionItemEditViewModel, ProductionItemEditDto?> interaction)
    {
        var window = new ProductionItemEditWindow { DataContext = interaction.Input };
        var owner = TopLevel.GetTopLevel(this) as Window ?? throw new InvalidOperationException("No top-level window found");
        var result = await window.ShowDialog<ProductionItemEditDto?>(owner);
        interaction.SetOutput(result);
    }

    private async Task DoAddDistributionAsync(IInteractionContext<DistributionDateViewModel, DateTimeOffset?> interaction)
    {
        var window = new DistributionDateWindow { DataContext = interaction.Input };
        var owner = TopLevel.GetTopLevel(this) as Window ?? throw new InvalidOperationException("No top-level window found");
        var result = await window.ShowDialog<DateTimeOffset?>(owner);
        interaction.SetOutput(result);
    }

    private async Task DoEditDistributionClientAsync(IInteractionContext<DistributionEditViewModel, DistributionClientEditDto?> interaction)
    {
        var window = new DistributionEditWindow { DataContext = interaction.Input };
        var owner = TopLevel.GetTopLevel(this) as Window ?? throw new InvalidOperationException("No top-level window found");
        var result = await window.ShowDialog<DistributionClientEditDto?>(owner);
        interaction.SetOutput(result);
    }

    private async Task DoConfirmDeleteAsync(IInteractionContext<ProductionNodeViewModel, bool> interaction)
    {
        var owner = TopLevel.GetTopLevel(this) as Window ?? throw new InvalidOperationException("No top-level window found");

        var node = interaction.Input;
        var entityName = node.Kind switch
        {
            ProductionNodeKind.Batch => $"производство \"{node.Name}\"",
            ProductionNodeKind.Item => $"позицию \"{node.Name}\"",
            ProductionNodeKind.Distribution => $"развоз \"{node.Name}\"",
            ProductionNodeKind.DistributionClient => $"клиента \"{node.Name}\"",
            _ => $"\"{node.Name}\""
        };
        var result = false;

        var deleteButton = new Button { Content = "Удалить" };
        deleteButton.Classes.Add("accent");
        var cancelButton = new Button { Content = "Отмена", IsCancel = true };

        var dialog = new Window
        {
            Title = "Подтверждение удаления",
            Width = 420,
            Height = 180,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = new StackPanel
            {
                Margin = new Avalonia.Thickness(24),
                Spacing = 24,
                Children =
                {
                    new TextBlock
                    {
                        Text = $"Удалить {entityName}?",
                        TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
                    },
                    new StackPanel
                    {
                        Orientation = Avalonia.Layout.Orientation.Horizontal,
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                        Spacing = 12,
                        Children = { deleteButton, cancelButton }
                    }
                }
            }
        };

        deleteButton.Click += (_, _) =>
        {
            result = true;
            dialog.Close();
        };
        cancelButton.Click += (_, _) =>
        {
            result = false;
            dialog.Close();
        };

        await dialog.ShowDialog(owner);
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
