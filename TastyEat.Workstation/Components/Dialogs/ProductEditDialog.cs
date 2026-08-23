using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Markup.Declarative;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TastyEat.Workstation.Models.Dto;
using TastyEat.Workstation.Models.Tables;
using TastyEat.Workstation.Options;
using TastyEat.Workstation.Services.Interfaces;
using TastyEat.Workstation.Ui;
using TastyEat.Workstation.Views.Utils;

namespace TastyEat.Workstation.Components.Dialogs;

public sealed record ProductEditResult(Product Product, bool IsNew);

public sealed partial class ProductEditDialog : Window
{
    public sealed partial class State(StringLengthOptions stringLengthOptions) : ObservableObject
    {
        [ObservableProperty]
        public partial string Name { get; set; } = string.Empty;

        [ObservableProperty]
        public partial ProductType? SelectedProductType { get; set; }

        [ObservableProperty]
        public partial decimal? Price { get; set; }

        [ObservableProperty]
        public partial string WindowTitle { get; set; } = "Добавить продукт";

        public ObservableCollection<ProductType> ProductTypes { get; } = [];

        public bool IsNew => Id == 0;

        public int Id { get; set; }

        public string? NameError =>
            string.IsNullOrWhiteSpace(Name)
                ? "Название продукта обязательно"
                : Name.Trim().Length > stringLengthOptions.ProductNameMaxLength
                    ? $"Название продукта не должно превышать {stringLengthOptions.ProductNameMaxLength} символов"
                    : null;

        public string? TypeError => SelectedProductType is null ? "Необходимо выбрать тип продукта" : null;

        public string? PriceError => Price is null || Price <= 0 ? "Цена обязательна и должна быть положительным числом" : null;

        public bool CanSave => NameError is null && TypeError is null && PriceError is null;

        partial void OnNameChanged(string value) => RaiseValidation();

        partial void OnSelectedProductTypeChanged(ProductType? value) => RaiseValidation();

        partial void OnPriceChanged(decimal? value) => RaiseValidation();

        private void RaiseValidation()
        {
            OnPropertyChanged(nameof(NameError));
            OnPropertyChanged(nameof(TypeError));
            OnPropertyChanged(nameof(PriceError));
            OnPropertyChanged(nameof(CanSave));
        }
    }

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly State _state;

    public ProductEditResult? Result { get; private set; }

    public ProductEditDialog(
        IServiceScopeFactory scopeFactory,
        IOptions<StringLengthOptions> stringLengthOptions)
    {
        _scopeFactory = scopeFactory;
        _state = new State(stringLengthOptions.Value);

        Width = 440;
        Height = 400;
        CanResize = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Title = _state.WindowTitle;
        _state.PropertyChanged += (_, eventArgs) =>
        {
            if (eventArgs.PropertyName == nameof(State.WindowTitle))
                Title = _state.WindowTitle;
        };

        var typeSelector = new ComboBox { PlaceholderText = "Выберите тип" }
            .ItemsSource(_state, x => x.ProductTypes)
            .SelectedItem(_state, x => x.SelectedProductType, Avalonia.Data.BindingMode.TwoWay);
        typeSelector.ItemTemplate = new FuncDataTemplate<ProductType>((type, _) => new TextBlock { Text = type?.Name ?? string.Empty });

        Content = new Grid { Margin = new Thickness(24) }.Rows("Auto, *, Auto")
            .Children(
                new TextBlock { FontSize = 20, FontWeight = Avalonia.Media.FontWeight.Bold }
                    .Text(_state, x => x.WindowTitle),
                new StackPanel { Margin = new Thickness(0, 20, 0, 0), Spacing = 16 }.Grid_Row(1)
                    .Children(
                        UiFactory.DialogField("Название",
                            new TextBox { PlaceholderText = "Название продукта" }
                                .Text(_state, x => x.Name, Avalonia.Data.BindingMode.TwoWay),
                            UiFactory.ErrorText(_state, x => x.NameError)),
                        UiFactory.DialogField("Тип", typeSelector, UiFactory.ErrorText(_state, x => x.TypeError)),
                        UiFactory.DialogField("Цена",
                            new NumericUpDown { Minimum = 1, Maximum = 1_000_000, Increment = 1 }
                                .Value(_state, x => x.Price, Avalonia.Data.BindingMode.TwoWay),
                            UiFactory.ErrorText(_state, x => x.PriceError))),
                new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12, HorizontalAlignment = HorizontalAlignment.Right }.Grid_Row(2)
                    .Children(
                        new Button { Content = "Сохранить", IsDefault = true }
                            .IsEnabled(_state, x => x.CanSave)
                            .OnClick(async _ => await SaveAsync()),
                        new Button { Content = "Отмена", IsCancel = true }
                            .OnClick(_ => Close(null))));
    }

    public void Initialize(IReadOnlyList<ProductType> productTypes, Product? product = null)
    {
        _state.ProductTypes.Clear();
        foreach (var type in productTypes)
            _state.ProductTypes.Add(type);

        if (product is null)
        {
            _state.Id = 0;
            _state.WindowTitle = "Добавить продукт";
            _state.Name = string.Empty;
            _state.SelectedProductType = productTypes.FirstOrDefault();
            _state.Price = null;
            return;
        }

        _state.Id = product.Id;
        _state.WindowTitle = "Изменить продукт";
        _state.Name = product.Name;
        _state.SelectedProductType = productTypes.FirstOrDefault(t => t.Id == product.ProductType.Id);
        _state.Price = product.Prices
            .Where(p => p.EffectiveTo == null)
            .OrderByDescending(p => p.EffectiveFrom)
            .FirstOrDefault()?.Price;
    }

    private async Task SaveAsync()
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var productService = scope.ServiceProvider.GetRequiredService<IProductService>();

            var dto = new ProductEditDto
            {
                Id = _state.Id,
                Name = _state.Name.Trim(),
                ProductTypeId = _state.SelectedProductType?.Id ?? throw new InvalidOperationException("Тип продукта не выбран"),
                Price = (int)(_state.Price ?? throw new InvalidOperationException("Цена не указана"))
            };

            var product = _state.IsNew
                ? await productService.CreateAsync(dto)
                : await productService.UpdateAsync(dto);

            Result = new ProductEditResult(product, _state.IsNew);
            Close(Result);
        }
        catch (Exception exception)
        {
            await MessageDialog.ShowInfoAsync(this.GetOwnerWindow(), $"Не удалось сохранить продукт:\n{exception.Message}");
        }
    }

}
