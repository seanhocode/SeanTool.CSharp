using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace SeanTool.CSharp.WPF
{
    public sealed class DynamicDataGridViewModel : ViewModelBase
    {
        private IEnumerable? _dataSource;
        private IEnumerable? _displayItems;
        private string? _sortProperty;
        private bool _sortDescending;

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
            Filters = new ObservableCollection<DynamicDataGridFilterViewModel>();
            Filters.CollectionChanged += FiltersChanged;
        }

        public IEnumerable? DataSource
        {
            get => _dataSource;
            set
            {
                if (ReferenceEquals(_dataSource, value)) return;
                _dataSource = value;
                _itemType = GetItemType(value);
                if (_hasManualColumnDefinitions)
                {
                    RebuildFilters();
                }
                else
                {
                    SetColumnDefinitions(null);
                }
                OnPropertyChanged();
                RefreshData();
            }
        }

        public IEnumerable? DisplayItems
        {
            get => _displayItems;
            private set
            {
                _displayItems = value;
                OnPropertyChanged();
            }
        }

        public IReadOnlyList<DynamicDataGridColumnDefinition> ColumnDefinitions { get; private set; } = Array.Empty<DynamicDataGridColumnDefinition>();

        public ObservableCollection<DynamicDataGridFilterViewModel> Filters { get; }

        public string? SortProperty
        {
            get => _sortProperty;
            set
            {
                if (_sortProperty == value) return;
                _sortProperty = value;
                OnPropertyChanged();
                RefreshData();
            }
        }

        public bool SortDescending
        {
            get => _sortDescending;
            set
            {
                if (_sortDescending == value) return;
                _sortDescending = value;
                OnPropertyChanged();
                RefreshData();
            }
        }

        public bool CanWrite => DataSource is IList list && !list.IsReadOnly && !list.IsFixedSize;

        /// <summary>
        /// DataSource 的項目型別 (Item Type)
        /// </summary>
        /// <remarks>如果是 IEnumerable<T>，則回傳 T 的型別；如果是非泛型 IEnumerable，則回傳第一個元素的型別；如果無法取得，則回傳 null</remarks>
        public Type? ItemType => _itemType;

        public object? CreateNewItem()
        {
            if (!CanWrite || _itemType is null)
            {
                return null;
            }

            return Activator.CreateInstance(_itemType);
        }

        public bool AddItem(object item)
        {
            if (!CanWrite || item is null || _itemType is null || !_itemType.IsInstanceOfType(item))
            {
                return false;
            }

            ((IList)DataSource!).Add(item);
            RefreshData();
            return true;
        }

        public bool RemoveItem(object item)
        {
            if (!CanWrite || item is null)
            {
                return false;
            }

            IList list = (IList)DataSource!;
            if (!list.Contains(item))
            {
                return false;
            }

            list.Remove(item);
            RefreshData();
            return true;
        }

        /// <summary>
        /// 設定欄位定義
        /// </summary>
        /// <param name="definitions"></param>
        /// <remarks>如果沒有傳入則根據 DataSource 的 ItemType 取得，如果 ItemType 是空的則欄位定義預設也會是空的</remarks>
        public void SetColumnDefinitions(IEnumerable<DynamicDataGridColumnDefinition>? definitions)
        {
            //檢查是否有手動設定欄位定義
            _hasManualColumnDefinitions = definitions is not null;

            //如果有手動設定則用手動設定的欄位定義，否則根據 DataSource 的 ItemType 取得欄位定義
            //如果 ItemType 是空的則欄位定義預設也會是空的
            ColumnDefinitions = definitions?.ToArray() ?? (
                _itemType is null ? Array.Empty<DynamicDataGridColumnDefinition>() : 
                    DynamicDataGridPropertyMetadata.Create(_itemType)
                        .Select(property => new DynamicDataGridColumnDefinition{
                                                Header = property.Header,
                                                BindingPath = property.Name,
                                                IsReadOnly = property.IsReadOnly
                                            }
                        ).ToArray()
            );

            RebuildFilters();
            OnPropertyChanged(nameof(ColumnDefinitions));
            RefreshData();
        }

        public void ClearFilters()
        {
            foreach (DynamicDataGridFilterViewModel filter in Filters)
            {
                filter.Value = string.Empty;
                filter.Operator = DynamicDataGridFilterOperator.Contains;
                filter.ClearCommand.Execute(null);
            }
        }

        /// <summary>
        /// 重新建立 Filters
        /// </summary>
        /// <remarks>根據新的 ColumnDefinitions 建立新的 Filter，故要先更新 ColumnDefinitions 再呼叫</remarks>
        private void RebuildFilters()
        {
            // 解除舊的 Filter 的事件訂閱
            foreach (DynamicDataGridFilterViewModel filter in Filters)
            {
                filter.PropertyChanged -= FilterChanged;
            }
            Filters.Clear();

            // 根據新的 ColumnDefinitions 建立新的 Filter
            foreach (DynamicDataGridColumnDefinition definition in ColumnDefinitions)
            {
                Type? propertyType = _itemType?.GetProperty(definition.BindingPath)?.PropertyType;
                var filter = new DynamicDataGridFilterViewModel(definition.BindingPath, definition.Header, propertyType);
                filter.PropertyChanged += FilterChanged;
                Filters.Add(filter);
            }
        }

        private void FiltersChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            RefreshData();
        }

        private void FilterChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(DynamicDataGridFilterViewModel.Filter))
            {
                RefreshData();
            }
        }

        public void RefreshData()
        {
            if (_dataSource is null || _itemType is null)
            {
                DisplayItems = _dataSource;
                return;
            }

            DisplayItems = DynamicDataGridQuery.Apply(
                _dataSource,
                _itemType,
                Filters.Select(filter => filter.Filter).Where(filter => filter is not null).Cast<DynamicDataGridFilter>(),
                SortProperty,
                SortDescending);
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
    }
}
