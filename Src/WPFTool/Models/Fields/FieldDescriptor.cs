using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace SeanTool.CSharp.WPFTool.Models.Fields
{
    /// <summary>
    /// 欄位描述
    /// </summary>
    /// <remarks>
    /// 包裝 <see cref="PropertyDescriptor"/>，統一「一般 CLR 物件屬性」與「DataTable(DataRowView) 動態欄位」的
    /// 讀取/寫入/中繼資料存取方式，供 <see cref="FieldAnalyzer"/> 產生。
    /// </remarks>
    public sealed class FieldDescriptor
    {
        private readonly PropertyDescriptor _property;

        /// <summary>
        /// 欄位名稱
        /// </summary>
        public string Name => _property.Name;

        /// <summary>
        /// 顯示名稱 (優先採用 DisplayNameAttribute，否則使用 Name)
        /// </summary>
        public string DisplayName => _property.DisplayName;

        /// <summary>
        /// 欄位型別 (原始型別，Nullable&lt;T&gt; 不會被展開)
        /// </summary>
        public Type PropertyType => _property.PropertyType;

        /// <summary>
        /// 欄位型別展開 Nullable 後的型別 (例如 int? 會回傳 int)
        /// </summary>
        public Type NonNullableType { get; }

        /// <summary>
        /// 是否唯讀
        /// </summary>
        public bool IsReadOnly => _property.IsReadOnly;

        /// <summary>
        /// 顯示順序 (來自 DisplayAttribute.Order，未標記時為 int.MaxValue)
        /// </summary>
        public int Order { get; }

        internal FieldDescriptor(PropertyDescriptor property)
        {
            _property = property;
            NonNullableType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
            Order = (property.Attributes[typeof(DisplayAttribute)] as DisplayAttribute)?.Order ?? int.MaxValue;
        }

        /// <summary>
        /// 取得指定實例此欄位的值
        /// </summary>
        public object? GetValue(object instance) => _property.GetValue(instance);

        /// <summary>
        /// 將值寫入指定實例此欄位
        /// </summary>
        public void SetValue(object instance, object? value) => _property.SetValue(instance, value);

        /// <summary>
        /// 取得此欄位標記的自訂 Attribute
        /// </summary>
        public T? GetAttribute<T>() where T : Attribute => _property.Attributes[typeof(T)] as T;
    }
}
