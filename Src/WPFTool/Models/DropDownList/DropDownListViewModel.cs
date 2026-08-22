using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Windows.Controls;
using SeanTool.CSharp.WPFTool.Enums.Filter;
using SeanTool.CSharp.WPFTool.Models.Filter;

namespace SeanTool.CSharp.WPFTool.Models.DropDownList
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

        /// <summary>
        /// 依 ItemsSource 包裝出的完整項目清單
        /// </summary>
        public ObservableCollection<DropDownItemViewModel> Items { get; } = [];

        /// <summary>
        /// 搜尋後顯示於下拉清單的項目
        /// </summary>
        public ObservableCollection<DropDownItemViewModel> FilteredItems { get; } = [];

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
            if (e.PropertyName == nameof(Filter.FilterViewModel.AppliedFilter))
            {
                RefreshFilteredItems();
            }
            else if (e.PropertyName == nameof(Filter.FilterViewModel.Value))
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

            Items.Clear();
            if (_itemsSource is not null)
            {
                foreach (object? value in _itemsSource)
                {
                    DropDownItemViewModel item = new(value, ResolveDisplayText(value));
                    item.PropertyChanged += ItemPropertyChanged;
                    Items.Add(item);
                }
            }

            // ponytail: 用 Value 相等比對還原選取狀態，來源重建(例如重新查詢)時不會無故清空使用者的選取。
            if (previousSelectedValues.Count > 0)
            {
                HashSet<object?> keep = new(previousSelectedValues);
                foreach (DropDownItemViewModel item in Items)
                {
                    item.IsSelected = keep.Contains(item.Value);
                }
            }

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
            IEnumerable<DropDownItemViewModel> result = filter is null
                ? Items
                : FilterQuery.Apply(Items, typeof(DropDownItemViewModel), new[] { filter }).Cast<DropDownItemViewModel>();

            FilteredItems.Clear();
            foreach (DropDownItemViewModel item in result)
            {
                FilteredItems.Add(item);
            }
        }

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

            PropertyInfo? property = value.GetType().GetProperty(DisplayMemberPath);
            return property?.GetValue(value)?.ToString() ?? string.Empty;
        }
    }
}
