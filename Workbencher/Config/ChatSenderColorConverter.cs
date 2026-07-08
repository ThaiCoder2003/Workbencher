using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Workbencher.Config
{
    public class ChatSenderColorConverter : IValueConverter
    {
        private int _userId = AppSession.Instance.CurrentUserId;
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string status)
            {

            }
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F3F4F6"));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
