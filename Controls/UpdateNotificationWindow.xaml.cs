using System;
using System.Diagnostics;
using System.Windows;

namespace FACTOVA_QueryHelper.Controls
{
    /// <summary>
    /// UpdateNotificationWindow.xaml에 대한 상호 작용 논리
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
            // 버전 정보 표시
            VersionInfoTextBlock.Text = $"현재: {_updateInfo.CurrentVersion} → 최신: {_updateInfo.LatestVersion}";

            // 릴리즈 노트 표시
            if (!string.IsNullOrWhiteSpace(_updateInfo.ReleaseNotes))
            {
                ReleaseNotesTextBlock.Text = _updateInfo.ReleaseNotes;
            }
            else
            {
                ReleaseNotesTextBlock.Text = "새로운 버전이 출시되었습니다.";
            }

            // 설정에서 자동 확인 옵션 로드
            var settings = SettingsManager.LoadSettings();
            AutoCheckCheckBox.IsChecked = settings.CheckUpdateOnStartup;
        }

        private void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 자동 확인 설정 저장
                SaveAutoCheckSetting();

                // 다운로드 URL 열기
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
                MessageBox.Show($"다운로드 페이지를 열 수 없습니다:\n{ex.Message}", 
                    "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LaterButton_Click(object sender, RoutedEventArgs e)
        {
            // 자동 확인 설정 저장
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
