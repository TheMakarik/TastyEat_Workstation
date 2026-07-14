using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using Avalonia.Controls;
using ReactiveUI;
using TastyEat.Workstation.ViewModels;

namespace TastyEat.Workstation.Views;

public partial class DistributionDateWindow : ReactiveWindow<DistributionDateViewModel>
{
    public DistributionDateWindow()
    {
        InitializeComponent();
        this.WhenActivated(disposables =>
        {
            ViewModel?.SaveCommand.Subscribe(result => Close(result)).DisposeWith(disposables);
        });
    }

    private void CancelButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close(null);
    }
}
