using Xunit;

namespace SeanTool.CSharp.WPFTool.Test
{
    /// <summary>
    /// CheckTypeConverter 單元測試集合
    /// 驗證 VirtualTreeView 三態勾選(CheckType)與 CheckBox 的 bool? 之間的轉換邏輯
    /// </summary>
    public class CheckTypeConverterUnitTest
    {
        private readonly CheckTypeConverter _converter = new();

        /// <summary>
        /// 測試：Convert - CheckType.All 對應 CheckBox 的 true(已勾選)
        /// </summary>
        [Fact(DisplayName = "Convert：CheckType.All 轉換為 true")]
        public void Convert_All_ReturnsTrue()
        {
            Assert.Equal(true, _converter.Convert(CheckType.All, typeof(bool?), null, null!));
        }

        /// <summary>
        /// 測試：Convert - CheckType.None 對應 CheckBox 的 false(未勾選)
        /// </summary>
        [Fact(DisplayName = "Convert：CheckType.None 轉換為 false")]
        public void Convert_None_ReturnsFalse()
        {
            Assert.Equal(false, _converter.Convert(CheckType.None, typeof(bool?), null, null!));
        }

        /// <summary>
        /// 測試：Convert - CheckType.HasValue(部分子節點勾選) 對應 CheckBox 的 null(indeterminate)
        /// </summary>
        [Fact(DisplayName = "Convert：CheckType.HasValue 轉換為 null(半選)")]
        public void Convert_HasValue_ReturnsNull()
        {
            Assert.Null(_converter.Convert(CheckType.HasValue, typeof(bool?), null, null!));
        }

        /// <summary>
        /// 測試：Convert - 非 CheckType 的值(型別不符/null)一律回退為 false，避免拋例外
        /// </summary>
        [Fact(DisplayName = "Convert：非 CheckType 值回退為 false")]
        public void Convert_NonCheckTypeValue_FallsBackToFalse()
        {
            Assert.Equal(false, _converter.Convert(null, typeof(bool?), null, null!));
            Assert.Equal(false, _converter.Convert("not-a-checktype", typeof(bool?), null, null!));
        }

        /// <summary>
        /// 測試：ConvertBack - CheckBox 的 true 對應 CheckType.All
        /// </summary>
        [Fact(DisplayName = "ConvertBack：true 轉換為 CheckType.All")]
        public void ConvertBack_True_ReturnsAll()
        {
            Assert.Equal(CheckType.All, _converter.ConvertBack(true, typeof(CheckType), null, null!));
        }

        /// <summary>
        /// 測試：ConvertBack - CheckBox 的 false 或 null(非 true)一律對應 CheckType.None
        /// </summary>
        [Fact(DisplayName = "ConvertBack：false 或 null 轉換為 CheckType.None")]
        public void ConvertBack_FalseOrNull_ReturnsNone()
        {
            Assert.Equal(CheckType.None, _converter.ConvertBack(false, typeof(CheckType), null, null!));
            Assert.Equal(CheckType.None, _converter.ConvertBack(null, typeof(CheckType), null, null!));
        }
    }
}
