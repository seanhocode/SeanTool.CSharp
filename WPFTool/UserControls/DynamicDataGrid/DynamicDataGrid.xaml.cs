using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace SeanTool.CSharp.Net8.WPF
{
    /// <summary>
    /// DynamicDataGrid.xaml 的互動邏輯
    /// </summary>
    public partial class DynamicDataGrid : UserControl
    {
        public DynamicDataGrid()
        {
            InitializeComponent();
        }

        #region Dependency Properties

        // 1. DataSource (資料來源)
        public static readonly DependencyProperty DataSourceProperty =
            DependencyProperty.Register(nameof(DataSource), typeof(IEnumerable), typeof(DynamicDataGrid),
                new PropertyMetadata(null, OnDataSourceChanged));

        public IEnumerable DataSource
        {
            get => (IEnumerable)GetValue(DataSourceProperty);
            set => SetValue(DataSourceProperty, value);
        }

        private static void OnDataSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DynamicDataGrid control)
            {
                control.MainDataGrid.ItemsSource = e.NewValue as IEnumerable;
            }
        }

        // 2. ColumnDefinitions (欄位定義)
        public static readonly DependencyProperty ColumnDefinitionsProperty =
            DependencyProperty.Register(nameof(ColumnDefinitions), typeof(IEnumerable<DynamicDataGridColumnDefinition>), typeof(DynamicDataGrid),
                new PropertyMetadata(null, OnDynamicDataGridColumnDefinitionsChanged));

        public IEnumerable<DynamicDataGridColumnDefinition> ColumnDefinitions
        {
            get => (IEnumerable<DynamicDataGridColumnDefinition>)GetValue(ColumnDefinitionsProperty);
            set => SetValue(ColumnDefinitionsProperty, value);
        }

        private static void OnDynamicDataGridColumnDefinitionsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DynamicDataGrid control && e.NewValue is IEnumerable<DynamicDataGridColumnDefinition> definitions)
            {
                control.GenerateColumns(definitions);
            }
        }

        // 3. IsReadOnly (是否唯讀)
        public static readonly DependencyProperty IsReadOnlyProperty =
            DependencyProperty.Register(nameof(IsReadOnly), typeof(bool), typeof(DynamicDataGrid),
                new PropertyMetadata(true)); // 預設可編輯

        public bool IsReadOnly
        {
            get => (bool)GetValue(IsReadOnlyProperty);
            set => SetValue(IsReadOnlyProperty, value);
        }
        #endregion

        /// <summary>
        /// 根據設定檔生成 DataGrid Columns
        /// </summary>
        private void GenerateColumns(IEnumerable<DynamicDataGridColumnDefinition> definitions)
        {
            MainDataGrid.Columns.Clear();

            if (definitions == null) return;

            foreach (DynamicDataGridColumnDefinition definition in definitions)
            {
                // 這裡目前使用 DataGridTextColumn，若需支援 CheckBox 或 Template 可再擴充
                DataGridTextColumn column = new DataGridTextColumn
                {
                    Header = definition.Header,
                    Width = definition.Width,
                    IsReadOnly = definition.IsReadOnly
                };

                // 設定 Binding
                if (!string.IsNullOrEmpty(definition.BindingPath))
                {
                    Binding binding = new Binding(definition.BindingPath)
                    {
                        Mode = BindingMode.TwoWay,                 // 允許寫回數據
                        UpdateSourceTrigger = UpdateSourceTrigger.LostFocus // 離開焦點時更新
                    };

                    if (!string.IsNullOrEmpty(definition.StringFormat))
                    {
                        binding.StringFormat = definition.StringFormat;
                    }

                    column.Binding = binding;
                }

                MainDataGrid.Columns.Add(column);
            }
        }
    }
}
