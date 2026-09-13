using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Xunit;

namespace SeanTool.CSharp.WPFTool.Test
{
    /// <summary>
    /// DateTimePicker 單元測試集合
    /// 驗證 TryParseDate 多格式解析、以及實際 UserControl 的 SelectedDateTime 雙向同步、唯讀模式、
    /// 日期驗證自動修正、無效時間還原預設值等 code-behind 行為(TryParseTime 測試位於 DataTimePickerUnitTest.cs)
    /// </summary>
    public class DateTimePickerBehaviorUnitTest
    {
        /// <summary>
        /// 測試：TryParseDate - 支援內建的多種日期格式(yyyyMMdd/yyyy/M/d/yyyy-MM-dd/yyyy.MM.dd 等)
        /// </summary>
        [Theory(DisplayName = "TryParseDate：支援內建的多種日期格式")]
        [InlineData("20260913")]
        [InlineData("2026/9/13")]
        [InlineData("2026/09/13")]
        [InlineData("2026-9-13")]
        [InlineData("2026-09-13")]
        [InlineData("2026.9.13")]
        [InlineData("2026.09.13")]
        [InlineData("  2026-09-13  ")]
        public void TryParseDate_AcceptsBuiltInFormats(string text)
        {
            Assert.True(DateTimePicker.TryParseDate(text, out DateTime date));
            Assert.Equal(new DateTime(2026, 9, 13), date);
        }

        /// <summary>
        /// 測試：TryParseDate - 內建格式解析失敗時，回退用 CurrentCulture/InvariantCulture 解析(文化 fallback)
        /// </summary>
        [Fact(DisplayName = "TryParseDate：內建格式失敗時回退到文化解析")]
        public void TryParseDate_FallsBackToCultureParsing()
        {
            Assert.True(DateTimePicker.TryParseDate("September 13, 2026", out DateTime date));
            Assert.Equal(new DateTime(2026, 9, 13), date);
        }

        /// <summary>
        /// 測試：TryParseDate - null/空白/無法解析的文字一律回傳 false 且不拋例外
        /// </summary>
        [Theory(DisplayName = "TryParseDate：null/空白/無效文字回傳 false")]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("not-a-date")]
        public void TryParseDate_RejectsInvalidText(string? text)
        {
            Assert.False(DateTimePicker.TryParseDate(text, out _));
        }

        /// <summary>
        /// 測試：SelectedDateTime 對外設值 - 內部 DatePicker/TimeTextBox 同步顯示對應的日期與時間
        /// </summary>
        [Fact(DisplayName = "SelectedDateTime：設值時同步內部 DatePicker 與 TimeTextBox")]
        public void SelectedDateTime_Set_UpdatesInternalControls()
        {
            RunOnStaThread(() =>
            {
                var picker = new DateTimePicker();
                Layout(picker);

                picker.SelectedDateTime = new DateTime(2026, 9, 13, 8, 30, 0);

                DatePicker datePicker = GetDatePickerControl(picker);
                TextBox timeTextBox = GetTimeTextBox(picker);

                Assert.Equal(new DateTime(2026, 9, 13), datePicker.SelectedDate);
                Assert.Equal("08:30:00", timeTextBox.Text);
            });
        }

        /// <summary>
        /// 測試：SelectedDateTime = null - 內部子控制項回復為預設(未選日期、時間 00:00:00)
        /// </summary>
        [Fact(DisplayName = "SelectedDateTime：設為 null 時內部子控制項回復預設值")]
        public void SelectedDateTime_SetNull_RestoresDefaultParts()
        {
            RunOnStaThread(() =>
            {
                var picker = new DateTimePicker { SelectedDateTime = new DateTime(2026, 9, 13, 8, 30, 0) };
                Layout(picker);

                picker.SelectedDateTime = null;

                DatePicker datePicker = GetDatePickerControl(picker);
                TextBox timeTextBox = GetTimeTextBox(picker);
                Assert.Null(datePicker.SelectedDate);
                Assert.Equal("00:00:00", timeTextBox.Text);
            });
        }

        /// <summary>
        /// 測試：內部 DatePicker/TimeTextBox 異動 - 合併日期時間後回寫 SelectedDateTime(子控制項回推)
        /// </summary>
        [Fact(DisplayName = "內部控制項異動：合併後回寫 SelectedDateTime")]
        public void InternalControlsChanged_UpdatesSelectedDateTime()
        {
            RunOnStaThread(() =>
            {
                var picker = new DateTimePicker();
                Layout(picker);
                DatePicker datePicker = GetDatePickerControl(picker);
                TextBox timeTextBox = GetTimeTextBox(picker);

                datePicker.SelectedDate = new DateTime(2026, 9, 13);
                timeTextBox.Text = "09:15:30";

                Assert.Equal(new DateTime(2026, 9, 13, 9, 15, 30), picker.SelectedDateTime);
            });
        }

