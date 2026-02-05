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
            {
                // 🔥 RowColor가 없으면 투명 반환 (AlternatingRowBackground가 적용됨)
                return Brushes.Transparent;
            }

            try
            {
                var colorName = value.ToString();
                var color = (Color)ColorConverter.ConvertFromString(colorName!);
                var brush = new SolidColorBrush(color);
                brush.Freeze(); // 🔥 성능 최적화
                return brush;
            }
            catch (Exception ex)
            {
return Brushes.Transparent;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
