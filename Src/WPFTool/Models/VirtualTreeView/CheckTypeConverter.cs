using System.Globalization;
using System.Windows.Data;
using SeanTool.CSharp.WPFTool.Enums.VirtualTreeView;

namespace SeanTool.CSharp.WPFTool.Models.VirtualTreeView
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