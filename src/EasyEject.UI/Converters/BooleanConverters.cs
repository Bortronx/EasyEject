using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace EasyEject.UI.Converters;

/// <summary>
/// Converts a boolean to a <see cref="Visibility"/> value.
/// </summary>
public sealed class BooleanToVisibilityConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        bool flag = value is bool b && b;
        return flag ? Visibility.Visible : Visibility.Collapsed;
    }

    /// <inheritdoc />
    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        return value is Visibility.Visible;
    }
}

/// <summary>
/// Converts a boolean to an inverted <see cref="Visibility"/> value.
/// </summary>
public sealed class InverseBooleanToVisibilityConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        bool flag = value is bool b && b;
        return flag ? Visibility.Collapsed : Visibility.Visible;
    }

    /// <inheritdoc />
    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        return value is not Visibility.Visible;
    }
}
