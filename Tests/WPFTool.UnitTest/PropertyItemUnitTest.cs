using System.ComponentModel;
using Xunit;

namespace SeanTool.CSharp.WPFTool.Test
{
    /// <summary>
    /// PropertyItem 單元測試集合
    /// 驗證屬性項的暫存、驗證、轉換、保存和取消流程
    /// </summary>
    public class PropertyItemUnitTest
    {
        /// <summary>
        /// 測試：有效型別轉換 - 轉換後的值在套用前保持暫存狀態
        /// 驗證暫存值機制：編輯不直接影響原始 Model，需要 ApplyChange 才生效
        /// </summary>
        [Fact(DisplayName = "有效型別轉換 - 轉換後的值在套用前保持暫存狀態")]
        public void Value_ValidConversion_IsStagedUntilApplied()
        {
            var model = new TestModel { Count = 3 };
            var item = CreatePropertyItem(model, nameof(TestModel.Count));

            item.Value = "42";

            Assert.Equal(42, item.Value);
            Assert.Equal(3, model.Count);

            item.ApplyChange();

            Assert.Equal(42, model.Count);
        }

        /// <summary>
        /// 測試：無效型別轉換 - 轉換失敗時報告錯誤且保持原值
        /// 驗證錯誤處理：轉換失敗不影響已暫存的值，Model 保持不變
        /// </summary>
        [Fact(DisplayName = "無效型別轉換 - 轉換失敗時報告錯誤且保持原值")]
        public void Value_InvalidConversion_ReportsErrorAndKeepsOriginalPendingValue()
        {
            var model = new TestModel { Count = 3 };
            var item = CreatePropertyItem(model, nameof(TestModel.Count));

            item.Value = "not a number";

            Assert.True(item.HasError);
            Assert.False(item.Validate());
            Assert.Equal(3, item.Value);
            Assert.Equal(3, model.Count);
        }

        /// <summary>
        /// 測試：Nullable 數字的空值 - 空字串轉換為 null
        /// 驗證 Nullable 支援：允許 Nullable 型別設置為 null
        /// </summary>
        [Fact(DisplayName = "Nullable 數字的空值 - 空字串轉換為 null")]
        public void Value_EmptyNullableNumber_StagesNull()
        {
            var model = new TestModel { OptionalCount = 3 };
            var item = CreatePropertyItem(model, nameof(TestModel.OptionalCount));

            item.Value = string.Empty;
            item.ApplyChange();

            Assert.False(item.HasError);
            Assert.Null(model.OptionalCount);
        }

        /// <summary>
        /// 測試：取消流程 - 丟棄暫存的修改
        /// 驗證 Reset 機制：取消操作將暫存值還原到原始值，Model 不受影響
        /// </summary>
        [Fact(DisplayName = "取消流程 - 丟棄暫存的修改")]
        public void Reset_DiscardsStagedChange()
        {
            var model = new TestModel { Count = 3 };
            var item = CreatePropertyItem(model, nameof(TestModel.Count));

            item.Value = "42";
            item.Reset();

            Assert.Equal(3, item.Value);
            Assert.Equal(3, model.Count);
        }

        /// <summary>
        /// 測試：唯讀屬性 - 可見但無法編輯
        /// 驗證唯讀保護：只讀屬性能顯示但 CanEdit 為 false，無法修改
        /// </summary>
        [Fact(DisplayName = "唯讀屬性 - 可見但無法編輯")]
        public void ReadOnlyProperty_IsVisibleButCannotBeEdited()
        {
            var model = new TestModel();
            var item = CreatePropertyItem(model, nameof(TestModel.ReadOnlyName));

            Assert.True(item.IsReadOnly);
            Assert.False(item.CanEdit);
            Assert.Equal("read only", item.Value);
        }

        /// <summary>
        /// 測試：巢狀物件的取消流程 - 父層取消能丟棄子物件修改
        /// 驗證巢狀物件隔離：編輯副本不影響原始物件，取消後恢復原狀
        /// </summary>
        [Fact(DisplayName = "巢狀物件的取消流程 - 父層取消能丟棄子物件修改")]
        public void NestedObject_SaveThenParentReset_DiscardsNestedChange()
        {
            var model = new ParentModel { Child = new ChildModel { Name = "original" } };
            var item = CreatePropertyItem(model, nameof(ParentModel.Child));
            var editableCopy = (ChildModel)item.CreateEditableCopy();

            editableCopy.Name = "edited";
            item.Value = editableCopy;
            item.Reset();

            Assert.Equal("original", model.Child.Name);
            Assert.Same(model.Child, item.Value);
        }

