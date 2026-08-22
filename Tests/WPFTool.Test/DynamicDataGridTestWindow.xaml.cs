using System.Collections.ObjectModel;
using System.Windows;
using SeanTool.CSharp.WPFTool.Models.DynamicDataGrid;
using SeanTool.CSharp.WPFTool.Test.Models;
using SeanTool.CSharp.WPFTool.Windows;

namespace SeanTool.CSharp.WPFTool.Test
{
    /// <summary>
    /// DynamicDataGridTestWindow.xaml 的互動邏輯
    /// </summary>
    public partial class DynamicDataGridTestWindow : Window
    {
        // 資料來源
        public ObservableCollection<Person> PersonList { get; set; }

        public List<DynamicDataGridActionDefinition> ActionDefinitions { get; set; }

        public DynamicDataGridTestWindow()
        {
            LoadDynamicDataGridTestData();

            this.DataContext = this;

            InitializeComponent();
        }

        private void LoadDynamicDataGridTestData()
        {
            PersonList = new ObservableCollection<Person>();
            ObservableCollection<Person>  list = new ObservableCollection<Person>();
            var random = new Random(20260819);
            int dataCount = 1000_000;
            for (int i = 0; i < dataCount; i++)
            {
                list.Add(new Person
                {
                    ID = i,
                    Name = $"User {random.Next(1, 1_000_000):D6}",
                    Age = (short)random.Next(18, 80),
                    BirthDate = DateTime.Today.AddDays(-random.Next(0, 20_000)),
                    IsEnabled = random.Next(2) == 1
                });
            }

            ActionDefinitions = new List<DynamicDataGridActionDefinition>
            {
                new DynamicDataGridActionDefinition
                {
                    Header = "",
                    Content = "編輯",
                    Action = item => new ModelEditorWindow((Person)item).ShowDialog()
                },
                new DynamicDataGridActionDefinition
                {
                    Header = "",
                    Content = "刪除",
                    Action = item =>
                    {
                        if (MessageBox.Show($"確定要刪除 {((Person)item).Name} 嗎？", "刪除確認", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                        {
                            PersonList.Remove((Person)item);
                        }
                    }
                }
            };

            PersonList = list;
        }

        private void CheckDataValue(object sender, RoutedEventArgs e)
        {
            ObservableCollection<Person> personList = PersonList;
            // 此處下中斷點檢查 person 內容
            MessageBox.Show(personList.Count().ToString());
        }

        private void ShowSelectedItems(object sender, RoutedEventArgs e)
        {
            string names = string.Join(Environment.NewLine,
                PersonDataGrid.SelectedItems
                    .OfType<Person>()
                    .Select(person => person.Name));

            MessageBox.Show(string.IsNullOrWhiteSpace(names) ? "目前沒有勾選項目。" : names,
                "選取項目名稱");
        }
    }
}
