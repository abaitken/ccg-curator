using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace CCGCurator
{
    class BitmapValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return (BitmapImage) null;
            var source = (Bitmap) value;

            return source.Convert();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}