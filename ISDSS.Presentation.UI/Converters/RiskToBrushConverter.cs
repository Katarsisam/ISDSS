using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia;

using System.Collections.Generic;

namespace ISDSS.Presentation.UI.Converters;

public class RiskToBrushConverter : IMultiValueConverter
{
    public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values == null || values.Count < 2)
            return AvaloniaProperty.UnsetValue;

        if (values[0] is not decimal risk)
            return AvaloniaProperty.UnsetValue;

        decimal threshold = 75m;

        var t = values[1];
        switch (t)
        {
            case decimal d: threshold = d; break;
            case double db: threshold = (decimal)db; break;
            case float fl: threshold = (decimal)fl; break;
            case int i: threshold = i; break;
            case string s when decimal.TryParse(s, out var p): threshold = p; break;
        }

        if (risk >= threshold)
            return new SolidColorBrush(Color.FromRgb(0xFF, 0xE5, 0xE5)); 

        if (risk >= threshold - 25)
            return new SolidColorBrush(Color.FromRgb(0xFF, 0xF7, 0xE0)); 

        return AvaloniaProperty.UnsetValue;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
