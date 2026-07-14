using System.Reactive.Linq;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using ReactiveUI.Validation.Extensions;

namespace TastyEat.Workstation.ViewModels;

public sealed partial class DistributionDateViewModel : ValidatableViewModelBase
{
    private readonly IObservable<bool> _canExecute;

    [Reactive]
    private DateTimeOffset _date;

    public DistributionDateViewModel()
    {
        Date = DateTimeOffset.Now;

        this.ValidationRule(
            vm => vm.Date,
            d => d > DateTimeOffset.MinValue,
            "Необходимо выбрать дату");

        _canExecute = this.IsValid().ObserveOn(RxApp.MainThreadScheduler);
    }

    [ReactiveCommand(CanExecute = nameof(_canExecute), OutputScheduler = "ReactiveUI.RxApp.MainThreadScheduler")]
    private async Task<DateTimeOffset?> SaveAsync()
    {
        var isValid = await this.IsValid().FirstAsync();
        return isValid ? Date : null;
    }
}
