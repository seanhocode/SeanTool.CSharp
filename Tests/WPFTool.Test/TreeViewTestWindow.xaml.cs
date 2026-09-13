using System.Collections.ObjectModel;
using System.Windows;
using SeanTool.CSharp.WPFTool.Test.Models;

namespace SeanTool.CSharp.WPFTool.Test
{
    public partial class TreeViewTestWindow : Window
    {
        public ObservableCollection<TreeNodeViewModel> RootNodes { get; } = [];

        public TreeViewTestWindow()
        {
            InitializeComponent();
            LoadTreeData();
            DataContext = this;
        }

        private record School(string Name);

        private record Class(string Name);

        private void LoadTreeData()
        {
            for (int rootIndex = 1; rootIndex <= 100; rootIndex++)
            {
                var school = new School($"School {rootIndex:D3}");
                var root = new TreeNode(school.Name, value: school);
                for (int branchIndex = 1; branchIndex <= 10; branchIndex++)
                {
                    var @class = new Class($"Class {rootIndex:D3}-{branchIndex:D2}");
                    var branch = new TreeNode(@class.Name, value: @class);
                    for (int itemIndex = 1; itemIndex <= 10; itemIndex++)
                    {
                        var person = new Person
                        {
                            Name = $"Person {rootIndex:D3}-{branchIndex:D2}-{itemIndex:D2}"
                        };
                        branch.Children.Add(new TreeNode(person.Name, value: person));
                    }

                    root.Children.Add(branch);
                }

                RootNodes.Add(new TreeNodeViewModel(root));
            }
        }

        private void ShowCheckedItems(object sender, RoutedEventArgs e)
        {
            string selectedItem = SchoolTreeView.SelectedItem?.Value switch
            {
                Person person => person.Name,
                Class @class => @class.Name,
                School school => school.Name,
                _ => "(無)"
            };

            if (!SchoolTreeView.IsCheckVisible)
            {
                MessageBox.Show($"SelectedItem: {selectedItem}", "TreeView");
                return;
            }

            string selectedValues = string.Join(", ", SchoolTreeView.CheckedValues.OfType<Person>().Select(person => person.Name));
            MessageBox.Show($"SelectedItem: {selectedItem}\nSelectedValues: {selectedValues switch { "" => "(無)", _ => selectedValues }}", "TreeView");
        }
    }
}
