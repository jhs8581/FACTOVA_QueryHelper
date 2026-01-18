using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using FACTOVA_QueryHelper.Database;
using FACTOVA_QueryHelper.Utilities; // 🔥 DataGridHelper 추가

namespace FACTOVA_QueryHelper.Controls
{
    /// <summary>
    /// BizQueryControl.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class BizQueryControl : UserControl
    {
        private SharedDataContext? _sharedData;
        private QueryDatabase? _database;
        private List<QueryItem> _allQueries = new List<QueryItem>();
        private List<QueryItem> _filteredQueries = new List<QueryItem>();
        private List<QueryItem> _displayedQueries = new List<QueryItem>(); // 🔥 필터 적용 후 표시되는 쿼리
        private QueryItem? _selectedQuery;

        public BizQueryControl()
        {
            InitializeComponent();
            
            // 🔥 행 번호 표시 활성화
            DataGridHelper.EnableRowNumbers(QueriesDataGrid);
        }

        /// <summary>
        /// 공유 데이터 컨텍스트를 초기화합니다.
        /// </summary>
        public void Initialize(SharedDataContext sharedData)
        {
            _sharedData = sharedData;
            _database = new QueryDatabase(sharedData.Settings.DatabasePath);
            LoadQueries();
        }

        /// <summary>
        /// 데이터베이스에서 모든 쿼리를 로드하고 비즈명 목록을 생성합니다.
        /// </summary>
        private void LoadQueries()
        {
            if (_database == null) return;

            try
            {
                var allQueries = _database.GetAllQueries();
                
                // 🔥 "비즈 조회" 구분 쿼리만 필터링
                _allQueries = allQueries
                    .Where(q => q.QueryType == "비즈 조회")
                    .ToList();
                
                // 🔥 그룹명(QueryName) 목록: 중복 제거 및 알파벳순 정렬
                var groupNames = _allQueries
                    .Where(q => !string.IsNullOrWhiteSpace(q.QueryName))
                    .Select(q => q.QueryName)
                    .Distinct()
                    .OrderBy(name => name)
                    .ToList();
                
                // 🔥 "전체" 옵션을 맨 앞에 추가
                groupNames.Insert(0, "[전체]");
                
                // 콤보박스에 설정
                BizNameComboBox.ItemsSource = groupNames;
                
                // 첫 번째 항목 자동 선택 (전체)
                if (groupNames.Count > 0)
                {
                    BizNameComboBox.SelectedIndex = 0;
                }
                else
                {
                    UpdateStatus("비즈 조회 쿼리(그룹명)가 없습니다.", Colors.Orange);
                    QueriesDataGrid.ItemsSource = null;
                    QueryCountTextBlock.Text = "0";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"쿼리 로드 실패:\n{ex.Message}", "오류",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                UpdateStatus($"쿼리 로드 실패: {ex.Message}", Colors.Red);
            }
        }

        /// <summary>
        /// 선택된 그룹명에 해당하는 쿼리를 필터링하고 표시합니다.
        /// </summary>
        private void FilterQueriesByBizName(string groupName)
        {
            if (string.IsNullOrWhiteSpace(groupName))
            {
                _filteredQueries.Clear();
                _displayedQueries.Clear();
                QueriesDataGrid.ItemsSource = null;
                QueryCountTextBlock.Text = "0";
                UpdateStatus("그룹명을 선택하세요.", Colors.Gray);
                
                // 🔥 그룹명 컬럼 숨김 (인덱스 1번 컬럼)
                if (QueriesDataGrid.Columns.Count > 1)
                    QueriesDataGrid.Columns[1].Visibility = Visibility.Collapsed;
                
                return;
            }

            try
            {
                // 🔥 "[전체]" 선택 시 모든 쿼리 표시 (단, 사용여부 체크된 것만)
                if (groupName == "[전체]")
                {
                    _filteredQueries = _allQueries
                        .Where(q => !string.Equals(q.ExcludeFlag, "Y", StringComparison.OrdinalIgnoreCase)) // 🔥 사용여부 필터
                        .OrderBy(q => q.QueryName)
                        .ThenBy(q => q.OrderNumber)
                        .ThenBy(q => q.BizName)
                        .ToList();
                    
                    // 🔥 그룹명 컬럼 표시 (인덱스 1번 컬럼)
                    if (QueriesDataGrid.Columns.Count > 1)
                        QueriesDataGrid.Columns[1].Visibility = Visibility.Visible;
                }
                else
                {
                    // 선택된 그룹명(QueryName)과 일치하는 쿼리만 필터링 (단, 사용여부 체크된 것만)
                    _filteredQueries = _allQueries
                        .Where(q => q.QueryName == groupName && 
                                   !string.Equals(q.ExcludeFlag, "Y", StringComparison.OrdinalIgnoreCase)) // 🔥 사용여부 필터
                        .OrderBy(q => q.OrderNumber)
                        .ThenBy(q => q.BizName)
                        .ToList();
                    
                    // 🔥 그룹명 컬럼 숨김 (같은 그룹만 표시되므로)
                    if (QueriesDataGrid.Columns.Count > 1)
                        QueriesDataGrid.Columns[1].Visibility = Visibility.Collapsed;
                }
                
                // 🔥 초기에는 모든 필터링된 쿼리를 표시
                ApplyTextFilters();
                
                string statusMessage = groupName == "[전체]" 
                    ? $"전체 비즈 조회 쿼리 {_displayedQueries.Count}개가 로드되었습니다."
                    : $"'{groupName}' 그룹의 쿼리 {_displayedQueries.Count}개가 로드되었습니다.";
                
                UpdateStatus(statusMessage, Colors.Green);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"쿼리 필터링 실패:\n{ex.Message}", "오류",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                UpdateStatus($"쿼리 필터링 실패: {ex.Message}", Colors.Red);
            }
        }

        /// <summary>
        /// 🔥 그룹명, 비즈명, 쿼리비즈명 텍스트 필터를 적용합니다.
        /// </summary>
        private void ApplyTextFilters()
        {
            string groupNameFilter = GroupNameFilterTextBox.Text?.Trim().ToLower() ?? "";
            string bizNameFilter = BizNameFilterTextBox.Text?.Trim().ToLower() ?? "";
            string queryBizNameFilter = QueryBizNameFilterTextBox.Text?.Trim().ToLower() ?? "";

            _displayedQueries = _filteredQueries.Where(q =>
            {
                bool matchesGroupName = string.IsNullOrEmpty(groupNameFilter) ||
                                       (q.QueryName?.ToLower().Contains(groupNameFilter) ?? false);

                bool matchesBizName = string.IsNullOrEmpty(bizNameFilter) ||
                                     (q.BizName?.ToLower().Contains(bizNameFilter) ?? false);

                bool matchesQueryBizName = string.IsNullOrEmpty(queryBizNameFilter) ||
                                          (q.QueryBizName?.ToLower().Contains(queryBizNameFilter) ?? false);

                return matchesGroupName && matchesBizName && matchesQueryBizName;
            }).ToList();

            // 🔥 ItemsSource를 null로 설정 후 다시 바인딩하여 LoadingRow 이벤트가 발생하도록 함
            QueriesDataGrid.ItemsSource = null;
            QueriesDataGrid.ItemsSource = _displayedQueries;
            
            // 🔥 명시적으로 Items.Refresh() 호출
            QueriesDataGrid.Items.Refresh();
            
            QueryCountTextBlock.Text = _displayedQueries.Count.ToString();

            if (!string.IsNullOrEmpty(groupNameFilter) || !string.IsNullOrEmpty(bizNameFilter) || !string.IsNullOrEmpty(queryBizNameFilter))
            {
                UpdateStatus($"필터 적용됨: {_displayedQueries.Count}개 항목 표시 중", Colors.Green);
            }
        }

        #region 이벤트 핸들러

        private void BizNameComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (BizNameComboBox.SelectedItem is string selectedBizName)
            {
                FilterQueriesByBizName(selectedBizName);
            }
        }

        /// <summary>
        /// 🔥 필터 텍스트 변경 이벤트
        /// </summary>
        private void FilterTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyTextFilters();
        }

