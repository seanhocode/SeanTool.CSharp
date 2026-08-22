using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SeanTool.CSharp.WPFTool.Enums.Filter;
using SeanTool.CSharp.WPFTool.Enums.VirtualTreeView;
using SeanTool.CSharp.WPFTool.Models.Filter;
using SeanTool.CSharp.WPFTool.Models.VirtualTreeView;
using SeanTool.CSharp.WPFTool.UserControls.VirtualTreeView;
using Xunit;

namespace SeanTool.CSharp.WPFTool.Test
{
    public class TreeFilterQueryUnitTest
    {
        [Fact]
        public void TreeOperators_ContainsOnlyTreeTextOperators()
        {
            Assert.Equal(
                [FilterOperator.Contains, FilterOperator.StartsWith, FilterOperator.Equals],
                FilterCondition.TreeOperators);
        }

        [Fact]
        public void Apply_KeepsMatchingNodeAndAncestors()
        {
            TreeNode root = CreateTree();

            IReadOnlyList<TreeNode> result = TreeFilterQuery.Apply(
                [root],
                node => node.Children,
                node => node.Name == "target.txt",
                (node, children) => new TreeNode(node.Name, children));

            TreeNode resultRoot = Assert.Single(result);
            TreeNode branch = Assert.Single(resultRoot.Children);

            Assert.Equal("root", resultRoot.Name);
            Assert.Equal("branch", branch.Name);
            Assert.Equal("target.txt", Assert.Single(branch.Children).Name);
            Assert.Equal("target-child.txt", Assert.Single(resultRoot.Children.Single().Children.Single().Children).Name);
        }

        [Fact]
        public void Apply_DoesNotReturnUnmatchedBranches()
        {
            TreeNode root = CreateTree();

            IReadOnlyList<TreeNode> result = TreeFilterQuery.Apply(
                [root],
                node => node.Children,
                node => node.Name == "missing",
                (node, children) => new TreeNode(node.Name, children));

            Assert.Empty(result);
        }

        [Fact]
        public void Apply_ViewNodeCheckStateWritesToSourceNode()
        {
            TreeNode root = CreateTree();
            var rootViewModel = new TreeNodeViewModel(root) { IsExpanded = true };

            Assert.Equal(CheckType.None, rootViewModel.CheckType);
            Assert.False(rootViewModel.Children[0].Children[0].Children[0].IsThreeState);
            Assert.True(rootViewModel.IsThreeState);

            TreeNodeViewModel resultRoot = Assert.Single(TreeFilterQuery.Apply(
                [rootViewModel],
                node => node.Children,
                _ => true,
                TreeNodeViewModel.CreateView));

            resultRoot.CheckType = CheckType.All;

            Assert.Equal(CheckType.All, rootViewModel.CheckType);
            Assert.All(rootViewModel.Children, child => Assert.Equal(CheckType.All, child.CheckType));
            resultRoot.Children[0].CheckType = CheckType.None;
            Assert.Equal(CheckType.HasValue, rootViewModel.CheckType);
            Assert.True(resultRoot.IsExpanded);
            Assert.NotSame(rootViewModel, resultRoot);
        }

        [Fact]
        public void VirtualTreeView_FiltersItemsAndKeepsCheckState()
        {
            RunOnStaThread(() =>
            {
                TreeNodeViewModel root = new TreeNodeViewModel(CreateTree());
                var control = new VirtualTreeView { ItemsSource = [root] };
                Assert.True(control.IsCheckVisible);
                control.IsCheckVisible = false;
                Assert.False(control.IsCheckVisible);
                root.Children[0].Children[1].CheckType = CheckType.All;
                control.FilterViewModel.Value = "target";
                control.FilterViewModel.ApplyCommand.Execute(null);

                Assert.Single(control.FilteredItems);
                TreeNodeViewModel filteredRoot = control.FilteredItems[0];
                Assert.Equal("target.txt", filteredRoot.Children[0].Children[0].Name);
                Assert.True(filteredRoot.IsExpanded);
                Assert.True(filteredRoot.Children[0].IsExpanded);
                Assert.False(filteredRoot.Children[0].Children[0].IsExpanded);
                Assert.Equal("target-child.txt", Assert.Single(filteredRoot.Children[0].Children[0].Children).Name);
                Assert.Empty(control.SelectedItems);
                Assert.Empty(control.CheckedValues);

                filteredRoot.VisibleCheckType = CheckType.All;
                Assert.Equal(CheckType.HasValue, root.CheckType);
                Assert.Equal(
                    ["root", "branch", "target.txt", "target-child.txt"],
                    control.CheckedValues);
                control.IsCheckVisible = true;
                Assert.Null(control.SelectedItem);
                Assert.Empty(control.SelectedItems);
            });
        }

