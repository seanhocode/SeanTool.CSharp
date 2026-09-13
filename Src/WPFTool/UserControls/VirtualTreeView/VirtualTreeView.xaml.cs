using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace SeanTool.CSharp.WPFTool
{
    /// <summary>
    /// 樹狀檢視器
    /// </summary>
    /// <remarks>
    /// 為何實作 <see cref="INotifyPropertyChanged"/>：
    /// 本控制項內部 XAML（如 TreeView.ItemsSource 與 FilterControl.DataContext）透過 RelativeSource 繫結至
    /// 本身的唯讀 CLR 屬性（如 <see cref="FilteredItems"/>、<see cref="FilterViewModel"/>、<see cref="CheckedItems"/>）
    /// 這些屬性並非 DependencyProperty，當內部 ViewModel 的資料異動時，必須透過實作 INotifyPropertyChanged
    /// 發出 PropertyChanged 事件，WPF 繫結引擎才能感知變更並即時更新 UI
    /// </remarks>
    public partial class VirtualTreeView : UserControl, INotifyPropertyChanged
    {
        /// <summary>
        /// 資料來源相依屬性
        /// </summary>
        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register(
                nameof(ItemsSource),
                typeof(IEnumerable<TreeNodeViewModel>),
                typeof(VirtualTreeView),
                new PropertyMetadata(null, OnSearchInputChanged));

        /// <summary>
        /// 是否顯示 CheckBox 相依屬性
        /// </summary>
        public static readonly DependencyProperty IsCheckVisibleProperty =
            DependencyProperty.Register(nameof(IsCheckVisible), typeof(bool), typeof(VirtualTreeView), new PropertyMetadata(true));

        /// <summary>
        /// 選取項目相依屬性
        /// </summary>
        /// <remarks>單選，非勾選項目</remarks>
        public static readonly DependencyProperty SelectedItemProperty =
            DependencyProperty.Register(
                nameof(SelectedItem),
                typeof(TreeNodeViewModel),
                typeof(VirtualTreeView),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedItemChanged));

        /// <summary>
        /// VirtualTreeViewViewModel
        /// </summary>
        private readonly VirtualTreeViewViewModel _viewModel = new();

        /// <summary>
        /// 篩選後的資料來源
        /// </summary>
        /// <remarks>綁定至 TreeView 的 ItemsSource</remarks>
        public ObservableCollection<TreeNodeViewModel> FilteredItems => _viewModel.FilteredItems;

        /// <summary>
        /// 篩選器 ViewModel
        /// </summary>
        public FilterViewModel FilterViewModel => _viewModel.FilterViewModel;

        /// <summary>
        /// 勾選項目清單
        /// </summary>
        /// <remarks>多選，勾選的項目，需開啟 IsCheckVisible</remarks>
        public ObservableCollection<TreeNodeViewModel> CheckedItems => _viewModel.CheckedItems;

        /// <summary>
        /// 選取項目
        /// </summary>
        /// <remarks>單選，反白的項目</remarks>
        public TreeNodeViewModel? SelectedItem
        {
            get => (TreeNodeViewModel?)GetValue(SelectedItemProperty);
            set => SetValue(SelectedItemProperty, value);
        }

        /// <summary>
        /// 選取項目的值清單
        /// </summary>
        public IReadOnlyList<object?> CheckedValues => _viewModel.CheckedValues;

        /// <summary>
        /// 是否顯示 CheckBox
        /// </summary>
        public bool IsCheckVisible
        {
            get => (bool)GetValue(IsCheckVisibleProperty);
            set => SetValue(IsCheckVisibleProperty, value);
        }

        /// <summary>
        /// 資料來源
        /// </summary>
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

        /// <summary>
        /// 屬性變更通知事件（實作 <see cref="INotifyPropertyChanged"/>）
        /// </summary>
        /// <remarks>
        /// 使用時機與觸發點：
        /// <para>1. <see cref="OnSelectedItemChanged"/>：相依屬性 SelectedItem 變更時觸發通知</para>
        /// <para>2. <see cref="ViewModelPropertyChanged"/>：內部 ViewModel 的 FilteredItems 或 CheckedItems 變更時，轉發通知給 XAML 繫結以刷新畫面</para>
        /// </remarks>
        public event PropertyChangedEventHandler? PropertyChanged;

        private static void OnSearchInputChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            VirtualTreeView control = (VirtualTreeView)dependencyObject;
            control._viewModel.ItemsSource = control.ItemsSource;
        }

        /// <summary>
        /// 選取項目變更後觸發事件
        /// </summary>
        /// <param name="dependencyObject"></param>
        /// <param name="e"></param>
        /// <remarks>更新 ViewModel 的 SelectedItem</remarks>
        private static void OnSelectedItemChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            VirtualTreeView control = (VirtualTreeView)dependencyObject;
            control._viewModel.SelectedItem = (TreeNodeViewModel?)e.NewValue;
            control.PropertyChanged?.Invoke(dependencyObject, new PropertyChangedEventArgs(nameof(SelectedItem)));
        }

        /// <summary>
        /// ViewModel 屬性變更後觸發
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch(e.PropertyName)
            {
                // 篩選後的資料來源變更，通知外部更新 FilteredItems
                case nameof(VirtualTreeViewViewModel.FilteredItems):
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FilteredItems)));
                    break;
                case nameof(VirtualTreeViewViewModel.CheckedItems):
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CheckedItems)));
                    break;
            }
        }

        /// <summary>
        /// <see cref="TreeView.SelectedItemChanged"/> 改變時觸發
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TreeViewSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            //更新 SelectedItem 相依屬性，觸發 OnSelectedItemChanged 事件，進而更新 ViewModel 的 SelectedItem
            SetCurrentValue(SelectedItemProperty, e.NewValue as TreeNodeViewModel);
        }

        /// <summary>
        /// 全部展開按鈕點擊事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ExpandAllClick(object sender, RoutedEventArgs e)
        {
            _viewModel.ExpandAll();
        }

        /// <summary>
        /// 全部收合按鈕點擊事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CollapseAllClick(object sender, RoutedEventArgs e)
        {
            _viewModel.CollapseAll();
        }
    }
}
