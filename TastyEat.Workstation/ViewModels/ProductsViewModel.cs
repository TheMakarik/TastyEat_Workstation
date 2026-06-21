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
using TastyEat.Workstation.Models.Tables;
using TastyEat.Workstation.Services.Interfaces;

namespace TastyEat.Workstation.ViewModels;

public sealed partial class ProductsViewModel : ViewModelBase, IDisposable
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ProductTypeEditViewModel _productTypeEditViewModel;
    private readonly ProductEditViewModel _productEditViewModel;
    private readonly ILogger<ProductsViewModel> _logger;
    private CancellationTokenSource? _loadCts;
    private bool _disposed;

    [Reactive]
    private string _searchText = string.Empty;

    [Reactive]
    private bool _isLoading;

    public ProductsViewModel(
        IServiceScopeFactory scopeFactory,
        ProductTypeEditViewModel productTypeEditViewModel,
        ProductEditViewModel productEditViewModel,
        ILogger<ProductsViewModel> logger)
    {
        _scopeFactory = scopeFactory;
        _productTypeEditViewModel = productTypeEditViewModel;
        _productEditViewModel = productEditViewModel;
        _logger = logger;

        ProductTypes = new ObservableCollection<ProductType>();
        ProductNodes = new ObservableCollection<ProductNodeViewModel>();

        ProductsSource = new HierarchicalTreeDataGridSource<ProductNodeViewModel>(ProductNodes)
        {
            Columns =
            {
                new HierarchicalExpanderColumn<ProductNodeViewModel>(
                    new TextColumn<ProductNodeViewModel, string>("Название", x => x.Name, new GridLength(2, GridUnitType.Star)),
                    x => x.Children),
                new TextColumn<ProductNodeViewModel, string>("Цена", x => x.PriceText, new GridLength(1, GridUnitType.Star)),
                new TemplateColumn<ProductNodeViewModel>(string.Empty, "ProductActionsCellTemplate", width: GridLength.Auto),
            }
        };

        this.WhenAnyValue(vm => vm.SearchText)
            .Throttle(TimeSpan.FromMilliseconds(400))
            .DistinctUntilChanged()
            .Select(_ => Unit.Default)
            .InvokeCommand(SearchCommand);

        _ = SearchAsync();
    }

    public override string Title => "Товары";
    public override string IconName => "PackageVariant";

    public ObservableCollection<ProductType> ProductTypes { get; }
    public ObservableCollection<ProductNodeViewModel> ProductNodes { get; }
    public HierarchicalTreeDataGridSource<ProductNodeViewModel> ProductsSource { get; }

    public Interaction<ProductTypeEditViewModel, bool> AddProductTypeInteraction { get; } = new();
    public Interaction<ProductTypeEditViewModel, bool> EditProductTypeInteraction { get; } = new();
    public Interaction<ProductEditViewModel, ProductEditResult?> AddProductInteraction { get; } = new();
    public Interaction<ProductEditViewModel, ProductEditResult?> EditProductInteraction { get; } = new();
    public Interaction<ProductNodeViewModel, bool> ConfirmDeleteInteraction { get; } = new();

    [ReactiveCommand(OutputScheduler = "ReactiveUI.RxApp.MainThreadScheduler")]
    private async Task SearchAsync()
    {
        var token = RefreshLoadCts();
        await using var scope = _scopeFactory.CreateAsyncScope();
        var productService = scope.ServiceProvider.GetRequiredService<IProductService>();
        var productTypeService = scope.ServiceProvider.GetRequiredService<IProductTypeService>();

        IsLoading = true;
        try
        {
            var types = await productService.SearchAsync(SearchText, token);
            var allTypes = await productTypeService.GetAllAsync(token);

            RxApp.MainThreadScheduler.Schedule(() =>
            {
                if (token.IsCancellationRequested)
                    return;

                ProductTypes.Clear();
                foreach (var type in allTypes)
                    ProductTypes.Add(type);

                ProductNodes.Clear();
                foreach (var type in types)
                    ProductNodes.Add(CreateTypeNode(type));
            });
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load products");
        }
        finally
        {
            RxApp.MainThreadScheduler.Schedule(() => IsLoading = false);
        }
    }

    [ReactiveCommand(OutputScheduler = "ReactiveUI.RxApp.MainThreadScheduler")]
    private async Task AddProductTypeAsync()
    {
        _productTypeEditViewModel.Initialize();
        var accepted = await AddProductTypeInteraction.Handle(_productTypeEditViewModel);
        if (accepted)
            await SearchAsync();
    }

    [ReactiveCommand(OutputScheduler = "ReactiveUI.RxApp.MainThreadScheduler")]
    private async Task AddProductAsync()
    {
        _productEditViewModel.Initialize(ProductTypes);
        var result = await AddProductInteraction.Handle(_productEditViewModel);
        if (result is not null)
            await SearchAsync();
    }

    [ReactiveCommand(OutputScheduler = "ReactiveUI.RxApp.MainThreadScheduler")]
    private async Task EditNodeAsync(ProductNodeViewModel node)
    {
        if (node.IsProductType)
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var productTypeService = scope.ServiceProvider.GetRequiredService<IProductTypeService>();
            var type = await productTypeService.GetByIdAsync(node.Id);

            if (type is null)
            {
                _logger.LogWarning("Product type with id {ProductTypeId} not found for editing", node.Id);
                return;
            }

            _productTypeEditViewModel.Initialize(type);
            var accepted = await EditProductTypeInteraction.Handle(_productTypeEditViewModel);
            if (accepted)
                await SearchAsync();
            return;
        }

        await using var productScope = _scopeFactory.CreateAsyncScope();
        var productService = productScope.ServiceProvider.GetRequiredService<IProductService>();
        var product = await productService.GetByIdAsync(node.Id);

        if (product is null)
        {
            _logger.LogWarning("Product with id {ProductId} not found for editing", node.Id);
            return;
        }

        _productEditViewModel.Initialize(ProductTypes, product);
        var result = await EditProductInteraction.Handle(_productEditViewModel);
        if (result is not null)
            await SearchAsync();
    }

    [ReactiveCommand(OutputScheduler = "ReactiveUI.RxApp.MainThreadScheduler")]
    private async Task DeleteNodeAsync(ProductNodeViewModel node)
    {
        var confirmed = await ConfirmDeleteInteraction.Handle(node);
        if (!confirmed)
            return;

        await using var scope = _scopeFactory.CreateAsyncScope();
        if (node.IsProductType)
        {
            var productTypeService = scope.ServiceProvider.GetRequiredService<IProductTypeService>();
            await productTypeService.DeleteAsync(node.Id);
        }
        else
        {
            var productService = scope.ServiceProvider.GetRequiredService<IProductService>();
            await productService.DeleteAsync(node.Id);
        }

        await SearchAsync();
    }

    private static ProductNodeViewModel CreateTypeNode(ProductType type)
    {
        var node = new ProductNodeViewModel
        {
            Id = type.Id,
            Name = type.Name,
            Kind = ProductNodeKind.Type
        };

        foreach (var product in type.Products)
            node.Children.Add(CreateProductNode(product));

        return node;
    }

    private static ProductNodeViewModel CreateProductNode(Product product)
    {
        var node = new ProductNodeViewModel
        {
            Id = product.Id,
            Name = product.Name,
            Kind = ProductNodeKind.Product
        };

        var currentPrice = product.Prices
            .Where(p => p.EffectiveTo == null)
            .OrderByDescending(p => p.EffectiveFrom)
            .FirstOrDefault();

        if (currentPrice is not null)
            node.SetPrice(currentPrice.Price);

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
