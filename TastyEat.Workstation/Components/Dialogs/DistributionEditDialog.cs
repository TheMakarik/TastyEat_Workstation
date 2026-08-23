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
using TastyEat.Workstation.Views.Utils;

namespace TastyEat.Workstation.Components.Dialogs;

public sealed partial class DistributionEditDialog : Window
{
    public sealed partial class ProductRow(Product product, int availableStock, int orderedFromCollections) : ObservableObject
    {
        [ObservableProperty]
        public partial decimal? OrderedQuantity { get; set; } = 0;

        public Product Product => product;

        public int AvailableStock { get; } = availableStock;

        public int OrderedFromCollections { get; } = orderedFromCollections;

        public string StockText => $"доступно {AvailableStock}";
    }

    public sealed partial class ProductGroup(ProductType productType) : ObservableObject
    {
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
        public partial decimal? TotalAmount { get; set; } = 0;

        [ObservableProperty]
        public partial string WindowTitle { get; set; } = "Добавить клиента в развоз";

        public ObservableCollection<Client> Clients { get; } = [];

        public ObservableCollection<ProductGroup> Groups { get; } = [];

        public int Id { get; set; }

        public int DistributionId { get; set; }

        public string? ClientError => SelectedClient is null ? "Необходимо выбрать клиента" : null;

        public string? TotalAmountError => TotalAmount is null || TotalAmount < 0 ? "Сумма должна быть неотрицательным числом" : null;

        public bool CanSave => ClientError is null && TotalAmountError is null;

        partial void OnSelectedClientChanged(Client? value)
        {
            OnPropertyChanged(nameof(ClientError));
            OnPropertyChanged(nameof(CanSave));
        }

        partial void OnTotalAmountChanged(decimal? value)
        {
            OnPropertyChanged(nameof(TotalAmountError));
            OnPropertyChanged(nameof(CanSave));
        }
    }

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly State _state = new();

    public DistributionEditDialog(IServiceScopeFactory scopeFactory)
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
                UiFactory.DialogField("Сумма",
                        new NumericUpDown { Minimum = 0, Maximum = 10_000_000, Increment = 10 }
                            .Value(_state, x => x.TotalAmount, Avalonia.Data.BindingMode.TwoWay),
                        UiFactory.ErrorText(_state, x => x.TotalAmountError))
                    .Grid_Row(2)
                    .Margin(0, 12, 0, 0),
                new ScrollViewer { Margin = new Thickness(0, 12, 0, 0), Content = groupsHost }.Grid_Row(3),
                new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12, HorizontalAlignment = HorizontalAlignment.Right }.Grid_Row(4)
                    .Margin(new Thickness(0, 16, 0, 0))
                    .Children(
                        new Button { Content = "Сохранить", IsDefault = true }
                            .IsEnabled(_state, x => x.CanSave)
                            .OnClick(async _ => await SaveAsync()),
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
        };
    }

    private static Control BuildProductRowUi(ProductRow row) =>
        new Grid().Cols("2*, *, Auto")
            .Margin(0, 4, 0, 4)
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

    public async void Initialize(IReadOnlyList<Client> clients, int distributionId, DistributionClient? existing = null)
    {
        _state.Clients.Clear();
        foreach (var client in clients)
            _state.Clients.Add(client);

        _state.Id = existing?.Id ?? 0;
        _state.DistributionId = distributionId;
        _state.WindowTitle = existing is null
            ? "Добавить клиента в развоз"
            : $"Изменить клиента в развозе: {existing.Client.FullName}";
        _state.SelectedClient = existing?.Client;
        _state.ClientSearchText = existing?.Client.FullName ?? string.Empty;
        _state.TotalAmount = existing?.TotalAmount ?? 0;

        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var distributionService = scope.ServiceProvider.GetRequiredService<IDistributionService>();
            var productTypeService = scope.ServiceProvider.GetRequiredService<IProductTypeService>();

            _state.Groups.Clear();
            var clientId = existing?.Client.Id;
            if (clientId is null)
                return;

            var products = await distributionService.GetClientOrderedProductsAsync(clientId.Value);
            var productTypes = await productTypeService.GetAllAsync();
            var existingItems = existing?.Items.ToDictionary(i => i.Product.Id, i => i.Quantity) ?? [];

            foreach (var type in productTypes.OrderBy(t => t.Name))
            {
                var typeProducts = products
                    .Where(p => p.Product.ProductType.Id == type.Id)
                    .OrderBy(p => p.Product.Name)
                    .ToList();
                if (typeProducts.Count == 0)
                    continue;

                var group = new ProductGroup(type);
                foreach (var ordered in typeProducts)
                {
                    var product = ordered.Product;
                    var quantity = existingItems.GetValueOrDefault(product.Id);
                    var remaining = await distributionService.GetRemainingQuantityAsync(product.Id, _state.Id);
                    group.Rows.Add(new ProductRow(product, remaining + quantity, ordered.OrderedQuantity)
                    {
                        OrderedQuantity = quantity
                    });
                }

                _state.Groups.Add(group);
            }
        }
        catch (Exception exception)
        {
            await MessageDialog.ShowInfoAsync(this.GetOwnerWindow(), $"Не удалось загрузить товары клиента:\n{exception.Message}");
        }
    }

    private Task SaveAsync()
    {
        var items = _state.Groups
            .SelectMany(g => g.Rows)
            .Where(r => r.OrderedQuantity > 0)
            .Select(r => new DistributionItemEditDto
            {
                ProductId = r.Product.Id,
                Quantity = (int)r.OrderedQuantity!
            })
            .ToList();

        var result = new DistributionClientEditDto
        {
            Id = _state.Id,
            DistributionId = _state.DistributionId,
            ClientId = _state.SelectedClient!.Id,
            TotalAmount = (int)(_state.TotalAmount ?? 0),
            Items = items
        };

        Close(result);
        return Task.CompletedTask;
    }
}
