using System.Reactive;
using System.Reactive.Disposables;
using Avalonia.Controls;
using Avalonia.ReactiveUI;
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
        // TODO: implement statistics window
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
}
