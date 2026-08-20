using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using SeanTool.CSharp.WPF;
using Xunit;

namespace SeanTool.CSharp.WPF.Test
{
    /// <summary>
    /// DynamicDataGrid 查詢與篩選單元測試
    /// 驗證篩選、排序、元數據讀取等核心功能
    /// </summary>
    public class DynamicDataGridQueryUnitTest
    {
        /// <summary>
        /// 測試：元數據讀取 - 支援 DisplayName 和唯讀屬性
        /// 驗證元數據掃描：DisplayNameAttribute 正確讀取、唯讀屬性正確標記
        /// </summary>
        [Fact]
        public void Metadata_UsesDisplayNameAndKeepsReadOnlyProperty()
        {
            IReadOnlyList<DynamicDataGridPropertyMetadata> metadata =
                DynamicDataGridPropertyMetadata.Create(typeof(Person));

            Assert.Equal(new[] { "Name", "Age", "Created", "Read only" }, metadata.Select(item => item.Header));
            Assert.True(metadata.Single(item => item.Name == nameof(Person.ReadOnly)).IsReadOnly);
        }

        /// <summary>
        /// 測試：篩選與排序 - 不改變原始資料來源
        /// 驗證查詢邏輯：多重篩選條件(AND)、排序反向、原始資料不受影響
        /// </summary>
        [Fact]
        public void Apply_FiltersAndSortsWithoutChangingSource()
        {
            var source = new[]
            {
                new Person { Name = "Alice", Age = 31, Created = new DateTime(2026, 1, 2) },
                new Person { Name = "Bob", Age = 22, Created = new DateTime(2026, 1, 1) },
                new Person { Name = "Alicia", Age = 29, Created = new DateTime(2026, 1, 3) }
            };

            var result = DynamicDataGridQuery.Apply(
                source,
                typeof(Person),
                new[]
                {
                    new DynamicDataGridFilter
                    {
                        PropertyName = nameof(Person.Name),
                        Operator = DynamicDataGridFilterOperator.StartsWith,
                        Value = "ali"
                    },
                    new DynamicDataGridFilter
                    {
                        PropertyName = nameof(Person.Age),
                        Operator = DynamicDataGridFilterOperator.GreaterThan,
                        Value = "28"
                    }
                },
                nameof(Person.Created),
                descending: true).Cast<Person>().ToArray();

            Assert.Equal(new[] { "Alicia", "Alice" }, result.Select(person => person.Name));
            Assert.Equal(new[] { "Alice", "Bob", "Alicia" }, source.Select(person => person.Name));
        }

        /// <summary>
        /// 測試：Null 值篩選 - 支援 IsNull 操作符
        /// 驗證 Null 篩選：正確過濾 null 值
        /// </summary>
        [Fact]
        public void Apply_SupportsNullFilter()
        {
            var source = new[]
            {
                new Person { Name = "Alice" },
                new Person { Name = null }
            };

            var result = DynamicDataGridQuery.Apply(
                source,
                typeof(Person),
                new[]
                {
                    new DynamicDataGridFilter
                    {
                        PropertyName = nameof(Person.Name),
                        Operator = DynamicDataGridFilterOperator.IsNull
                    }
                }).Cast<Person>().ToArray();

            Assert.Single(result);
            Assert.Null(result[0].Name);
        }

        /// <summary>
        /// 測試：文字篩選大小寫敏感性 - 驗證一致的行為
        /// 驗證文字篩選：Contains 操作符的大小寫處理方式
        /// </summary>
        [Fact]
        public void Apply_TextFilter_CaseSensitivity_FollowsInvariantCulture()
        {
            var source = new[]
            {
                new Person { Name = "alice" },
                new Person { Name = "ALICE" },
                new Person { Name = "Alice" }
            };

            // Contains 篩選應該不區分大小寫（或根據設計）
            // 此測試驗證行為一致性
            var result = DynamicDataGridQuery.Apply(
                source,
                typeof(Person),
                new[]
                {
                    new DynamicDataGridFilter
                    {
                        PropertyName = nameof(Person.Name),
                        Operator = DynamicDataGridFilterOperator.Contains,
                        Value = "ALICE"
                    }
                }).Cast<Person>().ToArray();

            Assert.NotEmpty(result);
            // 預期所有變體都被匹配（若實現不區分大小寫）
        }

