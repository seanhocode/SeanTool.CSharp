using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace SeanTool.CSharp.WPFTool
{
    /// <summary>
    /// VirtualTreeView 的 ViewModel
    /// </summary>
    public class VirtualTreeViewViewModel : ViewModelBase
    {
        private readonly HashSet<TreeNodeViewModel> _observedNodes = [];
        private INotifyCollectionChanged? _observedItemsSource;
        private IEnumerable<TreeNodeViewModel>? _itemsSource;
        private TreeNodeViewModel? _selectedItem;

        public VirtualTreeViewViewModel()
        {
            FilterViewModel = new FilterViewModel(
                nameof(TreeNodeViewModel.Name),
                "搜尋",
                FilterValueType.TreeNode);
            FilterViewModel.PropertyChanged += FilterViewModelPropertyChanged;
        }

        /// <summary>
        /// ItemsSource 經過篩選後的顯示節點清單 (僅頂層節點，子節點透過 Children 存取)
        /// </summary>
        public ObservableCollection<TreeNodeViewModel> FilteredItems { get; } = [];

        /// <summary>
        /// 目前已勾選的節點清單
        /// </summary>
        public ObservableCollection<TreeNodeViewModel> CheckedItems { get; } = [];

        /// <summary>
        /// 搜尋用的篩選條件
        /// </summary>
        public FilterViewModel FilterViewModel { get; }

        /// <summary>
        /// 樹狀資料來源
        /// </summary>
        public IEnumerable<TreeNodeViewModel>? ItemsSource
        {
            get => _itemsSource;
            set
            {
                if (ReferenceEquals(_itemsSource, value))
                {
                    return;
                }

                StopObservingNodes();
                _itemsSource = value;
                ObserveSource(_itemsSource);
                RefreshItems();
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 目前選取節點
        /// </summary>
        public TreeNodeViewModel? SelectedItem
        {
            get => _selectedItem;
            set
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
        /// 目前所有已勾選節點的 Value 清單
        /// </summary>
        /// <remarks>沒有套用篩選時以 CheckType 判斷、有套用篩選時以 VisibleCheckType(僅計算目前可見的子節點) 判斷</remarks>
        public IReadOnlyList<object?> CheckedValues =>
            EnumerateNodes(FilterViewModel.AppliedFilter is null ? ItemsSource : FilteredItems)
                .Where(node => (FilterViewModel.AppliedFilter is null ? node.CheckType : node.VisibleCheckType) == CheckType.All)
                .Select(node => node.Value)
                .ToArray();

        /// <summary>
        /// 展開所有節點
        /// </summary>
        public void ExpandAll()
        {
            foreach (TreeNodeViewModel node in FilteredItems)
            {
                SetExpanded(node, true);
            }
        }

        /// <summary>
        /// 收合所有節點
        /// </summary>
        public void CollapseAll()
        {
            foreach (TreeNodeViewModel node in FilteredItems)
            {
                SetExpanded(node, false);
            }
        }

        /// <summary>
        /// 篩選條件異動時觸發
        /// </summary>
        /// <remarks>更新 FilteredItems</remarks>
        private void FilterViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(FilterViewModel.AppliedFilter))
            {
                RefreshItems();
            }
        }

        /// <summary>
        /// 訂閱指定節點(含子孫)的事件
        /// </summary>
        private void ObserveNodes(IEnumerable<TreeNodeViewModel>? nodes)
        {
            foreach (TreeNodeViewModel node in EnumerateNodes(nodes))
            {
                if (_observedNodes.Add(node))
                {
                    node.PropertyChanged += TreeNodePropertyChanged;
                    node.Children.CollectionChanged += TreeNodeCollectionChanged;
                }
            }
        }

        /// <summary>
        /// 訂閱 ItemsSource 本身與其節點(含子孫)的事件
        /// </summary>
        private void ObserveSource(IEnumerable<TreeNodeViewModel>? nodes)
        {
            if (nodes is INotifyCollectionChanged collection)
            {
                collection.CollectionChanged += TreeNodeCollectionChanged;
                _observedItemsSource = collection;
            }

            ObserveNodes(nodes);
        }

        /// <summary>
        /// 解除目前所有已訂閱的節點與 ItemsSource 事件
        /// </summary>
        private void StopObservingNodes()
        {
            if (_observedItemsSource is not null)
            {
                _observedItemsSource.CollectionChanged -= TreeNodeCollectionChanged;
                _observedItemsSource = null;
            }

            foreach (TreeNodeViewModel node in _observedNodes)
            {
                node.PropertyChanged -= TreeNodePropertyChanged;
                node.Children.CollectionChanged -= TreeNodeCollectionChanged;
            }

            _observedNodes.Clear();
        }

        /// <summary>
        /// 解除指定節點(含子孫)的事件訂閱
        /// </summary>
        private void UnobserveNodes(IEnumerable<TreeNodeViewModel>? nodes)
        {
            foreach (TreeNodeViewModel node in EnumerateNodes(nodes))
            {
                if (_observedNodes.Remove(node))
                {
                    node.PropertyChanged -= TreeNodePropertyChanged;
                    node.Children.CollectionChanged -= TreeNodeCollectionChanged;
                }
            }
        }

        /// <summary>
        /// 節點屬性異動時觸發
        /// </summary>
        /// <remarks>CheckType 異動時更新 CheckedItems</remarks>
        private void TreeNodePropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(TreeNodeViewModel.CheckType))
            {
                RefreshCheckedItems();
            }
        }

        /// <summary>
        /// 節點集合(ItemsSource 本身或任一節點的 Children)異動時觸發
        /// </summary>
        /// <remarks>更新事件訂閱後重新整理 FilteredItems</remarks>
        private void TreeNodeCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            // ponytail: 只針對真正異動的節點(含其子孫)增量掛勾/解除觀察者，
            // 避免任何一次新增/移除都重新掃描整棵樹。Reset(例如 Clear())才整棵重建。
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    ObserveNodes(e.NewItems!.Cast<TreeNodeViewModel>());
                    break;
                case NotifyCollectionChangedAction.Remove:
                    UnobserveNodes(e.OldItems!.Cast<TreeNodeViewModel>());
                    break;
                case NotifyCollectionChangedAction.Replace:
                    UnobserveNodes(e.OldItems!.Cast<TreeNodeViewModel>());
                    ObserveNodes(e.NewItems!.Cast<TreeNodeViewModel>());
                    break;
                case NotifyCollectionChangedAction.Move:
                    break;
                default:
                    StopObservingNodes();
                    ObserveSource(ItemsSource);
                    break;
            }

            RefreshItems();
        }

        /// <summary>
        /// 更新 CheckedItems 與 CheckedValues
        /// </summary>
        private void RefreshCheckedItems()
        {
            CheckedItems.Clear();
            foreach (TreeNodeViewModel node in EnumerateNodes(ItemsSource).Where(node => node.CheckType == CheckType.All))
            {
                CheckedItems.Add(node);
            }

            OnPropertyChanged(nameof(CheckedValues));
        }

        /// <summary>
        /// 更新 FilteredItems
        /// </summary>
        /// <remarks>套用篩選條件並展開符合的節點，同步 SelectedItem 與 CheckedItems</remarks>
        private void RefreshItems()
        {
            TreeNodeViewModel? selectedSource = SelectedItem?.SourceNode;
            if (ItemsSource is null)
            {
                FilteredItems.Clear();
                SelectedItem = null;
                RefreshCheckedItems();
                return;
            }

            FilterCondition? filter = FilterViewModel.AppliedFilter;

            // ponytail: 沒有套用過濾時直接沿用原始節點，不整棵 clone，
            // 避免每次來源異動都重建整棵樹（節點數大時的主要效能瓶頸）。
            IReadOnlyList<TreeNodeViewModel> result = filter is null
                ? ItemsSource.ToArray()
                : TreeFilterQuery.Apply(
                    ItemsSource,
                    node => node.Children,
                    node => FilterQuery.Apply(new[] { node }, typeof(TreeNodeViewModel), new[] { filter }).Cast<TreeNodeViewModel>().Any(),
                    TreeNodeViewModel.CreateView);

            foreach (TreeNodeViewModel node in result)
            {
                ExpandToMatches(node, filter);
            }

            // ponytail: 只在頂層集合真的變動時才 Clear+重建，
            // 否則深層節點的新增/移除會讓整個 TreeView 收到 Reset，畫面閃爍、捲動位置歸零。
            if (!FilteredItems.SequenceEqual(result))
            {
                FilteredItems.Clear();
                foreach (TreeNodeViewModel node in result)
                {
                    FilteredItems.Add(node);
                }
            }

            SelectedItem = selectedSource is null ? null : FindNode(FilteredItems, selectedSource);
            RefreshCheckedItems();
        }

        /// <summary>
        /// 展開節點至符合篩選條件的子孫，並回傳此節點(含子孫)是否有符合條件的項目
        /// </summary>
        private static bool ExpandToMatches(TreeNodeViewModel node, FilterCondition? filter)
        {
            if (filter is null)
            {
                // ponytail: 沒有篩選時不要動使用者自己展開/收合的狀態，
                // 只有套用篩選才需要自動展開到符合的節點。
                return true;
            }

            bool matches = FilterQuery.Apply(new[] { node }, typeof(TreeNodeViewModel), new[] { filter })
                .Cast<TreeNodeViewModel>()
                .Any();
            if (matches)
            {
                // ponytail: 節點本身已符合條件時，子孫是靠 TreeFilterQuery 的 CloneChildren 整包帶入，
                // IsExpanded/回傳值都與子節點是否符合無關，提前短路可省去對整個已符合子樹的遞迴比對。
                node.IsExpanded = false;
                return true;
            }

            bool hasMatchingChild = node.Children.Any(child => ExpandToMatches(child, filter));
            node.IsExpanded = hasMatchingChild;
            return hasMatchingChild;
        }

        /// <summary>
        /// 設定節點與其所有子孫的展開狀態
        /// </summary>
        private static void SetExpanded(TreeNodeViewModel node, bool isExpanded)
        {
            node.IsExpanded = isExpanded;
            foreach (TreeNodeViewModel child in node.Children)
            {
                SetExpanded(child, isExpanded);
            }
        }

        /// <summary>
        /// 在指定節點清單(含子孫)中找出對應原始節點(SourceNode)的節點
        /// </summary>
        private static TreeNodeViewModel? FindNode(IEnumerable<TreeNodeViewModel> nodes, TreeNodeViewModel sourceNode)
        {
            foreach (TreeNodeViewModel node in nodes)
            {
                if (node.SourceNode == sourceNode)
                {
                    return node;
                }

                TreeNodeViewModel? match = FindNode(node.Children, sourceNode);
                if (match is not null)
                {
                    return match;
                }
            }

            return null;
        }

        /// <summary>
        /// 攤平列舉節點清單中的所有節點(含子孫)
        /// </summary>
        private static IEnumerable<TreeNodeViewModel> EnumerateNodes(IEnumerable<TreeNodeViewModel>? nodes)
        {
            if (nodes is null)
            {
                yield break;
            }

            foreach (TreeNodeViewModel node in nodes)
            {
                yield return node;
                foreach (TreeNodeViewModel child in EnumerateNodes(node.Children))
                {
                    yield return child;
                }
            }
        }
    }
}
