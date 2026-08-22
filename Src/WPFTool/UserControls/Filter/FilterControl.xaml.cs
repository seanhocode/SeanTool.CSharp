using System.Windows.Controls;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using SeanTool.CSharp.WPFTool.Models.Filter;

namespace SeanTool.CSharp.WPFTool.UserControls.Filter
{
    /// <summary>
    /// 篩選條件控制項
    /// </summary>
    /// <remarks>需在建立時指定 DataContext (FilterViewModel)</remarks>
    public partial class FilterControl : UserControl
    {
        public static readonly DependencyProperty AlwaysShowFilterOptionsProperty =
            DependencyProperty.Register(nameof(AlwaysShowFilterOptions), typeof(bool), typeof(FilterControl),
                new PropertyMetadata(false, OnAlwaysShowFilterOptionsChanged));

        public FilterControl()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        /// <summary>
        /// 是否總是展開篩選選項並隱藏漏斗按鈕
        /// </summary>
        public bool AlwaysShowFilterOptions
        {
            get => (bool)GetValue(AlwaysShowFilterOptionsProperty);
            set => SetValue(AlwaysShowFilterOptionsProperty, value);
        }

        /// <summary>
        /// 載入完成時，讓所在的 DataGridColumnHeader 內容跟著欄寬撐滿
        /// </summary>
        /// <remarks>
        /// 只附加設定 HorizontalContentAlignment 這一個屬性的區域值(local value)，
        /// 不建立/替換 DataGridColumnHeader 的 Style，避免蓋掉 WPF-UI 主題套用的樣式與範本。
        /// </remarks>
        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            UpdateFilterOptionsVisibility();

            DependencyObject? node = this;
            while (node is not null and not DataGridColumnHeader)
            {
                node = VisualTreeHelper.GetParent(node);
            }

            if (node is DataGridColumnHeader header)
            {
                header.HorizontalContentAlignment = HorizontalAlignment.Stretch;
            }
        }

        private static void OnAlwaysShowFilterOptionsChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            FilterControl control = (FilterControl)dependencyObject;
            if (control.IsInitialized)
            {
                control.UpdateFilterOptionsVisibility();
            }
        }

        private void UpdateFilterOptionsVisibility()
        {
            FilterOptions.Visibility = AlwaysShowFilterOptions ? Visibility.Visible : Visibility.Collapsed;
            FilterButton.Visibility = AlwaysShowFilterOptions ? Visibility.Collapsed : Visibility.Visible;
        }

        /// <summary>
        /// 展開或收合篩選選項面板
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OpenFilterOptions(object sender, RoutedEventArgs e)
        {
            FilterViewModel viewModel = (FilterViewModel)this.DataContext;
            viewModel.ClearTempCommand.Execute(null);
            FilterOptions.Visibility = FilterOptions.Visibility == Visibility.Visible
                ? Visibility.Collapsed
                : Visibility.Visible;
        }
    }
}
