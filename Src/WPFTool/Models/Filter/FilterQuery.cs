using System.Collections;
using System.ComponentModel;
using System.Globalization;
using SeanTool.CSharp.WPFTool.Enums.Filter;

namespace SeanTool.CSharp.WPFTool.Models.Filter
{
    /// <summary>
    /// 篩選器
    /// </summary>
    public static class FilterQuery
    {
        /// <summary>
        /// 屬性快取
        /// </summary>
        /// <remarks>依 (Type, PropertyName) 快取 PropertyDescriptor，避免逐節點/逐次搜尋都重複反射查找同一個屬性</remarks>
        private static readonly Dictionary<(Type Type, string PropertyName), PropertyDescriptor?> PropertyCache = [];

        /// <summary>
        /// 互斥鎖
        /// </summary>
        private static readonly object _LockObj = new object();

        /// <summary>
        /// 取得快取的 PropertyDescriptor
        /// </summary>
        /// <param name="itemType"></param>
        /// <param name="propertyName"></param>
        /// <remarks>
        /// 目前快取中沒有的則加入快取。僅適用一般 CLR 型別：型別本身即可決定所有屬性，可安全地依 Type 快取。
        /// 若型別實作 <see cref="ICustomTypeDescriptor"/>（例如繫結 DataTable 時的 DataRowView），
        /// 屬性是由「實例」動態提供（對應 DataTable 的 Columns），不可依 Type 快取，見 <see cref="Apply"/>。
        /// </remarks>
        /// <returns></returns>
        private static PropertyDescriptor? GetCachedProperty(Type itemType, string propertyName)
        {
            (Type itemType, string propertyName) key = (itemType, propertyName);

            //確保在多執行緒的環境下，同一時間只能有一個執行緒進入此區塊
            lock (_LockObj)
            {
                if (!PropertyCache.TryGetValue(key, out PropertyDescriptor? property))
                {
                    property = TypeDescriptor.GetProperties(itemType)[propertyName];
                    PropertyCache[key] = property;
                }

                return property;
            }
        }

