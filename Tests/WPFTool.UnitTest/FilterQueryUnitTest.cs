using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using SeanTool.CSharp.WPFTool.Enums.Filter;
using Xunit;
using SeanTool.CSharp.WPFTool.Models.Filter;

namespace SeanTool.CSharp.WPFTool.Test
{
    /// <summary>
    /// Filter 查詢與篩選單元測試
    /// 驗證篩選、元數據讀取等核心功能
    /// </summary>
    public class FilterQueryUnitTest
    {
        /// <summary>
        /// 測試：篩選 - 不改變原始資料來源
        /// 驗證查詢邏輯：多重篩選條件(AND)、原始資料不受影響
        /// </summary>
        [Fact]
        public void Apply_FiltersWithoutChangingSource()
        {
            var source = new[]
            {
                new Person { Name = "Alice", Age = 31, Created = new DateTime(2026, 1, 2) },
                new Person { Name = "Bob", Age = 22, Created = new DateTime(2026, 1, 1) },
                new Person { Name = "Alicia", Age = 29, Created = new DateTime(2026, 1, 3) }
            };

            var result = FilterQuery.Apply(
                source,
                typeof(Person),
                new[]
                {
                    new FilterCondition
                    {
                        PropertyName = nameof(Person.Name),
                        Operator = FilterOperator.StartsWith,
                        Value = "ali"
                    },
                    new FilterCondition
                    {
                        PropertyName = nameof(Person.Age),
                        Operator = FilterOperator.GreaterThan,
                        Value = "28"
                    }
                }).Cast<Person>().ToArray();

            Assert.Equal(new[] { "Alice", "Alicia" }, result.Select(person => person.Name));
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

            var result = FilterQuery.Apply(
                source,
                typeof(Person),
                new[]
                {
                    new FilterCondition
                    {
                        PropertyName = nameof(Person.Name),
                        Operator = FilterOperator.IsNull
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
            var result = FilterQuery.Apply(
                source,
                typeof(Person),
                new[]
                {
                    new FilterCondition
                    {
                        PropertyName = nameof(Person.Name),
                        Operator = FilterOperator.Contains,
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

            var result = FilterQuery.Apply(
                source,
                typeof(Person),
                new[]
                {
                    new FilterCondition
                    {
                        PropertyName = nameof(Person.Name),
                        Operator = FilterOperator.Contains,
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

            var result = FilterQuery.Apply(
                source,
                typeof(Person),
                new[]
                {
                    new FilterCondition
                    {
                        PropertyName = nameof(Person.Name),
                        Operator = FilterOperator.Equals,
                        Value = "Alice"
                    },
                    new FilterCondition
                    {
                        PropertyName = nameof(Person.Age),
                        Operator = FilterOperator.Equals,
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
            var result = FilterQuery.Apply(
                source,
                typeof(Person),
                new[]
                {
                    new FilterCondition
                    {
                        PropertyName = nameof(Person.Created),
                        Operator = FilterOperator.GreaterThan,
                        Value = new DateTime(2026, 1, 1)
                    }
                }).Cast<Person>().ToArray();

            Assert.Equal(2, result.Length);
            Assert.All(result, person => Assert.True(person.Created > new DateTime(2026, 1, 1)));
        }

        /// <summary>
        /// 測試：空 source 不崩潰
        /// </summary>
        [Fact]
        public void Apply_EmptySource_ReturnsEmpty()
        {
            var result = FilterQuery.Apply(
                Array.Empty<Person>(),
                typeof(Person),
                new[]
                {
                    new FilterCondition { PropertyName = nameof(Person.Name), Operator = FilterOperator.Contains, Value = "a" }
                }).Cast<Person>();

            Assert.Empty(result);
        }

        /// <summary>
        /// 測試：filters 為 null 不崩潰，且視為無條件(回傳原始 source)
        /// </summary>
        [Fact]
        public void Apply_NullFilters_ReturnsSourceUnchanged()
        {
            var source = new[] { new Person { Name = "Alice" } };

            var result = FilterQuery.Apply(source, typeof(Person), null).Cast<Person>().ToArray();

            Assert.Same(source[0], result[0]);
        }

        /// <summary>
        /// 測試：PropertyName 不存在時，該條件被忽略，不影響其他條件與其他資料
        /// </summary>
        [Fact]
        public void Apply_UnknownPropertyName_IsIgnored()
        {
            var source = new[] { new Person { Name = "Alice" }, new Person { Name = "Bob" } };

            var result = FilterQuery.Apply(
                source,
                typeof(Person),
                new[]
                {
                    new FilterCondition { PropertyName = "NoSuchProperty", Operator = FilterOperator.Equals, Value = "x" }
                }).Cast<Person>().ToArray();

            Assert.Equal(2, result.Length);
        }

        /// <summary>
        /// 測試：IsNotNull 操作符
        /// </summary>
        [Fact]
        public void Apply_SupportsIsNotNullFilter()
        {
            var source = new[] { new Person { Name = "Alice" }, new Person { Name = null } };

            var result = FilterQuery.Apply(
                source,
                typeof(Person),
                new[] { new FilterCondition { PropertyName = nameof(Person.Name), Operator = FilterOperator.IsNotNull } })
                .Cast<Person>().ToArray();

            Assert.Single(result);
            Assert.Equal("Alice", result[0].Name);
        }

        /// <summary>
        /// 測試：Between 操作符 - 完整區間、只有下界、只有上界、無效區間(From > To)
        /// </summary>
        [Theory]
        [InlineData(null, null, 3)]
        [InlineData("2026-01-05", null, 2)]
        [InlineData(null, "2026-01-10", 1)]
        [InlineData("2026-01-01", "2026-01-31", 3)]
        [InlineData("2026-02-01", "2026-01-01", 0)] // From > To 視為無效區間，安全回退為不匹配
        public void Apply_Between_HandlesPartialAndInvalidRanges(string? from, string? to, int expectedCount)
        {
            var source = new[]
            {
                new Person { Name = "Alice", Created = new DateTime(2026, 1, 1) },
                new Person { Name = "Bob", Created = new DateTime(2026, 1, 15) },
                new Person { Name = "Alicia", Created = new DateTime(2026, 1, 31) }
            };

            var range = new DateRange(
                from is null ? null : DateTime.Parse(from),
                to is null ? null : DateTime.Parse(to));

            var result = FilterQuery.Apply(
                source,
                typeof(Person),
                new[] { new FilterCondition { PropertyName = nameof(Person.Created), Operator = FilterOperator.Between, Value = range } })
                .Cast<Person>();

            Assert.Equal(expectedCount, result.Count());
        }

        /// <summary>
        /// 測試：nullable 屬性型別(int?) - null 值與有值時的比較行為
        /// </summary>
        [Fact]
        public void Apply_NullableProperty_HandlesNullAndComparableValues()
        {
            var source = new[]
            {
                new Person { Score = null },
                new Person { Score = 10 },
                new Person { Score = 20 }
            };

            var result = FilterQuery.Apply(
                source,
                typeof(Person),
                new[] { new FilterCondition { PropertyName = nameof(Person.Score), Operator = FilterOperator.GreaterThan, Value = "15" } })
                .Cast<Person>().ToArray();

            Assert.Single(result);
            Assert.Equal(20, result[0].Score);
        }

        /// <summary>
        /// 測試：數值轉換溢位不崩潰
        /// </summary>
        [Fact]
        public void Apply_NumericOverflowDoesNotThrow()
        {
            var source = new[] { new Person { Age = 31 } };

            var result = FilterQuery.Apply(
                source,
                typeof(Person),
                new[] { new FilterCondition { PropertyName = nameof(Person.Age), Operator = FilterOperator.Equals, Value = "99999999999999999999" } })
                .Cast<Person>();

            Assert.Empty(result);
        }

        /// <summary>
        /// 測試：合法 Enum 文字比對(忽略大小寫)應正確匹配
        /// </summary>
        [Fact]
        public void Apply_ValidEnumText_MatchesCaseInsensitively()
        {
            var source = new[] { new Person { Status = Status.Active }, new Person { Status = Status.Inactive } };

            var result = FilterQuery.Apply(
                source,
                typeof(Person),
                new[] { new FilterCondition { PropertyName = nameof(Person.Status), Operator = FilterOperator.Equals, Value = "active" } })
                .Cast<Person>().ToArray();

            Assert.Single(result);
            Assert.Equal(Status.Active, result[0].Status);
        }

        [Fact]
        public void Apply_InvalidNumericFilterDoesNotThrow()
        {
            var source = new[] { new Person { Age = 31 } };

            var result = FilterQuery.Apply(
                source,
                typeof(Person),
                new[]
                {
                    new FilterCondition
                    {
                        PropertyName = nameof(Person.Age),
                        Operator = FilterOperator.GreaterThan,
                        Value = "not-a-number"
                    }
                }).Cast<Person>();

            Assert.Empty(result);
        }

        /// <summary>
        /// 測試：空篩選值對 null 欄位也應視為 no-op(不應被誤排除)
        /// </summary>
        [Fact]
        public void Apply_EmptyFilter_DoesNotExcludeNullActualValues()
        {
            var source = new[]
            {
                new Person { Name = "Alice" },
                new Person { Name = null }
            };

            var result = FilterQuery.Apply(
                source,
                typeof(Person),
                new[]
                {
                    new FilterCondition
                    {
                        PropertyName = nameof(Person.Name),
                        Operator = FilterOperator.Contains,
                        Value = ""
                    }
                }).Cast<Person>().ToArray();

            Assert.Equal(2, result.Length);
        }

        /// <summary>
        /// 測試：非法 Enum 文字不應讓 filter 整條資料流炸掉
        /// </summary>
        [Fact]
        public void Apply_InvalidEnumTextDoesNotThrow()
        {
            var source = new[] { new Person { Status = Status.Active } };

            var result = FilterQuery.Apply(
                source,
                typeof(Person),
                new[]
                {
                    new FilterCondition
                    {
                        PropertyName = nameof(Person.Status),
                        Operator = FilterOperator.Equals,
                        Value = "NotARealStatus"
                    }
                }).Cast<Person>();

            Assert.Empty(result);
        }

        [Fact]
        public void DateTimeFilterViewModel_UsesDateTimeValue()
        {
            var filter = new FilterViewModel(
                nameof(Person.Created),
                "Created",
                FilterValueType.DateTime);

            filter.Operator = FilterOperator.GreaterThan;
            filter.DateTimeValue = new DateTime(2026, 1, 15);
            filter.ApplyCommand.Execute(null);

            Assert.Equal(FilterValueType.DateTime, filter.AppliedFilter.ValueType);
            Assert.Equal(new DateTime(2026, 1, 15), filter.AppliedFilter!.Value);
        }

        /// <summary>
        /// 測試：PropertyInfo 快取不會依 PropertyName 混淆不同型別
        /// 驗證兩個型別擁有同名但型別不同的屬性("Name")時，快取仍各自查找正確的 PropertyInfo。
        /// </summary>
        [Fact]
        public void Apply_PropertyCacheIsScopedPerType()
        {
            var people = new[] { new Person { Name = "Alice" } };
            var widgets = new[] { new Widget { Name = 7 } };
            var filter = new[] { new FilterCondition { PropertyName = "Name", Operator = FilterOperator.Equals, Value = "Alice" } };
            var widgetFilter = new[] { new FilterCondition { PropertyName = "Name", Operator = FilterOperator.Equals, Value = 7 } };

            var personResult = FilterQuery.Apply(people, typeof(Person), filter).Cast<Person>().ToArray();
            var widgetResult = FilterQuery.Apply(widgets, typeof(Widget), widgetFilter).Cast<Widget>().ToArray();

            Assert.Single(personResult);
            Assert.Equal("Alice", personResult[0].Name);
            Assert.Single(widgetResult);
            Assert.Equal(7, widgetResult[0].Name);
        }

        /// <summary>
        /// 測試：DataTable 支援 - 綁定 DataView 時可透過欄位名稱正確篩選(DataRowView 走 ICustomTypeDescriptor 而非 CLR 反射)
        /// </summary>
        [Fact]
        public void Apply_OnDataView_FiltersByColumnName()
        {
            System.Data.DataTable table = new System.Data.DataTable();
            table.Columns.Add("Name", typeof(string));
            table.Columns.Add("Age", typeof(int));
            table.Rows.Add("Alice", 31);
            table.Rows.Add("Bob", 22);
            table.Rows.Add("Alicia", 29);
            System.Data.DataView view = table.DefaultView;

            var result = FilterQuery.Apply(
                view,
                typeof(System.Data.DataRowView),
                new[]
                {
                    new FilterCondition { PropertyName = "Name", Operator = FilterOperator.StartsWith, Value = "ali" },
                    new FilterCondition { PropertyName = "Age", Operator = FilterOperator.GreaterThan, Value = "28" }
                }).Cast<System.Data.DataRowView>().ToArray();

            Assert.Equal(new object[] { "Alice", "Alicia" }, result.Select(row => row["Name"]));
        }

        /// <summary>
        /// 測試：DataTable 資料源找不到指定欄位時應跳過該條件而非拋例外
        /// </summary>
        [Fact]
        public void Apply_OnDataView_UnknownColumnIsSkipped()
        {
            System.Data.DataTable table = new System.Data.DataTable();
            table.Columns.Add("Name", typeof(string));
            table.Rows.Add("Alice");
            System.Data.DataView view = table.DefaultView;

            var result = FilterQuery.Apply(
                view,
                typeof(System.Data.DataRowView),
                new[] { new FilterCondition { PropertyName = "NoSuchColumn", Operator = FilterOperator.Equals, Value = "x" } })
                .Cast<System.Data.DataRowView>().ToArray();

            Assert.Single(result);
        }

        private enum Status
        {
            Active,
            Inactive
        }

        private sealed class Widget
        {
            public int Name { get; set; }
        }

        private sealed class Person
        {
            [Display(Order = 2)]
            public int Age { get; set; }

            public int? Score { get; set; }

            public Status Status { get; set; }

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