        /// <summary>
        /// 測試：清空內部 DatePicker 的選取日期 - SelectedDateTime 回寫為 null(不強制補回時間)
        /// </summary>
        [Fact(DisplayName = "內部控制項異動：清空日期後 SelectedDateTime 回寫為 null")]
        public void ClearingInternalDate_SetsSelectedDateTimeToNull()
        {
            RunOnStaThread(() =>
            {
                var picker = new DateTimePicker { SelectedDateTime = new DateTime(2026, 9, 13, 8, 0, 0) };
                Layout(picker);
                DatePicker datePicker = GetDatePickerControl(picker);

                datePicker.SelectedDate = null;

                Assert.Null(picker.SelectedDateTime);
            });
        }

        /// <summary>
        /// 測試：IsReadOnly - 停用內部 DatePicker、TimeTextBox 設為唯讀，且子控制項異動不再回寫 SelectedDateTime
        /// </summary>
        [Fact(DisplayName = "IsReadOnly：停用子控制項並阻止回寫 SelectedDateTime")]
        public void IsReadOnly_DisablesInternalControlsAndBlocksWriteBack()
        {
            RunOnStaThread(() =>
            {
                var picker = new DateTimePicker { IsReadOnly = true };
                Layout(picker);
                DatePicker datePicker = GetDatePickerControl(picker);
                TextBox timeTextBox = GetTimeTextBox(picker);

                Assert.False(datePicker.IsEnabled);
                Assert.True(timeTextBox.IsReadOnly);

                datePicker.SelectedDate = new DateTime(2026, 9, 13);
                timeTextBox.Text = "09:15:30";

                Assert.Null(picker.SelectedDateTime);
            });
        }

        /// <summary>
        /// 測試：TimeTextBox 失焦時內容不是合法時間 - 自動還原為預設值 00:00:00(RestoreDefaultTime)
        /// </summary>
        [Fact(DisplayName = "TimeTextBox：失焦時無效時間自動還原為 00:00:00")]
        public void TimeTextBox_InvalidTextOnLostFocus_RestoresDefaultTime()
        {
            RunOnStaThread(() =>
            {
                var picker = new DateTimePicker { SelectedDateTime = new DateTime(2026, 9, 13, 8, 0, 0) };
                Layout(picker);
                TextBox timeTextBox = GetTimeTextBox(picker);

                timeTextBox.Text = "not-a-time";
                timeTextBox.RaiseEvent(new RoutedEventArgs(UIElement.LostFocusEvent));

                Assert.Equal("00:00:00", timeTextBox.Text);
                Assert.Equal(new DateTime(2026, 9, 13, 0, 0, 0), picker.SelectedDateTime);
            });
        }

        /// <summary>
        /// 測試：使用者於 DatePicker 手動輸入不符內建格式的日期字串時，透過 DateValidationError 事件以 TryParseDate 補救解析
        /// </summary>
        [Fact(DisplayName = "DateValidationError：以 TryParseDate 補救解析手動輸入的日期")]
        public void DateValidationError_RecoversUsingTryParseDate()
        {
            RunOnStaThread(() =>
            {
                var picker = new DateTimePicker();
                Layout(picker);
                DatePicker datePicker = GetDatePickerControl(picker);

                // DateValidationError 是一般 CLR 事件(非 RoutedEvent)，無法從外部直接引發，
                // 改用反射直接呼叫已掛勾在 DatePickerControl 上的 private static 處理函式。
                var handler = typeof(DateTimePicker).GetMethod(
                    "DatePickerDateValidationError",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;
                var args = new DatePickerDateValidationErrorEventArgs(new FormatException("bad date"), "2026.09.13");
                handler.Invoke(null, new object?[] { datePicker, args });

                Assert.False(args.ThrowException);
                Assert.Equal(new DateTime(2026, 9, 13), datePicker.SelectedDate);
            });
        }

        private static void Layout(FrameworkElement element)
        {
            element.Measure(new Size(400, 100));
            element.Arrange(new Rect(0, 0, 400, 100));
            element.UpdateLayout();
        }

        // 直接以 x:Name 取得子控制項，避免 FindVisualChild 誤抓到 DatePicker 樣板內部自己的 TextBox。
        private static TextBox GetTimeTextBox(DateTimePicker picker) => (TextBox)picker.FindName("TimeTextBox")!;

        private static DatePicker GetDatePickerControl(DateTimePicker picker) => (DatePicker)picker.FindName("DatePickerControl")!;

        private static T? FindVisualChild<T>(DependencyObject parent)
            where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);
                if (child is T typed)
                {
                    return typed;
                }

                T? found = FindVisualChild<T>(child);
                if (found is not null)
                {
                    return found;
                }
            }

            return null;
        }

        private static void RunOnStaThread(Action action)
        {
            Exception? exception = null;
            using var completed = new ManualResetEventSlim();
            var thread = new Thread(() =>
            {
                try
                {
                    action();
                }
                catch (Exception caught)
                {
                    exception = caught;
                }
                finally
                {
                    completed.Set();
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            completed.Wait();
            thread.Join();
            if (exception is not null)
            {
                throw new Xunit.Sdk.XunitException(exception.ToString());
            }
        }
    }
}
