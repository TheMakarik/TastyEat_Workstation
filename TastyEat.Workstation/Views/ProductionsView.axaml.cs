using System.Reactive;
using System.Reactive.Disposables;
using Avalonia.Controls;
using Avalonia.ReactiveUI;
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

    private async Task DoConfirmDeleteAsync(IInteractionContext<ProductionNodeViewModel, bool> interaction)
    {
        var owner = TopLevel.GetTopLevel(this) as Window ?? throw new InvalidOperationException("No top-level window found");

        var node = interaction.Input;
        var entityName = node.IsBatch
            ? $"развод \"{node.Name}\""
            : $"позицию \"{node.Name}\"";
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
}
