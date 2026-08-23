using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Markup.Declarative;
using CommunityToolkit.Mvvm.ComponentModel;
using TastyEat.Workstation.Models.Dto;
using TastyEat.Workstation.Models.Tables;
using TastyEat.Workstation.Ui;

namespace TastyEat.Workstation.Components.Dialogs;

public sealed partial class ProductionPositionEditDialog : Window
{
    public sealed partial class State : ObservableObject
    {
        [ObservableProperty]
        public partial ProductType? SelectedProductType { get; set; }

        [ObservableProperty]
        public partial Product? SelectedProduct { get; set; }

        [ObservableProperty]
        public partial decimal? Quantity { get; set; }

        public ObservableCollection<ProductType> ProductTypes { get; } = [];

        public ObservableCollection<Product> Products { get; } = [];

        public string? TypeError => SelectedProductType is null ? "Выберите категорию" : null;

        public string? ProductError => SelectedProduct is null ? "Выберите продукт" : null;

        public string? QuantityError => Quantity is null || Quantity <= 0 ? "Количество должно быть больше нуля" : null;

        public bool CanSave => TypeError is null && ProductError is null && QuantityError is null;

        partial void OnSelectedProductTypeChanged(ProductType? value)
        {
            Products.Clear();
            if (value is not null)
                foreach (var product in value.Products.OrderBy(p => p.Name))
                    Products.Add(product);

            SelectedProduct = null;
            RaiseValidation();
        }

        partial void OnSelectedProductChanged(Product? value) => RaiseValidation();

        partial void OnQuantityChanged(decimal? value) => RaiseValidation();

        private void RaiseValidation()
        {
            OnPropertyChanged(nameof(TypeError));
            OnPropertyChanged(nameof(ProductError));
            OnPropertyChanged(nameof(QuantityError));
            OnPropertyChanged(nameof(CanSave));
        }
    }

    private readonly State _state = new();

    public ProductionPositionEditDialog()
    {
        Width = 440;
        SizeToContent = SizeToContent.Height;
        CanResize = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Title = "Изменить позицию производства";

        var typeSelector = new ComboBox { PlaceholderText = "Выберите категорию" }
            .ItemsSource(_state, x => x.ProductTypes)
            .SelectedItem(_state, x => x.SelectedProductType, Avalonia.Data.BindingMode.TwoWay);
        typeSelector.ItemTemplate = new FuncDataTemplate<ProductType>((type, _) => new TextBlock { Text = type?.Name ?? string.Empty });

        var productSelector = new ComboBox { PlaceholderText = "Выберите продукт" }
            .ItemsSource(_state, x => x.Products)
            .SelectedItem(_state, x => x.SelectedProduct, Avalonia.Data.BindingMode.TwoWay);
        productSelector.ItemTemplate = new FuncDataTemplate<Product>((product, _) => new TextBlock { Text = product?.Name ?? string.Empty });

        Content = new Grid { Margin = new Thickness(24) }.Rows("Auto, *, Auto")
            .Children(
                new TextBlock { Text = "Позиция производства", FontSize = 20, FontWeight = Avalonia.Media.FontWeight.Bold },
                new StackPanel { Margin = new Thickness(0, 20, 0, 0), Spacing = 16 }.Grid_Row(1)
                    .Children(
                        UiFactory.DialogField("Категория", typeSelector, UiFactory.ErrorText(_state, x => x.TypeError)),
                        UiFactory.DialogField("Продукт", productSelector, UiFactory.ErrorText(_state, x => x.ProductError)),
                        UiFactory.DialogField("Количество",
                            new NumericUpDown { Minimum = 1, Maximum = 1_000_000, Increment = 1 }
                                .Value(_state, x => x.Quantity, Avalonia.Data.BindingMode.TwoWay),
                            UiFactory.ErrorText(_state, x => x.QuantityError))),
                new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12, HorizontalAlignment = HorizontalAlignment.Right }.Grid_Row(2)
                    .Children(
                        new Button { Content = "Сохранить", IsDefault = true }
                            .IsEnabled(_state, x => x.CanSave)
                            .OnClick(_ => Close(BuildResult())),
                        new Button { Content = "Отмена", IsCancel = true }
                            .OnClick(_ => Close(null))));
    }

    public void Initialize(IReadOnlyList<ProductType> productTypes, ProductType? selectedType = null, Product? selectedProduct = null, int? quantity = null)
    {
        _state.ProductTypes.Clear();
        foreach (var type in productTypes)
            _state.ProductTypes.Add(type);

        _state.SelectedProductType = selectedType ?? productTypes.FirstOrDefault();
        _state.SelectedProduct = selectedProduct;
        _state.Quantity = quantity;
    }

    private ProductionItemEditDto? BuildResult() =>
        _state.SelectedProduct is null || _state.Quantity is null || _state.Quantity <= 0
            ? null
            : new ProductionItemEditDto
            {
                ProductId = _state.SelectedProduct.Id,
                Quantity = (int)_state.Quantity
            };
}
