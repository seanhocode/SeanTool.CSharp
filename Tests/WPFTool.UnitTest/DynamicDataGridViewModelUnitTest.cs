using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using SeanTool.CSharp.WPFTool.Models.DynamicDataGrid;
using SeanTool.CSharp.WPFTool.Enums.Filter;
using Xunit;
using SeanTool.CSharp.WPFTool.Models.Filter;

namespace SeanTool.CSharp.WPFTool.Test
{
    /// <summary>
    /// DynamicDataGridViewModel 單元測試集合
    /// 驗證資料來源、篩選、排序、資料編輯等 ViewModel 核心功能
    /// </summary>
    public class DynamicDataGridViewModelUnitTest
    {
        /// <summary>
        /// 測試：資料來源設定 - 自動產生預設欄位定義與篩選
        /// 驗證自動掃描：資料來源設置時自動產生供隊位定義與篩選
        /// </summary>
        [Fact]
        public void DataSource_BuildsDefaultColumnsAndFilters()
        {
            var viewModel = new DynamicDataGridViewModel
            {
                DataSource = new[] { new Person { Name = "Alice" } }
            };

            Assert.Equal(new[] { "Age", "Name" }, viewModel.ColumnDefinitions.Select(item => item.BindingPath));
            Assert.Equal(new[] { "Age", "Name" }, viewModel.Filters.Select(item => item.PropertyName));
        }

        /// <summary>
        /// 測試：篩選變更 - 套用所有欄位的篩選條件
        /// 驗證篩選應用：篩選改變時自動轉換爲適當的操作符、將本地資料重新篩選
        /// </summary>
        [Fact]
        public void FilterChanges_ApplyAllColumnConditions()
        {
            var viewModel = CreateViewModel();
            FilterViewModel ageFilter = viewModel.Filters.Single(item => item.PropertyName == nameof(Person.Age));
            FilterViewModel nameFilter = viewModel.Filters.Single(item => item.PropertyName == nameof(Person.Name));

            ageFilter.Operator = FilterOperator.GreaterThan;
            ageFilter.Value = "25";
            nameFilter.Operator = FilterOperator.StartsWith;
            nameFilter.Value = "Ali";

            Assert.Equal(3, viewModel.FilteredItems!.Cast<Person>().Count());

            ageFilter.ApplyCommand.Execute(null);
            nameFilter.ApplyCommand.Execute(null);

            Person[] result = viewModel.FilteredItems!.Cast<Person>().ToArray();

            Assert.Equal(new[] { "Alice", "Alicia" }, result.Select(person => person.Name));
        }

        /// <summary>
        /// 測試：清除篩選 - 鞛除所有碩選條件
        /// 驗證清除功能：清除後所有篩選犠新抄齤，資料全部轉讀
        /// </summary>
        [Fact]
        public void ClearFilters_RemovesAllConditions()
        {
            var viewModel = CreateViewModel();
            FilterViewModel filter = viewModel.Filters.Single(item => item.PropertyName == nameof(Person.Name));
            filter.Value = "Ali";

            viewModel.ClearFilters();

            Assert.All(viewModel.Filters, item => Assert.False(item.HasValue));
            Assert.Equal(3, viewModel.FilteredItems!.Cast<Person>().Count());
        }

        /// <summary>
        /// 測試：顧模式型訪週 - 從資料來源搜索
        /// 驗證型別掭示：啊陣溢式資料來源也能正確穞捳這些穞型
        /// </summary>
        [Fact]
        public void ItemType_IsDetectedFromDataSource()
        {
            var viewModel = new DynamicDataGridViewModel
            {
                DataSource = new[] { new Person() }
            };

            Assert.Equal(typeof(Person), viewModel.ItemType);

            viewModel.DataSource = new ObservableCollection<Person>();
            Assert.Equal(typeof(Person), viewModel.ItemType);
        }

        /// <summary>
        /// 測試：非泛型且空的集合，型別真的無法推斷時應保持 null（不亂猜）
        /// </summary>
        [Fact]
        public void ItemType_StaysNull_WhenUndetectable()
        {
            var viewModel = new DynamicDataGridViewModel
            {
                DataSource = new ArrayList()
            };

            Assert.Null(viewModel.ItemType);
            Assert.Empty(viewModel.ColumnDefinitions);
        }

