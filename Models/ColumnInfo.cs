using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace FACTOVA_QueryHelper.Models
{
    public class ColumnInfo : INotifyPropertyChanged
    {
        private bool _isJoin;
        private bool _isSelect;
        private bool _isWhere;

        public string ColumnName { get; set; } = string.Empty;
        public string Comments { get; set; } = string.Empty;
        public string DataType { get; set; } = string.Empty;
        public string Nullable { get; set; } = string.Empty;

        public bool IsJoin
        {
            get => _isJoin;
            set
            {
                _isJoin = value;
                OnPropertyChanged();
            }
        }

        public bool IsSelect
        {
            get => _isSelect;
            set
            {
                _isSelect = value;
                OnPropertyChanged();
            }
        }

        public bool IsWhere
        {
            get => _isWhere;
            set
            {
                _isWhere = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
