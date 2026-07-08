using System;
using System.Windows.Data;
using System.Windows.Media;

namespace Workbencher.Config
{
    public class StatusDisplayConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is string status)
            {
                // Return soft green for "Completed" and soft red for "To Do"
                if (!string.IsNullOrEmpty(status))
                {
                    if (status.Equals("Completed", StringComparison.OrdinalIgnoreCase))
                    {
                        return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D1FAE5")); // Soft green for "Completed"
                    }
                    else if (status.Equals("Overdue", StringComparison.OrdinalIgnoreCase))
                    {
                        return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FEE2E2")); // Soft red for "Overdue"
                    }
                    else if (status.Equals("In Progress", StringComparison.OrdinalIgnoreCase))
                    {
                        return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FEF3C7")); // Soft yellow for "In Progress"
                    }
                    else if (status.Equals("To Do", StringComparison.OrdinalIgnoreCase))
                    {
                        return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E0E7FF")); // Soft blue for "To Do"
                    }
                }
            }
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F3F4F6")); // Default soft gray for any non-string value
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
