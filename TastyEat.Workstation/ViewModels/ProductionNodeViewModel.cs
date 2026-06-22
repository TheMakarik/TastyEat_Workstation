using System.Collections.ObjectModel;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace TastyEat.Workstation.ViewModels;

public enum ProductionNodeKind
{
    Batch,
    Item
}

public sealed partial class ProductionNodeViewModel : ReactiveObject
{
    [Reactive]
    private int _id;

    [Reactive]
    private string _name = string.Empty;

    [Reactive]
    private ProductionNodeKind _kind;

    [Reactive]
    private string _quantityText = string.Empty;

    [Reactive]
    private string _priceText = string.Empty;

    [Reactive]
    private bool _isWeighted;

    public ObservableCollection<ProductionNodeViewModel> Children { get; } = [];

    public bool IsBatch => Kind == ProductionNodeKind.Batch;

    public bool IsItem => Kind == ProductionNodeKind.Item;

    public void SetQuantity(double quantity)
        => QuantityText = IsWeighted ? $"{quantity:F2} кг" : $"{quantity:F0} шт";

    public void SetPrice(int price)
        => PriceText = IsWeighted ? $"{price:N0} ₽/кг" : $"{price:N0} ₽";

    public void SetTotalCost(int total)
        => PriceText = $"{total:N0} ₽";
}
