using SeanTool.CSharp.WPFTool.Enums.Filter;
using System.Collections.Generic;

namespace SeanTool.CSharp.WPFTool.Models.Filter
{
    /// <summary>
    /// 篩選條件
    /// </summary>
    public class FilterCondition
    {
        /// <summary>
        /// TreeView 搜尋可用的運算子
        /// </summary>
        public static readonly IReadOnlyList<FilterOperator> TreeOperators = new[]
        {
            FilterOperator.Contains,
            FilterOperator.StartsWith,
            FilterOperator.Equals
        };

        /// <summary>
        /// 文字條件可用的運算子
        /// </summary>
        public static readonly IReadOnlyList<FilterOperator> TextOperators = new[]
        {
            FilterOperator.Contains,
            FilterOperator.StartsWith,
            FilterOperator.Equals,
            FilterOperator.GreaterThan,
            FilterOperator.LessThan,
            FilterOperator.IsNull,
            FilterOperator.IsNotNull
        };

        /// <summary>
        /// 日期時間條件可用的運算子
        /// </summary>
        public static readonly IReadOnlyList<FilterOperator> DateTimeOperators = new[]
        {
            FilterOperator.IsNull,
            FilterOperator.IsNotNull,
            FilterOperator.LessThanOrEqual,
            FilterOperator.GreaterThanOrEqual,
            FilterOperator.Between
        };

        public IList<FilterOperator> CustomizeOperators;

        /// <summary>
        /// 取得目前條件類型可用的運算子
        /// </summary>
        /// <param name="valueType"></param>
        /// <returns></returns>
        public IReadOnlyList<FilterOperator> GetAvailableOperators()
        {
            IList <FilterOperator> availableOperators = ValueType switch
            {
                FilterValueType.DateTime => DateTimeOperators.ToList(),
                FilterValueType.TreeNode => TreeOperators.ToList(),
                _ => TextOperators.ToList()
            };

            availableOperators = CustomizeOperators != null && CustomizeOperators.Count > 0
                ? availableOperators.Where(x => CustomizeOperators.Contains(x)).ToList()
                : availableOperators;
            
            return availableOperators.ToList();
        }

        /// <summary>
        /// 取得目前條件類型的預設運算子
        /// </summary>
        /// <returns></returns>
        public FilterOperator GetDefaultOperator()
        {
            return ValueType switch
            {
                FilterValueType.DateTime => CustomizeOperators != null && CustomizeOperators.Count > 0 ? CustomizeOperators.FirstOrDefault() : FilterOperator.GreaterThanOrEqual,
                _ => CustomizeOperators != null && CustomizeOperators.Count > 0 ? CustomizeOperators.FirstOrDefault() : FilterOperator.Contains
            };
        }

        /// <summary>
        /// 篩選目標
        /// </summary>
        public required string PropertyName { get; init; }

        /// <summary>
        /// 條件類型
        /// </summary>
        public FilterValueType ValueType { get; init; } = FilterValueType.Text;

        /// <summary>
        /// 條件運算子
        /// </summary>
        public FilterOperator Operator { get; init; }

        /// <summary>
        /// 條件值
        /// </summary>
        public object? Value { get; init; }
    }
}
