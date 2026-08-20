using System.Collections;
using System.Globalization;
using System.Reflection;

namespace SeanTool.CSharp.WPF
{
    public enum DynamicDataGridFilterOperator
    {
        Contains,
        StartsWith,
        Equals,
        GreaterThan,
        LessThan,
        IsNull,
        IsNotNull
    }

    public sealed class DynamicDataGridFilter
    {
        public string PropertyName { get; init; } = string.Empty;
        public DynamicDataGridFilterOperator Operator { get; init; }
        public object? Value { get; init; }
    }

    public static class DynamicDataGridQuery
    {
        public static IEnumerable Apply(
            IEnumerable source,
            Type itemType,
            IEnumerable<DynamicDataGridFilter>? filters = null,
            string? sortProperty = null,
            bool descending = false)
        {
            IEnumerable<object?> result = source.Cast<object?>();

            foreach (DynamicDataGridFilter filter in filters ?? Enumerable.Empty<DynamicDataGridFilter>())
            {
                PropertyInfo? property = itemType.GetProperty(filter.PropertyName);
                if (property is null)
                {
                    continue;
                }

                result = result.Where(item => Matches(item, property, filter));
            }

            if (!string.IsNullOrWhiteSpace(sortProperty))
            {
                PropertyInfo? property = itemType.GetProperty(sortProperty);
                if (property is not null)
                {
                    result = descending
                        ? result.OrderByDescending(item => property.GetValue(item), Comparer<object?>.Create(Compare))
                        : result.OrderBy(item => property.GetValue(item), Comparer<object?>.Create(Compare));
                }
            }

            return result.ToArray();
        }

        private static bool Matches(object? item, PropertyInfo property, DynamicDataGridFilter filter)
        {
            object? actual = item is null ? null : property.GetValue(item);
            try
            {
                return filter.Operator switch
                {
                    DynamicDataGridFilterOperator.IsNull => actual is null,
                    DynamicDataGridFilterOperator.IsNotNull => actual is not null,
                    DynamicDataGridFilterOperator.Contains => actual?.ToString()?.Contains(filter.Value?.ToString() ?? string.Empty, StringComparison.CurrentCultureIgnoreCase) == true,
                    DynamicDataGridFilterOperator.StartsWith => actual?.ToString()?.StartsWith(filter.Value?.ToString() ?? string.Empty, StringComparison.CurrentCultureIgnoreCase) == true,
                    DynamicDataGridFilterOperator.Equals => Compare(actual, ConvertValue(filter.Value, property.PropertyType)) == 0,
                    DynamicDataGridFilterOperator.GreaterThan => Compare(actual, ConvertValue(filter.Value, property.PropertyType)) > 0,
                    DynamicDataGridFilterOperator.LessThan => Compare(actual, ConvertValue(filter.Value, property.PropertyType)) < 0,
                    _ => true
                };
            }
            catch (FormatException)
            {
                return false;
            }
            catch (InvalidCastException)
            {
                return false;
            }
            catch (OverflowException)
            {
                return false;
            }
        }

        private static object? ConvertValue(object? value, Type targetType)
        {
            if (value is null || value is string { Length: 0 })
            {
                return null;
            }

            Type nonNullableType = Nullable.GetUnderlyingType(targetType) ?? targetType;
            if (value is not null && nonNullableType.IsInstanceOfType(value))
            {
                return value;
            }
            if (nonNullableType.IsEnum)
            {
                return value is string text
                    ? Enum.Parse(nonNullableType, text, true)
                    : Enum.ToObject(nonNullableType, value!);
            }

            return value is string stringValue
                ? Convert.ChangeType(stringValue, nonNullableType, CultureInfo.CurrentCulture)
                : Convert.ChangeType(value, nonNullableType, CultureInfo.CurrentCulture);
        }

        private static int Compare(object? left, object? right)
        {
            if (left is null) return right is null ? 0 : -1;
            if (right is null) return 1;
            return left is IComparable comparable
                ? comparable.CompareTo(right)
                : string.Compare(left.ToString(), right.ToString(), StringComparison.CurrentCultureIgnoreCase);
        }
    }
}
