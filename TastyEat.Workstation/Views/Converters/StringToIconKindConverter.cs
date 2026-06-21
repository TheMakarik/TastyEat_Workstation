using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Material.Icons;

namespace TastyEat.Workstation.Views.Converters;

public sealed class StringToIconKindConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string name)
        {
            return default;
        }

        return Enum.TryParse<MaterialIconKind>(name, out var kind) ? kind : default;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value?.ToString() ?? string.Empty;
    }
}