        [Fact]
        public void VirtualTreeView_NoFilterUsesSourceNodesWithoutCloning()
        {
            RunOnStaThread(() =>
            {
                TreeNodeViewModel root = new TreeNodeViewModel(CreateTree());
                var source = new ObservableCollection<TreeNodeViewModel> { root };
                var control = new VirtualTreeView { ItemsSource = source };

                Assert.Same(root, control.FilteredItems[0]);
                Assert.Same(root.Children[0], control.FilteredItems[0].Children[0]);
            });
        }

        [Fact]
        public void VirtualTreeView_RemovedSubtreeStopsAffectingCheckedValues()
        {
            RunOnStaThread(() =>
            {
                TreeNodeViewModel root = new TreeNodeViewModel(CreateTree());
                var source = new ObservableCollection<TreeNodeViewModel> { root };
                var control = new VirtualTreeView { ItemsSource = source };

                TreeNodeViewModel branch = root.Children[0];
                TreeNodeViewModel removedTarget = branch.Children[0];
                branch.Children.Remove(removedTarget);

                // 節點被移除後應解除觀察，之後再改它的 CheckType 不應觸發 CheckedItems 重新計算。
                removedTarget.CheckType = CheckType.All;
                Assert.DoesNotContain(removedTarget, control.CheckedItems);

                TreeNodeViewModel added = new(new TreeNode("added.txt", value: "added.txt"), branch);
                branch.Children.Add(added);
                added.CheckType = CheckType.All;
                Assert.Contains(added, control.CheckedItems);
            });
        }

        [Fact]
        public void VirtualTreeView_RefreshesWhenSourceTreeChanges()
        {
            RunOnStaThread(() =>
            {
                TreeNodeViewModel root = new TreeNodeViewModel(CreateTree());
                var source = new ObservableCollection<TreeNodeViewModel> { root };
                var control = new VirtualTreeView { ItemsSource = source };

                root.Children.Add(new TreeNodeViewModel(new TreeNode("added.txt", value: "added.txt"), root));
                Assert.Contains(control.FilteredItems[0].Children, node => node.Name == "added.txt");

                source.Add(new TreeNodeViewModel(new TreeNode("top-level.txt", value: "top-level.txt")));

                Assert.Equal(2, control.FilteredItems.Count);
            });
        }

        [Fact]
        public void VirtualTreeView_DoesNotForceExpandOnUnrelatedSourceChange()
        {
            RunOnStaThread(() =>
            {
                TreeNodeViewModel root = new TreeNodeViewModel(CreateTree());
                var source = new ObservableCollection<TreeNodeViewModel> { root };
                var control = new VirtualTreeView { ItemsSource = source };

                TreeNodeViewModel branch = root.Children[0];
                branch.IsExpanded = false;

                // 在別的節點新增子項，不應該把使用者手動收合的 branch 打開。
                root.Children.Add(new TreeNodeViewModel(new TreeNode("added.txt", value: "added.txt"), root));

                Assert.False(branch.IsExpanded);
            });
        }

        [Fact]
        public void VirtualTreeView_UnrelatedSourceChangeDoesNotResetFilteredItems()
        {
            RunOnStaThread(() =>
            {
                TreeNodeViewModel root = new TreeNodeViewModel(CreateTree());
                var source = new ObservableCollection<TreeNodeViewModel> { root };
                var control = new VirtualTreeView { ItemsSource = source };

                var resetCount = 0;
                control.FilteredItems.CollectionChanged += (_, args) =>
                {
                    if (args.Action == NotifyCollectionChangedAction.Reset)
                    {
                        resetCount++;
                    }
                };

                root.Children[0].Children.Add(new TreeNodeViewModel(new TreeNode("added.txt", value: "added.txt"), root.Children[0]));

                Assert.Equal(0, resetCount);
                Assert.Same(root, control.FilteredItems[0]);
            });
        }

