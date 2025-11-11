using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using FACTOVA_QueryHelper.Controls;
using FACTOVA_QueryHelper.SFC;

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
            
            // 윈도우 타이틀에 버전 정보 추가
            SetWindowTitle();
            
            Loaded += MainWindow_Loaded;
            Closing += MainWindow_Closing;
        }

        /// <summary>
        /// 윈도우 타이틀에 버전 정보를 설정합니다.
        /// </summary>
        private void SetWindowTitle()
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                var version = assembly.GetName().Version;
                
                if (version != null)
                {
                    // Major.Minor.Patch 형식으로 표시
                    Title = $"FACTOVA Query Helper v{version.Major}.{version.Minor}.{version.Build}";
                }
            }
            catch
            {
                // 버전 정보를 가져올 수 없으면 기본 타이틀 유지
                Title = "FACTOVA Query Helper";
            }
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
            this.GmesInfoControl.Initialize(_sharedData);
            this.BizQueryControl.Initialize(_sharedData);
            this.QueryManagementControl.Initialize(_sharedData);
            this.SfcMonitoringControl.Initialize(_sharedData);
            this.SettingsControl.Initialize(_sharedData);
            
            // 설정 탭의 이벤트 구독
            this.SettingsControl.TnsPathChanged += (s, args) => LoadTnsEntries();
            this.SettingsControl.ConnectionInfoChanged += OnConnectionInfoChanged;
        }

        /// <summary>
        /// 접속 정보가 변경되었을 때 호출됩니다.
        /// </summary>
        private void OnConnectionInfoChanged(object? sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("🔔 Connection info changed - notifying all controls");
            UpdateStatus("접속 정보가 업데이트되었습니다.", Colors.Blue);
            
            // 필요한 경우 다른 컨트롤에 알림
            // 예: QueryExecutorControl이 열려 있다면 연결 정보 새로고침
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
    }
}
