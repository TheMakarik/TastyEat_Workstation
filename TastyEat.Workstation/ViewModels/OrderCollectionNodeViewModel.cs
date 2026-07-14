using System.Collections.ObjectModel;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace TastyEat.Workstation.ViewModels;

public enum OrderCollectionNodeKind
{
    Collection,
    Client
}

public sealed partial class OrderCollectionNodeViewModel : ReactiveObject
{
    [Reactive]
    private int _id;

    [Reactive]
    private int _collectionId;

    [Reactive]
    private string _name = string.Empty;

    [Reactive]
    private OrderCollectionNodeKind _kind;

    [Reactive]
    private string _statusText = string.Empty;

    [Reactive]
    private bool _isCompleted;

    public ObservableCollection<OrderCollectionNodeViewModel> Children { get; } = [];

    public bool IsCollection => Kind == OrderCollectionNodeKind.Collection;

    public bool IsClient => Kind == OrderCollectionNodeKind.Client;
}