        [Fact]
        public void VirtualTreeView_RealCheckBoxClick_PropagatesToChildrenAndParent()
        {
            RunOnStaThread(() =>
            {
                if (Application.Current is null)
                {
                    new Application
                    {
                        Resources = { { "BooleanToVisibilityConverter", new System.Windows.Controls.BooleanToVisibilityConverter() } }
                    };
                }

                TreeNodeViewModel root = new TreeNodeViewModel(CreateTree()) { IsExpanded = true };
                TreeNodeViewModel branch = root.Children[0];
                branch.IsExpanded = true;
                TreeNodeViewModel target = branch.Children[0];
                target.IsExpanded = true;

                var control = new VirtualTreeView { ItemsSource = [root] };
                control.Measure(new Size(400, 1000));
                control.Arrange(new Rect(0, 0, 400, 1000));
                control.UpdateLayout();

                TreeView treeView = FindVisualChild<TreeView>(control)!;
                treeView.UpdateLayout();

                TreeViewItem branchContainer = FindTreeViewItem(treeView, branch)!;
                Assert.NotNull(branchContainer);
                branchContainer.UpdateLayout();
                CheckBox branchCheckBox = FindVisualChild<CheckBox>(branchContainer)!;
                Assert.NotNull(branchCheckBox);

                // 模擬使用者實際點擊 branch 的 CheckBox 全選。
                branchCheckBox.IsChecked = true;

                Assert.Equal(CheckType.All, target.CheckType);
                Assert.Equal(CheckType.HasValue, root.CheckType);

                treeView.UpdateLayout();
                TreeViewItem targetContainer = FindTreeViewItem(treeView, target)!;
                Assert.NotNull(targetContainer);
                CheckBox targetCheckBox = FindVisualChild<CheckBox>(targetContainer)!;
                Assert.NotNull(targetCheckBox);
                Assert.True(targetCheckBox.IsChecked);

                // 反向：取消子項的 CheckBox，父項應變回半選。
                targetCheckBox.IsChecked = false;
                Assert.Equal(CheckType.HasValue, branch.CheckType);
            });
        }

        [Fact]
        public void Apply_ThrowsOnNullArguments()
        {
            TreeNode root = CreateTree();
            Func<TreeNode, IEnumerable<TreeNode>> children = node => node.Children;
            Func<TreeNode, bool> predicate = _ => true;
            Func<TreeNode, IEnumerable<TreeNode>, TreeNode> create = (node, c) => new TreeNode(node.Name, c);

            Assert.Throws<ArgumentNullException>(() => TreeFilterQuery.Apply(null!, children, predicate, create));
            Assert.Throws<ArgumentNullException>(() => TreeFilterQuery.Apply([root], null!, predicate, create));
            Assert.Throws<ArgumentNullException>(() => TreeFilterQuery.Apply([root], children, null!, create));
            Assert.Throws<ArgumentNullException>(() => TreeFilterQuery.Apply([root], children, predicate, null!));
        }

        [Fact]
        public void Apply_EmptySource_ReturnsEmpty()
        {
            IReadOnlyList<TreeNode> result = TreeFilterQuery.Apply(
                Array.Empty<TreeNode>(),
                node => node.Children,
                _ => true,
                (node, children) => new TreeNode(node.Name, children));

            Assert.Empty(result);
        }

        [Fact]
        public void Apply_MatchOnIntermediateNode_KeepsNonMatchingSiblingsUnderneath()
        {
            TreeNode root = CreateTree();

            // "branch" 本身符合，預期整個子樹（含不符合的 other.txt）都要原封帶回。
            IReadOnlyList<TreeNode> result = TreeFilterQuery.Apply(
                [root],
                node => node.Children,
                node => node.Name == "branch",
                (node, children) => new TreeNode(node.Name, children));

            TreeNode resultRoot = Assert.Single(result);
            TreeNode branch = Assert.Single(resultRoot.Children);
            Assert.Equal(["target.txt", "other.txt"], branch.Children.Select(c => c.Name));
            Assert.Equal("target-child.txt", Assert.Single(branch.Children[0].Children).Name);
        }

