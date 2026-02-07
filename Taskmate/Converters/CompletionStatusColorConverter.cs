using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Taskmate
{
    public class CompletionStatusColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double completionPercentage)
            {
                if (completionPercentage >= 100)
                {
                    // Green - Complete
                    return new SolidColorBrush(Color.FromRgb(76, 175, 80));  // #4caf50
                }
                else if (completionPercentage > 0)
                {
                    // Yellow - Partial
                    return new SolidColorBrush(Color.FromRgb(255, 152, 0));   // #ff9800
                }
                else
                {
                    // Red - Incomplete
                    return new SolidColorBrush(Color.FromRgb(244, 67, 54));   // #f44336
                }
            }

            // Default - Gray
            return new SolidColorBrush(Color.FromRgb(158, 158, 158));  // #9e9e9e
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
