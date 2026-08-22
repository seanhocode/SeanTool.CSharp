using System.ComponentModel;

namespace SeanTool.CSharp.WPFTool.Models.Fields
{
    /// <summary>
    /// 共用欄位分析器
    /// </summary>
    /// <remarks>
    /// 分析資料來源(一般 CLR 物件，或 DataTable/DataView 繫結時的 DataRowView)的欄位結構，
    /// 產生 <see cref="FieldDescriptor"/> 清單。ModelEditor、DynamicDataGrid 皆透過此分析器取得欄位，
    /// 再各自轉換成所需的呈現內容(PropertyItem / DynamicDataGridColumnDefinition)，避免重複實作欄位掃描邏輯。
    /// </remarks>
    public static class FieldAnalyzer
    {
        /// <summary>
        /// 分析指定型別的欄位
        /// </summary>
        /// <param name="itemType">項目型別</param>
        /// <param name="sampleItem">
        /// 項目的實際實例(可為 null)。若 <paramref name="itemType"/> 是透過
        /// <see cref="ICustomTypeDescriptor"/> 動態提供屬性的型別(例如繫結 DataTable 時的
        /// DataRowView，其欄位對應到當下 DataTable 的 Columns)，必須傳入實例才能掃描到真正的欄位；
        /// 一般 CLR 型別則不需要。
        /// </param>
        public static IReadOnlyList<FieldDescriptor> Analyze(Type itemType, object? sampleItem = null)
        {
            PropertyDescriptorCollection properties = sampleItem is not null && itemType.IsInstanceOfType(sampleItem)
                ? TypeDescriptor.GetProperties(sampleItem)
                : TypeDescriptor.GetProperties(itemType);

            return properties.Cast<PropertyDescriptor>()
                .Select(property => new FieldDescriptor(property))
                .ToArray();
        }

        /// <summary>
        /// 分析單一實例的欄位
        /// </summary>
        /// <remarks>供只需編輯/掃描單一物件的情境使用(例如 ModelEditor)，實例可以是一般 CLR 物件，
        /// 也可以是繫結 DataTable 時取得的單一 DataRowView。</remarks>
        public static IReadOnlyList<FieldDescriptor> Analyze(object instance)
        {
            return Analyze(instance.GetType(), instance);
        }
    }
}
