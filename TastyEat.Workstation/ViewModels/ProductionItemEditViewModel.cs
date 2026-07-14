using System.Reactive.Linq;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using ReactiveUI.Validation.Extensions;
using TastyEat.Workstation.Models.Dto;
using TastyEat.Workstation.Models.Tables;

namespace TastyEat.Workstation.ViewModels;

public sealed partial class ProductionItemEditViewModel : ValidatableViewModelBase
{
    [Reactive]
    private ProductType? _selectedProductType;

    [Reactive]
    private Product? _selectedProduct;

    [Reactive]
    private int? _quantity;

    [Reactive]
    private IReadOnlyList<ProductType> _productTypes = [];

    [Reactive]
    private IReadOnlyList<Product> _products = [];

    [Reactive]
    private bool _isProductEnabled;

    public ProductionItemEditViewModel()
    {
        this.ValidationRule(
            vm => vm.SelectedProductType,
            type => type is not null,
            "Выберите категорию");

        this.ValidationRule(
            vm => vm.SelectedProduct,
            product => product is not null,
            "Выберите продукт");

        this.ValidationRule(
            vm => vm.Quantity,
            quantity => quantity.HasValue && quantity.Value > 0,
            "Количество должно быть больше нуля");

        this.WhenAnyValue(vm => vm.SelectedProductType)
            .Subscribe(type =>
            {
                if (type is null)
                {
                    Products = [];
                    SelectedProduct = null;
                    IsProductEnabled = false;
                    return;
                }

                Products = [.. type.Products.OrderBy(p => p.Name)];
                SelectedProduct = null;
                IsProductEnabled = true;
            });
    }

    [ReactiveCommand(OutputScheduler = "ReactiveUI.RxApp.MainThreadScheduler")]
    private async Task<ProductionItemEditDto?> SaveAsync()
    {
        var isValid = await this.IsValid().FirstAsync();
        if (!isValid)
            return null;

        return ToDto();
    }

    public void Initialize(IReadOnlyList<ProductType> productTypes, ProductType? selectedType = null, Product? selectedProduct = null, int? quantity = null)
    {
        ProductTypes = productTypes;
        SelectedProductType = selectedType;
        SelectedProduct = selectedProduct;
        Quantity = quantity;
    }

    public ProductionItemEditDto ToDto(int id = 0)
    {
        return new ProductionItemEditDto
        {
            Id = id,
            ProductId = SelectedProduct?.Id ?? throw new InvalidOperationException("Продукт не выбран"),
            Quantity = Quantity ?? throw new InvalidOperationException("Количество не указано")
        };
    }
}
