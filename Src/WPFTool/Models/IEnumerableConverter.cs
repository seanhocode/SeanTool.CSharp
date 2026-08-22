using System.Collections;
using System.ComponentModel;

namespace SeanTool.CSharp.WPFTool.Models
{
    /// <summary>
    /// IEnumerable 轉換器
    /// </summary>
    /// <remarks>
    /// DataTable/DataSet 實作的是 <see cref="IListSource"/> 而非 <see cref="IEnumerable"/>，
    /// 需透過 GetList() 取得其 DefaultView 才能被列舉，有支援 DataTable/DataSet 統一在此處理可避免各自實作，也避免外界忘記自行轉換為 DefaultView
    /// </remarks>
    public static class IEnumerableConverter
    {
        /// <summary>
        /// 將傳入的資料來源轉換為可列舉的集合
        /// </summary>
        /// <param name="value">資料來源，可為 null、IEnumerable，或 DataTable/DataSet 等 IListSource</param>
        /// <returns>可列舉的集合；傳入 null 則回傳 null</returns>
        public static IEnumerable? Convert(object? value) => value switch
        {
            null => null,
            IListSource listSource => listSource.GetList(),
            IEnumerable enumerable => enumerable,
            _ => throw new ArgumentException($"DataSource 必須是 IEnumerable 或 IListSource，實際型別為 {value.GetType()}", nameof(value))
        };
    }
}
