using System.Reactive.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using ReactiveUI.Validation.Extensions;
using TastyEat.Workstation.Models.Tables;
using TastyEat.Workstation.Options;
using TastyEat.Workstation.Services.Interfaces;

namespace TastyEat.Workstation.ViewModels;

public sealed partial class ProductTypeEditViewModel : ValidatableViewModelBase
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly StringLengthOptions _stringLengthOptions;
    private readonly IObservable<bool> _canExecute;

    [Reactive]
    private int _id;

    [Reactive]
    private string _name = string.Empty;

    [Reactive]
    private string _title = "Добавить тип продукта";

    public ProductTypeEditViewModel(IServiceScopeFactory scopeFactory, IOptions<StringLengthOptions> stringLengthOptions)
    {
        _scopeFactory = scopeFactory;
        _stringLengthOptions = stringLengthOptions.Value;

        this.ValidationRule(
            vm => vm.Name,
            name => !string.IsNullOrWhiteSpace(name),
            "Тип не должен быть пустым");

        this.ValidationRule(
            vm => vm.Name,
            name => string.IsNullOrWhiteSpace(name) || name.Length <= _stringLengthOptions.ProductTypeNameMaxLength,
            $"Название типа не должно превышать {_stringLengthOptions.ProductTypeNameMaxLength} символов");

        var nameUniqueObservable = this.WhenAnyValue(vm => vm.Name)
            .Throttle(TimeSpan.FromMilliseconds(500))
            .DistinctUntilChanged()
            .SelectMany(name => Observable.FromAsync(async () =>
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var productTypeService = scope.ServiceProvider.GetRequiredService<IProductTypeService>();
                var trimmed = name?.Trim() ?? string.Empty;
                if (string.IsNullOrEmpty(trimmed) || trimmed.Length > _stringLengthOptions.ProductTypeNameMaxLength)
                    return false;

                return await productTypeService.ExistsByNameAsync(trimmed, IsNew ? null : Id);
            }))
            .Select(exists => !exists)
            .ObserveOn(RxApp.MainThreadScheduler);

        this.ValidationRule(nameUniqueObservable, "Тип продукта с таким названием уже существует");

        _canExecute = this.IsValid().ObserveOn(RxApp.MainThreadScheduler);
    }

    public bool IsNew => Id == 0;

    public void Initialize(ProductType? productType = null)
    {
        if (productType is null)
        {
            Id = 0;
            Title = "Добавить тип продукта";
            Name = string.Empty;
            return;
        }

        Id = productType.Id;
        Title = "Изменить тип продукта";
        Name = productType.Name;
    }

    [ReactiveCommand(CanExecute = nameof(_canExecute), OutputScheduler = "ReactiveUI.RxApp.MainThreadScheduler")]
    private async Task<bool> SaveAsync()
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var productTypeService = scope.ServiceProvider.GetRequiredService<IProductTypeService>();

        if (IsNew)
        {
            await productTypeService.CreateAsync(Name.Trim());
            return true;
        }

        await productTypeService.UpdateAsync(new Models.Dto.ProductTypeEditDto { Id = Id, Name = Name.Trim() });
        return true;
    }
}
