using System.Collections.ObjectModel;
using System.Collections.Specialized;
using SeanTool.CSharp.WPFTool.Enums.VirtualTreeView;

namespace SeanTool.CSharp.WPFTool.Models.VirtualTreeView
{
    public class TreeNodeViewModel : ViewModelBase
    {
        private readonly TreeNode _sourceNode;
        private readonly TreeNodeViewModel? _sourceViewModel;
        private TreeNodeViewModel? _parent;
        private CheckType _checkType = CheckType.None;
        private bool _isExpanded;

        public TreeNodeViewModel(TreeNode sourceNode, TreeNodeViewModel? parent = null)
        {
            _sourceNode = sourceNode ?? throw new ArgumentNullException(nameof(sourceNode));
            _parent = parent;
            foreach (TreeNode child in sourceNode.Children)
            {
                Children.Add(new TreeNodeViewModel(child, this));
            }

            Children.CollectionChanged += OnChildrenChanged;
        }

        private TreeNodeViewModel(TreeNodeViewModel sourceNode, IEnumerable<TreeNodeViewModel> children)
        {
            _sourceNode = sourceNode._sourceNode;
            _sourceViewModel = sourceNode;
            foreach (TreeNodeViewModel child in children)
            {
                child._parent = this;
                Children.Add(child);
            }

            Children.CollectionChanged += OnChildrenChanged;
        }

        public string Name => _sourceNode.Name;

        public object? Value => _sourceNode.Value;

        public ObservableCollection<TreeNodeViewModel> Children { get; } = [];

        internal TreeNodeViewModel SourceNode => _sourceViewModel ?? this;

        public bool IsThreeState => Children.Count > 0;

        // ponytail: IsThreeState 沒有變更通知，AddChild/RemoveFromParent（或直接操作 Children）
        // 讓節點在葉節點/父節點之間切換時，已產生的 CheckBox 容器不會更新三態顯示。
        private void OnChildrenChanged(object? sender, NotifyCollectionChangedEventArgs e)
            => OnPropertyChanged(nameof(IsThreeState));

        public CheckType CheckType
        {
            get => _sourceViewModel?.CheckType ?? _checkType;
            set
            {
                if (_sourceViewModel is not null)
                {
                    _sourceViewModel.SetChecked(value, true, true);
                    NotifyCheckedTree();
                    NotifyCheckedAncestors();
                }
                else
                {
                    SetChecked(value, true, true);
                }
            }
        }

        public CheckType VisibleCheckType
        {
            get
            {
                if (_sourceViewModel is null || Children.Count == 0)
                {
                    return CheckType;
                }

                return Children.All(child => child.VisibleCheckType == CheckType.All)
                    ? CheckType.All
                    : Children.All(child => child.VisibleCheckType == CheckType.None)
                        ? CheckType.None
                        : CheckType.HasValue;
            }
            set
            {
                if (_sourceViewModel is null)
                {
                    CheckType = value;
                    return;
                }

                SetVisibleChecked(value);
                NotifyVisibleCheckedTree();
                NotifyVisibleCheckedAncestors();
            }
        }

        private void SetChecked(CheckType value, bool updateChildren, bool updateParent)
        {
            if (_checkType != value)
            {
                _checkType = value;
                OnPropertyChanged(nameof(CheckType));

                // ponytail: CheckBox 實際綁定的是 VisibleCheckType，只通知 CheckType
                // 會讓已經產生的子/父節點 CheckBox 容器讀不到新值（資料其實是對的，UI 沒刷新）。
                OnPropertyChanged(nameof(VisibleCheckType));
            }

            if (updateChildren)
            {
                foreach (TreeNodeViewModel child in Children)
                {
                    child.SetChecked(value == CheckType.All ? CheckType.All : CheckType.None, true, false);
                }
            }

            if (updateParent)
            {
                _parent?.UpdateCheckedFromChildren();
            }
        }

        private void SetVisibleChecked(CheckType value)
        {
            if (Children.Count == 0)
            {
                CheckType = value;
                return;
            }

            foreach (TreeNodeViewModel child in Children)
            {
                child.SetVisibleChecked(value);
            }
        }

        private void UpdateCheckedFromChildren()
        {
            CheckType value = Children.Count == 0
                ? CheckType.None
                : Children.All(child => child.CheckType == CheckType.All)
                    ? CheckType.All
                    : Children.All(child => child.CheckType == CheckType.None)
                    ? CheckType.None
                    : CheckType.HasValue;
            SetChecked(value, false, true);
        }

        private void NotifyCheckedTree()
        {
            OnPropertyChanged(nameof(CheckType));
            OnPropertyChanged(nameof(VisibleCheckType));
            foreach (TreeNodeViewModel child in Children)
            {
                child.NotifyCheckedTree();
            }
        }

        private void NotifyCheckedAncestors()
        {
            _parent?.OnPropertyChanged(nameof(CheckType));
            _parent?.OnPropertyChanged(nameof(VisibleCheckType));
            _parent?.NotifyCheckedAncestors();
        }

        private void NotifyVisibleCheckedTree()
        {
            OnPropertyChanged(nameof(VisibleCheckType));
            foreach (TreeNodeViewModel child in Children)
            {
                child.NotifyVisibleCheckedTree();
            }
        }

        private void NotifyVisibleCheckedAncestors()
        {
            _parent?.OnPropertyChanged(nameof(VisibleCheckType));
            _parent?.NotifyVisibleCheckedAncestors();
        }

        public bool IsExpanded
        {
            get => _sourceViewModel?.IsExpanded ?? _isExpanded;
            set
            {
                if (_sourceViewModel is not null)
                {
                    _sourceViewModel.IsExpanded = value;
                    OnPropertyChanged();
                }
                else
                {
                    if (_isExpanded == value)
                    {
                        return;
                    }

                    _isExpanded = value;
                    OnPropertyChanged();
                }
            }
        }

        public TreeNodeViewModel AddChild(string name, object? value = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            TreeNodeViewModel sourceNode = SourceNode;
            TreeNodeViewModel child = new(new TreeNode(name, value: value), sourceNode);
            sourceNode.Children.Add(child);
            sourceNode.UpdateCheckedFromChildren();
            return child;
        }

        public bool RemoveFromParent()
        {
            TreeNodeViewModel sourceNode = SourceNode;
            TreeNodeViewModel? parent = sourceNode._parent;
            bool removed = parent?.Children.Remove(sourceNode) == true;
            if (removed)
            {
                parent!.UpdateCheckedFromChildren();
            }

            return removed;
        }

        public static TreeNodeViewModel CreateView(TreeNodeViewModel sourceNode, IEnumerable<TreeNodeViewModel> children)
        {
            return new TreeNodeViewModel(sourceNode, children);
        }
    }
}
