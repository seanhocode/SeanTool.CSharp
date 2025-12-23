using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SeanTool.CSharp.WPF
{
    public class ViewModelBase : INotifyPropertyChanged
    {
        /// <summary>
        /// 屬性變更通知事件
        /// </summary>
        /// <remarks>
        /// <para>對外的接口 (Socket)</para>
        /// <para>當 WPF 的 Binding Engine 偵測到此介面時，會將「更新 UI 的邏輯」訂閱 (掛載) 到此事件上</para>
        /// </remarks>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// 觸發屬性變更通知 (供 Setter 呼叫)
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
