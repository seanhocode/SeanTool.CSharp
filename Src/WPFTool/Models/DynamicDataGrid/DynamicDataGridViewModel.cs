using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using SeanTool.CSharp.WPFTool.Enums.Filter;
using SeanTool.CSharp.WPFTool.Models.Filter;

namespace SeanTool.CSharp.WPFTool.Models.DynamicDataGrid
{
    /// <summary>
    /// DynamicDataGrid 的 ViewModel
    /// </summary>
    public sealed class DynamicDataGridViewModel : ViewModelBase
    {
        private IEnumerable? _dataSource;
        private IEnumerable? _filteredItems;
        private readonly HashSet<object> _selectedItems = new(ReferenceEqualityComparer.Instance);
        private object? _selectedItem;
        private bool _isRebuildingFilters;

        /// <summary>
        /// DataSource 的項目型別 (Item Type)
        /// </summary>
        private Type? _itemType;

        /// <summary>
        /// 是否有手動設定欄位定義
        /// </summary>
        private bool _hasManualColumnDefinitions;

        public DynamicDataGridViewModel()
        {
            //ObservableCollection: 一個當項目被新增、移除、清空時，會自動發出通知的集合。它實作了 INotifyCollectionChanged，讓 WPF 的 UI 能自動更新畫面
            Filters = new ObservableCollection<FilterViewModel>();

            //CollectionChanged: 當集合發生變動時觸發
            Filters.CollectionChanged += FiltersChanged;
        }

        /// <summary>
        /// 資料來源
        /// </summary>
        public IEnumerable? DataSource
        {
            get => _dataSource;
            set
            {
                if (ReferenceEquals(_dataSource, value)) return;

                // 直接用舊值 cast 取消訂閱，不需額外欄位存放(舊值已存在 _dataSource 中)
                if (_dataSource is INotifyCollectionChanged oldCollection)
                {
                    oldCollection.CollectionChanged -= DataSourceCollectionChanged;
                }

                _dataSource = value;
                if (_dataSource is INotifyCollectionChanged newCollection)
                {
                    newCollection.CollectionChanged += DataSourceCollectionChanged;
                }

                _itemType = GetItemType(value);
                bool refreshedBySetColumnDefinitions = false;
                if (_hasManualColumnDefinitions)
                {
                    RebuildFilters();
                }
                else
                {
                    SetColumnDefinitions(null);
                    refreshedBySetColumnDefinitions = true;
                }
                OnPropertyChanged();
                if (!refreshedBySetColumnDefinitions)
                {
                    RefreshData();
                }
            }
        }

