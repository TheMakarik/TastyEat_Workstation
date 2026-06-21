using System.Reactive;
using System.Reactive.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using ReactiveUI.Validation.Extensions;
using TastyEat.Workstation.Options;
using TastyEat.Workstation.Services.Interfaces;

namespace TastyEat.Workstation.ViewModels;

public sealed partial class CityEditViewModel : ValidatableViewModelBase
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly StringLengthOptions _stringLengthOptions;
    private readonly IObservable<bool> _canExecute;

    [Reactive]
    private string _name = string.Empty;

    public CityEditViewModel(IServiceScopeFactory scopeFactory, IOptions<StringLengthOptions> stringLengthOptions)
    {
        _scopeFactory = scopeFactory;
        _stringLengthOptions = stringLengthOptions.Value;

        this.ValidationRule(
            vm => vm.Name,
            name => !string.IsNullOrWhiteSpace(name)
                    && name.Length <= _stringLengthOptions.CityNameMaxLength,
            $"Название города не должно превышать {_stringLengthOptions.CityNameMaxLength} символов");

        var nameUniqueObservable = this.WhenAnyValue(vm => vm.Name)
            .Throttle(TimeSpan.FromMilliseconds(500))
            .DistinctUntilChanged()
            .SelectMany(name => Observable.FromAsync(async () =>
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var cityService = scope.ServiceProvider.GetRequiredService<ICityService>();
                var trimmed = name?.Trim() ?? string.Empty;
                if (string.IsNullOrEmpty(trimmed) || trimmed.Length > _stringLengthOptions.CityNameMaxLength)
                    return false;

                return await cityService.ExistsByNameAsync(trimmed);
            }))
            .Select(exists => !exists)
            .ObserveOn(RxApp.MainThreadScheduler);

        this.ValidationRule(nameUniqueObservable, "Город с таким названием уже существует");

        _canExecute = this.IsValid().ObserveOn(RxApp.MainThreadScheduler);
    }

    [ReactiveCommand(CanExecute = nameof(_canExecute), OutputScheduler = "ReactiveUI.RxApp.MainThreadScheduler")]
    private async Task<Models.Tables.City?> SaveAsync()
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var cityService = scope.ServiceProvider.GetRequiredService<ICityService>();
        return await cityService.CreateAsync(Name.Trim());
    }
}
