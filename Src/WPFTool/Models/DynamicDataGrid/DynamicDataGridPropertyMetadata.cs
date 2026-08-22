using SeanTool.CSharp.WPFTool.Models.Fields;

namespace SeanTool.CSharp.WPFTool.Models.DynamicDataGrid
{
    public sealed class DynamicDataGridPropertyMetadata
    {
        public string Name { get; }
        public string Header { get; }
        public Type PropertyType { get; }
        public bool IsReadOnly { get; }
        public int Order { get; }

        private DynamicDataGridPropertyMetadata(FieldDescriptor field)
        {
            Name = field.Name;
            Header = field.DisplayName;
            PropertyType = field.NonNullableType;
            IsReadOnly = field.IsReadOnly;
            Order = field.Order;
        }

        /// <summary>
        /// 掃描項目型別的欄位
        /// </summary>
        /// <param name="itemType">項目型別</param>
        /// <param name="sampleItem">
        /// 項目的實際實例（可為 null）。若 <paramref name="itemType"/> 是動態提供屬性的型別
        /// （例如繫結 <see cref="System.Data.DataTable"/> 時，項目型別會是 <see cref="System.Data.DataRowView"/>，
        /// 其欄位對應到 DataTable 的 Columns，只能透過實例的 <see cref="System.ComponentModel.ICustomTypeDescriptor"/> 取得），
        /// 必須傳入實例才能掃描到真正的欄位；一般 CLR 型別則不需要。
        /// </param>
        /// <remarks>實際欄位掃描邏輯共用 <see cref="FieldAnalyzer"/>，此處僅負責轉換成 DynamicDataGrid 所需的呈現順序(Order/Name)。</remarks>
        public static IReadOnlyList<DynamicDataGridPropertyMetadata> Create(Type itemType, object? sampleItem = null)
        {
            return FieldAnalyzer.Analyze(itemType, sampleItem)
                .Select(field => new DynamicDataGridPropertyMetadata(field))
                .OrderBy(property => property.Order)
                .ThenBy(property => property.Name)
                .ToArray();
        }
    }
}
