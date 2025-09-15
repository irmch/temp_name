using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace L2Market.UI.Converters
{
    public class BooleanToColorConverter : IValueConverter
    {
        public Brush TrueValue { get; set; } = Brushes.Red;
        public Brush FalseValue { get; set; } = Brushes.Green;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? TrueValue : FalseValue;
            }
            return FalseValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
