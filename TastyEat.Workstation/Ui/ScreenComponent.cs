using Avalonia.Controls;
using System.ComponentModel;
using Avalonia.Markup.Declarative;
using CommunityToolkit.Mvvm.ComponentModel;
using Material.Icons;

namespace TastyEat.Workstation.Ui;

public interface IScreen
{
    string Title { get; }

    MaterialIconKind Icon { get; }
}

public abstract partial class ScreenComponent<TState>(TState state) : ViewBase<TState>(state), IScreen
    where TState : class, INotifyPropertyChanged
{
    protected TState ScreenState { get; } = state;

    public abstract string Title { get; }

    public abstract MaterialIconKind Icon { get; }
}
