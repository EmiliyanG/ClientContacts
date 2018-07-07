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
        private static double bottomMargin = 5;
        private static double leftMargin = 15;

        public object Convert(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null)
            {
                return new Thickness(0, 0, 0, bottomMargin);
            }
            else
            {
                return (String)value == ""
                                           ? new Thickness(0, 0, 0, bottomMargin)
                                           : new Thickness(leftMargin, 0, 0, bottomMargin);
            }


            
        }

        public object ConvertBack(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }
}
