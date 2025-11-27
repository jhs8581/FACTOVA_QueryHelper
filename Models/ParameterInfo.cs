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

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
