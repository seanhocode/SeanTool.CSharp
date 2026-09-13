using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Windows.Controls;
using SeanTool.CSharp.WPFTool;

namespace SeanTool.CSharp.WPFTool
{
    /// <summary>
    /// DropDownList 的核心邏輯：包裝來源項目、以既有的 FilterCondition/FilterQuery 依 DisplayText 搜尋，
    /// 並維護單選/多選狀態。獨立於 UserControl/Dispatcher，方便單元測試涵蓋所有分支。
    /// </summary>
    public class DropDownListViewModel : ViewModelBase
    {
        private IEnumerable? _itemsSource;
        private INotifyCollectionChanged? _observedSource;
        private string? _displayMemberPath;
        private SelectionMode _selectionMode = SelectionMode.Single;

        public DropDownListViewModel()
        {
            FilterViewModel = new FilterViewModel(nameof(DropDownItemViewModel.DisplayText), "搜尋", FilterValueType.Text);
            FilterViewModel.PropertyChanged += FilterViewModelPropertyChanged;
        }

        /// <summary>
        /// 搜尋條件 ViewModel，共用既有的 FilterCondition/FilterQuery 機制
        /// </summary>
        public FilterViewModel FilterViewModel { get; }

        private IReadOnlyList<DropDownItemViewModel> _items = Array.Empty<DropDownItemViewModel>();

        /// <summary>
        /// 依 ItemsSource 包裝出的完整項目清單
        /// </summary>
        /// <remarks>
        /// ponytail: 原本是固定的 ObservableCollection，RebuildItems 每次都 Clear()+逐筆 Add()；
        /// 項目數大時，光是重建就會觸發 N 次 CollectionChanged。改成整包在背景 List 組好後
        /// 一次性換參考(單次 PropertyChanged)，理由與 FilteredItems 相同。
        /// </remarks>
        public IReadOnlyList<DropDownItemViewModel> Items
        {
            get => _items;
            private set
            {
                _items = value;
                OnPropertyChanged();
            }
        }

        private IReadOnlyList<DropDownItemViewModel> _filteredItems = Array.Empty<DropDownItemViewModel>();

