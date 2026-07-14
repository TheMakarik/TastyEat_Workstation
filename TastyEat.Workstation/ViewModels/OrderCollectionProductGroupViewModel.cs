using System.Collections.ObjectModel;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using TastyEat.Workstation.Models.Tables;

namespace TastyEat.Workstation.ViewModels;

public sealed partial class OrderCollectionProductGroupViewModel : ReactiveObject
{
    [Reactive]
    private ProductType _productType = null!;

    [Reactive]
    private bool _isVisible = true;

    public ObservableCollection<OrderCollectionProductRowViewModel> Rows { get; } = [];
}
