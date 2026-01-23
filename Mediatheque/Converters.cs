using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace Mediatheque;

/// <summary>
/// Convertit un booléen en couleur de fond pour le calendrier
/// </summary>
public class BoolToBackgroundConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool estAujourdhui && estAujourdhui)
        {
            // Jour actuel : fond orange clair
            return new SolidColorBrush(Color.FromRgb(255, 220, 180));
        }

        // Par défaut : gris clair pour semaine, jaune clair pour weekend
        string? param = parameter?.ToString();
        if (param == "weekend")
        {
            return new SolidColorBrush(Colors.LightYellow);
        }

        return new SolidColorBrush(Colors.LightGray);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

/// <summary>
/// Convertit un booléen en Visibility (true = Visible, false = Collapsed)
/// </summary>
public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue && boolValue)
        {
            return Visibility.Visible;
        }

        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

/// <summary>
/// Convertit null en Visibility (non-null = Visible, null = Collapsed)
/// </summary>
public class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value != null ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

/// <summary>
/// Convertit null en Visibility inverse (null = Visible, non-null = Collapsed)
/// </summary>
public class InverseNullToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value == null ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}