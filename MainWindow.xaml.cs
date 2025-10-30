using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using FACTOVA_QueryHelper.Controls;

namespace FACTOVA_QueryHelper
{
    // SFC 모니터링 데이터 모델
    public class SfcEquipmentInfo
    {
        public string IpAddress { get; set; } = string.Empty;
        public string EquipmentId { get; set; } = string.Empty;
        public string EquipmentName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string BizActor { get; set; } = string.Empty;
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private SharedDataContext _sharedData;

        public MainWindow()
        {
            InitializeComponent();
            
            _sharedData = new SharedDataContext();
            
            Loaded += MainWindow_Loaded;
            Closing += MainWindow_Closing;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // 설정 로드
            _sharedData.Settings = SettingsManager.LoadSettings();
            
            // 공유 콜백 설정
            _sharedData.UpdateStatusCallback = UpdateStatus;
            _sharedData.SaveSettingsCallback = SaveSettings;

            // TNS 엔트리 로드
            LoadTnsEntries();

            // SFC 쿼리 매니저 초기화
            _sharedData.SfcQueryManager = new SfcQueryManager(_sharedData.TnsEntries);

            // 각 UserControl 초기화 (this.로 명시적 참조)
            // QueryExecutionManager는 LogAnalysisControl에서 초기화됨
            this.LogAnalysisControl.Initialize(_sharedData);
            this.QueryManagementControl.Initialize(_sharedData);
            this.SfcMonitoringControl.Initialize(_sharedData);
            this.SettingsControl.Initialize(_sharedData);
            
            // 설정 탭의 TNS 경로 변경 이벤트 구독
            this.SettingsControl.TnsPathChanged += (s, args) => LoadTnsEntries();

            // 업데이트 확인 (비동기)
            _ = CheckForUpdatesAsync();
        }

        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            // 로그 분석 컨트롤의 자동 실행 중지
            this.LogAnalysisControl.StopAutoQuery();

            // 설정 저장
            SaveSettings();
        }

        private void SaveSettings()
        {
            SettingsManager.SaveSettings(_sharedData.Settings);
        }

        private void LoadTnsEntries()
        {
            try
            {
                _sharedData.TnsEntries = TnsParser.ParseTnsFile(_sharedData.Settings.TnsPath);

                if (_sharedData.TnsEntries.Count > 0)
                {
                    UpdateStatus($"TNS 엔트리 {_sharedData.TnsEntries.Count}개 로드됨", Colors.Green);
                    
                    // Manager 업데이트
                    _sharedData.SfcQueryManager?.UpdateTnsEntries(_sharedData.TnsEntries);
                    _sharedData.QueryExecutionManager?.UpdateTnsEntries(_sharedData.TnsEntries);
                }
                else
                {
                    UpdateStatus("TNS 엔트리를 찾을 수 없습니다. 설정 탭에서 경로를 확인하세요.", Colors.Orange);
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"TNS 로드 실패: {ex.Message}", Colors.Red);
                System.Diagnostics.Debug.WriteLine($"TNS 로드 오류: {ex}");
            }
        }

        private void UpdateStatus(string message, Color color)
        {
            // 메인 윈도우에는 상태바가 없으므로 디버그 출력만 수행
            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now:HH:mm:ss}] {message}");
        }

        /// <summary>
        /// 애플리케이션 시작 시 업데이트를 확인합니다.
        /// </summary>
        private async System.Threading.Tasks.Task CheckForUpdatesAsync()
        {
            // 설정에서 자동 확인이 비활성화되어 있으면 건너뛰기
            if (!_sharedData.Settings.CheckUpdateOnStartup)
            {
                return;
            }

            try
            {
                // UI가 완전히 로드된 후 약간 지연
                await System.Threading.Tasks.Task.Delay(1000);

                var updateInfo = await UpdateChecker.CheckForUpdatesAsync();

                if (updateInfo.HasUpdate)
                {
                    // UI 스레드에서 업데이트 알림 창 표시
                    Dispatcher.Invoke(() =>
                    {
                        var updateWindow = new UpdateNotificationWindow(updateInfo)
                        {
                            Owner = this
                        };
                        updateWindow.ShowDialog();
                    });
                }
            }
            catch (Exception ex)
            {
                // 업데이트 확인 실패는 조용히 무시
                System.Diagnostics.Debug.WriteLine($"업데이트 확인 중 오류: {ex.Message}");
            }
        }
    }
}