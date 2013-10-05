#region

using System;
using System.Globalization;
using System.Windows.Data;

#endregion

namespace ProcessTools.Core.Converters
{
    public class RangeToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (null == value)
                return false;

            int intValue = 0;
            if (value is string)
            {
                if (!Int32.TryParse((string) value, out intValue))
                    return false;
            }
            else if (value is int || value is double || value is float)
                intValue = System.Convert.ToInt32(value);
            else
                throw new InvalidOperationException("Unsupported Type [" + value.GetType().Name + "]");

            return intValue > 100;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}