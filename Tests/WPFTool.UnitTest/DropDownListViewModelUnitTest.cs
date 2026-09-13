using System.Collections.ObjectModel;
using System.Windows.Controls;
using Xunit;

namespace SeanTool.CSharp.WPFTool.Test
{
    /// <summary>
    /// DropDownList 核心邏輯單元測試
    /// 驗證搜尋(共用 FilterCondition/FilterQuery)、單選/多選、選取狀態同步等行為
    /// </summary>
    public class DropDownListViewModelUnitTest
    {
        private class Person
        {
            public string Name { get; set; } = string.Empty;
            public int Age { get; set; }
        }

        /// <summary>
        /// 測試：無 DisplayMemberPath 時，DisplayText 直接使用項目的 ToString()
        /// </summary>
        [Fact(DisplayName = "ItemsSource：無 DisplayMemberPath 時使用 ToString()")]
        public void ItemsSource_WithoutDisplayMemberPath_UsesToString()
        {
            var viewModel = new DropDownListViewModel { ItemsSource = new[] { "Alice", "Bob" } };

            Assert.Equal(new[] { "Alice", "Bob" }, viewModel.Items.Select(i => i.DisplayText));
        }

        /// <summary>
        /// 測試：設定 DisplayMemberPath 時，DisplayText 透過反射讀取指定屬性
        /// </summary>
        [Fact(DisplayName = "ItemsSource：設定 DisplayMemberPath 時以反射讀取屬性")]
        public void ItemsSource_WithDisplayMemberPath_ReadsProperty()
        {
            var viewModel = new DropDownListViewModel
            {
                DisplayMemberPath = nameof(Person.Name),
                ItemsSource = new[] { new Person { Name = "Alice" }, new Person { Name = "Bob" } }
            };

            Assert.Equal(new[] { "Alice", "Bob" }, viewModel.Items.Select(i => i.DisplayText));
        }

        /// <summary>
        /// 測試：ItemsSource 設為 null 時，Items/FilteredItems 皆清空為空集合
        /// </summary>
        [Fact(DisplayName = "ItemsSource：設為 null 時 Items/FilteredItems 皆清空")]
        public void ItemsSource_Null_ProducesEmptyItems()
        {
            var viewModel = new DropDownListViewModel { ItemsSource = new[] { "Alice" } };

            viewModel.ItemsSource = null;

            Assert.Empty(viewModel.Items);
            Assert.Empty(viewModel.FilteredItems);
        }

        /// <summary>
        /// 測試：SearchText 依 DisplayText 做 Contains 篩選(不分大小寫)
        /// </summary>
        [Fact(DisplayName = "SearchText：依 DisplayText 做 Contains 篩選")]
        public void SearchText_FiltersByDisplayText_UsingContains()
        {
            var viewModel = new DropDownListViewModel { ItemsSource = new[] { "Alice", "Bob", "Alicia" } };

            viewModel.SearchText = "ali";

            Assert.Equal(new[] { "Alice", "Alicia" }, viewModel.FilteredItems.Select(i => i.DisplayText));
        }

        /// <summary>
        /// 測試：清空 SearchText 後恢復顯示全部項目
        /// </summary>
        [Fact(DisplayName = "SearchText：清空後恢復顯示全部項目")]
        public void SearchText_Cleared_RestoresAllItems()
        {
            var viewModel = new DropDownListViewModel { ItemsSource = new[] { "Alice", "Bob" } };
            viewModel.SearchText = "ali";

            viewModel.SearchText = string.Empty;

            Assert.Equal(2, viewModel.FilteredItems.Count);
        }

        /// <summary>
        /// 測試：單選模式下，切換選取新項目會自動取消前一個已選項目
        /// </summary>
        [Fact(DisplayName = "ToggleSelected：單選模式只保留最後一個選取項目")]
        public void ToggleSelected_SingleMode_OnlyKeepsOneSelection()
        {
            var viewModel = new DropDownListViewModel { ItemsSource = new[] { "Alice", "Bob", "Carol" } };

            viewModel.ToggleSelected(viewModel.Items[0]);
            viewModel.ToggleSelected(viewModel.Items[1]);

            Assert.Equal("Bob", viewModel.SelectedValue);
            Assert.False(viewModel.Items[0].IsSelected);
            Assert.True(viewModel.Items[1].IsSelected);
        }

