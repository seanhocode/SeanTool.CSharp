using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using SeanTool.CSharp.WPFTool.Test.Models;

namespace SeanTool.CSharp.WPFTool.Test
{
    /// <summary>
    /// DropDownListTestWindow.xaml 的互動邏輯
    /// </summary>
    public partial class DropDownListTestWindow : Window
    {
        // 資料來源，測試單選/多選 DropDownList 是否共用同一份 ObservableCollection 也能正常搜尋、選取
        public ObservableCollection<Person> PersonList { get; } = [];

        public DropDownListTestWindow()
        {
            LoadPersonData();
            DataContext = this;
            InitializeComponent();
        }

        private void LoadPersonData()
        {
            var random = new Random(20260827);
            for (int i = 1; i <= 1000000; i++)
            {
                PersonList.Add(new Person
                {
                    ID = i,
                    Name = $"Person {i:D3}",
                    Age = (short)random.Next(18, 80)
                });
            }
        }

        private void ShowSingleSelection(object sender, RoutedEventArgs e)
        {
            string text = SinglePersonDropDown.SelectedValue is Person person
                ? $"{person.Name} (Age {person.Age})"
                : "(未選擇)";

            MessageBox.Show(text, "單選結果");
        }

        private void ShowMultiSelection(object sender, RoutedEventArgs e)
        {
            string names = string.Join(Environment.NewLine,
                (MultiPersonDropDown.SelectedValues?.OfType<Person>() ?? Enumerable.Empty<Person>())
                    .Select(person => person.Name));

            MessageBox.Show(string.IsNullOrWhiteSpace(names) ? "目前沒有選取項目。" : names, "多選結果");
        }

        private void ClearMultiSelection(object sender, RoutedEventArgs e)
        {
            MultiPersonDropDown.ViewModel.ClearSelection();
        }

        private void AddPerson(object sender, RoutedEventArgs e)
        {
            int nextIndex = PersonList.Count + 1;
            PersonList.Add(new Person { ID = nextIndex, Name = $"Person {nextIndex:D3}", Age = 30 });
        }
    }
}
