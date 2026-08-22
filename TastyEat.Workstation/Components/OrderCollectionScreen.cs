using System.Collections.ObjectModel;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Threading;
using Avalonia.Markup.Declarative;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Material.Icons;
using Material.Icons.Avalonia;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TastyEat.Workstation.Models.Dto;
using TastyEat.Workstation.Models.Tables;
using TastyEat.Workstation.Services.Interfaces;
using TastyEat.Workstation.Ui;
using TastyEat.Workstation.Views.Utils;

namespace TastyEat.Workstation.Components;

public sealed partial class OrderCollectionScreen(
    IServiceScopeFactory scopeFactory,
    ILogger<OrderCollectionScreen> logger) : ScreenComponent<OrderCollectionScreen.State>(new State())
{
    public sealed partial class State : ObservableObject
    {
        [ObservableProperty]
        public partial string SearchText { get; set; } = string.Empty;

        [ObservableProperty]
        public partial bool IsLoading { get; set; } = true;
    }

    public enum OrderCollectionNodeKind
    {
        Collection,
        Client
    }

    public sealed partial class OrderCollectionNode : ObservableObject
    {
        public int Id { get; init; }
        public int CollectionId { get; init; }
        public string Name { get; init; } = string.Empty;
        public OrderCollectionNodeKind Kind { get; init; }
        public string StatusText { get; init; } = string.Empty;
        public bool IsCompleted { get; init; }
        public ObservableCollection<OrderCollectionNode> Children { get; } = [];

        public bool IsCollection => Kind == OrderCollectionNodeKind.Collection;
        public bool IsClient => Kind == OrderCollectionNodeKind.Client;
    }

    public override string Title => "Сбор заказов";
    public override MaterialIconKind Icon => MaterialIconKind.CartOutline;

    private readonly ObservableCollection<OrderCollectionNode> _nodes = [];
    private readonly DispatcherTimer _searchDebounce = new() { Interval = TimeSpan.FromMilliseconds(400) };
    private CancellationTokenSource? _loadCts;
    private HierarchicalTreeDataGridSource<OrderCollectionNode>? _orderCollectionsSource;

    private HierarchicalTreeDataGridSource<OrderCollectionNode> BuildOrderCollectionsSource() => new(_nodes)
    {
        Columns =
        {
            new HierarchicalExpanderColumn<OrderCollectionNode>(
                new TextColumn<OrderCollectionNode, string>("Название", x => x.Name, new GridLength(2, GridUnitType.Star)),
                x => x.Children),
            new TextColumn<OrderCollectionNode, string>("Статус", x => x.StatusText, new GridLength(1, GridUnitType.Star)),
            new TemplateColumn<OrderCollectionNode>(string.Empty, new FuncDataTemplate<OrderCollectionNode>((node, _) =>
                new Button
                {
                    Classes = { "action" },
                    Content = new MaterialIcon { Kind = MaterialIconKind.Menu }
                }.OnClick(eventArgs => ShowNodeActions(eventArgs.Source as Button, node))), width: GridLength.Auto)
        }
    };

    protected override object Build(State state)
    {
        _orderCollectionsSource ??= BuildOrderCollectionsSource();
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
                UiFactory.ActionButton(MaterialIconKind.PlayCircleOutline, "Начать сбор", () => _ = StartCollectionAsync()),
                UiFactory.ActionButton(MaterialIconKind.AccountPlus, "Добавить клиента", () => _ = AddClientAsync(), "sidebarAction"));

        Avalonia.Threading.Dispatcher.UIThread.Post(async () => await SearchAsync());

        return new Grid().Rows("Auto, Auto, *").Classes("managementLayout")
            .Children(
                UiFactory.Header(MaterialIconKind.CartOutline, "Сбор заказов", "Сбор заказов от клиентов"),
                new Grid().Cols("*, Auto").Classes("topbar").Grid_Row(1)
                    .Children(searchBox, buttons.Grid_Column(1)),
                new Border().Classes("dataGridHost").Grid_Row(2)
                    .Child(new TreeDataGrid { Source = _orderCollectionsSource }),
                UiFactory.LoadingOverlay(state, x => x.IsLoading).Grid_RowSpan(3)
            );
    }

    private void ShowNodeActions(Button? anchor, OrderCollectionNode? node)
    {
        if (anchor is null || node is null)
            return;

        var flyout = new MenuFlyout();

        if (node.IsClient)
        {
            flyout.Items.Add(new MenuItem
            {
                Header = "Изменить",
                Icon = new MaterialIcon { Kind = MaterialIconKind.Pencil },
                Command = EditNodeCommand,
                CommandParameter = node
            });
        }

        if (node.IsCollection)
        {
            flyout.Items.Add(new MenuItem
            {
                Header = "Завершить сбор",
                Icon = new MaterialIcon { Kind = MaterialIconKind.CheckCircle },
                Command = CloseCollectionCommand,
                CommandParameter = node,
                IsEnabled = !node.IsCompleted
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
            var searchText = ScreenState.SearchText;
            var filtered = await Task.Run(async () =>
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var orderService = scope.ServiceProvider.GetRequiredService<IOrderCollectionService>();
                var collections = await orderService.GetAllAsync(token);
                return FilterCollections(collections, searchText);
            }, token);

            if (token.IsCancellationRequested)
                return;

            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                _nodes.Clear();
                foreach (var collection in filtered)
                    _nodes.Add(CreateCollectionNode(collection));
            });
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Не удалось загрузить сборы заказов");
        }
        finally
        {
            if (!token.IsCancellationRequested)
                ScreenState.IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task StartCollectionAsync()
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var orderService = scope.ServiceProvider.GetRequiredService<IOrderCollectionService>();
        var active = await orderService.GetActiveAsync();

        if (active is not null)
        {
            var choice = await MessageDialog.ChoiceAsync(
                this.GetOwnerWindow(),
                $"Уже есть незавершённый сбор ({active.StartDate:yyyy-MM-dd}). Закрыть его и начать новый?",
                "Незавершённый сбор",
                "Закрыть и начать новый",
                "Отмена");

            if (choice != true)
                return;

            await orderService.CloseAsync(active.Id);
        }

        await orderService.CreateAsync();
        await SearchAsync();
    }

    [RelayCommand]
    private async Task AddClientAsync()
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var orderService = scope.ServiceProvider.GetRequiredService<IOrderCollectionService>();
        var productTypeService = scope.ServiceProvider.GetRequiredService<IProductTypeService>();
        var clientService = scope.ServiceProvider.GetRequiredService<IClientService>();

        var active = await orderService.GetActiveAsync();
        if (active is null)
        {
            await MessageDialog.ShowInfoAsync(this.GetOwnerWindow(), "Нет открытого сбора заказов. Сначала нажмите «Начать сбор».");
            return;
        }

        var productTypes = await productTypeService.GetAllAsync();
        var clients = await clientService.GetAllAsync();

        var dialog = scope.ServiceProvider.GetRequiredService<Dialogs.OrderCollectionClientEditDialog>();
        await dialog.InitializeAsync(active.Id, clients, productTypes);
        var result = await dialog.ShowDialog<OrderCollectionClientEditDto?>(this.GetOwnerWindow());
        if (result is null)
            return;

        await orderService.UpsertClientAsync(active.Id, result);
        await SearchAsync();
    }

    [RelayCommand]
    private async Task EditNodeAsync(OrderCollectionNode node)
    {
        if (!node.IsClient)
            return;

        await using var scope = scopeFactory.CreateAsyncScope();
        var orderService = scope.ServiceProvider.GetRequiredService<IOrderCollectionService>();
        var productTypeService = scope.ServiceProvider.GetRequiredService<IProductTypeService>();
        var clientService = scope.ServiceProvider.GetRequiredService<IClientService>();

        var clientEntry = await orderService.GetByIdAsync(node.CollectionId);
        if (clientEntry is null)
        {
            logger.LogWarning("Родительский сбор для записи клиента {ClientEntryId} не найден", node.Id);
            return;
        }

        var existing = clientEntry.Clients.FirstOrDefault(cc => cc.Id == node.Id);
        if (existing is null)
        {
            logger.LogWarning("Запись клиента {ClientEntryId} не найдена в сборе", node.Id);
            return;
        }

        var productTypes = await productTypeService.GetAllAsync();
        var clients = await clientService.GetAllAsync();

        var dialog = scope.ServiceProvider.GetRequiredService<Dialogs.OrderCollectionClientEditDialog>();
        await dialog.InitializeAsync(node.CollectionId, clients, productTypes, existing);
        var result = await dialog.ShowDialog<OrderCollectionClientEditDto?>(this.GetOwnerWindow());
        if (result is null)
            return;

        await orderService.UpsertClientAsync(node.CollectionId, result);
        await SearchAsync();
    }

    [RelayCommand]
    private async Task CloseCollectionAsync(OrderCollectionNode node)
    {
        if (!node.IsCollection || node.IsCompleted)
            return;

        await using var scope = scopeFactory.CreateAsyncScope();
        var orderService = scope.ServiceProvider.GetRequiredService<IOrderCollectionService>();
        await orderService.CloseAsync(node.Id);
        await SearchAsync();
    }

    [RelayCommand]
    private async Task DeleteNodeAsync(OrderCollectionNode node)
    {
        var entityName = node.IsCollection
            ? $"сбор \"{node.Name}\""
            : $"клиента \"{node.Name}\"";

        var confirmed = await DeleteConfirmationDialog.ShowAsync(this.GetOwnerWindow(), $"Удалить {entityName}?");
        if (!confirmed)
            return;

        await using var scope = scopeFactory.CreateAsyncScope();
        var orderService = scope.ServiceProvider.GetRequiredService<IOrderCollectionService>();

        if (node.IsCollection)
            await orderService.DeleteCollectionAsync(node.Id);
        else
            await orderService.DeleteClientAsync(node.Id);

        await SearchAsync();
    }

    private static OrderCollectionNode CreateCollectionNode(OrderCollection collection)
    {
        var endText = collection.EndDate.HasValue ? collection.EndDate.Value.ToString("yyyy-MM-dd") : "...";
        var node = new OrderCollectionNode
        {
            Id = collection.Id,
            Name = $"Сбор заказов {collection.StartDate:yyyy-MM-dd} - {endText}",
            Kind = OrderCollectionNodeKind.Collection,
            StatusText = collection.EndDate.HasValue ? "Завершён" : "Не завершён",
            IsCompleted = collection.EndDate.HasValue
        };

        foreach (var client in collection.Clients)
            node.Children.Add(CreateClientNode(client, collection.Id));

        return node;
    }

    private static OrderCollectionNode CreateClientNode(OrderCollectionClient client, int collectionId) =>
        new()
        {
            Id = client.Id,
            CollectionId = collectionId,
            Name = client.Client.FullName,
            Kind = OrderCollectionNodeKind.Client
        };

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

            filtered.Add(new OrderCollection
            {
                Id = collection.Id,
                StartDate = collection.StartDate,
                EndDate = collection.EndDate,
                Clients = matchingClients
            });
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
}
