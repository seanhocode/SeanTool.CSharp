using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Xunit;

namespace SeanTool.CSharp.WPFTool.Test
{
    /// <summary>
    /// TreeFilterQuery / VirtualTreeView 單元測試集合
    /// 驗證樹狀資料的遞迴篩選引擎(TreeFilterQuery)、VirtualTreeView 的搜尋/三態勾選/展開狀態同步，
    /// 以及 TreeNodeViewModel 的 View/Source 節點分離架構等行為
    /// </summary>
    public class VirtualTreeUnitTest
    {   
        /// <summary>
        /// 測試：FilterCondition.TreeOperators - TreeView 搜尋僅開放 Contains/StartsWith/Equals 三種文字運算子
        /// </summary>
        [Fact(DisplayName = "TreeOperators：僅包含 Contains/StartsWith/Equals")]
        public void TreeOperators_ContainsOnlyTreeTextOperators()
        {
            Assert.Equal(
                [FilterOperator.Contains, FilterOperator.StartsWith, FilterOperator.Equals],
                FilterCondition.TreeOperators);
        }

        /// <summary>
        /// 測試：Apply - 符合條件的節點與其祖先節點都會保留在篩選結果中
        /// </summary>
        [Fact(DisplayName = "Apply：保留符合條件的節點與其祖先")]
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

        /// <summary>
        /// 測試：Apply - 完全沒有符合條件的節點時，回傳空結果(不會有空的殘留分支)
        /// </summary>
        [Fact(DisplayName = "Apply：無符合節點時回傳空結果")]
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

        /// <summary>
        /// 測試：View/Source 節點分離架構 - 對篩選產生的 View 節點操作 CheckType，最終會轉發寫回真正的 SourceNode
        /// </summary>
        [Fact(DisplayName = "View 節點勾選狀態轉發寫回 SourceNode")]
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

        /// <summary>
        /// 測試：VirtualTreeView 搜尋 - 套用篩選後只顯示符合的節點路徑，並保留三態勾選狀態(以可見的 VisibleCheckType 判斷)
        /// </summary>
        [Fact(DisplayName = "VirtualTreeView：套用篩選後只顯示符合節點且保留勾選狀態")]
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
                Assert.Empty(control.CheckedValues);

                filteredRoot.VisibleCheckType = CheckType.All;
                Assert.Equal(CheckType.HasValue, root.CheckType);
                Assert.Equal(
                    ["root", "branch", "target.txt", "target-child.txt"],
                    control.CheckedValues);
                control.IsCheckVisible = true;
                Assert.Null(control.SelectedItem);
            });
        }

        /// <summary>
        /// 測試：未套用篩選時，FilteredItems 直接沿用來源節點(含子孫)，不整棵複製，維持節點參考一致
        /// </summary>
        [Fact(DisplayName = "VirtualTreeView：未篩選時直接沿用來源節點，不整棵複製")]
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

        /// <summary>
        /// 測試：從樹上移除的子樹會解除事件觀察，之後異動其 CheckType 不再影響 CheckedItems；新增的子節點則會被正確觀察
        /// </summary>
        [Fact(DisplayName = "VirtualTreeView：移除的子樹不再影響 CheckedValues，新增節點正確被觀察")]
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

        /// <summary>
        /// 測試：來源樹狀結構異動(新增子節點/新增頂層節點) - FilteredItems 自動同步反映最新結構
        /// </summary>
        [Fact(DisplayName = "VirtualTreeView：來源樹結構異動時 FilteredItems 自動刷新")]
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

        /// <summary>
        /// 測試：沒有套用篩選時，其他節點的新增動作不應強制展開使用者手動收合的節點
        /// </summary>
        [Fact(DisplayName = "VirtualTreeView：無關節點異動不強制展開已手動收合的節點")]
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

        /// <summary>
        /// 測試：深層節點的無關異動不會讓 FilteredItems 整棵 Reset(避免 TreeView 畫面閃爍、捲動位置歸零)
        /// </summary>
        [Fact(DisplayName = "VirtualTreeView：深層無關異動不會讓 FilteredItems 整棵 Reset")]
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

        /// <summary>
        /// 測試：實際模擬使用者點擊 CheckBox(透過真正的視覺樹/TreeViewItem) - 父子節點三態勾選正確連動
        /// </summary>
        [Fact(DisplayName = "VirtualTreeView：實際點擊 CheckBox 時父子三態正確連動")]
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

        /// <summary>
        /// 測試：Apply - source/childrenSelector/predicate/createNode 任一參數為 null 皆拋出 ArgumentNullException
        /// </summary>
        [Fact(DisplayName = "Apply：任一必要參數為 null 時拋出 ArgumentNullException")]
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

        /// <summary>
        /// 測試：Apply - 空的來源清單不崩潰，回傳空結果
        /// </summary>
        [Fact(DisplayName = "Apply：空來源清單回傳空結果")]
        public void Apply_EmptySource_ReturnsEmpty()
        {
            IReadOnlyList<TreeNode> result = TreeFilterQuery.Apply(
                Array.Empty<TreeNode>(),
                node => node.Children,
                _ => true,
                (node, children) => new TreeNode(node.Name, children));

            Assert.Empty(result);
        }

        /// <summary>
        /// 測試：中間節點本身即符合條件時 - 該節點下不符合的手足節點也會原封不動一併帶回(整個子樹保留)
        /// </summary>
        [Fact(DisplayName = "Apply：中間節點符合條件時整個子樹原封保留")]
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

        /// <summary>
        /// 測試：CheckType - 逐一取消所有子節點的勾選後，父節點狀態依序變化為 HasValue(半選) 再回到 None
        /// </summary>
        [Fact(DisplayName = "CheckType：取消所有子節點勾選後父節點變回 None")]
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

        /// <summary>
        /// 測試：IsThreeState - 動態新增/移除子節點(AddChild/RemoveFromParent)時即時更新，並觸發屬性變更通知
        /// </summary>
        [Fact(DisplayName = "IsThreeState：新增/移除子節點時即時更新並通知")]
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

        /// <summary>
        /// 測試：VirtualTreeView.SelectedItem 變更時觸發 PropertyChanged 通知(供 UI 綁定使用)
        /// </summary>
        [Fact(DisplayName = "VirtualTreeView：SelectedItem 變更時觸發 PropertyChanged")]
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
            });
        }

        /// <summary>
        /// 測試：FilterViewModel.ClearCommand - 清除後 AppliedFilter 變為 null，暫存輸入值也一併清空
        /// </summary>
        [Fact(DisplayName = "FilterViewModel：ClearCommand 清除已套用篩選與輸入值")]
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
