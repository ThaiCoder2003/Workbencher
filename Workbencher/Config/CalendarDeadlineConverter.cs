using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using System.Windows.Media;
using Workbencher.Database.Models;

namespace Workbencher.Config
{
    public class CalendarDeadlineConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // values[0] is the current Day Button's Date
            // values[1] is the collection of tasks with deadlines
            if (values[0] is DateTime date && values[1] is IEnumerable<TaskItem> tasks)
            {
                bool hasDeadline = tasks.Any(t => t.Deadline.Date == date.Date);
                if (hasDeadline)
                {
                    // Clean Soft Red accent background for deadline days
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FEE2E2"));
                }
            }
            // Return transparent/default if no deadline exists
            return Brushes.Transparent;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
