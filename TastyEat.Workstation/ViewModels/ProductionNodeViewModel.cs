using System.Collections.ObjectModel;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace TastyEat.Workstation.ViewModels;

public enum ProductionNodeKind
{
    Batch,
    Item,
    Distribution,
    DistributionClient
}

public sealed partial class ProductionNodeViewModel : ReactiveObject
{
    [Reactive]
    private int _id;

    [Reactive]
    private int _distributionId;

    [Reactive]
    private string _name = string.Empty;

    [Reactive]
    private ProductionNodeKind _kind;

    [Reactive]
    private string _quantityText = string.Empty;

    public ObservableCollection<ProductionNodeViewModel> Children { get; } = [];

    public bool IsBatch => Kind == ProductionNodeKind.Batch;

    public bool IsItem => Kind == ProductionNodeKind.Item;

    public bool IsDistribution => Kind == ProductionNodeKind.Distribution;

    public bool IsDistributionClient => Kind == ProductionNodeKind.DistributionClient;

    public void SetQuantity(int quantity)
        => QuantityText = $"{quantity} шт";

    public void SetAmount(int amount)
        => QuantityText = $"{amount:N0} ₽";
}
