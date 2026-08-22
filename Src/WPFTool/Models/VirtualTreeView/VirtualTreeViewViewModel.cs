using System.Collections.ObjectModel;
using System.Collections.Specialized;
using SeanTool.CSharp.WPFTool.Enums.Filter;
using SeanTool.CSharp.WPFTool.Enums.VirtualTreeView;
using SeanTool.CSharp.WPFTool.Models.Filter;

namespace SeanTool.CSharp.WPFTool.Models.VirtualTreeView
{
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

        public ObservableCollection<TreeNodeViewModel> FilteredItems { get; } = [];

        public ObservableCollection<TreeNodeViewModel> CheckedItems { get; } = [];

        public FilterViewModel FilterViewModel { get; }

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
                OnPropertyChanged(nameof(SelectedItems));
            }
        }

        public IReadOnlyList<TreeNodeViewModel> SelectedItems =>
            SelectedItem is null ? Array.Empty<TreeNodeViewModel>() : [SelectedItem];

        public IReadOnlyList<object?> CheckedValues =>
            EnumerateNodes(FilterViewModel.AppliedFilter is null ? ItemsSource : FilteredItems)
                .Where(node => (FilterViewModel.AppliedFilter is null ? node.CheckType : node.VisibleCheckType) == CheckType.All)
                .Select(node => node.Value)
                .ToArray();

        public void ExpandAll()
        {
            foreach (TreeNodeViewModel node in FilteredItems)
            {
                SetExpanded(node, true);
            }
        }

        public void CollapseAll()
        {
            foreach (TreeNodeViewModel node in FilteredItems)
            {
                SetExpanded(node, false);
            }
        }

        private void FilterViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(FilterViewModel.AppliedFilter))
            {
                RefreshItems();
            }
        }

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

        private void ObserveSource(IEnumerable<TreeNodeViewModel>? nodes)
        {
            if (nodes is INotifyCollectionChanged collection)
            {
                collection.CollectionChanged += TreeNodeCollectionChanged;
                _observedItemsSource = collection;
            }

            ObserveNodes(nodes);
        }

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

        private void TreeNodePropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(TreeNodeViewModel.CheckType))
            {
                RefreshCheckedItems();
            }
        }

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

        private void RefreshCheckedItems()
        {
            CheckedItems.Clear();
            foreach (TreeNodeViewModel node in EnumerateNodes(ItemsSource).Where(node => node.CheckType == CheckType.All))
            {
                CheckedItems.Add(node);
            }

            OnPropertyChanged(nameof(CheckedValues));
        }

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

        private static void SetExpanded(TreeNodeViewModel node, bool isExpanded)
        {
            node.IsExpanded = isExpanded;
            foreach (TreeNodeViewModel child in node.Children)
            {
                SetExpanded(child, isExpanded);
            }
        }

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
