using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using FACTOVA_QueryHelper.Database;

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
                
                // 🔥 그룹명(QueryName) 목록: 중복 제거 및 알파벳순 정렬 (순번 필터링 제거)
                var groupNames = _allQueries
                    .Where(q => !string.IsNullOrWhiteSpace(q.QueryName))
                    .Select(q => q.QueryName)
                    .Distinct()
                    .OrderBy(name => name)
                    .ToList();
                
                // 콤보박스에 설정
                BizNameComboBox.ItemsSource = groupNames;
                
                // 첫 번째 항목 자동 선택
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
                return;
            }

            try
            {
                // 🔥 선택된 그룹명(QueryName)과 일치하는 모든 쿼리 필터링 및 정렬
                _filteredQueries = _allQueries
                    .Where(q => q.QueryName == groupName)
                    .OrderBy(q => q.OrderNumber)
                    .ThenBy(q => q.BizName)
                    .ToList();
                
                // 🔥 초기에는 모든 필터링된 쿼리를 표시
                ApplyTextFilters();
                
                UpdateStatus($"'{groupName}' 그룹의 쿼리 {_displayedQueries.Count}개가 로드되었습니다.", Colors.Green);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"쿼리 필터링 실패:\n{ex.Message}", "오류",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                UpdateStatus($"쿼리 필터링 실패: {ex.Message}", Colors.Red);
            }
        }

        /// <summary>
        /// 🔥 비즈명 및 쿼리비즈명 텍스트 필터를 적용합니다.
        /// </summary>
        private void ApplyTextFilters()
        {
            string bizNameFilter = BizNameFilterTextBox.Text?.Trim().ToLower() ?? "";
            string queryBizNameFilter = QueryBizNameFilterTextBox.Text?.Trim().ToLower() ?? "";

            _displayedQueries = _filteredQueries.Where(q =>
            {
                bool matchesBizName = string.IsNullOrEmpty(bizNameFilter) ||
                                     (q.BizName?.ToLower().Contains(bizNameFilter) ?? false);

                bool matchesQueryBizName = string.IsNullOrEmpty(queryBizNameFilter) ||
                                          (q.QueryBizName?.ToLower().Contains(queryBizNameFilter) ?? false);

                return matchesBizName && matchesQueryBizName;
            }).ToList();

            QueriesDataGrid.ItemsSource = null;
            QueriesDataGrid.ItemsSource = _displayedQueries;
            QueryCountTextBlock.Text = _displayedQueries.Count.ToString();

            if (!string.IsNullOrEmpty(bizNameFilter) || !string.IsNullOrEmpty(queryBizNameFilter))
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
            BizNameFilterTextBox.Text = "";
            QueryBizNameFilterTextBox.Text = "";
            UpdateStatus("필터가 초기화되었습니다.", Colors.Blue);
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            // 현재 선택된 그룹명 저장
            string? currentSelection = BizNameComboBox.SelectedItem as string;
            
            // 쿼리 재로드
            LoadQueries();
            
            // 이전 선택 복원 (가능한 경우)
            if (!string.IsNullOrEmpty(currentSelection))
            {
                BizNameComboBox.SelectedItem = currentSelection;
            }
            
            UpdateStatus("쿼리 목록이 새로고침되었습니다.", Colors.Blue);
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
                    // 쿼리 텍스트 편집 윈도우를 읽기 전용 모드로 표시
                    var window = new QueryTextEditWindow(query.Query, isReadOnly: true)
                    {
                        Title = $"쿼리 보기 - {query.BizName}",
                        Owner = Window.GetWindow(this),
                        WindowStartupLocation = WindowStartupLocation.CenterOwner
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
