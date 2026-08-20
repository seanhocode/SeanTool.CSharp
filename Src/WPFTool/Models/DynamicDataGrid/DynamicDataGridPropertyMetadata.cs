using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace SeanTool.CSharp.WPF
{
    public sealed class DynamicDataGridPropertyMetadata
    {
        public string Name { get; }
        public string Header { get; }
        public Type PropertyType { get; }
        public bool IsReadOnly { get; }
        public int Order { get; }

        private DynamicDataGridPropertyMetadata(PropertyInfo property)
        {
            Name = property.Name;
            Header = property.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName ?? property.Name;
            PropertyType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
            IsReadOnly = property.SetMethod is null;
            Order = property.GetCustomAttribute<DisplayAttribute>()?.Order ?? int.MaxValue;
        }

        public static IReadOnlyList<DynamicDataGridPropertyMetadata> Create(Type itemType)
        {
            return itemType.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(property => property.GetIndexParameters().Length == 0)
                .Select(property => new DynamicDataGridPropertyMetadata(property))
                .OrderBy(property => property.Order)
                .ThenBy(property => property.Name)
                .ToArray();
        }
    }
}
