using System.Reactive.Linq;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using TastyEat.Workstation.Models.Tables;

namespace TastyEat.Workstation.ViewModels;

public sealed partial class OrderCollectionProductRowViewModel : ReactiveObject
{
    [Reactive]
    private Product _product = null!;

    [Reactive]
    private int _orderedQuantity;

    [Reactive]
    private int _availableStock;

    [Reactive]
    private int _producedQuantity;

    [Reactive]
    private int _remainingStock;

    [Reactive]
    private int _orderedFromCollections;

    [Reactive]
    private bool _isVisible = true;

    public OrderCollectionProductRowViewModel()
    {
        this.WhenAnyValue(vm => vm.OrderedQuantity, vm => vm.AvailableStock)
            .Select(tuple => tuple.Item2 - tuple.Item1)
            .Subscribe(value => RemainingStock = value);
    }

    public string CategoryName => Product.ProductType.Name;
}
