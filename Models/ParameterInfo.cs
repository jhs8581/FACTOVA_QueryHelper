using System.ComponentModel;

namespace FACTOVA_QueryHelper.Models
{
    /// <summary>
    /// 기준정보 파라미터 정보를 나타내는 클래스
    /// </summary>
    public class ParameterInfo : INotifyPropertyChanged
    {
    private int _id;
        private string _parameter = string.Empty;
        private string _description = string.Empty;
        private string _value = string.Empty;
        private bool _isHighlighted;
        private bool _noQuotes;

        public int Id
        {
            get => _id;
            set
            {
                if (_id != value)
                {
                    _id = value;
                    OnPropertyChanged(nameof(Id));
                }
            }
        }

        public string Parameter
        {
            get => _parameter;
            set
            {
                if (_parameter != value)
                {
                    _parameter = value;
                    OnPropertyChanged(nameof(Parameter));
                }
            }
        }

        public string Description
        {
            get => _description;
            set
            {
                if (_description != value)
                {
                    _description = value;
                    OnPropertyChanged(nameof(Description));
                }
            }
        }

        public string Value
        {
            get => _value;
            set
            {
                if (_value != value)
                {
                    _value = value;
                    OnPropertyChanged(nameof(Value));
                }
            }
        }

        /// <summary>
        /// 파라미터 확인 시 하이라이트 여부
        /// </summary>
        public bool IsHighlighted
        {
            get => _isHighlighted;
            set
            {
                if (_isHighlighted != value)
                {
                    _isHighlighted = value;
                    OnPropertyChanged(nameof(IsHighlighted));
                }
            }
        }

        /// <summary>
        /// 치환 시 따옴표 제외 여부 (true: 따옴표 없이 치환)
        /// </summary>
        public bool NoQuotes
        {
            get => _noQuotes;
            set
            {
                if (_noQuotes != value)
                {
                    _noQuotes = value;
                    OnPropertyChanged(nameof(NoQuotes));
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
