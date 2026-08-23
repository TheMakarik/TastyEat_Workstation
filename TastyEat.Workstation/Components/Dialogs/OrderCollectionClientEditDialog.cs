using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Markup.Declarative;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using TastyEat.Workstation.Models.Dto;
using TastyEat.Workstation.Models.Tables;
using TastyEat.Workstation.Services.Interfaces;
using TastyEat.Workstation.Ui;

namespace TastyEat.Workstation.Components.Dialogs;

public sealed partial class OrderCollectionClientEditDialog : Window
{
    public sealed partial class ProductRow(Product product, int availableStock, int producedQuantity, int orderedQuantity) : ObservableObject
    {
        [ObservableProperty]
        public partial decimal? OrderedQuantity { get; set; } = orderedQuantity;

        [ObservableProperty]
        public partial bool IsVisible { get; set; } = true;

        public Product Product => product;

        public int ProducedQuantity { get; } = producedQuantity;

        public string StockText => $"доступно {availableStock}, произведено {producedQuantity}";
    }

    public sealed partial class ProductGroup(ProductType productType) : ObservableObject
    {
        [ObservableProperty]
        public partial bool IsVisible { get; set; } = true;

        public ProductType ProductType => productType;

        public ObservableCollection<ProductRow> Rows { get; } = [];
    }

    public sealed partial class State : ObservableObject
    {
        [ObservableProperty]
        public partial Client? SelectedClient { get; set; }

        [ObservableProperty]
        public partial string? ClientSearchText { get; set; }

        [ObservableProperty]
        public partial bool ShowOnlyProducedProducts { get; set; }

        [ObservableProperty]
        public partial bool HideZeroQuantityProducts { get; set; }

        [ObservableProperty]
        public partial string WindowTitle { get; set; } = "Добавить клиента в сбор";

        public ObservableCollection<Client> Clients { get; } = [];

        public ObservableCollection<ProductGroup> Groups { get; } = [];

        public int Id { get; set; }

        public string? ClientError => SelectedClient is null ? "Необходимо выбрать клиента" : null;

        public bool CanSave => ClientError is null;

        partial void OnSelectedClientChanged(Client? value)
        {
            OnPropertyChanged(nameof(ClientError));
            OnPropertyChanged(nameof(CanSave));
        }
    }

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly State _state = new();

