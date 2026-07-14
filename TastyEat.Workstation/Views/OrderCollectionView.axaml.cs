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

    private async Task DoEditClientOrderAsync(IInteractionContext<OrderCollectionClientEditViewModel, OrderCollectionClientEditDto?> interaction)
    {
        var window = new OrderCollectionClientEditWindow { DataContext = interaction.Input };
        var owner = TopLevel.GetTopLevel(this) as Window ?? throw new InvalidOperationException("No top-level window found");
        var result = await window.ShowDialog<OrderCollectionClientEditDto?>(owner);
        interaction.SetOutput(result);
    }

    private async Task DoConfirmDeleteAsync(IInteractionContext<OrderCollectionNodeViewModel, bool> interaction)
    {
        var owner = TopLevel.GetTopLevel(this) as Window ?? throw new InvalidOperationException("No top-level window found");

        var node = interaction.Input;
        var entityName = node.IsCollection
            ? $"сбор \"{node.Name}\""
            : $"клиента \"{node.Name}\"";
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

    private async Task DoStartCollectionChoiceAsync(IInteractionContext<string, bool?> interaction)
    {
        var owner = TopLevel.GetTopLevel(this) as Window ?? throw new InvalidOperationException("No top-level window found");

        var closeButton = new Button { Content = "Закрыть и начать новый" };
        closeButton.Classes.Add("accent");
        var cancelButton = new Button { Content = "Отмена", IsCancel = true };

        bool? result = null;

        var dialog = new Window
        {
            Title = "Незавершённый сбор",
            Width = 480,
            Height = 200,
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
                        Text = interaction.Input,
                        TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
                    },
                    new StackPanel
                    {
                        Orientation = Avalonia.Layout.Orientation.Horizontal,
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                        Spacing = 12,
                        Children = { closeButton, cancelButton }
                    }
                }
            }
        };

        closeButton.Click += (_, _) =>
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

    private async Task DoShowInfoAsync(IInteractionContext<string, Unit> interaction)
    {
        var owner = TopLevel.GetTopLevel(this) as Window ?? throw new InvalidOperationException("No top-level window found");

        var okButton = new Button { Content = "ОК", IsDefault = true };
        okButton.Classes.Add("accent");

        var dialog = new Window
        {
            Title = "Информация",
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
                        Text = interaction.Input,
                        TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
                    },
                    new StackPanel
                    {
                        Orientation = Avalonia.Layout.Orientation.Horizontal,
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                        Spacing = 12,
                        Children = { okButton }
                    }
                }
            }
        };

        okButton.Click += (_, _) => dialog.Close();

        await dialog.ShowDialog(owner);
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
