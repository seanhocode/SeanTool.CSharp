using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using SeanTool.CSharp.WPFTool.UserControls.DynamicDataGrid;
using SeanTool.CSharp.WPFTool.Windows;
using SeanTool.CSharp.WPFTool.Test.Models;

namespace SeanTool.CSharp.WPFTool.Test
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // 1. 這是我們要編輯的測試物件
        public Person _Person { get; set; }
        private DynamicDataGridTestWindow? _dataGridWindow;
        private DynamicDataGridDataTableTestWindow? _dataGridDataTableWindow;
        private TreeViewTestWindow? _treeViewWindow;
        private DropDownListTestWindow? _dropDownListWindow;


        public MainWindow()
        {
            InitializeComponent();

            _Person = new Person();
        }

        private void GenModelEditorByTool(object sender, RoutedEventArgs e)
        {
            Window editorWindow = new ModelEditorWindow(_Person);

            editorWindow.ShowDialog();
        }

        private void GenModelEditorByTestProject(object sender, RoutedEventArgs e)
        {
            Window editorWindow = new ModelEditorTestWindow();

            editorWindow.ShowDialog();
        }

        private void GenModelEditorDataTable(object sender, RoutedEventArgs e)
        {
            Window editorWindow = new ModelEditorDataTableTestWindow();

            editorWindow.ShowDialog();
        }

        private void CheckModelValue(object sender, RoutedEventArgs e)
        {
            Person person = _Person;
            // 此處下中斷點檢查 person 內容
            MessageBox.Show($"Name: {person.Name}, Age: {person.Age}");
        }

        private void ShowDynamicDataGrid(object sender, RoutedEventArgs e)
        {
            if (_dataGridWindow is null)
            {
                _dataGridWindow = new DynamicDataGridTestWindow();
                _dataGridWindow.Closed += DataGridWindowClosed;
            }

            _dataGridWindow.Show();
            _dataGridWindow.Activate();
        }

        private void DataGridWindowClosed(object? sender, EventArgs e)
        {
            if (sender is DynamicDataGridTestWindow window)
            {
                window.Closed -= DataGridWindowClosed;
            }

            _dataGridWindow = null;
        }

        private void ShowDynamicDataGridDataTable(object sender, RoutedEventArgs e)
        {
            if (_dataGridDataTableWindow is null)
            {
                _dataGridDataTableWindow = new DynamicDataGridDataTableTestWindow();
                _dataGridDataTableWindow.Closed += DataGridDataTableWindowClosed;
            }

            _dataGridDataTableWindow.Show();
            _dataGridDataTableWindow.Activate();
        }

        private void DataGridDataTableWindowClosed(object? sender, EventArgs e)
        {
            if (sender is DynamicDataGridDataTableTestWindow window)
            {
                window.Closed -= DataGridDataTableWindowClosed;
            }

            _dataGridDataTableWindow = null;
        }

        private void ShowTreeView(object sender, RoutedEventArgs e)
        {
            if (_treeViewWindow is null)
            {
                _treeViewWindow = new TreeViewTestWindow();
                _treeViewWindow.Closed += TreeViewWindowClosed;
            }

            _treeViewWindow.Show();
            _treeViewWindow.Activate();
        }

        private void TreeViewWindowClosed(object? sender, EventArgs e)
        {
            if (sender is TreeViewTestWindow window)
            {
                window.Closed -= TreeViewWindowClosed;
            }

            _treeViewWindow = null;
        }

        private void ShowDropDownList(object sender, RoutedEventArgs e)
        {
            if (_dropDownListWindow is null)
            {
                _dropDownListWindow = new DropDownListTestWindow();
                _dropDownListWindow.Closed += DropDownListWindowClosed;
            }

            _dropDownListWindow.Show();
            _dropDownListWindow.Activate();
        }

        private void DropDownListWindowClosed(object? sender, EventArgs e)
        {
            if (sender is DropDownListTestWindow window)
            {
                window.Closed -= DropDownListWindowClosed;
            }

            _dropDownListWindow = null;
        }
    }
}