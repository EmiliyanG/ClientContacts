﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace ClientContactViews
{

    public class CollectionViewGroupToNegatedVisibilityConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is CollectionViewGroup pgd)
            {
                return pgd.IsBottomLevel == true ? Visibility.Visible : Visibility.Collapsed;
            }
            else
            {
                throw new InvalidOperationException("expected object of type: CollectionViewGroup");
            }


        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

    }

}
