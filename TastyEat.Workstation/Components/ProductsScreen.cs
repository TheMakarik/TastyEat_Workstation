using System.Collections.ObjectModel;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Markup.Declarative;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Material.Icons;
using Material.Icons.Avalonia;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TastyEat.Workstation.Components.Dialogs;
using TastyEat.Workstation.Models.Tables;
using TastyEat.Workstation.Services.Interfaces;
using TastyEat.Workstation.Ui;
using TastyEat.Workstation.Views.Utils;

namespace TastyEat.Workstation.Components;

public sealed partial class ProductsScreen(
    IServiceScopeFactory scopeFactory,
    ILogger<ProductsScreen> logger) : ScreenComponent<ProductsScreen.State>(new State())
{
    public sealed partial class State : ObservableObject
    {
        [ObservableProperty]
        public partial string SearchText { get; set; } = string.Empty;

        [ObservableProperty]
        public partial bool IsLoading { get; set; } = true;
    }

    public enum ProductNodeKind
    {
        Type,
        Product
    }

    public sealed partial class ProductNode : ObservableObject
    {
        [ObservableProperty]
        public partial string PriceText { get; set; } = string.Empty;

        public int Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public ProductNodeKind Kind { get; init; }
        public ObservableCollection<ProductNode> Children { get; } = [];

        public bool IsProductType => Kind == ProductNodeKind.Type;
    }

    public override string Title => "Товары";
    public override MaterialIconKind Icon => MaterialIconKind.PackageVariant;

    private readonly ObservableCollection<ProductType> _productTypes = [];
    private readonly ObservableCollection<ProductNode> _nodes = [];
    private readonly DispatcherTimer _searchDebounce = new() { Interval = TimeSpan.FromMilliseconds(400) };
    private CancellationTokenSource? _loadCts;
    private HierarchicalTreeDataGridSource<ProductNode>? _productsSource;

    private HierarchicalTreeDataGridSource<ProductNode> BuildProductsSource() => new(_nodes)
    {
        Columns =
        {
            new HierarchicalExpanderColumn<ProductNode>(
                new TextColumn<ProductNode, string>("Название", x => x.Name, new GridLength(2, GridUnitType.Star)),
                x => x.Children),
            new TextColumn<ProductNode, string>("Цена", x => x.PriceText, new GridLength(1, GridUnitType.Star)),
            new TemplateColumn<ProductNode>(string.Empty, new FuncDataTemplate<ProductNode>((node, _) =>
                new Button
                {
                    Classes = { "action" },
                    Content = new MaterialIcon { Kind = MaterialIconKind.Menu }
                }.OnClick(eventArgs => ShowNodeActions(eventArgs.Source as Button, node))), width: GridLength.Auto)
        }
    };

    protected override object Build(State state)
    {
        _productsSource ??= BuildProductsSource();
        _searchDebounce.Tick += (_, _) => _ = SearchAsync();
        state.PropertyChanged += (_, eventArgs) =>
        {
            if (eventArgs.PropertyName != nameof(State.SearchText))
                return;
            _searchDebounce.Stop();
            _searchDebounce.Start();
        };

        var searchBox = new SearchTextBox { Width = 320, HorizontalAlignment = HorizontalAlignment.Left }
            .Text(state, x => x.SearchText, Avalonia.Data.BindingMode.TwoWay);

        var buttons = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12 }
            .Children(
                UiFactory.ActionButton(MaterialIconKind.ShapeOutline, "Добавить тип", () => _ = AddProductTypeAsync(), "sidebarAction"),
                UiFactory.ActionButton(MaterialIconKind.Plus, "Добавить продукт", () => _ = AddProductAsync()));

        Avalonia.Threading.Dispatcher.UIThread.Post(async () => await SearchAsync());

        return new Grid().Rows("Auto, Auto, *").Classes("managementLayout")
            .Children(
                UiFactory.Header(MaterialIconKind.PackageVariant, "Товары", "Управление товарами, типами и ценами"),
                new Grid().Cols("*, Auto").Classes("topbar").Grid_Row(1)
                    .Children(searchBox, buttons.Grid_Column(1)),
                new Border().Classes("dataGridHost").Grid_Row(2)
                    .Child(new TreeDataGrid { Source = _productsSource }),
                UiFactory.LoadingOverlay(state, x => x.IsLoading).Grid_RowSpan(3)
            );
    }

    private void ShowNodeActions(Button? anchor, ProductNode? node)
    {
        if (anchor is null || node is null)
            return;

        var flyout = new MenuFlyout();
        if (node.IsProductType)
        {
            flyout.Items.Add(new MenuItem
            {
                Header = "Изменить тип",
                Icon = new MaterialIcon { Kind = MaterialIconKind.Pencil },
                Command = EditNodeCommand,
                CommandParameter = node
            });
            flyout.Items.Add(new MenuItem
            {
                Header = "Удалить тип",
                Icon = new MaterialIcon { Kind = MaterialIconKind.Delete },
                Command = DeleteNodeCommand,
                CommandParameter = node
            });
        }
        else
        {
            flyout.Items.Add(new MenuItem
            {
                Header = "Изменить продукт",
                Icon = new MaterialIcon { Kind = MaterialIconKind.Pencil },
                Command = EditNodeCommand,
                CommandParameter = node
            });
            flyout.Items.Add(new MenuItem
            {
                Header = "Удалить продукт",
                Icon = new MaterialIcon { Kind = MaterialIconKind.Delete },
                Command = DeleteNodeCommand,
                CommandParameter = node
            });
        }

        flyout.ShowAt(anchor);
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        var token = RefreshLoadCts();

        ScreenState.IsLoading = true;
        try
        {
            var searchText = ScreenState.SearchText;
            var (types, allTypes) = await Task.Run(async () =>
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var productService = scope.ServiceProvider.GetRequiredService<IProductService>();
                var productTypeService = scope.ServiceProvider.GetRequiredService<IProductTypeService>();
                var loadedTypes = await productService.SearchAsync(searchText, token);
                var loadedAllTypes = await productTypeService.GetAllAsync(token);
                return (loadedTypes, loadedAllTypes);
            }, token);

            if (token.IsCancellationRequested)
                return;

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                _productTypes.Clear();
                foreach (var type in allTypes)
                    _productTypes.Add(type);

                _nodes.Clear();
                foreach (var type in types)
                    _nodes.Add(CreateTypeNode(type));
            });
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Не удалось загрузить продукты");
        }
        finally
        {
            if (!token.IsCancellationRequested)
                ScreenState.IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task AddProductTypeAsync()
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dialog = scope.ServiceProvider.GetRequiredService<ProductTypeEditDialog>();
        var accepted = await dialog.ShowDialog<bool?>(this.GetOwnerWindow());
        if (accepted == true)
            await SearchAsync();
    }

    [RelayCommand]
    private async Task AddProductAsync()
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dialog = scope.ServiceProvider.GetRequiredService<ProductEditDialog>();
        dialog.Initialize(_productTypes);
        var result = await dialog.ShowDialog<ProductEditResult?>(this.GetOwnerWindow());
        if (result is not null)
            await SearchAsync();
    }

    [RelayCommand]
    private async Task EditNodeAsync(ProductNode node)
    {
        if (node.IsProductType)
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var productTypeService = scope.ServiceProvider.GetRequiredService<IProductTypeService>();
            var type = await productTypeService.GetByIdAsync(node.Id);

            if (type is null)
            {
                logger.LogWarning("Тип продукта с id {ProductTypeId} не найден для редактирования", node.Id);
                return;
            }

            var typeDialog = scope.ServiceProvider.GetRequiredService<ProductTypeEditDialog>();
            typeDialog.Initialize(type);
            var accepted = await typeDialog.ShowDialog<bool?>(this.GetOwnerWindow());
            if (accepted == true)
                await SearchAsync();
            return;
        }

        await using var productScope = scopeFactory.CreateAsyncScope();
        var productService = productScope.ServiceProvider.GetRequiredService<IProductService>();
        var product = await productService.GetByIdAsync(node.Id);

        if (product is null)
        {
            logger.LogWarning("Продукт с id {ProductId} не найден для редактирования", node.Id);
            return;
        }

        var dialog = productScope.ServiceProvider.GetRequiredService<ProductEditDialog>();
        dialog.Initialize(_productTypes, product);
        var result = await dialog.ShowDialog<ProductEditResult?>(this.GetOwnerWindow());
        if (result is not null)
            await SearchAsync();
    }

    [RelayCommand]
    private async Task DeleteNodeAsync(ProductNode node)
    {
        var entityName = node.IsProductType ? $"тип \"{node.Name}\"" : $"продукт \"{node.Name}\"";
        var confirmed = await DeleteConfirmationDialog.ShowAsync(this.GetOwnerWindow(), $"Удалить {entityName}?");
        if (!confirmed)
            return;

        await using var scope = scopeFactory.CreateAsyncScope();
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

    private static ProductNode CreateTypeNode(ProductType type)
    {
        var node = new ProductNode { Id = type.Id, Name = type.Name, Kind = ProductNodeKind.Type };
        foreach (var product in type.Products)
            node.Children.Add(CreateProductNode(product));
        return node;
    }

    private static ProductNode CreateProductNode(Product product)
    {
        var node = new ProductNode { Id = product.Id, Name = product.Name, Kind = ProductNodeKind.Product };
        var currentPrice = product.Prices
            .Where(p => p.EffectiveTo == null)
            .OrderByDescending(p => p.EffectiveFrom)
            .FirstOrDefault();
        if (currentPrice is not null)
            node.PriceText = $"{currentPrice.Price:N0} ₽";
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
}
