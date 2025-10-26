using System.ComponentModel;

namespace FACTOVA_Palletizing_Analysis
{
    /// <summary>
    /// üũ�ڽ��� �ִ� ComboBox ������
    /// </summary>
    public class CheckableComboBoxItem : INotifyPropertyChanged
    {
        private bool _isChecked;
        private string _text = string.Empty;

        public string Text
        {
            get => _text;
            set
            {
                _text = value;
                OnPropertyChanged(nameof(Text));
            }
        }

        public bool IsChecked
        {
            get => _isChecked;
            set
            {
                _isChecked = value;
                OnPropertyChanged(nameof(IsChecked));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
