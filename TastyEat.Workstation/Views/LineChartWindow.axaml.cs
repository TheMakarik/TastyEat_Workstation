using Avalonia.ReactiveUI;
using TastyEat.Workstation.ViewModels;

namespace TastyEat.Workstation.Views;

public partial class LineChartWindow : ReactiveWindow<LineChartViewModel>
{
    public LineChartWindow()
    {
        InitializeComponent();
    }
}
