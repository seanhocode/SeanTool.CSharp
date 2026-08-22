using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;
using SeanTool.CSharp.WPFTool.Models.DropDownList;

namespace SeanTool.CSharp.WPFTool.UserControls.DropDownList
{
    /// <summary>
    /// 支援搜尋、單選/多選的下拉清單控制項。
    /// 搜尋與選取邏輯全部委派給 <see cref="DropDownListViewModel"/>（不依賴 Dispatcher/視覺樹），
    /// 本類別只負責 DependencyProperty 對外綁定與滑鼠/彈出視窗互動。
    /// </summary>
    public partial class DropDownList : UserControl, INotifyPropertyChanged
    {
        private readonly DropDownListViewModel _viewModel = new();
        private bool _isSyncingSelectedValue;
        private bool _isSyncingSelectedValues;
        private bool _isSyncingText;

        // ponytail: n 筆項目時，每個按鍵都同步觸發 FilteredItems Clear+n次Add 太卡；
        // 用 DispatcherTimer debounce，停止輸入 150ms 後才真的套用搜尋。項目數再往上一個數量級，
        // 才需要考慮把 ObservableCollection 換成支援批次通知(Reset)的集合。
        private readonly DispatcherTimer _searchDebounceTimer = new() { Interval = TimeSpan.FromMilliseconds(1000) };

        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register(
                nameof(ItemsSource),
                typeof(IEnumerable),
                typeof(DropDownList),
                new PropertyMetadata(null, OnItemsSourceChanged));

        public static readonly DependencyProperty DisplayMemberPathProperty =
            DependencyProperty.Register(
                nameof(DisplayMemberPath),
                typeof(string),
                typeof(DropDownList),
                new PropertyMetadata(null, OnDisplayMemberPathChanged));

        public static readonly DependencyProperty SelectionModeProperty =
            DependencyProperty.Register(
                nameof(SelectionMode),
                typeof(SelectionMode),
                typeof(DropDownList),
                new PropertyMetadata(SelectionMode.Single, OnSelectionModeChanged));

        public static readonly DependencyProperty SelectedValueProperty =
            DependencyProperty.Register(
                nameof(SelectedValue),
                typeof(object),
                typeof(DropDownList),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedValueChanged));

