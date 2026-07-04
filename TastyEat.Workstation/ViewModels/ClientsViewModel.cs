using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using Avalonia.Controls;
using Avalonia.Controls.Models.TreeDataGrid;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using TastyEat.Workstation.Models.Tables;
using TastyEat.Workstation.Services.Interfaces;

namespace TastyEat.Workstation.ViewModels;

public sealed partial class ClientsViewModel : ViewModelBase, IDisposable
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ClientEditViewModel _clientEditViewModel;
    private readonly CityEditViewModel _cityEditViewModel;
    private readonly ILogger<ClientsViewModel> _logger;
    private CancellationTokenSource? _loadCts;
    private bool _disposed;

    [Reactive]
    private string _searchText = string.Empty;

    [Reactive]
    private bool _isLoading;

    public ClientsViewModel(
        IServiceScopeFactory scopeFactory,
        ClientEditViewModel clientEditViewModel,
        CityEditViewModel cityEditViewModel,
        ILogger<ClientsViewModel> logger)
    {
        _scopeFactory = scopeFactory;
        _clientEditViewModel = clientEditViewModel;
        _cityEditViewModel = cityEditViewModel;
        _logger = logger;

        Clients = new ObservableCollection<ClientRowViewModel>();
        Cities = new ObservableCollection<City>();
        Referrers = new ObservableCollection<Client>();

        ClientsSource = new FlatTreeDataGridSource<ClientRowViewModel>(Clients)
        {
            Columns =
            {
                new TextColumn<ClientRowViewModel, string>("ФИО", x => x.FullName, new GridLength(2, GridUnitType.Star)),
                new TextColumn<ClientRowViewModel, string>("Телефон", x => x.PhoneNumber, new GridLength(1, GridUnitType.Star)),
                new TemplateColumn<ClientRowViewModel>("В группе", "IsInTelegramChannelCellTemplate", width: GridLength.Auto),
                new TemplateColumn<ClientRowViewModel>("Город", "CityCellTemplate", width: new GridLength(1, GridUnitType.Star)),
                new TemplateColumn<ClientRowViewModel>("Приглашён", "ReferrerCellTemplate", width: new GridLength(1, GridUnitType.Star)),
                new TextColumn<ClientRowViewModel, string>("Купил на сумму", x => x.TotalAmountText, new GridLength(1, GridUnitType.Star)),
                new TemplateColumn<ClientRowViewModel>(string.Empty, "ActionsCellTemplate", width: GridLength.Auto),
            }
        };

        this.WhenAnyValue(vm => vm.SearchText)
            .Throttle(TimeSpan.FromMilliseconds(400))
            .DistinctUntilChanged()
            .Select(_ => Unit.Default)
            .InvokeCommand(SearchCommand);

        _ = SearchAsync();
    }

    public override string Title => "Клиенты";
    public override string IconName => "AccountMultiple";

    public ObservableCollection<ClientRowViewModel> Clients { get; }
    public ObservableCollection<City> Cities { get; }
    public ObservableCollection<Client> Referrers { get; }
    public FlatTreeDataGridSource<ClientRowViewModel> ClientsSource { get; }

    public Interaction<ClientEditViewModel, ClientEditResult?> AddClientInteraction { get; } = new();
    public Interaction<ClientEditViewModel, ClientEditResult?> EditClientInteraction { get; } = new();
    public Interaction<ClientRowViewModel, bool> ConfirmDeleteInteraction { get; } = new();
    public Interaction<ClientRowViewModel, Unit> ShowStatisticsInteraction { get; } = new();
    public Interaction<CityEditViewModel, bool> AddCityInteraction { get; } = new();

    [ReactiveCommand(OutputScheduler = "ReactiveUI.RxApp.MainThreadScheduler")]
    private async Task SearchAsync()
    {
        var token = RefreshLoadCts();
        await using var scope = _scopeFactory.CreateAsyncScope();
        var clientService = scope.ServiceProvider.GetRequiredService<IClientService>();
        var cityService = scope.ServiceProvider.GetRequiredService<ICityService>();

        IsLoading = true;
        try
        {
            foreach (var client in Clients)
                client.CancelLoading();

            var cities = await cityService.GetAllAsync(token);
            var clients = await clientService.SearchAsync(SearchText, token);

            RxApp.MainThreadScheduler.Schedule(() =>
            {
                if (token.IsCancellationRequested)
                    return;

                Cities.Clear();
                foreach (var city in cities)
                    Cities.Add(city);

                Referrers.Clear();
                foreach (var client in clients)
                    Referrers.Add(client);

                Clients.Clear();
                foreach (var client in clients)
                    Clients.Add(CreateRow(client));
            });
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load clients");
        }
        finally
        {
            RxApp.MainThreadScheduler.Schedule(() => IsLoading = false);
        }
    }

    [ReactiveCommand(OutputScheduler = "ReactiveUI.RxApp.MainThreadScheduler")]
    private async Task AddClientAsync()
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var clientService = scope.ServiceProvider.GetRequiredService<IClientService>();
        var existingClients = await clientService.GetAllAsync();

        _clientEditViewModel.Initialize(Cities, Referrers, existingClients);
        var result = await AddClientInteraction.Handle(_clientEditViewModel);
        if (result is not null)
            await SearchAsync();
    }

    [ReactiveCommand(OutputScheduler = "ReactiveUI.RxApp.MainThreadScheduler")]
    private async Task AddCityAsync()
    {
        var accepted = await AddCityInteraction.Handle(_cityEditViewModel);
        if (accepted)
            await SearchAsync();
    }

    [ReactiveCommand(OutputScheduler = "ReactiveUI.RxApp.MainThreadScheduler")]
    private async Task EditClientAsync(ClientRowViewModel row)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var clientService = scope.ServiceProvider.GetRequiredService<IClientService>();
        var client = await clientService.GetByIdAsync(row.Id);

        if (client is null)
        {
            _logger.LogWarning("Client with id {ClientId} not found for editing", row.Id);
            return;
        }

        var existingClients = await clientService.GetAllAsync();

        _clientEditViewModel.Initialize(Cities, Referrers, existingClients, client);
        var result = await EditClientInteraction.Handle(_clientEditViewModel);
        if (result is not null)
            await SearchAsync();
    }

    [ReactiveCommand(OutputScheduler = "ReactiveUI.RxApp.MainThreadScheduler")]
    private async Task DeleteClientAsync(ClientRowViewModel row)
    {
        var confirmed = await ConfirmDeleteInteraction.Handle(row);
        if (!confirmed)
            return;

        await using var scope = _scopeFactory.CreateAsyncScope();
        var clientService = scope.ServiceProvider.GetRequiredService<IClientService>();
        await clientService.DeleteAsync(row.Id);
        await SearchAsync();
    }

    [ReactiveCommand(OutputScheduler = "ReactiveUI.RxApp.MainThreadScheduler")]
    private void ShowStatistics(ClientRowViewModel row) =>
        ShowStatisticsInteraction.Handle(row).Subscribe();

    private ClientRowViewModel CreateRow(Client client)
    {
        var row = new ClientRowViewModel(_scopeFactory)
        {
            Id = client.Id,
            FullName = client.FullName,
            PhoneNumber = client.PhoneNumber,
            IsInTelegramChannel = client.IsInTelegramChannel,
            City = client.City,
            Referrer = client.Referrer
        };
        row.SetContext(Cities, Referrers);
        row.StartLoadingTotalAmount();
        return row;
    }

    private CancellationToken RefreshLoadCts()
    {
        var newCts = new CancellationTokenSource();
        var oldCts = Interlocked.Exchange(ref _loadCts, newCts);
        oldCts?.Cancel();
        oldCts?.Dispose();
        return newCts.Token;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        var cts = Interlocked.Exchange(ref _loadCts, null);
        cts?.Cancel();
        cts?.Dispose();
    }
}