        /// <summary>
        /// 🔥 필터 초기화 버튼 클릭
        /// </summary>
        private void ClearFilterButton_Click(object sender, RoutedEventArgs e)
        {
            GroupNameFilterTextBox.Text = "";
            BizNameFilterTextBox.Text = "";
            QueryBizNameFilterTextBox.Text = "";
            UpdateStatus("필터가 초기화되었습니다.", Colors.Blue);
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            // 현재 선택된 그룹명 저장
            string? currentSelection = BizNameComboBox.SelectedItem as string;
            int currentIndex = BizNameComboBox.SelectedIndex;
            
            // 🔥 쿼리 재로드 (데이터베이스에서 최신 데이터 가져오기)
            LoadQueries();
            
            // 🔥 이전 선택 복원 (인덱스 기반으로 복원)
            if (currentIndex >= 0 && currentIndex < BizNameComboBox.Items.Count)
            {
                BizNameComboBox.SelectedIndex = currentIndex;
            }
            else if (!string.IsNullOrEmpty(currentSelection))
            {
                // 인덱스 복원이 안 되면 문자열로 찾기
                for (int i = 0; i < BizNameComboBox.Items.Count; i++)
                {
                    if (BizNameComboBox.Items[i] as string == currentSelection)
                    {
                        BizNameComboBox.SelectedIndex = i;
                        break;
                    }
                }
            }
            
            // 🔥 선택이 제대로 복원되지 않았다면 수동으로 필터링 호출
            if (BizNameComboBox.SelectedItem is string selectedName)
            {
                FilterQueriesByBizName(selectedName);
            }
            
            UpdateStatus("쿼리 목록이 새로고침되었습니다. (색상 포함)", Colors.Blue);
        }

        private void QueriesDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedQuery = QueriesDataGrid.SelectedItem as QueryItem;
            
