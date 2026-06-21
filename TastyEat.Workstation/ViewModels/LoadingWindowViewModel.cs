using ReactiveUI.SourceGenerators;

namespace TastyEat.Workstation.ViewModels;

public sealed partial class LoadingWindowViewModel : ViewModelBase
{
    [Reactive]
    private double _progress;

    [Reactive]
    private string _status = "Загрузка...";

    public override string Title => "Загрузка";
    public override string IconName => "Loading";
}
