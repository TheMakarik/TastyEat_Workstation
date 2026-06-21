using ReactiveUI;

namespace TastyEat.Workstation.ViewModels;

public abstract class ViewModelBase : ReactiveObject
{
    public abstract string Title { get; }
    public abstract string IconName { get; }
}