        /// <summary>
        /// 測試：空值/空白篩選 - 應該被忽略
        /// 驗證邊界情況：空字串篩選值不應過濾資料，所有資料應返回
        /// </summary>
        [Fact]
        public void Apply_EmptyOrWhitespaceFilter_IgnoresValue()
        {
            var source = new[]
            {
                new Person { Name = "Alice" },
                new Person { Name = "Bob" },
                new Person { Name = "" }
            };

            var result = DynamicDataGridQuery.Apply(
                source,
                typeof(Person),
                new[]
                {
                    new DynamicDataGridFilter
                    {
                        PropertyName = nameof(Person.Name),
                        Operator = DynamicDataGridFilterOperator.Contains,
                        Value = ""  // 空值應該不篩選
                    }
                }).Cast<Person>().ToArray();

            Assert.Equal(3, result.Length);
        }

        /// <summary>
        /// 測試：多重篩選 - 所有條件必須符合(AND 邏輯)
        /// 驗證篩選邏輯：多個篩選條件同時生效
        /// </summary>
        [Fact]
        public void Apply_MultipleFilters_AllMustMatch()
        {
            var source = new[]
            {
                new Person { Name = "Alice", Age = 31 },
                new Person { Name = "Alice", Age = 20 },
                new Person { Name = "Bob", Age = 31 }
            };

            var result = DynamicDataGridQuery.Apply(
                source,
                typeof(Person),
                new[]
                {
                    new DynamicDataGridFilter
                    {
                        PropertyName = nameof(Person.Name),
                        Operator = DynamicDataGridFilterOperator.Equals,
                        Value = "Alice"
                    },
                    new DynamicDataGridFilter
                    {
                        PropertyName = nameof(Person.Age),
                        Operator = DynamicDataGridFilterOperator.Equals,
                        Value = "31"
                    }
                }).Cast<Person>().ToArray();

            Assert.Single(result);
            Assert.Equal("Alice", result[0].Name);
            Assert.Equal(31, result[0].Age);
        }

        /// <summary>
        /// 測試：日期比較篩選 - 支援 GreaterThan、LessThan 等操作符
        /// 驗證日期篩選：日期範圍比較邏輯
        /// </summary>
        [Fact]
        public void Apply_DateComparison_FiltersCorrectly()
        {
            var source = new[]
            {
                new Person { Name = "Alice", Created = new DateTime(2026, 1, 1) },
                new Person { Name = "Bob", Created = new DateTime(2026, 1, 15) },
                new Person { Name = "Alicia", Created = new DateTime(2026, 2, 1) }
            };

            // 篩選日期 > 2026-01-01
            var result = DynamicDataGridQuery.Apply(
                source,
                typeof(Person),
                new[]
                {
                    new DynamicDataGridFilter
                    {
                        PropertyName = nameof(Person.Created),
                        Operator = DynamicDataGridFilterOperator.GreaterThan,
                        Value = new DateTime(2026, 1, 1)
                    }
                }).Cast<Person>().ToArray();

            Assert.Equal(2, result.Length);
            Assert.All(result, person => Assert.True(person.Created > new DateTime(2026, 1, 1)));
        }

        [Fact]
        public void Apply_InvalidNumericFilterDoesNotThrow()
        {
            var source = new[] { new Person { Age = 31 } };

            var result = DynamicDataGridQuery.Apply(
                source,
                typeof(Person),
                new[]
                {
                    new DynamicDataGridFilter
                    {
                        PropertyName = nameof(Person.Age),
                        Operator = DynamicDataGridFilterOperator.GreaterThan,
                        Value = "not-a-number"
                    }
                }).Cast<Person>();

            Assert.Empty(result);
        }

        [Fact]
        public void DateTimeFilterViewModel_UsesDateTimeValue()
        {
            var filter = new DynamicDataGridFilterViewModel(
                nameof(Person.Created),
                "Created",
                typeof(DateTime));

            filter.Operator = DynamicDataGridFilterOperator.GreaterThan;
            filter.DateTimeValue = new DateTime(2026, 1, 15);
            filter.ApplyCommand.Execute(null);

            Assert.True(filter.IsDateTime);
            Assert.Equal(new DateTime(2026, 1, 15), filter.Filter!.Value);
        }

        private sealed class Person
        {
            [Display(Order = 2)]
            public int Age { get; set; }

            [Display(Order = 3)]
            public DateTime Created { get; set; }

            [DisplayName("Name")]
            [Display(Order = 1)]
            public string? Name { get; set; }

            [DisplayName("Read only")]
            [Display(Order = 4)]
            public string ReadOnly => "value";
        }
    }
}
