using System.Collections.ObjectModel;
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
    private DateTimeOffset _date;

    [Reactive]
    private IReadOnlyList<ProductType> _productTypes = [];

    public ProductionEditViewModel(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
        Date = DateTimeOffset.Now;
        Rows = new ObservableCollection<ProductionItemEditViewModel>();

        _canExecute = this.WhenAnyValue(vm => vm.Rows)
            .Select(_ => Rows)
            .Select(rows => rows.Any())
            .ObserveOn(RxApp.MainThreadScheduler);
    }

    public ObservableCollection<ProductionItemEditViewModel> Rows { get; }

    public void Initialize(IReadOnlyList<ProductType> productTypes)
    {
        ProductTypes = productTypes;
        Rows.Clear();
        AddRow();
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

        await productionService.CreateAsync(dto);
        return true;
    }
}