        /// <summary>
        /// 測試：DataSource 觸發 Reset（例如 Clear()）後，選取狀態應清空而非殘留舊 item
        /// </summary>
        [Fact]
        public void SelectionState_DataSourceReset_ClearsStaleSelection()
        {
            var source = new ObservableCollection<Person>
            {
                new() { Name = "Alice" },
                new() { Name = "Bob" }
            };
            var viewModel = new DynamicDataGridViewModel { DataSource = source };
            viewModel.SetItemSelected(source[0], true);
            viewModel.SetItemSelected(source[1], true);

            source.Clear();

            Assert.Empty(viewModel.SelectedItems);
            Assert.Null(viewModel.SelectedItem);
        }

        /// <summary>
        /// 測試：DataSource = null 不崩潰，欄位/篩選/顯示項目皆回歸空狀態
        /// </summary>
        [Fact]
        public void DataSource_Null_DoesNotThrowAndClearsState()
        {
            var viewModel = new DynamicDataGridViewModel
            {
                DataSource = new[] { new Person { Name = "Alice" } }
            };

            viewModel.DataSource = null;

            Assert.Null(viewModel.ItemType);
            Assert.Null(viewModel.FilteredItems);
            Assert.Empty(viewModel.ColumnDefinitions);
            Assert.Empty(viewModel.Filters);
        }

        /// <summary>
        /// 測試：空集合不崩潰，FilteredItems 為空但不為 null
        /// </summary>
        [Fact]
        public void DataSource_EmptyCollection_DoesNotThrow()
        {
            var viewModel = new DynamicDataGridViewModel
            {
                DataSource = Array.Empty<Person>()
            };

            Assert.Equal(typeof(Person), viewModel.ItemType);
            Assert.NotEmpty(viewModel.ColumnDefinitions);
            Assert.Empty(viewModel.FilteredItems!.Cast<Person>());
        }

        [Fact]
        public void DataSourceCollectionChanges_RefreshFilteredItems()
        {
            var source = new ObservableCollection<Person>
            {
                new() { Name = "Alice" }
            };
            var viewModel = new DynamicDataGridViewModel { DataSource = source };

            Assert.Same(source, viewModel.FilteredItems);

            source.Add(new Person { Name = "Bob" });
            Assert.Equal(2, viewModel.FilteredItems!.Cast<Person>().Count());
            Assert.Same(source, viewModel.FilteredItems);

            source.RemoveAt(0);
            Assert.Single(viewModel.FilteredItems!.Cast<Person>());
            Assert.Equal("Bob", viewModel.FilteredItems!.Cast<Person>().Single().Name);
            Assert.Same(source, viewModel.FilteredItems);
        }

        /// <summary>
        /// 測試：資料來源變更 - 重建篩選與重新掃描欄位
        /// 驗證資料靈掴性：變更資料來源時筑錯袋與項目數量
        /// </summary>
        [Fact]
        public void DataSource_ChangeResetsFiltersAndRefreshesColumns()
        {
            var viewModel = new DynamicDataGridViewModel
            {
                DataSource = new[] { new Person { Name = "Alice" } }
            };
            viewModel.SetColumnDefinitions(null);

            int oldFilterCount = viewModel.Filters.Count;

            viewModel.DataSource = new[] { new Person { Name = "Bob" } };

            // 驗證篩選已重建
            Assert.Equal(oldFilterCount, viewModel.Filters.Count);
            Assert.Equal(1, viewModel.FilteredItems!.Cast<Person>().Count());
        }

        /// <summary>
        /// 測試：显示項目暫時更新 - 套用篩選時轉換
        /// 驗證篩選轉換：篩選條件改變時 FilteredItems 及時暫新伱數
        /// </summary>
        [Fact]
        public void FilteredItems_UpdatesWhenFilterApplied()
        {
            var viewModel = CreateViewModel();
            Assert.Equal(3, viewModel.FilteredItems!.Cast<Person>().Count());

            var nameFilter = viewModel.Filters.Single(f => f.PropertyName == nameof(Person.Name));
            nameFilter.Value = "Alice";
            nameFilter.ApplyCommand.Execute(null);

            Assert.Single(viewModel.FilteredItems!.Cast<Person>());
            Assert.Equal("Alice", viewModel.FilteredItems!.Cast<Person>().First().Name);
        }

