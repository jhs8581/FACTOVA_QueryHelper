using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace FACTOVA_QueryHelper.Converters
{
    /// <summary>
    /// RowColor 문자열을 Background Brush로 변환
    /// </summary>
    public class RowColorToBackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
                return Brushes.White;

            try
            {
                var colorName = value.ToString();
                var color = (Color)ColorConverter.ConvertFromString(colorName);
                return new SolidColorBrush(color);
            }
            catch
            {
                return Brushes.White;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
