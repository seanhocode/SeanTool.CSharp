using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Xunit;

namespace SeanTool.CSharp.WPFTool.Test
{
    /// <summary>
    /// DynamicDataGridViewModel 單元測試集合
    /// 驗證資料來源、篩選、排序、資料編輯等 ViewModel 核心功能
    /// </summary>
    public class DynamicDataGridViewModelUnitTest
    {
        /// <summary>
        /// 測試：資料來源設定 - 自動掃描屬性並產生預設欄位定義與篩選條件
        /// </summary>
        [Fact(DisplayName = "資料來源設定 - 自動產生預設欄位定義與篩選")]
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
        [Fact(DisplayName = "篩選變更 - 套用所有欄位的篩選條件")]
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
        /// 測試：清除篩選 - 清除所有欄位的篩選條件
        /// 驗證清除功能：清除後所有篩選皆重設，資料恢復為未篩選的全部項目
        /// </summary>
        [Fact(DisplayName = "清除篩選：清除後所有欄位條件重設並恢復全部資料")]
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
        /// 測試：ItemType 型別自動推斷 - 從資料來源(陣列/ObservableCollection)偵測項目型別
        /// 驗證型別推斷：不同型態的泛型集合皆可正確推斷出相同的項目型別
        /// </summary>
        [Fact(DisplayName = "ItemType：從不同泛型集合來源正確推斷項目型別")]
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
        [Fact(DisplayName = "非泛型且空的集合，型別真的無法推斷時應保持 null（不亂猜）")]
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
        [Fact(DisplayName = "DataSource 觸發 Reset（例如 Clear()）後，選取狀態應清空而非殘留舊 item")]
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
        [Fact(DisplayName = "DataSource = null 不崩潰，欄位/篩選/顯示項目皆回歸空狀態")]
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
        [Fact(DisplayName = "空集合不崩潰，FilteredItems 為空但不為 null")]
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

        /// <summary>
        /// 測試：DataSource 為 ObservableCollection - 來源集合異動(Add/Remove)即時反映到 FilteredItems，且沒有篩選時共用同一參考
        /// </summary>
        [Fact(DisplayName = "DataSource：ObservableCollection 異動即時反映到 FilteredItems")]
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
        /// 測試：資料來源變更 - 重建篩選條件並重新掃描欄位定義
        /// 驗證資料來源可替換性：變更 DataSource 後篩選數量與 FilteredItems 項目數皆正確更新
        /// </summary>
        [Fact(DisplayName = "資料來源變更：重建篩選條件並重新掃描欄位")]
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
        /// 測試：顯示項目即時更新 - 套用篩選條件後 FilteredItems 立即反映篩選結果
        /// 驗證篩選連動：FilterViewModel.ApplyCommand 執行後，FilteredItems 隨之更新
        /// </summary>
        [Fact(DisplayName = "FilteredItems：套用篩選後立即反映篩選結果")]
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
        /// 測試：設定欄位定義為 null - 自動改用反射掃描產生預設欄位定義
        /// 驗證自動產生：傳入 null 時退回自動掃描的預設欄位定義
        /// </summary>
        [Fact(DisplayName = "SetColumnDefinitions(null)：自動改用反射掃描的預設欄位")]
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
        /// 測試：手動指定欄位定義 - 優先於自動掃描的預設欄位
        /// 驗證優先級：手動 ColumnDefinitions 完全取代自動掃描結果
        /// </summary>
        [Fact(DisplayName = "SetColumnDefinitions：手動欄位定義優先於自動掃描")]
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

        /// <summary>
        /// 測試：手動欄位定義指定 FilterValueType - 覆寫依屬性型別自動推斷出的篩選值型別
        /// </summary>
        [Fact(DisplayName = "SetColumnDefinitions：手動 FilterValueType 覆寫型別自動推斷")]
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

        /// <summary>
        /// 測試：手動欄位定義 - 每一個欄位都會各自產生對應的 FilterViewModel
        /// </summary>
        [Fact(DisplayName = "SetColumnDefinitions：每個手動欄位皆產生對應的篩選")]
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

        /// <summary>
        /// 測試：SetItemSelected - 追蹤單一 SelectedItem(最後勾選者)與完整的 SelectedItems 清單
        /// </summary>
        [Fact(DisplayName = "SetItemSelected：追蹤 SelectedItem 與 SelectedItems 清單")]
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

        /// <summary>
        /// 測試：DataSource 移除已勾選的項目 - 選取清單自動移除該項目，其餘選取狀態不受影響
        /// </summary>
        [Fact(DisplayName = "選取狀態：移除已勾選項目時選取清單自動同步")]
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

        /// <summary>
        /// 測試：套用篩選後被隱藏的已勾選項目 - 自動從 SelectedItems 移除，僅保留篩選後仍可見的項目
        /// </summary>
        [Fact(DisplayName = "選取狀態：套用篩選後移除被隱藏的已勾選項目")]
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
        [Fact(DisplayName = "DataSource 綁定 DataTable(DefaultView) - 自動掃描 DataTable 的欄位並可正確篩選")]
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
