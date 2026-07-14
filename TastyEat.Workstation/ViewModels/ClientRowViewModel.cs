using System.Reactive;
using System.Reactive.Concurrency;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using TastyEat.Workstation.Models.Tables;
using TastyEat.Workstation.Services.Interfaces;

namespace TastyEat.Workstation.ViewModels;

public sealed partial class ClientRowViewModel(IServiceScopeFactory scopeFactory) : ReactiveObject
{
    private CancellationTokenSource? _loadingCts;

    [Reactive]
    private int _id;

    [Reactive]
    private string _fullName = string.Empty;

    [Reactive]
    private string _phoneNumber = string.Empty;

    [Reactive]
    private bool _isInTelegramChannel;

    [Reactive]
    private City? _city;

    [Reactive]
    private Client? _referrer;

    [Reactive]
    private IReadOnlyList<City> _cities = [];

    [Reactive]
    private IReadOnlyList<Client> _referrers = [];

    [Reactive]
    private string _totalAmountText = "Загрузка";

    [Reactive]
    private string _invitedCountText = "Загрузка";

    public void SetContext(IReadOnlyList<City> cities, IReadOnlyList<Client> referrers) =>
        (Cities, Referrers) = (cities, referrers);

    public void StartLoadingDetails()
    {
        _loadingCts?.Cancel();
        _loadingCts?.Dispose();
        _loadingCts = new CancellationTokenSource();
        var token = _loadingCts.Token;

        _ = Task.Run(async () =>
        {
            var dots = 0;
            var loading = true;

            var animationTask = Task.Run(async () =>
            {
                while (loading && !token.IsCancellationRequested)
                {
                    var text = "Загрузка" + new string('.', dots);
                    RxApp.MainThreadScheduler.Schedule(() =>
                    {
                        TotalAmountText = text;
                        InvitedCountText = text;
                    });
                    dots = (dots + 1) % 4;

                    try
                    {
                        await Task.Delay(500, token);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
            }, token);

            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var clientService = scope.ServiceProvider.GetRequiredService<IClientService>();
                var amountTask = clientService.GetTotalPurchasedAmountAsync(Id, token);
                var invitedTask = clientService.GetInvitedCountAsync(Id, token);
                await Task.WhenAll(amountTask, invitedTask);

                var amount = amountTask.Result;
                var invited = invitedTask.Result;
                loading = false;
                await animationTask;
                RxApp.MainThreadScheduler.Schedule(() =>
                {
                    TotalAmountText = $"{amount:N0}";
                    InvitedCountText = invited.ToString();
                });
            }
            catch (OperationCanceledException)
            {
                loading = false;
                await animationTask;
            }
            catch (Exception)
            {
                loading = false;
                await animationTask;
                RxApp.MainThreadScheduler.Schedule(() =>
                {
                    TotalAmountText = "Ошибка";
                    InvitedCountText = "Ошибка";
                });
            }
        }, token);
    }

    public void CancelLoading()
    {
        _loadingCts?.Cancel();
        _loadingCts?.Dispose();
        _loadingCts = null;
    }
}
