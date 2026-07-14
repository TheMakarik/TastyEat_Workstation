using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using Avalonia.Controls;
using Avalonia.Controls.Models.TreeDataGrid;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using TastyEat.Workstation.Models.Dto;
using TastyEat.Workstation.Models.Tables;
using TastyEat.Workstation.Services.Interfaces;

namespace TastyEat.Workstation.ViewModels;

public sealed partial class OrderCollectionViewModel : ViewModelBase, IDisposable
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly OrderCollectionClientEditViewModel _clientEditViewModel;
    private readonly ILogger<OrderCollectionViewModel> _logger;
    private readonly LoadingControlViewModel _loading;
    private CancellationTokenSource? _loadCts;
    private bool _disposed;

    [Reactive]
    private string _searchText = string.Empty;

    [Reactive]
    private bool _isLoading;

    public OrderCollectionViewModel(
        IServiceScopeFactory scopeFactory,
        OrderCollectionClientEditViewModel clientEditViewModel,
        LoadingControlViewModel loading,
        ILogger<OrderCollectionViewModel> logger)
    {
        _scopeFactory = scopeFactory;
        _clientEditViewModel = clientEditViewModel;
        _loading = loading;
        _logger = logger;

        OrderCollectionNodes = new ObservableCollection<OrderCollectionNodeViewModel>();

        OrderCollectionsSource = new HierarchicalTreeDataGridSource<OrderCollectionNodeViewModel>(OrderCollectionNodes)
        {
            Columns =
            {
                new HierarchicalExpanderColumn<OrderCollectionNodeViewModel>(
                    new TextColumn<OrderCollectionNodeViewModel, string>("Название", x => x.Name, new GridLength(2, GridUnitType.Star)),
                    x => x.Children),
                new TextColumn<OrderCollectionNodeViewModel, string>("Статус", x => x.StatusText, new GridLength(1, GridUnitType.Star)),
                new TemplateColumn<OrderCollectionNodeViewModel>(string.Empty, "OrderCollectionActionsCellTemplate", width: GridLength.Auto),
            }
        };

        this.WhenAnyValue(vm => vm.SearchText)
            .Throttle(TimeSpan.FromMilliseconds(400))
            .DistinctUntilChanged()
            .Select(_ => Unit.Default)
            .InvokeCommand(SearchCommand);

        _ = SearchAsync();
    }

    public override string Title => "Сбор заказов";
    public override string IconName => "CartOutline";

    public ObservableCollection<OrderCollectionNodeViewModel> OrderCollectionNodes { get; }
    public HierarchicalTreeDataGridSource<OrderCollectionNodeViewModel> OrderCollectionsSource { get; }
    public LoadingControlViewModel Loading => _loading;

    public Interaction<OrderCollectionClientEditViewModel, OrderCollectionClientEditDto?> EditClientOrderInteraction { get; } = new();
    public Interaction<OrderCollectionNodeViewModel, bool> ConfirmDeleteInteraction { get; } = new();
    public Interaction<string, bool?> StartCollectionChoiceInteraction { get; } = new();
    public Interaction<string, Unit> ShowInfoInteraction { get; } = new();

    [ReactiveCommand(OutputScheduler = "ReactiveUI.RxApp.MainThreadScheduler")]
    private async Task SearchAsync()
    {
        var token = RefreshLoadCts();

        _loading.IsLoading = true;
        try
        {
            var filtered = await Task.Factory.StartNew(async () =>
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var orderService = scope.ServiceProvider.GetRequiredService<IOrderCollectionService>();
                var collections = await orderService.GetAllAsync(token);
                return FilterCollections(collections, SearchText);
            }, token, TaskCreationOptions.LongRunning, TaskScheduler.Default).Unwrap();

            RxApp.MainThreadScheduler.Schedule(() =>
            {
                if (token.IsCancellationRequested)
                    return;

                OrderCollectionNodes.Clear();
                foreach (var collection in filtered)
                    OrderCollectionNodes.Add(CreateCollectionNode(collection));
            });
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load order collections");
        }
        finally
        {
            RxApp.MainThreadScheduler.Schedule(() => _loading.IsLoading = false);
        }
    }

    [ReactiveCommand(OutputScheduler = "ReactiveUI.RxApp.MainThreadScheduler")]
    private async Task CloseCollectionAsync(OrderCollectionNodeViewModel node)
    {
        if (!node.IsCollection || node.IsCompleted)
            return;

        await using var scope = _scopeFactory.CreateAsyncScope();
        var orderService = scope.ServiceProvider.GetRequiredService<IOrderCollectionService>();
        await orderService.CloseAsync(node.Id);
        await SearchAsync();
    }

    [ReactiveCommand(OutputScheduler = "ReactiveUI.RxApp.MainThreadScheduler")]
    private async Task StartCollectionAsync()
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var orderService = scope.ServiceProvider.GetRequiredService<IOrderCollectionService>();
        var active = await orderService.GetActiveAsync();

        if (active is not null)
        {
            var choice = await StartCollectionChoiceInteraction.Handle(
                $"Уже есть незавершённый сбор ({active.StartDate:yyyy-MM-dd}). Закрыть его и начать новый?");

            if (choice != true)
                return;

            await orderService.CloseAsync(active.Id);
        }

        await orderService.CreateAsync();
        await SearchAsync();
    }

    [ReactiveCommand(OutputScheduler = "ReactiveUI.RxApp.MainThreadScheduler")]
    private async Task AddClientAsync()
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var orderService = scope.ServiceProvider.GetRequiredService<IOrderCollectionService>();
        var productTypeService = scope.ServiceProvider.GetRequiredService<IProductTypeService>();
        var clientService = scope.ServiceProvider.GetRequiredService<IClientService>();

        var active = await orderService.GetActiveAsync();
        if (active is null)
        {
            await ShowInfoInteraction.Handle("Нет открытого сбора заказов. Сначала нажмите «Начать сбор».");
            return;
        }

        var productTypes = await productTypeService.GetAllAsync();
        var clients = await clientService.GetAllAsync();

        await _clientEditViewModel.InitializeAsync(active.Id, clients, productTypes);
        var result = await EditClientOrderInteraction.Handle(_clientEditViewModel);
        if (result is null)
            return;

        await orderService.UpsertClientAsync(active.Id, result);
        await SearchAsync();
    }

    [ReactiveCommand(OutputScheduler = "ReactiveUI.RxApp.MainThreadScheduler")]
    private async Task EditNodeAsync(OrderCollectionNodeViewModel node)
    {
        if (!node.IsClient)
            return;

        await using var scope = _scopeFactory.CreateAsyncScope();
        var orderService = scope.ServiceProvider.GetRequiredService<IOrderCollectionService>();
        var productTypeService = scope.ServiceProvider.GetRequiredService<IProductTypeService>();
        var clientService = scope.ServiceProvider.GetRequiredService<IClientService>();

        var clientEntry = await orderService.GetByIdAsync(node.Id);
        if (clientEntry is null)
        {
            _logger.LogWarning("Order collection client with id {ClientEntryId} not found for editing", node.Id);
            return;
        }

        var collection = await orderService.GetByIdAsync(node.CollectionId);
        if (collection is null)
        {
            _logger.LogWarning("Parent collection for client entry {ClientEntryId} not found", node.Id);
            return;
        }

        var existing = collection.Clients.FirstOrDefault(cc => cc.Id == node.Id);
        if (existing is null)
        {
            _logger.LogWarning("Client entry {ClientEntryId} not found in collection", node.Id);
            return;
        }

        var productTypes = await productTypeService.GetAllAsync();
        var clients = await clientService.GetAllAsync();

        await _clientEditViewModel.InitializeAsync(node.CollectionId, clients, productTypes, existing);
        var result = await EditClientOrderInteraction.Handle(_clientEditViewModel);
        if (result is null)
            return;

        await orderService.UpsertClientAsync(node.CollectionId, result);
        await SearchAsync();
    }

    [ReactiveCommand(OutputScheduler = "ReactiveUI.RxApp.MainThreadScheduler")]
    private async Task DeleteNodeAsync(OrderCollectionNodeViewModel node)
    {
        var confirmed = await ConfirmDeleteInteraction.Handle(node);
        if (!confirmed)
            return;

        await using var scope = _scopeFactory.CreateAsyncScope();
        var orderService = scope.ServiceProvider.GetRequiredService<IOrderCollectionService>();

        if (node.IsCollection)
            await orderService.DeleteCollectionAsync(node.Id);
        else
            await orderService.DeleteClientAsync(node.Id);

        await SearchAsync();
    }

    private static OrderCollectionNodeViewModel CreateCollectionNode(OrderCollection collection, IEnumerable<OrderCollectionClient>? clientsOverride = null)
    {
        var endText = collection.EndDate.HasValue ? collection.EndDate.Value.ToString("yyyy-MM-dd") : "...";
        var node = new OrderCollectionNodeViewModel
        {
            Id = collection.Id,
            Name = $"Сбор заказов {collection.StartDate:yyyy-MM-dd} - {endText}",
            Kind = OrderCollectionNodeKind.Collection,
            StatusText = collection.EndDate.HasValue ? "Завершён" : "Не завершён",
            IsCompleted = collection.EndDate.HasValue
        };

        foreach (var client in clientsOverride ?? collection.Clients)
            node.Children.Add(CreateClientNode(client, collection.Id));

        return node;
    }

    private static OrderCollectionNodeViewModel CreateClientNode(OrderCollectionClient client, int collectionId)
    {
        return new OrderCollectionNodeViewModel
        {
            Id = client.Id,
            CollectionId = collectionId,
            Name = client.Client.FullName,
            Kind = OrderCollectionNodeKind.Client,
            StatusText = string.Empty
        };
    }

    private static IEnumerable<OrderCollection> FilterCollections(IReadOnlyList<OrderCollection> collections, string pattern)
    {
        var trimmed = pattern.Trim();
        if (string.IsNullOrWhiteSpace(trimmed) || trimmed == "*")
            return collections;

        var lower = trimmed.ToLowerInvariant();
        var filtered = new List<OrderCollection>();

        foreach (var collection in collections)
        {
            var name = $"Сбор заказов {collection.StartDate:yyyy-MM-dd} - {(collection.EndDate.HasValue ? collection.EndDate.Value.ToString("yyyy-MM-dd") : "...")}";
            if (name.ToLowerInvariant().Contains(lower))
            {
                filtered.Add(collection);
                continue;
            }

            var matchingClients = collection.Clients
                .Where(c => c.Client.FullName.ToLowerInvariant().Contains(lower))
                .ToList();

            if (matchingClients.Count == 0)
                continue;

            var clone = new OrderCollection
            {
                Id = collection.Id,
                StartDate = collection.StartDate,
                EndDate = collection.EndDate,
                Clients = matchingClients
            };
            filtered.Add(clone);
        }

        return filtered;
    }

    private CancellationToken RefreshLoadCts()
    {
        var newCts = new CancellationTokenSource();
        var oldCts = Interlocked.Exchange(ref _loadCts, newCts);
        oldCts?.Cancel();
        oldCts?.Dispose();
        return newCts.Token;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        var cts = Interlocked.Exchange(ref _loadCts, null);
        cts?.Cancel();
        cts?.Dispose();
    }
}
