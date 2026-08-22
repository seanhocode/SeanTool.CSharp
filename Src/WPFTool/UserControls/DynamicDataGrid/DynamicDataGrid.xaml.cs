using SeanTool.CSharp.WPFTool.Models;
using SeanTool.CSharp.WPFTool.Models.DynamicDataGrid;
using SeanTool.CSharp.WPFTool.Models.Filter;
using SeanTool.CSharp.WPFTool.UserControls.Filter;
using System.Collections;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;

namespace SeanTool.CSharp.WPFTool.UserControls.DynamicDataGrid
{
    public partial class DynamicDataGrid : UserControl
    {
        public DynamicDataGridViewModel ViewModel { get; } = new();

        public DynamicDataGrid()
        {
            InitializeComponent();
            ViewModel.PropertyChanged += ViewModelPropertyChanged;
        }

        # region 註冊 DependencyProperty

        /// <summary>
        /// 資料來源
        /// </summary>
        /// <remarks>型別為 object 而非 IEnumerable，是為了允許直接繫結 DataTable/DataSet(IListSource)，
        /// 並在 <see cref="OnDataSourceChanged"/> 內自動轉換為 DefaultView，避免外界忘記轉換。</remarks>
        public static readonly DependencyProperty DataSourceProperty =
            DependencyProperty.Register(nameof(DataSource), typeof(object), typeof(DynamicDataGrid),
                new PropertyMetadata(null, OnDataSourceChanged));

        /// <summary>
        /// 欄位定義
        /// </summary>
        public static readonly DependencyProperty ColumnDefinitionsProperty =
            DependencyProperty.Register(nameof(ColumnDefinitions), typeof(IEnumerable<DynamicDataGridColumnDefinition>), typeof(DynamicDataGrid),
                new PropertyMetadata(null, OnColumnDefinitionsChanged));

        /// <summary>
        /// 操作欄位定義
        /// </summary>
        public static readonly DependencyProperty ActionDefinitionsProperty =
            DependencyProperty.Register(nameof(ActionDefinitions), typeof(IEnumerable<DynamicDataGridActionDefinition>), typeof(DynamicDataGrid),
                new PropertyMetadata(null, OnActionDefinitionsChanged));

        /// <summary>
        /// 是否顯示選取方塊
        /// </summary>
        public static readonly DependencyProperty ShowCheckBoxProperty =
            DependencyProperty.Register(nameof(ShowCheckBox), typeof(bool), typeof(DynamicDataGrid),
                new PropertyMetadata(false, OnShowCheckBoxChanged));

        /// <summary>
        /// 選取項目
        /// </summary>
        public static readonly DependencyProperty SelectedItemProperty =
            DependencyProperty.Register(nameof(SelectedItem), typeof(object), typeof(DynamicDataGrid),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedItemChanged));

        #endregion

        # region DependencyProperty 封裝
        /// <summary>
        /// 資料來源
        /// </summary>
        public object? DataSource
        {
            get => GetValue(DataSourceProperty);
            set => SetValue(DataSourceProperty, value);
        }

        /// <summary>
        /// 欄位定義
        /// </summary>
        public IEnumerable<DynamicDataGridColumnDefinition>? ColumnDefinitions
        {
            get => (IEnumerable<DynamicDataGridColumnDefinition>?)GetValue(ColumnDefinitionsProperty);
            set => SetValue(ColumnDefinitionsProperty, value);
        }

        /// <summary>
        /// 操作欄位定義
        /// </summary>
        public IEnumerable<DynamicDataGridActionDefinition>? ActionDefinitions
        {
            get => (IEnumerable<DynamicDataGridActionDefinition>?)GetValue(ActionDefinitionsProperty);
            set => SetValue(ActionDefinitionsProperty, value);
        }

        /// <summary>
        /// 是否顯示選取方塊
        /// </summary>
        public bool ShowCheckBox
        {
            get => (bool)GetValue(ShowCheckBoxProperty);
            set => SetValue(ShowCheckBoxProperty, value);
        }

