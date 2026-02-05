using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using FACTOVA_QueryHelper.Database;
using FACTOVA_QueryHelper.Utilities;

namespace FACTOVA_QueryHelper.Controls
{
    /// <summary>
    /// LogAnalysisControl.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class LogAnalysisControl : UserControl
    {
        private SharedDataContext? _sharedData;
        private DispatcherTimer? _queryTimer;
        private DispatcherTimer? _countdownTimer;
        private int _remainingSeconds;
        private int _totalIntervalSeconds;
        private bool _isAutoQueryRunning = false;
        private QueryDatabase? _database; // DB 관리 추가

        public LogAnalysisControl()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 공유 데이터 컨텍스트를 초기화합니다.
        /// </summary>
        public void Initialize(SharedDataContext sharedData)
        {
            _sharedData = sharedData;
            
            // QueryExecutionManager를 LogAnalysisControl에서 초기화
            // (ResultTabControl과 CreateResultTab은 이 컨트롤에만 있음)
            _sharedData.QueryExecutionManager = new QueryExecutionManager(
                UpdateStatus,
                ResultTabControl,
                _sharedData.TnsEntries,
                _sharedData.Settings,
                CreateResultTab);
            
            // QueryDatabase 초기화 (사용자 지정 경로 사용)
            _database = new QueryDatabase(_sharedData.Settings.DatabasePath);
            
            LoadSettings();
        }

        /// <summary>
        /// 설정을 로드합니다.
        /// </summary>
        private void LoadSettings()
        {
            if (_sharedData == null) return;

            // 쿼리 타이머 간격 로드
            QueryIntervalTextBox.Text = _sharedData.Settings.QueryIntervalSeconds.ToString();

            // 알림 시 자동 실행 중지 설정 로드
            StopOnNotificationCheckBox.IsChecked = _sharedData.Settings.StopOnNotification;
            
            // 자동 실행 활성화 설정 로드
            EnableAutoExecutionCheckBox.IsChecked = _sharedData.Settings.EnableAutoExecution;
            QueryIntervalTextBox.IsEnabled = _sharedData.Settings.EnableAutoExecution;
            
            // 폰트 크기 설정 로드
            FontSizeTextBlock.Text = _sharedData.Settings.FontSize.ToString();
            
            // 자동으로 DB에서 쿼리 로드
            LoadFromDbButton_Click(null!, null!);
        }

        /// <summary>
        /// 자동 실행 타이머를 중지합니다.
        /// </summary>
        public void StopAutoQuery()
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

            if (_sharedData != null && _sharedData.LoadedQueries != null)
            {
                ToggleAutoQueryButton.IsEnabled = _sharedData.LoadedQueries.Count > 0;
                ToggleAutoQueryButton.Content = "실행";
                ToggleAutoQueryButton.Background = new SolidColorBrush(Color.FromRgb(0, 120, 215)); // 파란색
            }
            
            QueryIntervalTextBox.IsEnabled = EnableAutoExecutionCheckBox.IsChecked == true;

            UpdateStatus("자동 쿼리 실행 중지", Colors.Orange);
        }

        #region DB 관련 이벤트 핸들러

