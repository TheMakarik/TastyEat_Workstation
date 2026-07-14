using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using ReactiveUI.SourceGenerators;
using SkiaSharp;
using TastyEat.Workstation.Views;

namespace TastyEat.Workstation.ViewModels;

public sealed partial class LineChartViewModel : ViewModelBase
{
    private static readonly SKColor[] Colors = ChartColorProvider.GetColors();

    public LineChartViewModel(LoadingControlViewModel loading)
    {
        Loading = loading;
    }

    [Reactive]
    private string _chartTitle = string.Empty;

    [Reactive]
    private ISeries[] _series = [];

    [Reactive]
    private Axis[] _xAxes = [];

    [Reactive]
    private Axis[] _yAxes = [];

    public LoadingControlViewModel Loading { get; }

    public override string Title => "График";
    public override string IconName => "ChartLine";

    public void LoadCollectionStatistics(IReadOnlyList<Models.Dto.OrderCollectionStatisticDto> statistics)
    {
        ChartTitle = "Статистика покупателей";
        XAxes =
        [
            new Axis
            {
                Labels = statistics.Select(s => s.Date.ToString("yyyy-MM-dd")).ToArray(),
                LabelsPaint = new SolidColorPaint(SKColors.Black)
            }
        ];
        YAxes =
        [
            new Axis
            {
                Name = "Количество клиентов",
                LabelsPaint = new SolidColorPaint(SKColors.Black),
                NamePaint = new SolidColorPaint(SKColors.Black)
            }
        ];
        Series =
        [
            new LineSeries<int>
            {
                Values = statistics.Select(s => s.ClientCount).ToArray(),
                Name = "Клиенты",
                Stroke = new SolidColorPaint(Colors[0], 3),
                Fill = null,
                GeometryStroke = new SolidColorPaint(Colors[0], 3),
                GeometryFill = new SolidColorPaint(SKColors.White),
                DataLabelsPaint = new SolidColorPaint(SKColors.Black),
                YToolTipLabelFormatter = point => $"{point.Coordinate.PrimaryValue} клиентов"
            }
        ];
    }

    public void LoadPurchaseHistory(IReadOnlyList<Models.Dto.ClientPurchaseHistoryDto> history)
    {
        ChartTitle = "График покупок";
        XAxes =
        [
            new Axis
            {
                Labels = history.Select(h => h.Date.ToString("yyyy-MM-dd")).ToArray(),
                LabelsPaint = new SolidColorPaint(SKColors.Black)
            }
        ];
        YAxes =
        [
            new Axis
            {
                Name = "Сумма, ₽",
                LabelsPaint = new SolidColorPaint(SKColors.Black),
                NamePaint = new SolidColorPaint(SKColors.Black)
            }
        ];
        Series =
        [
            new LineSeries<int>
            {
                Values = history.Select(h => h.TotalAmount).ToArray(),
                Name = "Покупки",
                Stroke = new SolidColorPaint(Colors[0], 3),
                Fill = new SolidColorPaint(Colors[0].WithAlpha(50)),
                GeometryStroke = new SolidColorPaint(Colors[0], 3),
                GeometryFill = new SolidColorPaint(SKColors.White),
                DataLabelsPaint = new SolidColorPaint(SKColors.Black),
                YToolTipLabelFormatter = point => $"{point.Coordinate.PrimaryValue:N0} ₽"
            }
        ];
    }
}