        /// <summary>
        /// 測試：巢狀物件的編輯副本 - CreateEditableCopy 產生的是獨立複本，非原始物件參考
        /// </summary>
        [Fact(DisplayName = "巢狀物件：CreateEditableCopy 產生獨立複本")]
        public void NestedObject_CreateEditableCopy_ClonesMutableChild()
        {
            var model = new ParentModel { Child = new ChildModel { Name = "original" } };
            var item = CreatePropertyItem(model, nameof(ParentModel.Child));

            var editableCopy = (ChildModel)item.CreateEditableCopy();
            editableCopy.Name = "edited";

            Assert.NotSame(model.Child, editableCopy);
            Assert.Equal("original", model.Child.Name);
        }

        /// <summary>
        /// 測試：Enum 型別 - 轉換與選項清單
        /// 驗證 Enum 支援：自動產生所有選項、字串轉換、Enum 選擇
        /// </summary>
        [Fact(DisplayName = "Enum 型別 - 轉換與選項清單")]
        public void Enum_ConversionAndOptions_AreCorrect()
        {
            var model = new TestModel();
            var item = CreatePropertyItem(model, nameof(TestModel.Status));

            // 驗證 InputType 正確識別為 Enum
            Assert.Equal(EditorInputType.Enum, item.InputType);

            // 驗證 Options 包含所有 Enum 值名稱
            Assert.NotNull(item.Options);
            Assert.True(item.Options.Count > 0);
            Assert.Contains("Active", item.Options);
            Assert.Contains("Inactive", item.Options);
            Assert.Contains("Pending", item.Options);

            // 驗證 Value getter 返回字串（UI 綁定用）
            model.Status = Status.Inactive;
            item = CreatePropertyItem(model, nameof(TestModel.Status));
            Assert.Equal("Inactive", item.Value);

            // 驗證 Value setter 能從字串轉換回 Enum
            item.Value = "Pending";
            Assert.False(item.HasError);
            
            // 暫存值已改變，但 Model 未改變（需要 ApplyChange）
            Assert.Equal(Status.Inactive, model.Status);
            
            item.ApplyChange();
            Assert.Equal(Status.Pending, model.Status);
        }

        /// <summary>
        /// 測試：DateTime 型別 - 日期與時間部分獨立編輯
        /// 驗證日期時間支援：分離編輯日期/時間，互不影響
        /// </summary>
        [Fact(DisplayName = "DateTime 型別 - 日期與時間部分獨立編輯")]
        public void DateTime_DatePartAndTimePart_AreIndependent()
        {
            var original = new DateTime(2026, 8, 19, 14, 30, 45);
            var model = new TestModel { CreatedDate = original };
            var item = CreatePropertyItem(model, nameof(TestModel.CreatedDate));

            // 驗證 InputType 識別為 DateTime
            Assert.Equal(EditorInputType.DateTime, item.InputType);

            // 驗證 DatePart 只返回日期部分
            Assert.Equal(original.Date, item.DatePart);

            // 驗證 TimePart 返回時間字串
            Assert.Equal("14:30:45", item.TimePart);

            // 修改日期，時間保留
            item.DatePart = new DateTime(2026, 12, 25);
            Assert.Equal(new DateTime(2026, 12, 25, 14, 30, 45), item.Value);

            // 修改時間，日期保留
            item.TimePart = "09:15:00";
            Assert.Equal(new DateTime(2026, 12, 25, 9, 15, 0), item.Value);
        }

        /// <summary>
        /// 測試：TimePart 設定無效時間字串 - 回報錯誤且保留原本的 DateTime 值
        /// </summary>
        [Fact(DisplayName = "DateTime：TimePart 無效時間字串回報錯誤並保留原值")]
        public void DateTime_InvalidTimePartReportsError()
        {
            var model = new TestModel { CreatedDate = new DateTime(2026, 8, 19, 14, 30, 45) };
            var item = CreatePropertyItem(model, nameof(TestModel.CreatedDate));

            item.TimePart = "not-a-time";

            Assert.True(item.HasError);
            Assert.Equal(new DateTime(2026, 8, 19, 14, 30, 45), item.Value);
        }

        /// <summary>
        /// 測試：數字溢位 - 超出型別範圍時報告錯誤
        /// 驗證數字驗證：無效值設置失敗，原值不變，錯誤提示清晰
        /// </summary>
        [Fact(DisplayName = "數字溢位 - 超出型別範圍時報告錯誤")]
        public void Number_Overflow_IsReported()
        {
            var model = new TestModel { Count = 10 };
            var item = CreatePropertyItem(model, nameof(TestModel.Count));

            // 嘗試設置超出 int 範圍的值
            item.Value = "999999999999999999";

            Assert.True(item.HasError);
            Assert.NotEmpty(item.ErrorMessage);
            Assert.Equal(10, model.Count); // 原始值未改變
        }

