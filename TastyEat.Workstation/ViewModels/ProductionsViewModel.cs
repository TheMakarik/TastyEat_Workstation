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

public sealed partial class ProductionsViewModel : ViewModelBase, IDisposable
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ProductionEditViewModel _productionEditViewModel;
    private readonly ProductionItemEditViewModel _productionItemEditViewModel;
    private readonly ILogger<ProductionsViewModel> _logger;
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
        ILogger<ProductionsViewModel> logger)
    {
        _scopeFactory = scopeFactory;
        _productionEditViewModel = productionEditViewModel;
        _productionItemEditViewModel = productionItemEditViewModel;
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
                new TextColumn<ProductionNodeViewModel, string>("Произведено", x => x.QuantityText, new GridLength(1, GridUnitType.Star)),
                new TextColumn<ProductionNodeViewModel, string>("Цена", x => x.PriceText, new GridLength(1, GridUnitType.Star)),
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

    public override string Title => "Создание/Развод продукции";
    public override string IconName => "Factory";

    public ObservableCollection<ProductType> ProductTypes { get; }
    public ObservableCollection<ProductionNodeViewModel> ProductionNodes { get; }
    public HierarchicalTreeDataGridSource<ProductionNodeViewModel> ProductionsSource { get; }

    public Interaction<ProductionEditViewModel, bool> AddProductionInteraction { get; } = new();
    public Interaction<ProductionItemEditViewModel, ProductionItemEditDto?> EditProductionItemInteraction { get; } = new();
    public Interaction<ProductionNodeViewModel, bool> ConfirmDeleteInteraction { get; } = new();

    [ReactiveCommand(OutputScheduler = "ReactiveUI.RxApp.MainThreadScheduler")]
    private async Task SearchAsync()
    {
        var token = RefreshLoadCts();
        await using var scope = _scopeFactory.CreateAsyncScope();
        var productionService = scope.ServiceProvider.GetRequiredService<IProductionService>();
        var productTypeService = scope.ServiceProvider.GetRequiredService<IProductTypeService>();

        IsLoading = true;
        try
        {
            var batches = await productionService.GetBatchesAsync(SearchText, token);
            var allTypes = await productTypeService.GetAllAsync(token);

            RxApp.MainThreadScheduler.Schedule(() =>
            {
                if (token.IsCancellationRequested)
                    return;

                ProductTypes.Clear();
                foreach (var type in allTypes)
                    ProductTypes.Add(type);

                ProductionNodes.Clear();
                foreach (var batch in batches)
                    ProductionNodes.Add(CreateBatchNode(batch));
            });
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load production batches");
        }
        finally
        {
            RxApp.MainThreadScheduler.Schedule(() => IsLoading = false);
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
    private async Task EditNodeAsync(ProductionNodeViewModel node)
    {
        if (node.IsBatch)
            return;

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
        _productionItemEditViewModel.Initialize(ProductTypes, productType, item.Product, item.Quantity);

        var result = await EditProductionItemInteraction.Handle(_productionItemEditViewModel);
        if (result is null)
            return;

        result = result with { Id = item.Id };
        await productionService.UpdateItemAsync(result);
        await SearchAsync();
    }

    [ReactiveCommand(OutputScheduler = "ReactiveUI.RxApp.MainThreadScheduler")]
    private async Task DeleteNodeAsync(ProductionNodeViewModel node)
    {
        var confirmed = await ConfirmDeleteInteraction.Handle(node);
        if (!confirmed)
            return;

        await using var scope = _scopeFactory.CreateAsyncScope();
        var productionService = scope.ServiceProvider.GetRequiredService<IProductionService>();

        if (node.IsBatch)
            await productionService.DeleteBatchAsync(node.Id);
        else
            await productionService.DeleteItemAsync(node.Id);

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
            Name = $"Создание продукции {batch.StartDate:yyyy-MM-dd}",
            Kind = ProductionNodeKind.Batch
        };

        var totalCost = 0;
        foreach (var item in batch.Items)
        {
            var itemNode = CreateItemNode(item);
            totalCost += GetItemCost(item);
            node.Children.Add(itemNode);
        }

        node.SetTotalCost(totalCost);
        return node;
    }

    private static ProductionNodeViewModel CreateItemNode(ProductionBatchItem item)
    {
        var product = item.Product!;
        var node = new ProductionNodeViewModel
        {
            Id = item.Id,
            Name = product.Name,
            Kind = ProductionNodeKind.Item,
            IsWeighted = product.IsWeighted
        };

        node.SetQuantity(item.Quantity);

        var totalCost = GetItemCost(item);
        if (totalCost > 0)
            node.SetTotalCost(totalCost);

        return node;
    }

    private static int GetItemCost(ProductionBatchItem item)
    {
        var currentPrice = item.Product!.Prices
            .Where(p => p.EffectiveTo == null)
            .OrderByDescending(p => p.EffectiveFrom)
            .FirstOrDefault();

        return currentPrice is null ? 0 : (int)Math.Round(item.Quantity * currentPrice.Price);
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
