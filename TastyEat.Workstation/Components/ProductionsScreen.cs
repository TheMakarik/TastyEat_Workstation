using System.Collections.ObjectModel;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Markup.Declarative;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Material.Icons;
using Material.Icons.Avalonia;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TastyEat.Workstation.Messages;
using TastyEat.Workstation.Models.Dto;
using TastyEat.Workstation.Models.Tables;
using TastyEat.Workstation.Services.Interfaces;
using TastyEat.Workstation.Ui;
using TastyEat.Workstation.Views.Utils;

namespace TastyEat.Workstation.Components;

public sealed partial class ProductionsScreen(
    IServiceScopeFactory scopeFactory,
    ILogger<ProductionsScreen> logger) : ScreenComponent<ProductionsScreen.State>(new State())
{
    public sealed partial class State : ObservableObject
    {
        [ObservableProperty]
        public partial bool IsLoading { get; set; } = true;
    }

    public enum ProductionNodeKind
    {
        Batch,
        Item,
        Distribution,
        DistributionClient
    }

    public sealed partial class ProductionNode : ObservableObject
    {
        [ObservableProperty]
        public partial string QuantityText { get; set; } = string.Empty;

        public int Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public ProductionNodeKind Kind { get; init; }
        public ObservableCollection<ProductionNode> Children { get; } = [];

        public bool IsBatch => Kind == ProductionNodeKind.Batch;
        public bool IsItem => Kind == ProductionNodeKind.Item;
        public bool IsDistribution => Kind == ProductionNodeKind.Distribution;
        public bool IsDistributionClient => Kind == ProductionNodeKind.DistributionClient;

        public void SetQuantity(int quantity) => QuantityText = $"{quantity} шт";

        public void SetAmount(int amount) => QuantityText = $"{amount:N0} ₽";
    }

    public override string Title => "Производство/Развоз продукции";
    public override MaterialIconKind Icon => MaterialIconKind.Factory;

    private readonly ObservableCollection<ProductType> _productTypes = [];
    private readonly ObservableCollection<ProductionNode> _nodes = [];
    private CancellationTokenSource? _loadCts;
    private HierarchicalTreeDataGridSource<ProductionNode>? _productionsSource;

    private HierarchicalTreeDataGridSource<ProductionNode> BuildProductionsSource() => new(_nodes)
    {
        Columns =
        {
            new HierarchicalExpanderColumn<ProductionNode>(
                new TextColumn<ProductionNode, string>("Название", x => x.Name, new GridLength(2, GridUnitType.Star)),
                x => x.Children),
            new TextColumn<ProductionNode, string>("Количество", x => x.QuantityText, new GridLength(1, GridUnitType.Star)),
            new TemplateColumn<ProductionNode>(string.Empty, new FuncDataTemplate<ProductionNode>((node, _) =>
                new Button
                {
                    Classes = { "action" },
                    Content = new MaterialIcon { Kind = MaterialIconKind.Menu }
                }.OnClick(eventArgs => ShowNodeActions(eventArgs.Source as Button, node))), width: GridLength.Auto)
        }
    };

    protected override object Build(State state)
    {
        _productionsSource ??= BuildProductionsSource();

        var buttons = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12 }
            .Children(
                UiFactory.ActionButton(MaterialIconKind.Factory, "Новое производство", () => _ = AddProductionAsync()),
                UiFactory.ActionButton(MaterialIconKind.TruckDeliveryOutline, "Новый развоз", () => _ = AddDistributionAsync(), "sidebarAction"));

        Avalonia.Threading.Dispatcher.UIThread.Post(async () => await SearchAsync());

        return new Grid().Rows("Auto, Auto, *").Classes("managementLayout")
            .Children(
                UiFactory.Header(MaterialIconKind.Factory, "Производство и развозы", "Партии производства и распределение продукции"),
                new Grid().Cols("*, Auto").Classes("topbar").Grid_Row(1)
                    .Children(buttons.Grid_Column(1)),
                new Border().Classes("dataGridHost").Grid_Row(2)
                    .Child(new TreeDataGrid { Source = _productionsSource }),
                UiFactory.LoadingOverlay(state, x => x.IsLoading).Grid_RowSpan(3)
            );
    }

    private void ShowNodeActions(Button? anchor, ProductionNode? node)
    {
        if (anchor is null || node is null)
            return;

        var flyout = new MenuFlyout();

        if (node.IsBatch || node.IsItem || node.IsDistributionClient)
        {
            flyout.Items.Add(new MenuItem
            {
                Header = "Изменить",
                Icon = new MaterialIcon { Kind = MaterialIconKind.Pencil },
                Command = EditNodeCommand,
                CommandParameter = node
            });
        }

        if (node.IsDistribution)
        {
            flyout.Items.Add(new MenuItem
            {
                Header = "Добавить клиента",
                Icon = new MaterialIcon { Kind = MaterialIconKind.AccountPlus },
                Command = AddDistributionClientCommand,
                CommandParameter = node
            });
        }

        flyout.Items.Add(new MenuItem
        {
            Header = "Удалить",
            Icon = new MaterialIcon { Kind = MaterialIconKind.Delete },
            Command = DeleteNodeCommand,
            CommandParameter = node
        });

        flyout.ShowAt(anchor);
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        var token = RefreshLoadCts();

        ScreenState.IsLoading = true;
        try
        {
            var (batches, distributions, allTypes) = await Task.Run(async () =>
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var productionService = scope.ServiceProvider.GetRequiredService<IProductionService>();
                var distributionService = scope.ServiceProvider.GetRequiredService<IDistributionService>();
                var productTypeService = scope.ServiceProvider.GetRequiredService<IProductTypeService>();
                var loadedBatches = await productionService.GetBatchesAsync(string.Empty, token);
                var loadedDistributions = await distributionService.GetAllAsync(token);
                var loadedAllTypes = await productTypeService.GetAllAsync(token);
                return (loadedBatches, loadedDistributions, loadedAllTypes);
            }, token);

            if (token.IsCancellationRequested)
                return;

            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                _productTypes.Clear();
                foreach (var type in allTypes)
                    _productTypes.Add(type);

                _nodes.Clear();
                var productionNodes = batches
                    .Select(b => (Date: b.StartDate, Node: CreateBatchNode(b)))
                    .Concat(distributions.Select(d => (Date: d.Date, Node: CreateDistributionNode(d))))
                    .OrderByDescending(x => x.Date)
                    .Select(x => x.Node);

                foreach (var node in productionNodes)
                    _nodes.Add(node);
            });
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Не удалось загрузить данные производств и развозов");
        }
        finally
        {
            if (!token.IsCancellationRequested)
                ScreenState.IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task AddProductionAsync()
    {
        await RefreshProductTypesAsync();
        await using var scope = scopeFactory.CreateAsyncScope();
        var dialog = scope.ServiceProvider.GetRequiredService<Dialogs.ProductionEditDialog>();
        dialog.Initialize(_productTypes);
        var accepted = await dialog.ShowDialog<bool?>(this.GetOwnerWindow());
        if (accepted == true)
            await SearchAsync();
    }

    [RelayCommand]
    private async Task AddDistributionAsync()
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dialog = scope.ServiceProvider.GetRequiredService<Dialogs.DistributionDateDialog>();
        var date = await dialog.ShowDialog<DateTimeOffset?>(this.GetOwnerWindow());
        if (!date.HasValue)
            return;

        var distributionService = scope.ServiceProvider.GetRequiredService<IDistributionService>();
        await distributionService.CreateAsync(date.Value.Date);
        await SearchAsync();
    }

    [RelayCommand]
    private async Task AddDistributionClientAsync(ProductionNode node)
    {
        if (!node.IsDistribution)
            return;

        await using var scope = scopeFactory.CreateAsyncScope();
        var clientService = scope.ServiceProvider.GetRequiredService<IClientService>();
        var clients = await clientService.GetAllAsync();

        var dialog = scope.ServiceProvider.GetRequiredService<Dialogs.DistributionEditDialog>();
        dialog.Initialize(clients, node.Id);
        var result = await dialog.ShowDialog<DistributionClientEditDto?>(this.GetOwnerWindow());
        if (result is null)
            return;

        var distributionService = scope.ServiceProvider.GetRequiredService<IDistributionService>();
        await distributionService.AddClientAsync(node.Id, result.ClientId, result.TotalAmount, result.Items);
        WeakReferenceMessenger.Default.Send(new ClientPurchasesChangedMessage(result.ClientId));
        await SearchAsync();
    }

    [RelayCommand]
    private async Task EditNodeAsync(ProductionNode node)
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

    [RelayCommand]
    private async Task DeleteNodeAsync(ProductionNode node)
    {
        var entityName = node.Kind switch
        {
            ProductionNodeKind.Batch => $"производство \"{node.Name}\"",
            ProductionNodeKind.Item => $"позицию \"{node.Name}\"",
            ProductionNodeKind.Distribution => $"развоз \"{node.Name}\"",
            ProductionNodeKind.DistributionClient => $"клиента \"{node.Name}\"",
            _ => $"\"{node.Name}\""
        };

        var confirmed = await DeleteConfirmationDialog.ShowAsync(this.GetOwnerWindow(), $"Удалить {entityName}?");
        if (!confirmed)
            return;

        await using var scope = scopeFactory.CreateAsyncScope();

        if (node.IsBatch || node.IsItem)
        {
            var productionService = scope.ServiceProvider.GetRequiredService<IProductionService>();
            if (node.IsBatch)
                await productionService.DeleteBatchAsync(node.Id);
            else
                await productionService.DeleteItemAsync(node.Id);
        }
        else
        {
            var distributionService = scope.ServiceProvider.GetRequiredService<IDistributionService>();
            if (node.IsDistribution)
            {
                await distributionService.DeleteDistributionAsync(node.Id);
            }
            else
            {
                var distributionClient = await distributionService.GetClientByIdAsync(node.Id);
                if (distributionClient is not null)
                    WeakReferenceMessenger.Default.Send(new ClientPurchasesChangedMessage(distributionClient.Client.Id));

                await distributionService.DeleteClientAsync(node.Id);
            }
        }

        await SearchAsync();
    }

    private async Task EditProductionItemAsync(ProductionNode node)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var productionService = scope.ServiceProvider.GetRequiredService<IProductionService>();
        var item = await productionService.GetItemByIdAsync(node.Id);

        if (item is null)
        {
            logger.LogWarning("Позиция производства с id {ItemId} не найдена для редактирования", node.Id);
            return;
        }

        await RefreshProductTypesAsync();

        var dialog = scope.ServiceProvider.GetRequiredService<Dialogs.ProductionPositionEditDialog>();
        dialog.Initialize(_productTypes, item.Product!.ProductType, item.Product, (int)item.Quantity);

        var result = await dialog.ShowDialog<ProductionItemEditDto?>(this.GetOwnerWindow());
        if (result is null)
            return;

        result = result with { Id = item.Id };
        await productionService.UpdateItemAsync(result);
        await SearchAsync();
    }

    private async Task EditBatchAsync(ProductionNode node)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var productionService = scope.ServiceProvider.GetRequiredService<IProductionService>();
        var batch = await productionService.GetBatchByIdAsync(node.Id);

        if (batch is null)
        {
            logger.LogWarning("Партия производства с id {BatchId} не найдена для редактирования", node.Id);
            return;
        }

        await RefreshProductTypesAsync();

        var dialog = scope.ServiceProvider.GetRequiredService<Dialogs.ProductionEditDialog>();
        dialog.Initialize(_productTypes, batch);
        var accepted = await dialog.ShowDialog<bool?>(this.GetOwnerWindow());
        if (accepted == true)
            await SearchAsync();
    }

    private async Task EditDistributionClientAsync(ProductionNode node)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var distributionService = scope.ServiceProvider.GetRequiredService<IDistributionService>();
        var clientService = scope.ServiceProvider.GetRequiredService<IClientService>();

        var distributionClient = await distributionService.GetClientByIdAsync(node.Id);
        if (distributionClient is null)
        {
            logger.LogWarning("Клиент развоза с id {DistributionClientId} не найден для редактирования", node.Id);
            return;
        }

        var clients = await clientService.GetAllAsync();
        var dialog = scope.ServiceProvider.GetRequiredService<Dialogs.DistributionEditDialog>();
        dialog.Initialize(clients, distributionClient.Distribution.Id, distributionClient);

        var result = await dialog.ShowDialog<DistributionClientEditDto?>(this.GetOwnerWindow());
        if (result is null)
            return;

        await distributionService.UpdateClientAsync(node.Id, result.ClientId, result.TotalAmount, result.Items);
        WeakReferenceMessenger.Default.Send(new ClientPurchasesChangedMessage(result.ClientId));
        await SearchAsync();
    }

    private async Task RefreshProductTypesAsync(CancellationToken cancellationToken = default)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var productTypeService = scope.ServiceProvider.GetRequiredService<IProductTypeService>();
        var types = await productTypeService.GetAllAsync(cancellationToken);

        await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
        {
            _productTypes.Clear();
            foreach (var type in types)
                _productTypes.Add(type);
        });
    }

    private static ProductionNode CreateBatchNode(ProductionBatch batch)
    {
        var node = new ProductionNode
        {
            Id = batch.Id,
            Name = $"Производство {batch.StartDate:yyyy-MM-dd}",
            Kind = ProductionNodeKind.Batch
        };

        foreach (var item in batch.Items)
            node.Children.Add(CreateItemNode(item));

        return node;
    }

    private static ProductionNode CreateItemNode(ProductionBatchItem item)
    {
        var node = new ProductionNode
        {
            Id = item.Id,
            Name = item.Product!.Name,
            Kind = ProductionNodeKind.Item
        };
        node.SetQuantity((int)item.Quantity);
        return node;
    }

    private static ProductionNode CreateDistributionNode(Distribution distribution)
    {
        var node = new ProductionNode
        {
            Id = distribution.Id,
            Name = $"Развоз {distribution.Date:yyyy-MM-dd}",
            Kind = ProductionNodeKind.Distribution
        };

        foreach (var client in distribution.Clients)
            node.Children.Add(CreateDistributionClientNode(client));

        return node;
    }

    private static ProductionNode CreateDistributionClientNode(DistributionClient client)
    {
        var node = new ProductionNode
        {
            Id = client.Id,
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
}
