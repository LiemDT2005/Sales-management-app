using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace SMA.Converters
{
    class GenderToBoolConverter : IValueConverter
    {
        // Chuyển từ Gender ("Male"/"Female") => bool cho RadioButton
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null) return false;
            return value.ToString().Equals(parameter.ToString(), StringComparison.OrdinalIgnoreCase);
        }

        // Chuyển ngược từ RadioButton.IsChecked => Gender string
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((bool)value)
                return parameter.ToString();
            return Binding.DoNothing;
        }
    }
}