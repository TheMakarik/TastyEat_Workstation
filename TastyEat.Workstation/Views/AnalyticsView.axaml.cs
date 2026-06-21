using Avalonia.Controls;
using Avalonia.ReactiveUI;
using TastyEat.Workstation.ViewModels;

namespace TastyEat.Workstation.Views;

public partial class AnalyticsView : ReactiveUserControl<AnalyticsViewModel>
{
    public AnalyticsView()
    {
        InitializeComponent();
    }
}
