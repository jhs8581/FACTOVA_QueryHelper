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
        private bool _isAutoQueryRunning = false;
        private ObservableCollection<SfcEquipmentInfo> _sfcEquipmentList;
        private ObservableCollection<SfcEquipmentInfo> _sfcFilteredList;
        private ObservableCollection<CheckableComboBoxItem> _statusFilterItems;
        private ObservableCollection<CheckableComboBoxItem> _bizActorFilterItems;
        private SfcFilterManager? _sfcFilterManager;

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
                if (_tnsEntries.Count > 0)
                {
                    SfcTnsComboBox.SelectedIndex = 0;
                }
            }
        }

        private void InitializeSfcFilterComboBoxes()
        {
            // 상태 필터 초기화 (ON, OFF)
            _statusFilterItems.Add(new CheckableComboBoxItem { Text = "전체", IsChecked = true });
            _statusFilterItems.Add(new CheckableComboBoxItem { Text = "ON", IsChecked = false });
            _statusFilterItems.Add(new CheckableComboBoxItem { Text = "OFF", IsChecked = false });
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

            // 쿼리 타이머 간격 저장
            if (int.TryParse(QueryIntervalTextBox.Text, out int interval))
            {
                _settings.QueryIntervalSeconds = interval;
            }

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

        /// <summary>
        /// 파일 대화상자를 열고 선택된 파일 경로를 반환합니다.
        /// </summary>
        private string? OpenFileDialog(string filter, string title)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = filter,
                Title = title
            };

            return openFileDialog.ShowDialog() == true ? openFileDialog.FileName : null;
        }

        /// <summary>
        /// Excel 파일의 시트 목록을 로드합니다.
        /// </summary>
        private void LoadExcelSheets(string filePath)
        {
            try
            {
                var sheets = ExcelQueryReader.GetSheetNames(filePath);
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
        }

        private void BrowseExcelButton_Click(object sender, RoutedEventArgs e)
        {
            string? filePath = OpenFileDialog(
                "Excel Files (*.xlsx;*.xls)|*.xlsx;*.xls|All Files (*.*)|*.*",
                "Excel 쿼리 파일 선택");

            if (filePath != null)
            {
                ExcelFilePathTextBox.Text = filePath;
                LoadQueriesButton.IsEnabled = true;
                LoadExcelSheets(filePath);
                SaveSettings();
            }
        }

        /// <summary>
        /// 쿼리 로드 시작 행 번호를 검증합니다.
        /// </summary>
        private bool ValidateStartRow(out int startRow)
        {
            startRow = 2;
            
            if (!int.TryParse(StartRowTextBox.Text, out startRow) || startRow < 1)
            {
                MessageBox.Show("시작 행 번호는 1 이상이어야 합니다.", "오류",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            
            return true;
        }

        private void LoadQueriesButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!ValidateStartRow(out int startRow))
                {
                    return;
                }

                string filePath = ExcelFilePathTextBox.Text;
                string? sheetName = SheetComboBox.SelectedItem?.ToString();

                // 쿼리 로드 - 열 고정: TNS=A, UserID=B, Password=C, 탭이름=D, 쿼리=F
                // 추가 열: G(실행여부), H(알림여부), I(이상), J(같음), K(이하), L(컬럼명), M(컬럼값), N(제외여부)
                _loadedQueries = ExcelQueryReader.ReadQueriesFromExcel(
                    filePath,
                    sheetName,
                    "F",     // 쿼리 (고정)
                    "D",     // 탭 이름 (고정)
                    "",      // 설명 열 사용 안 함
                    "A",     // TNS (고정)
                    "B",     // User ID (고정)
                    "C",     // Password (고정)
                    startRow,
                    "G",  // 실행 여부
                    "H",  // 알림 여부
                    "I",  // 이상
                    "J",  // 같음
                    "K",  // 이하
                    "L",  // 컬럼명
                    "M",  // 컬럼값
                    "N"); // 제외 여부

                // N열이 'N'인 쿼리는 제외
                _loadedQueries = _loadedQueries.Where(q => q.ExcludeFlag != "N").ToList();

                LoadedQueriesTextBlock.Text = $"{_loadedQueries.Count}개";
                ExecuteAllButton.IsEnabled = _loadedQueries.Count > 0;
                StartAutoQueryButton.IsEnabled = _loadedQueries.Count > 0;

                UpdateStatus($"{_loadedQueries.Count}개의 쿼리를 로드했습니다.", Colors.Green);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"쿼리 로드 실패:\n{ex.Message}", "오류",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                UpdateStatus($"쿼리 로드 실패: {ex.Message}", Colors.Red);
            }
        }

        private async void ExecuteAllButton_Click(object sender, RoutedEventArgs e)
        {
            await ExecuteQueries();
        }

        private async System.Threading.Tasks.Task ExecuteQueries()
        {
            if (_loadedQueries.Count == 0)
            {
                MessageBox.Show("먼저 Excel에서 쿼리를 로드하세요.", "알림", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

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

            // UI 비활성화
            ExecuteAllButton.IsEnabled = false;
            LoadQueriesButton.IsEnabled = false;
            BrowseExcelButton.IsEnabled = false;

            // 기존 결과 탭 초기화
            ResultTabControl.Items.Clear();

            var totalStartTime = DateTime.Now;
            int successCount = 0;
            int failCount = 0;
            List<string> notifications = new List<string>();
            List<string> executionLogs = new List<string>();

            // 작업 로그 헤더 추가
            executionLogs.Add($"작업 시작 시간: {totalStartTime:yyyy-MM-dd HH:mm:ss}");
            executionLogs.Add($"로드된 쿼리 수: {_loadedQueries.Count}개");
            executionLogs.Add(new string('=', 80));
            executionLogs.Add("");

            // G열이 'Y'인 쿼리만 필터링
            var queriesToExecute = _loadedQueries.Where(q => 
                string.IsNullOrWhiteSpace(q.EnabledFlag) || q.EnabledFlag == "Y").ToList();

            executionLogs.Add($"실행 대상 쿼리: {queriesToExecute.Count}개");
            executionLogs.Add("");

            for (int i = 0; i < queriesToExecute.Count; i++)
            {
                var queryItem = queriesToExecute[i];
                
                UpdateStatus($"쿼리 실행 중... ({i + 1}/{queriesToExecute.Count}) - {queryItem.QueryName}", Colors.Blue);

                var logEntry = new StringBuilder();
                logEntry.AppendLine($"[{i + 1}/{queriesToExecute.Count}] {queryItem.QueryName}");
                logEntry.AppendLine($"  시작 시간: {DateTime.Now:HH:mm:ss}");

                try
                {
                    string connectionString;
                    
                    // 직접 연결 정보가 있는지 확인 (Host:Port:ServiceName 형식)
                    if (!string.IsNullOrWhiteSpace(queryItem.Host) && 
                        !string.IsNullOrWhiteSpace(queryItem.Port) && 
                        !string.IsNullOrWhiteSpace(queryItem.ServiceName))
                    {
                        // 직접 연결 문자열 생성
                        connectionString = $"Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST={queryItem.Host})(PORT={queryItem.Port}))(CONNECT_DATA=(SERVICE_NAME={queryItem.ServiceName})));";
                        
                        logEntry.AppendLine($"  연결: {queryItem.Host}:{queryItem.Port}/{queryItem.ServiceName}");
                        System.Diagnostics.Debug.WriteLine($"직접 연결 사용: {queryItem.Host}:{queryItem.Port}:{queryItem.ServiceName}");
                    }
                    else
                    {
                        // TNS 정보 찾기
                        TnsEntry? selectedTns = null;
                        if (!string.IsNullOrWhiteSpace(queryItem.TnsName))
                        {
                            selectedTns = _tnsEntries.FirstOrDefault(t => 
                                t.Name.Equals(queryItem.TnsName, StringComparison.OrdinalIgnoreCase));
                        }

                        if (selectedTns == null)
                        {
                            // 사용 가능한 TNS 목록 표시
                            var availableTns = string.Join(", ", _tnsEntries.Select(t => t.Name));
                            throw new Exception($"TNS '{queryItem.TnsName}'를 찾을 수 없습니다.\n\n" +
                                $"💡 해결 방법:\n" +
                                $"1. Excel A열에 정확한 TNS 이름 입력\n" +
                                $"2. 또는 Host:Port:ServiceName 형식으로 입력\n" +
                                $"   예) 192.168.1.10:1521:ORCL\n\n" +
                                $"사용 가능한 TNS 목록:\n{availableTns}\n\n" +
                                $"tnsnames.ora 파일 경로:\n{_settings.TnsPath}");
                        }
                        
                        connectionString = selectedTns.ConnectionString;
                        logEntry.AppendLine($"  TNS: {queryItem.TnsName}");
                    }

                    // User ID와 Password 검증
                    if (string.IsNullOrWhiteSpace(queryItem.UserId))
                    {
                        throw new Exception("User ID가 지정되지 않았습니다.");
                    }

                    if (string.IsNullOrWhiteSpace(queryItem.Password))
                    {
                        throw new Exception("Password가 지정되지 않았습니다.");
                    }

                    logEntry.AppendLine($"  사용자: {queryItem.UserId}");

                    var startTime = DateTime.Now;

                    // 쿼리 실행
                    DataTable result = await OracleDatabase.ExecuteQueryAsync(
                        connectionString,
                        queryItem.UserId,
                        queryItem.Password,
                        queryItem.Query);

                    var endTime = DateTime.Now;
                    var duration = (endTime - startTime).TotalSeconds;

                    logEntry.AppendLine($"  완료 시간: {endTime:HH:mm:ss}");
                    logEntry.AppendLine($"  소요 시간: {duration:F2}초");
                    logEntry.AppendLine($"  결과: {result.Rows.Count}행 × {result.Columns.Count}열");

                    // 결과 건수 체크 및 알림
                    var itemNotifications = new List<string>();
                    CheckResultCountAndNotify(queryItem, result.Rows.Count, itemNotifications);

                    // 특정 컬럼 값 체크 및 알림
                    CheckColumnValuesAndNotify(queryItem, result, itemNotifications);

                    if (itemNotifications.Count > 0)
                    {
                        notifications.AddRange(itemNotifications);
                        logEntry.AppendLine($"  🔔 알림: {itemNotifications.Count}개");
                        foreach (var notif in itemNotifications)
                        {
                            logEntry.AppendLine($"    - {notif.Replace($"[{queryItem.QueryName}] ", "")}");
                        }
                    }

                    logEntry.AppendLine($"  ✅ 성공");

                    // 결과 탭 생성
                    CreateResultTab(queryItem, result, duration, null);

                    successCount++;
                }
                catch (Exception ex)
                {
                    logEntry.AppendLine($"  ❌ 실패: {ex.Message}");
                    
                    // 오류 탭 생성
                    CreateResultTab(queryItem, null, 0, ex.Message);
                    failCount++;
                }

                executionLogs.Add(logEntry.ToString());
            }

            var totalDuration = (DateTime.Now - totalStartTime).TotalSeconds;

            // 작업 요약 추가
            executionLogs.Add(new string('=', 80));
            executionLogs.Add("");
            executionLogs.Add("📊 작업 요약");
            executionLogs.Add($"  총 실행 시간: {totalDuration:F2}초");
            executionLogs.Add($"  성공: {successCount}개");
            executionLogs.Add($"  실패: {failCount}개");
            executionLogs.Add($"  알림: {notifications.Count}개");
            executionLogs.Add("");
            executionLogs.Add($"작업 완료 시간: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

            // UI 활성화
            ExecuteAllButton.IsEnabled = true;
            LoadQueriesButton.IsEnabled = true;
            BrowseExcelButton.IsEnabled = true;

            UpdateStatus($"전체 완료: 성공 {successCount}개, 실패 {failCount}개 (소요시간: {totalDuration:F2}초)", 
                failCount > 0 ? Colors.Orange : Colors.Green);

            // 작업 로그 탭을 맨 앞에 추가
            CreateExecutionLogTab(executionLogs, totalStartTime, totalDuration, successCount, failCount, notifications.Count);

            // 첫 번째 탭(작업 로그) 선택
            if (ResultTabControl.Items.Count > 0)
            {
                ResultTabControl.SelectedIndex = 0;
            }

            // 알림이 있으면 팝업 표시
            if (notifications.Count > 0)
            {
                ShowNotificationsPopup(notifications);
            }
        }

        private void CheckResultCountAndNotify(QueryItem queryItem, int rowCount, List<string> notifications)
        {
            // H열이 'Y'가 아니면 알림을 추가하지 않음
            if (queryItem.NotifyFlag != "Y")
                return;

            // I열: 이상일 때
            if (!string.IsNullOrWhiteSpace(queryItem.CountGreaterThan) && 
                int.TryParse(queryItem.CountGreaterThan, out int greaterThan))
            {
                if (rowCount >= greaterThan)
                {
                    notifications.Add($"[{queryItem.QueryName}] 조회 결과 {rowCount}건 (기준: {greaterThan}건 이상)");
                }
            }

            // J열: 같을 때
            if (!string.IsNullOrWhiteSpace(queryItem.CountEquals) && 
                int.TryParse(queryItem.CountEquals, out int equals))
            {
                if (rowCount == equals)
                {
                    notifications.Add($"[{queryItem.QueryName}] 조회 결과 {rowCount}건 (기준: {equals}건과 같음)");
                }
            }

            // K열: 이하일 때
            if (!string.IsNullOrWhiteSpace(queryItem.CountLessThan) && 
                int.TryParse(queryItem.CountLessThan, out int lessThan))
            {
                if (rowCount <= lessThan)
                {
                    notifications.Add($"[{queryItem.QueryName}] 조회 결과 {rowCount}건 (기준: {lessThan}건 이하)");
                }
            }
        }

        private void CheckColumnValuesAndNotify(QueryItem queryItem, DataTable result, List<string> notifications)
        {
            // H열이 'Y'가 아니면 알림을 추가하지 않음
            if (queryItem.NotifyFlag != "Y")
                return;

            // L열과 M열 체크
            if (string.IsNullOrWhiteSpace(queryItem.ColumnNames) || 
                string.IsNullOrWhiteSpace(queryItem.ColumnValues))
            {
                return;
            }

            // 컬럼명과 값을 쉼표로 분리
            var columnNames = queryItem.ColumnNames.Split(',').Select(c => c.Trim()).ToList();
            var columnValues = queryItem.ColumnValues.Split(',').Select(v => v.Trim()).ToList();

            // 개수가 다르면 처리 안 함
            if (columnNames.Count != columnValues.Count)
            {
                return;
            }

            // 각 행을 검사
            for (int i = 0; i < result.Rows.Count; i++)
            {
                var row = result.Rows[i];
                bool allMatch = true;

                for (int j = 0; j < columnNames.Count; j++)
                {
                    string columnName = columnNames[j];
                    string expectedValue = columnValues[j];

                    // 컬럼이 존재하는지 확인
                    if (!result.Columns.Contains(columnName))
                    {
                        allMatch = false;
                        break;
                    }

                    var actualValue = row[columnName]?.ToString()?.Trim() ?? "";
                    if (actualValue != expectedValue)
                    {
                        allMatch = false;
                        break;
                    }
                }

                if (allMatch)
                {
                    var matchInfo = string.Join(", ", columnNames.Zip(columnValues, (n, v) => $"{n}={v}"));
                    notifications.Add($"[{queryItem.QueryName}] 조건 일치 발견 (행 {i + 1}): {matchInfo}");
                }
            }
        }

        private void ShowNotificationsPopup(List<string> notifications)
        {
            // 팝업이 뜨면 자동 조회 타이머 중지
            if (_isAutoQueryRunning)
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
            string? filePath = OpenFileDialog(
                "Excel Files (*.xlsx;*.xls)|*.xlsx;*.xls|All Files (*.*)|*.*",
                "SFC 설비 목록 Excel 파일 선택");

            if (filePath != null)
            {
                SfcExcelFilePathTextBox.Text = filePath;
                LoadSfcExcelButton.IsEnabled = true;
                SaveSettings();
            }
        }

        /// <summary>
        /// SFC Excel 파일을 로드합니다.
        /// </summary>
        private bool LoadSfcEquipmentFromExcel(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    MessageBox.Show("Excel 파일을 찾을 수 없습니다.", "오류",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }

                _sfcEquipmentList.Clear();

                using (var package = new ExcelPackage(new FileInfo(filePath)))
                {
                    var worksheet = package.Workbook.Worksheets[0];
                    int rowCount = worksheet.Dimension?.End.Row ?? 0;

                    // 2행부터 데이터 읽기 (1행은 헤더)
                    for (int row = 2; row <= rowCount; row++)
                    {
                        var ipAddress = worksheet.Cells[row, 1].Text?.Trim();
                        var equipmentId = worksheet.Cells[row, 2].Text?.Trim();
                        var equipmentName = worksheet.Cells[row, 3].Text?.Trim();

                        if (!string.IsNullOrWhiteSpace(ipAddress))
                        {
                            _sfcEquipmentList.Add(new SfcEquipmentInfo
                            {
                                IpAddress = ipAddress,
                                EquipmentId = equipmentId ?? "",
                                EquipmentName = equipmentName ?? "",
                                Status = ""
                            });
                        }
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Excel 파일 로드 실패:\n{ex.Message}", "오류",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        private void LoadSfcExcelButton_Click(object sender, RoutedEventArgs e)
        {
            string filePath = SfcExcelFilePathTextBox.Text;
            
            if (LoadSfcEquipmentFromExcel(filePath))
            {
                ExecuteSfcQueryButton.IsEnabled = _sfcEquipmentList.Count > 0;
                ApplySfcFilter();
                UpdateStatus($"{_sfcEquipmentList.Count}개의 설비 정보를 로드했습니다.", Colors.Green);
            }
        }

        /// <summary>
        /// SFC 쿼리 실행을 위한 입력값을 검증합니다.
        /// </summary>
        private bool ValidateSfcQueryInputs(out string userId, out string password)
        {
            userId = string.Empty;
            password = string.Empty;

            if (_sfcEquipmentList.Count == 0)
            {
                MessageBox.Show("먼저 Excel 파일을 로드하세요.", "알림",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return false;
            }

            if (SfcTnsComboBox.SelectedItem == null)
            {
                MessageBox.Show("TNS를 선택하세요.", "알림",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return false;
            }

            if (ConfigDatePicker.SelectedDate == null)
            {
                MessageBox.Show("조회 날짜를 선택하세요.", "알림",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return false;
            }

            userId = SfcUserIdTextBox.Text?.Trim() ?? "";
            password = SfcPasswordBox.Password ?? "";

            if (string.IsNullOrWhiteSpace(userId))
            {
                MessageBox.Show("User ID를 입력하세요.", "알림",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                SfcUserIdTextBox.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Password를 입력하세요.", "알림",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                SfcPasswordBox.Focus();
                return false;
            }

            return true;
        }

        /// <summary>
        /// SFC 쿼리를 실행합니다.
        /// </summary>
        private async System.Threading.Tasks.Task<DataTable?> ExecuteSfcQueryAsync(string userId, string password)
        {
            string selectedTnsName = SfcTnsComboBox.SelectedItem.ToString() ?? "";
            var selectedTns = _tnsEntries.FirstOrDefault(t => t.Name == selectedTnsName);

            if (selectedTns == null)
            {
                MessageBox.Show("선택한 TNS 정보를 찾을 수 없습니다.", "오류",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }

            string query = SfcQueryTextBox.Text;
            string configDate = ConfigDatePicker.SelectedDate!.Value.ToString("yyyyMMdd");
            var ipList = string.Join(",", _sfcEquipmentList.Select(e => $"'{e.IpAddress}'"));

            query = query.Replace("@CONFIG_REGISTER_YMD", configDate);
            query = query.Replace("@PC_IP_ADDR", ipList);

            return await OracleDatabase.ExecuteQueryAsync(
                selectedTns.ConnectionString,
                userId,
                password,
                query);
        }

        /// <summary>
        /// SFC 쿼리 결과를 처리합니다.
        /// </summary>
        private void ProcessSfcQueryResult(DataTable result)
        {
            var registeredData = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            
            foreach (DataRow row in result.Rows)
            {
                if (row["PC_IP_ADDR"] != null && row["PC_IP_ADDR"] != DBNull.Value)
                {
                    string ip = row["PC_IP_ADDR"].ToString()?.Trim() ?? "";
                    string configJson = row["CONFIG_JSON"]?.ToString() ?? "";
                    registeredData[ip] = configJson;
                }
            }

            foreach (var equipment in _sfcEquipmentList)
            {
                if (registeredData.ContainsKey(equipment.IpAddress))
                {
                    equipment.Status = "ON";
                    equipment.BizActor = ExtractBizActor(registeredData[equipment.IpAddress]);
                }
                else
                {
                    equipment.Status = "OFF";
                    equipment.BizActor = "";
                }
            }

            SfcMonitorDataGrid.Items.Refresh();
            ApplySfcDataGridRowStyle();
            ApplySfcFilter();
        }

        private async void ExecuteSfcQueryButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateSfcQueryInputs(out string userId, out string password))
            {
                return;
            }

            try
            {
                ExecuteSfcQueryButton.IsEnabled = false;
                SaveSettings();

                var result = await ExecuteSfcQueryAsync(userId, password);
                
                if (result != null)
                {
                    ProcessSfcQueryResult(result);
                    
                    int onCount = _sfcEquipmentList.Count(e => e.Status == "ON");
                    int offCount = _sfcEquipmentList.Count(e => e.Status == "OFF");
                    
                    UpdateStatus($"조회 완료 - ON: {onCount}개, OFF: {offCount}개", Colors.Green);
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

        private string ExtractBizActor(string configJson)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(configJson))
                    return "";

                using (JsonDocument doc = JsonDocument.Parse(configJson))
                {
                    // MWCONFIG_INFO.SQL_QUEUE에서 BIZACTOR 추출
                    if (doc.RootElement.TryGetProperty("MWCONFIG_INFO", out JsonElement mwConfig))
                    {
                        if (mwConfig.TryGetProperty("SQL_QUEUE", out JsonElement sqlQueue))
                        {
                            string sqlQueueValue = sqlQueue.GetString() ?? "";
                            
                            // "PROC_TYPE/LGE_MES_PRD/BIZACTOR_SQL/RS" 형식에서 BIZACTOR 추출
                            if (sqlQueueValue.Contains("BIZACTOR_"))
                            {
                                int startIndex = sqlQueueValue.IndexOf("BIZACTOR_") + 9;
                                int endIndex = sqlQueueValue.IndexOf("/", startIndex);
                                
                                if (endIndex > startIndex)
                                {
                                    return sqlQueueValue.Substring(startIndex, endIndex - startIndex);
                                }
                                else
                                {
                                    return sqlQueueValue.Substring(startIndex);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"JSON 파싱 오류: {ex.Message}");
            }

            return "";
        }

        // 주기적 쿼리 실행 관련 메서드
        
        /// <summary>
        /// 쿼리 실행 주기를 검증합니다.
        /// </summary>
        private bool ValidateQueryInterval(out int interval)
        {
            interval = 0;
            
            if (!int.TryParse(QueryIntervalTextBox.Text, out interval) || interval < 5)
            {
                MessageBox.Show("쿼리 실행 주기는 5초 이상이어야 합니다.", "오류",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            
            return true;
        }
        
        private void StartAutoQueryButton_Click(object sender, RoutedEventArgs e)
        {
            if (_loadedQueries.Count == 0)
            {
                MessageBox.Show("먼저 Excel에서 쿼리를 로드하세요.", "알림",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (!ValidateQueryInterval(out int interval))
            {
                return;
            }

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
            
            _queryTimer = new DispatcherTimer();
            _queryTimer.Interval = TimeSpan.FromSeconds(intervalSeconds);
            _queryTimer.Tick += async (s, e) => await ExecuteQueries();
            _queryTimer.Start();

            // 즉시 한 번 실행
            _ = ExecuteQueries();

            StartAutoQueryButton.IsEnabled = false;
            StopAutoQueryButton.IsEnabled = true;
            QueryIntervalTextBox.IsEnabled = false;
            LoadQueriesButton.IsEnabled = false;
            BrowseExcelButton.IsEnabled = false;

            UpdateStatus($"자동 쿼리 실행 시작 (주기: {intervalSeconds}초)", Colors.Green);
        }

        private void StopAutoQuery()
        {
            if (_queryTimer != null)
            {
                _queryTimer.Stop();
                _queryTimer = null;
            }

            _isAutoQueryRunning = false;

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
            var openFileDialog = new OpenFileDialog
            {
                Filter = "TNS Names File (tnsnames.ora)|tnsnames.ora|All Files (*.*)|*.*",
                Title = "tnsnames.ora 파일 선택",
                InitialDirectory = Path.GetDirectoryName(_settings.TnsPath)
            };

            if (openFileDialog.ShowDialog() == true)
            {
                TnsPathTextBox.Text = openFileDialog.FileName;
            }
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            TnsPathTextBox.Text = SettingsManager.GetDefaultTnsPath();
            UpdateStatus("TNS 경로가 기본값으로 복원되었습니다.", Colors.Green);
        }

        private void SaveSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TnsPathTextBox.Text))
            {
                MessageBox.Show("TNS 파일 경로를 입력하세요.", "오류",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!File.Exists(TnsPathTextBox.Text))
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
            LoadTnsEntries();

            UpdateStatus("설정이 저장되었습니다.", Colors.Green);
            MessageBox.Show("설정이 저장되었습니다.", "완료",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private const string DEFAULT_SFC_QUERY = "SELECT * FROM TB_SFC_MAINFRAME_CONFIG_N A WHERE A.TRANSACTION_TYPE_CODE = 'LOGIN_AUTO' AND SFC_MODE = 'PROD' AND A.CONFIG_REGISTER_YMD = @CONFIG_REGISTER_YMD AND PC_IP_ADDR IN (@PC_IP_ADDR)";
        
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