        /// <summary>
        /// 測試：數字格式無效 - 非數字字串轉換失敗
        /// 驗證數字格式驗證：非數字值被拒絕，Model 保持原值
        /// </summary>
        [Fact(DisplayName = "數字格式無效 - 非數字字串轉換失敗")]
        public void Number_InvalidFormat_IsReported()
        {
            var model = new TestModel { Price = 99.99 };
            var item = CreatePropertyItem(model, nameof(TestModel.Price));

            Assert.Equal(EditorInputType.Number, item.InputType);

            item.Value = "not-a-number";

            Assert.True(item.HasError);
            Assert.Equal(99.99, model.Price);
        }

        /// <summary>
        /// 測試：null 巢狀物件 - 無法建立編輯副本
        /// 驗證 null 安全性：null 物件無法複製，提示明確
        /// </summary>
        [Fact(DisplayName = "null 巢狀物件 - 無法建立編輯副本")]
        public void NestedObject_WhenNull_CannotCreateCopy()
        {
            var model = new TestModel { NullableChild = null };
            var item = CreatePropertyItem(model, nameof(TestModel.NullableChild));

            var ex = Assert.Throws<InvalidOperationException>(() => item.CreateEditableCopy());
            Assert.Contains("沒有可編輯的物件", ex.Message);
        }

        /// <summary>
        /// 測試：DisplayNameAttribute - 使用自訂顯示名稱
        /// 驗證元數據支援：帶 DisplayName 特性的屬性使用自訂名稱
        /// </summary>
        [Fact(DisplayName = "DisplayNameAttribute - 使用自訂顯示名稱")]
        public void Property_WithDisplayNameAttribute_UsesCustomName()
        {
            var model = new TestModel();
            var item = CreatePropertyItem(model, nameof(TestModel.CustomName));

            Assert.Equal("自訂名稱", item.DisplayName);
        }

        /// <summary>
        /// 測試：無 DisplayNameAttribute - 使用屬性名稱作為顯示名
        /// 驗證預設行為：無自訂名稱時，使用程式碼中定義的屬性名
        /// </summary>
        [Fact(DisplayName = "無 DisplayNameAttribute - 使用屬性名稱作為顯示名")]
        public void Property_WithoutDisplayNameAttribute_UsesPropertyName()
        {
            var model = new TestModel();
            var item = CreatePropertyItem(model, nameof(TestModel.Count));

            Assert.Equal("Count", item.DisplayName);
        }

        /// <summary>
        /// 測試：編輯模式控制 - IsEditing 屬性影響 CanEdit
        /// 驗證編輯/檢視切換：IsEditing 為 false 時，CanEdit 也為 false
        /// </summary>
        [Fact(DisplayName = "編輯模式控制 - IsEditing 屬性影響 CanEdit")]
        public void IsEditing_ControlsCanEdit()
        {
            var model = new TestModel { Count = 5 };
            var item = CreatePropertyItem(model, nameof(TestModel.Count));

            Assert.True(item.IsEditing);
            Assert.True(item.CanEdit);

            item.IsEditing = false;

            Assert.False(item.IsEditing);
            Assert.False(item.CanEdit);
        }

        /// <summary>
        /// 測試：唯讀屬性保護 - ApplyChange 不會覆寫唯讀屬性
        /// 驗證保存保護：唯讀屬性即使有暫存值也不會被 ApplyChange 寫回 Model
        /// </summary>
        [Fact(DisplayName = "唯讀屬性保護 - ApplyChange 不會覆寫唯讀屬性")]
        public void IsReadOnly_Property_CannotBeEdited()
        {
            var model = new TestModel();
            var item = CreatePropertyItem(model, nameof(TestModel.ReadOnlyName));

            Assert.True(item.IsReadOnly);
            Assert.False(item.CanEdit);

            // 唯讀屬性無法透過 ApplyChange 寫回 Model
            // 即使 Value 被設置到暫存值，ApplyChange 也會跳過它
            item.Value = "new value";  // 設置到暫存值
            item.ApplyChange();  // 不會寫回 Model（因為 IsReadOnly=true）
            
            Assert.Equal("read only", model.ReadOnlyName);  // Model 未改變
        }

        /// <summary>
        /// 測試：ReadOnlyAttribute - 標記 [ReadOnly(true)] 的屬性即使有公開 Setter，仍應視為唯讀且無法編輯
        /// 驗證 ObjectList/單一物件情境下 ReadOnlyAttribute 能正確保護欄位不被 ApplyChange 寫回
        /// </summary>
        [Fact(DisplayName = "ReadOnlyAttribute - 標記 [ReadOnly(true)] 的屬性即使有公開 Setter，仍應視為唯讀且無法編輯")]
        public void ReadOnlyAttribute_Property_WithSetter_IsTreatedAsReadOnly()
        {
            var model = new TestModel { LockedName = "original" };
            var item = CreatePropertyItem(model, nameof(TestModel.LockedName));

            Assert.True(item.IsReadOnly);
            Assert.False(item.CanEdit);

            item.Value = "changed";
            item.ApplyChange();

            Assert.Equal("original", model.LockedName); // 有 Setter 但仍不會被寫回
        }

