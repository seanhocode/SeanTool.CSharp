using SeanTool.CSharp.WPF;
using Xunit;

namespace SeanTool.CSharp.WPF.Test
{
    public class DataTimePickerUnitTest
    {
        [Theory]
        [InlineData("09:15", 9, 15, 0)]
        [InlineData("09:15:30", 9, 15, 30)]
        public void TryParseTime_AcceptsSupportedFormats(string text, int hours, int minutes, int seconds)
        {
            Assert.True(DataTimePicker.TryParseTime(text, out TimeSpan time));
            Assert.Equal(new TimeSpan(hours, minutes, seconds), time);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("25:00")]
        [InlineData("09:60")]
        [InlineData("not-a-time")]
        public void TryParseTime_RejectsInvalidFormats(string? text)
        {
            Assert.False(DataTimePicker.TryParseTime(text, out _));
        }

        [Fact]
        public void TryParseTime_RejectsMoreThanOneDay()
        {
            Assert.False(DataTimePicker.TryParseTime("24:00", out _));
        }
    }
}