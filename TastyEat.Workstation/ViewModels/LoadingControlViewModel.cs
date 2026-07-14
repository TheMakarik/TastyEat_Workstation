using System.Reactive.Concurrency;
using System.Reactive.Linq;
using Microsoft.Extensions.Options;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using TastyEat.Workstation.Options;

namespace TastyEat.Workstation.ViewModels;

public sealed partial class LoadingControlViewModel : ViewModelBase, IDisposable
{
    private readonly LoadingAnimationOptions _options;
    private readonly IDisposable _timerSubscription;

    [Reactive]
    private bool _isLoading;

    [Reactive]
    private string _displayText = "Загрузка";

    public LoadingControlViewModel()
        : this(new LoadingAnimationOptions())
    {
    }

    public LoadingControlViewModel(IOptions<LoadingAnimationOptions> options)
        : this(options.Value)
    {
    }

    public LoadingControlViewModel(LoadingAnimationOptions options)
    {
        _options = options;
        UpdateDisplayText(_options.MinDots);

        _timerSubscription = Observable
            .Interval(TimeSpan.FromMilliseconds(_options.IntervalMilliseconds))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(_ =>
            {
                var currentDots = DisplayText.Length - "Загрузка".Length;
                var nextDots = currentDots + 1;
                if (nextDots > _options.MaxDots)
                    nextDots = _options.MinDots;

                UpdateDisplayText(nextDots);
            });
    }

    public override string Title => string.Empty;
    public override string IconName => string.Empty;

    public void Dispose()
    {
        _timerSubscription.Dispose();
    }

    private void UpdateDisplayText(int dotsCount)
    {
        DisplayText = $"Загрузка{new string('.', dotsCount)}";
    }
}
