using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Reactive;
using System.Reactive.Linq;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using ReactiveUI.Validation.Extensions;
using TastyEat.Workstation.Models.Dto;
using TastyEat.Workstation.Models.Tables;
using TastyEat.Workstation.Services.Interfaces;

namespace TastyEat.Workstation.ViewModels;

public sealed partial class ProductionEditViewModel : ValidatableViewModelBase
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IObservable<bool> _canExecute;

    [Reactive]
    private int _id;

    [Reactive]
    private DateTimeOffset _date;

    [Reactive]
    private string _title = "Добавить производство";

    [Reactive]
    private IReadOnlyList<ProductType> _productTypes = [];

    public ProductionEditViewModel(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
        Date = DateTimeOffset.Now;
        Rows = new ObservableCollection<ProductionItemEditViewModel>();

        _canExecute = Observable
            .FromEventPattern<NotifyCollectionChangedEventHandler, NotifyCollectionChangedEventArgs>(
                h => Rows.CollectionChanged += h,
                h => Rows.CollectionChanged -= h)
            .Select(_ => Rows.Any())
            .StartWith(Rows.Any())
            .ObserveOn(RxApp.MainThreadScheduler);
    }

    public bool IsNew => Id == 0;

    public ObservableCollection<ProductionItemEditViewModel> Rows { get; }

    public void Initialize(IReadOnlyList<ProductType> productTypes, ProductionBatch? batch = null)
    {
        ProductTypes = productTypes;
        Rows.Clear();

        if (batch is null)
        {
            Id = 0;
            Title = "Добавить производство";
            Date = DateTimeOffset.Now;
            AddRow();
            return;
        }

        Id = batch.Id;
        Title = $"Изменить производство {batch.StartDate:yyyy-MM-dd}";
        Date = batch.StartDate;

        foreach (var item in batch.Items)
        {
            var row = new ProductionItemEditViewModel();
            var productType = productTypes.FirstOrDefault(t => t.Id == item.Product!.ProductType.Id);
            row.Initialize(productTypes, productType, item.Product, (int)item.Quantity);
            Rows.Add(row);
        }
    }

    [ReactiveCommand(OutputScheduler = "ReactiveUI.RxApp.MainThreadScheduler")]
    private void AddRow()
    {
        var row = new ProductionItemEditViewModel();
        row.Initialize(ProductTypes);
        Rows.Add(row);
    }

    [ReactiveCommand(OutputScheduler = "ReactiveUI.RxApp.MainThreadScheduler")]
    private void RemoveRow(ProductionItemEditViewModel row)
    {
        Rows.Remove(row);
    }

    [ReactiveCommand(CanExecute = nameof(_canExecute), OutputScheduler = "ReactiveUI.RxApp.MainThreadScheduler")]
    private async Task<bool> SaveAsync()
    {
        if (Rows.Any(r => !r.IsValid().FirstAsync().Wait()))
            return false;

        await using var scope = _scopeFactory.CreateAsyncScope();
        var productionService = scope.ServiceProvider.GetRequiredService<IProductionService>();

        var dto = new ProductionEditDto
        {
            Date = Date.Date,
            Items = Rows.Select(r => r.ToDto()).ToList()
        };

        if (IsNew)
        {
            await productionService.CreateAsync(dto);
        }
        else
        {
            await productionService.UpdateBatchAsync(Id, dto);
        }

        return true;
    }
}
