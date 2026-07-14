using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using Avalonia.Controls;
using ReactiveUI;
using TastyEat.Workstation.ViewModels;

namespace TastyEat.Workstation.Views;

public partial class DistributionEditWindow : ReactiveWindow<DistributionEditViewModel>
{
    public DistributionEditWindow()
    {
        InitializeComponent();
        this.WhenActivated(disposables =>
        {
            ViewModel?.SaveCommand.Subscribe(result => Close(result)).DisposeWith(disposables);
        });
    }

    private async void CancelButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var owner = TopLevel.GetTopLevel(this) as Window ?? throw new InvalidOperationException("No top-level window found");

        var result = false;
        var discardButton = new Button { Content = "Отменить изменения" };
        discardButton.Classes.Add("accent");
        var continueButton = new Button { Content = "Продолжить редактирование", IsCancel = true };

        var dialog = new Window
        {
            Title = "Подтверждение отмены",
            Width = 460,
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
                        Text = "Внесённые изменения не будут сохранены. Отменить?",
                        TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
                    },
                    new StackPanel
                    {
                        Orientation = Avalonia.Layout.Orientation.Horizontal,
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                        Spacing = 12,
                        Children = { discardButton, continueButton }
                    }
                }
            }
        };

        discardButton.Click += (_, _) =>
        {
            result = true;
            dialog.Close();
        };
        continueButton.Click += (_, _) =>
        {
            result = false;
            dialog.Close();
        };

        await dialog.ShowDialog(owner);
        if (result)
            Close(null);
    }
}
