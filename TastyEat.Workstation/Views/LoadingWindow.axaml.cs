using Avalonia.ReactiveUI;
using TastyEat.Workstation.ViewModels;

namespace TastyEat.Workstation.Views;

public partial class LoadingWindow : ReactiveWindow<LoadingWindowViewModel>
{
    public LoadingWindow()
    {
        InitializeComponent();
    }
}