        /// <summary>
        /// 測試：設定供隊位定義 (null) - 产控預設定義
        /// 驗證自動產生：null 传递時自動本地斃描供隊位定義
        /// </summary>
        [Fact]
        public void SetColumnDefinitions_WithNullGeneratesDefault()
        {
            var viewModel = new DynamicDataGridViewModel
            {
                DataSource = new[] { new Person { Name = "Test", Age = 30 } }
            };

            viewModel.SetColumnDefinitions(null);

            Assert.NotEmpty(viewModel.ColumnDefinitions);
            // 驗證欄位包含 Age 和 Name（可能排序不同）
            var headers = viewModel.ColumnDefinitions.Select(c => c.BindingPath).ToList();
            Assert.Contains(nameof(Person.Age), headers);
            Assert.Contains(nameof(Person.Name), headers);
        }

        /// <summary>
        /// 測試：手動供隊位定義 - 罨寶自動上佋
        /// 驗證优先級：手動供隊位定義有既會罨寶自動上佋
        /// </summary>
        [Fact]
        public void SetColumnDefinitions_Manual_OverridesDefault()
        {
            var viewModel = new DynamicDataGridViewModel
            {
                DataSource = new[] { new Person { Name = "Test", Age = 30 } }
            };

            var customDef = new[]
            {
                new DynamicDataGridColumnDefinition { Header = "Custom Name", BindingPath = nameof(Person.Name) }
            };
            viewModel.SetColumnDefinitions(customDef);

            Assert.Single(viewModel.ColumnDefinitions);
            Assert.Equal("Custom Name", viewModel.ColumnDefinitions.First().Header);
        }

        [Fact]
        public void SetColumnDefinitions_ManualFilterValueType_OverridesTypeInference()
        {
            var viewModel = new DynamicDataGridViewModel
            {
                DataSource = new[] { new Person { Name = "Test", Age = 30 } }
            };

            viewModel.SetColumnDefinitions(new[]
            {
                new DynamicDataGridColumnDefinition
                {
                    Header = "Name",
                    BindingPath = nameof(Person.Name),
                    FilterValueType = FilterValueType.DateTime
                }
            });

            FilterViewModel filter = Assert.Single(viewModel.Filters);
            Assert.Equal(FilterValueType.DateTime, filter.FilterDefinition.ValueType);
        }

        [Fact]
        public void SetColumnDefinitions_ManualDefinitions_CreateFiltersForEveryColumn()
        {
            var viewModel = new DynamicDataGridViewModel
            {
                DataSource = new[] { new Person { Name = "Test", Age = 30 } }
            };

            viewModel.SetColumnDefinitions(new[]
            {
                new DynamicDataGridColumnDefinition { Header = "Name", BindingPath = nameof(Person.Name) }
            });

            Assert.Single(viewModel.Filters);
            Assert.Equal(nameof(Person.Name), viewModel.Filters[0].PropertyName);
        }

        [Fact]
        public void SelectionState_SetItemSelected_TracksSelectedItemAndList()
        {
            var viewModel = new DynamicDataGridViewModel();
            object first = new();
            object second = new();

            viewModel.SetItemSelected(first, true);
            viewModel.SetItemSelected(second, true);

            Assert.Same(second, viewModel.SelectedItem);
            Assert.Equal(2, viewModel.SelectedItems.Count);
            Assert.Contains(first, viewModel.SelectedItems);
            Assert.Contains(second, viewModel.SelectedItems);

            viewModel.SetItemSelected(second, false);

            Assert.Same(first, viewModel.SelectedItem);
            Assert.Single(viewModel.SelectedItems);
            Assert.Contains(first, viewModel.SelectedItems);
        }