        /// <summary>
        /// DataSource 經過篩選後的顯示項目清單
        /// </summary>
        public IEnumerable? FilteredItems
        {
            get => _filteredItems;
            private set
            {
                if (ReferenceEquals(_filteredItems, value)) return;
                _filteredItems = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 欄位定義清單
        /// </summary>
        public IReadOnlyList<DynamicDataGridColumnDefinition> ColumnDefinitions { get; private set; } = Array.Empty<DynamicDataGridColumnDefinition>();

        /// <summary>
        /// 篩選條件清單
        /// </summary>
        /// <remarks>使用 ObservableCollection 是因為當此集合發生變動時觸發資料更新</remarks>
        public ObservableCollection<FilterViewModel> Filters { get; }

        /// <summary>
        /// DataSource 的項目型別 (Item Type)
        /// </summary>
        /// <remarks>如果是 IEnumerable<T>，則回傳 T 的型別；如果是非泛型 IEnumerable，則回傳第一個元素的型別；如果無法取得，則回傳 null</remarks>
        public Type? ItemType => _itemType;

        /// <summary>
        /// 目前選取項目
        /// </summary>
        public object? SelectedItem
        {
            get => _selectedItem;
            private set
            {
                if (ReferenceEquals(_selectedItem, value))
                {
                    return;
                }

                _selectedItem = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 目前選取項目清單
        /// </summary>
        public IReadOnlyList<object> SelectedItems => _selectedItems.ToArray();

        /// <summary>
        /// 更新 ViewModel 的欄位定義
        /// </summary>
        /// <param name="definitions"></param>
        /// <remarks>如果沒有傳入則根據 DataSource 的 ItemType 取得，如果 ItemType 是空的則欄位定義預設也會是空的</remarks>
        public void SetColumnDefinitions(IEnumerable<DynamicDataGridColumnDefinition>? definitions)
        {
            //檢查是否有手動設定欄位定義
            _hasManualColumnDefinitions = definitions is not null;

            /* 更新 ColumnDefinitions
             * 如果有手動設定則用手動設定的欄位定義，否則根據 DataSource 的 ItemType 取得欄位定義
             * 如果 ItemType 是空的則欄位定義預設也會是空的
             */
            ColumnDefinitions = definitions?.ToArray() ?? (
                _itemType is null ? Array.Empty<DynamicDataGridColumnDefinition>() : 
                    DynamicDataGridPropertyMetadata.Create(_itemType, GetSampleItem())
                        .Select(property => new DynamicDataGridColumnDefinition{
                                                Header = property.Header,
                                                BindingPath = property.Name,
                                                IsReadOnly = property.IsReadOnly,
                                                FilterValueType = GetFilterValueType(property.PropertyType)
                                            }
                        ).ToArray()
            );

            RebuildFilters();
            OnPropertyChanged(nameof(ColumnDefinitions));
            RefreshData();
        }

        /// <summary>
        /// 清除篩選條件
        /// </summary>
        /// <remarks>呼叫 DynamicDataGridFilterViewModel 的 ClearCommand</remarks>
        public void ClearFilters()
        {
            foreach (FilterViewModel filter in Filters)
            {
                filter.ClearCommand.Execute(null);
            }
        }

        /// <summary>
        /// 設定目前選取項目 (不變更選取清單)
        /// </summary>
        public void SetSelectedItem(object? item)
        {
            SelectedItem = item;
        }

        /// <summary>
        /// 設定項目是否選取
        /// </summary>
        public void SetItemSelected(object item, bool isSelected)
        {
            if (isSelected)
            {
                if (_selectedItems.Add(item))
                {
                    OnPropertyChanged(nameof(SelectedItems));
                }

                SelectedItem = item;
                return;
            }

            if (!_selectedItems.Remove(item))
            {
                return;
            }

            OnPropertyChanged(nameof(SelectedItems));
            if (ReferenceEquals(SelectedItem, item))
            {
                SelectedItem = _selectedItems.LastOrDefault();
            }
        }

        public void SelectAllItems()
        {
            if (FilteredItems is null) { return; }

            bool changed = false;

            foreach (object? item in FilteredItems)
            {
                if (item is not null)
                {
                    changed |= _selectedItems.Add(item);
                }
            }
            if (changed)
            {
                OnPropertyChanged(nameof(SelectedItems));
            }
            SelectedItem = _selectedItems.LastOrDefault();
        }

        /// <summary>
        /// 判斷項目是否已選取
        /// </summary>
        public bool IsItemSelected(object item)
        {
            return _selectedItems.Contains(item);
        }

        /// <summary>
        /// 清空選取狀態
        /// </summary>
        public void ClearSelection()
        {
            if (_selectedItems.Count == 0 && SelectedItem is null)
            {
                return;
            }

            _selectedItems.Clear();
            OnPropertyChanged(nameof(SelectedItems));
            SelectedItem = null;
        }

        /// <summary>
        /// DataSource 集合異動時同步選取狀態
        /// </summary>
        public void HandleDataSourceCollectionChanged(NotifyCollectionChangedEventArgs e, IEnumerable? source)
        {
            if (e.Action is NotifyCollectionChangedAction.Remove or NotifyCollectionChangedAction.Replace)
            {
                RemoveFromSelection(e.OldItems);
            }
            else if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                KeepCurrentSelectionOnly(source);
            }

            if (SelectedItem is object selectedItem && !_selectedItems.Contains(selectedItem))
            {
                SelectedItem = _selectedItems.LastOrDefault();
            }
        }

        /// <summary>
        /// 重新建立 Filters
        /// </summary>
        /// <remarks>根據新的 ColumnDefinitions 建立新的 Filter，故要先更新 ColumnDefinitions 再呼叫</remarks>
        private void RebuildFilters()
        {
            _isRebuildingFilters = true;
            try
            {
                // 解除舊的 Filter 的事件訂閱
                foreach (FilterViewModel filter in Filters)
                {
                    filter.PropertyChanged -= FilterChanged;
                }
                Filters.Clear();

                // 根據新的 ColumnDefinitions 建立新的 Filters(更新此ViewModel的Filters)
                foreach (DynamicDataGridColumnDefinition definition in ColumnDefinitions)
                {
                    Type? propertyType = GetPropertyType(definition.BindingPath);
                    FilterValueType filterValueType = definition.FilterValueType
                        ?? GetFilterValueType(propertyType);

                    FilterViewModel filter = new FilterViewModel(
                        definition.BindingPath,
                        definition.Header,
                        filterValueType);
                    filter.PropertyChanged += FilterChanged;
                    Filters.Add(filter);
                }
            }
            finally
            {
                _isRebuildingFilters = false;
            }
        }

        /// <summary>
        /// 取得 DataSource 的第一筆項目作為樣本
        /// </summary>
        /// <remarks>
        /// 用於掃描透過 <see cref="ICustomTypeDescriptor"/> 動態提供屬性的型別(例如繫結 DataTable 時的
        /// DataRowView)，因為這類型別的欄位是由實例(對應到當下 DataTable 的 Columns)而非型別本身決定。
        /// </remarks>
        private object? GetSampleItem()
        {
            return _dataSource?.Cast<object?>().FirstOrDefault(item => item is not null);
        }

        /// <summary>
        /// 取得指定屬性名稱對應的型別
        /// </summary>
        private Type? GetPropertyType(string propertyName)
        {
            if (_itemType is null)
            {
                return null;
            }

            object? sample = GetSampleItem();
            PropertyDescriptorCollection properties = sample is not null && _itemType.IsInstanceOfType(sample)
                ? TypeDescriptor.GetProperties(sample)
                : TypeDescriptor.GetProperties(_itemType);
            return properties[propertyName]?.PropertyType;
        }

        /// <summary>
        /// 取得屬性型別對應的 FilterValueType
        /// </summary>
        /// <param name="propertyType"></param>
        /// <returns></returns>
        /// <remarks>傳入 null 時回傳 FilterValueType.Text</remarks>
        private static FilterValueType GetFilterValueType(Type? propertyType)
        {
            if (propertyType is null)
            {
                return FilterValueType.Text;
            }

            Type nonNullableType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;
            return nonNullableType == typeof(DateTime)
                ? FilterValueType.DateTime
                : FilterValueType.Text;
        }

        /// <summary>
        /// 篩選條件 List 異動時觸發
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <remarks>更新 FilteredItems</remarks>
        private void FiltersChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (_isRebuildingFilters)
            {
                return;
            }

            RefreshData();
        }

        /// <summary>
        /// DataSource 異動時觸發
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DataSourceCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            HandleDataSourceCollectionChanged(e, sender as IEnumerable);
            RefreshData();
        }

        /// <summary>
        /// 篩選條件異動時觸發
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <remarks>更新 FilteredItems</remarks>
        private void FilterChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(FilterViewModel.AppliedFilter))
            {
                RefreshData();
            }
        }

