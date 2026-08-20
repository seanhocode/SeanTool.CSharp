using System.Collections;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;

namespace SeanTool.CSharp.WPF
{
    public partial class DynamicDataGrid : UserControl
    {
        public DynamicDataGridViewModel ViewModel { get; } = new();

        public DynamicDataGrid()
        {
            InitializeComponent();
            ViewModel.PropertyChanged += ViewModelPropertyChanged;
        }

        public static readonly DependencyProperty DataSourceProperty =
            DependencyProperty.Register(nameof(DataSource), typeof(IEnumerable), typeof(DynamicDataGrid),
                new PropertyMetadata(null, OnDataSourceChanged));

        public IEnumerable? DataSource
        {
            get => (IEnumerable?)GetValue(DataSourceProperty);
            set => SetValue(DataSourceProperty, value);
        }

        public static readonly DependencyProperty ColumnDefinitionsProperty =
            DependencyProperty.Register(nameof(ColumnDefinitions), typeof(IEnumerable<DynamicDataGridColumnDefinition>), typeof(DynamicDataGrid),
                new PropertyMetadata(null, OnColumnDefinitionsChanged));

        public IEnumerable<DynamicDataGridColumnDefinition>? ColumnDefinitions
        {
            get => (IEnumerable<DynamicDataGridColumnDefinition>?)GetValue(ColumnDefinitionsProperty);
            set => SetValue(ColumnDefinitionsProperty, value);
        }

        public static readonly DependencyProperty SortPropertyProperty =
            DependencyProperty.Register(nameof(SortProperty), typeof(string), typeof(DynamicDataGrid),
                new PropertyMetadata(null, OnSortChanged));

        public string? SortProperty
        {
            get => (string?)GetValue(SortPropertyProperty);
            set => SetValue(SortPropertyProperty, value);
        }

        public static readonly DependencyProperty SortDescendingProperty =
            DependencyProperty.Register(nameof(SortDescending), typeof(bool), typeof(DynamicDataGrid),
                new PropertyMetadata(false, OnSortChanged));

        public bool SortDescending
        {
            get => (bool)GetValue(SortDescendingProperty);
            set => SetValue(SortDescendingProperty, value);
        }

        public static readonly DependencyProperty IsReadOnlyProperty =
            DependencyProperty.Register(nameof(IsReadOnly), typeof(bool), typeof(DynamicDataGrid),
                new PropertyMetadata(true, OnEditModeChanged));

        public bool IsReadOnly
        {
            get => (bool)GetValue(IsReadOnlyProperty);
            set => SetValue(IsReadOnlyProperty, value);
        }

        public static readonly DependencyProperty ShowCheckBoxProperty =
            DependencyProperty.Register(nameof(ShowCheckBox), typeof(bool), typeof(DynamicDataGrid),
                new PropertyMetadata(false, OnShowCheckBoxChanged));

        public bool ShowCheckBox
        {
            get => (bool)GetValue(ShowCheckBoxProperty);
            set => SetValue(ShowCheckBoxProperty, value);
        }

        public static readonly DependencyProperty SelectedItemProperty =
            DependencyProperty.Register(nameof(SelectedItem), typeof(object), typeof(DynamicDataGrid),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public object? SelectedItem
        {
            get => GetValue(SelectedItemProperty);
            set => SetValue(SelectedItemProperty, value);
        }

        public IReadOnlyList<object> SelectedItems => _selectedItems.ToArray();

        private readonly HashSet<object> _selectedItems = new();

        private static void OnDataSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DynamicDataGrid control)
            {
                control.ViewModel.DataSource = e.NewValue as IEnumerable;
                control.SetColumns(control.ColumnDefinitions);
            }
        }

