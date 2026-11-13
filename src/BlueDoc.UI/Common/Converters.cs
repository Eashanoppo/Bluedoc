using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using BlueDoc.Core.Enums;

namespace BlueDoc.UI.Common
{
    public class StatusToBrushConverter : IValueConverter
    {
        private static readonly SolidColorBrush PassedBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981"));
        private static readonly SolidColorBrush WarningBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F59E0B"));
        private static readonly SolidColorBrush FailedBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444"));
        private static readonly SolidColorBrush InconclusiveBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#64748B"));
        private static readonly SolidColorBrush NotSupportedBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#475569"));
        private static readonly SolidColorBrush NeutralBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#334155"));

        static StatusToBrushConverter()
        {
            PassedBrush.Freeze();
            WarningBrush.Freeze();
            FailedBrush.Freeze();
            InconclusiveBrush.Freeze();
            NotSupportedBrush.Freeze();
            NeutralBrush.Freeze();
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DiagnosticStatus diagStatus)
            {
                switch (diagStatus)
                {
                    case DiagnosticStatus.Passed: return PassedBrush;
                    case DiagnosticStatus.Warning: return WarningBrush;
                    case DiagnosticStatus.Failed: return FailedBrush;
                    case DiagnosticStatus.Inconclusive: return InconclusiveBrush;
                    case DiagnosticStatus.NotSupported: return NotSupportedBrush;
                    default: return NeutralBrush;
                }
            }
            else if (value is AdapterStatus adapterStatus)
            {
                switch (adapterStatus)
                {
                    case AdapterStatus.Operational: return PassedBrush;
                    case AdapterStatus.Disabled: return WarningBrush;
                    case AdapterStatus.DeviceError: return FailedBrush;
                    case AdapterStatus.DriverMissing: return FailedBrush;
                    case AdapterStatus.NotPresent: return NotSupportedBrush;
                    default: return NeutralBrush;
                }
            }

            return NeutralBrush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class StatusToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DiagnosticStatus status)
            {
                switch (status)
                {
                    case DiagnosticStatus.Passed: return "PASSED";
                    case DiagnosticStatus.Warning: return "WARNING";
                    case DiagnosticStatus.Failed: return "FAILED";
                    case DiagnosticStatus.Inconclusive: return "INCONCLUSIVE";
                    case DiagnosticStatus.NotSupported: return "NOT SUPPORTED";
                    case DiagnosticStatus.NotTested: return "NOT TESTED";
                }
            }
            return value?.ToString() ?? "UNKNOWN";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool val = value is bool b && b;
            if (parameter is string p && p.Equals("Inverse", StringComparison.OrdinalIgnoreCase))
            {
                val = !val;
            }
            return val ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class NullToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isNull = value == null;
            if (parameter is string p && p.Equals("Inverse", StringComparison.OrdinalIgnoreCase))
            {
                return isNull ? Visibility.Visible : Visibility.Collapsed;
            }
            return isNull ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BoolToSuccessConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b)
            {
                return b ? "Success" : "Failed";
            }
            return "Unknown";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

