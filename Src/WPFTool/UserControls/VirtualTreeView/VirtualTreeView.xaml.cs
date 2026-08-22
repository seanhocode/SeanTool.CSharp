using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using SeanTool.CSharp.WPFTool.Models.Filter;
using SeanTool.CSharp.WPFTool.Models.VirtualTreeView;

namespace SeanTool.CSharp.WPFTool.UserControls.VirtualTreeView
{
    public partial class VirtualTreeView : UserControl, INotifyPropertyChanged
    {
        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register(
                nameof(ItemsSource),
                typeof(IEnumerable<TreeNodeViewModel>),
                typeof(VirtualTreeView),
                new PropertyMetadata(null, OnSearchInputChanged));

        public static readonly DependencyProperty IsCheckVisibleProperty =
            DependencyProperty.Register(nameof(IsCheckVisible), typeof(bool), typeof(VirtualTreeView), new PropertyMetadata(true));

        public static readonly DependencyProperty SelectedItemProperty =
            DependencyProperty.Register(
                nameof(SelectedItem),
                typeof(TreeNodeViewModel),
                typeof(VirtualTreeView),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedItemChanged));

        private readonly VirtualTreeViewViewModel _viewModel = new();

        public ObservableCollection<TreeNodeViewModel> FilteredItems => _viewModel.FilteredItems;

        public FilterViewModel FilterViewModel => _viewModel.FilterViewModel;

        public ObservableCollection<TreeNodeViewModel> CheckedItems => _viewModel.CheckedItems;

        public TreeNodeViewModel? SelectedItem
        {
            get => (TreeNodeViewModel?)GetValue(SelectedItemProperty);
            set => SetValue(SelectedItemProperty, value);
        }

        public IReadOnlyList<TreeNodeViewModel> SelectedItems => _viewModel.SelectedItems;

        public IReadOnlyList<object?> CheckedValues => _viewModel.CheckedValues;

        public bool IsCheckVisible
        {
            get => (bool)GetValue(IsCheckVisibleProperty);
            set => SetValue(IsCheckVisibleProperty, value);
        }

        public IEnumerable<TreeNodeViewModel>? ItemsSource
        {
            get => (IEnumerable<TreeNodeViewModel>?)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        public VirtualTreeView()
        {
            _viewModel.PropertyChanged += ViewModelPropertyChanged;
            InitializeComponent();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private static void OnSearchInputChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            VirtualTreeView control = (VirtualTreeView)dependencyObject;
            control._viewModel.ItemsSource = control.ItemsSource;
        }

        private static void OnSelectedItemChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            VirtualTreeView control = (VirtualTreeView)dependencyObject;
            control._viewModel.SelectedItem = (TreeNodeViewModel?)e.NewValue;
            control.PropertyChanged?.Invoke(dependencyObject, new PropertyChangedEventArgs(nameof(SelectedItem)));
            control.PropertyChanged?.Invoke(dependencyObject, new PropertyChangedEventArgs(nameof(SelectedItems)));
        }

        private void ViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(VirtualTreeViewViewModel.SelectedItem))
            {
                SetCurrentValue(SelectedItemProperty, _viewModel.SelectedItem);
            }
            else if (e.PropertyName == nameof(VirtualTreeViewViewModel.CheckedValues))
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CheckedValues)));
            }
        }

        private void TreeViewSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            SetCurrentValue(SelectedItemProperty, e.NewValue as TreeNodeViewModel);
        }

        private void ExpandAllClick(object sender, RoutedEventArgs e)
        {
            _viewModel.ExpandAll();
        }

        private void CollapseAllClick(object sender, RoutedEventArgs e)
        {
            _viewModel.CollapseAll();
        }
    }
}
