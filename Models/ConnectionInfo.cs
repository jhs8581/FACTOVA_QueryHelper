using System.ComponentModel;

namespace FACTOVA_QueryHelper.Models
{
    /// <summary>
    /// 데이터베이스 접속 정보 모델
    /// </summary>
    public class ConnectionInfo : INotifyPropertyChanged
    {
        private int _id;
        private string _name = string.Empty;
        private string _tns = string.Empty;
        private string _host = string.Empty;
        private string _port = string.Empty;
        private string _service = string.Empty;
        private string _userId = string.Empty;
        private string _password = string.Empty;
        private string _sqlQuery = string.Empty;
        private bool _isActive;
        private bool _isFavorite;

        public event PropertyChangedEventHandler? PropertyChanged;

        public int Id
        {
            get => _id;
            set
            {
                _id = value;
                OnPropertyChanged(nameof(Id));
            }
        }

        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                OnPropertyChanged(nameof(Name));
            }
        }

        public string TNS
        {
            get => _tns;
            set
            {
                _tns = value;
                OnPropertyChanged(nameof(TNS));
            }
        }

        public string Host
        {
            get => _host;
            set
            {
                _host = value;
                OnPropertyChanged(nameof(Host));
            }
        }

        public string Port
        {
            get => _port;
            set
            {
                _port = value;
                OnPropertyChanged(nameof(Port));
            }
        }

        public string Service
        {
            get => _service;
            set
            {
                _service = value;
                OnPropertyChanged(nameof(Service));
            }
        }

        public string UserId
        {
            get => _userId;
            set
            {
                _userId = value;
                OnPropertyChanged(nameof(UserId));
            }
        }

        public string Password
        {
            get => _password;
            set
            {
                _password = value;
                OnPropertyChanged(nameof(Password));
            }
        }

        public string SQLQuery
        {
            get => _sqlQuery;
            set
            {
                _sqlQuery = value;
                OnPropertyChanged(nameof(SQLQuery));
            }
        }

        public bool IsActive
        {
            get => _isActive;
            set
            {
                _isActive = value;
                OnPropertyChanged(nameof(IsActive));
            }
        }

        public bool IsFavorite
        {
            get => _isFavorite;
            set
            {
                _isFavorite = value;
                OnPropertyChanged(nameof(IsFavorite));
            }
        }

        /// <summary>
        /// ComboBox에 표시할 이름 (Name - UserId@TNS)
        /// </summary>
        public string DisplayName => $"{Name} - {UserId}@{TNS}";

        /// <summary>
        /// TNS 이름 (TNS 속성의 Alias)
        /// </summary>
        public string TnsName => TNS;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            
            // DisplayName이 의존하는 속성이 변경되면 DisplayName도 갱신
            if (propertyName == nameof(Name) || propertyName == nameof(UserId) || propertyName == nameof(TNS))
            {
                OnPropertyChanged(nameof(DisplayName));
                OnPropertyChanged(nameof(TnsName));
            }
        }
    }
}
