using System.Windows;

namespace SeanTool.CSharp.Net8.WPF.Test
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // 1. 這是我們要編輯的測試物件
        public Person _Person { get; set; }

        public MainWindow()
        {
            InitializeComponent();

            _Person = new Person();
        }

        private void GenModelEditorByTool(object sender, RoutedEventArgs e)
        {
            var editorWindow = new EditorModelWindow(_Person);

            editorWindow.ShowDialog();
        }

        private void GenModelEditorByTestProject(object sender, RoutedEventArgs e)
        {
            var editorWindow = new ModelEditorTestWindow();

            editorWindow.ShowDialog();
        }

        private void CheckModelValue(object sender, RoutedEventArgs e)
        {
            Person person = _Person;
            // 此處下中斷點檢查 person 內容
            MessageBox.Show($"Name: {person.Name}, Age: {person.Age}");
        }
    }
}