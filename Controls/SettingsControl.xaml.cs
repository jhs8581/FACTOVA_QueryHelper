using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace FACTOVA_QueryHelper.Controls
{
    /// <summary>
    /// SettingsControl.xaml�� ���� ��ȣ �ۿ� ��
    /// </summary>
    public partial class SettingsControl : UserControl
    {
        private SharedDataContext? _sharedData;
        
        // TNS ��� ���� �̺�Ʈ
        public event EventHandler? TnsPathChanged;

        public SettingsControl()
        {
            InitializeComponent();
        }

        /// <summary>
        /// ���� ������ ���ؽ�Ʈ�� �ʱ�ȭ�մϴ�.
        /// </summary>
        public void Initialize(SharedDataContext sharedData)
        {
            _sharedData = sharedData;
            LoadSettings();
        }

        /// <summary>
        /// ������ �ε��մϴ�.
        /// </summary>
        private void LoadSettings()
        {
            if (_sharedData == null) return;

            // �⺻ ��� ǥ��
            DefaultPathTextBlock.Text = SettingsManager.GetDefaultTnsPath();
            TnsPathTextBox.Text = _sharedData.Settings.TnsPath;
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            if (_sharedData == null) return;

            string? filePath = FileDialogManager.OpenTnsFileDialog(
                Path.GetDirectoryName(_sharedData.Settings.TnsPath) ?? "");

            if (filePath != null)
            {
                TnsPathTextBox.Text = filePath;
            }
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            TnsPathTextBox.Text = SettingsManager.GetDefaultTnsPath();
            UpdateStatus("TNS ��ΰ� �⺻������ �����Ǿ����ϴ�.", Colors.Green);
        }

        private void SaveSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            if (_sharedData == null) return;

            if (!ValidationHelper.ValidateNotEmpty(TnsPathTextBox.Text, "TNS ���� ���"))
                return;

            if (!FileDialogManager.FileExists(TnsPathTextBox.Text))
            {
                var result = MessageBox.Show(
                    "������ ������ �������� �ʽ��ϴ�.\n�׷��� �����Ͻðڽ��ϱ�?",
                    "Ȯ��",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                    return;
            }

            _sharedData.Settings.TnsPath = TnsPathTextBox.Text;
            _sharedData.SaveSettingsCallback?.Invoke();
            
            // QueryExecutionManager ���� ������Ʈ
            _sharedData.QueryExecutionManager?.UpdateSettings(_sharedData.Settings);
            
            // TNS ��� ���� �̺�Ʈ �߻�
            TnsPathChanged?.Invoke(this, EventArgs.Empty);

            UpdateStatus("������ ����Ǿ����ϴ�.", Colors.Green);
            MessageBox.Show("������ ����Ǿ����ϴ�.", "�Ϸ�",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void UpdateStatus(string message, Color color)
        {
            // ���� ������ ���¹� ������Ʈ
            _sharedData?.UpdateStatusCallback?.Invoke(message, color);
        }
    }
}
