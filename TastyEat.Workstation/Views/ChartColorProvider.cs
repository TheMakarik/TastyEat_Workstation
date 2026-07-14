using Avalonia;
using Avalonia.Media;
using SkiaSharp;

namespace TastyEat.Workstation.Views;

public static class ChartColorProvider
{
    private static readonly SKColor[] DefaultColors =
    [
        new(0x51, 0x2B, 0xD4),
        new(0xE3, 0x40, 0x5F),
        new(0xF2, 0xA9, 0x3B),
        new(0x2B, 0x95, 0xD4),
        new(0x3B, 0xD9, 0xA1),
        new(0x8E, 0x44, 0xAD),
        new(0xD3, 0x54, 0x00),
        new(0x27, 0xAE, 0x60),
        new(0xC0, 0x39, 0x2B),
        new(0x16, 0xA0, 0x85)
    ];

    public static SKColor[] GetColors()
    {
        if (Application.Current?.Resources is null)
            return DefaultColors;

        var colors = new List<SKColor>();
        for (var i = 1; i <= 10; i++)
        {
            if (Application.Current.Resources.TryGetResource($"ChartColor{i}", null, out var value) && value is Color color)
                colors.Add(new SKColor(color.R, color.G, color.B, color.A));
        }

        return colors.Count > 0 ? colors.ToArray() : DefaultColors;
    }
}
