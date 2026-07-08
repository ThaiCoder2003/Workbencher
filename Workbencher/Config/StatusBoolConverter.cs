using System;
using System.Globalization;
using System.Windows.Data;
namespace Workbencher.Config
{
    public class StatusBoolConverter : IValueConverter
    {
        // 1. Convert: Database String -> UI CheckBox
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string status)
            {
                // Returns true if status is "Completed" (ignoring capitalization)
                return status.Equals("Completed", StringComparison.OrdinalIgnoreCase);
            }
            return false;
        }

        // 2. ConvertBack: UI CheckBox -> Database String
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isChecked)
            {
                return isChecked ? "Completed" : "To Do";
            }
            return "To Do";
        }
    }
}
