using System.Collections.ObjectModel;
using System.Windows;
using SeanTool.CSharp.WPFTool.Models.VirtualTreeView;
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

        private void LoadTreeData()
        {
            for (int rootIndex = 1; rootIndex <= 100; rootIndex++)
            {
                var root = new TreeNode($"School {rootIndex:D3}");
                for (int branchIndex = 1; branchIndex <= 10; branchIndex++)
                {
                    var branch = new TreeNode($"Class {rootIndex:D3}-{branchIndex:D2}");
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
            if (!SchoolTreeView.IsCheckVisible)
            {
                string selectedItem = SchoolTreeView.SelectedItem is TreeNodeViewModel node
                    ? $"{node.Name} ({node.Value ?? "無 Value"})"
                    : "(無)";
                MessageBox.Show($"SelectedItem: {selectedItem}", "TreeView");
                return;
            }

            string selectedItems = string.Join(", ", SchoolTreeView.SelectedItems.Select(node => node.Name));
            string selectedValues = string.Join(", ", SchoolTreeView.CheckedValues.OfType<Person>().Select(person => person.Name));
            MessageBox.Show($"SelectedItems: {selectedItems switch { "" => "(無)", _ => selectedItems }}\nSelectedValues: {selectedValues switch { "" => "(無)", _ => selectedValues }}", "TreeView");
        }
    }
}