        private static void OnColumnDefinitionsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DynamicDataGrid control)
            {
                control.SetColumns(e.NewValue as IEnumerable<DynamicDataGridColumnDefinition>);
            }
        }

        private static void OnShowCheckBoxChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DynamicDataGrid control)
            {
                control.SetColumns(control.ColumnDefinitions);
            }
        }

        private static void OnSortChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DynamicDataGrid control)
            {
                control.ViewModel.SortProperty = control.SortProperty;
                control.ViewModel.SortDescending = control.SortDescending;
            }
        }

        private static void OnEditModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DynamicDataGrid control)
            {
                control.UpdateEditability();
            }
        }

        private void SetColumns(IEnumerable<DynamicDataGridColumnDefinition>? definitions)
        {
            ViewModel.SetColumnDefinitions(definitions);
            MainDataGrid.Columns.Clear();
            SortColumnComboBox.ItemsSource = ViewModel.ColumnDefinitions;

            foreach (DynamicDataGridColumnDefinition definition in ViewModel.ColumnDefinitions)
            {
                DynamicDataGridFilterViewModel? filter = ViewModel.Filters
                    .FirstOrDefault(item => item.PropertyName == definition.BindingPath);

                var column = new DataGridTextColumn
                {
                    Header = new DynamicDataGridFilterControl { DataContext = filter },
                    Width = definition.Width,
                    IsReadOnly = definition.IsReadOnly
                };

                if (!string.IsNullOrWhiteSpace(definition.BindingPath))
                {
                    var binding = new Binding(definition.BindingPath)
                    {
                        Mode = BindingMode.TwoWay,
                        UpdateSourceTrigger = UpdateSourceTrigger.LostFocus,
                        StringFormat = definition.StringFormat
                    };
                    column.Binding = binding;
                }

                MainDataGrid.Columns.Add(column);
            }

            if (ShowCheckBox)
            {
                MainDataGrid.Columns.Insert(0, CreateCheckBoxColumn());
            }

            if (CanEditRows)
            {
                MainDataGrid.Columns.Add(CreateEditColumn());
            }

            UpdateEditability();
        }

        private bool CanEditRows => !IsReadOnly && ViewModel.CanWrite;

        private void UpdateEditability()
        {
            AddButton.IsEnabled = CanEditRows;
            MainDataGrid.IsReadOnly = true;
        }

        private void ViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(DynamicDataGridViewModel.DisplayItems))
            {
                MainDataGrid.ItemsSource = ViewModel.DisplayItems;
            }
        }

        private void ClearFilter(object sender, RoutedEventArgs e)
        {
            ViewModel.ClearFilters();
        }

        private void AddItem(object sender, RoutedEventArgs e)
        {
            if (!CanEditRows)
            {
                return;
            }

            object? newItem = ViewModel.CreateNewItem();
            if (newItem is null)
            {
                MessageBox.Show("目前資料來源不支援新增，或找不到可建立的 Model 型別。", "無法新增", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var editorWindow = new ModelEditorWindow(newItem)
            {
                Owner = Window.GetWindow(this),
                Title = $"新增: {newItem.GetType().Name}"
            };

            if (editorWindow.ShowDialog() == true)
            {
                ViewModel.AddItem(newItem);
            }
        }

        private void SortColumnChanged(object sender, SelectionChangedEventArgs e)
        {
            SortProperty = (SortColumnComboBox.SelectedItem as DynamicDataGridColumnDefinition)?.BindingPath;
        }

        private void SortChanged(object sender, RoutedEventArgs e)
        {
            SortDescending = SortDescendingCheckBox.IsChecked == true;
        }

        private DataGridTemplateColumn CreateEditColumn()
        {
            var buttonFactory = new FrameworkElementFactory(typeof(Button));
            buttonFactory.SetValue(Button.ContentProperty, "編輯");
            buttonFactory.SetValue(Button.PaddingProperty, new Thickness(8, 2, 8, 2));
            buttonFactory.AddHandler(Button.ClickEvent, new RoutedEventHandler(EditRow));

            var deleteButtonFactory = new FrameworkElementFactory(typeof(Button));
            deleteButtonFactory.SetValue(Button.ContentProperty, "刪除");
            deleteButtonFactory.SetValue(Button.PaddingProperty, new Thickness(8, 2, 8, 2));
            deleteButtonFactory.AddHandler(Button.ClickEvent, new RoutedEventHandler(DeleteRow));

            var actionPanel = new FrameworkElementFactory(typeof(StackPanel));
            actionPanel.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);
            actionPanel.AppendChild(buttonFactory);
            actionPanel.AppendChild(deleteButtonFactory);

            return new DataGridTemplateColumn
            {
                Header = "操作",
                Width = DataGridLength.Auto,
                IsReadOnly = true,
                CellTemplate = new DataTemplate { VisualTree = actionPanel }
            };
        }

        private DataGridTemplateColumn CreateCheckBoxColumn()
        {
            var checkBoxFactory = new FrameworkElementFactory(typeof(CheckBox));
            checkBoxFactory.SetValue(CheckBox.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            checkBoxFactory.AddHandler(FrameworkElement.LoadedEvent, new RoutedEventHandler(SelectionCheckBoxLoaded));
            checkBoxFactory.AddHandler(ButtonBase.ClickEvent, new RoutedEventHandler(SelectionCheckBoxClicked));

            return new DataGridTemplateColumn
            {
                Header = "選取",
                Width = DataGridLength.Auto,
                IsReadOnly = true,
                CellTemplate = new DataTemplate { VisualTree = checkBoxFactory }
            };
        }

        private void SelectionCheckBoxLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox && checkBox.DataContext is object item)
            {
                checkBox.IsChecked = _selectedItems.Contains(item);
            }
        }

        private void SelectionCheckBoxClicked(object sender, RoutedEventArgs e)
        {
            if (sender is not CheckBox checkBox || checkBox.DataContext is not object item)
            {
                return;
            }

            if (checkBox.IsChecked == true)
            {
                _selectedItems.Add(item);
                SetCurrentValue(SelectedItemProperty, item);
            }
            else
            {
                _selectedItems.Remove(item);
                if (ReferenceEquals(SelectedItem, item))
                {
                    SetCurrentValue(SelectedItemProperty, _selectedItems.LastOrDefault());
                }
            }
        }

        private void EditRow(object sender, RoutedEventArgs e)
        {
            if (!CanEditRows)
            {
                return;
            }

            if (sender is not Button button || button.DataContext is null)
            {
                return;
            }

            var editorWindow = new ModelEditorWindow(button.DataContext)
            {
                Owner = Window.GetWindow(this)
            };

            if (editorWindow.ShowDialog() == true)
            {
                ViewModel.RefreshData();
            }
        }

        private void DeleteRow(object sender, RoutedEventArgs e)
        {
            if (!CanEditRows)
            {
                return;
            }

            if (sender is Button button && button.DataContext is object item &&
                MessageBox.Show("確定要刪除此資料列嗎？", "確認刪除", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                _selectedItems.Remove(item);
                if (ReferenceEquals(SelectedItem, item))
                {
                    SetCurrentValue(SelectedItemProperty, _selectedItems.LastOrDefault());
                }

                ViewModel.RemoveItem(item);
            }
        }
    }
}
