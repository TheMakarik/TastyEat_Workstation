using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using Avalonia.Controls;
using ReactiveUI;
using TastyEat.Workstation.ViewModels;

namespace TastyEat.Workstation.Views;

public partial class OrderCollectionClientEditWindow : ReactiveWindow<OrderCollectionClientEditViewModel>
{
    public OrderCollectionClientEditWindow()
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
