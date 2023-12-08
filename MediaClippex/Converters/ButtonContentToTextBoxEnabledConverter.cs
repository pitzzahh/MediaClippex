using System;
using System.Globalization;
using System.Windows.Data;

namespace MediaClippex.Converters;

public class ButtonContentToTextBoxEnabledConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value?.ToString() == "Save";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return (bool)value! ? "Save" : "Change";
    }
}