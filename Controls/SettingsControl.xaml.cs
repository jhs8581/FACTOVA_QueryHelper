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
            
            // DB �⺻ ��� ǥ��
            DefaultDatabasePathTextBlock.Text = QueryDatabase.GetDefaultDatabasePath();
            DatabasePathTextBox.Text = string.IsNullOrWhiteSpace(_sharedData.Settings.DatabasePath) 
                ? QueryDatabase.GetDefaultDatabasePath() 
                : _sharedData.Settings.DatabasePath;
            
            // ������Ʈ �ڵ� Ȯ�� ���� �ε�
            CheckUpdateOnStartupCheckBox.IsChecked = _sharedData.Settings.CheckUpdateOnStartup;
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

        private void BrowseDatabaseButton_Click(object sender, RoutedEventArgs e)
        {
            var saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "SQLite Database (*.db)|*.db|All Files (*.*)|*.*",
                Title = "�����ͺ��̽� ���� ��ġ ����",
                FileName = "queries.db",
                DefaultExt = ".db"
            };

            if (!string.IsNullOrWhiteSpace(DatabasePathTextBox.Text))
            {
                var directory = Path.GetDirectoryName(DatabasePathTextBox.Text);
                if (!string.IsNullOrEmpty(directory) && Directory.Exists(directory))
                {
                    saveFileDialog.InitialDirectory = directory;
                }
            }

            if (saveFileDialog.ShowDialog() == true)
            {
                DatabasePathTextBox.Text = saveFileDialog.FileName;
            }
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            TnsPathTextBox.Text = SettingsManager.GetDefaultTnsPath();
            DatabasePathTextBox.Text = QueryDatabase.GetDefaultDatabasePath();
            UpdateStatus("������ �⺻������ �����Ǿ����ϴ�.", Colors.Green);
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

            bool databasePathChanged = _sharedData.Settings.DatabasePath != DatabasePathTextBox.Text;

            _sharedData.Settings.TnsPath = TnsPathTextBox.Text;
            _sharedData.Settings.DatabasePath = DatabasePathTextBox.Text;
            _sharedData.SaveSettingsCallback?.Invoke();
            
            // QueryExecutionManager ���� ������Ʈ
            _sharedData.QueryExecutionManager?.UpdateSettings(_sharedData.Settings);
            
            // TNS ��� ���� �̺�Ʈ �߻�
            TnsPathChanged?.Invoke(this, EventArgs.Empty);

            UpdateStatus("������ ����Ǿ����ϴ�.", Colors.Green);
            
            if (databasePathChanged)
            {
                MessageBox.Show(
                    "������ ����Ǿ����ϴ�.\n\n" +
                    "?? �����ͺ��̽� ���� ��ΰ� ����Ǿ����ϴ�.\n" +
                    "��������� �����Ϸ��� ���α׷��� ������ϼ���.",
                    "�Ϸ�",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("������ ����Ǿ����ϴ�.", "�Ϸ�",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void CheckUpdateOnStartupCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (_sharedData == null) return;

            // üũ�ڽ� ���¸� ������ ����
            _sharedData.Settings.CheckUpdateOnStartup = CheckUpdateOnStartupCheckBox.IsChecked ?? true;
            _sharedData.SaveSettingsCallback?.Invoke();

            System.Diagnostics.Debug.WriteLine($"������Ʈ �ڵ� Ȯ�� ���� ����: {_sharedData.Settings.CheckUpdateOnStartup}");
        }

        private async void CheckUpdateNowButton_Click(object sender, RoutedEventArgs e)
        {
            CheckUpdateNowButton.IsEnabled = false;
            CheckUpdateNowButton.Content = "Ȯ�� ��...";
            UpdateStatus("������Ʈ�� Ȯ���ϴ� ��...", Colors.Blue);

            try
            {
                var updateInfo = await UpdateChecker.CheckForUpdatesAsync();

                if (updateInfo.HasUpdate)
                {
                    UpdateStatus($"�� ���� {updateInfo.LatestVersion}�� �ֽ��ϴ�.", Colors.Green);
                    
                    var updateWindow = new UpdateNotificationWindow(updateInfo)
                    {
                        Owner = Window.GetWindow(this)
                    };
                    updateWindow.ShowDialog();
                }
                else if (!string.IsNullOrEmpty(updateInfo.ErrorMessage))
                {
                    UpdateStatus($"������Ʈ Ȯ�� ����: {updateInfo.ErrorMessage}", Colors.Red);
                    MessageBox.Show($"������Ʈ�� Ȯ���� �� �����ϴ�:\n{updateInfo.ErrorMessage}", 
                        "����", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else
                {
                    UpdateStatus("�ֽ� ������ ��� ���Դϴ�.", Colors.Green);
                    MessageBox.Show("���� �ֽ� ������ ����ϰ� �ֽ��ϴ�.", 
                        "������Ʈ", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"������Ʈ Ȯ�� �� ����: {ex.Message}", Colors.Red);
                MessageBox.Show($"������Ʈ Ȯ�� �� ������ �߻��߽��ϴ�:\n{ex.Message}", 
                    "����", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                CheckUpdateNowButton.IsEnabled = true;
                CheckUpdateNowButton.Content = "���� ������Ʈ Ȯ��";
            }
        }

        private void UpdateStatus(string message, Color color)
        {
            // ���� ������ ���¹� ������Ʈ
            _sharedData?.UpdateStatusCallback?.Invoke(message, color);
        }
    }
}
