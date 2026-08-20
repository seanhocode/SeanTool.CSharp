using System.Windows.Input;

namespace SeanTool.CSharp.WPF
{
    public sealed class DynamicDataGridFilterViewModel : ViewModelBase
    {
        private DynamicDataGridFilterOperator _operator = DynamicDataGridFilterOperator.Contains;
        private string _value = string.Empty;
        private DateTime? _dateTimeValue;
        private DateTime? _appliedDateTimeValue;
        private DynamicDataGridFilterOperator _appliedOperator = DynamicDataGridFilterOperator.Contains;
        private string _appliedValue = string.Empty;

        public DynamicDataGridFilterViewModel(string propertyName, string header, Type? propertyType = null)
        {
            PropertyName = propertyName;
            Header = header;
            IsDateTime = propertyType == typeof(DateTime) || Nullable.GetUnderlyingType(propertyType ?? typeof(object)) == typeof(DateTime);
            ApplyCommand = new RelayCommand<object>(_ => Apply());
            ClearCommand = new RelayCommand<object>(_ => Clear());
        }

        public string PropertyName { get; }
        public string Header { get; }
        public bool IsDateTime { get; }
        public ICommand ClearCommand { get; }
        public ICommand ApplyCommand { get; }

        public DynamicDataGridFilterOperator Operator
        {
            get => _operator;
            set
            {
                if (_operator == value) return;
                _operator = value;
                OnPropertyChanged();
            }
        }

        public string Value
        {
            get => _value;
            set
            {
                if (_value == value) return;
                _value = value;
                OnPropertyChanged();
            }
        }

        public DateTime? DateTimeValue
        {
            get => _dateTimeValue;
            set
            {
                if (_dateTimeValue == value) return;
                _dateTimeValue = value;
                OnPropertyChanged();
            }
        }

        public bool HasValue => _appliedOperator is DynamicDataGridFilterOperator.IsNull or DynamicDataGridFilterOperator.IsNotNull ||
            (IsDateTime ? _appliedDateTimeValue.HasValue : !string.IsNullOrWhiteSpace(_appliedValue));

        public DynamicDataGridFilter? Filter => HasValue
            ? new DynamicDataGridFilter { PropertyName = PropertyName, Operator = _appliedOperator, Value = IsDateTime ? _appliedDateTimeValue : _appliedValue }
            : null;

        private void Apply()
        {
            _appliedOperator = Operator;
            _appliedValue = Value;
            _appliedDateTimeValue = DateTimeValue;
            OnPropertyChanged(nameof(DateTimeValue));
            OnPropertyChanged(nameof(HasValue));
            OnPropertyChanged(nameof(Filter));
        }

        private void Clear()
        {
            Value = string.Empty;
            _appliedValue = string.Empty;
            _appliedDateTimeValue = null;
            DateTimeValue = null;
            _appliedOperator = DynamicDataGridFilterOperator.Contains;
            Operator = DynamicDataGridFilterOperator.Contains;
            OnPropertyChanged(nameof(HasValue));
            OnPropertyChanged(nameof(Filter));
        }
    }
}
