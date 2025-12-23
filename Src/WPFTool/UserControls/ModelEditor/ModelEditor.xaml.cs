using System.Windows;
using System.Windows.Controls;

namespace SeanTool.CSharp.WPF
{
    /// <summary>
    /// ModelEditorView.xaml 的互動邏輯
    /// </summary>
    public partial class ModelEditor : UserControl
    {
        public ModelEditor()
        {
            InitializeComponent();
        }
        /**
         * DataContext: 將指定 Object 與 xaml 綁定(由內而外找資料(我要綁定誰？我去哪裡拿資料？))
         * Dependency Property: 讓呼叫者可以與自己的屬性綁定(由外而內開接口(我允許別人把資料灌進這個屬性裡))
         */


        /**
         * 相依屬性的用處： 讓呼叫者可以與自己的屬性綁定
         * 要使用如 Data Binding、Styling、Templating 等功能
         * 必須要繼承 DependencyObject 類別，並且把屬性實作成 DependencyProperty
         * (不需要特別設定相依物件，因為 WPF 內建的控件全部都繼承了 DependencyObject)
         * 
         * 相依屬性並不存在於 UserControl 裡，而是註冊進 DependencyProperty 類別
         * (WPF 中所有的控件中的相依屬性全部都交由 DependencyProperty 統籌管理)
         */

        /**
         * 1. 定義相依屬性 (Dependency Property)
         * 這讓 UserControl 可以在 XAML 中被綁定
         * 透過 DependencyProperty.RegisterAttached 方法來註冊 Dependency Property
         * readonly: 相依屬性一旦註冊完後就不應該被修改
         * 屬性的命名是有明確規範，必須要是屬性名稱 + Property
         */
        public static readonly DependencyProperty TargetObjectProperty =
            DependencyProperty.Register(
                nameof(TargetObject),           // 相依屬性名稱
                typeof(object),                 // 相依屬性型別
                typeof(ModelEditor),            // 擁有相依屬性的類別
                /**
                 * 相依屬性的其它設定
                 * param1: 屬性的預設值
                 * param2: 屬性值改變時的 Callback
                 * param3: 轉換屬性值的 Callback
                 * 在呼叫 Callback 時會透過參數(DependencyObject d)將相依物件傳入
                 */
                new PropertyMetadata(null, OnTargetObjectChanged, CoerceTargetObject));

        /**
         * 2. C# 屬性封裝 (Wrapper)
         * 相依屬性的值調整: 需要透過相依物件所提供的方法 GetValue, SetValue 來操作
         * TargetObject 屬性只是相依屬性的包裝(Wrapper)
         * 在資料綁定同步相依屬性時，系統會直接呼叫 GetValue, SetValue 來操作屬性，並不會透過 TargetObject
         * 不應該在 TargetObject 的 getter, setter 中加入其他的指令，應使用 OnTargetObjectChanged、CoerceTargetObject 來處理屬性值改變的邏輯
         */
        public object TargetObject
        {
            get { return GetValue(TargetObjectProperty); }
            set { SetValue(TargetObjectProperty, value); }
        }

        /**
         * 若 (this.DataContext as ModelEditorViewModel).IsEditing = IsEditing;
         * 則只有 CoerceTargetObject 會被觸發
         */
        /// <summary>
        /// 當屬性值改變時觸發
        /// </summary>
        /// <param name="d">相依物件</param>
        /// <param name="e">屬性值改變事件的參數</param>
        private static void OnTargetObjectChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ModelEditor? view = d as ModelEditor;
            if (view != null && e.NewValue != null)
            {
                // 當外部給了新的 Model，我們內部就 new 一個 ViewModel
                // 並設為這個 UserControl 的 DataContext
                view.DataContext = new ModelEditorViewModel(e.NewValue);
            }
        }

        /// <summary>
        /// 當屬性值被設定時(不管是否改變)，用來轉換屬性值的方法
        /// </summary>
        /// <param name="d">相依物件</param>
        /// <param name="baseValue">原始屬性值</param>
        /// <returns>轉換後的屬性值</returns>
        private static object CoerceTargetObject(DependencyObject d, object baseValue)
        {
            return baseValue;
        }
    }
}