        [Fact]
        public void CheckType_UncheckingAllChildren_ParentBecomesNone()
        {
            RunOnStaThread(() =>
            {
                TreeNodeViewModel root = new TreeNodeViewModel(CreateTree());
                TreeNodeViewModel branch = root.Children[0];
                TreeNodeViewModel target = branch.Children[0];
                TreeNodeViewModel other = branch.Children[1];

                branch.CheckType = CheckType.All;
                Assert.Equal(CheckType.All, branch.CheckType);

                target.CheckType = CheckType.None;
                Assert.Equal(CheckType.HasValue, branch.CheckType);

                other.CheckType = CheckType.None;
                Assert.Equal(CheckType.None, branch.CheckType);
                Assert.Equal(CheckType.None, root.CheckType);
            });
        }

        [Fact]
        public void IsThreeState_UpdatesWhenChildAddedOrRemoved()
        {
            RunOnStaThread(() =>
            {
                TreeNodeViewModel leaf = new TreeNodeViewModel(new TreeNode("leaf"));
                Assert.False(leaf.IsThreeState);

                var changedProperties = new List<string?>();
                leaf.PropertyChanged += (_, args) => changedProperties.Add(args.PropertyName);

                TreeNodeViewModel child = leaf.AddChild("child");
                Assert.True(leaf.IsThreeState);
                Assert.Contains(nameof(TreeNodeViewModel.IsThreeState), changedProperties);

                changedProperties.Clear();
                child.RemoveFromParent();
                Assert.False(leaf.IsThreeState);
                Assert.Contains(nameof(TreeNodeViewModel.IsThreeState), changedProperties);
            });
        }

        [Fact]
        public void VirtualTreeView_NotifiesWhenSelectionChanges()
        {
            RunOnStaThread(() =>
            {
                var source = new ObservableCollection<TreeNodeViewModel>
                {
                    new(new TreeNode("root"))
                };
                var control = new VirtualTreeView { ItemsSource = source };
                var changedProperties = new List<string?>();
                control.PropertyChanged += (_, args) => changedProperties.Add(args.PropertyName);

                control.SelectedItem = source[0];

                Assert.Contains(nameof(VirtualTreeView.SelectedItem), changedProperties);
                Assert.Contains(nameof(VirtualTreeView.SelectedItems), changedProperties);
            });
        }

        [Fact]
        public void FilterViewModel_ClearCommandRemovesAppliedFilter()
        {
            var filter = new FilterViewModel(nameof(TreeNode.Name), "TreeView 搜尋", FilterValueType.TreeNode)
            {
                Value = "target"
            };

            filter.ApplyCommand.Execute(null);
            Assert.NotNull(filter.AppliedFilter);

            filter.ClearCommand.Execute(null);

            Assert.Null(filter.AppliedFilter);
            Assert.Empty(filter.Value);
        }

        private static TreeNode CreateTree()
        {
            return new TreeNode("root", [
                new TreeNode("branch", [
                    new TreeNode("target.txt", [new TreeNode("target-child.txt", value: "target-child.txt")], "target.txt"),
                    new TreeNode("other.txt", value: "other.txt")
                ], "branch"),
                new TreeNode("other-branch", value: "other-branch")
            ], "root");
        }

        private static T? FindVisualChild<T>(DependencyObject parent)
            where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);
                if (child is T typed)
                {
                    return typed;
                }

                T? found = FindVisualChild<T>(child);
                if (found is not null)
                {
                    return found;
                }
            }

            return null;
        }

        private static TreeViewItem? FindTreeViewItem(ItemsControl container, object item)
        {
            container.UpdateLayout();
            if (container.ItemContainerGenerator.ContainerFromItem(item) is TreeViewItem direct)
            {
                return direct;
            }

            foreach (object child in container.Items)
            {
                if (container.ItemContainerGenerator.ContainerFromItem(child) is TreeViewItem childContainer)
                {
                    TreeViewItem? found = FindTreeViewItem(childContainer, item);
                    if (found is not null)
                    {
                        return found;
                    }
                }
            }

            return null;
        }

        private static void RunOnStaThread(Action action)
        {
            Exception? exception = null;
            using var completed = new ManualResetEventSlim();
            var thread = new Thread(() =>
            {
                try
                {
                    action();
                }
                catch (Exception caught)
                {
                    exception = caught;
                }
                finally
                {
                    completed.Set();
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            completed.Wait();
            thread.Join();
            if (exception is not null)
            {
                throw new Xunit.Sdk.XunitException(exception.ToString());
            }
        }
    }
}
