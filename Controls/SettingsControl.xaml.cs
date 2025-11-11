using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using FACTOVA_QueryHelper.Database;

namespace FACTOVA_QueryHelper.Controls
{
    /// <summary>
    /// SettingsControl.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class SettingsControl : UserControl
    {
        private SharedDataContext? _sharedData;
        
        // TNS 경로 변경 이벤트
        public event EventHandler? TnsPathChanged;
        
        // 접속 정보 변경 이벤트
        public event EventHandler? ConnectionInfoChanged;

        public SettingsControl()
        {
            InitializeComponent();
            
            // ConnectionManagementControl의 저장 완료 이벤트 구독
            ConnectionManagement.ConnectionInfosSaved += OnConnectionInfosSaved;
        }

        /// <summary>
        /// 접속 정보가 저장되었을 때 호출됩니다.
        /// </summary>
        private void OnConnectionInfosSaved(object? sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("✅ Connection infos saved in ConnectionManagementControl");
            
            // 상위로 이벤트 전파
            ConnectionInfoChanged?.Invoke(this, EventArgs.Empty);
            
            // 상태바 업데이트
            UpdateStatus("접속 정보가 저장되었습니다.", Colors.Green);
        }

        /// <summary>
        /// 공유 데이터 컨텍스트를 초기화합니다.
        /// </summary>
        public void Initialize(SharedDataContext sharedData)
        {
            _sharedData = sharedData;
            LoadSettings();
            
            // ConnectionManagementControl 초기화
            // 별도의 SharedDataContext가 필요 없이 독립적으로 동작
        }

        /// <summary>
        /// 설정을 로드합니다.
        /// </summary>
        private void LoadSettings()
        {
            if (_sharedData == null) return;

            // 기본 경로 표시
            DefaultPathTextBlock.Text = SettingsManager.GetDefaultTnsPath();
            TnsPathTextBox.Text = _sharedData.Settings.TnsPath;
            
            // DB 기본 경로 표시
            DefaultDatabasePathTextBlock.Text = QueryDatabase.GetDefaultDatabasePath();
            DatabasePathTextBox.Text = string.IsNullOrWhiteSpace(_sharedData.Settings.DatabasePath) 
                ? QueryDatabase.GetDefaultDatabasePath() 
                : _sharedData.Settings.DatabasePath;
            
            // 현재 버전 표시
            var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            CurrentVersionTextBlock.Text = $"v{version}";
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
                Title = "데이터베이스 파일 위치 선택",
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
            UpdateStatus("설정이 기본값으로 복원되었습니다.", Colors.Green);
        }

        private void SaveSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            if (_sharedData == null) return;

            if (!ValidationHelper.ValidateNotEmpty(TnsPathTextBox.Text, "TNS 파일 경로"))
                return;

            if (!FileDialogManager.FileExists(TnsPathTextBox.Text))
            {
                var result = MessageBox.Show(
                    "지정한 파일이 존재하지 않습니다.\n그래도 저장하시겠습니까?",
                    "확인",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                    return;
            }

            bool databasePathChanged = _sharedData.Settings.DatabasePath != DatabasePathTextBox.Text;

            _sharedData.Settings.TnsPath = TnsPathTextBox.Text;
            _sharedData.Settings.DatabasePath = DatabasePathTextBox.Text;
            _sharedData.SaveSettingsCallback?.Invoke();
            
            // QueryExecutionManager 설정 업데이트
            _sharedData.QueryExecutionManager?.UpdateSettings(_sharedData.Settings);
            
            // TNS 경로 변경 이벤트 발생
            TnsPathChanged?.Invoke(this, EventArgs.Empty);

            UpdateStatus("설정이 저장되었습니다.", Colors.Green);
            
            if (databasePathChanged)
            {
                MessageBox.Show(
                    "설정이 저장되었습니다.\n\n" +
                    "⚠️ 데이터베이스 파일 경로가 변경되었습니다.\n" +
                    "변경사항을 적용하려면 프로그램을 재시작하세요.",
                    "완료",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("설정이 저장되었습니다.", "완료",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private async void CheckUpdateButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                CheckUpdateButton.IsEnabled = false;
                CheckUpdateButton.Content = "🔄 확인 중...";
                LastCheckTextBlock.Text = "업데이트 확인 중...";
                HideUpdateStatus(); // 이전 상태 숨기기

                System.Diagnostics.Debug.WriteLine("=== Manual Update Check Started (force refresh) ===");
                
                // 🔥 forceCheck = true: 캐시 무시하고 강제로 API 호출
                var updateInfo = await UpdateChecker.CheckForUpdatesAsync(forceCheck: true);
                
                var now = DateTime.Now;
                LastCheckTextBlock.Text = $"마지막 확인: {now:yyyy-MM-dd HH:mm:ss}";

                System.Diagnostics.Debug.WriteLine($"=== Manual Update Check Result ===");
                System.Diagnostics.Debug.WriteLine($"   updateInfo is null: {updateInfo == null}");

                if (updateInfo == null)
                {
                    System.Diagnostics.Debug.WriteLine("❌ Update check returned null (should not happen)");
                    
                    ShowUpdateStatus(
                        "❌",
                        "업데이트 확인 실패",
                        "업데이트를 확인할 수 없습니다.\n\n" +
                        "가능한 원인:\n" +
                        "• 인터넷 연결 확인\n" +
                        "• 방화벽에서 GitHub API 접근 허용\n" +
                        "• GitHub 서비스 상태 확인",
                        "#DC3545",
                        "#F8D7DA");
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"   Current: {updateInfo.CurrentVersion}");
                System.Diagnostics.Debug.WriteLine($"   Latest: {updateInfo.LatestVersion}");
                System.Diagnostics.Debug.WriteLine($"   HasUpdate: {updateInfo.HasUpdate}");
                System.Diagnostics.Debug.WriteLine($"   ErrorMessage: {updateInfo.ErrorMessage ?? "None"}");

                // 🔥 Rate Limit 정보 업데이트
                UpdateRateLimitDisplay();

                // 🔥 에러가 있는 경우
                if (!string.IsNullOrEmpty(updateInfo.ErrorMessage))
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ Update check completed with error");
                    
                    ShowUpdateStatus(
                        "⚠️",
                        "업데이트 확인 중 문제 발생",
                        $"현재 버전: {updateInfo.CurrentVersion}\n" +
                        $"GitHub 릴리즈: {updateInfo.LatestVersion}\n\n" +
                        $"오류:\n{updateInfo.ErrorMessage}\n\n" +
                        "현재 버전을 계속 사용할 수 있습니다.",
                        "#FFC107",
                        "#FFF3CD");
                    return;
                }

                if (updateInfo.HasUpdate)
                {
                    System.Diagnostics.Debug.WriteLine("🎉 Update available!");
                    
                    ShowUpdateStatus(
                        "🎉",
                        "새로운 버전이 있습니다!",
                        $"현재 버전: {updateInfo.CurrentVersion}\n" +
                        $"최신 버전: {updateInfo.LatestVersion}\n\n" +
                        $"아래 버튼을 클릭하여 업데이트하세요.",
                        "#0078D7",
                        "#D1E7FF");
                    
                    // UpdateNotificationWindow 표시
                    var updateWindow = new UpdateNotificationWindow(updateInfo)
                    {
                        Owner = Window.GetWindow(this)
                    };
                    
                    updateWindow.ShowDialog();
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("✅ Already up to date");
                    
                    ShowUpdateStatus(
                        "✅",
                        "최신 버전입니다",
                        $"현재 버전: {updateInfo.CurrentVersion}\n\n" +
                        "최신 버전을 사용 중입니다.",
                        "#28A745",
                        "#D4EDDA");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Exception in CheckUpdateButton_Click:");
                System.Diagnostics.Debug.WriteLine($"   Type: {ex.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"   Message: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"   StackTrace: {ex.StackTrace}");
                
                LastCheckTextBlock.Text = "확인 실패";
                
                ShowUpdateStatus(
                    "❌",
                    "업데이트 확인 오류",
                    $"오류: {ex.GetType().Name}\n" +
                    $"메시지: {ex.Message}\n\n" +
                    "Output 창의 디버그 탭에서 자세한 로그를 확인하세요.",
                    "#DC3545",
                    "#F8D7DA");
            }
            finally
            {
                CheckUpdateButton.IsEnabled = true;
                CheckUpdateButton.Content = "🔄 지금 업데이트 확인";
            }
        }

        /// <summary>
        /// 업데이트 상태 패널을 숨깁니다.
        /// </summary>
        private void HideUpdateStatus()
        {
            UpdateStatusPanel.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// 업데이트 상태 패널을 표시합니다.
        /// </summary>
        private void ShowUpdateStatus(string icon, string title, string message, string borderColor, string backgroundColor)
        {
            UpdateStatusPanel.Visibility = Visibility.Visible;
            UpdateStatusIcon.Text = icon;
            UpdateStatusTitle.Text = title;
            UpdateStatusMessage.Text = message;
            UpdateStatusPanel.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(borderColor));
            UpdateStatusPanel.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(backgroundColor));
        }

        /// <summary>
        /// Rate Limit 정보를 업데이트합니다.
        /// </summary>
        private void UpdateRateLimitDisplay()
        {
            var rateLimitInfo = UpdateChecker.GetLastRateLimitInfo();
            
            if (rateLimitInfo != null)
            {
                int remaining = rateLimitInfo.Remaining;
                int limit = rateLimitInfo.Limit;
                int minutesUntilReset = rateLimitInfo.MinutesUntilReset;
                
                RateLimitTextBlock.Text = $"API 호출 제한: {remaining}/{limit} 남음 ({minutesUntilReset}분 후 리셋)";
                
                // 남은 횟수에 따라 색상 변경
                if (remaining == 0)
                {
                    RateLimitTextBlock.Foreground = new SolidColorBrush(Colors.Red);
                }
                else if (remaining < 10)
                {
                    RateLimitTextBlock.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFF9800"));
                }
                else
                {
                    RateLimitTextBlock.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF666666"));
                }
                
                System.Diagnostics.Debug.WriteLine($"Rate Limit UI 업데이트: {remaining}/{limit} (리셋: {minutesUntilReset}분 후)");
            }
            else
            {
                // Rate Limit 정보가 없으면 기본 메시지
                RateLimitTextBlock.Text = "API 호출 제한: GitHub API는 시간당 60회 제한";
                RateLimitTextBlock.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF666666"));
            }
        }

        private void UpdateStatus(string message, Color color)
        {
            // 메인 윈도우 상태바 업데이트
            _sharedData?.UpdateStatusCallback?.Invoke(message, color);
        }
    }
}