        private void LoadFromDbButton_Click(object sender, RoutedEventArgs e)
        {
            if (_sharedData == null || _database == null) return;

            try
            {
var allQueries = _database.GetAllQueries();
                // "쿼리 실행" 구분만 필터링
                _sharedData.LoadedQueries = allQueries.Where(q => q.QueryType == "쿼리 실행").ToList();
// 쿼리 필터 콤보박스 초기화
                InitializeQueryFilterComboBox();

                LoadedQueriesTextBlock.Text = $"{_sharedData.LoadedQueries.Count}개";
                
                // 🔥 결과 제목에 쿼리 수 표시
                ResultTitleTextBlock.Text = $"쿼리 결과 ({_sharedData.LoadedQueries.Count}개)";
                
                ToggleAutoQueryButton.IsEnabled = _sharedData.LoadedQueries.Count > 0;

                UpdateStatus($"데이터베이스에서 {_sharedData.LoadedQueries.Count}개의 쿼리를 로드했습니다.", Colors.Green);
}
            catch (Exception ex)
            {

                
                MessageBox.Show($"데이터베이스 로드 실패:\n{ex.Message}", "오류",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                UpdateStatus($"DB 로드 실패: {ex.Message}", Colors.Red);
            }
        }

        #endregion

        #region 쿼리 필터 관련 메서드

        /// <summary>
        /// 쿼리 필터 콤보박스를 초기화합니다.
        /// </summary>
        private void InitializeQueryFilterComboBox()
        {
            if (_sharedData == null) return;

            _sharedData.QueryFilterItems.Clear();

            // ID 순으로 정렬하여 각 쿼리를 항목으로 추가 ("전체" 항목 제거)
            var sortedQueries = _sharedData.LoadedQueries.OrderBy(q => q.RowNumber).ToList();
            
            foreach (var query in sortedQueries)
            {
                // 🔥 "사용여부" 체크박스에 따라 화면 표시 여부 결정
                // ExcludeFlag가 'Y'이면 화면에 표시하지 않음 (제외)
                if (string.Equals(query.ExcludeFlag, "Y", StringComparison.OrdinalIgnoreCase))
                {
                    continue; // 화면에 표시하지 않음
                }
                
                // ExcludeFlag가 'N'이거나 빈 값이면 화면에 표시하고 체크 (사용 = 실행 대상)
                _sharedData.QueryFilterItems.Add(new CheckableComboBoxItem
                {
                    Text = query.QueryName,
                    IsChecked = true // 화면에 표시되는 쿼리는 기본적으로 체크
                });
            }

            QueryFilterComboBox.ItemsSource = _sharedData.QueryFilterItems;
            UpdateQueryFilterComboBoxText();

            // 디폴트 폼 콤보박스 초기화
            InitializeDefaultFormComboBox();
        }

        /// <summary>
        /// 디폴트 폼 콤보박스를 초기화합니다.
        /// </summary>
        private void InitializeDefaultFormComboBox()
        {
            if (_sharedData == null) return;

            var defaultFormItems = new List<CheckableComboBoxItem>();
            int defaultIndex = -1;

            // ID 순으로 정렬하여 각 쿼리를 항목으로 추가 (단일 선택용)
            var sortedQueries = _sharedData.LoadedQueries.OrderBy(q => q.RowNumber).ToList();
            
            int displayIndex = 0; // 실제 표시되는 인덱스
            for (int i = 0; i < sortedQueries.Count; i++)
            {
                var query = sortedQueries[i];
                
                // 🔥 "사용여부" 체크박스에 따라 디폴트폼에도 표시 여부 결정
                // ExcludeFlag가 'Y'이면 디폴트폼에 표시하지 않음 (제외)
                if (string.Equals(query.ExcludeFlag, "Y", StringComparison.OrdinalIgnoreCase))
                {
                    continue; // 디폴트폼에 표시하지 않음
                }
                
                defaultFormItems.Add(new CheckableComboBoxItem
                {
                    Text = query.QueryName,
                    IsChecked = false
                });
                
                // 디폴트로 설정된 쿼리 찾기
                if (query.DefaultFlagBool && defaultIndex == -1)
                {
                    defaultIndex = displayIndex;
                }
                
                displayIndex++;
            }

            DefaultFormComboBox.ItemsSource = defaultFormItems;
            
            // 디폴트 쿼리가 있으면 선택, 없으면 첫 번째 항목 선택
            if (defaultIndex >= 0)
            {
                DefaultFormComboBox.SelectedIndex = defaultIndex;
            }
            else if (defaultFormItems.Count > 0)
            {
                DefaultFormComboBox.SelectedIndex = 0;
            }
        }

        /// <summary>
        /// 쿼리 필터 콤보박스의 표시 텍스트를 업데이트합니다.
        /// </summary>
        private void UpdateQueryFilterComboBoxText()
        {
            if (_sharedData?.QueryFilterItems == null || _sharedData.QueryFilterItems.Count == 0)
            {
                QueryFilterComboBox.Text = "";
                return;
            }

            var checkedItems = _sharedData.QueryFilterItems.Where(item => item.IsChecked).ToList();
            int totalQueries = _sharedData.QueryFilterItems.Count;
            
            if (checkedItems.Count == 0)
            {
                QueryFilterComboBox.Text = "선택 안 됨";
            }
            else if (checkedItems.Count == totalQueries)
            {
                QueryFilterComboBox.Text = "전체";
            }
            else
            {
                QueryFilterComboBox.Text = string.Join(", ", checkedItems.Select(item => item.Text));
            }
        }

        private void QueryFilterCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox && 
                checkBox.DataContext is CheckableComboBoxItem item)
            {
                HandleQueryFilterCheckBoxChanged(item);
            }
        }

        private void QueryFilterComboBox_DropDownClosed(object sender, EventArgs e)
        {
            UpdateQueryFilterComboBoxText();
        }

        /// <summary>
        /// 전체선택 버튼 클릭 이벤트
        /// </summary>
        private void SelectAllQueriesButton_Click(object sender, RoutedEventArgs e)
        {
            if (_sharedData == null || _sharedData.QueryFilterItems == null) return;

            // 모든 항목 체크
            foreach (var item in _sharedData.QueryFilterItems)
            {
                item.IsChecked = true;
            }

            UpdateQueryFilterComboBoxText();
            UpdateStatus("모든 쿼리가 선택되었습니다.", Colors.Green);
        }

        /// <summary>
        /// 전체해제 버튼 클릭 이벤트
        /// </summary>
        private void DeselectAllQueriesButton_Click(object sender, RoutedEventArgs e)
        {
            if (_sharedData == null || _sharedData.QueryFilterItems == null) return;

            // 모든 항목 해제
            foreach (var item in _sharedData.QueryFilterItems)
            {
                item.IsChecked = false;
            }

            UpdateQueryFilterComboBoxText();
            UpdateStatus("모든 쿼리 선택이 해제되었습니다.", Colors.Orange);
        }

        private void HandleQueryFilterCheckBoxChanged(CheckableComboBoxItem changedItem)
        {
            if (_sharedData == null) return;

            UpdateQueryFilterComboBoxText();
        }

        /// <summary>
        /// 선택된 쿼리 목록을 반환합니다.
        /// </summary>
        private List<QueryItem> GetSelectedQueries()
        {
            if (_sharedData == null || _sharedData.QueryFilterItems == null || _sharedData.QueryFilterItems.Count == 0)
            {
                return _sharedData?.LoadedQueries ?? new List<QueryItem>();
            }

            var selectedQueryNames = _sharedData.QueryFilterItems
                .Where(item => item.IsChecked)
                .Select(item => item.Text)
                .ToList();

            if (selectedQueryNames.Count == 0)
            {
                return new List<QueryItem>();
            }

            return _sharedData.LoadedQueries
                .Where(q => selectedQueryNames.Contains(q.QueryName))
                .ToList();
        }

        #endregion

        #region 쿼리 실행 관련 메서드

        private async System.Threading.Tasks.Task ExecuteQueries()
        {
            if (_sharedData == null) return;

            var selectedQueries = GetSelectedQueries();
            
            if (!ValidationHelper.ValidateListNotEmpty(selectedQueries, "선택된 쿼리 목록"))
                return;

            if (_sharedData.QueryExecutionManager == null)
            {
                MessageBox.Show("쿼리 실행 매니저가 초기화되지 않았습니다.", "오류",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // UI 비활성화
            SetQueryExecutionUIEnabled(false);

            try
            {
                var result = await _sharedData.QueryExecutionManager.ExecuteQueriesAsync(selectedQueries);

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
                    result.Notifications.Count,
                    result.NotifiedQueryNames);

                // UI가 완전히 업데이트될 때까지 대기한 후 탭 선택
                await Dispatcher.InvokeAsync(() =>
                {
                    if (ResultTabControl.Items.Count > 0)
                    {
                        // 디폴트 폼이 선택되어 있으면 해당 탭으로 이동
                        if (DefaultFormComboBox.SelectedItem is CheckableComboBoxItem selectedForm)
                        {
                            var defaultTabIndex = FindTabIndexByName(selectedForm.Text);
                            if (defaultTabIndex >= 0)
                            {
                                ResultTabControl.SelectedIndex = defaultTabIndex;
                            }
                            else
                            {
                                // 디폴트 폼을 찾지 못하면 첫 번째 탭(작업 로그) 선택
                                ResultTabControl.SelectedIndex = 0;
                            }
                        }
                        else
                        {
                            // 디폴트 폼이 선택되지 않았으면 첫 번째 탭(작업 로그) 선택
                            ResultTabControl.SelectedIndex = 0;
                        }
                    }
                }, System.Windows.Threading.DispatcherPriority.Loaded);

                // 알림이 있으면 메인 윈도우를 깜빡이고 탭 색상 변경 (팝업은 표시하지 않음)
                if (result.Notifications.Count > 0)
                {
                    FlashMainWindow();
                    
                    // 알림 시 자동 실행 중지
                    if (_isAutoQueryRunning && StopOnNotificationCheckBox.IsChecked == true)
                    {
                        StopAutoQuery();
                    }
                }
            }
            finally
            {
                SetQueryExecutionUIEnabled(true);
            }
        }

        /// <summary>
        /// 탭 이름으로 탭 인덱스를 찾습니다.
        /// </summary>
        private int FindTabIndexByName(string tabName)
        {
            for (int i = 0; i < ResultTabControl.Items.Count; i++)
            {
                if (ResultTabControl.Items[i] is TabItem tabItem && 
                    tabItem.Header?.ToString() == tabName)
                {
                    return i;
                }
            }
            return -1;
        }

        private void SetQueryExecutionUIEnabled(bool enabled)
        {
            // UI 활성화/비활성화는 필요 시 추가
        }

        private void FlashMainWindow()
        {
            try
            {
                var mainWindow = Application.Current.MainWindow;
                if (mainWindow != null)
                {
                    var helper = new System.Windows.Interop.WindowInteropHelper(mainWindow);
                    if (helper.Handle != IntPtr.Zero)
                    {
                        FlashWindow(helper.Handle);
                    }
                }
            }
            catch
            {
                // 깜빡임 실패 시 무시
            }
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        private static extern bool FlashWindowEx(ref FLASHWINFO pwfi);

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        private struct FLASHWINFO
        {
            public uint cbSize;
            public IntPtr hwnd;
            public uint dwFlags;
            public uint uCount;
            public uint dwTimeout;
        }

        private const uint FLASHW_ALL = 3;
        private const uint FLASHW_TIMERNOFG = 12;

        private void FlashWindow(IntPtr hwnd)
        {
            var info = new FLASHWINFO
            {
                cbSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf<FLASHWINFO>(),
                hwnd = hwnd,
                dwFlags = FLASHW_ALL | FLASHW_TIMERNOFG,
                uCount = 5,
                dwTimeout = 0
            };

            FlashWindowEx(ref info);
        }

        #endregion

        #region 자동 실행 관련 메서드

        private void ToggleAutoQueryButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isAutoQueryRunning)
            {
                StopAutoQuery();
            }
            else
            {
                if (_sharedData == null) return;

                var selectedQueries = GetSelectedQueries();
                if (!ValidationHelper.ValidateListNotEmpty(selectedQueries, "선택된 쿼리 목록"))
                    return;

                // 자동 실행이 체크되어 있으면 주기 실행, 아니면 1회만 실행
                if (EnableAutoExecutionCheckBox.IsChecked == true)
                {
                    if (!ValidationHelper.ValidateQueryInterval(QueryIntervalTextBox.Text, out int interval))
                        return;

                    // 설정 저장
                    _sharedData.Settings.QueryIntervalSeconds = interval;
                    _sharedData.SaveSettingsCallback?.Invoke();

                    StartAutoQuery(interval);
                }
                else
                {
                    // 1회만 실행
                    _ = ExecuteQueries();
                }
            }
        }

        private void EnableAutoExecutionCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (_sharedData == null) return;

            bool isEnabled = EnableAutoExecutionCheckBox.IsChecked == true;

            // 체크박스 상태를 설정에 저장
            _sharedData.Settings.EnableAutoExecution = isEnabled;
            _sharedData.SaveSettingsCallback?.Invoke();

            // 실행 주기 입력란 활성화/비활성화
            QueryIntervalTextBox.IsEnabled = isEnabled && !_isAutoQueryRunning;

            // 버튼 텍스트 변경
            if (isEnabled)
            {
                ToggleAutoQueryButton.Content = _isAutoQueryRunning ? "종료" : "실행";
            }
            else
            {
                ToggleAutoQueryButton.Content = "실행";
            }
}

        private void StopOnNotificationCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (_sharedData == null) return;

            // 체크박스 상태를 설정에 저장
            _sharedData.Settings.StopOnNotification = StopOnNotificationCheckBox.IsChecked ?? true;
            _sharedData.SaveSettingsCallback?.Invoke();
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
                _remainingSeconds = _totalIntervalSeconds;
                await ExecuteQueries();
            };
            _queryTimer.Start();

            // 카운트다운 타이머 설정
            _countdownTimer = new DispatcherTimer();
            _countdownTimer.Interval = TimeSpan.FromSeconds(1);
            _countdownTimer.Tick += CountdownTimer_Tick;
            _countdownTimer.Start();

            // 즉시 한 번 실행
            _ = ExecuteQueries();

            ToggleAutoQueryButton.Content = "종료";
            ToggleAutoQueryButton.Background = new SolidColorBrush(Color.FromRgb(220, 53, 69)); // 빨간색
            QueryIntervalTextBox.IsEnabled = false;

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

        #endregion

        #region 결과 탭 관련 메서드

        private void ClearResultsButton_Click(object sender, RoutedEventArgs e)
        {
            ResultTabControl.Items.Clear();
            UpdateStatus("결과 탭이 초기화되었습니다.", Colors.Gray);
        }

        private void IncreaseFontButton_Click(object sender, RoutedEventArgs e)
        {
            if (_sharedData == null) return;

            if (_sharedData.Settings.FontSize < 20)
            {
                _sharedData.Settings.FontSize++;
                ApplyFontSize();
                _sharedData.SaveSettingsCallback?.Invoke();
            }
        }

        private void DecreaseFontButton_Click(object sender, RoutedEventArgs e)
        {
            if (_sharedData == null) return;

            if (_sharedData.Settings.FontSize > 8)
            {
                _sharedData.Settings.FontSize--;
                ApplyFontSize();
                _sharedData.SaveSettingsCallback?.Invoke();
            }
        }

        private void ApplyFontSize()
        {
            if (_sharedData == null) return;

            int fontSize = _sharedData.Settings.FontSize;
            FontSizeTextBlock.Text = fontSize.ToString();

            // 모든 탭에 폰트 크기 적용
            foreach (TabItem tab in ResultTabControl.Items)
            {
                ApplyFontSizeToTab(tab, fontSize);
            }
        }

        private void ApplyFontSizeToTab(TabItem tab, int fontSize)
        {
            if (tab.Content is Grid grid)
            {
                foreach (var child in grid.Children)
                {
                    // DataGrid 폰트 크기 적용
                    if (child is DataGrid dataGrid)
                    {
                        dataGrid.FontSize = fontSize;
                    }
                    // RichTextBox 폰트 크기 적용 (작업 로그)
                    else if (child is System.Windows.Controls.RichTextBox richTextBox)
                    {
                        richTextBox.FontSize = fontSize;
                    }
                    // TextBox 폰트 크기 적용 (오류 메시지)
                    else if (child is TextBox textBox)
                    {
                        textBox.FontSize = fontSize;
                    }
                    // StackPanel 내부의 TextBlock 폰트 크기 적용
                    else if (child is StackPanel stackPanel)
                    {
                        foreach (var stackChild in stackPanel.Children)
                        {
                            if (stackChild is TextBlock textBlock)
                            {
                                textBlock.FontSize = fontSize;
                            }
                        }
                    }
                    // GroupBox 내부 적용
                    else if (child is GroupBox groupBox && groupBox.Content is ScrollViewer scrollViewer)
                    {
                        ApplyFontSizeToScrollViewer(scrollViewer, fontSize);
                    }
                }
            }
        }

        private void ApplyFontSizeToScrollViewer(ScrollViewer scrollViewer, int fontSize)
        {
            if (scrollViewer.Content is WrapPanel wrapPanel)
            {
                foreach (var child in wrapPanel.Children)
                {
                    if (child is Border border && border.Child is StackPanel stackPanel)
                    {
                        foreach (var stackChild in stackPanel.Children)
                        {
                            if (stackChild is TextBlock textBlock)
                            {
                                textBlock.FontSize = fontSize - 1; // 요약 카드는 약간 작게
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 쿼리 결과 탭을 생성합니다.
        /// </summary>
        private void CreateResultTab(QueryItem queryItem, System.Data.DataTable? result, double duration, string? errorMessage)
        {
            var tabItem = new TabItem
            {
                Header = queryItem.QueryName
            };

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // 폰트 크기 가져오기
            int fontSize = _sharedData?.Settings.FontSize ?? 11;

            if (result != null && errorMessage == null)
            {
                // 요약 정보 패널 생성
                var summaryBorder = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(240, 248, 255)),
                    Padding = new Thickness(15, 10, 15, 10),
                    Margin = new Thickness(0, 0, 0, 10)
                };

                var summaryStack = new StackPanel();

                // 첫 번째 줄: 기본 정보
                var row1Panel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Margin = new Thickness(0, 0, 0, 5)
                };

                // 쿼리명
                row1Panel.Children.Add(new TextBlock
                {
                    Text = $"쿼리: {queryItem.QueryName}",
                    FontSize = fontSize,
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(0, 0, 20, 0),
                    VerticalAlignment = VerticalAlignment.Center
                });

                // 결과 건수
                row1Panel.Children.Add(new TextBlock
                {
                    Text = $"결과: {result.Rows.Count}건",
                    FontSize = fontSize,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Colors.Green),
                    Margin = new Thickness(0, 0, 20, 0),
                    VerticalAlignment = VerticalAlignment.Center
                });

                // 컬럼 수
                row1Panel.Children.Add(new TextBlock
                {
                    Text = $"컬럼: {result.Columns.Count}개",
                    FontSize = fontSize,
                    Foreground = new SolidColorBrush(Colors.Blue),
                    Margin = new Thickness(0, 0, 20, 0),
                    VerticalAlignment = VerticalAlignment.Center
                });

                // 소요시간
                row1Panel.Children.Add(new TextBlock
                {
                    Text = $"소요시간: {duration:F2}초",
                    FontSize = fontSize,
                    Foreground = new SolidColorBrush(Colors.Gray),
                    Margin = new Thickness(0, 0, 0, 0),
                    VerticalAlignment = VerticalAlignment.Center
                });

                summaryStack.Children.Add(row1Panel);

                // 두 번째 줄: DB 연결 정보
                var row2Panel = new StackPanel
                {
                    Orientation = Orientation.Horizontal
                };

                if (!string.IsNullOrEmpty(queryItem.TnsName))
                {
                    row2Panel.Children.Add(new TextBlock
                    {
                        Text = $"TNS: {queryItem.TnsName}",
                        FontSize = fontSize - 1,
                        Foreground = new SolidColorBrush(Colors.DarkBlue),
                        Margin = new Thickness(0, 0, 20, 0),
                        VerticalAlignment = VerticalAlignment.Center
                    });

                    row2Panel.Children.Add(new TextBlock
                    {
                        Text = $"User: {queryItem.UserId}",
                        FontSize = fontSize - 1,
                        Foreground = new SolidColorBrush(Colors.DarkBlue),
                        Margin = new Thickness(0, 0, 0, 0),
                        VerticalAlignment = VerticalAlignment.Center
                    });
                }
                else if (!string.IsNullOrEmpty(queryItem.Host))
                {
                    row2Panel.Children.Add(new TextBlock
                    {
                        Text = $"DB: {queryItem.Host}:{queryItem.Port}/{queryItem.ServiceName}",
                        FontSize = fontSize - 1,
                        Foreground = new SolidColorBrush(Colors.DarkBlue),
                        Margin = new Thickness(0, 0, 20, 0),
                        VerticalAlignment = VerticalAlignment.Center
                    });

                    row2Panel.Children.Add(new TextBlock
                    {
                        Text = $"User: {queryItem.UserId}",
                        FontSize = fontSize - 1,
                        Foreground = new SolidColorBrush(Colors.DarkBlue),
                        Margin = new Thickness(0, 0, 0, 0),
                        VerticalAlignment = VerticalAlignment.Center
                    });
                }

                summaryStack.Children.Add(row2Panel);

                summaryBorder.Child = summaryStack;
                Grid.SetRow(summaryBorder, 0);
                grid.Children.Add(summaryBorder);

                // 성공 - 데이터 그리드 생성
                var dataGrid = new DataGrid
                {
                    AutoGenerateColumns = true,
                    IsReadOnly = true,
                    CanUserAddRows = false,
                    CanUserDeleteRows = false,
                    CanUserReorderColumns = true,
                    CanUserResizeColumns = true,
                    AlternatingRowBackground = new SolidColorBrush(Color.FromRgb(245, 245, 245)),
                    GridLinesVisibility = DataGridGridLinesVisibility.All,
                    SelectionMode = DataGridSelectionMode.Extended,
                    SelectionUnit = DataGridSelectionUnit.CellOrRowHeader,
                    ClipboardCopyMode = DataGridClipboardCopyMode.IncludeHeader,
                    ItemsSource = result.DefaultView
                };
                
                // 🔥 DataGridHelper로 통일된 스타일 적용 (FontFamily, FontSize, ColumnHeaderStyle 포함)
                DataGridHelper.EnableRowNumbers(dataGrid);
                
                // 🔥 사용자 설정 FontSize 적용 (DataGridHelper 이후)
                dataGrid.FontSize = fontSize;
                
                // AutoGeneratingColumn 이벤트 핸들러 추가 (숫자 포맷, DateTime 포맷 등)
                dataGrid.AutoGeneratingColumn += (s, e) =>
                {
                    // 숫자 타입 컬럼 자동 인식
                    bool isNumericColumn = e.PropertyType == typeof(int) || 
                                           e.PropertyType == typeof(long) || 
                                           e.PropertyType == typeof(decimal) || 
                                           e.PropertyType == typeof(double) || 
                                           e.PropertyType == typeof(float) ||
                                           e.PropertyType == typeof(short);
                    
                    // 🔥 DateTime 타입 컬럼 자동 인식
                    bool isDateTimeColumn = e.PropertyType == typeof(DateTime) || 
                                            e.PropertyType == typeof(DateTime?);
                    
                    if (e.Column is DataGridTextColumn textColumn)
                    {
                        var elementStyle = new Style(typeof(TextBlock));
                        elementStyle.Setters.Add(new Setter(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center));
                        elementStyle.Setters.Add(new Setter(TextBlock.TextWrappingProperty, TextWrapping.Wrap));
                        
                        if (isNumericColumn)
                        {
                            // 숫자 컬럼 오른쪽 정렬 + 콤마 포맷
                            elementStyle.Setters.Add(new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Right));
                            textColumn.Binding = new System.Windows.Data.Binding(e.PropertyName)
                            {
                                StringFormat = "#,##0.######"
                            };
                        }
                        else if (isDateTimeColumn)
                        {
                            // 🔥 DateTime 컬럼 yyyy-MM-dd HH:mm:ss 포맷
                            textColumn.Binding = new System.Windows.Data.Binding(e.PropertyName)
                            {
                                StringFormat = "yyyy-MM-dd HH:mm:ss"
                            };
                        }
                        
                        textColumn.ElementStyle = elementStyle;
                    }
                    
                    e.Column.MinWidth = 80;
                    e.Column.Width = new DataGridLength(1, DataGridLengthUnitType.Auto);
                };

                // 컨텍스트 메뉴 추가
                var contextMenu = new ContextMenu();
                
                var copyMenuItem = new MenuItem { Header = "복사 (Ctrl+C)" };
                copyMenuItem.Click += (s, e) =>
                {
                    try
                    {
                        if (dataGrid.SelectedCells.Count > 0)
                        {
                            dataGrid.ClipboardCopyMode = DataGridClipboardCopyMode.ExcludeHeader;
                            ApplicationCommands.Copy.Execute(null, dataGrid);
                            dataGrid.UnselectAllCells();
                            UpdateStatus("선택한 셀이 복사되었습니다.", Colors.Green);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"복사 실패:\n{ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                };
                contextMenu.Items.Add(copyMenuItem);

                var copyWithHeaderMenuItem = new MenuItem { Header = "헤더 포함 복사" };
                copyWithHeaderMenuItem.Click += (s, e) =>
                {
                    try
                    {
                        if (dataGrid.SelectedCells.Count > 0)
                        {
                            dataGrid.ClipboardCopyMode = DataGridClipboardCopyMode.IncludeHeader;
                            ApplicationCommands.Copy.Execute(null, dataGrid);
                            dataGrid.UnselectAllCells();
                            UpdateStatus("헤더 포함하여 복사되었습니다.", Colors.Green);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"복사 실패:\n{ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                };
                contextMenu.Items.Add(copyWithHeaderMenuItem);

                contextMenu.Items.Add(new Separator());

                var selectAllMenuItem = new MenuItem { Header = "모두 선택 (Ctrl+A)" };
                selectAllMenuItem.Click += (s, e) => dataGrid.SelectAllCells();
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

                Grid.SetRow(dataGrid, 1);
                grid.Children.Add(dataGrid);

                // 상태 표시
                var statusPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Margin = new Thickness(5)
                };

                statusPanel.Children.Add(new TextBlock
                {
                    Text = $"[성공] {result.Rows.Count}개 행 | {result.Columns.Count}개 열 | 소요시간: {duration:F2}초",
                    FontSize = fontSize - 1,
                    Foreground = new SolidColorBrush(Colors.Green),
                    VerticalAlignment = VerticalAlignment.Center
                });

                Grid.SetRow(statusPanel, 2);
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
                    Padding = new Thickness(10),
                    FontSize = fontSize
                };

                Grid.SetRow(errorTextBox, 0);
                grid.Children.Add(errorTextBox);

                var statusText = new TextBlock
                {
                    Text = "[실패] 실행 실패",
                    FontSize = fontSize - 1,
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

        private void CreateExecutionLogTab(List<string> logs, DateTime startTime, double totalDuration, 
            int successCount, int failCount, int notificationCount, List<string> notifiedQueryNames)
        {
            var tabItem = new TabItem
            {
                Header = "작업 로그",
                FontWeight = FontWeights.Bold
            };
            
            // 알림이 있으면 탭 색상을 빨간색으로 변경
            if (notificationCount > 0)
            {
                tabItem.Foreground = new SolidColorBrush(Colors.Red);
            }

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            // 폰트 크기 가져오기
            int fontSize = _sharedData?.Settings.FontSize ?? 11;

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

            summaryPanel.Children.Add(new TextBlock
            {
                Text = $"시작: {startTime:HH:mm:ss}",
                FontSize = fontSize,
                Margin = new Thickness(0, 0, 20, 0),
                VerticalAlignment = VerticalAlignment.Center
            });

            summaryPanel.Children.Add(new TextBlock
            {
                Text = $"소요시간: {totalDuration:F2}초",
                FontSize = fontSize,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Colors.Blue),
                Margin = new Thickness(0, 0, 20, 0),
                VerticalAlignment = VerticalAlignment.Center
            });

            summaryPanel.Children.Add(new TextBlock
            {
                Text = $"성공: {successCount}개",
                FontSize = fontSize,
                Foreground = new SolidColorBrush(Colors.Green),
                Margin = new Thickness(0, 0, 20, 0),
                VerticalAlignment = VerticalAlignment.Center
            });

            if (failCount > 0)
            {
                summaryPanel.Children.Add(new TextBlock
                {
                    Text = $"실패: {failCount}개",
                    FontSize = fontSize,
                    Foreground = new SolidColorBrush(Colors.Red),
                    Margin = new Thickness(0, 0, 20, 0),
                    VerticalAlignment = VerticalAlignment.Center
                });
            }

            if (notificationCount > 0)
            {
                summaryPanel.Children.Add(new TextBlock
                {
                    Text = $"알림: {notificationCount}개",
                    FontSize = fontSize,
                    Foreground = new SolidColorBrush(Colors.Red),
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(0, 0, 20, 0),
                    VerticalAlignment = VerticalAlignment.Center
                });
            }

            summaryBorder.Child = summaryPanel;
            Grid.SetRow(summaryBorder, 0);
            grid.Children.Add(summaryBorder);

            // RichTextBox로 로그 표시 (알림 영역은 빨간색 텍스트로 강조)
            var richTextBox = new System.Windows.Controls.RichTextBox
            {
                IsReadOnly = true,
                FontFamily = new FontFamily("Consolas"),
                FontSize = fontSize,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                Padding = new Thickness(10),
                Background = new SolidColorBrush(Color.FromRgb(250, 250, 250))
            };

            var document = new System.Windows.Documents.FlowDocument();
            
            string? currentQueryName = null;
            bool isInNotificationSection = false;

            foreach (var log in logs)
            {
                var paragraph = new System.Windows.Documents.Paragraph
                {
                    Margin = new Thickness(0)
                };

                // 쿼리 시작/종료 추적
                if (log.Contains("쿼리:"))
                {
                    var match = System.Text.RegularExpressions.Regex.Match(log, @"쿼리: (.+)");
                    if (match.Success)
                    {
                        currentQueryName = match.Groups[1].Value.Trim();
                        isInNotificationSection = notifiedQueryNames.Contains(currentQueryName);
                    }
                }
                else if (log.StartsWith("---") || log.StartsWith("[작업 요약]"))
                {
                    currentQueryName = null;
                    isInNotificationSection = false;
                }

                Color textColor = Colors.Black;
                bool isBold = false;

                // 알림 관련 로그 - 진한 빨간색 + 굵게
                if (log.Contains("[알림]") || log.Contains("알림:") || 
                    (log.Contains("조회 결과") && log.Contains("기준:")) ||
                    log.Contains("조건 일치") || log.Contains("조건 불일치") ||
                    log.StartsWith("    - "))
                {
                    textColor = Color.FromRgb(220, 53, 69);
                    isBold = true;
                }
                // 알림 있는 쿼리 영역 - 빨간색 계열
                else if (isInNotificationSection)
                {
                    if (log.Contains("쿼리:"))
                    {
                        textColor = Color.FromRgb(220, 53, 69);
                        isBold = true;
                    }
                    else if (log.Contains("[성공]") || log.Contains("성공:"))
                    {
                        textColor = Color.FromRgb(255, 69, 58);
                    }
                    else if (log.Contains("[실패]"))
                    {
                        textColor = Color.FromRgb(220, 53, 69);
                        isBold = true;
                    }
                    else
                    {
                        textColor = Color.FromRgb(255, 99, 71);
                    }
                }
                // 일반 로그
                else
                {
                    if (log.Contains("[성공]") || log.Contains("성공:"))
                    {
                        textColor = Colors.Green;
                    }
                    else if (log.Contains("[실패]"))
                    {
                        textColor = Colors.Red;
                    }
                    else if (log.StartsWith("[작업 요약]"))
                    {
                        textColor = Colors.Blue;
                        isBold = true;
                    }
                }

                var run = new System.Windows.Documents.Run(log)
                {
                    Foreground = new SolidColorBrush(textColor)
                };

                if (isBold)
                    run.FontWeight = FontWeights.Bold;

                paragraph.Inlines.Add(run);
                document.Blocks.Add(paragraph);
            }

            richTextBox.Document = document;
            Grid.SetRow(richTextBox, 1);
            grid.Children.Add(richTextBox);

            tabItem.Content = grid;
            ResultTabControl.Items.Insert(0, tabItem);
            
            // 알림이 있는 쿼리 탭의 색상을 빨간색으로 변경
            foreach (var queryName in notifiedQueryNames)
            {
                var tabIndex = FindTabIndexByName(queryName);
                if (tabIndex >= 0 && ResultTabControl.Items[tabIndex] is TabItem queryTab)
                {
                    queryTab.Foreground = new SolidColorBrush(Colors.Red);
                }
            }
        }

        #endregion

        #region 유틸리티 메서드

        private void UpdateStatus(string message, Color color)
        {
            StatusTextBlock.Text = message;
            StatusTextBlock.Foreground = new SolidColorBrush(color);
            
            // 메인 윈도우 상태바도 업데이트
            _sharedData?.UpdateStatusCallback?.Invoke(message, color);
        }

        #endregion
    }
}
