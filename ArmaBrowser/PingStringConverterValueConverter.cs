using System;
using System.Globalization;
using System.Windows.Data;

namespace ArmaBrowser
{
    public class PingStringValueConverter : IValueConverter
    {
        #region Implementation of IValueConverter

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int i)
            {
                if (i > 0 && i < 999)
                    return value;
                return "--";
            }

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        #endregion
    }
}