using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Controls;
using SeanTool.CSharp.WPFTool.Models.DropDownList;
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

        [Fact]
        public void ItemsSource_WithoutDisplayMemberPath_UsesToString()
        {
            var viewModel = new DropDownListViewModel { ItemsSource = new[] { "Alice", "Bob" } };

            Assert.Equal(new[] { "Alice", "Bob" }, viewModel.Items.Select(i => i.DisplayText));
        }

        [Fact]
        public void ItemsSource_WithDisplayMemberPath_ReadsProperty()
        {
            var viewModel = new DropDownListViewModel
            {
                DisplayMemberPath = nameof(Person.Name),
                ItemsSource = new[] { new Person { Name = "Alice" }, new Person { Name = "Bob" } }
            };

            Assert.Equal(new[] { "Alice", "Bob" }, viewModel.Items.Select(i => i.DisplayText));
        }

        [Fact]
        public void ItemsSource_Null_ProducesEmptyItems()
        {
            var viewModel = new DropDownListViewModel { ItemsSource = new[] { "Alice" } };

            viewModel.ItemsSource = null;

            Assert.Empty(viewModel.Items);
            Assert.Empty(viewModel.FilteredItems);
        }

        [Fact]
        public void SearchText_FiltersByDisplayText_UsingContains()
        {
            var viewModel = new DropDownListViewModel { ItemsSource = new[] { "Alice", "Bob", "Alicia" } };

            viewModel.SearchText = "ali";

            Assert.Equal(new[] { "Alice", "Alicia" }, viewModel.FilteredItems.Select(i => i.DisplayText));
        }

        [Fact]
        public void SearchText_Cleared_RestoresAllItems()
        {
            var viewModel = new DropDownListViewModel { ItemsSource = new[] { "Alice", "Bob" } };
            viewModel.SearchText = "ali";

            viewModel.SearchText = string.Empty;

            Assert.Equal(2, viewModel.FilteredItems.Count);
        }

        [Fact]
        public void ToggleSelected_SingleMode_OnlyKeepsOneSelection()
        {
            var viewModel = new DropDownListViewModel { ItemsSource = new[] { "Alice", "Bob", "Carol" } };

            viewModel.ToggleSelected(viewModel.Items[0]);
            viewModel.ToggleSelected(viewModel.Items[1]);

            Assert.Equal("Bob", viewModel.SelectedValue);
            Assert.False(viewModel.Items[0].IsSelected);
            Assert.True(viewModel.Items[1].IsSelected);
        }

        [Fact]
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

        [Fact]
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

        [Fact]
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

        [Fact]
        public void SelectValue_MarksMatchingItemSelected()
        {
            var viewModel = new DropDownListViewModel { ItemsSource = new[] { "Alice", "Bob" } };

            viewModel.SelectValue("Bob");

            Assert.Equal("Bob", viewModel.SelectedValue);
        }

        [Fact]
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

        [Fact]
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

        [Fact]
        public void ItemsSourceReplaced_PreservesSelectionForMatchingValues()
        {
            var viewModel = new DropDownListViewModel { ItemsSource = new[] { "Alice", "Bob" } };
            viewModel.SelectValue("Bob");

            viewModel.ItemsSource = new[] { "Bob", "Carol" };

            Assert.Equal("Bob", viewModel.SelectedValue);
        }

        [Fact]
        public void ObservableItemsSource_CollectionChanged_RefreshesItems()
        {
            var source = new ObservableCollection<string> { "Alice" };
            var viewModel = new DropDownListViewModel { ItemsSource = source };

            source.Add("Bob");

            Assert.Equal(new[] { "Alice", "Bob" }, viewModel.Items.Select(i => i.DisplayText));
        }

        [Fact]
        public void ToggleSelected_ItemNotInList_IsIgnored()
        {
            var viewModel = new DropDownListViewModel { ItemsSource = new[] { "Alice" } };
            var foreignItem = new DropDownItemViewModel("Bob", "Bob");

            viewModel.ToggleSelected(foreignItem);

            Assert.Null(viewModel.SelectedValue);
        }

        [Fact]
        public void SelectionChanged_RaisedWhenSelectionChanges()
        {
            var viewModel = new DropDownListViewModel { ItemsSource = new[] { "Alice", "Bob" } };
            int raisedCount = 0;
            viewModel.SelectionChanged += (_, _) => raisedCount++;

            viewModel.ToggleSelected(viewModel.Items[0]);

            Assert.True(raisedCount > 0);
        }

        [Fact]
        public void SelectionSummary_SingleMode_ReflectsSelectedDisplayText()
        {
            var viewModel = new DropDownListViewModel { ItemsSource = new[] { "Alice", "Bob" } };

            viewModel.ToggleSelected(viewModel.Items[1]);

            Assert.Equal("Bob", viewModel.SelectionSummary);
        }

        [Fact]
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