    public OrderCollectionClientEditDialog(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;

        Width = 640;
        Height = 640;
        CanResize = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Title = _state.WindowTitle;
        _state.PropertyChanged += (_, eventArgs) =>
        {
            if (eventArgs.PropertyName == nameof(State.WindowTitle))
                Title = _state.WindowTitle;
        };

        var clientSelector = new AutoCompleteBox
        {
            PlaceholderText = "Начните вводить ФИО",
            MinimumPrefixLength = 1
        }
            .ItemsSource(_state, x => x.Clients)
            .SelectedItem(_state, x => x.SelectedClient, Avalonia.Data.BindingMode.TwoWay)
            .Text(_state, x => x.ClientSearchText, Avalonia.Data.BindingMode.TwoWay);

        var groupsHost = new ItemsControl
        {
            ItemsSource = _state.Groups,
            ItemTemplate = new FuncDataTemplate<ProductGroup>((group, _) => group is null ? null : BuildGroupUi(group))
        };

        Content = new Grid { Margin = new Thickness(24) }.Rows("Auto, Auto, Auto, *, Auto")
            .Children(
                new TextBlock { FontSize = 20, FontWeight = Avalonia.Media.FontWeight.Bold }
                    .Text(_state, x => x.WindowTitle),
                UiFactory.DialogField("Клиент", clientSelector, UiFactory.ErrorText(_state, x => x.ClientError))
                    .Grid_Row(1)
                    .Margin(new Thickness(0, 16, 0, 0)),
                new StackPanel { Orientation = Orientation.Horizontal, Spacing = 20 }.Grid_Row(2).Margin(new Thickness(0, 12, 0, 0))
                    .Children(
                        new CheckBox { Content = "Только произведённые" }
                            .IsChecked(_state, x => x.ShowOnlyProducedProducts, Avalonia.Data.BindingMode.TwoWay),
                        new CheckBox { Content = "Скрыть с нулевым количеством" }
                            .IsChecked(_state, x => x.HideZeroQuantityProducts, Avalonia.Data.BindingMode.TwoWay)),
                new ScrollViewer { Margin = new Thickness(0, 12, 0, 0), Content = groupsHost }.Grid_Row(3),
                new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12, HorizontalAlignment = HorizontalAlignment.Right }.Grid_Row(4)
                    .Margin(new Thickness(0, 16, 0, 0))
                    .Children(
                        new Button { Content = "Сохранить", IsDefault = true }
                            .IsEnabled(_state, x => x.CanSave)
                            .OnClick(_ => Save()),
                        new Button { Content = "Отмена", IsCancel = true }
                            .OnClick(_ => Close(null))));
    }

    private Control BuildGroupUi(ProductGroup group)
    {
        var rowsHost = new ItemsControl
        {
            ItemsSource = group.Rows,
            ItemTemplate = new FuncDataTemplate<ProductRow>((row, _) => row is null ? null : BuildProductRowUi(row))
        };

        return new Expander
        {
            Header = group.ProductType.Name,
            Margin = new Thickness(0, 0, 0, 8),
            Content = rowsHost
        }.IsVisible(group, x => x.IsVisible);
    }

    private static Control BuildProductRowUi(ProductRow row) =>
        new Grid().Cols("2*, *, Auto")
            .Margin(new Thickness(0, 4, 0, 4))
            .Children(
                new TextBlock { Text = row.Product.Name, VerticalAlignment = VerticalAlignment.Center },
                new TextBlock()
                    .Text(row, x => x.StockText)
                    .Grid_Column(1)
                    .Foreground(Avalonia.Media.Brush.Parse("#666666"))
                    .VerticalAlignment(VerticalAlignment.Center),
                new NumericUpDown { Minimum = 0, Maximum = 1_000_000, Increment = 1, Width = 120 }
                    .Value(row, x => x.OrderedQuantity, Avalonia.Data.BindingMode.TwoWay)
                    .Grid_Column(2));

    public async Task InitializeAsync(
        int collectionId,
        IReadOnlyList<Client> clients,
        IReadOnlyList<ProductType> productTypes,
        OrderCollectionClient? existing = null)
    {
        _state.Clients.Clear();
        foreach (var client in clients)
            _state.Clients.Add(client);

        _state.Id = existing?.Id ?? 0;
        _state.WindowTitle = existing is null ? "Добавить клиента в сбор" : $"Изменить заказ: {existing.Client.FullName}";
        _state.SelectedClient = existing?.Client;
        _state.ClientSearchText = existing?.Client.FullName ?? string.Empty;

        await using var scope = _scopeFactory.CreateAsyncScope();
        var orderService = scope.ServiceProvider.GetRequiredService<IOrderCollectionService>();

        _state.Groups.Clear();
        foreach (var type in productTypes.OrderBy(t => t.Name))
        {
            var group = new ProductGroup(type);
            foreach (var product in type.Products.OrderBy(p => p.Name))
            {
                var existingItem = existing?.Items.FirstOrDefault(i => i.Product.Id == product.Id);
                var orderedQuantity = existingItem?.Quantity ?? 0;
                var available = await orderService.GetAvailableStockAsync(product.Id, existing?.Id);
                var produced = await orderService.GetProducedQuantityAsync(product.Id);
                group.Rows.Add(new ProductRow(product, available + orderedQuantity, produced, orderedQuantity));
            }

            if (group.Rows.Count > 0)
            {
                foreach (var row in group.Rows)
                    row.PropertyChanged += (_, _) => ApplyFilter();
                _state.Groups.Add(group);
            }
        }

        _state.PropertyChanged += (_, eventArgs) =>
        {
            if (eventArgs.PropertyName is nameof(State.ShowOnlyProducedProducts) or nameof(State.HideZeroQuantityProducts))
                ApplyFilter();
        };

        ApplyFilter();
    }

    private void ApplyFilter()
    {
        foreach (var group in _state.Groups)
        {
            foreach (var row in group.Rows)
            {
                var producedVisible = !_state.ShowOnlyProducedProducts || row.ProducedQuantity > 0;
                row.IsVisible = producedVisible && (!_state.HideZeroQuantityProducts || (row.OrderedQuantity ?? 0) > 0);
            }

            group.IsVisible = group.Rows.Any(r => r.IsVisible);
        }
    }

    private void Save()
    {
        var items = _state.Groups
            .SelectMany(g => g.Rows)
            .Where(r => (r.OrderedQuantity ?? 0) > 0)
            .Select(r => new OrderCollectionItemEditDto
            {
                ProductId = r.Product.Id,
                Quantity = (int)r.OrderedQuantity!
            })
            .ToList();

        Close(new OrderCollectionClientEditDto
        {
            Id = _state.Id,
            ClientId = _state.SelectedClient!.Id,
            Items = items
        });
    }
}
