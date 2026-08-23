using System.Collections.ObjectModel;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Markup.Declarative;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using TastyEat.Workstation.Components.Dialogs;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Material.Icons;
using Material.Icons.Avalonia;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TastyEat.Workstation.Messages;
using TastyEat.Workstation.Models.Tables;
using TastyEat.Workstation.Services.Interfaces;
using TastyEat.Workstation.Ui;
using TastyEat.Workstation.Views.Utils;

namespace TastyEat.Workstation.Components;

public sealed partial class ClientsScreen(
    IServiceScopeFactory scopeFactory,
    ILogger<ClientsScreen> logger) : ScreenComponent<ClientsScreen.State>(new State())
{
    public sealed partial class State : ObservableObject
    {
        [ObservableProperty]
        public partial string SearchText { get; set; } = string.Empty;

        [ObservableProperty]
        public partial bool IsLoading { get; set; } = true;
    }

    public sealed partial class ClientRow : ObservableObject
    {
        [ObservableProperty]
        public partial string TotalAmountText { get; set; } = "…";

        [ObservableProperty]
        public partial string InvitedCountText { get; set; } = "…";

        public int Id { get; init; }
        public string FullName { get; init; } = string.Empty;
        public string PhoneNumber { get; init; } = string.Empty;
        public bool IsInTelegramChannel { get; init; }
        public City? City { get; init; }
        public Client? Referrer { get; init; }

        public static ClientRow Create(IServiceScopeFactory scopeFactory, Client client)
        {
            var row = new ClientRow
            {
                Id = client.Id,
                FullName = client.FullName,
                PhoneNumber = client.PhoneNumber,
                IsInTelegramChannel = client.IsInTelegramChannel,
                City = client.City,
                Referrer = client.Referrer
            };
            row.StartLoadingDetails(scopeFactory);
            return row;
        }

        private void StartLoadingDetails(IServiceScopeFactory scopeFactory) =>
            _ = Task.Run(async () =>
            {
                try
                {
                    await using var scope = scopeFactory.CreateAsyncScope();
                    var clientService = scope.ServiceProvider.GetRequiredService<IClientService>();
                    var amount = await clientService.GetTotalPurchasedAmountAsync(Id);
                    var invited = await clientService.GetInvitedCountAsync(Id);

                    await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        TotalAmountText = $"{amount:N0}";
                        InvitedCountText = invited.ToString();
                    });
                }
                catch (Exception)
                {
                    await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        TotalAmountText = "Ошибка";
                        InvitedCountText = "Ошибка";
                    });
                }
            });
    }

    public override string Title => "Клиенты";
    public override MaterialIconKind Icon => MaterialIconKind.AccountMultiple;

    private readonly ObservableCollection<ClientRow> _rows = [];
    private readonly ObservableCollection<City> _cities = [];
    private readonly ObservableCollection<Client> _referrers = [];
    private readonly DispatcherTimer _searchDebounce = new() { Interval = TimeSpan.FromMilliseconds(400) };
    private CancellationTokenSource? _loadCts;
    private FlatTreeDataGridSource<ClientRow>? _clientsSource;

    private FlatTreeDataGridSource<ClientRow> BuildClientsSource() => new(_rows)
    {
        Columns =
        {
            new TextColumn<ClientRow, string>("ФИО", x => x.FullName, new GridLength(2, GridUnitType.Star)),
            new TextColumn<ClientRow, string>("Телефон", x => x.PhoneNumber, new GridLength(1, GridUnitType.Star)),
            new TemplateColumn<ClientRow>("В группе", new FuncDataTemplate<ClientRow>((row, _) =>
                new CheckBox
                {
                    IsChecked = row?.IsInTelegramChannel ?? false,
                    IsHitTestVisible = false,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 8, 0, 0)
                }), width: GridLength.Auto),
            new TemplateColumn<ClientRow>("Город", new FuncDataTemplate<ClientRow>((row, _) =>
                new TextBlock { Text = row?.City?.Name ?? string.Empty, VerticalAlignment = VerticalAlignment.Center }),
                width: new GridLength(1, GridUnitType.Star)),
            new TemplateColumn<ClientRow>("Приглашён", new FuncDataTemplate<ClientRow>((row, _) =>
                new TextBlock { Text = row?.Referrer?.FullName ?? string.Empty, VerticalAlignment = VerticalAlignment.Center }),
                width: new GridLength(1, GridUnitType.Star)),
            new TextColumn<ClientRow, string>("Купил на сумму", x => x.TotalAmountText, new GridLength(1, GridUnitType.Star)),
            new TextColumn<ClientRow, string>("Всего пригласил(а)", x => x.InvitedCountText, new GridLength(1, GridUnitType.Star)),
            new TemplateColumn<ClientRow>(string.Empty, new FuncDataTemplate<ClientRow>((row, _) =>
                new Button
                {
                    Classes = { "action" },
                    Content = new MaterialIcon { Kind = MaterialIconKind.Menu }
                }.OnClick(eventArgs => ShowRowActions(eventArgs.Source as Button, row))), width: GridLength.Auto)
        }
    };

    protected override object Build(State state)
    {
        _clientsSource ??= BuildClientsSource();
        _searchDebounce.Tick += (_, _) => _ = SearchAsync();
        state.PropertyChanged += (_, eventArgs) =>
        {
            if (eventArgs.PropertyName != nameof(State.SearchText))
                return;
            _searchDebounce.Stop();
            _searchDebounce.Start();
        };

        WeakReferenceMessenger.Default.Register<ClientPurchasesChangedMessage>(this, (_, _) =>
            Avalonia.Threading.Dispatcher.UIThread.Post(() => _ = SearchAsync()));

        var searchBox = new SearchTextBox { Width = 320, HorizontalAlignment = HorizontalAlignment.Left }
            .Text(state, x => x.SearchText, Avalonia.Data.BindingMode.TwoWay);

        var buttons = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12 }
            .Children(
                UiFactory.ActionButton(MaterialIconKind.AccountPlus, "Добавить клиента", () => _ = AddClientAsync()),
                UiFactory.ActionButton(MaterialIconKind.CityVariantOutline, "Управление городами", () => _ = ManageCitiesAsync(), "sidebarAction"));

        Avalonia.Threading.Dispatcher.UIThread.Post(async () => await SearchAsync());

        return new Grid().Rows("Auto, Auto, *").Classes("managementLayout")
            .Children(
                UiFactory.Header(MaterialIconKind.AccountMultiple, "Клиенты", "Управление клиентами и городами"),
                new Grid().Cols("*, Auto").Classes("topbar").Grid_Row(1)
                    .Children(searchBox, buttons.Grid_Column(1)),
                new Border().Classes("dataGridHost").Grid_Row(2)
                    .Child(new TreeDataGrid { Source = _clientsSource! }),
                UiFactory.LoadingOverlay(state, x => x.IsLoading).Grid_RowSpan(3)
            );
    }

    private void ShowRowActions(Button? anchor, ClientRow? row)
    {
        if (anchor is null || row is null)
            return;

        var flyout = new MenuFlyout();
        flyout.Items.Add(new MenuItem
        {
            Header = "Изменить клиента",
            Icon = new MaterialIcon { Kind = MaterialIconKind.Pencil },
            Command = EditClientCommand,
            CommandParameter = row
        });
        flyout.Items.Add(new MenuItem
        {
            Header = "Удалить клиента",
            Icon = new MaterialIcon { Kind = MaterialIconKind.Delete },
            Command = DeleteClientCommand,
            CommandParameter = row
        });
        flyout.ShowAt(anchor);
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        var token = RefreshLoadCts();

        ScreenState.IsLoading = true;
        try
        {
            var searchText = ScreenState.SearchText;
            var (cities, clients) = await Task.Run(async () =>
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var clientService = scope.ServiceProvider.GetRequiredService<IClientService>();
                var cityService = scope.ServiceProvider.GetRequiredService<ICityService>();
                var loadedCities = await cityService.GetAllAsync(token);
                var loadedClients = await clientService.SearchAsync(searchText, token);
                return (loadedCities, loadedClients);
            }, token);

            if (token.IsCancellationRequested)
                return;

            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                _cities.Clear();
                foreach (var city in cities)
                    _cities.Add(city);

                _referrers.Clear();
                foreach (var client in clients)
                    _referrers.Add(client);

                _rows.Clear();
                foreach (var client in clients)
                    _rows.Add(ClientRow.Create(scopeFactory, client));
            });
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Не удалось загрузить клиентов");
        }
        finally
        {
            if (!token.IsCancellationRequested)
                ScreenState.IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task AddClientAsync()
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var clientService = scope.ServiceProvider.GetRequiredService<IClientService>();
        var existingClients = await clientService.GetAllAsync();

        var dialog = scope.ServiceProvider.GetRequiredService<Dialogs.ClientEditDialog>();
        dialog.Initialize(_cities, _referrers, existingClients);
        var result = await dialog.ShowDialog<ClientEditResult?>(this.GetOwnerWindow());
        if (result is not null)
            await SearchAsync();
    }

    [RelayCommand]
    private async Task AddCityAsync()
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dialog = scope.ServiceProvider.GetRequiredService<Dialogs.CityEditDialog>();
        var accepted = await dialog.ShowDialog<bool?>(this.GetOwnerWindow());
        if (accepted == true)
            await SearchAsync();
    }

    [RelayCommand]
    private async Task ManageCitiesAsync()
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dialog = scope.ServiceProvider.GetRequiredService<Dialogs.CityManagementDialog>();
        dialog.Initialize();
        await dialog.ShowDialog<object?>(this.GetOwnerWindow());
        await SearchAsync();
    }


    [RelayCommand]
    private async Task EditClientAsync(ClientRow row)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var clientService = scope.ServiceProvider.GetRequiredService<IClientService>();
        var client = await clientService.GetByIdAsync(row.Id);

        if (client is null)
        {
            logger.LogWarning("Клиент с id {ClientId} не найден для редактирования", row.Id);
            return;
        }

        var existingClients = await clientService.GetAllAsync();

        var dialog = scope.ServiceProvider.GetRequiredService<Dialogs.ClientEditDialog>();
        dialog.Initialize(_cities, _referrers, existingClients, client);
        var result = await dialog.ShowDialog<ClientEditResult?>(this.GetOwnerWindow());
        if (result is not null)
            await SearchAsync();
    }

    [RelayCommand]
    private async Task DeleteClientAsync(ClientRow row)
    {
        var confirmed = await DeleteConfirmationDialog.ShowAsync(this.GetOwnerWindow(), $"Удалить клиента \"{row.FullName}\"?");
        if (!confirmed)
            return;

        await using var scope = scopeFactory.CreateAsyncScope();
        var clientService = scope.ServiceProvider.GetRequiredService<IClientService>();
        await clientService.DeleteAsync(row.Id);
        await SearchAsync();
    }

    private CancellationToken RefreshLoadCts()
    {
        var newCts = new CancellationTokenSource();
        var oldCts = Interlocked.Exchange(ref _loadCts, newCts);
        oldCts?.Cancel();
        oldCts?.Dispose();
        return newCts.Token;
    }
}
