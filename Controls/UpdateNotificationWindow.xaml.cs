using System;
using System.Diagnostics;
using System.Windows;

namespace FACTOVA_QueryHelper.Controls
{
    /// <summary>
    /// UpdateNotificationWindow.xaml�� ���� ��ȣ �ۿ� ��
    /// </summary>
    public partial class UpdateNotificationWindow : Window
    {
        private readonly UpdateInfo _updateInfo;

        public bool AutoCheckEnabled { get; private set; }

        public UpdateNotificationWindow(UpdateInfo updateInfo)
        {
            InitializeComponent();
            _updateInfo = updateInfo;
            InitializeContent();
        }

        private void InitializeContent()
        {
            // ���� ���� ǥ��
            VersionInfoTextBlock.Text = $"����: {_updateInfo.CurrentVersion} �� �ֽ�: {_updateInfo.LatestVersion}";

            // ������ ��Ʈ ǥ��
            if (!string.IsNullOrWhiteSpace(_updateInfo.ReleaseNotes))
            {
                ReleaseNotesTextBlock.Text = _updateInfo.ReleaseNotes;
            }
            else
            {
                ReleaseNotesTextBlock.Text = "���ο� ������ ��õǾ����ϴ�.";
            }

            // �������� �ڵ� Ȯ�� �ɼ� �ε�
            var settings = SettingsManager.LoadSettings();
            AutoCheckCheckBox.IsChecked = settings.CheckUpdateOnStartup;
        }

        private void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // �ڵ� Ȯ�� ���� ����
                SaveAutoCheckSetting();

                // �ٿ�ε� URL ����
                if (!string.IsNullOrEmpty(_updateInfo.DownloadUrl))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = _updateInfo.DownloadUrl,
                        UseShellExecute = true
                    });
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"�ٿ�ε� �������� �� �� �����ϴ�:\n{ex.Message}", 
                    "����", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LaterButton_Click(object sender, RoutedEventArgs e)
        {
            // �ڵ� Ȯ�� ���� ����
            SaveAutoCheckSetting();

            DialogResult = false;
            Close();
        }

        private void SaveAutoCheckSetting()
        {
            AutoCheckEnabled = AutoCheckCheckBox.IsChecked ?? true;
            
            var settings = SettingsManager.LoadSettings();
            settings.CheckUpdateOnStartup = AutoCheckEnabled;
            SettingsManager.SaveSettings(settings);
        }
    }
}