        /// <summary>
        /// 篩選
        /// </summary>
        /// <param name="source">欲篩選的資料</param>
        /// <param name="itemType">欲篩選的資料的型別</param>
        /// <param name="filters">篩選條件</param>
        /// <returns>篩選後的資料</returns>
        public static IEnumerable Apply(
            IEnumerable source,
            Type itemType,
            IEnumerable<FilterCondition>? filters = null)
        {
            // 實際有值的篩選條件，避免空條件造成不必要的篩選
            FilterCondition[] activeFilters = (filters ?? Enumerable.Empty<FilterCondition>()).ToArray();
            if (activeFilters.Length == 0) { return source; }

            // 將 source 轉為 object?，避免 IEnumerable<T> 的型別限制，方便後續反射取得屬性值
            object?[] items = source.Cast<object?>().ToArray();

            /* DataRowView/DataRow 等透過 ICustomTypeDescriptor 動態提供欄位的型別(例如繫結 DataTable)，
             * 欄位是由「實例」而非型別本身決定(對應到當下繫結的 DataTable 的 Columns)，故不能用
             * GetCachedProperty 的 Type-based 快取，改為每次從資料中取一筆實例現查。
             */
            bool isCustomDescribed = typeof(ICustomTypeDescriptor).IsAssignableFrom(itemType);
            object? sample = isCustomDescribed ? items.FirstOrDefault(item => item is not null) : null;
            PropertyDescriptorCollection? customProperties = isCustomDescribed
                ? (sample is not null ? TypeDescriptor.GetProperties(sample) : TypeDescriptor.GetProperties(itemType))
                : null;

            IEnumerable<object?> result = items;

            // 逐一套用篩選條件
            foreach (FilterCondition filter in activeFilters)
            {
                //取得篩選條件目標是 source 的哪個屬性，若找不到則跳過此條件
                PropertyDescriptor? property = customProperties?[filter.PropertyName]
                    ?? (isCustomDescribed ? null : GetCachedProperty(itemType, filter.PropertyName));
                if (property is null) { continue; }

                // 每個 filter 只轉換一次目標值，避免每個 item 都重複解析/轉型同一個值
                (object? convertedValue, bool conversionFailed) = TryConvertValue(filter.Value, property.PropertyType);
                result = result.Where(item => Matches(item, property, filter, convertedValue, conversionFailed));
            }

            return result.ToArray();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item">目標欄位(被篩選的欄位)</param>
        /// <param name="property">目標欄位的型別</param>
        /// <param name="filter">篩選條件</param>
        /// <param name="convertedValue">轉換為目標欄位型別的篩選條件值</param>
        /// <param name="conversionFailed">是否轉換失敗</param>
        /// <returns></returns>
        private static bool Matches(object? item, PropertyDescriptor property, FilterCondition filter, object? convertedValue, bool conversionFailed)
        {
            object? actual = item is null ? null : property.GetValue(item);
            try
            {
                return filter.Operator switch
                {
                    FilterOperator.IsNull => actual is null,
                    FilterOperator.IsNotNull => actual is not null,
                    FilterOperator.Contains => string.IsNullOrEmpty(filter.Value?.ToString()) || actual?.ToString()?.Contains(filter.Value!.ToString()!, StringComparison.CurrentCultureIgnoreCase) == true,
                    FilterOperator.StartsWith => string.IsNullOrEmpty(filter.Value?.ToString()) || actual?.ToString()?.StartsWith(filter.Value!.ToString()!, StringComparison.CurrentCultureIgnoreCase) == true,
                    FilterOperator.Equals => !conversionFailed && Compare(actual, convertedValue) == 0,
                    FilterOperator.GreaterThan => !conversionFailed && Compare(actual, convertedValue) > 0,
                    FilterOperator.LessThan => !conversionFailed && Compare(actual, convertedValue) < 0,
                    FilterOperator.GreaterThanOrEqual => !conversionFailed && Compare(actual, convertedValue) >= 0,
                    FilterOperator.LessThanOrEqual => !conversionFailed && Compare(actual, convertedValue) <= 0,
                    FilterOperator.Between => filter.Value is DateRange range && IsWithinRange(actual, range),
                    _ => true //未知的 Operator，視為不篩選，避免 filter 定義錯誤造成整個篩選失敗
                };
            }
            catch (Exception)
            {
                // ponytail: 任何轉型/比較例外(含無效 Enum 文字的 ArgumentException)一律視為不匹配，
                // 避免單一壞條件讓整個 filter 結果炸掉。這裡是唯一進入點，攔截點不需要下放到每個呼叫者。
                return false;
            }
        }

        /// <summary>
        /// 嘗試將篩選條件值轉換為目標型別
        /// </summary>
        /// <param name="value">篩選條件值</param>
        /// <param name="targetType">目標欄位型別</param>
        /// <remarks>若轉換失敗則清空篩選值並標記失敗</remarks>
        /// <returns><para>Value : 轉換為目標欄位型別的篩選條件值 </para><para>Failed : 是否轉換失敗</para></returns>
        private static (object? Value, bool Failed) TryConvertValue(object? value, Type targetType)
        {
            try { return (ConvertValue(value, targetType), false); }
            catch (Exception) { return (null, true); }
        }

        /// <summary>
        /// 將篩選條件值轉換為目標型別
        /// </summary>
        /// <param name="value">篩選條件值</param>
        /// <param name="targetType">目標欄位型別</param>
        /// <returns>轉換為目標欄位型別的篩選條件值</returns>
        private static object? ConvertValue(object? value, Type targetType)
        {
            // 針對 null 或空字串，直接回傳 null，避免後續轉型失敗
            if (value is null || value is string { Length: 0 }) { return null; }

            //嘗試取得目標類型最終的非 Nullable 型別，若是 Nullable<T> 則取得 T，否則直接使用 targetType
            Type nonNullableType = Nullable.GetUnderlyingType(targetType) ?? targetType;

            //若 value 已經是目標型別，直接回傳，避免不必要的轉型
            if (value is not null && nonNullableType.IsInstanceOfType(value)) { return value; }

            // 若目標型別是 Enum，則嘗試將 value 轉換為 Enum
            if (nonNullableType.IsEnum)
            {
                return value is string text
                    ? Enum.Parse(nonNullableType, text, true) //從字串取得 Enum，忽略大小寫
                    : Enum.ToObject(nonNullableType, value!); //從數值取得 Enum
            }

            // 若 value 是字串，則使用 Convert.ChangeType 轉換為目標型別，否則直接使用 Convert.ChangeType 轉換
            return value is string stringValue
                ? Convert.ChangeType(stringValue, nonNullableType, CultureInfo.CurrentCulture)
                : Convert.ChangeType(value, nonNullableType, CultureInfo.CurrentCulture);
        }

        
        /// <summary>
        /// 判斷指定的值是否落在給定的日期區間內
        /// </summary>
        /// <param name="actual">目標欄位值</param>
        /// <param name="range">包含起始與結束的日期區間的篩選條件值</param>
        /// <returns>若值符合區間條件則回傳 true；若超出範圍或實際值為 null 則回傳 false。</returns>
        /// <remarks>
        /// <para>1. 若實際值為 null 則 false</para>
        /// <para>2. 若 range.From 或 range.To 為 null，則該邊界將不作限制</para>
        /// </remarks>
        private static bool IsWithinRange(object? actual, DateRange range)
        {
            if (actual is null) return false;
            if (range.From is DateTime from && Compare(actual, from) < 0) return false;
            if (range.To is DateTime to && Compare(actual, to) > 0) return false;
            return true;
        }

        /// <summary>
        /// 比較兩個物件的大小，主要用於篩選器的條件判斷 (大於、小於、等於)
        /// </summary>
        /// <param name="left">目標欄位值</param>
        /// <param name="right">篩選條件值</param>
        /// <returns>
        /// <para>小於 0：左側小於右側</para>
        /// <para>等於 0：兩者相等</para>
        /// <para>大於 0：左側大於右側</para>
        /// </returns>
        /// <remarks>
        /// 比較規則：
        /// <para>1. 兩者皆為 null 視為相等(0)；null 永遠小於非 null (-1)</para>
        /// <para>2. 若左側實作了 <see cref="IComparable"/> (如數字、日期、Enum)，優先使用型別本身的比較邏輯</para>
        /// <para>3. 若未實作 IComparable，則一律轉為字串，並進行「忽略大小寫」的文字比對</para>
        /// </remarks>
        private static int Compare(object? left, object? right)
        {
            if (left is null) return right is null ? 0 : -1;
            if (right is null) return 1;
            return left is IComparable comparable
                ? comparable.CompareTo(right)
                : string.Compare(left.ToString(), right.ToString(), StringComparison.CurrentCultureIgnoreCase);//
        }
    }
}
