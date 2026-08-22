using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace SeanTool.CSharp.WPFTool.UserControls.DateTimePicker
{
    public partial class DateTimePicker : UserControl
    {
        private bool _updatingParts;

        private static readonly string[] DateFormats =
        {
            "yyyyMMdd",
            "yyyy/M/d",
            "yyyy/MM/dd",
            "yyyy-M-d",
            "yyyy-MM-dd",
            "yyyy.M.d",
            "yyyy.MM.dd",
        };

        public DateTimePicker()
        {
            InitializeComponent();
            TimeTextBox.Text = "00:00:00";
            UpdateParts(SelectedDateTime);
            DatePickerControl.DateValidationError += DatePickerDateValidationError;
        }

        public static readonly DependencyProperty SelectedDateTimeProperty =
            DependencyProperty.Register(
                nameof(SelectedDateTime),
                typeof(DateTime?),
                typeof(DateTimePicker),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedDateTimeChanged));

        public static readonly DependencyProperty IsReadOnlyProperty =
            DependencyProperty.Register(
                nameof(IsReadOnly),
                typeof(bool),
                typeof(DateTimePicker),
                new PropertyMetadata(false, OnIsReadOnlyChanged));

        public DateTime? SelectedDateTime
        {
            get => (DateTime?)GetValue(SelectedDateTimeProperty);
            set => SetValue(SelectedDateTimeProperty, value);
        }

        public bool IsReadOnly
        {
            get => (bool)GetValue(IsReadOnlyProperty);
            set => SetValue(IsReadOnlyProperty, value);
        }

        public static bool TryParseTime(string? text, out TimeSpan time)
        {
            return TimeSpan.TryParseExact(
                       text,
                       new[] { "hh\\:mm", "hh\\:mm\\:ss" },
                       CultureInfo.InvariantCulture,
                       out time)
                   && time < TimeSpan.FromDays(1);
        }

        public static bool TryParseDate(string? text, out DateTime date)
        {
            if (!string.IsNullOrWhiteSpace(text))
            {
                text = text.Trim();
                if (DateTime.TryParseExact(text, DateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out date)
                    || DateTime.TryParse(text, CultureInfo.CurrentCulture, DateTimeStyles.None, out date)
                    || DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
                {
                    return true;
                }
            }

            date = default;
            return false;
        }

        private static void DatePickerDateValidationError(object? sender, DatePickerDateValidationErrorEventArgs args)
        {
            args.ThrowException = false;
            if (sender is DatePicker datePicker && TryParseDate(args.Text, out DateTime date))
            {
                datePicker.SelectedDate = date;
            }
        }

        private static void OnSelectedDateTimeChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
        {
            if (dependencyObject is DateTimePicker picker)
            {
                picker.UpdateParts((DateTime?)args.NewValue);
            }
        }

        private static void OnIsReadOnlyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
        {
            if (dependencyObject is DateTimePicker picker)
            {
                picker.DatePickerControl.IsEnabled = !(bool)args.NewValue;
                picker.TimeTextBox.IsReadOnly = (bool)args.NewValue;
            }
        }

        private void DatePickerChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateSelectedDateTime();
        }

        private void TimeTextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateSelectedDateTime();
        }

        private void TimeTextLostFocus(object sender, RoutedEventArgs e)
        {
            if (!TryParseTime(TimeTextBox.Text, out _))
            {
                RestoreDefaultTime();
            }
        }

        private void UpdateSelectedDateTime(DateTime? value)
        {
            _updatingParts = true;
            DatePickerControl.SelectedDate = value?.Date;
            TimeTextBox.Text = value?.ToString("HH:mm:ss") ?? "00:00:00";
            _updatingParts = false;
        }

        private void UpdateParts(DateTime? value)
        {
            if (!_updatingParts)
            {
                UpdateSelectedDateTime(value);
            }
        }

        private void UpdateSelectedDateTime()
        {
            if (_updatingParts || IsReadOnly)
            {
                return;
            }

            if (DatePickerControl.SelectedDate is not DateTime date)
            {
                SetCurrentValue(SelectedDateTimeProperty, null);
                return;
            }

            if (TryParseTime(TimeTextBox.Text, out TimeSpan time))
            {
                SetCurrentValue(SelectedDateTimeProperty, date.Date.Add(time));
            }
        }

        private void RestoreDefaultTime()
        {
            _updatingParts = true;
            TimeTextBox.Text = "00:00:00";
            _updatingParts = false;
            UpdateSelectedDateTime();
        }
    }
}
