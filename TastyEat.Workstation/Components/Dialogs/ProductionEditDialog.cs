using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Markup.Declarative;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using TastyEat.Workstation.Models.Dto;
using TastyEat.Workstation.Models.Tables;
using TastyEat.Workstation.Services.Interfaces;
using TastyEat.Workstation.Ui;
using TastyEat.Workstation.Views.Utils;

namespace TastyEat.Workstation.Components.Dialogs;

public sealed partial class ProductionEditDialog : Window
{
    public sealed partial class ItemRow : ObservableObject
    {
        [ObservableProperty]
        public partial ProductType? SelectedProductType { get; set; }

        [ObservableProperty]
        public partial Product? SelectedProduct { get; set; }

        [ObservableProperty]
        public partial decimal? Quantity { get; set; } = 1;

        public ObservableCollection<Product> Products { get; } = [];

        public string? TypeError => SelectedProductType is null ? "Выберите категорию" : null;

        public string? ProductError => SelectedProduct is null ? "Выберите продукт" : null;

        public string? QuantityError => Quantity is null || Quantity <= 0 ? "Количество должно быть больше нуля" : null;

        public bool IsValid => TypeError is null && ProductError is null && QuantityError is null;

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
            OnPropertyChanged(nameof(IsValid));
            RowChanged?.Invoke();
        }

