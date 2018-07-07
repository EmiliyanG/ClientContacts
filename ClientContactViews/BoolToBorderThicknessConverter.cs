using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;
using System.Windows;

namespace ClientContactViews
{
    public class BoolToBorderThicknessConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool)
            {
               
                return (bool)value == true ? new Thickness(0, 0, 0, 0) : new Thickness(5, 0, 0, 0);
            }
            else
            {
                throw new InvalidOperationException("value should be boolean");
                
            }

        }


        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

    }
}
