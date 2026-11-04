using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace RoSpriteEnhancer.Views.Converters
{
    /// <summary>
    /// Konvertiert einen Bool-Wert zur umgekehrten Sichtbarkeit:
    /// true → Collapsed, false → Visible.
    /// </summary>
    [ValueConversion(typeof(bool), typeof(Visibility))]
    public class InverseBoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b)
                return b ? Visibility.Collapsed : Visibility.Visible;
            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility v)
                return v == Visibility.Collapsed;
            return false;
        }
    }
}