        /// <summary>
        /// 更新 FilteredItems
        /// </summary>
        public void RefreshData()
        {
            if (_dataSource is null || _itemType is null)
            {
                FilteredItems = _dataSource;
                return;
            }

            FilteredItems = FilterQuery.Apply(
                _dataSource,
                _itemType,
                Filters.Select(filter => filter.AppliedFilter).Where(filter => filter is not null).Cast<FilterCondition>());

            if (_selectedItems.Count > 0 && HasActiveFilters())
            {
                KeepCurrentSelectionOnly(FilteredItems);
                if (SelectedItem is object selectedItem && !_selectedItems.Contains(selectedItem))
                {
                    SelectedItem = _selectedItems.LastOrDefault();
                }
            }
        }

        /// <summary>
        /// 取得物件的項目型別 (Item Type)
        /// </summary>
        /// <param name="source"></param>
        /// <returns>如果是 IEnumerable<T>，則回傳 T 的型別；如果是非泛型 IEnumerable，則回傳第一個元素的型別；如果無法取得，則回傳 null</returns>
        private static Type? GetItemType(object? source)
        {
            if (source is null) return null;

            Type sourceType = source.GetType();
            Type? itemType = sourceType.GetInterfaces()
                .Append(sourceType)
                .Select(type => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>)
                    ? type.GetGenericArguments()[0]
                    : null)
                .FirstOrDefault(type => type is not null);

            return itemType ?? (source as IEnumerable)?.Cast<object?>().FirstOrDefault()?.GetType();
        }

        /// <summary>
        /// 選取清單移除項目
        /// </summary>
        /// <param name="oldItems">要移除的項目</param>
        private void RemoveFromSelection(IList? oldItems)
        {
            if (oldItems is null) { return; }

            bool changed = false;
            foreach (object? item in oldItems)
            {
                if (item is not null)
                {
                    changed |= _selectedItems.Remove(item);
                }
            }

            if (changed)
            {
                OnPropertyChanged(nameof(SelectedItems));
            }
        }

        /// <summary>
        /// 移除不在新清單中的已選取項目
        /// </summary>
        /// <param name="source">新的項目清單</param>
        private void KeepCurrentSelectionOnly(IEnumerable? source)
        {
            if (source is null) { return; }

            HashSet<object?> currentItems = source.Cast<object?>().ToHashSet(ReferenceEqualityComparer.Instance);
            int previousCount = _selectedItems.Count;
            _selectedItems.RemoveWhere(item => !currentItems.Contains(item));
            if (_selectedItems.Count != previousCount)
            {
                OnPropertyChanged(nameof(SelectedItems));
            }
        }

        /// <summary>
        /// 是否有生效的篩選條件
        /// </summary>
        /// <returns></returns>
        private bool HasActiveFilters()
        {
            return Filters.Any(filter => filter.AppliedFilter is not null);
        }
    }
}
