using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

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
            
            LoadSettings();
        }

        /// <summary>
        /// 설정을 로드합니다.
        /// </summary>
        private void LoadSettings()
        {
            if (_sharedData == null) return;

            // Excel 파일 경로 로드
            if (!string.IsNullOrWhiteSpace(_sharedData.Settings.ExcelFilePath) && 
                File.Exists(_sharedData.Settings.ExcelFilePath))
            {
                ExcelFilePathTextBox.Text = _sharedData.Settings.ExcelFilePath;
                LoadQueriesButton.IsEnabled = true;

                // 시트 목록 로드
                try
                {
                    var sheets = ExcelQueryReader.GetSheetNames(_sharedData.Settings.ExcelFilePath);
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

            // 쿼리 타이머 간격 로드
            QueryIntervalTextBox.Text = _sharedData.Settings.QueryIntervalSeconds.ToString();

            // 알림 시 자동 실행 중지 설정 로드
            StopOnNotificationCheckBox.IsChecked = _sharedData.Settings.StopOnNotification;
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
                StartAutoQueryButton.IsEnabled = _sharedData.LoadedQueries.Count > 0;
            }
            StopAutoQueryButton.IsEnabled = false;
            QueryIntervalTextBox.IsEnabled = true;
            LoadQueriesButton.IsEnabled = true;
            BrowseExcelButton.IsEnabled = true;

            UpdateStatus("자동 쿼리 실행 중지", Colors.Orange);
        }

        #region Excel 파일 관련 이벤트 핸들러

        private void BrowseExcelButton_Click(object sender, RoutedEventArgs e)
        {
            if (_sharedData == null) return;

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

                // 설정 저장
                _sharedData.Settings.ExcelFilePath = filePath;
                _sharedData.SaveSettingsCallback?.Invoke();
            }
        }

        private void LoadQueriesButton_Click(object sender, RoutedEventArgs e)
        {
            if (_sharedData == null) return;

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
                
                _sharedData.LoadedQueries = ExcelManager.LoadQueries(
                    ExcelFilePathTextBox.Text,
                    SheetComboBox.SelectedItem?.ToString(),
                    startRow);

                System.Diagnostics.Debug.WriteLine($"로드된 쿼리 수: {_sharedData.LoadedQueries.Count}개");
                
                // 쿼리 필터 콤보박스 초기화
                InitializeQueryFilterComboBox();

                LoadedQueriesTextBlock.Text = $"{_sharedData.LoadedQueries.Count}개";
                ExecuteAllButton.IsEnabled = _sharedData.LoadedQueries.Count > 0;
                StartAutoQueryButton.IsEnabled = _sharedData.LoadedQueries.Count > 0;

                UpdateStatus($"{_sharedData.LoadedQueries.Count}개의 쿼리를 로드했습니다.", Colors.Green);
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

        #endregion

        #region 쿼리 필터 관련 메서드

        /// <summary>
        /// 쿼리 필터 콤보박스를 초기화합니다.
        /// </summary>
        private void InitializeQueryFilterComboBox()
        {
            if (_sharedData == null) return;

            _sharedData.QueryFilterItems.Clear();

            // "전체" 항목 추가 (기본값: 체크 해제)
            _sharedData.QueryFilterItems.Add(new CheckableComboBoxItem 
            { 
                Text = "전체", 
                IsChecked = false 
            });

            // 각 쿼리를 항목으로 추가
            foreach (var query in _sharedData.LoadedQueries)
            {
                // ExcludeFlag가 'Y'이면 기본 체크
                bool isChecked = string.Equals(query.ExcludeFlag, "Y", StringComparison.OrdinalIgnoreCase);
                
                _sharedData.QueryFilterItems.Add(new CheckableComboBoxItem
                {
                    Text = query.QueryName,
                    IsChecked = isChecked
                });
            }

            QueryFilterComboBox.ItemsSource = _sharedData.QueryFilterItems;
            UpdateQueryFilterComboBoxText();
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

            var checkedItems = _sharedData.QueryFilterItems.Where(item => item.IsChecked && item.Text != "전체").ToList();
            int totalQueries = _sharedData.QueryFilterItems.Count - 1;
            
            if (checkedItems.Count == 0)
            {
                QueryFilterComboBox.Text = "선택 안 됨";
                var allItem = _sharedData.QueryFilterItems.FirstOrDefault(item => item.Text == "전체");
                if (allItem != null && allItem.IsChecked)
                {
                    allItem.IsChecked = false;
                }
            }
            else if (checkedItems.Count == totalQueries)
            {
                QueryFilterComboBox.Text = "전체";
                var allItem = _sharedData.QueryFilterItems.FirstOrDefault(item => item.Text == "전체");
                if (allItem != null && !allItem.IsChecked)
                {
                    allItem.IsChecked = true;
                }
            }
            else
            {
                QueryFilterComboBox.Text = string.Join(", ", checkedItems.Select(item => item.Text));
                var allItem = _sharedData.QueryFilterItems.FirstOrDefault(item => item.Text == "전체");
                if (allItem != null && allItem.IsChecked)
                {
                    allItem.IsChecked = false;
                }
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

        private void HandleQueryFilterCheckBoxChanged(CheckableComboBoxItem changedItem)
        {
            if (_sharedData == null) return;

            if (changedItem.Text == "전체")
            {
                foreach (var item in _sharedData.QueryFilterItems.Where(i => i.Text != "전체"))
                {
                    item.IsChecked = changedItem.IsChecked;
                }
            }
            else
            {
                var allItem = _sharedData.QueryFilterItems.FirstOrDefault(item => item.Text == "전체");
                if (allItem != null)
                {
                    int totalItems = _sharedData.QueryFilterItems.Count - 1;
                    int checkedItems = _sharedData.QueryFilterItems.Count(item => item.IsChecked && item.Text != "전체");
                    
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
            if (_sharedData == null || _sharedData.QueryFilterItems == null || _sharedData.QueryFilterItems.Count == 0)
            {
                return _sharedData?.LoadedQueries ?? new List<QueryItem>();
            }

            var selectedQueryNames = _sharedData.QueryFilterItems
                .Where(item => item.IsChecked && item.Text != "전체")
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

        private async void ExecuteAllButton_Click(object sender, RoutedEventArgs e)
        {
            await ExecuteQueries();
        }

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

        #endregion

        #region 자동 실행 관련 메서드

        private void StartAutoQueryButton_Click(object sender, RoutedEventArgs e)
        {
            if (_sharedData == null) return;

            var selectedQueries = GetSelectedQueries();
            if (!ValidationHelper.ValidateListNotEmpty(selectedQueries, "선택된 쿼리 목록"))
                return;

            if (!ValidationHelper.ValidateQueryInterval(QueryIntervalTextBox.Text, out int interval))
                return;

            // 설정 저장
            _sharedData.Settings.QueryIntervalSeconds = interval;
            _sharedData.SaveSettingsCallback?.Invoke();

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

            StartAutoQueryButton.IsEnabled = false;
            StopAutoQueryButton.IsEnabled = true;
            QueryIntervalTextBox.IsEnabled = false;
            LoadQueriesButton.IsEnabled = false;
            BrowseExcelButton.IsEnabled = false;

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
                    SelectionUnit = DataGridSelectionUnit.Cell,
                    ClipboardCopyMode = DataGridClipboardCopyMode.IncludeHeader
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

        private void CreateExecutionLogTab(List<string> logs, DateTime startTime, double totalDuration, 
            int successCount, int failCount, int notificationCount)
        {
            var tabItem = new TabItem
            {
                Header = "작업 로그",
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

            summaryPanel.Children.Add(new TextBlock
            {
                Text = $"시작: {startTime:HH:mm:ss}",
                FontSize = 12,
                Margin = new Thickness(0, 0, 20, 0),
                VerticalAlignment = VerticalAlignment.Center
            });

            summaryPanel.Children.Add(new TextBlock
            {
                Text = $"소요시간: {totalDuration:F2}초",
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Colors.Blue),
                Margin = new Thickness(0, 0, 20, 0),
                VerticalAlignment = VerticalAlignment.Center
            });

            summaryPanel.Children.Add(new TextBlock
            {
                Text = $"성공: {successCount}개",
                FontSize = 12,
                Foreground = new SolidColorBrush(Colors.Green),
                Margin = new Thickness(0, 0, 20, 0),
                VerticalAlignment = VerticalAlignment.Center
            });

            if (failCount > 0)
            {
                summaryPanel.Children.Add(new TextBlock
                {
                    Text = $"실패: {failCount}개",
                    FontSize = 12,
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
            ResultTabControl.Items.Insert(0, tabItem);
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
