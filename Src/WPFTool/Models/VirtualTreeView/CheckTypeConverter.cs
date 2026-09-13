using System.Globalization;
using System.Windows.Data;
using SeanTool.CSharp.WPFTool;

namespace SeanTool.CSharp.WPFTool
{
    public class CheckTypeConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value is CheckType checkType
                ? checkType switch
                {
                    CheckType.All => true,
                    CheckType.HasValue => null,
                    _ => false
                }
                : false;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value switch
            {
                true => CheckType.All,
                _ => CheckType.None
            };
        }
    }
}