        /// <summary>
        /// 搜尋後顯示於下拉清單的項目
        /// </summary>
        /// <remarks>
        /// ponytail: 原本是固定的 ObservableCollection，每次搜尋都 Clear()+逐筆 Add()；
        /// 項目數大(例如數十萬筆)時，光是觸發 N 次 CollectionChanged 通知就會卡住 UI，
        /// 而每次聚焦搜尋框都會經由 <see cref="DropDownItemViewModel"/> 的 GotFocus 清空搜尋文字、
        /// 重新套用一次篩選，等於每次聚焦都重播這個 O(n) 通知風暴。改成整包替換參考(單次 PropertyChanged)，
        /// 與 <see cref="DynamicDataGrid.DynamicDataGridViewModel.FilteredItems"/> 作法一致。
        /// </remarks>
        public IReadOnlyList<DropDownItemViewModel> FilteredItems
        {
            get => _filteredItems;
            private set
            {
                _filteredItems = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 項目顯示文字對應的屬性名稱；未設定時使用 ToString()
        /// </summary>
        public string? DisplayMemberPath
        {
            get => _displayMemberPath;
            set
            {
                if (_displayMemberPath == value)
                {
                    return;
                }

                _displayMemberPath = value;
                RebuildItems();
            }
        }

        /// <summary>
        /// 單選/多選模式 (沿用 System.Windows.Controls.SelectionMode，避免自訂重複的列舉)
        /// </summary>
        public SelectionMode SelectionMode
        {
            get => _selectionMode;
            set
            {
                if (_selectionMode == value)
                {
                    return;
                }

                _selectionMode = value;
                if (value == SelectionMode.Single)
                {
                    // 切換為單選時只保留第一個已選項目，避免殘留多選狀態
                    bool keepFirst = true;
                    foreach (DropDownItemViewModel item in Items)
                    {
                        if (item.IsSelected && keepFirst)
                        {
                            keepFirst = false;
                            continue;
                        }

                        item.IsSelected = false;
                    }
                }

                OnPropertyChanged();
            }
        }

        public IEnumerable? ItemsSource
        {
            get => _itemsSource;
            set
            {
                if (ReferenceEquals(_itemsSource, value))
                {
                    return;
                }

                DetachSource();
                _itemsSource = value;
                AttachSource();
                RebuildItems();
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 搜尋文字，雙向對應至 FilterViewModel.Value 並即時套用(不需按查詢按鈕)
        /// </summary>
        public string SearchText
        {
            get => FilterViewModel.Value;
            set
            {
                if (FilterViewModel.Value == value)
                {
                    return;
                }

                FilterViewModel.Value = value;
                FilterViewModel.ApplyCommand.Execute(null);
            }
        }

        /// <summary>
        /// 單選模式下目前選取項目的原始值
        /// </summary>
        public object? SelectedValue => Items.FirstOrDefault(item => item.IsSelected)?.Value;

        /// <summary>
        /// 多選模式下所有已選取項目的原始值
        /// </summary>
        public IReadOnlyList<object?> SelectedValues =>
            Items.Where(item => item.IsSelected).Select(item => item.Value).ToArray();

        /// <summary>
        /// 顯示於下拉清單頭部的摘要文字
        /// </summary>
        public string SelectionSummary => SelectionMode == SelectionMode.Single
            ? Items.FirstOrDefault(item => item.IsSelected)?.DisplayText ?? string.Empty
            : string.Join(", ", Items.Where(item => item.IsSelected).Select(item => item.DisplayText));

        /// <summary>
        /// 選取狀態(SelectedValue/SelectedValues)變更時觸發
        /// </summary>
        public event EventHandler? SelectionChanged;

        /// <summary>
        /// 切換單一項目的選取狀態；單選模式下會先清除其他已選項目
        /// </summary>
        public void ToggleSelected(DropDownItemViewModel item)
        {
            ArgumentNullException.ThrowIfNull(item);
            if (!Items.Contains(item))
            {
                return;
            }

            if (SelectionMode == SelectionMode.Single)
            {
                foreach (DropDownItemViewModel other in Items)
                {
                    other.IsSelected = ReferenceEquals(other, item);
                }
            }
            else
            {
                item.IsSelected = !item.IsSelected;
            }
        }

        /// <summary>
        /// 依原始值設定單選選取狀態 (供外部以 SelectedValue 反向套用)
        /// </summary>
        public void SelectValue(object? value)
        {
            foreach (DropDownItemViewModel item in Items)
            {
                item.IsSelected = Equals(item.Value, value);
            }
        }

        /// <summary>
        /// 依原始值集合設定多選選取狀態 (供外部以 SelectedValues 反向套用)
        /// </summary>
        public void SelectValues(IEnumerable<object?>? values)
        {
            HashSet<object?> set = new(values ?? Enumerable.Empty<object?>());
            foreach (DropDownItemViewModel item in Items)
            {
                item.IsSelected = set.Contains(item.Value);
            }
        }

        public void ClearSelection()
        {
            foreach (DropDownItemViewModel item in Items)
            {
                item.IsSelected = false;
            }
        }

        private void FilterViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(FilterViewModel.AppliedFilter))
            {
                RefreshFilteredItems();
            }
            else if (e.PropertyName == nameof(FilterViewModel.Value))
            {
                OnPropertyChanged(nameof(SearchText));
            }
        }

        private void AttachSource()
        {
            if (_itemsSource is INotifyCollectionChanged collection)
            {
                collection.CollectionChanged += SourceCollectionChanged;
                _observedSource = collection;
            }
        }

        private void DetachSource()
        {
            if (_observedSource is not null)
            {
                _observedSource.CollectionChanged -= SourceCollectionChanged;
                _observedSource = null;
            }
        }

        private void SourceCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => RebuildItems();

        private void RebuildItems()
        {
            IReadOnlyList<object?> previousSelectedValues = Items.Where(item => item.IsSelected).Select(item => item.Value).ToArray();

            foreach (DropDownItemViewModel item in Items)
            {
                item.PropertyChanged -= ItemPropertyChanged;
            }

            // 先在一般 List 組好整批項目(不觸發任何通知)，最後才整包換上 Items 參考，
            // 避免舊做法對 ObservableCollection 逐筆 Add 造成的 N 次 CollectionChanged。
            List<DropDownItemViewModel> items = [];
            if (_itemsSource is not null)
            {
                foreach (object? value in _itemsSource)
                {
                    DropDownItemViewModel item = new(value, ResolveDisplayText(value));
                    item.PropertyChanged += ItemPropertyChanged;
                    items.Add(item);
                }
            }

            // ponytail: 用 Value 相等比對還原選取狀態，來源重建(例如重新查詢)時不會無故清空使用者的選取。
            if (previousSelectedValues.Count > 0)
            {
                HashSet<object?> keep = new(previousSelectedValues);
                foreach (DropDownItemViewModel item in items)
                {
                    item.IsSelected = keep.Contains(item.Value);
                }
            }

            Items = items;
            RefreshFilteredItems();
            NotifySelectionChanged();
        }

        private void ItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(DropDownItemViewModel.IsSelected))
            {
                NotifySelectionChanged();
            }
        }

        private void NotifySelectionChanged()
        {
            OnPropertyChanged(nameof(SelectedValue));
            OnPropertyChanged(nameof(SelectedValues));
            OnPropertyChanged(nameof(SelectionSummary));
            SelectionChanged?.Invoke(this, EventArgs.Empty);
        }

        private void RefreshFilteredItems()
        {
            FilterCondition? filter = FilterViewModel.AppliedFilter;

            // 沒有篩選時直接沿用 Items 本身，不需要整包複製一份陣列。
            FilteredItems = filter is null
                ? Items
                : FilterQuery.Apply(Items, typeof(DropDownItemViewModel), new[] { filter }).Cast<DropDownItemViewModel>().ToArray();
        }

        // ponytail: 依 (Type, DisplayMemberPath) 快取 PropertyInfo，避免整批項目逐筆反射查找同一個屬性，
        // 作法與 FilterQuery.GetCachedProperty 一致。
        private static readonly Dictionary<(Type Type, string PropertyName), PropertyInfo?> _propertyCache = [];

        private string ResolveDisplayText(object? value)
        {
            if (value is null)
            {
                return string.Empty;
            }

            if (string.IsNullOrEmpty(DisplayMemberPath))
            {
                return value.ToString() ?? string.Empty;
            }

            Type type = value.GetType();
            (Type type, string DisplayMemberPath) key = (type, DisplayMemberPath);
            if (!_propertyCache.TryGetValue(key, out PropertyInfo? property))
            {
                property = type.GetProperty(DisplayMemberPath);
                _propertyCache[key] = property;
            }

            return property?.GetValue(value)?.ToString() ?? string.Empty;
        }
    }
}
