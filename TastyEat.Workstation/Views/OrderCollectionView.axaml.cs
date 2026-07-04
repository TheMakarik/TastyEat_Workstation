using Avalonia.ReactiveUI;
using TastyEat.Workstation.ViewModels;

namespace TastyEat.Workstation.Views;

public partial class OrderCollectionView : ReactiveUserControl<OrderCollectionViewModel>
{
    public OrderCollectionView()
    {
        InitializeComponent();
    }
}
