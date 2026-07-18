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

public partial class ClientsView : ReactiveUserControl<ClientsViewModel>
{
    public ClientsView()
    {
        InitializeComponent();
        this.WhenActivated(disposables =>
        {
            ViewModel?.AddClientInteraction.RegisterHandler(async interaction => await DoAddClientAsync(interaction)).DisposeWith(disposables);
            ViewModel?.EditClientInteraction.RegisterHandler(async interaction => await DoEditClientAsync(interaction)).DisposeWith(disposables);
            ViewModel?.ConfirmDeleteInteraction.RegisterHandler(async interaction => await DoConfirmDeleteAsync(interaction)).DisposeWith(disposables);
            ViewModel?.ShowStatisticsInteraction.RegisterHandler(async interaction => await DoShowStatisticsAsync(interaction)).DisposeWith(disposables);
            ViewModel?.AddCityInteraction.RegisterHandler(async interaction => await DoAddCityAsync(interaction)).DisposeWith(disposables);
            ViewModel?.ShowPieChartInteraction.RegisterHandler(async interaction => await DoShowPieChartAsync(interaction)).DisposeWith(disposables);
            ViewModel?.ShowLineChartInteraction.RegisterHandler(async interaction => await DoShowLineChartAsync(interaction)).DisposeWith(disposables);
        });
    }

    private async Task DoAddClientAsync(IInteractionContext<ClientEditViewModel, ClientEditResult?> interaction)
    {
        var window = new ClientEditWindow { DataContext = interaction.Input };
        var owner = TopLevel.GetTopLevel(this) as Window ?? throw new InvalidOperationException("No top-level window found");
        var result = await window.ShowDialog<ClientEditResult?>(owner);
        interaction.SetOutput(result);
    }

    private async Task DoEditClientAsync(IInteractionContext<ClientEditViewModel, ClientEditResult?> interaction)
    {
        var window = new ClientEditWindow { DataContext = interaction.Input };
        var owner = TopLevel.GetTopLevel(this) as Window ?? throw new InvalidOperationException("No top-level window found");
        var result = await window.ShowDialog<ClientEditResult?>(owner);
        interaction.SetOutput(result);
    }

    private async Task DoConfirmDeleteAsync(IInteractionContext<ClientRowViewModel, bool> interaction)
    {
        var owner = TopLevel.GetTopLevel(this) as Window ?? throw new InvalidOperationException("No top-level window found");

        var result = false;
        var deleteButton = new Button { Content = "Удалить" };
        deleteButton.Classes.Add("Danger");
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
                        Text = $"Удалить клиента \"{interaction.Input.FullName}\"?",
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

    private Task DoShowStatisticsAsync(IInteractionContext<ClientRowViewModel, Unit> interaction)
    {
        interaction.SetOutput(Unit.Default);
        return Task.CompletedTask;
    }

    private async Task DoAddCityAsync(IInteractionContext<CityEditViewModel, bool> interaction)
    {
        var window = new CityEditWindow { DataContext = interaction.Input };
        var owner = TopLevel.GetTopLevel(this) as Window ?? throw new InvalidOperationException("No top-level window found");
        var result = await window.ShowDialog<bool>(owner);
        interaction.SetOutput(result);
    }

    private async Task DoShowPieChartAsync(IInteractionContext<PieChartViewModel, Unit> interaction)
    {
        var window = new PieChartWindow { DataContext = interaction.Input };
        var owner = TopLevel.GetTopLevel(this) as Window ?? throw new InvalidOperationException("No top-level window found");
        await window.ShowDialog(owner);
        interaction.SetOutput(Unit.Default);
    }

    private async Task DoShowLineChartAsync(IInteractionContext<LineChartViewModel, Unit> interaction)
    {
        var window = new LineChartWindow { DataContext = interaction.Input };
        var owner = TopLevel.GetTopLevel(this) as Window ?? throw new InvalidOperationException("No top-level window found");
        await window.ShowDialog(owner);
        interaction.SetOutput(Unit.Default);
    }

    private void OnActionsButtonClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not ClientRowViewModel row || ViewModel is null)
            return;

        var flyout = new MenuFlyout();

        flyout.Items.Add(new MenuItem
        {
            Header = "Изменить клиента",
            Icon = new MaterialIcon { Kind = MaterialIconKind.Pencil },
            Command = ViewModel.EditClientCommand,
            CommandParameter = row
        });

        /*
        flyout.Items.Add(new MenuItem
        {
            Header = "Открыть статистику клиента",
            Icon = new MaterialIcon { Kind = MaterialIconKind.ChartBox },
            Command = ViewModel.ShowStatisticsCommand,
            CommandParameter = row
        });

        flyout.Items.Add(new MenuItem
        {
            Header = "Диаграмма купленных товаров",
            Icon = new MaterialIcon { Kind = MaterialIconKind.ChartPie },
            Command = ViewModel.ShowClientProductShareChartCommand,
            CommandParameter = row
        });

        flyout.Items.Add(new MenuItem
        {
            Header = "График покупок",
            Icon = new MaterialIcon { Kind = MaterialIconKind.ChartLine },
            Command = ViewModel.ShowClientPurchaseHistoryChartCommand,
            CommandParameter = row
        });
        */

        flyout.Items.Add(new MenuItem
        {
            Header = "Удалить клиента",
            Icon = new MaterialIcon { Kind = MaterialIconKind.Delete },
            Command = ViewModel.DeleteClientCommand,
            CommandParameter = row
        });

        flyout.ShowAt(button);
    }
}
