using System.Collections.ObjectModel;
using System.Linq;
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
using TastyEat.Workstation.Messages;
using TastyEat.Workstation.Models.Dto;
using TastyEat.Workstation.Models.Tables;
using TastyEat.Workstation.Services.Interfaces;

namespace TastyEat.Workstation.ViewModels;

public sealed partial class ProductionsViewModel : ViewModelBase, IDisposable
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ProductionEditViewModel _productionEditViewModel;
    private readonly ProductionItemEditViewModel _productionItemEditViewModel;
    private readonly DistributionEditViewModel _distributionEditViewModel;
    private readonly DistributionDateViewModel _distributionDateViewModel;
    private readonly ILogger<ProductionsViewModel> _logger;
    private readonly LoadingControlViewModel _loading;
    private CancellationTokenSource? _loadCts;
    private bool _disposed;

    [Reactive]
    private string _searchText = string.Empty;

    [Reactive]
    private bool _isLoading;

    public ProductionsViewModel(
        IServiceScopeFactory scopeFactory,
        ProductionEditViewModel productionEditViewModel,
        ProductionItemEditViewModel productionItemEditViewModel,
        DistributionEditViewModel distributionEditViewModel,
        DistributionDateViewModel distributionDateViewModel,
        LoadingControlViewModel loading,
        ILogger<ProductionsViewModel> logger)
    {
        _scopeFactory = scopeFactory;
        _productionEditViewModel = productionEditViewModel;
        _productionItemEditViewModel = productionItemEditViewModel;
        _distributionEditViewModel = distributionEditViewModel;
        _distributionDateViewModel = distributionDateViewModel;
        _loading = loading;
        _logger = logger;

        ProductTypes = new ObservableCollection<ProductType>();
        ProductionNodes = new ObservableCollection<ProductionNodeViewModel>();

        ProductionsSource = new HierarchicalTreeDataGridSource<ProductionNodeViewModel>(ProductionNodes)
        {
            Columns =
            {
                new HierarchicalExpanderColumn<ProductionNodeViewModel>(
                    new TextColumn<ProductionNodeViewModel, string>("Название", x => x.Name, new GridLength(2, GridUnitType.Star)),
                    x => x.Children),
                new TextColumn<ProductionNodeViewModel, string>("Количество", x => x.QuantityText, new GridLength(1, GridUnitType.Star)),
                new TemplateColumn<ProductionNodeViewModel>(string.Empty, "ProductionActionsCellTemplate", width: GridLength.Auto),
            }
        };

        this.WhenAnyValue(vm => vm.SearchText)
            .Throttle(TimeSpan.FromMilliseconds(400))
            .DistinctUntilChanged()
            .Select(_ => Unit.Default)
            .InvokeCommand(SearchCommand);

        _ = SearchAsync();
    }

    public override string Title => "Производство/Развод продукции";
    public override string IconName => "Factory";

    public ObservableCollection<ProductType> ProductTypes { get; }
    public ObservableCollection<ProductionNodeViewModel> ProductionNodes { get; }
    public HierarchicalTreeDataGridSource<ProductionNodeViewModel> ProductionsSource { get; }
    public LoadingControlViewModel Loading => _loading;

    public Interaction<ProductionEditViewModel, bool> AddProductionInteraction { get; } = new();
    public Interaction<ProductionItemEditViewModel, ProductionItemEditDto?> EditProductionItemInteraction { get; } = new();
    public Interaction<DistributionDateViewModel, DateTimeOffset?> AddDistributionInteraction { get; } = new();
    public Interaction<DistributionEditViewModel, DistributionClientEditDto?> EditDistributionClientInteraction { get; } = new();
    public Interaction<ProductionNodeViewModel, bool> ConfirmDeleteInteraction { get; } = new();

    [ReactiveCommand(OutputScheduler = "ReactiveUI.RxApp.MainThreadScheduler")]
    private async Task SearchAsync()
    {
        var token = RefreshLoadCts();

        _loading.IsLoading = true;
        try
        {
            var (batches, distributions, allTypes) = await Task.Factory.StartNew(async () =>
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var productionService = scope.ServiceProvider.GetRequiredService<IProductionService>();
                var distributionService = scope.ServiceProvider.GetRequiredService<IDistributionService>();
                var productTypeService = scope.ServiceProvider.GetRequiredService<IProductTypeService>();
                var loadedBatches = await productionService.GetBatchesAsync(SearchText, token);
                var loadedDistributions = await distributionService.GetAllAsync(token);
                var loadedAllTypes = await productTypeService.GetAllAsync(token);
                return (loadedBatches, loadedDistributions, loadedAllTypes);
            }, token, TaskCreationOptions.LongRunning, TaskScheduler.Default).Unwrap();

            RxApp.MainThreadScheduler.Schedule(() =>
            {
                if (token.IsCancellationRequested)
                    return;

                ProductTypes.Clear();
                foreach (var type in allTypes)
                    ProductTypes.Add(type);

                ProductionNodes.Clear();
                var productionNodes = batches
                    .Select(b => (Date: b.StartDate, Node: CreateBatchNode(b)))
                    .Concat(distributions.Select(d => (Date: d.Date, Node: CreateDistributionNode(d))))
                    .OrderByDescending(x => x.Date)
                    .Select(x => x.Node);

                foreach (var node in productionNodes)
                    ProductionNodes.Add(node);
            });
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load production and distribution data");
        }
        finally
        {
            RxApp.MainThreadScheduler.Schedule(() => _loading.IsLoading = false);
        }
    }

    [ReactiveCommand(OutputScheduler = "ReactiveUI.RxApp.MainThreadScheduler")]
    private async Task AddProductionAsync()
    {
        await RefreshProductTypesAsync();
        _productionEditViewModel.Initialize(ProductTypes);
        var accepted = await AddProductionInteraction.Handle(_productionEditViewModel);
        if (accepted)
            await SearchAsync();
    }

    [ReactiveCommand(OutputScheduler = "ReactiveUI.RxApp.MainThreadScheduler")]
    private async Task AddDistributionAsync()
    {
        var date = await AddDistributionInteraction.Handle(_distributionDateViewModel);
        if (!date.HasValue)
            return;

        await using var scope = _scopeFactory.CreateAsyncScope();
        var distributionService = scope.ServiceProvider.GetRequiredService<IDistributionService>();
        await distributionService.CreateAsync(date.Value.Date);
        await SearchAsync();
    }

    [ReactiveCommand(OutputScheduler = "ReactiveUI.RxApp.MainThreadScheduler")]
    private async Task AddDistributionClientAsync(ProductionNodeViewModel node)
    {
        if (!node.IsDistribution)
            return;

        await using var scope = _scopeFactory.CreateAsyncScope();
        var clientService = scope.ServiceProvider.GetRequiredService<IClientService>();
        var clients = await clientService.GetAllAsync();

        _distributionEditViewModel.Initialize(clients, node.Id);
        var result = await EditDistributionClientInteraction.Handle(_distributionEditViewModel);
        if (result is null)
            return;

        await using var saveScope = _scopeFactory.CreateAsyncScope();
        var distributionService = saveScope.ServiceProvider.GetRequiredService<IDistributionService>();
        await distributionService.AddClientAsync(node.Id, result.ClientId, result.TotalAmount, result.Items);
        MessageBus.Current.SendMessage(new ClientPurchasesChangedMessage(result.ClientId));
        await SearchAsync();
    }

    [ReactiveCommand(OutputScheduler = "ReactiveUI.RxApp.MainThreadScheduler")]
    private async Task EditNodeAsync(ProductionNodeViewModel node)
    {
        if (node.IsBatch)
        {
            await EditBatchAsync(node);
            return;
        }

        if (node.IsDistributionClient)
        {
            await EditDistributionClientAsync(node);
            return;
        }

        await EditProductionItemAsync(node);
    }

    [ReactiveCommand(OutputScheduler = "ReactiveUI.RxApp.MainThreadScheduler")]
    private async Task DeleteNodeAsync(ProductionNodeViewModel node)
    {
        var confirmed = await ConfirmDeleteInteraction.Handle(node);
        if (!confirmed)
            return;

        await using var scope = _scopeFactory.CreateAsyncScope();

        if (node.IsBatch)
        {
            var productionService = scope.ServiceProvider.GetRequiredService<IProductionService>();
            await productionService.DeleteBatchAsync(node.Id);
        }
        else if (node.IsItem)
        {
            var productionService = scope.ServiceProvider.GetRequiredService<IProductionService>();
            await productionService.DeleteItemAsync(node.Id);
        }
        else if (node.IsDistribution)
        {
            var distributionService = scope.ServiceProvider.GetRequiredService<IDistributionService>();
            await distributionService.DeleteDistributionAsync(node.Id);
        }
        else if (node.IsDistributionClient)
        {
            var distributionService = scope.ServiceProvider.GetRequiredService<IDistributionService>();
            var distributionClient = await distributionService.GetClientByIdAsync(node.Id);
            if (distributionClient is not null)
                MessageBus.Current.SendMessage(new ClientPurchasesChangedMessage(distributionClient.Client.Id));

            await distributionService.DeleteClientAsync(node.Id);
        }

        await SearchAsync();
    }

    private async Task EditProductionItemAsync(ProductionNodeViewModel node)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var productionService = scope.ServiceProvider.GetRequiredService<IProductionService>();
        var item = await productionService.GetItemByIdAsync(node.Id);

        if (item is null)
        {
            _logger.LogWarning("Production item with id {ItemId} not found for editing", node.Id);
            return;
        }

        await RefreshProductTypesAsync();

        var productType = ProductTypes.FirstOrDefault(t => t.Id == item.Product!.ProductType.Id);
        _productionItemEditViewModel.Initialize(ProductTypes, productType, item.Product, (int)item.Quantity);

        var result = await EditProductionItemInteraction.Handle(_productionItemEditViewModel);
        if (result is null)
            return;

        result = result with { Id = item.Id };
        await productionService.UpdateItemAsync(result);
        await SearchAsync();
    }

    private async Task EditBatchAsync(ProductionNodeViewModel node)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var productionService = scope.ServiceProvider.GetRequiredService<IProductionService>();
        var batch = await productionService.GetBatchByIdAsync(node.Id);

        if (batch is null)
        {
            _logger.LogWarning("Production batch with id {BatchId} not found for editing", node.Id);
            return;
        }

        await RefreshProductTypesAsync();

        _productionEditViewModel.Initialize(ProductTypes, batch);
        var accepted = await AddProductionInteraction.Handle(_productionEditViewModel);
        if (accepted)
            await SearchAsync();
    }

    private async Task EditDistributionClientAsync(ProductionNodeViewModel node)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var distributionService = scope.ServiceProvider.GetRequiredService<IDistributionService>();
        var clientService = scope.ServiceProvider.GetRequiredService<IClientService>();

        var distributionClient = await distributionService.GetClientByIdAsync(node.Id);
        if (distributionClient is null)
        {
            _logger.LogWarning("Distribution client with id {DistributionClientId} not found for editing", node.Id);
            return;
        }

        var clients = await clientService.GetAllAsync();
        _distributionEditViewModel.Initialize(clients, distributionClient.Distribution.Id, distributionClient);

        var result = await EditDistributionClientInteraction.Handle(_distributionEditViewModel);
        if (result is null)
            return;

        await distributionService.UpdateClientAsync(
            node.Id,
            result.ClientId,
            result.TotalAmount,
            result.Items);
        MessageBus.Current.SendMessage(new ClientPurchasesChangedMessage(result.ClientId));
        await SearchAsync();
    }

    private async Task RefreshProductTypesAsync(CancellationToken cancellationToken = default)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var productTypeService = scope.ServiceProvider.GetRequiredService<IProductTypeService>();
        var types = await productTypeService.GetAllAsync(cancellationToken);

        RxApp.MainThreadScheduler.Schedule(() =>
        {
            ProductTypes.Clear();
            foreach (var type in types)
                ProductTypes.Add(type);
        });
    }

    private static ProductionNodeViewModel CreateBatchNode(ProductionBatch batch)
    {
        var node = new ProductionNodeViewModel
        {
            Id = batch.Id,
            Name = $"Производство {batch.StartDate:yyyy-MM-dd}",
            Kind = ProductionNodeKind.Batch
        };

        foreach (var item in batch.Items)
        {
            var itemNode = CreateItemNode(item);
            node.Children.Add(itemNode);
        }

        return node;
    }

    private static ProductionNodeViewModel CreateItemNode(ProductionBatchItem item)
    {
        var product = item.Product!;
        var node = new ProductionNodeViewModel
        {
            Id = item.Id,
            Name = product.Name,
            Kind = ProductionNodeKind.Item
        };

        node.SetQuantity((int)item.Quantity);

        return node;
    }

    private static ProductionNodeViewModel CreateDistributionNode(Distribution distribution)
    {
        var node = new ProductionNodeViewModel
        {
            Id = distribution.Id,
            Name = $"Развоз {distribution.Date:yyyy-MM-dd}",
            Kind = ProductionNodeKind.Distribution
        };

        foreach (var client in distribution.Clients)
            node.Children.Add(CreateDistributionClientNode(client));

        return node;
    }

    private static ProductionNodeViewModel CreateDistributionClientNode(DistributionClient client)
    {
        var node = new ProductionNodeViewModel
        {
            Id = client.Id,
            DistributionId = client.Distribution.Id,
            Name = client.Client.FullName,
            Kind = ProductionNodeKind.DistributionClient
        };

        node.SetAmount(client.TotalAmount);
        return node;
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
