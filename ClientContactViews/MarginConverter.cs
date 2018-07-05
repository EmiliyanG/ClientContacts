using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace ClientContactViews
{
    public class MarginConverter : IValueConverter
    {

        public object Convert(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null)
            {
                return new Thickness(System.Convert.ToDouble(0), 0, 0, 5);
            }
            else
            {
                return (String)value == ""
                                           ? new Thickness(System.Convert.ToDouble(0), 0, 0, 5)
                                           : new Thickness(System.Convert.ToDouble(20), 0, 0, 5);
            }


            
        }

        public object ConvertBack(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }
}
