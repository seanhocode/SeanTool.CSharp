using System.Windows;

namespace SeanTool.CSharp.WPF.Test
{
    /// <summary>
    /// ModelEditorTestWindow.xaml 的互動邏輯
    /// </summary>
    public partial class ModelEditorTestWindow : Window
    {
        public Person _Person { get; set; }

        public ModelEditorTestWindow()
        {
            InitializeComponent();
            _Person = new Person();
            this.DataContext = this;
        }

        private void CheckModelValue(object sender, RoutedEventArgs e)
        {
            Person person = _Person;
            // 此處下中斷點檢查 person 內容
            MessageBox.Show($"Name: {person.Name}, Age: {person.Age}");
        }
    }
}
