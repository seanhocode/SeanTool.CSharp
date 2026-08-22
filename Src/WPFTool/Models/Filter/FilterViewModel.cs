using System.Windows.Input;
using SeanTool.CSharp.WPFTool.Common;
using SeanTool.CSharp.WPFTool.Enums.Filter;

namespace SeanTool.CSharp.WPFTool.Models.Filter
{
    /// <summary>
    /// 篩選條件 ViewModel
    /// </summary>
    public class FilterViewModel : ViewModelBase
    {
        #region 暫存欄位
        private FilterOperator _operator;
        private string _value = string.Empty;
        private DateTime? _dateTimeValue;
        private DateTime? _dateTimeValueTo;
        #endregion

        #region 已套用的欄位
        private DateTime? _appliedDateTimeValue;
        private DateTime? _appliedDateTimeValueTo;
        private FilterOperator _appliedOperator;
        private string _appliedValue = string.Empty;
        private IList<FilterOperator> _appliedCustomizeOperators;
        #endregion

        public FilterViewModel(string propertyName, string header, FilterValueType valueType = FilterValueType.Text, IList<FilterOperator> customizeOperator = null)
        {
            PropertyName = propertyName;
            Header = header;
            FilterDefinition = new FilterCondition { PropertyName = propertyName, ValueType = valueType, CustomizeOperators = customizeOperator };
            _operator = DefaultOperator;
            _appliedCustomizeOperators = customizeOperator;
            _appliedOperator = DefaultOperator;
            ApplyCommand = new RelayCommand<object>(_ => Apply());
            ClearCommand = new RelayCommand<object>(_ => Clear());
            ClearTempCommand = new RelayCommand<object>(_ => ClearTemp());
        }

        public string PropertyName { get; }

        /// <summary>
        /// 條件標題
        /// </summary>
        public string Header { get; }

        /// <summary>
        /// 篩選條件
        /// </summary>
        public FilterCondition FilterDefinition { get; }

        /// <summary>
        /// 清空篩選條件
        /// </summary>
        public ICommand ClearCommand { get; }

        public ICommand ClearTempCommand { get; }

        /// <summary>
        /// 套用篩選條件
        /// </summary>
        public ICommand ApplyCommand { get; }

        /// <summary>
        /// 可用的條件運算子清單
        /// </summary>
        public IReadOnlyList<FilterOperator> AvailableOperators => FilterDefinition.GetAvailableOperators();

        /// <summary>
        /// 預設選取的條件運算子
        /// </summary>
        private FilterOperator DefaultOperator => FilterDefinition.GetDefaultOperator();

        /// <summary>
        /// 條件運算子
        /// </summary>
        public FilterOperator Operator
        {
            get => _operator;
            set
            {
                if (_operator == value) return;
                _operator = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 條件值
        /// </summary>
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

        /// <summary>
        /// 條件值，日期時間型態
        /// </summary>
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

        /// <summary>
        /// 條件值，日期時間型態，僅在運算子為 Between 時使用
        /// </summary>
        public DateTime? DateTimeValueTo
        {
            get => _dateTimeValueTo;
            set
            {
                if (_dateTimeValueTo == value) return;
                _dateTimeValueTo = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 是否有套用的條件值
        /// </summary>
        public bool HasValue => _appliedOperator switch
        {
            FilterOperator.IsNull or FilterOperator.IsNotNull => true,
            FilterOperator.Between => _appliedDateTimeValue.HasValue || _appliedDateTimeValueTo.HasValue,
            _ => FilterDefinition.ValueType == FilterValueType.DateTime
                ? _appliedDateTimeValue.HasValue
                : !string.IsNullOrWhiteSpace(_appliedValue)
        };

        /// <summary>
        /// 套用的條件
        /// </summary>
        public FilterCondition? AppliedFilter => HasValue
            ? new FilterCondition
            {
                PropertyName = PropertyName,
                ValueType = FilterDefinition.ValueType,
                Operator = _appliedOperator,
                CustomizeOperators = _appliedCustomizeOperators,
                Value = _appliedOperator == FilterOperator.Between
                    ? new DateRange(_appliedDateTimeValue, _appliedDateTimeValueTo)
                    : (FilterDefinition.ValueType == FilterValueType.DateTime ? _appliedDateTimeValue : _appliedValue)
            }
            : null;

        /// <summary>
        /// 套用暫存的篩選條件
        /// </summary>
        private void Apply()
        {
            _appliedOperator = Operator;
            _appliedValue = Value;
            _appliedDateTimeValue = DateTimeValue;
            _appliedDateTimeValueTo = DateTimeValueTo;
            OnPropertyChanged(nameof(HasValue));
            OnPropertyChanged(nameof(AppliedFilter));
        }

        /// <summary>
        /// 清空篩選條件
        /// </summary>
        private void Clear()
        {
            Value = string.Empty;
            _appliedValue = string.Empty;
            _appliedDateTimeValue = null;
            _appliedDateTimeValueTo = null;
            DateTimeValue = null;
            DateTimeValueTo = null;
            _appliedOperator = DefaultOperator;
            Operator = DefaultOperator;
            OnPropertyChanged(nameof(HasValue));
            OnPropertyChanged(nameof(AppliedFilter));
        }

        private void ClearTemp()
        {
            Value = _appliedValue;
            DateTimeValue = _appliedDateTimeValue;
            DateTimeValueTo = _appliedDateTimeValueTo;
            Operator = _appliedOperator;
            OnPropertyChanged(nameof(Value));
            OnPropertyChanged(nameof(DateTimeValue));
            OnPropertyChanged(nameof(DateTimeValueTo));
            OnPropertyChanged(nameof(Operator));
        }
    }
}