        /// <summary>
        /// 測試：多選模式下，切換選取會累加到已選清單
        /// </summary>
        [Fact(DisplayName = "ToggleSelected：多選模式累加選取項目")]
        public void ToggleSelected_MultiMode_AccumulatesSelection()
        {
            var viewModel = new DropDownListViewModel
            {
                SelectionMode = SelectionMode.Multiple,
                ItemsSource = new[] { "Alice", "Bob", "Carol" }
            };

            viewModel.ToggleSelected(viewModel.Items[0]);
            viewModel.ToggleSelected(viewModel.Items[2]);

            Assert.Equal(new object?[] { "Alice", "Carol" }, viewModel.SelectedValues);
        }

        /// <summary>
        /// 測試：多選模式下，再次切換同一個已選項目會將其取消選取
        /// </summary>
        [Fact(DisplayName = "ToggleSelected：多選模式再次切換會取消選取")]
        public void ToggleSelected_MultiMode_TogglingSameItemDeselectsIt()
        {
            var viewModel = new DropDownListViewModel
            {
                SelectionMode = SelectionMode.Multiple,
                ItemsSource = new[] { "Alice", "Bob" }
            };

            viewModel.ToggleSelected(viewModel.Items[0]);
            viewModel.ToggleSelected(viewModel.Items[0]);

            Assert.Empty(viewModel.SelectedValues);
        }

        /// <summary>
        /// 測試：從多選切回單選時，只保留第一個已選項目
        /// </summary>
        [Fact(DisplayName = "SelectionMode：切回單選時只保留第一個已選項目")]
        public void SwitchingToSingleMode_KeepsOnlyFirstSelectedItem()
        {
            var viewModel = new DropDownListViewModel
            {
                SelectionMode = SelectionMode.Multiple,
                ItemsSource = new[] { "Alice", "Bob", "Carol" }
            };
            viewModel.ToggleSelected(viewModel.Items[0]);
            viewModel.ToggleSelected(viewModel.Items[1]);

            viewModel.SelectionMode = SelectionMode.Single;

            Assert.Single(viewModel.SelectedValues);
            Assert.Equal("Alice", viewModel.SelectedValue);
        }

        /// <summary>
        /// 測試：SelectValue 依值標記對應項目為已選取
        /// </summary>
        [Fact(DisplayName = "SelectValue：依值標記對應項目為已選取")]
        public void SelectValue_MarksMatchingItemSelected()
        {
            var viewModel = new DropDownListViewModel { ItemsSource = new[] { "Alice", "Bob" } };

            viewModel.SelectValue("Bob");

            Assert.Equal("Bob", viewModel.SelectedValue);
        }

        /// <summary>
        /// 測試：SelectValues 依值集合標記對應多個項目為已選取
        /// </summary>
        [Fact(DisplayName = "SelectValues：依值集合標記多個項目為已選取")]
        public void SelectValues_MarksMatchingItemsSelected()
        {
            var viewModel = new DropDownListViewModel
            {
                SelectionMode = SelectionMode.Multiple,
                ItemsSource = new[] { "Alice", "Bob", "Carol" }
            };

            viewModel.SelectValues(new object?[] { "Bob", "Carol" });

            Assert.Equal(new object?[] { "Bob", "Carol" }, viewModel.SelectedValues);
        }

        /// <summary>
        /// 測試：ClearSelection 取消所有項目的選取狀態
        /// </summary>
        [Fact(DisplayName = "ClearSelection：取消所有項目的選取狀態")]
        public void ClearSelection_DeselectsAllItems()
        {
            var viewModel = new DropDownListViewModel
            {
                SelectionMode = SelectionMode.Multiple,
                ItemsSource = new[] { "Alice", "Bob" }
            };
            viewModel.SelectValues(new object?[] { "Alice", "Bob" });

            viewModel.ClearSelection();

            Assert.Empty(viewModel.SelectedValues);
            Assert.Null(viewModel.SelectedValue);
        }

