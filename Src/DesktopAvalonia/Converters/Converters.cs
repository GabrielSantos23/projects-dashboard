using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace ProjectDashboard.Avalonia.Converters;

public class StatusColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string status)
        {
            return status switch
            {
                "Active" => new SolidColorBrush(Color.Parse("#34c759")),
                "Recent" => new SolidColorBrush(Color.Parse("#0088ff")),
                "Stalled" => new SolidColorBrush(Color.Parse("#ffcc00")),
                "Archived" => new SolidColorBrush(Color.Parse("#8b8b90")),
                _ => new SolidColorBrush(Color.Parse("#8b8b90"))
            };
        }
        return new SolidColorBrush(Color.Parse("#8b8b90"));
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class StatusBgColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string status)
        {
            return status switch
            {
                "Active" => new SolidColorBrush(Color.Parse("#34c75920")),
                "Recent" => new SolidColorBrush(Color.Parse("#0088ff20")),
                "Stalled" => new SolidColorBrush(Color.Parse("#ffcc0020")),
                "Archived" => new SolidColorBrush(Color.Parse("#8b8b9020")),
                _ => new SolidColorBrush(Color.Parse("#8b8b9020"))
            };
        }
        return new SolidColorBrush(Color.Parse("#8b8b9020"));
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class TechColorConverter : IValueConverter
{
    private static readonly string[] Colors = { "#0088ff", "#ff3b30", "#ffcc00", "#34c759", "#00E5FF" };

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int index)
        {
            return new SolidColorBrush(Color.Parse(Colors[index % Colors.Length]));
        }
        return new SolidColorBrush(Color.Parse("#0088ff"));
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class StringInitialConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string s && !string.IsNullOrEmpty(s))
        {
            return s[..1].ToUpper();
        }
        return "?";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
