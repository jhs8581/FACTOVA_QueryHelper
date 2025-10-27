using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.IO;
using System.Collections.ObjectModel;
using System.Windows.Threading;
using OfficeOpenXml;
using System.Text.Json;

namespace FACTOVA_Palletizing_Analysis
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
        private AppSettings _settings;
        private List<TnsEntry> _tnsEntries;
        private List<QueryItem> _loadedQueries;
        private DispatcherTimer? _queryTimer;
        private DispatcherTimer? _countdownTimer;
        private int _remainingSeconds;
        private int _totalIntervalSeconds;
        private bool _isAutoQueryRunning = false;
        private ObservableCollection<SfcEquipmentInfo> _sfcEquipmentList;
        private ObservableCollection<SfcEquipmentInfo> _sfcFilteredList;
        private ObservableCollection<CheckableComboBoxItem> _statusFilterItems;
        private ObservableCollection<CheckableComboBoxItem> _bizActorFilterItems;
        private ObservableCollection<CheckableComboBoxItem> _queryFilterItems;
        private SfcFilterManager? _sfcFilterManager;
        private SfcQueryManager? _sfcQueryManager;
        private QueryExecutionManager? _queryExecutionManager;

        public MainWindow()
        {
            InitializeComponent();
            _settings = new AppSettings();
            _tnsEntries = new List<TnsEntry>();
            _loadedQueries = new List<QueryItem>();
            _sfcEquipmentList = new ObservableCollection<SfcEquipmentInfo>();
            _sfcFilteredList = new ObservableCollection<SfcEquipmentInfo>();
            _statusFilterItems = new ObservableCollection<CheckableComboBoxItem>();
            _bizActorFilterItems = new ObservableCollection<CheckableComboBoxItem>();
            _queryFilterItems = new ObservableCollection<CheckableComboBoxItem>();
            
            Loaded += MainWindow_Loaded;
            Closing += MainWindow_Closing;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // 설정 로드
            _settings = SettingsManager.LoadSettings();
            
            // 기본 경로 표시
            DefaultPathTextBlock.Text = SettingsManager.GetDefaultTnsPath();
            TnsPathTextBox.Text = _settings.TnsPath;

            // Excel 파일 경로 로드
            if (!string.IsNullOrWhiteSpace(_settings.ExcelFilePath) && File.Exists(_settings.ExcelFilePath))
            {
                ExcelFilePathTextBox.Text = _settings.ExcelFilePath;
                LoadQueriesButton.IsEnabled = true;

                // 시트 목록 로드
                try
                {
                    var sheets = ExcelQueryReader.GetSheetNames(_settings.ExcelFilePath);
                    SheetComboBox.ItemsSource = sheets;
                    if (sheets.Count > 0)
                    {
                        SheetComboBox.SelectedIndex = 0;
                    }
                }
                catch
                {
                    // 시트 로드 실패 무시
                }
            }

            // SFC Excel 파일 경로 로드
            if (!string.IsNullOrWhiteSpace(_settings.SfcExcelFilePath) && File.Exists(_settings.SfcExcelFilePath))
            {
                SfcExcelFilePathTextBox.Text = _settings.SfcExcelFilePath;
                LoadSfcExcelButton.IsEnabled = true;
            }

            //쿼리 로드
            SfcQueryTextBox.Text = DEFAULT_SFC_QUERY;

            // SFC 계정 정보 로드
            SfcUserIdTextBox.Text = _settings.SfcUserId;
            SfcPasswordBox.Password = _settings.SfcPassword;

            // 쿼리 타이머 간격 로드
            QueryIntervalTextBox.Text = _settings.QueryIntervalSeconds.ToString();

            // 알림 시 자동 실행 중지 설정 로드
            StopOnNotificationCheckBox.IsChecked = _settings.StopOnNotification;

            // SFC 조회 날짜를 오늘로 설정
            ConfigDatePicker.SelectedDate = DateTime.Today;

            // TNS 엔트리 로드
            LoadTnsEntries();

            // SFC TNS 콤보박스 설정
            LoadSfcTnsComboBox();

            // SFC 필터 콤보박스 초기화
            InitializeSfcFilterComboBoxes();

            // SFC 데이터그리드 바인딩
            SfcMonitorDataGrid.ItemsSource = _sfcFilteredList;
        }

        private void LoadSfcTnsComboBox()
        {
            if (_tnsEntries.Count > 0)
            {
                SfcTnsComboBox.ItemsSource = _tnsEntries.Select(t => t.Name).ToList();
                
                // 저장된 TNS 이름이 있으면 해당 항목 선택
                if (!string.IsNullOrWhiteSpace(_settings.SfcTnsName))
                {
                    var savedIndex = _tnsEntries.FindIndex(t => t.Name == _settings.SfcTnsName);
                    if (savedIndex >= 0)
                    {
                        SfcTnsComboBox.SelectedIndex = savedIndex;
                    }
                    else if (_tnsEntries.Count > 0)
                    {
                        SfcTnsComboBox.SelectedIndex = 0;
                    }
                }
                else if (_tnsEntries.Count > 0)
                {
                    SfcTnsComboBox.SelectedIndex = 0;
                }
            }
        }

        private void InitializeSfcFilterComboBoxes()
        {
            // 상태 필터 초기화 (기본값: OFF만 선택)
            _statusFilterItems.Add(new CheckableComboBoxItem { Text = "전체", IsChecked = false });
            _statusFilterItems.Add(new CheckableComboBoxItem { Text = "ON", IsChecked = false });
            _statusFilterItems.Add(new CheckableComboBoxItem { Text = "OFF", IsChecked = true }); // OFF를 기본값으로
            FilterStatusComboBox.ItemsSource = _statusFilterItems;

            // BIZACTOR 필터 초기화 (SQL, WIP, RPT)
            _bizActorFilterItems.Add(new CheckableComboBoxItem { Text = "전체", IsChecked = true });
            _bizActorFilterItems.Add(new CheckableComboBoxItem { Text = "SQL", IsChecked = false });
            _bizActorFilterItems.Add(new CheckableComboBoxItem { Text = "WIP", IsChecked = false });
            _bizActorFilterItems.Add(new CheckableComboBoxItem { Text = "RPT", IsChecked = false });
            FilterBizActorComboBox.ItemsSource = _bizActorFilterItems;

            // SFC 필터 매니저 초기화
            _sfcFilterManager = new SfcFilterManager(
                _sfcEquipmentList,
                _sfcFilteredList,
                _statusFilterItems,
                _bizActorFilterItems);

            // SFC 쿼리 매니저 초기화
            _sfcQueryManager = new SfcQueryManager(_tnsEntries);

            // 쿼리 실행 매니저 초기화
            _queryExecutionManager = new QueryExecutionManager(
                UpdateStatus,
                ResultTabControl,
                _tnsEntries,
                _settings,
                CreateResultTab);

            // 콤보박스 텍스트 초기화
            UpdateFilterComboBoxText();
        }

        private void UpdateFilterComboBoxText()
        {
            if (_sfcFilterManager == null)
                return;

            var filterText = _sfcFilterManager.GetFilterComboBoxText();
            FilterStatusComboBox.Text = filterText.StatusText;
            FilterBizActorComboBox.Text = filterText.BizActorText;
        }

        private void FilterCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox && 
                checkBox.DataContext is CheckableComboBoxItem item &&
                _sfcFilterManager != null)
            {
                _sfcFilterManager.HandleCheckBoxChanged(item);
                UpdateFilterComboBoxText();
            }
        }

        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            // 주기적 쿼리 실행 중지
            StopAutoQuery();

            // 설정 저장
            SaveSettings();
        }

        private void SaveSettings()
        {
            // Excel 파일 경로 저장
            _settings.ExcelFilePath = ExcelFilePathTextBox.Text ?? string.Empty;

            // SFC Excel 파일 경로 저장
            _settings.SfcExcelFilePath = SfcExcelFilePathTextBox.Text ?? string.Empty;

            // SFC 계정 정보 저장
            _settings.SfcUserId = SfcUserIdTextBox.Text ?? string.Empty;
            _settings.SfcPassword = SfcPasswordBox.Password ?? string.Empty;

            // SFC TNS 선택값 저장
            _settings.SfcTnsName = SfcTnsComboBox.SelectedItem?.ToString() ?? string.Empty;

            // 쿼리 타이머 간격 저장
            if (int.TryParse(QueryIntervalTextBox.Text, out int interval))
            {
                _settings.QueryIntervalSeconds = interval;
            }

            // 알림 시 자동 실행 중지 설정 저장
            _settings.StopOnNotification = StopOnNotificationCheckBox.IsChecked ?? true;

            SettingsManager.SaveSettings(_settings);
        }

        private void LoadTnsEntries()
        {
            try
            {
                _tnsEntries = TnsParser.ParseTnsFile(_settings.TnsPath);

                if (_tnsEntries.Count > 0)
                {
                    UpdateStatus($"TNS 엔트리 {_tnsEntries.Count}개 로드됨", Colors.Green);
                    
                    // Manager 업데이트
                    _sfcQueryManager?.UpdateTnsEntries(_tnsEntries);
                    _queryExecutionManager?.UpdateTnsEntries(_tnsEntries);
                    
                    // 디버그: 로드된 TNS 이름들을 로그에 출력
                    System.Diagnostics.Debug.WriteLine("=== 로드된 TNS 엔트리 목록 ===");
                    foreach (var entry in _tnsEntries)
                    {
                        System.Diagnostics.Debug.WriteLine($"  - {entry.Name} (Host: {entry.Host}, Port: {entry.Port}, Service: {entry.ServiceName})");
                    }
                }
                else
                {
                    UpdateStatus("TNS 엔트리를 찾을 수 없습니다. 설정 탭에서 경로를 확인하세요.", Colors.Orange);
                    
                    // 디버그: 파일 내용 확인
                    if (File.Exists(_settings.TnsPath))
                    {
                        var allNames = TnsParser.GetAllEntryNames(_settings.TnsPath);
                        System.Diagnostics.Debug.WriteLine($"TNS 파일에서 발견된 엔트리 이름들: {string.Join(", ", allNames)}");
                    }
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"TNS 로드 실패: {ex.Message}", Colors.Red);
                System.Diagnostics.Debug.WriteLine($"TNS 로드 오류: {ex}");
            }
        }

        // Excel 관련 메서드

        private void BrowseExcelButton_Click(object sender, RoutedEventArgs e)
        {
            string? filePath = FileDialogManager.OpenExcelFileDialog("Excel 쿼리 파일 선택");

            if (filePath != null)
            {
                ExcelFilePathTextBox.Text = filePath;
                LoadQueriesButton.IsEnabled = true;

                try
                {
                    var sheets = ExcelManager.GetSheetNames(filePath);
                    SheetComboBox.ItemsSource = sheets;
                    if (sheets.Count > 0)
                    {
                        SheetComboBox.SelectedIndex = 0;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Excel 파일 읽기 실패:\n{ex.Message}", "오류",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }

                SaveSettings();
            }
        }

        private void LoadQueriesButton_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("=== 쿼리 로드 시작 ===");
            System.Diagnostics.Debug.WriteLine($"Excel 파일 경로: {ExcelFilePathTextBox.Text}");
            System.Diagnostics.Debug.WriteLine($"시작 행: {StartRowTextBox.Text}");
            System.Diagnostics.Debug.WriteLine($"선택된 시트: {SheetComboBox.SelectedItem?.ToString()}");

            if (!ValidationHelper.ValidateStartRow(StartRowTextBox.Text, out int startRow))
            {
                System.Diagnostics.Debug.WriteLine("시작 행 검증 실패");
                return;
            }

            try
            {
                System.Diagnostics.Debug.WriteLine($"Excel 파일에서 쿼리 로드 시작... (시작 행: {startRow})");
                
                _loadedQueries = ExcelManager.LoadQueries(
                    ExcelFilePathTextBox.Text,
                    SheetComboBox.SelectedItem?.ToString(),
                    startRow);

                System.Diagnostics.Debug.WriteLine($"로드된 쿼리 수: {_loadedQueries.Count}개");
                
                // 디버그: 로드된 쿼리 목록 출력
                for (int i = 0; i < Math.Min(_loadedQueries.Count, 5); i++)
                {
                    var q = _loadedQueries[i];
                    System.Diagnostics.Debug.WriteLine($"  [{i + 1}] {q.QueryName} (행: {q.RowNumber})");
                    System.Diagnostics.Debug.WriteLine($"      TNS/Host: {q.TnsName}{q.Host}");
                    System.Diagnostics.Debug.WriteLine($"      UserID: {q.UserId}");
                    System.Diagnostics.Debug.WriteLine($"      EnabledFlag: {q.EnabledFlag}");
                    System.Diagnostics.Debug.WriteLine($"      NotifyFlag: {q.NotifyFlag}");
                }

                // 쿼리 필터 콤보박스 초기화
                InitializeQueryFilterComboBox();

                LoadedQueriesTextBlock.Text = $"{_loadedQueries.Count}개";
                ExecuteAllButton.IsEnabled = _loadedQueries.Count > 0;
                StartAutoQueryButton.IsEnabled = _loadedQueries.Count > 0;

                UpdateStatus($"{_loadedQueries.Count}개의 쿼리를 로드했습니다.", Colors.Green);
                System.Diagnostics.Debug.WriteLine("=== 쿼리 로드 완료 ===");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"=== 쿼리 로드 실패 ===");
                System.Diagnostics.Debug.WriteLine($"오류 메시지: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"스택 트레이스:\n{ex.StackTrace}");
                
                var errorMessage = new StringBuilder();
                errorMessage.AppendLine($"쿼리 로드 실패: {ex.Message}");
                errorMessage.AppendLine();
                errorMessage.AppendLine("상세 정보:");
                errorMessage.AppendLine($"- Excel 파일: {ExcelFilePathTextBox.Text}");
                errorMessage.AppendLine($"- 시트: {SheetComboBox.SelectedItem?.ToString() ?? "(선택되지 않음)"}");
                errorMessage.AppendLine($"- 시작 행: {startRow}");
                errorMessage.AppendLine();
                errorMessage.AppendLine($"오류 상세:");
                errorMessage.AppendLine(ex.ToString());

                MessageBox.Show(errorMessage.ToString(), "쿼리 로드 오류",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                UpdateStatus($"쿼리 로드 실패: {ex.Message}", Colors.Red);
            }
        }

        /// <summary>
        /// 쿼리 필터 콤보박스를 초기화합니다.
        /// </summary>
        private void InitializeQueryFilterComboBox()
        {
            _queryFilterItems.Clear();

            // "전체" 항목 추가 (기본값: 체크 해제)
            _queryFilterItems.Add(new CheckableComboBoxItem 
            { 
                Text = "전체", 
                IsChecked = false 
            });

            // 각 쿼리를 항목으로 추가 (D열: QueryName, N열: ExcludeFlag)
            // N열이 'Y'인 쿼리만 기본 체크
            foreach (var query in _loadedQueries)
            {
                // ExcludeFlag가 'Y'이면 기본 체크 (즉, N열에 Y가 입력된 경우)
                bool isChecked = string.Equals(query.ExcludeFlag, "Y", StringComparison.OrdinalIgnoreCase);
                
                _queryFilterItems.Add(new CheckableComboBoxItem
                {
                    Text = query.QueryName,
                    IsChecked = isChecked
                });
                
                // 디버그 로그
                System.Diagnostics.Debug.WriteLine($"쿼리: {query.QueryName}, ExcludeFlag: '|{query.ExcludeFlag}|', IsChecked: {isChecked}");
            }

            QueryFilterComboBox.ItemsSource = _queryFilterItems;
            
            // 콤보박스 텍스트 업데이트 (전체 체크 여부 확인)
            UpdateQueryFilterComboBoxText();
        }

        /// <summary>
        /// 쿼리 필터 콤보박스의 표시 텍스트를 업데이트합니다.
        /// </summary>
        private void UpdateQueryFilterComboBoxText()
        {
            if (_queryFilterItems == null || _queryFilterItems.Count == 0)
            {
                QueryFilterComboBox.Text = "";
                return;
            }

            // "전체" 항목 제외하고 체크된 항목만 가져오기
            var checkedItems = _queryFilterItems.Where(item => item.IsChecked && item.Text != "전체").ToList();
            
            // 총 쿼리 개수 (전체 항목 제외)
            int totalQueries = _queryFilterItems.Count - 1;
            
            if (checkedItems.Count == 0)
            {
                // 선택된 항목이 없을 때
                QueryFilterComboBox.Text = "선택 안 됨";
                
                // "전체" 항목 체크 해제
                var allItem = _queryFilterItems.FirstOrDefault(item => item.Text == "전체");
                if (allItem != null && allItem.IsChecked)
                {
                    allItem.IsChecked = false;
                }
            }
            else if (checkedItems.Count == totalQueries)
            {
                // 모든 쿼리가 선택되었을 때
                QueryFilterComboBox.Text = "전체";
                
                // "전체" 항목도 체크
                var allItem = _queryFilterItems.FirstOrDefault(item => item.Text == "전체");
                if (allItem != null && !allItem.IsChecked)
                {
                    allItem.IsChecked = true;
                }
            }
            else
            {
                // 일부 쿼리만 선택되었을 때 - 쉼표로 연결하여 표시
                QueryFilterComboBox.Text = string.Join(", ", checkedItems.Select(item => item.Text));
                
                // "전체" 항목 체크 해제
                var allItem = _queryFilterItems.FirstOrDefault(item => item.Text == "전체");
                if (allItem != null && allItem.IsChecked)
                {
                    allItem.IsChecked = false;
                }
            }
        }

        /// <summary>
        /// 쿼리 필터 체크박스 변경 이벤트 핸들러
        /// </summary>
        private void QueryFilterCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox && 
                checkBox.DataContext is CheckableComboBoxItem item)
            {
                HandleQueryFilterCheckBoxChanged(item);
            }
        }

        /// <summary>
        /// 쿼리 필터 콤보박스 닫힘 이벤트 핸들러
        /// </summary>
        private void QueryFilterComboBox_DropDownClosed(object sender, EventArgs e)
        {
            UpdateQueryFilterComboBoxText();
        }

        /// <summary>
        /// 쿼리 필터 체크박스 변경 처리
        /// </summary>
        private void HandleQueryFilterCheckBoxChanged(CheckableComboBoxItem changedItem)
        {
            if (changedItem.Text == "전체")
            {
                // "전체"가 체크되면 모든 항목 체크, 해제되면 모든 항목 해제
                foreach (var item in _queryFilterItems.Where(i => i.Text != "전체"))
                {
                    item.IsChecked = changedItem.IsChecked;
                }
            }
            else
            {
                // 개별 항목이 변경된 경우
                var allItem = _queryFilterItems.FirstOrDefault(item => item.Text == "전체");
                if (allItem != null)
                {
                    // 모든 개별 항목이 체크되면 "전체"도 체크
                    int totalItems = _queryFilterItems.Count - 1; // "전체" 제외
                    int checkedItems = _queryFilterItems.Count(item => item.IsChecked && item.Text != "전체");
                    
                    if (checkedItems == totalItems && !allItem.IsChecked)
                    {
                        allItem.IsChecked = true;
                    }
                    else if (checkedItems < totalItems && allItem.IsChecked)
                    {
                        allItem.IsChecked = false;
                    }
                }
            }

            UpdateQueryFilterComboBoxText();
        }

        /// <summary>
        /// 선택된 쿼리 목록을 반환합니다.
        /// </summary>
        private List<QueryItem> GetSelectedQueries()
        {
            if (_queryFilterItems == null || _queryFilterItems.Count == 0)
            {
                return _loadedQueries;
            }

            var selectedQueryNames = _queryFilterItems
                .Where(item => item.IsChecked && item.Text != "전체")
                .Select(item => item.Text)
                .ToList();

            if (selectedQueryNames.Count == 0)
            {
                return new List<QueryItem>();
            }

            return _loadedQueries
                .Where(q => selectedQueryNames.Contains(q.QueryName))
                .ToList();
        }
        
        private async void ExecuteAllButton_Click(object sender, RoutedEventArgs e)
        {
            await ExecuteQueries();
        }

        private async System.Threading.Tasks.Task ExecuteQueries()
        {
            // 선택된 쿼리 가져오기
            var selectedQueries = GetSelectedQueries();
            
            if (!ValidationHelper.ValidateListNotEmpty(selectedQueries, "선택된 쿼리 목록"))
                return;

            // TNS 엔트리가 로드되지 않았으면 로드 시도
            if (_tnsEntries.Count == 0)
            {
                LoadTnsEntries();
                if (_tnsEntries.Count == 0)
                {
                    MessageBox.Show("TNS 엔트리를 로드할 수 없습니다. 설정 탭에서 tnsnames.ora 경로를 확인하세요.",
                        "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            if (_queryExecutionManager == null)
            {
                MessageBox.Show("쿼리 실행 매니저가 초기화되지 않았습니다.", "오류",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // UI 비활성화
            SetQueryExecutionUIEnabled(false);

            try
            {
                // Manager를 사용하여 선택된 쿼리만 실행
                var result = await _queryExecutionManager.ExecuteQueriesAsync(selectedQueries);

                // 상태 업데이트
                UpdateStatus(
                    $"전체 완료: 성공 {result.SuccessCount}개, 실패 {result.FailCount}개 (소요시간: {result.TotalDuration:F2}초)",
                    result.FailCount > 0 ? Colors.Orange : Colors.Green);

                // 작업 로그 탭 생성
                CreateExecutionLogTab(
                    result.ExecutionLogs,
                    result.StartTime,
                    result.TotalDuration,
                    result.SuccessCount,
                    result.FailCount,
                    result.Notifications.Count);

                // 첫 번째 탭 선택
                if (ResultTabControl.Items.Count > 0)
                {
                    ResultTabControl.SelectedIndex = 0;
                }

                // 알림 표시
                if (result.Notifications.Count > 0)
                {
                    ShowNotificationsPopup(result.Notifications);
                }
            }
            finally
            {
                // UI 활성화
                SetQueryExecutionUIEnabled(true);
            }
        }

        private void SetQueryExecutionUIEnabled(bool enabled)
        {
            ExecuteAllButton.IsEnabled = enabled;
            LoadQueriesButton.IsEnabled = enabled;
            BrowseExcelButton.IsEnabled = enabled;
        }

        private void ShowNotificationsPopup(List<string> notifications)
        {
            // 팝업이 뜨면 체크박스 설정에 따라 자동 조회 타이머 중지
            if (_isAutoQueryRunning && StopOnNotificationCheckBox.IsChecked == true)
            {
                StopAutoQuery();
            }

            var message = new StringBuilder();
            message.AppendLine("알림이 있습니다:");
            message.AppendLine();

            foreach (var notification in notifications)
            {
                message.AppendLine($"• {notification}");
            }

            MessageBox.Show(message.ToString(), "조회 결과 알림", 
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // SFC 모니터링 관련 메서드
        
        private void BrowseSfcExcelButton_Click(object sender, RoutedEventArgs e)
        {
            string? filePath = FileDialogManager.OpenExcelFileDialog("SFC 설비 목록 Excel 파일 선택");

            if (filePath != null)
            {
                SfcExcelFilePathTextBox.Text = filePath;
                LoadSfcExcelButton.IsEnabled = true;
                SaveSettings();
            }
        }

        private void LoadSfcExcelButton_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("=== SFC Excel 로드 시작 ===");
            System.Diagnostics.Debug.WriteLine($"Excel 파일 경로: {SfcExcelFilePathTextBox.Text}");

            try
            {
                var equipmentList = ExcelManager.LoadSfcEquipmentList(SfcExcelFilePathTextBox.Text);
                
                System.Diagnostics.Debug.WriteLine($"로드된 설비 수: {equipmentList.Count}개");
                
                // 디버그: 처음 5개 설비 정보 출력
                for (int i = 0; i < Math.Min(equipmentList.Count, 5); i++)
                {
                    var eq = equipmentList[i];
                    System.Diagnostics.Debug.WriteLine($"  [{i + 1}] {eq.EquipmentName} ({eq.IpAddress})");
                }

                _sfcEquipmentList.Clear();
                foreach (var item in equipmentList)
                {
                    _sfcEquipmentList.Add(item);
                }

                ExecuteSfcQueryButton.IsEnabled = _sfcEquipmentList.Count > 0;
                ApplySfcFilter();
                UpdateStatus($"{_sfcEquipmentList.Count}개의 설비 정보를 로드했습니다.", Colors.Green);
                
                System.Diagnostics.Debug.WriteLine("=== SFC Excel 로드 완료 ===");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"=== SFC Excel 로드 실패 ===");
                System.Diagnostics.Debug.WriteLine($"오류 메시지: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"스택 트레이스:\n{ex.StackTrace}");

                var errorMessage = new StringBuilder();
                errorMessage.AppendLine($"Excel 파일 로드 실패: {ex.Message}");
                errorMessage.AppendLine();
                errorMessage.AppendLine("상세 정보:");
                errorMessage.AppendLine($"- Excel 파일: {SfcExcelFilePathTextBox.Text}");
                errorMessage.AppendLine($"- 파일 존재: {File.Exists(SfcExcelFilePathTextBox.Text)}");
                errorMessage.AppendLine();
                errorMessage.AppendLine($"오류 상세:");
                errorMessage.AppendLine(ex.ToString());

                MessageBox.Show(errorMessage.ToString(), "SFC Excel 로드 오류",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                UpdateStatus($"Excel 파일 로드 실패: {ex.Message}", Colors.Red);
            }
        }

        /// <summary>
        /// SFC 쿼리 실행을 리팩토링하여 SfcQueryManager와 ValidationHelper를 사용하도록 변경
        /// </summary>
        private async void ExecuteSfcQueryButton_Click(object sender, RoutedEventArgs e)
        {
            // 입력값 검증
            if (!ValidationHelper.ValidateListNotEmpty(_sfcEquipmentList.ToList(), "설비 목록"))
                return;

            if (!ValidationHelper.ValidateSelection(SfcTnsComboBox.SelectedItem, "TNS"))
                return;

            if (!ValidationHelper.ValidateSelection(ConfigDatePicker.SelectedDate, "조회 날짜"))
                return;

            string userId = SfcUserIdTextBox.Text?.Trim() ?? "";
            string password = SfcPasswordBox.Password ?? "";

            if (!ValidationHelper.ValidateNotEmpty(userId, "User ID"))
            {
                SfcUserIdTextBox.Focus();
                return;
            }

            if (!ValidationHelper.ValidateNotEmpty(password, "Password"))
            {
                SfcPasswordBox.Focus();
                return;
            }

            if (_sfcQueryManager == null)
            {
                MessageBox.Show("SFC 쿼리 매니저가 초기화되지 않았습니다.", "오류",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                ExecuteSfcQueryButton.IsEnabled = false;
                SaveSettings();

                // SfcQueryManager를 사용하여 쿼리 실행
                var result = await _sfcQueryManager.ExecuteQueryAsync(
                    SfcTnsComboBox.SelectedItem.ToString() ?? "",
                    userId,
                    password,
                    SfcQueryTextBox.Text,
                    ConfigDatePicker.SelectedDate!.Value,
                    _sfcEquipmentList.ToList());

                if (result != null)
                {
                    // 결과 처리
                    _sfcQueryManager.ProcessQueryResult(result, _sfcEquipmentList.ToList());

                    // UI 업데이트
                    SfcMonitorDataGrid.Items.Refresh();
                    ApplySfcDataGridRowStyle();
                    ApplySfcFilter();

                    // 요약 생성
                    var summary = SfcQueryManager.GetResultSummary(_sfcEquipmentList.ToList());
                    UpdateStatus(summary.GetSummaryMessage(), Colors.Green);

                    // OFF 상태인 설비 알림
                    ShowOffEquipmentNotification();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"쿼리 실행 실패:\n{ex.Message}", "오류",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                UpdateStatus($"쿼리 실행 실패: {ex.Message}", Colors.Red);
            }
            finally
            {
                ExecuteSfcQueryButton.IsEnabled = true;
            }
        }

        /// <summary>
        /// OFF 상태인 설비에 대한 알림을 표시합니다.
        /// </summary>
        private void ShowOffEquipmentNotification()
        {
            var offEquipments = _sfcEquipmentList.Where(e => e.Status == "OFF").ToList();

            if (offEquipments.Count > 0)
            {
                var message = new StringBuilder();
                message.AppendLine($"⚠️ OFF 상태 설비 {offEquipments.Count}개 발견");
                message.AppendLine();

                foreach (var equipment in offEquipments)
                {
                    message.AppendLine($"• {equipment.EquipmentName} ({equipment.IpAddress})");
                }

                MessageBox.Show(message.ToString(), "SFC 설비 상태 알림",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void StartAutoQueryButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedQueries = GetSelectedQueries();
            if (!ValidationHelper.ValidateListNotEmpty(selectedQueries, "선택된 쿼리 목록"))
                return;

            if (!ValidationHelper.ValidateQueryInterval(QueryIntervalTextBox.Text, out int interval))
                return;

            SaveSettings();
            StartAutoQuery(interval);
        }

        private void StopAutoQueryButton_Click(object sender, RoutedEventArgs e)
        {
            StopAutoQuery();
        }

        private void StartAutoQuery(int intervalSeconds)
        {
            _isAutoQueryRunning = true;
            _totalIntervalSeconds = intervalSeconds;
            _remainingSeconds = intervalSeconds;
            
            // 쿼리 실행 타이머 설정
            _queryTimer = new DispatcherTimer();
            _queryTimer.Interval = TimeSpan.FromSeconds(intervalSeconds);
            _queryTimer.Tick += async (s, e) =>
            {
                _remainingSeconds = _totalIntervalSeconds; // 타이머 리셋
                await ExecuteQueries();
            };
            _queryTimer.Start();

            // 카운트다운 타이머 설정 (1초마다 업데이트)
            _countdownTimer = new DispatcherTimer();
            _countdownTimer.Interval = TimeSpan.FromSeconds(1);
            _countdownTimer.Tick += CountdownTimer_Tick;
            _countdownTimer.Start();

            // 즉시 한 번 실행
            _ = ExecuteQueries();

            StartAutoQueryButton.IsEnabled = false;
            StopAutoQueryButton.IsEnabled = true;
            QueryIntervalTextBox.IsEnabled = false;
            LoadQueriesButton.IsEnabled = false;
            BrowseExcelButton.IsEnabled = false;

            // 카운트다운 표시 활성화
            AutoQueryCountdownBorder.Visibility = Visibility.Visible;
            UpdateCountdownDisplay();

            UpdateStatus($"자동 쿼리 실행 시작 (주기: {intervalSeconds}초)", Colors.Green);
        }

        private void CountdownTimer_Tick(object? sender, EventArgs e)
        {
            if (_remainingSeconds > 0)
            {
                _remainingSeconds--;
                UpdateCountdownDisplay();
            }
        }

        private void UpdateCountdownDisplay()
        {
            if (_remainingSeconds > 0)
            {
                int minutes = _remainingSeconds / 60;
                int seconds = _remainingSeconds % 60;
                
                if (minutes > 0)
                {
                    AutoQueryCountdownTextBlock.Text = $"{minutes}분 {seconds}초";
                }
                else
                {
                    AutoQueryCountdownTextBlock.Text = $"{seconds}초";
                }
            }
            else
            {
                AutoQueryCountdownTextBlock.Text = "실행 중...";
            }
        }

        private void StopAutoQuery()
        {
            if (_queryTimer != null)
            {
                _queryTimer.Stop();
                _queryTimer = null;
            }

            if (_countdownTimer != null)
            {
                _countdownTimer.Stop();
                _countdownTimer = null;
            }

            _isAutoQueryRunning = false;
            _remainingSeconds = 0;

            // 카운트다운 텍스트 초기화 및 숨기기
            AutoQueryCountdownTextBlock.Text = "";
            AutoQueryCountdownBorder.Visibility = Visibility.Collapsed;

            StartAutoQueryButton.IsEnabled = _loadedQueries.Count > 0;
            StopAutoQueryButton.IsEnabled = false;
            QueryIntervalTextBox.IsEnabled = true;
            LoadQueriesButton.IsEnabled = true;
            BrowseExcelButton.IsEnabled = true;

            UpdateStatus("자동 쿼리 실행 중지", Colors.Orange);
        }

        private void CreateResultTab(QueryItem queryItem, DataTable? result, double duration, string? errorMessage)
        {
            var tabItem = new TabItem
            {
                Header = queryItem.QueryName
            };

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            if (result != null && errorMessage == null)
            {
                // 성공 - 데이터 그리드 생성
                var dataGrid = new DataGrid
                {
                    AutoGenerateColumns = true,
                    IsReadOnly = true,
                    AlternatingRowBackground = new SolidColorBrush(Color.FromRgb(240, 240, 240)),
                    GridLinesVisibility = DataGridGridLinesVisibility.All,
                    HeadersVisibility = DataGridHeadersVisibility.All,
                    ItemsSource = result.DefaultView,
                    CanUserSortColumns = true,
                    CanUserResizeColumns = true,
                    CanUserReorderColumns = true,
                    SelectionMode = DataGridSelectionMode.Extended,
                    SelectionUnit = DataGridSelectionUnit.Cell,  // 셀 단위 선택 가능
                    ClipboardCopyMode = DataGridClipboardCopyMode.IncludeHeader  // 헤더 포함 복사
                };

                // 컨텍스트 메뉴 추가 (마우스 우클릭)
                var contextMenu = new ContextMenu();
                
                var copyMenuItem = new MenuItem
                {
                    Header = "복사 (Ctrl+C)"
                };
                copyMenuItem.Click += (s, e) =>
                {
                    try
                    {
                        if (dataGrid.SelectedCells.Count > 0)
                        {
                            // 선택된 셀들의 내용을 클립보드에 복사
                            dataGrid.ClipboardCopyMode = DataGridClipboardCopyMode.ExcludeHeader;
                            ApplicationCommands.Copy.Execute(null, dataGrid);
                            dataGrid.UnselectAllCells();
                            UpdateStatus("선택한 셀이 복사되었습니다.", Colors.Green);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"복사 실패:\n{ex.Message}", "오류",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                };
                contextMenu.Items.Add(copyMenuItem);

                var copyWithHeaderMenuItem = new MenuItem
                {
                    Header = "헤더 포함 복사"
                };
                copyWithHeaderMenuItem.Click += (s, e) =>
                {
                    try
                    {
                        if (dataGrid.SelectedCells.Count > 0)
                        {
                            // 헤더 포함하여 복사
                            dataGrid.ClipboardCopyMode = DataGridClipboardCopyMode.IncludeHeader;
                            ApplicationCommands.Copy.Execute(null, dataGrid);
                            dataGrid.UnselectAllCells();
                            UpdateStatus("헤더 포함하여 복사되었습니다.", Colors.Green);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"복사 실패:\n{ex.Message}", "오류",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                };
                contextMenu.Items.Add(copyWithHeaderMenuItem);

                contextMenu.Items.Add(new Separator());

                var selectAllMenuItem = new MenuItem
                {
                    Header = "모두 선택 (Ctrl+A)"
                };
                selectAllMenuItem.Click += (s, e) =>
                {
                    dataGrid.SelectAllCells();
                };
                contextMenu.Items.Add(selectAllMenuItem);

                dataGrid.ContextMenu = contextMenu;

                // Ctrl+C 키보드 단축키 지원
                dataGrid.PreviewKeyDown += (s, e) =>
                {
                    if (e.Key == Key.C && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                    {
                        try
                        {
                            if (dataGrid.SelectedCells.Count > 0)
                            {
                                dataGrid.ClipboardCopyMode = DataGridClipboardCopyMode.ExcludeHeader;
                                ApplicationCommands.Copy.Execute(null, dataGrid);
                                e.Handled = true;
                            }
                        }
                        catch { }
                    }
                };

                Grid.SetRow(dataGrid, 0);
                grid.Children.Add(dataGrid);

                // 상태 표시
                var statusPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Margin = new Thickness(5)
                };

                statusPanel.Children.Add(new TextBlock
                {
                    Text = $"✓ {result.Rows.Count}개 행 | {result.Columns.Count}개 열 | 소요시간: {duration:F2}초",
                    FontSize = 11,
                    Foreground = new SolidColorBrush(Colors.Green),
                    VerticalAlignment = VerticalAlignment.Center
                });

                // DB 연결 정보 표시
                if (!string.IsNullOrEmpty(queryItem.TnsName))
                {
                    statusPanel.Children.Add(new TextBlock
                    {
                        Text = $" | TNS: {queryItem.TnsName}, User: {queryItem.UserId}",
                        FontSize = 11,
                        Foreground = new SolidColorBrush(Colors.Blue),
                        Margin = new Thickness(5, 0, 0, 0),
                        VerticalAlignment = VerticalAlignment.Center
                    });
                }
                else if (!string.IsNullOrEmpty(queryItem.Host))
                {
                    statusPanel.Children.Add(new TextBlock
                    {
                        Text = $" | DB: {queryItem.Host}:{queryItem.Port}/{queryItem.ServiceName}, User: {queryItem.UserId}",
                        FontSize = 11,
                        Foreground = new SolidColorBrush(Colors.Blue),
                        Margin = new Thickness(5, 0, 0, 0),
                        VerticalAlignment = VerticalAlignment.Center
                    });
                }

                Grid.SetRow(statusPanel, 1);
                grid.Children.Add(statusPanel);
            }
            else
            {
                // 오류 - 오류 메시지 표시
                var errorInfo = new StringBuilder();
                errorInfo.AppendLine("쿼리 실행 실패");
                errorInfo.AppendLine();
                errorInfo.AppendLine($"오류: {errorMessage}");
                errorInfo.AppendLine();
                
                if (!string.IsNullOrEmpty(queryItem.TnsName))
                {
                    errorInfo.AppendLine($"TNS: {queryItem.TnsName}");
                }
                else if (!string.IsNullOrEmpty(queryItem.Host))
                {
                    errorInfo.AppendLine($"DB: {queryItem.Host}:{queryItem.Port}/{queryItem.ServiceName}");
                }
                
                errorInfo.AppendLine($"User ID: {queryItem.UserId}");
                errorInfo.AppendLine();
                errorInfo.AppendLine("쿼리:");
                errorInfo.AppendLine(queryItem.Query);

                var errorTextBox = new TextBox
                {
                    Text = errorInfo.ToString(),
                    IsReadOnly = true,
                    Background = new SolidColorBrush(Color.FromRgb(255, 240, 240)),
                    Foreground = new SolidColorBrush(Colors.Red),
                    TextWrapping = TextWrapping.Wrap,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    FontFamily = new FontFamily("Consolas"),
                    Padding = new Thickness(10)
                };

                Grid.SetRow(errorTextBox, 0);
                grid.Children.Add(errorTextBox);

                var statusText = new TextBlock
                {
                    Text = "✗ 실행 실패",
                    FontSize = 11,
                    Foreground = new SolidColorBrush(Colors.Red),
                    Margin = new Thickness(5)
                };

                Grid.SetRow(statusText, 1);
                grid.Children.Add(statusText);

                // 탭 헤더를 빨간색으로 표시
                tabItem.Foreground = new SolidColorBrush(Colors.Red);
            }

            tabItem.Content = grid;
            ResultTabControl.Items.Add(tabItem);
        }

        private void CreateExecutionLogTab(List<string> logs, DateTime startTime, double totalDuration, int successCount, int failCount, int notificationCount)
        {
            var tabItem = new TabItem
            {
                Header = "📋 작업 로그",
                FontWeight = FontWeights.Bold
            };

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            // 상단 요약 패널
            var summaryBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(240, 248, 255)),
                Padding = new Thickness(15, 10, 15, 10),
                Margin = new Thickness(0, 0, 0, 10)
            };

            var summaryPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal
            };

            // 시작 시간
            summaryPanel.Children.Add(new TextBlock
            {
                Text = $"⏰ 시작: {startTime:HH:mm:ss}",
                FontSize = 12,
                Margin = new Thickness(0, 0, 20, 0),
                VerticalAlignment = VerticalAlignment.Center
            });

            // 소요 시간
            summaryPanel.Children.Add(new TextBlock
            {
                Text = $"⏱️ 소요시간: {totalDuration:F2}초",
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Colors.Blue),
                Margin = new Thickness(0, 0, 20, 0),
                VerticalAlignment = VerticalAlignment.Center
            });

            // 성공
            summaryPanel.Children.Add(new TextBlock
            {
                Text = $"✅ 성공: {successCount}개",
                FontSize = 12,
                Foreground = new SolidColorBrush(Colors.Green),
                Margin = new Thickness(0, 0, 20, 0),
                VerticalAlignment = VerticalAlignment.Center
            });

            // 실패
            if (failCount > 0)
            {
                summaryPanel.Children.Add(new TextBlock
                {
                    Text = $"❌ 실패: {failCount}개",
                    FontSize = 12,
                    Foreground = new SolidColorBrush(Colors.Red),
                    Margin = new Thickness(0, 0, 20, 0),
                    VerticalAlignment = VerticalAlignment.Center
                });
            }

            // 알림
            if (notificationCount > 0)
            {
                summaryPanel.Children.Add(new TextBlock
                {
                    Text = $"🔔 알림: {notificationCount}개",
                    FontSize = 12,
                    Foreground = new SolidColorBrush(Colors.Orange),
                    Margin = new Thickness(0, 0, 20, 0),
                    VerticalAlignment = VerticalAlignment.Center
                });
            }

            summaryBorder.Child = summaryPanel;
            Grid.SetRow(summaryBorder, 0);
            grid.Children.Add(summaryBorder);

            // 로그 내용
            var logTextBox = new TextBox
            {
                Text = string.Join(Environment.NewLine, logs),
                IsReadOnly = true,
                FontFamily = new FontFamily("Consolas"),
                FontSize = 11,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                TextWrapping = TextWrapping.NoWrap,
                Padding = new Thickness(10),
                Background = new SolidColorBrush(Color.FromRgb(250, 250, 250))
            };

            Grid.SetRow(logTextBox, 1);
            grid.Children.Add(logTextBox);

            tabItem.Content = grid;
            
            // 맨 앞에 삽입
            ResultTabControl.Items.Insert(0, tabItem);
        }

        private void ClearResultsButton_Click(object sender, RoutedEventArgs e)
        {
            ResultTabControl.Items.Clear();
            UpdateStatus("결과 탭이 초기화되었습니다.", Colors.Gray);
        }

        private void ClearFilterButton_Click(object sender, RoutedEventArgs e)
        {
            if (_sfcFilterManager == null)
                return;

            // 텍스트 필터 초기화
            FilterIpTextBox.Text = "";
            FilterEquipmentIdTextBox.Text = "";
            FilterEquipmentNameTextBox.Text = "";

            // 필터 매니저를 사용하여 초기화
            _sfcFilterManager.ClearAllFilters();
            UpdateFilterComboBoxText();

            // 필터 적용
            ApplySfcFilter();
            UpdateStatus("필터가 초기화되었습니다.", Colors.Green);
        }

        private void UpdateStatus(string message, Color color)
        {
            StatusTextBlock.Text = message;
            StatusTextBlock.Foreground = new SolidColorBrush(color);
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            string? filePath = FileDialogManager.OpenTnsFileDialog(
                Path.GetDirectoryName(_settings.TnsPath) ?? "");

            if (filePath != null)
            {
                TnsPathTextBox.Text = filePath;
            }
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            TnsPathTextBox.Text = SettingsManager.GetDefaultTnsPath();
            UpdateStatus("TNS 경로가 기본값으로 복원되었습니다.", Colors.Green);
        }

        private void SaveSettingsButton_Click(object sender, RoutedEventArgs e)
        {
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

            _settings.TnsPath = TnsPathTextBox.Text;
            SettingsManager.SaveSettings(_settings);
            _queryExecutionManager?.UpdateSettings(_settings);
            LoadTnsEntries();

            UpdateStatus("설정이 저장되었습니다.", Colors.Green);
            MessageBox.Show("설정이 저장되었습니다.", "완료",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private const string DEFAULT_SFC_QUERY = "SELECT * FROM TB_SFC_MAINFRAME_CONFIG_N A WHERE A.TRANSACTION_TYPE_CODE = 'LOGIN_AUTO' AND SFC_MODE = 'PROD' AND PC_IP_ADDR IN (@PC_IP_ADDR) /*AND A.CONFIG_REGISTER_YMD = @CONFIG_REGISTER_YMD*/";
        
        private void ResetSfcQueryButton_Click(object sender, RoutedEventArgs e)
        {
            SfcQueryTextBox.Text = DEFAULT_SFC_QUERY;
            UpdateStatus("SFC 쿼리가 기본값으로 초기화되었습니다.", Colors.Green);
        }

        // SFC 필터링 관련 메서드
        
        /// <summary>
        /// 필터를 적용합니다 (XAML TextChanged 이벤트에서 호출됨).
        /// </summary>
        private void ApplyFilter(object sender, EventArgs e)
        {
            ApplySfcFilter();
        }

        /// <summary>
        /// SFC 설비 목록에 필터를 적용합니다.
        /// </summary>
        private void ApplySfcFilter()
        {
            // UI 컨트롤이 초기화되지 않았으면 리턴
            if (_sfcFilterManager == null ||
                FilterIpTextBox == null || 
                FilterEquipmentIdTextBox == null || 
                FilterEquipmentNameTextBox == null)
                return;

            // 콤보박스 텍스트 업데이트
            UpdateFilterComboBoxText();

            // 필터 조건 생성
            var criteria = new SfcFilterManager.FilterCriteria
            {
                IpAddress = FilterIpTextBox.Text,
                EquipmentId = FilterEquipmentIdTextBox.Text,
                EquipmentName = FilterEquipmentNameTextBox.Text
            };

            // 필터 적용
            var result = _sfcFilterManager.ApplyFilter(criteria);

            // 필터 상태 업데이트
            UpdateFilterStatus(result);
        }

        /// <summary>
        /// 필터 상태 메시지를 업데이트합니다.
        /// </summary>
        private void UpdateFilterStatus(FilterResult result)
        {
            // FilterStatusTextBlock이 초기화되지 않았으면 리턴
            if (FilterStatusTextBlock == null || result == null)
                return;

            FilterStatusTextBlock.Text = result.GetStatusMessage();
            FilterStatusTextBlock.Foreground = new SolidColorBrush(
                result.IsFiltered ? Colors.Blue : Colors.Gray);
        }

        /// <summary>
        /// SFC DataGrid 행 스타일을 적용합니다.
        /// </summary>
        private void ApplySfcDataGridRowStyle()
        {
            // DataGrid 새로고침을 위한 이벤트 핸들러 등록
            SfcMonitorDataGrid.LoadingRow -= SfcMonitorDataGrid_LoadingRow;
            SfcMonitorDataGrid.LoadingRow += SfcMonitorDataGrid_LoadingRow;
        }

        /// <summary>
        /// SFC DataGrid 행 로딩 시 스타일을 적용합니다.
        /// </summary>
        private void SfcMonitorDataGrid_LoadingRow(object? sender, DataGridRowEventArgs e)
        {
            var item = e.Row.Item as SfcEquipmentInfo;
            if (item != null && item.Status == "OFF")
            {
                e.Row.Background = new SolidColorBrush(Colors.LightCoral);
            }
            else
            {
                e.Row.Background = new SolidColorBrush(Colors.White);
            }
        }

    }
}