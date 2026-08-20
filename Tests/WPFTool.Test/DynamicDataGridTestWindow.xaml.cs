using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace SeanTool.CSharp.WPF.Test
{
    /// <summary>
    /// DynamicDataGridTestWindow.xaml 的互動邏輯
    /// </summary>
    public partial class DynamicDataGridTestWindow : Window
    {
        // 資料來源
        public ObservableCollection<Person> PersonList { get; set; }

        // 欄位定義 (這通常在建構子初始化，或是從設定檔讀取)
        public List<DynamicDataGridColumnDefinition> ColumnDefinitions { get; set; }

        public DynamicDataGridTestWindow()
        {
            LoadDynamicDataGridTestData();

            this.DataContext = this;

            InitializeComponent();
        }

        private void LoadDynamicDataGridTestData()
        {
            PersonList = new ObservableCollection<Person>();
            var random = new Random(20260819);
            for (int i = 0; i < 100_000; i++)
            {
                PersonList.Add(new Person
                {
                    ID = i,
                    Name = $"User {random.Next(1, 1_000_000):D6}",
                    Age = (short)random.Next(18, 80),
                    BirthDate = DateTime.Today.AddDays(-random.Next(0, 20_000)),
                    IsEnabled = random.Next(2) == 1
                });
            }

            // 2. 定義欄位
            ColumnDefinitions = new List<DynamicDataGridColumnDefinition>
            {
                new DynamicDataGridColumnDefinition { Header = "編號", BindingPath = "ID", Width = 100 },
                new DynamicDataGridColumnDefinition { Header = "姓名", BindingPath = "Name", Width = new DataGridLength(1, DataGridLengthUnitType.Star) }, // Star width
                new DynamicDataGridColumnDefinition { Header = "加入時間", BindingPath = "BirthDate", StringFormat = "yyyy/MM/dd HH:mm", Width = 150 }
            };
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
