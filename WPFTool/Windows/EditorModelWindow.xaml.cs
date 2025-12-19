using System.Windows;

namespace SeanTool.CSharp.Net8.WPF
{
    /// <summary>
    /// EditorModelWindow.xaml 的互動邏輯
    /// </summary>
    public partial class EditorModelWindow : Window
    {
        // 定義相依屬性，讓 XAML 裡的 ModelEditorView 可以綁定到這裡
        public static readonly DependencyProperty TargetObjectProperty =
            DependencyProperty.Register(
                nameof(TargetObject),
                typeof(object),
                typeof(EditorModelWindow),
                new PropertyMetadata(null));

        public object TargetObject
        {
            get { return GetValue(TargetObjectProperty); }
            set { SetValue(TargetObjectProperty, value); }
        }

        public EditorModelWindow(object targetModel)
        {
            InitializeComponent();

            // 設定要編輯的物件
            TargetObject = targetModel;

            // 根據物件型別自動設定視窗標題 (可選)
            if (targetModel != null)
            {
                Title = $"編輯: {targetModel.GetType().Name}";
            }
        }
    }
}
