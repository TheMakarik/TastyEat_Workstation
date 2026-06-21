using System.Collections.ObjectModel;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace TastyEat.Workstation.ViewModels;

public enum ProductNodeKind
{
    Type,
    Product
}

public sealed partial class ProductNodeViewModel : ReactiveObject
{
    [Reactive]
    private int _id;

    [Reactive]
    private string _name = string.Empty;

    [Reactive]
    private ProductNodeKind _kind;

    [Reactive]
    private string _priceText = string.Empty;

    public ObservableCollection<ProductNodeViewModel> Children { get; } = [];

    public bool IsProductType => Kind == ProductNodeKind.Type;

    public bool IsProduct => Kind == ProductNodeKind.Product;

    public void SetPrice(int price) => PriceText = $"{price:N0} ₽";
}
