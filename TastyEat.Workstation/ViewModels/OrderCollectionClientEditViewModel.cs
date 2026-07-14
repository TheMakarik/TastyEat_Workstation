using System.Collections.ObjectModel;
using System.Reactive.Linq;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using ReactiveUI.Validation.Extensions;
using TastyEat.Workstation.Models.Dto;
using TastyEat.Workstation.Models.Tables;
using TastyEat.Workstation.Services.Interfaces;

namespace TastyEat.Workstation.ViewModels;

public sealed partial class OrderCollectionClientEditViewModel : ValidatableViewModelBase
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IObservable<bool> _canExecute;
    private readonly List<OrderCollectionProductGroupViewModel> _allGroups = [];

    private const string ShowOnlyProducedProductsKey = "ShowOnlyProducedProducts";

    [Reactive]
    private int _id;

    [Reactive]
    private int _collectionId;

    [Reactive]
    private Client? _selectedClient;

    [Reactive]
    private string _clientSearchText = string.Empty;

    [Reactive]
    private bool _showOnlyProducedProducts;

    [Reactive]
    private string _title = "Добавить клиента в сбор";

    [Reactive]
    private IReadOnlyList<Client> _clients = [];

    public OrderCollectionClientEditViewModel(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
        Groups = new ObservableCollection<OrderCollectionProductGroupViewModel>();

        this.ValidationRule(
            vm => vm.SelectedClient,
            client => client is not null,
            "Необходимо выбрать клиента");

        _canExecute = this.IsValid().ObserveOn(RxApp.MainThreadScheduler);

        this.WhenAnyValue(vm => vm.ShowOnlyProducedProducts)
            .Skip(1)
            .Subscribe(async _ =>
            {
                ApplyFilter();
                await SaveSettingAsync();
            });
    }

    public ObservableCollection<OrderCollectionProductGroupViewModel> Groups { get; }

    public bool IsNew => Id == 0;

    public async Task InitializeAsync(
        int collectionId,
        IReadOnlyList<Client> clients,
        IReadOnlyList<ProductType> productTypes,
        OrderCollectionClient? existing = null)
    {
        CollectionId = collectionId;
        Clients = clients;
        Id = existing?.Id ?? 0;
        Title = existing is null ? "Добавить клиента в сбор" : $"Изменить заказ: {existing.Client.FullName}";
        SelectedClient = existing?.Client;
        ClientSearchText = existing?.Client.FullName ?? string.Empty;

        await using var scope = _scopeFactory.CreateAsyncScope();
        var orderService = scope.ServiceProvider.GetRequiredService<IOrderCollectionService>();
        var settingsService = scope.ServiceProvider.GetRequiredService<IApplicationSettingsService>();

        ShowOnlyProducedProducts = await settingsService.GetBoolAsync(ShowOnlyProducedProductsKey);

        _allGroups.Clear();
        Groups.Clear();

        foreach (var type in productTypes.OrderBy(t => t.Name))
        {
            var group = new OrderCollectionProductGroupViewModel
            {
                ProductType = type
            };

            foreach (var product in type.Products.OrderBy(p => p.Name))
            {
                var existingItem = existing?.Items.FirstOrDefault(i => i.Product.Id == product.Id);
                var orderedQuantity = existingItem?.Quantity ?? 0;
                var available = await orderService.GetAvailableStockAsync(product.Id, existing?.Id, default);
                var produced = await orderService.GetProducedQuantityAsync(product.Id, default);

                var row = new OrderCollectionProductRowViewModel
                {
                    Product = product,
                    AvailableStock = available + orderedQuantity,
                    ProducedQuantity = produced,
                    OrderedQuantity = orderedQuantity
                };

                group.Rows.Add(row);
            }

            if (group.Rows.Count > 0)
            {
                _allGroups.Add(group);
                Groups.Add(group);
            }
        }

        ApplyFilter();
    }

    [ReactiveCommand(CanExecute = nameof(_canExecute), OutputScheduler = "ReactiveUI.RxApp.MainThreadScheduler")]
    private async Task<OrderCollectionClientEditDto?> SaveAsync()
    {
        var isValid = await this.IsValid().FirstAsync();
        if (!isValid)
            return null;

        var items = _allGroups
            .SelectMany(g => g.Rows)
            .Where(r => r.OrderedQuantity > 0)
            .Select(r => new OrderCollectionItemEditDto
            {
                ProductId = r.Product.Id,
                Quantity = r.OrderedQuantity
            })
            .ToList();

        return new OrderCollectionClientEditDto
        {
            Id = Id,
            ClientId = SelectedClient!.Id,
            Items = items
        };
    }

    private void ApplyFilter()
    {
        Groups.Clear();

        foreach (var group in _allGroups)
        {
            foreach (var row in group.Rows)
            {
                row.IsVisible = !ShowOnlyProducedProducts || row.ProducedQuantity > 0;
            }

            group.IsVisible = group.Rows.Any(r => r.IsVisible);

            if (group.IsVisible)
                Groups.Add(group);
        }
    }

    private async Task SaveSettingAsync()
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var settingsService = scope.ServiceProvider.GetRequiredService<IApplicationSettingsService>();
        await settingsService.SetBoolAsync(ShowOnlyProducedProductsKey, ShowOnlyProducedProducts);
    }
}
