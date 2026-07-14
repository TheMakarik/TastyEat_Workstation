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

public sealed partial class DistributionEditViewModel : ValidatableViewModelBase
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IObservable<bool> _canExecute;

    [Reactive]
    private int _id;

    [Reactive]
    private int _distributionId;

    [Reactive]
    private Client? _selectedClient;

    [Reactive]
    private string _clientSearchText = string.Empty;

    [Reactive]
    private int? _totalAmount;

    [Reactive]
    private string _title = "Добавить клиента в развоз";

    [Reactive]
    private IReadOnlyList<Client> _clients = [];

    public DistributionEditViewModel(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
        Groups = new ObservableCollection<OrderCollectionProductGroupViewModel>();

        this.ValidationRule(
            vm => vm.SelectedClient,
            client => client is not null,
            "Необходимо выбрать клиента");

        this.ValidationRule(
            vm => vm.TotalAmount,
            total => total.HasValue && total.Value >= 0,
            "Сумма должна быть неотрицательным числом");

        _canExecute = this.IsValid().ObserveOn(RxApp.MainThreadScheduler);

        this.WhenAnyValue(vm => vm.SelectedClient)
            .Subscribe(async _ => await LoadProductsAsync());
    }

    public bool IsNew => Id == 0;

    public ObservableCollection<OrderCollectionProductGroupViewModel> Groups { get; }

    public void Initialize(IReadOnlyList<Client> clients, int distributionId, DistributionClient? existing = null)
    {
        Clients = clients;
        DistributionId = distributionId;
        Id = existing?.Id ?? 0;
        Title = existing is null ? "Добавить клиента в развоз" : $"Изменить клиента в развозе: {existing.Client.FullName}";
        SelectedClient = existing?.Client;
        ClientSearchText = existing?.Client.FullName ?? string.Empty;
        TotalAmount = existing?.TotalAmount ?? 0;

        _existingItems = existing?.Items.ToDictionary(i => i.Product.Id, i => i.Quantity) ?? [];
    }

    private Dictionary<int, int> _existingItems = [];

    private async Task LoadProductsAsync()
    {
        Groups.Clear();

        if (SelectedClient is null)
            return;

        await using var scope = _scopeFactory.CreateAsyncScope();
        var distributionService = scope.ServiceProvider.GetRequiredService<IDistributionService>();
        var productTypeService = scope.ServiceProvider.GetRequiredService<IProductTypeService>();

        var products = await distributionService.GetClientOrderedProductsAsync(SelectedClient.Id);
        var productTypes = await productTypeService.GetAllAsync();

        foreach (var type in productTypes.OrderBy(t => t.Name))
        {
            var typeProducts = products.Where(p => p.Product.ProductType.Id == type.Id).OrderBy(p => p.Product.Name).ToList();
            if (typeProducts.Count == 0)
                continue;

            var group = new OrderCollectionProductGroupViewModel
            {
                ProductType = type
            };

            foreach (var ordered in typeProducts)
            {
                var product = ordered.Product;
                var quantity = _existingItems.GetValueOrDefault(product.Id);
                var remaining = await distributionService.GetRemainingQuantityAsync(product.Id, Id, default);

                group.Rows.Add(new OrderCollectionProductRowViewModel
                {
                    Product = product,
                    AvailableStock = remaining + quantity,
                    OrderedQuantity = quantity,
                    OrderedFromCollections = ordered.OrderedQuantity
                });
            }

            Groups.Add(group);
        }
    }

    [ReactiveCommand(CanExecute = nameof(_canExecute), OutputScheduler = "ReactiveUI.RxApp.MainThreadScheduler")]
    private async Task<DistributionClientEditDto?> SaveAsync()
    {
        var isValid = await this.IsValid().FirstAsync();
        if (!isValid)
            return null;

        var items = Groups
            .SelectMany(g => g.Rows)
            .Where(r => r.OrderedQuantity > 0)
            .Select(r => new DistributionItemEditDto
            {
                ProductId = r.Product.Id,
                Quantity = r.OrderedQuantity
            })
            .ToList();

        return new DistributionClientEditDto
        {
            Id = Id,
            DistributionId = DistributionId,
            ClientId = SelectedClient!.Id,
            TotalAmount = TotalAmount ?? 0,
            Items = items
        };
    }
}
