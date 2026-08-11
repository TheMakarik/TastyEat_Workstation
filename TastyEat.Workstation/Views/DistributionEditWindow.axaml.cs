using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using Avalonia.Controls;
using ReactiveUI;
using TastyEat.Workstation.ViewModels;
using TastyEat.Workstation.Views.Utils;

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
        var owner = this.GetOwnerWindow();
        var result = await MessageDialog.ConfirmCancelAsync(owner, "Внесённые изменения не будут сохранены. Отменить?");
        if (result)
            Close(null);
    }
}