        /// <summary>
        /// 測試：EditorPathAttribute(PathType.File) - InputType 判定為 FilePath，並帶入自訂 FileFilter
        /// </summary>
        [Fact(DisplayName = "EditorPathAttribute(File)：InputType 為 FilePath 並帶入自訂 FileFilter")]
        public void EditorPathAttribute_File_SetsFilePathInputTypeAndFilter()
        {
            var model = new PathTestModel();
            var item = CreatePropertyItem(model, nameof(PathTestModel.FilePath));

            Assert.Equal(EditorInputType.FilePath, item.InputType);
            Assert.Equal("Text files (*.txt)|*.txt", item.FileFilter);
        }

        /// <summary>
        /// 測試：EditorPathAttribute(PathType.Folder) - InputType 判定為 FolderPath
        /// </summary>
        [Fact(DisplayName = "EditorPathAttribute(Folder)：InputType 為 FolderPath")]
        public void EditorPathAttribute_Folder_SetsFolderPathInputType()
        {
            var model = new PathTestModel();
            var item = CreatePropertyItem(model, nameof(PathTestModel.FolderPath));

            Assert.Equal(EditorInputType.FolderPath, item.InputType);
        }

        /// <summary>
        /// 測試：EditorTextAreaAttribute - 標記的字串屬性判定為多行文字框，並帶入 MinLines
        /// </summary>
        [Fact(DisplayName = "EditorTextAreaAttribute：判定為多行文字框並帶入 MinLines")]
        public void EditorTextAreaAttribute_MarksPropertyAsTextAreaWithMinLines()
        {
            var model = new PathTestModel();
            var item = CreatePropertyItem(model, nameof(PathTestModel.Notes));

            Assert.True(item.IsTextArea);
            Assert.Equal(8, item.TextAreaMinLines);
        }

        /// <summary>
        /// 測試：一般文字屬性(無 EditorTextAreaAttribute) - IsTextArea 維持預設 false
        /// </summary>
        [Fact(DisplayName = "無 EditorTextAreaAttribute：IsTextArea 維持預設 false")]
        public void PlainTextProperty_WithoutTextAreaAttribute_IsNotTextArea()
        {
            var model = new TestModel();
            var item = CreatePropertyItem(model, nameof(TestModel.CustomName));

            Assert.False(item.IsTextArea);
        }

        /// <summary>
        /// 測試：巢狀複雜物件屬性 - DetermineInputType 判定為 Object
        /// </summary>
        [Fact(DisplayName = "巢狀複雜物件屬性：InputType 判定為 Object")]
        public void ComplexObjectProperty_IsDeterminedAsObjectInputType()
        {
            var model = new ParentModel { Child = new ChildModel { Name = "original" } };
            var item = CreatePropertyItem(model, nameof(ParentModel.Child));

            Assert.Equal(EditorInputType.Object, item.InputType);
        }

        private static PropertyItem CreatePropertyItem(object model, string propertyName)
        {
            var field = FieldAnalyzer.Analyze(model).Single(f => f.Name == propertyName);
            return new PropertyItem(model, field);
        }

        private sealed class TestModel
        {
            public int Count { get; set; }

            public int? OptionalCount { get; set; }

            [DisplayName("Read only")]
            public string ReadOnlyName => "read only";

            [ReadOnly(true)]
            public string LockedName { get; set; } = "locked";

            public Status Status { get; set; } = Status.Active;

            public DateTime CreatedDate { get; set; } = DateTime.Now;

            [DisplayName("自訂名稱")]
            public string CustomName { get; set; } = "default";

            public ChildModel? NullableChild { get; set; }

            public double Price { get; set; } = 99.99;
        }

        private sealed class EnumTestModel
        {
            public Status Status { get; set; } = Status.Active;
        }

        private sealed class PathTestModel
        {
            [EditorPath(PathType.File, "Text files (*.txt)|*.txt")]
            public string FilePath { get; set; } = string.Empty;

            [EditorPath(PathType.Folder)]
            public string FolderPath { get; set; } = string.Empty;

            [EditorTextArea(8)]
            public string Notes { get; set; } = string.Empty;
        }

        private enum Status
        {
            Active,
            Inactive,
            Pending
        }

        private sealed class ParentModel
        {
            public ChildModel Child { get; set; } = new ChildModel();
        }

        private sealed class ChildModel
        {
            public string Name { get; set; } = string.Empty;
        }
    }
}