        /// <summary>
        /// 測試：替換 ItemsSource 後，若新來源仍含相同值則保留原本選取狀態
        /// </summary>
        [Fact(DisplayName = "ItemsSource：替換來源後保留符合值的選取狀態")]
        public void ItemsSourceReplaced_PreservesSelectionForMatchingValues()
        {
            var viewModel = new DropDownListViewModel { ItemsSource = new[] { "Alice", "Bob" } };
            viewModel.SelectValue("Bob");

            viewModel.ItemsSource = new[] { "Bob", "Carol" };

            Assert.Equal("Bob", viewModel.SelectedValue);
        }

        /// <summary>
        /// 測試：ItemsSource 為 ObservableCollection 時，來源集合異動會自動刷新 Items
        /// </summary>
        [Fact(DisplayName = "ItemsSource：ObservableCollection 異動時自動刷新 Items")]
        public void ObservableItemsSource_CollectionChanged_RefreshesItems()
        {
            var source = new ObservableCollection<string> { "Alice" };
            var viewModel = new DropDownListViewModel { ItemsSource = source };

            source.Add("Bob");

            Assert.Equal(new[] { "Alice", "Bob" }, viewModel.Items.Select(i => i.DisplayText));
        }

        /// <summary>
        /// 測試：切換不屬於目前清單的項目(非本 ViewModel 產生的實例)會被忽略，不影響選取狀態
        /// </summary>
        [Fact(DisplayName = "ToggleSelected：不屬於清單的項目會被忽略")]
        public void ToggleSelected_ItemNotInList_IsIgnored()
        {
            var viewModel = new DropDownListViewModel { ItemsSource = new[] { "Alice" } };
            var foreignItem = new DropDownItemViewModel("Bob", "Bob");

            viewModel.ToggleSelected(foreignItem);

            Assert.Null(viewModel.SelectedValue);
        }

        /// <summary>
        /// 測試：選取狀態異動時會觸發 SelectionChanged 事件
        /// </summary>
        [Fact(DisplayName = "SelectionChanged：選取狀態異動時觸發事件")]
        public void SelectionChanged_RaisedWhenSelectionChanges()
        {
            var viewModel = new DropDownListViewModel { ItemsSource = new[] { "Alice", "Bob" } };
            int raisedCount = 0;
            viewModel.SelectionChanged += (_, _) => raisedCount++;

            viewModel.ToggleSelected(viewModel.Items[0]);

            Assert.True(raisedCount > 0);
        }

        /// <summary>
        /// 測試：單選模式下，SelectionSummary 反映目前選取項目的顯示文字
        /// </summary>
        [Fact(DisplayName = "SelectionSummary：單選模式反映選取項目的顯示文字")]
        public void SelectionSummary_SingleMode_ReflectsSelectedDisplayText()
        {
            var viewModel = new DropDownListViewModel { ItemsSource = new[] { "Alice", "Bob" } };

            viewModel.ToggleSelected(viewModel.Items[1]);

            Assert.Equal("Bob", viewModel.SelectionSummary);
        }

        /// <summary>
        /// 測試：多選模式下，SelectionSummary 以逗號串接所有已選項目的顯示文字
        /// </summary>
        [Fact(DisplayName = "SelectionSummary：多選模式以逗號串接已選項目")]
        public void SelectionSummary_MultiMode_JoinsSelectedDisplayTexts()
        {
            var viewModel = new DropDownListViewModel
            {
                SelectionMode = SelectionMode.Multiple,
                ItemsSource = new[] { "Alice", "Bob", "Carol" }
            };

            viewModel.ToggleSelected(viewModel.Items[0]);
            viewModel.ToggleSelected(viewModel.Items[2]);

            Assert.Equal("Alice, Carol", viewModel.SelectionSummary);
        }
    }
}