        public static readonly DependencyProperty SelectedValuesProperty =
            DependencyProperty.Register(
                nameof(SelectedValues),
                typeof(IEnumerable),
                typeof(DropDownList),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedValuesChanged));

        public static readonly DependencyProperty PlaceholderTextProperty =
            DependencyProperty.Register(
                nameof(PlaceholderText),
                typeof(string),
                typeof(DropDownList),
                new PropertyMetadata("請選擇"));

        public static readonly DependencyProperty IsDropDownOpenProperty =
            DependencyProperty.Register(
                nameof(IsDropDownOpen),
                typeof(bool),
                typeof(DropDownList),
                new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public DropDownList()
        {
            _viewModel.SelectionChanged += ViewModelSelectionChanged;
            _searchDebounceTimer.Tick += SearchDebounceTimerTick;
            InitializeComponent();
            RefreshDisplayText();
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// 供內部 XAML 綁定使用的核心 ViewModel
        /// </summary>
        public DropDownListViewModel ViewModel => _viewModel;

        public IEnumerable? ItemsSource
        {
            get => (IEnumerable?)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        public string? DisplayMemberPath
        {
            get => (string?)GetValue(DisplayMemberPathProperty);
            set => SetValue(DisplayMemberPathProperty, value);
        }

        public SelectionMode SelectionMode
        {
            get => (SelectionMode)GetValue(SelectionModeProperty);
            set => SetValue(SelectionModeProperty, value);
        }

        /// <summary>
        /// 是否為多選模式，供內部 CheckBox 顯示與否使用
        /// </summary>
        public bool IsMultiSelect => SelectionMode != SelectionMode.Single;

        /// <summary>
        /// 單選模式下目前選取的原始值
        /// </summary>
        public object? SelectedValue
        {
            get => GetValue(SelectedValueProperty);
            set => SetValue(SelectedValueProperty, value);
        }

        /// <summary>
        /// 多選模式下目前選取的原始值集合
        /// </summary>
        public IEnumerable? SelectedValues
        {
            get => (IEnumerable?)GetValue(SelectedValuesProperty);
            set => SetValue(SelectedValuesProperty, value);
        }

        public string PlaceholderText
        {
            get => (string)GetValue(PlaceholderTextProperty);
            set => SetValue(PlaceholderTextProperty, value);
        }

        public bool IsDropDownOpen
        {
            get => (bool)GetValue(IsDropDownOpenProperty);
            set => SetValue(IsDropDownOpenProperty, value);
        }

        private static void OnItemsSourceChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            ((DropDownList)dependencyObject)._viewModel.ItemsSource = (IEnumerable?)e.NewValue;
        }

        private static void OnDisplayMemberPathChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            ((DropDownList)dependencyObject)._viewModel.DisplayMemberPath = (string?)e.NewValue;
        }

        private static void OnSelectionModeChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            DropDownList control = (DropDownList)dependencyObject;
            control._viewModel.SelectionMode = (SelectionMode)e.NewValue;
            control.PropertyChanged?.Invoke(control, new PropertyChangedEventArgs(nameof(IsMultiSelect)));
        }

        private static void OnSelectedValueChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            DropDownList control = (DropDownList)dependencyObject;
            if (control._isSyncingSelectedValue)
            {
                return;
            }

            control._viewModel.SelectValue(e.NewValue);
        }

        private static void OnSelectedValuesChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            DropDownList control = (DropDownList)dependencyObject;
            if (control._isSyncingSelectedValues)
            {
                return;
            }

            control._viewModel.SelectValues(((IEnumerable?)e.NewValue)?.Cast<object?>());
        }

        private void ViewModelSelectionChanged(object? sender, EventArgs e)
        {
            _isSyncingSelectedValue = true;
            SetCurrentValue(SelectedValueProperty, _viewModel.SelectedValue);
            _isSyncingSelectedValue = false;

            _isSyncingSelectedValues = true;
            SetCurrentValue(SelectedValuesProperty, _viewModel.SelectedValues);
            _isSyncingSelectedValues = false;

            if (!ComboBoxControl.IsDropDownOpen)
            {
                // 下拉關閉時(例如外部程式呼叫 ViewModel.SelectValue)，選取摘要文字也要同步更新。
                ScheduleDisplayTextRefresh();
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // 真正的根因：WPF-UI 的 ComboBox 範本裡 Popup 沒有把 StaysOpen 綁出來，預設值是 True，
            // 導致 Popup 完全沒有「點擊外部空白處自動關閉」的原生行為——
            // 這也是為何點其他「控制項」看似正常(那是因為該控制項本身搶走焦點而非 Popup 自己關閉)，
            // 點真正的空白處却完全沒反應。直接把範本內的 Popup.StaysOpen 改回 False，
            // 讓 WPF 內建、經過驗證的「點外部即關閉」機制生效，不必自己在 Window 層猜測/攔截滑鼠事件。
            ComboBoxControl.ApplyTemplate();
            if (ComboBoxControl.Template?.FindName("Popup", ComboBoxControl) is Popup popup)
            {
                popup.StaysOpen = false;
            }
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            _searchDebounceTimer.Stop();
        }

        private void ComboBoxGotFocus(object sender, RoutedEventArgs e)
        {
            // 原生 ComboBox 只有點下拉箭頭才會開合，點文字輸入框並不會；
            // 這裡讓「點輸入框」等同於「點開下拉」，符合可搜尋下拉的直覺操作。
            if (!ComboBoxControl.IsDropDownOpen)
            {
                ComboBoxControl.IsDropDownOpen = true;
            }

            // 進入輸入框就應該能直接打字搜尋，不能讓選取摘要/預留字擋在前面。
            // 不能只靠 DropDownOpened 觸發：IsDropDownOpen 已是 true 的情況下(例如
            // 下拉還沒真正收合就再次取得焦點)不會重新觸發 DropDownOpened，摘要文字就會殘留。
            ScheduleSearchTextClear();
        }

        private void ComboBoxItemPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            // 根因：單選模式若讓點擊走原生選取流程，ComboBox(IsEditable=True) 會在失焦時
            // 用自己的邏輯拿 SelectedItem 對比目前 Text，比對不到(我們顯示的是自訂摘要文字，
            // 不是原生 ToString())就會直接把 Text 清空。多選原本就在此攔截、從未觸發過這段
            // 原生同步，因此不會有此問題；單選改成一致做法，兩種模式都不讓 ComboBox 自己選取。
            if (sender is FrameworkElement { DataContext: DropDownItemViewModel item })
            {
                _viewModel.ToggleSelected(item);
            }

            if (!IsMultiSelect)
            {
                // 單選維持原本「選完就收合下拉」的行為。
                ComboBoxControl.IsDropDownOpen = false;
            }

            // 攔在 Preview 階段：讓 ComboBoxItem 完全不進入原生選取流程，
            // 也不會觸發 WPF-UI 樣式對選取狀態播放的圖示動畫。
            e.Handled = true;
        }

        private void ComboBoxSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isSyncingText)
            {
                // 這次 Text 變更是程式自己寫入的顯示摘要(選取結果/預留字)，不是使用者在搜尋，
                // 不應該把選取項目的名稱當成下一次的搜尋字串重新套用 filter。
                return;
            }

            // 停止輸入 150ms 後才真正套用搜尋，避免每個按鍵都同步掃完整清單。
            _searchDebounceTimer.Stop();
            _searchDebounceTimer.Start();
        }

        private void SearchDebounceTimerTick(object? sender, EventArgs e)
        {
            _searchDebounceTimer.Stop();
            _viewModel.SearchText = ComboBoxControl.Text;
        }

        private void ComboBoxDropDownOpened(object sender, EventArgs e)
        {
            ScheduleSearchTextClear();
        }

        private void ScheduleSearchTextClear()
        {
            // 根因同 ScheduleDisplayTextRefresh：ComboBox(IsEditable=True) 開啟下拉/取得焦點時，
            // WPF 內部會自己排程一次「用原生 SelectedItem 顯示字串同步 Text」，其排程優先權比
            // 這裡同步寫入用的 Normal 高，會在我們清空之後又把預留字/摘要蓋回來。用 ContextIdle
            // 排到比原生同步更後面執行，確保清空是佇列裡最後一筆、不會被蓋掉。
            Dispatcher.BeginInvoke(() =>
            {
                SetTextWithoutTriggeringSearch(string.Empty);
                _viewModel.SearchText = string.Empty;
            }, DispatcherPriority.ContextIdle);
        }

        private void ComboBoxDropDownClosed(object sender, EventArgs e)
        {
            // 不論下拉是因為選取、按 Esc、或點擊外部空白處而關閉，只要收合了，
            // 輸入框就不該再保留鍵盤焦點，否則游標會持續閃爍，讓人誤以為還在搜尋。
            Keyboard.ClearFocus();
            ScheduleDisplayTextRefresh();
        }

        private void ComboBoxLostFocus(object sender, RoutedEventArgs e)
        {
            ScheduleDisplayTextRefresh();
        }

        private void ScheduleDisplayTextRefresh()
        {
            // 根因：ComboBox 是可編輯(IsEditable=True)控制項，選取變更/失焦時，
            // WPF 內部會自己排程一次「用原生 SelectedItem 的顯示字串同步 Text」，
            // 我們的 DataTemplate/DisplayText 對不上原生預設字串，所以這個內部同步
            // 一定會把 Text 洗成空白——而且它的排程優先權比一般事件處理常式(Normal)高，
            // 導致我們在 DropDownClosed/LostFocus 當下寫入的正確文字之後又被蓋掉，
            // 只有等下一次真正的 UI 事件(例如點別的控制項)把 Dispatcher 佇列推進，
            // 才會意外地把畫面刷新成新值。
            // 用 ContextIdle(比原生同步用的優先權都低)延後執行，確保這次寫入
            // 一定是佇列裡最後一筆，不會再被原生同步覆蓋。
            Dispatcher.BeginInvoke(RefreshDisplayText, DispatcherPriority.ContextIdle);
        }

        private void RefreshDisplayText()
        {
            // 多選時不要把逗號串接的完整名稱塞進「可編輯」的輸入框裡，
            // 那看起來就像使用者自己打的搜尋字串，容易誤會還能繼續編輯/搜尋。
            // 改顯示「已選擇 N 項」，完整名單改用 ToolTip 呈現即可。
            if (IsMultiSelect)
            {
                int count = _viewModel.SelectedValues.Count;
                SetTextWithoutTriggeringSearch(count == 0 ? PlaceholderText : $"已選擇 {count} 項");
                ComboBoxControl.ToolTip = count == 0 ? "輸入關鍵字搜尋" : _viewModel.SelectionSummary;
                return;
            }

            string summary = _viewModel.SelectionSummary;
            SetTextWithoutTriggeringSearch(string.IsNullOrEmpty(summary) ? PlaceholderText : summary);
        }

        private void SetTextWithoutTriggeringSearch(string text)
        {
            // 根因修正：ComboBoxControl.Text 是程式寫入的顯示摘要/預留字，不是使用者輸入，
            // 用旗標讓 ComboBoxSearchTextChanged 略過這次變更，避免選取結果的名稱被誤當成搜尋字串，
            // 重新套用 filter 導致 FilteredItems 被清空重建、連帶把剛顯示的選取文字洗掉。
            _isSyncingText = true;
            ComboBoxControl.Text = text;
            _isSyncingText = false;
        }
    }
}
