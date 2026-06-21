using Avalonia.Interactivity;
using Avalonia.ReactiveUI;
using ReactiveUI;
using System.Reactive.Disposables;
using TastyEat.Workstation.ViewModels;

namespace TastyEat.Workstation.Views;

public partial class CityEditWindow : ReactiveWindow<CityEditViewModel>
{
    public CityEditWindow()
    {
        InitializeComponent();
        this.WhenActivated(disposables =>
        {
            ViewModel?.SaveCommand.Subscribe(result => Close(result is not null)).DisposeWith(disposables);
        });
    }

    private void CancelButton_Click(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }
}