        /// <summary>
        /// 選取項目
        /// </summary>
        public object? SelectedItem
        {
            get => GetValue(SelectedItemProperty);
            set => SetValue(SelectedItemProperty, value);
        }

        /// <summary>
        /// 選取項目清單
        /// </summary>
        public IReadOnlyList<object> SelectedItems => ViewModel.SelectedItems;

        # endregion

        # region OnChanged
        /// <summary>
        /// 資料來源改變時觸發
        /// </summary>
        /// <param name="d"></param>
        /// <param name="e"></param>
        /// <remarks>更新 ViewModel.DataSource 並更新欄位</remarks>
        private static void OnDataSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DynamicDataGrid control)
            {
                /* DataTable/DataSet 實作的是 IListSource 而非 IEnumerable，
                 * 需透過 GetList() 取得其 DefaultView 才能被 DataGrid 列舉，
                 * 在此統一處理可避免外界忘記自行轉換為 DefaultView
                 * 
                 * e.NewValue: 異動後的DataSource
                 */
                IEnumerable? resolved = IEnumerableConverter.Convert(e.NewValue);

                // 整包 DataSource 被替換，直接清空選取即可；
                // 個別項目異動時的選取同步已由 ViewModel.DataSource 內部訂閱 CollectionChanged 處理
                control.ViewModel.ClearSelection();
                control.ViewModel.DataSource = resolved;
                control.SetColumns(control.ColumnDefinitions, updateViewModel: false);
            }
        }

        /// <summary>
        /// 欄位定義改變時觸發
        /// </summary>
        /// <param name="d"></param>
        /// <param name="e"></param>
        private static void OnColumnDefinitionsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DynamicDataGrid control)
            {
                control.SetColumns(e.NewValue as IEnumerable<DynamicDataGridColumnDefinition>);
            }
        }

        /// <summary>
        /// 操作欄位定義改變時觸發
        /// </summary>
        /// <param name="d"></param>
        /// <param name="e"></param>
        private static void OnActionDefinitionsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DynamicDataGrid control)
            {
                control.SetColumns(control.ColumnDefinitions, updateViewModel: false);
            }
        }

        /// <summary>
        /// 是否顯示選取方塊改變時觸發
        /// </summary>
        /// <param name="d"></param>
        /// <param name="e"></param>
        private static void OnShowCheckBoxChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DynamicDataGrid control)
            {
                control.SetColumns(control.ColumnDefinitions, updateViewModel: false);
            }
        }

        /// <summary>
        /// 選取項目更新時觸發
        /// </summary>
        /// <param name="d"></param>
        /// <param name="e"></param>
        private static void OnSelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DynamicDataGrid control)
            {
                control.ViewModel.SetSelectedItem(e.NewValue);
            }
        }

        # endregion

        private CheckBox _selectAllCheckBox;

        /// <summary>
        /// 建立 / 更新 DataGrid 的欄位
        /// </summary>
        /// <param name="definitions">是否更新 ViewModel 的 ColumnDefinitions</param>
        private void SetColumns(IEnumerable<DynamicDataGridColumnDefinition>? definitions, bool updateViewModel = true)
        {
            if (updateViewModel) { ViewModel.SetColumnDefinitions(definitions); }

            MainDataGrid.Columns.Clear();

            if (ShowCheckBox) { MainDataGrid.Columns.Add(GetSelectionCheckBoxColumn()); }

            foreach (DynamicDataGridActionDefinition definition in ActionDefinitions ?? [])
            {
                MainDataGrid.Columns.Add(GetActionColumn(definition));
            }

            //根據 ViewModel 的欄位定義建立 DataGrid 的欄位
            foreach (DynamicDataGridColumnDefinition definition in ViewModel.ColumnDefinitions)
            {
                //取得欄位的篩選條件
                FilterViewModel? filter = ViewModel.Filters
                    .FirstOrDefault(item => item.PropertyName == definition.BindingPath);

                DataGridTextColumn column = new DataGridTextColumn
                {
                    //Header 設定為 FilterControl，並將篩選條件的 DataContext 設定為 filter
                    Header = new FilterControl { DataContext = filter, Margin = new Thickness(3, 3, 25, 3) },
                    Width = definition.Width,
                    IsReadOnly = definition.IsReadOnly,
                    ElementStyle = new Style(typeof(TextBlock))
                    {
                        Setters = { 
                            new Setter(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Stretch)
                        }
                    }
                };

                if (!string.IsNullOrWhiteSpace(definition.BindingPath))
                {
                    //綁定欄位的資料來源，使用 TwoWay 模式，並在失去焦點時更新來源
                    Binding binding = new Binding(definition.BindingPath)
                    {
                        Mode = definition.IsReadOnly ? BindingMode.OneWay : BindingMode.TwoWay,
                        UpdateSourceTrigger = UpdateSourceTrigger.LostFocus,
                        StringFormat = definition.StringFormat
                    };
                    column.Binding = binding;
                }

                MainDataGrid.Columns.Add(column);
            }

        }

        /// <summary>
        /// ViewModel 屬性異動時觸發
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <remarks>目前只有當 FilteredItems 改變時才會作用，將 DynamicDataGrid 的 ItemSource 設定為 FilteredItems</remarks>
        private void ViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(DynamicDataGridViewModel.FilteredItems):
                    MainDataGrid.ItemsSource = ViewModel.FilteredItems;
                    break;
                case nameof(DynamicDataGridViewModel.SelectedItem):
                    SetCurrentValue(SelectedItemProperty, ViewModel.SelectedItem);
                    break;

            }
        }

        /// <summary>
        /// 清除篩選條件的觸發鈕點擊事件
        /// </summary>
        private void ClearFilter(object sender, RoutedEventArgs e)
        {
            ViewModel.ClearFilters();
        }

        /// <summary>
        /// 取得選取 CheckBox 欄位
        /// </summary>
        /// <returns></returns>
        private DataGridTemplateColumn GetSelectionCheckBoxColumn()
        {
            FrameworkElementFactory checkBoxFactory = new FrameworkElementFactory(typeof(CheckBox));
             _selectAllCheckBox = new CheckBox();

            checkBoxFactory.SetValue(CheckBox.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            // 根因修復已改為全域套用：見 Styles\WPFUICheckBox.xaml，故此處不再需要指定局部 Style
            checkBoxFactory.AddHandler(FrameworkElement.LoadedEvent, new RoutedEventHandler(SelectionCheckBoxLoaded));
            checkBoxFactory.AddHandler(ButtonBase.ClickEvent, new RoutedEventHandler(SelectionCheckBoxClicked));

            _selectAllCheckBox.SetValue(CheckBox.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            _selectAllCheckBox.SetValue(CheckBox.ContentProperty, "選取全部");
            _selectAllCheckBox.AddHandler(ButtonBase.ClickEvent, new RoutedEventHandler(SelectAllCheckBoxClicked));



            return new DataGridTemplateColumn
            {
                Header = _selectAllCheckBox,
                Width = 100,
                IsReadOnly = true,
                CellTemplate = new DataTemplate { VisualTree = checkBoxFactory }
            };
        }

        /// <summary>
        /// 取得操作欄位的 DataGridTemplateColumn
        /// </summary>
        /// <param name="definition"></param>
        /// <returns></returns>
        private DataGridTemplateColumn GetActionColumn(DynamicDataGridActionDefinition definition)
        {
            FrameworkElementFactory buttonFactory = new FrameworkElementFactory(typeof(Button));
            buttonFactory.SetValue(Button.ContentProperty, definition.Content);
            buttonFactory.SetValue(FrameworkElement.TagProperty, definition);
            buttonFactory.AddHandler(ButtonBase.ClickEvent, new RoutedEventHandler(ActionButtonClicked));

            return new DataGridTemplateColumn
            {
                Header = definition.Header,
                Width = definition.Width,
                IsReadOnly = true,
                CellTemplate = new DataTemplate { VisualTree = buttonFactory }
            };
        }

        /// <summary>
        /// 操作欄位點選時觸發
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ActionButtonClicked(object sender, RoutedEventArgs e)
        {
            /* 關於 Button 的 DataContext 來源：
             * 1. DataGrid 在生成資料列時，會自動將單筆資料綁定至對應 DataGridRow 的 DataContext
             * 2. 透過 WPF 的「DataContext 繼承機制」，若未明確指定子控制項的 DataContext，會自動向上繼承
             * 3. 因此，這裡無需設定 Button 的 DataContext，它會自然繼承所在列的資料本體，使 Click 事件能精準取得該列對應的 item
             */

            /* 檢查 sender 是不是一個 Button？如果不是，直接 return
             * 如果是 Button，就把這個按鈕的 Tag 屬性拿出來，檢查它是否為 DynamicDataGridActionDefinition 型別。如果是，就宣告成變數 definition
             * 同時，把這個按鈕的 DataContext 屬性拿出來，宣告成物件變數 item
             */
            if (sender is not Button { Tag: DynamicDataGridActionDefinition definition, DataContext: object item })
            { return; }

            try
            {
                definition.Action(item);
            }
            catch (Exception ex)
            {
                /* Action 是使用端傳入的委派，內容不受控。若在此拋出未攔截例外，
                 * 會直接砸毀 UI thread 導致整個應用程式崩潰，故在此攔截並記錄，
                 * 避免單一列的操作失敗波及整個畫面。
                 */
                System.Diagnostics.Debug.WriteLine($"DynamicDataGrid Action 執行失敗: {ex}");
                return;
            }

            ViewModel.RefreshData();

            /* 如果 row model 未實作 INotifyPropertyChanged，且Action 改變了 model 的屬性，
             * 無篩選時 RefreshData 可能仍是同一個 ItemsSource 參考，
             * DataGrid 不會自動重繪，所以這裡要明確 Refresh 畫面。
             */
            MainDataGrid.Items.Refresh();
        }

        /// <summary>
        /// 選取 ChkeckBox 載入到畫面時觸發
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <remarks>勾選已選取清單</remarks>
        private void SelectionCheckBoxLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox && checkBox.DataContext is object item)
            {
                checkBox.IsChecked = ViewModel.IsItemSelected(item);
            }
        }

        /// <summary>
        /// 選取 CheckBox 點擊時觸發
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <remarks>將選取清單更新，並更新 SelectedItem</remarks>
        private void SelectionCheckBoxClicked(object sender, RoutedEventArgs e)
        {
            if (sender is not CheckBox checkBox || checkBox.DataContext is not object item)
            {
                return;
            }

            ViewModel.SetItemSelected(item, checkBox.IsChecked == true);

            if(ViewModel.SelectedItems.Count == ViewModel.FilteredItems?.Cast<object>().Count())
            {
                _selectAllCheckBox.IsChecked = true;
            }
            else
            {
                _selectAllCheckBox.IsChecked = false;
            }
        }

        /// <summary>
        /// 全選
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SelectAllCheckBoxClicked(object sender, RoutedEventArgs e)
        {
            if (sender is not CheckBox checkBox) { return; }


            bool selectAll = checkBox.IsChecked == true;

            if(!selectAll)
            {
                ViewModel.ClearSelection();
            }
            else
            {
                ViewModel.SelectAllItems();
            }

            /* 每列的選取 CheckBox.IsChecked 只在 SelectionCheckBoxLoaded 時設定一次(非 Binding)，
             * 全選/取消全選只更新 ViewModel._selectedItems，已存在的 row container 不會重新觸發
             * Loaded，畫面不會更新。Refresh() 會重新產生 row container，藉此重新觸發 Loaded 讀取最新狀態。
             */
            MainDataGrid.Items.Refresh();
        }
    }
}