        [Fact]
        public void SelectionState_DataSourceRemove_DropsRemovedSelection()
        {
            var source = new ObservableCollection<Person>
            {
                new() { Name = "Alice" },
                new() { Name = "Bob" }
            };
            var viewModel = new DynamicDataGridViewModel { DataSource = source };
            Person removed = source[0];
            Person kept = source[1];

            viewModel.SetItemSelected(removed, true);
            viewModel.SetItemSelected(kept, true);
            source.Remove(removed);

            Assert.Same(kept, viewModel.SelectedItem);
            Assert.Single(viewModel.SelectedItems);
            Assert.Contains(kept, viewModel.SelectedItems);
            Assert.DoesNotContain(removed, viewModel.SelectedItems);
        }

        [Fact]
        public void SelectionState_FilterApplied_DropsHiddenSelections()
        {
            var source = new ObservableCollection<Person>
            {
                new() { Name = "Alice", Age = 31 },
                new() { Name = "Bob", Age = 22 }
            };
            var viewModel = new DynamicDataGridViewModel { DataSource = source };
            viewModel.SetColumnDefinitions(new[]
            {
                new DynamicDataGridColumnDefinition { Header = "Name", BindingPath = nameof(Person.Name) },
                new DynamicDataGridColumnDefinition { Header = "Age", BindingPath = nameof(Person.Age) }
            });

            Person alice = source[0];
            Person bob = source[1];
            viewModel.SetItemSelected(alice, true);
            viewModel.SetItemSelected(bob, true);

            FilterViewModel nameFilter = viewModel.Filters.Single(item => item.PropertyName == nameof(Person.Name));
            nameFilter.Operator = FilterOperator.Equals;
            nameFilter.Value = "Alice";
            nameFilter.ApplyCommand.Execute(null);

            Assert.Single(viewModel.FilteredItems!.Cast<Person>());
            Assert.Same(alice, viewModel.FilteredItems!.Cast<Person>().Single());
            Assert.Single(viewModel.SelectedItems);
            Assert.Contains(alice, viewModel.SelectedItems);
            Assert.DoesNotContain(bob, viewModel.SelectedItems);
            Assert.Same(alice, viewModel.SelectedItem);
        }

        /// <summary>
        /// 測試：DataSource 綁定 DataTable(DefaultView) - 自動掃描 DataTable 的欄位並可正確篩選
        /// 驗證 DataTable 支援：ColumnDefinitions/Filters 依 DataColumn 產生，FilteredItems 依欄位值正確篩選
        /// </summary>
        [Fact]
        public void DataSource_BindsDataTable_BuildsColumnsAndFilters()
        {
            var table = new System.Data.DataTable();
            table.Columns.Add("Name", typeof(string));
            table.Columns.Add("Age", typeof(int));
            table.Rows.Add("Alice", 31);
            table.Rows.Add("Bob", 22);

            var viewModel = new DynamicDataGridViewModel { DataSource = table.DefaultView };

            Assert.Equal(typeof(System.Data.DataRowView), viewModel.ItemType);
            Assert.Equal(new[] { "Age", "Name" }, viewModel.ColumnDefinitions.Select(item => item.BindingPath));
            Assert.Equal(new[] { "Age", "Name" }, viewModel.Filters.Select(item => item.PropertyName));

            FilterViewModel nameFilter = viewModel.Filters.Single(item => item.PropertyName == "Name");
            nameFilter.Operator = FilterOperator.Equals;
            nameFilter.Value = "Alice";
            nameFilter.ApplyCommand.Execute(null);

            System.Data.DataRowView row = Assert.Single(viewModel.FilteredItems!.Cast<System.Data.DataRowView>());
            Assert.Equal("Alice", row["Name"]);
        }

        private static DynamicDataGridViewModel CreateViewModel()
        {
            var viewModel = new DynamicDataGridViewModel
            {
                DataSource = new[]
                {
                    new Person { Name = "Alice", Age = 31 },
                    new Person { Name = "Bob", Age = 22 },
                    new Person { Name = "Alicia", Age = 29 }
                }
            };
            viewModel.SetColumnDefinitions(new[]
            {
                new DynamicDataGridColumnDefinition { Header = "Age", BindingPath = nameof(Person.Age) },
                new DynamicDataGridColumnDefinition { Header = "Name", BindingPath = nameof(Person.Name) }
            });
            return viewModel;
        }

        private sealed class Person
        {
            public int Age { get; set; }

            [DisplayName("Name")]
            public string? Name { get; set; }
        }
    }
}
