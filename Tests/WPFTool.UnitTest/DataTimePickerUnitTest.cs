using Xunit;

namespace SeanTool.CSharp.WPFTool.Test
{
    /// <summary>
    /// DateTimePicker 的靜態時間字串解析工具方法(TryParseTime)單元測試集合
    /// 日期解析(TryParseDate)與 UserControl 本身的 code-behind 行為測試位於 DateTimePickerBehaviorUnitTest.cs
    /// </summary>
    public class DataTimePickerUnitTest
    {
        /// <summary>
        /// 測試：TryParseTime - 接受 HH:mm 與 HH:mm:ss 兩種內建格式
        /// </summary>
        [Theory]
        [InlineData("09:15", 9, 15, 0)]
        [InlineData("09:15:30", 9, 15, 30)]
        public void TryParseTime_AcceptsSupportedFormats(string text, int hours, int minutes, int seconds)
        {
            Assert.True(DateTimePicker.TryParseTime(text, out TimeSpan time));
            Assert.Equal(new TimeSpan(hours, minutes, seconds), time);
        }

        /// <summary>
        /// 測試：TryParseTime - null/空字串/超出範圍(時或分)/非法格式一律回傳 false
        /// </summary>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("25:00")]
        [InlineData("09:60")]
        [InlineData("not-a-time")]
        public void TryParseTime_RejectsInvalidFormats(string? text)
        {
            Assert.False(DateTimePicker.TryParseTime(text, out _));
        }

        /// <summary>
        /// 測試：TryParseTime - 剛好等於 24 小時的邊界值應被拒絕(時間需小於 24 小時)
        /// </summary>
        [Fact(DisplayName = "TryParseTime：拒絕滿 24 小時的邊界值")]
        public void TryParseTime_RejectsMoreThanOneDay()
        {
            Assert.False(DateTimePicker.TryParseTime("24:00", out _));
        }
    }
}