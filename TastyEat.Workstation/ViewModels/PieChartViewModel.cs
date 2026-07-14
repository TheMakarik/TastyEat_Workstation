using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using ReactiveUI.SourceGenerators;
using SkiaSharp;
using TastyEat.Workstation.Views;

namespace TastyEat.Workstation.ViewModels;

public sealed partial class PieChartViewModel : ViewModelBase
{
    private static readonly SKColor[] Colors = ChartColorProvider.GetColors();

    public PieChartViewModel(LoadingControlViewModel loading)
    {
        Loading = loading;
    }

    [Reactive]
    private string _chartTitle = string.Empty;

    [Reactive]
    private ISeries[] _series = [];

    public LoadingControlViewModel Loading { get; }

    public override string Title => "Диаграмма";
    public override string IconName => "ChartPie";

    public void LoadShares(IReadOnlyList<Models.Dto.ClientPurchaseShareDto> shares)
    {
        ChartTitle = "Доля клиентов";
        var series = new List<ISeries>();
        for (var i = 0; i < shares.Count; i++)
        {
            var share = shares[i];
            series.Add(new PieSeries<int>
            {
                Values = [share.TotalAmount],
                Name = share.FullName,
                Fill = new SolidColorPaint(Colors[i % Colors.Length]),
                DataLabelsPaint = new SolidColorPaint(SKColors.Black),
                DataLabelsPosition = LiveChartsCore.Measure.PolarLabelsPosition.Outer,
                DataLabelsFormatter = point => $"{point.Context.Series.Name}: {point.Coordinate.PrimaryValue:N0} ₽",
                ToolTipLabelFormatter = point => $"{point.Context.Series.Name}: {point.Coordinate.PrimaryValue:N0} ₽"
            });
        }

        Series = series.ToArray();
    }

    public void LoadProductShares(IReadOnlyList<Models.Dto.ClientProductShareDto> shares)
    {
        var series = new List<ISeries>();
        for (var i = 0; i < shares.Count; i++)
        {
            var share = shares[i];
            series.Add(new PieSeries<int>
            {
                Values = [share.Quantity],
                Name = $"{share.ProductTypeName} - {share.ProductName}",
                Fill = new SolidColorPaint(Colors[i % Colors.Length]),
                DataLabelsPaint = new SolidColorPaint(SKColors.Black),
                DataLabelsPosition = LiveChartsCore.Measure.PolarLabelsPosition.Outer,
                DataLabelsFormatter = point => $"{point.Context.Series.Name}: {point.Coordinate.PrimaryValue:N0}",
                ToolTipLabelFormatter = point => $"{point.Context.Series.Name}: {point.Coordinate.PrimaryValue:N0}"
            });
        }

        Series = series.ToArray();
    }
}
