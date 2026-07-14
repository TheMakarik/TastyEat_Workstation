using System.Reactive;
using System.Reactive.Disposables;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.ReactiveUI;
using Material.Icons;
using Material.Icons.Avalonia;
using ReactiveUI;
using TastyEat.Workstation.ViewModels;

namespace TastyEat.Workstation.Views;

public partial class ProductsView : ReactiveUserControl<ProductsViewModel>
{
    public ProductsView()
    {
        InitializeComponent();
        this.WhenActivated(disposables =>
        {
            ViewModel?.AddProductTypeInteraction.RegisterHandler(async interaction => await DoEditProductTypeAsync(interaction)).DisposeWith(disposables);
            ViewModel?.EditProductTypeInteraction.RegisterHandler(async interaction => await DoEditProductTypeAsync(interaction)).DisposeWith(disposables);
            ViewModel?.AddProductInteraction.RegisterHandler(async interaction => await DoEditProductAsync(interaction)).DisposeWith(disposables);
            ViewModel?.EditProductInteraction.RegisterHandler(async interaction => await DoEditProductAsync(interaction)).DisposeWith(disposables);
            ViewModel?.ConfirmDeleteInteraction.RegisterHandler(async interaction => await DoConfirmDeleteAsync(interaction)).DisposeWith(disposables);
        });
    }

    private async Task DoEditProductTypeAsync(IInteractionContext<ProductTypeEditViewModel, bool> interaction)
    {
        var window = new ProductTypeEditWindow { DataContext = interaction.Input };
        var owner = TopLevel.GetTopLevel(this) as Window ?? throw new InvalidOperationException("No top-level window found");
        var result = await window.ShowDialog<bool>(owner);
        interaction.SetOutput(result);
    }

    private async Task DoEditProductAsync(IInteractionContext<ProductEditViewModel, ProductEditResult?> interaction)
    {
        var window = new ProductEditWindow { DataContext = interaction.Input };
        var owner = TopLevel.GetTopLevel(this) as Window ?? throw new InvalidOperationException("No top-level window found");
        var result = await window.ShowDialog<ProductEditResult?>(owner);
        interaction.SetOutput(result);
    }

    private async Task DoConfirmDeleteAsync(IInteractionContext<ProductNodeViewModel, bool> interaction)
    {
        var owner = TopLevel.GetTopLevel(this) as Window ?? throw new InvalidOperationException("No top-level window found");

        var node = interaction.Input;
        var entityName = node.IsProductType ? $"тип \"{node.Name}\"" : $"продукт \"{node.Name}\"";
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
        if (sender is not Button button || button.Tag is not ProductNodeViewModel node || ViewModel is null)
            return;

        var flyout = new MenuFlyout();

        if (node.IsProductType)
        {
            flyout.Items.Add(new MenuItem
            {
                Header = "Изменить тип",
                Icon = new MaterialIcon { Kind = MaterialIconKind.Pencil },
                Command = ViewModel.EditNodeCommand,
                CommandParameter = node
            });

            flyout.Items.Add(new MenuItem
            {
                Header = "Удалить тип",
                Icon = new MaterialIcon { Kind = MaterialIconKind.Delete },
                Command = ViewModel.DeleteNodeCommand,
                CommandParameter = node
            });
        }
        else if (node.IsProduct)
        {
            flyout.Items.Add(new MenuItem
            {
                Header = "Изменить продукт",
                Icon = new MaterialIcon { Kind = MaterialIconKind.Pencil },
                Command = ViewModel.EditNodeCommand,
                CommandParameter = node
            });

            flyout.Items.Add(new MenuItem
            {
                Header = "Удалить продукт",
                Icon = new MaterialIcon { Kind = MaterialIconKind.Delete },
                Command = ViewModel.DeleteNodeCommand,
                CommandParameter = node
            });
        }

        flyout.ShowAt(button);
    }
}
