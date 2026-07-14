using Avalonia.ReactiveUI;
using TastyEat.Workstation.ViewModels;

namespace TastyEat.Workstation.Views;

public partial class PieChartWindow : ReactiveWindow<PieChartViewModel>
{
    public PieChartWindow()
    {
        InitializeComponent();
    }
}
