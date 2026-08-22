using System;
using System.Collections.Generic;
using Avalonia.Media;

namespace TastyEat.Workstation.Ui;

public static class ChartColors
{
    public static readonly IReadOnlyList<KeyValuePair<string, Color>> Palette =
    [
        new("ChartColor1", Color.Parse("#512BD4")),
        new("ChartColor2", Color.Parse("#E3405F")),
        new("ChartColor3", Color.Parse("#F2A93B")),
        new("ChartColor4", Color.Parse("#2B95D4")),
        new("ChartColor5", Color.Parse("#3BD9A1")),
        new("ChartColor6", Color.Parse("#8E44AD")),
        new("ChartColor7", Color.Parse("#D35400")),
        new("ChartColor8", Color.Parse("#27AE60")),
        new("ChartColor9", Color.Parse("#C0392B")),
        new("ChartColor10", Color.Parse("#16A085"))
    ];
}
