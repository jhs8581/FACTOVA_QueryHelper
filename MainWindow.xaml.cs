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
            this.GmesInfoControlNew.Initialize(_sharedData);  // 🔥 GMES 정보 조회 (New) 초기화
            this.BizQueryControl.Initialize(_sharedData);
            this.QueryManagementControl.Initialize(_sharedData);
            this.QueryEditorView.SetSharedDataContext(_sharedData);  // 🔥 SharedDataContext 설정
            this.QueryEditorView.RefreshAllQueryExecutorConnections();  // 🔥 연결 정보 새로고침
            this.NerpValidationControl.SetSharedDataContext(_sharedData);  // 🔥 NERP 검증 컨트롤 초기화
            this.SfcMonitoringControl.Initialize(_sharedData);
            this.QueryBizTransformView.Initialize(_sharedData);
            this.SettingsControl.Initialize(_sharedData);
            
            // 설정 탭의 이벤트 구독
            this.SettingsControl.TnsPathChanged += (s, args) => LoadTnsEntries();
            this.SettingsControl.ConnectionInfoChanged += OnConnectionInfoChanged;
            this.SettingsControl.ShortcutsChanged += OnShortcutsChanged;  // 🔥 단축어 변경 이벤트 구독
            
            // 🔥 탭 설정 적용 (순서 및 표시 여부)
            ApplyTabSettings();
        }

        /// <summary>
        /// 🔥 탭 설정을 적용합니다 (순서 및 표시 여부)
        /// </summary>
        private void ApplyTabSettings()
        {
            try
            {
                var tabSettings = _sharedData.Settings.TabSettings;
                
                // 설정이 없으면 기본값 사용
                if (tabSettings == null || tabSettings.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("ℹ️ Tab settings not found, using default order");
                    return;
                }

                // TabId와 TabItem 매핑
                var tabMap = new Dictionary<string, TabItem>
                {
                    { "LogAnalysis", GetTabItemByIndex(0) },
                    { "GmesInfo", GetTabItemByIndex(1) },
                    { "GmesInfoNew", GetTabItemByIndex(2) },
                    { "BizQuery", GetTabItemByIndex(3) },
                    { "QueryManagement", GetTabItemByIndex(4) },
                    { "QueryEditor", GetTabItemByIndex(5) },
                    { "NerpValidation", GetTabItemByIndex(6) },
                    { "SfcMonitoring", GetTabItemByIndex(7) },
                    { "BizTransform", GetTabItemByIndex(8) },
                    { "InTransform", GetTabItemByIndex(9) },
                    { "Settings", GetTabItemByIndex(10) },
                    { "Help", GetTabItemByIndex(11) }
                };

                // 기존 탭 제거 (컬렉션에서만)
                var tabsToReorder = new List<TabItem>();
                foreach (TabItem tab in MainTabControl.Items)
                {
                    tabsToReorder.Add(tab);
                }
                MainTabControl.Items.Clear();

                // 탭 설정에 따라 순서대로 추가
                var sortedSettings = tabSettings.OrderBy(t => t.Order).ToList();
                
                foreach (var setting in sortedSettings)
                {
                    if (tabMap.TryGetValue(setting.TabId, out var tabItem) && tabItem != null)
                    {
                        // 표시 여부 설정
                        tabItem.Visibility = setting.IsVisible ? Visibility.Visible : Visibility.Collapsed;
                        MainTabControl.Items.Add(tabItem);
                        
                        System.Diagnostics.Debug.WriteLine($"📑 Tab '{setting.TabName}' - Order: {setting.Order}, Visible: {setting.IsVisible}");
                    }
                }

                // 설정에 없는 탭이 있으면 끝에 추가 (새로 추가된 탭)
                foreach (var kvp in tabMap)
                {
                    if (kvp.Value != null && !MainTabControl.Items.Contains(kvp.Value))
                    {
                        MainTabControl.Items.Add(kvp.Value);
                        System.Diagnostics.Debug.WriteLine($"📑 Tab '{kvp.Key}' - Added (not in settings)");
                    }
                }

                // 첫 번째 보이는 탭 선택
                foreach (TabItem tab in MainTabControl.Items)
                {
                    if (tab.Visibility == Visibility.Visible)
                    {
                        MainTabControl.SelectedItem = tab;
                        break;
                    }
                }

                System.Diagnostics.Debug.WriteLine($"✅ Tab settings applied: {sortedSettings.Count} tabs configured");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Failed to apply tab settings: {ex.Message}");
            }
        }

        /// <summary>
        /// 🔥 인덱스로 TabItem 가져오기
        /// </summary>
        private TabItem? GetTabItemByIndex(int index)
        {
            if (MainTabControl.Items.Count > index && MainTabControl.Items[index] is TabItem tabItem)
            {
                return tabItem;
            }
            return null;
        }

        /// <summary>
        /// 접속 정보가 변경되었을 때 호출됩니다.
        /// </summary>
        private void OnConnectionInfoChanged(object? sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("🔔 Connection info changed - notifying all controls");
            UpdateStatus("접속 정보가 업데이트되었습니다.", Colors.Blue);
            
            // 🔥 QueryEditorView의 접속 정보도 새로고침
            this.QueryEditorView.RefreshAllQueryExecutorConnections();
            
            // 🔥 NERP 검증 컨트롤의 접속 정보도 새로고침
            this.NerpValidationControl.RefreshConnectionInfos();
            
            // 필요한 경우 다른 컨트롤에 알림
            // 예: QueryExecutorControl이 열려 있다면 연결 정보 새로고침
        }
        
        /// <summary>
        /// 🔥 테이블 단축어가 변경되었을 때 호출됩니다.
        /// </summary>
        private void OnShortcutsChanged(object? sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("🔔 Table shortcuts changed - reloading shortcuts in all controls");
            UpdateStatus("테이블 단축어가 업데이트되었습니다.", Colors.Green);
            
            // 🔥 QueryEditorView의 모든 QueryExecutor에 단축어 재로드
            this.QueryEditorView.ReloadAllShortcuts();
            
            // 🔥 NERP 검증 컨트롤에도 단축어 재로드
            this.NerpValidationControl.ReloadShortcuts(_sharedData.Settings.DatabasePath);
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
        /// 🔥 쿼리 실행 탭에서 쿼리 열기
        /// </summary>
        public void OpenQueryInEditorTab(QueryItem query)
        {
            try
            {
                // 쿼리 실행 탭 찾기
                TabItem? queryEditorTab = null;
                foreach (TabItem tab in MainTabControl.Items)
                {
                    if (tab.Content is QueryEditorView)
                    {
                        queryEditorTab = tab;
                        break;
                    }
                }

                if (queryEditorTab != null)
                {
                    // 쿼리 실행 탭으로 이동
                    MainTabControl.SelectedItem = queryEditorTab;
                    
                    // QueryEditorView에 쿼리 전달
                    QueryEditorView.OpenQueryInNewTab(query);
                    
                    System.Diagnostics.Debug.WriteLine($"📤 Query '{query.BizName}' opened in Query Editor tab");
                }
                else
                {
                    MessageBox.Show("쿼리 실행 탭을 찾을 수 없습니다.", "오류",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"쿼리 탭 열기 실패:\n{ex.Message}", "오류",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