            if (_selectedQuery != null)
            {
                UpdateStatus($"선택됨: {_selectedQuery.BizName}", Colors.Blue);
            }
        }

        private void ViewQueryButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is QueryItem query)
            {
                try
                {
                    // 🔥 쿼리 RowNumber와 DB 경로를 포함하여 팝업 열기 (편집 및 저장 가능)
                    var window = new QueryTextEditWindow(
                        query.Query, 
                        isReadOnly: true, 
                        queryId: query.RowNumber, 
                        databasePath: _sharedData?.Settings.DatabasePath ?? "")
                    {
                        Title = $"쿼리 보기 - {query.BizName}",
                        Owner = Window.GetWindow(this),
                        WindowStartupLocation = WindowStartupLocation.CenterOwner
                    };
                    
                    // 🔥 저장 후 쿼리 목록 다시 로드
                    window.QuerySaved += (s, args) =>
                    {
                        // 🔥 현재 필터 상태 저장
                        string? currentBizName = BizNameComboBox.SelectedItem as string;
                        string groupFilter = GroupNameFilterTextBox.Text;
                        string bizFilter = BizNameFilterTextBox.Text;
                        string queryBizFilter = QueryBizNameFilterTextBox.Text;
                        
                        LoadQueries();
                        
                        // 🔥 텍스트 필터 복원
                        GroupNameFilterTextBox.Text = groupFilter;
                        BizNameFilterTextBox.Text = bizFilter;
                        QueryBizNameFilterTextBox.Text = queryBizFilter;
                        
                        // 🔥 비즈명 선택 복원 및 필터링
                        if (!string.IsNullOrEmpty(currentBizName))
                        {
                            BizNameComboBox.SelectedItem = currentBizName;
                            FilterQueriesByBizName(currentBizName);
                        }
                        
                        // 🔥 텍스트 필터 적용
                        ApplyTextFilters();
                    };
                    
                    window.ShowDialog();
                    
                    UpdateStatus($"'{query.BizName}' 쿼리를 확인했습니다.", Colors.Blue);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"쿼리 표시 실패:\n{ex.Message}", "오류",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    UpdateStatus($"쿼리 표시 실패: {ex.Message}", Colors.Red);
                }
            }
        }

        private void ExecuteBizButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is QueryItem query)
            {
                try
                {
                    // QueryExecutorControl을 포함하는 팝업 윈도우 생성
                    var window = new Window
                    {
                        Title = $"비즈 실행 - {query.BizName}",
                        Width = 1000,
                        Height = 700,
                        Owner = Window.GetWindow(this),
                        WindowStartupLocation = WindowStartupLocation.CenterOwner,
                        WindowStyle = WindowStyle.SingleBorderWindow,
                        ResizeMode = ResizeMode.CanResize,
                        ShowInTaskbar = true
                    };

                    // QueryExecutorControl 생성
                    var executorControl = new QueryExecutorControl();
                    
                    // SharedDataContext 설정
                    if (_sharedData != null)
                    {
                        executorControl.SetSharedDataContext(_sharedData);
                    }
                    
                    // OracleDbService 생성 및 설정
                    var dbService = new Services.OracleDbService();
                    executorControl.SetDbService(dbService);
                    
                    // ConnectionInfo 목록 새로고침
                    executorControl.RefreshConnectionInfos();
                    
                    // 쿼리 설정
                    executorControl.SetQuery(query.Query);
                    
                    // Window에 Control 추가
                    window.Content = executorControl;
                    
                    // 팝업으로 표시
                    window.ShowDialog();
                    
                    UpdateStatus($"'{query.BizName}' 비즈를 실행했습니다.", Colors.Blue);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"비즈 실행 실패:\n{ex.Message}", "오류",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    UpdateStatus($"비즈 실행 실패: {ex.Message}", Colors.Red);
                }
            }
        }

        /// <summary>
        /// 🔥 쿼리 탭으로 열기 버튼 클릭
        /// </summary>
        private void OpenInQueryTabButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is QueryItem query)
            {
                try
                {
                    // MainWindow 찾기
                    var mainWindow = Window.GetWindow(this) as MainWindow;
                    if (mainWindow != null)
                    {
                        // 쿼리 실행 탭으로 이동하고 쿼리 전달
                        mainWindow.OpenQueryInEditorTab(query);
                        UpdateStatus($"'{query.BizName}' 쿼리를 쿼리 실행 탭에서 열었습니다.", Colors.Blue);
                    }
                    else
                    {
                        MessageBox.Show("메인 윈도우를 찾을 수 없습니다.", "오류",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"쿼리 탭 열기 실패:\n{ex.Message}", "오류",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    UpdateStatus($"쿼리 탭 열기 실패: {ex.Message}", Colors.Red);
                }
            }
        }

        #endregion

        /// <summary>
        /// 상태 메시지를 업데이트합니다.
        /// </summary>
        private void UpdateStatus(string message, Color color)
        {
            StatusTextBlock.Text = $"[{DateTime.Now:HH:mm:ss}] {message}";
            StatusTextBlock.Foreground = new SolidColorBrush(color);

            // 메인 윈도우 상태바도 업데이트
            _sharedData?.UpdateStatusCallback?.Invoke(message, color);
        }
    }
}