        public event Action? RowChanged;
    }

    public sealed partial class State : ObservableObject
    {
        [ObservableProperty]
        public partial DateTimeOffset Date { get; set; } = DateTimeOffset.Now;

        [ObservableProperty]
        public partial string WindowTitle { get; set; } = "Добавить производство";

        public ObservableCollection<ItemRow> Rows { get; } = [];

        public bool CanSave => Rows.Count > 0 && Rows.All(r => r.IsValid);

        public int Id { get; set; }

        public bool IsNew => Id == 0;

        public void RaiseCanSave() => OnPropertyChanged(nameof(CanSave));
    }

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly State _state = new();

    public ProductionEditDialog(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
        Width = 560;
        Height = 560;
        CanResize = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Title = _state.WindowTitle;
        _state.PropertyChanged += (_, eventArgs) =>
        {
            if (eventArgs.PropertyName == nameof(State.WindowTitle))
                Title = _state.WindowTitle;
        };

        var datePicker = new DatePicker()
            .SelectedDate(_state, x => x.Date, Avalonia.Data.BindingMode.TwoWay);

        var rowsHost = new ItemsControl
        {
            ItemsSource = _state.Rows,
            ItemTemplate = new FuncDataTemplate<ItemRow>((row, _) => row is null ? null : BuildRowUi(row))
        };

        Content = new Grid { Margin = new Thickness(24) }.Rows("Auto, Auto, *, Auto, Auto")
            .Children(
                new TextBlock { FontSize = 20, FontWeight = Avalonia.Media.FontWeight.Bold }
                    .Text(_state, x => x.WindowTitle),
                UiFactory.DialogField("Дата производства", datePicker).Grid_Row(1).Margin(new Thickness(0, 16, 0, 0)),
                new ScrollViewer { Margin = new Thickness(0, 12, 0, 0), Content = rowsHost }.Grid_Row(2),
                new Button { Content = "+ Добавить позицию" }
                    .Classes("sidebarAction")
                    .Grid_Row(3)
                    .Margin(new Thickness(0, 12, 0, 0))
                    .OnClick(_ => AddRow()),
                new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12, HorizontalAlignment = HorizontalAlignment.Right }.Grid_Row(4)
                    .Margin(new Thickness(0, 16, 0, 0))
                    .Children(
                        new Button { Content = "Сохранить", IsDefault = true }
                            .IsEnabled(_state, x => x.CanSave)
                            .OnClick(async _ => await SaveAsync()),
                        new Button { Content = "Отмена", IsCancel = true }
                            .OnClick(_ => Close(null))));
    }

    private Control BuildRowUi(ItemRow row)
    {
        var typeSelector = new ComboBox { PlaceholderText = "Категория", ItemsSource = _productTypes }
            .SelectedItem(row, x => x.SelectedProductType, Avalonia.Data.BindingMode.TwoWay);
        typeSelector.ItemTemplate = new FuncDataTemplate<ProductType>((type, _) => new TextBlock { Text = type?.Name ?? string.Empty });

        var productSelector = new ComboBox { PlaceholderText = "Продукт" }
            .ItemsSource(row, x => x.Products)
            .SelectedItem(row, x => x.SelectedProduct, Avalonia.Data.BindingMode.TwoWay);
        productSelector.ItemTemplate = new FuncDataTemplate<Product>((product, _) => new TextBlock { Text = product?.Name ?? string.Empty });

        var quantityEditor = new NumericUpDown { Minimum = 1, Maximum = 1_000_000, Increment = 1, Width = 110 }
            .Value(row, x => x.Quantity, Avalonia.Data.BindingMode.TwoWay);

        return new Border
        {
            BorderBrush = Avalonia.Media.Brush.Parse("#E0E0E0"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(10),
            Margin = new Thickness(0, 0, 0, 8),
            Child = new Grid().Cols("*, *, Auto, Auto, Auto")
                .Children(
                    typeSelector,
                    productSelector.Grid_Column(1).Margin(8, 0, 0, 0),
                    quantityEditor.Grid_Column(2).Margin(8, 0, 0, 0),
                    new Button
                        {
                            Classes = { "action" },
                            Content = new Material.Icons.Avalonia.MaterialIcon { Kind = Material.Icons.MaterialIconKind.Close }
                        }
                        .Grid_Column(3)
                        .Margin(8, 0, 0, 0)
                        .OnClick(_ => RemoveRow(row)),
                    new TextBlock
                        {
                            Text = row.TypeError ?? row.ProductError ?? row.QuantityError ?? string.Empty,
                            FontSize = 12,
                            Foreground = Avalonia.Media.Brush.Parse("#C0392B"),
                            TextWrapping = TextWrapping.Wrap
                        }
                        .Grid_Column(4)
                        .Margin(8, 0, 0, 0)
                        .VerticalAlignment(VerticalAlignment.Center)
                        .Text(row, x => x.TypeError ?? x.ProductError ?? x.QuantityError ?? string.Empty))
        };
    }

    private readonly ObservableCollection<ProductType> _productTypes = [];

    public void Initialize(IReadOnlyList<ProductType> productTypes, ProductionBatch? batch = null)
    {
        _productTypes.Clear();
        foreach (var type in productTypes)
            _productTypes.Add(type);

        _state.Rows.Clear();

        if (batch is null)
        {
            _state.Id = 0;
            _state.WindowTitle = "Добавить производство";
            _state.Date = DateTimeOffset.Now;
            AddRow();
            return;
        }

        _state.Id = batch.Id;
        _state.WindowTitle = $"Изменить производство {batch.StartDate:yyyy-MM-dd}";
        _state.Date = batch.StartDate;

        foreach (var item in batch.Items)
            _state.Rows.Add(new ItemRow
            {
                SelectedProductType = productTypes.FirstOrDefault(t => t.Id == item.Product!.ProductType.Id),
                SelectedProduct = item.Product,
                Quantity = (decimal?)item.Quantity
            });
    }

    private void AddRow()
    {
        var row = new ItemRow { SelectedProductType = _productTypes.FirstOrDefault() };
        row.RowChanged += _state.RaiseCanSave;
        _state.Rows.Add(row);
        _state.RaiseCanSave();
    }

    private void RemoveRow(ItemRow row)
    {
        row.RowChanged -= _state.RaiseCanSave;
        _state.Rows.Remove(row);
        _state.RaiseCanSave();
    }

    private async Task SaveAsync()
    {
        try
        {
            var dto = new ProductionEditDto
            {
                Date = _state.Date.Date,
                Items = _state.Rows
                    .Select(r => new ProductionItemEditDto
                    {
                        ProductId = r.SelectedProduct!.Id,
                        Quantity = (int)r.Quantity!
                    })
                    .ToList()
            };

            await using var scope = _scopeFactory.CreateAsyncScope();
            var productionService = scope.ServiceProvider.GetRequiredService<IProductionService>();

            if (_state.IsNew)
                await productionService.CreateAsync(dto);
            else
                await productionService.UpdateBatchAsync(_state.Id, dto);

            Close(true);
        }
        catch (Exception exception)
        {
            await MessageDialog.ShowInfoAsync(this.GetOwnerWindow(), $"Не удалось сохранить производство:\n{exception.Message}");
        }
    }
}
