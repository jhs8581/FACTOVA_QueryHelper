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
            
            // 🔥 SharedDataContext의 이벤트 발생
            if (_sharedData != null)
            {
                _sharedData.NotifyConnectionInfosChanged();
                System.Diagnostics.Debug.WriteLine("🔔 NotifyConnectionInfosChanged called from SettingsControl");
            }
            
            // 🔥 SiteManagement의 ComboBox도 새로고침
            SiteManagement.RefreshConnectionInfos();
            
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
            
            // 🔥 ConnectionManagementControl 초기화 (동일한 DB 경로 사용)
            ConnectionManagement.Initialize(sharedData.Settings.DatabasePath);
            
            // 🔥 SiteManagementControl 초기화 (동일한 DB 경로 사용)
            var database = new QueryDatabase(sharedData.Settings.DatabasePath);
            SiteManagement.Initialize(database);
            
            // 🔥 SiteManagementControl의 저장 이벤트 구독
            SiteManagement.SiteInfosSaved += OnSiteInfosSaved;
        }

        /// <summary>
        /// 사업장 정보가 저장되었을 때 호출됩니다.
        /// </summary>
        private void OnSiteInfosSaved(object? sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("✅ Site infos saved in SiteManagementControl");
            
            // 상태바 업데이트
            UpdateStatus("사업장 정보가 저장되었습니다.", Colors.Green);
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
                FileName = "FACTOVA_DB.db",
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
        
        /// <summary>
        /// DB 계정 정보 일괄 변경
        /// </summary>
        private void BulkUpdateButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var tns = BulkTnsTextBox.Text.Trim();
                var userId = BulkUserIdTextBox.Text.Trim();
                var password = BulkPasswordBox.Text.Trim();

                // 필수 필드 검증
                if (string.IsNullOrEmpty(tns))
                {
                    MessageBox.Show("TNS 이름을 입력해주세요.", "입력 오류",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    BulkTnsTextBox.Focus();
                    return;
                }

                if (string.IsNullOrEmpty(userId))
                {
                    MessageBox.Show("User ID를 입력해주세요.", "입력 오류",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    BulkUserIdTextBox.Focus();
                    return;
                }

                if (string.IsNullOrEmpty(password))
                {
                    MessageBox.Show("새 Password를 입력해주세요.", "입력 오류",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    BulkPasswordBox.Focus();
                    return;
                }

                // 확인 메시지
                var confirmMessage = $"다음 조건에 일치하는 모든 항목의 Password를 변경하시겠습니까?\n\n";
                confirmMessage += $"🔍 조건:\n";
                confirmMessage += $"  • TNS: {tns}\n";
                confirmMessage += $"  • User ID: {userId}\n\n";
                confirmMessage += $"🔐 새 Password: {password}\n\n";
                confirmMessage += $"변경 대상 테이블:\n";
                confirmMessage += $"  • 쿼리 관리 (모든 쿼리 타입)\n";
                confirmMessage += $"  • 접속 정보 관리\n\n";
                confirmMessage += "⚠️ 이 작업은 되돌릴 수 없습니다!";

                var result = MessageBox.Show(
                    confirmMessage,
                    "Password 일괄 변경 확인",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result != MessageBoxResult.Yes)
                    return;

                BulkUpdateButton.IsEnabled = false;
                BulkUpdateButton.Content = "🔄 변경 중...";
                BulkUpdateResultPanel.Visibility = Visibility.Collapsed;

                int queryUpdated = 0;
                int connectionUpdated = 0;

                // 1. 쿼리 테이블 업데이트
                if (_sharedData?.Settings.DatabasePath != null)
                {
                    var database = new QueryDatabase(_sharedData.Settings.DatabasePath);
                    queryUpdated = database.BulkUpdateCredentials(tns, userId, password);
                }

                // 2. 접속 정보 테이블 업데이트
                if (_sharedData?.Settings.DatabasePath != null)
                {
                    var connectionService = new Services.ConnectionInfoService(_sharedData.Settings.DatabasePath);
                    connectionUpdated = connectionService.BulkUpdateCredentials(tns, userId, password);
                }

                // 결과 표시
                ShowBulkUpdateResult(queryUpdated, connectionUpdated, tns, userId, password);

                // 입력 필드 초기화
                BulkTnsTextBox.Clear();
                BulkUserIdTextBox.Clear();
                BulkPasswordBox.Clear();

                // 다른 컨트롤들에게 변경 알림
                ConnectionInfoChanged?.Invoke(this, EventArgs.Empty);

                UpdateStatus($"Password 일괄 변경 완료: 쿼리 {queryUpdated}개, 접속정보 {connectionUpdated}개", Colors.Green);
            }
            catch (ArgumentException ex)
            {
                MessageBox.Show(ex.Message, "입력 오류",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"일괄 변경 중 오류가 발생했습니다:\n\n{ex.Message}",
                    "오류",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                
                UpdateStatus($"Password 일괄 변경 실패: {ex.Message}", Colors.Red);
            }
            finally
            {
                BulkUpdateButton.IsEnabled = true;
                
                var updateButtonContent = new StackPanel { Orientation = Orientation.Horizontal };
                updateButtonContent.Children.Add(new TextBlock { Text = "🔑", FontSize = 20, Margin = new Thickness(0, 0, 10, 0) });
                updateButtonContent.Children.Add(new TextBlock { Text = "Password 일괄 변경" });
                BulkUpdateButton.Content = updateButtonContent;
            }
        }

        /// <summary>
        /// 일괄 변경 결과를 표시합니다.
        /// </summary>
        private void ShowBulkUpdateResult(int queryCount, int connectionCount, string tns, string userId, string password)
        {
            BulkUpdateResultPanel.Visibility = Visibility.Visible;
            
            if (queryCount + connectionCount > 0)
            {
                BulkUpdateResultTitle.Text = "✅ Password 일괄 변경 완료";
                
                var resultMessage = $"총 {queryCount + connectionCount}개의 Password가 변경되었습니다.\n\n";
                resultMessage += $"🔍 변경 조건:\n";
                resultMessage += $"  • TNS: {tns}\n";
                resultMessage += $"  • User ID: {userId}\n\n";
                resultMessage += $"📊 변경 결과:\n";
                resultMessage += $"  • 쿼리 관리: {queryCount}개\n";
                resultMessage += $"  • 접속 정보: {connectionCount}개\n\n";
                resultMessage += $"🔐 새 Password: {password}";

                BulkUpdateResultMessage.Text = resultMessage;
                BulkUpdateResultPanel.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D4EDDA"));
                BulkUpdateResultPanel.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#28A745"));

                MessageBox.Show(
                    resultMessage,
                    "Password 일괄 변경 완료",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            else
            {
                BulkUpdateResultTitle.Text = "⚠️ 변경된 항목 없음";
                
                var resultMessage = $"조건에 일치하는 항목을 찾을 수 없습니다.\n\n";
                resultMessage += $"🔍 입력한 조건:\n";
                resultMessage += $"  • TNS: {tns}\n";
                resultMessage += $"  • User ID: {userId}\n\n";
                resultMessage += $"💡 확인 사항:\n";
                resultMessage += $"  • TNS 이름이 정확한지 확인하세요\n";
                resultMessage += $"  • User ID가 정확한지 확인하세요\n";
                resultMessage += $"  • 대소문자를 구분합니다";

                BulkUpdateResultMessage.Text = resultMessage;
                BulkUpdateResultPanel.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFF3CD"));
                BulkUpdateResultPanel.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFC107"));

                MessageBox.Show(
                    resultMessage,
                    "변경된 항목 없음",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }
    }
}
