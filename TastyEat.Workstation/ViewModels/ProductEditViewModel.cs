using System.Reactive;
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

public sealed partial class ProductEditViewModel : ValidatableViewModelBase
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly StringLengthOptions _stringLengthOptions;
    private readonly IObservable<bool> _canExecute;

    [Reactive]
    private int _id;

    [Reactive]
    private string _name = string.Empty;

    [Reactive]
    private ProductType? _selectedProductType;

    [Reactive]
    private int? _price;

    [Reactive]
    private bool _isWeighted;

    [Reactive]
    private IReadOnlyList<ProductType> _productTypes = [];

    [Reactive]
    private string _title = "Добавить продукт";

    public ProductEditViewModel(IServiceScopeFactory scopeFactory, IOptions<StringLengthOptions> stringLengthOptions)
    {
        _scopeFactory = scopeFactory;
        _stringLengthOptions = stringLengthOptions.Value;
        
        this.ValidationRule(
            vm => vm.Name,
            name => string.IsNullOrWhiteSpace(name) || name.Length <= _stringLengthOptions.ProductNameMaxLength,
            $"Название продукта не должно превышать {_stringLengthOptions.ProductNameMaxLength} символов");

        this.ValidationRule(
            vm => vm.Name,
            name => !string.IsNullOrWhiteSpace(name),
            "Название продукта обязательно");
        
        this.ValidationRule(
            vm => vm.SelectedProductType,
            type => type is not null,
            "Необходимо выбрать тип продукта");

        this.ValidationRule(
            vm => vm.Price,
            price => price.HasValue && price.Value > 0,
            "Цена обязательна и должна быть положительным числом");

        _canExecute = this.IsValid().ObserveOn(RxApp.MainThreadScheduler);
    }

    public bool IsNew => Id == 0;

    public void Initialize(IReadOnlyList<ProductType> productTypes, Product? product = null)
    {
        ProductTypes = productTypes;

        if (product is null)
        {
            Id = 0;
            Title = "Добавить продукт";
            Name = string.Empty;
            SelectedProductType = productTypes.FirstOrDefault();
            Price = null;
            IsWeighted = false;
            return;
        }

        Id = product.Id;
        Title = "Изменить продукт";
        Name = product.Name;
        SelectedProductType = productTypes.FirstOrDefault(t => t.Id == product.ProductType.Id);
        Price = product.Prices
            .Where(p => p.EffectiveTo == null)
            .OrderByDescending(p => p.EffectiveFrom)
            .FirstOrDefault()?.Price;
        IsWeighted = product.IsWeighted;
    }

    [ReactiveCommand(CanExecute = nameof(_canExecute), OutputScheduler = "ReactiveUI.RxApp.MainThreadScheduler")]
    private async Task<ProductEditResult?> SaveAsync()
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var productService = scope.ServiceProvider.GetRequiredService<IProductService>();

        var dto = new Models.Dto.ProductEditDto
        {
            Id = Id,
            Name = Name.Trim(),
            ProductTypeId = SelectedProductType?.Id ?? throw new InvalidOperationException("Тип продукта не выбран"),
            Price = Price ?? throw new InvalidOperationException("Цена не указана"),
            IsWeighted = IsWeighted
        };

        var product = IsNew
            ? await productService.CreateAsync(dto)
            : await productService.UpdateAsync(dto);

        return new ProductEditResult(product, IsNew);
    }
}

public sealed record ProductEditResult(Product Product, bool IsNew);
