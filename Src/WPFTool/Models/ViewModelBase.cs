using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SeanTool.CSharp.WPFTool.Models
{
    public class ViewModelBase : INotifyPropertyChanged
    {
        /// <summary>
        /// 屬性變更通知後觸發
        /// </summary>
        /// <remarks>
        /// <para>對外的通知接口</para>
        /// <para>當 WPF Binding Engine 偵測到物件實作 INotifyPropertyChanged 時，會觸發此事件，並在指定屬性變更時更新對應的 Binding 目標。</para>
        /// <para>屬性改變時若需要執行其他邏輯，也可以由外部訂閱此事件處理。</para>
        /// </remarks>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// 屬性變更通知 (供 Setter 呼叫)
        /// </summary>
        /// <remarks>
        /// 當屬性值改變時，通知所有訂閱者 (如 WPF UI) 進行更新
        /// </remarks>
        /// <param name="name">發生變更的屬性名稱</param>
        // [CallerMemberName]: 在 Setter 呼叫 OnPropertyChanged 時，不用手動打字串名稱
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
