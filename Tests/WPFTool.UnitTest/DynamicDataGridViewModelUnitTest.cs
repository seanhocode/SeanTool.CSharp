using System.Collections.ObjectModel;
using System.ComponentModel;
using SeanTool.CSharp.WPF;
using Xunit;

namespace SeanTool.CSharp.WPF.Test
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
            DynamicDataGridFilterViewModel ageFilter = viewModel.Filters.Single(item => item.PropertyName == nameof(Person.Age));
            DynamicDataGridFilterViewModel nameFilter = viewModel.Filters.Single(item => item.PropertyName == nameof(Person.Name));

            ageFilter.Operator = DynamicDataGridFilterOperator.GreaterThan;
            ageFilter.Value = "25";
            nameFilter.Operator = DynamicDataGridFilterOperator.StartsWith;
            nameFilter.Value = "Ali";

            Assert.Equal(3, viewModel.DisplayItems!.Cast<Person>().Count());

            ageFilter.ApplyCommand.Execute(null);
            nameFilter.ApplyCommand.Execute(null);

            Person[] result = viewModel.DisplayItems!.Cast<Person>().ToArray();

            Assert.Equal(new[] { "Alice", "Alicia" }, result.Select(person => person.Name));
        }

        /// <summary>
        /// 測試：排序變更 - 暫示項目暫時更新
        /// 驗證排序功能：設定排序位欏與序讀後自動重新排列
        /// </summary>
        [Fact]
        public void SortChanges_UpdateDisplayItems()
        {
            var viewModel = CreateViewModel();

            viewModel.SortProperty = nameof(Person.Age);
            viewModel.SortDescending = true;

            Assert.Equal(new[] { 31, 29, 22 }, viewModel.DisplayItems!.Cast<Person>().Select(person => person.Age));
        }

        /// <summary>
        /// 測試：清除篩選 - 鞛除所有碩選條件
        /// 驗證清除功能：清除後所有篩選犠新抄齤，資料全部轉讀
        /// </summary>
        [Fact]
        public void ClearFilters_RemovesAllConditions()
        {
            var viewModel = CreateViewModel();
            DynamicDataGridFilterViewModel filter = viewModel.Filters.Single(item => item.PropertyName == nameof(Person.Name));
            filter.Value = "Ali";

            viewModel.ClearFilters();

            Assert.All(viewModel.Filters, item => Assert.False(item.HasValue));
            Assert.Equal(3, viewModel.DisplayItems!.Cast<Person>().Count());
        }

        /// <summary>
        /// 測試：CanWrite 屬性 - 反映資料來源的固娹性
        /// 驗證寫入阳和：ObservableCollection 支援寫入（CanWrite=true)、陣列不支援(CanWrite=false)
        /// </summary>
        [Fact]
        public void CanWrite_ReflectsDataSourceCapability()
        {
            var viewModel = new DynamicDataGridViewModel
            {
                DataSource = new ObservableCollection<Person>()
            };

            Assert.True(viewModel.CanWrite);

            viewModel.DataSource = new[] { new Person() };

            Assert.False(viewModel.CanWrite);
        }

        /// <summary>
        /// 測試：新建項目 - 使用資料來源顧模式
        /// 驗證物件創新：CreateNewItem 能正確根據資料來源类型創新實體
        /// </summary>
        [Fact]
        public void CreateNewItem_UsesDataSourceModelType()
        {
            var viewModel = new DynamicDataGridViewModel
            {
                DataSource = new ObservableCollection<Person>()
            };

            object? newItem = viewModel.CreateNewItem();

            Assert.IsType<Person>(newItem);
        }

        /// <summary>
        /// 測試：新增與移除項目 - 暫時更新資料來源
        /// 驗證數据操作：新增/移除同步暫時更新，避免揯捳不一致
        /// </summary>
        [Fact]
        public void AddAndRemoveItem_UpdatesDataSource()
        {
            var source = new ObservableCollection<Person>();
            var viewModel = new DynamicDataGridViewModel { DataSource = source };
            var item = new Person { Name = "New" };

            Assert.True(viewModel.AddItem(item));
            Assert.Contains(item, source);

            Assert.True(viewModel.RemoveItem(item));
            Assert.DoesNotContain(item, source);
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
            Assert.Equal(1, viewModel.DisplayItems!.Cast<Person>().Count());
        }

        /// <summary>
        /// 測試：新增失敗 - 資料來源是唯讀時
        /// 驗證阳和棄遭：資料來源無寫入權時 AddItem 應由回 false
        /// </summary>
        [Fact]
        public void AddItem_FailsWhenDataSourceIsReadOnly()
        {
            var viewModel = new DynamicDataGridViewModel
            {
                DataSource = new[] { new Person() }  // 陣列是唯讀
            };
            var newItem = new Person { Name = "New" };

            Assert.False(viewModel.CanWrite);
            Assert.False(viewModel.AddItem(newItem));
        }

        /// <summary>
        /// 測試：移除失敗 - 項目不存在于資料來源中
        /// 驗證防驢機制：榲旧項目無法移除，資料不變
        /// </summary>
        [Fact]
        public void RemoveItem_FailsWhenItemNotInDataSource()
        {
            var source = new ObservableCollection<Person> { new Person { Name = "Alice" } };
            var viewModel = new DynamicDataGridViewModel { DataSource = source };
            var item = new Person { Name = "Bob" };

            Assert.False(viewModel.RemoveItem(item));
            Assert.Equal(1, source.Count);
        }

        /// <summary>
        /// 測試：显示項目暫時更新 - 套用篩選時轉換
        /// 驗證篩選轉換：篩選條件改變時 DisplayItems 及時暫新伱數
        /// </summary>
        [Fact]
        public void DisplayItems_UpdatesWhenFilterApplied()
        {
            var viewModel = CreateViewModel();
            Assert.Equal(3, viewModel.DisplayItems!.Cast<Person>().Count());

            var nameFilter = viewModel.Filters.Single(f => f.PropertyName == nameof(Person.Name));
            nameFilter.Value = "Alice";
            nameFilter.ApplyCommand.Execute(null);

            Assert.Single(viewModel.DisplayItems!.Cast<Person>());
            Assert.Equal("Alice", viewModel.DisplayItems!.Cast<Person>().First().Name);
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
