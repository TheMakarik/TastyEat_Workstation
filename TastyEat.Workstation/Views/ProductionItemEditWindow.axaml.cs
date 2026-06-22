using Avalonia.Interactivity;
using Avalonia.ReactiveUI;
using ReactiveUI;
using System.Reactive.Disposables;
using TastyEat.Workstation.Models.Dto;
using TastyEat.Workstation.ViewModels;

namespace TastyEat.Workstation.Views;

public partial class ProductionItemEditWindow : ReactiveWindow<ProductionItemEditViewModel>
{
    public ProductionItemEditWindow()
    {
        InitializeComponent();
        this.WhenActivated(disposables =>
        {
            ViewModel?.SaveCommand.Subscribe(result => Close(result)).DisposeWith(disposables);
        });
    }

    private void CancelButton_Click(object? sender, RoutedEventArgs e)
    {
        Close(null);
    }
}
