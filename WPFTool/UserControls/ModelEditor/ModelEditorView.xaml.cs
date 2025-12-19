using System.Windows;
using System.Windows.Controls;

namespace SeanTool.CSharp.Net8.WPF
{
    /// <summary>
    /// ModelEditorView.xaml 的互動邏輯
    /// </summary>
    public partial class ModelEditorView : UserControl
    {
        public ModelEditorView()
        {
            InitializeComponent();
        }

        // 1. 定義相依屬性 (Dependency Property)
        // 這讓 UserControl 可以在 XAML 中被綁定
        public static readonly DependencyProperty TargetObjectProperty =
            DependencyProperty.Register(
                nameof(TargetObject),           // 屬性名稱
                typeof(object),                 // 屬性型別
                typeof(ModelEditorView),        // 擁有者型別
                new PropertyMetadata(null, OnTargetObjectChanged)); // 當值改變時的回呼

        // 2. C# 屬性封裝 (Wrapper)
        public object TargetObject
        {
            get { return GetValue(TargetObjectProperty); }
            set { SetValue(TargetObjectProperty, value); }
        }

        // 3. 當外部傳入新的 Model 時觸發
        private static void OnTargetObjectChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ModelEditorView? view = d as ModelEditorView;
            if (view != null && e.NewValue != null)
            {
                // 【核心邏輯】
                // 當外部給了新的 Model，我們內部就 new 一個 ViewModel
                // 並設為這個 UserControl 的 DataContext
                view.DataContext = new ModelEditorViewModel(e.NewValue);
            }
        }
    }
}
