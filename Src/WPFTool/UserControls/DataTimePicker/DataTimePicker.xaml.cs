using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace SeanTool.CSharp.WPF
{
    public partial class DataTimePicker : UserControl
    {
        private bool _updatingParts;

        public DataTimePicker()
        {
            InitializeComponent();
            TimeTextBox.Text = "00:00:00";
            UpdateParts(SelectedDateTime);
        }

        public static readonly DependencyProperty SelectedDateTimeProperty =
            DependencyProperty.Register(
                nameof(SelectedDateTime),
                typeof(DateTime?),
                typeof(DataTimePicker),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedDateTimeChanged));

        public static readonly DependencyProperty IsReadOnlyProperty =
            DependencyProperty.Register(
                nameof(IsReadOnly),
                typeof(bool),
                typeof(DataTimePicker),
                new PropertyMetadata(false, OnIsReadOnlyChanged));

        public static readonly DependencyProperty ErrorMessageProperty =
            DependencyProperty.Register(nameof(ErrorMessage), typeof(string), typeof(DataTimePicker));

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

        public string? ErrorMessage
        {
            get => (string?)GetValue(ErrorMessageProperty);
            private set => SetValue(ErrorMessageProperty, value);
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

        private static void OnSelectedDateTimeChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
        {
            if (dependencyObject is DataTimePicker picker)
            {
                picker.UpdateParts((DateTime?)args.NewValue);
            }
        }

        private static void OnIsReadOnlyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
        {
            if (dependencyObject is DataTimePicker picker)
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
                ErrorMessage = null;
                SetCurrentValue(SelectedDateTimeProperty, null);
                return;
            }

            if (!TryParseTime(TimeTextBox.Text, out TimeSpan time))
            {
                ErrorMessage = "時間格式錯誤，請輸入 HH:mm 或 HH:mm:ss。";
                return;
            }

            ErrorMessage = null;
            SetCurrentValue(SelectedDateTimeProperty, date.Date.Add(time));
        }
    }
}
