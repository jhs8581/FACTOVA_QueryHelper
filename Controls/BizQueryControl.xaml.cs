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
                
                // "비즈 조회" 구분이면서 비즈명이 있는 쿼리만 필터링
                _allQueries = allQueries
                    .Where(q => q.QueryType == "비즈 조회" && !string.IsNullOrWhiteSpace(q.BizName))
                    .ToList();
                
                // 중복 제거된 비즈명 목록 생성 (알파벳순 정렬)
                var bizNames = _allQueries
                    .Select(q => q.BizName)
                    .Distinct()
                    .OrderBy(name => name)
                    .ToList();
                
                // 콤보박스에 설정
                BizNameComboBox.ItemsSource = bizNames;
                
                // 첫 번째 항목 자동 선택
                if (bizNames.Count > 0)
                {
                    BizNameComboBox.SelectedIndex = 0;
                }
                else
                {
                    UpdateStatus("비즈 조회 구분에 비즈명이 설정된 쿼리가 없습니다.", Colors.Orange);
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
        /// 선택된 비즈명에 해당하는 쿼리를 필터링하고 표시합니다.
        /// </summary>
        private void FilterQueriesByBizName(string bizName)
        {
            if (string.IsNullOrWhiteSpace(bizName))
            {
                _filteredQueries.Clear();
                QueriesDataGrid.ItemsSource = null;
                QueryCountTextBlock.Text = "0";
                UpdateStatus("비즈명을 선택하세요.", Colors.Gray);
                return;
            }

            try
            {
                // 선택된 비즈명과 일치하는 쿼리를 순번 순으로 정렬
                _filteredQueries = _allQueries
                    .Where(q => q.BizName == bizName)
                    .OrderBy(q => q.OrderNumber)
                    .ThenBy(q => q.QueryName)
                    .ToList();
                
                QueriesDataGrid.ItemsSource = _filteredQueries;
                QueryCountTextBlock.Text = _filteredQueries.Count.ToString();
                
                UpdateStatus($"'{bizName}' 관련 쿼리 {_filteredQueries.Count}개가 로드되었습니다.", Colors.Green);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"쿼리 필터링 실패:\n{ex.Message}", "오류",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                UpdateStatus($"쿼리 필터링 실패: {ex.Message}", Colors.Red);
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

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            // 현재 선택된 비즈명 저장
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

        private void ViewQueryButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is QueryItem query)
            {
                try
                {
                    // 쿼리 텍스트 편집 윈도우를 읽기 전용 모드로 표시
                    var window = new QueryTextEditWindow(query.Query)
                    {
                        Title = $"쿼리 보기 - {query.QueryName}",
                        Owner = Window.GetWindow(this),
                        WindowStartupLocation = WindowStartupLocation.CenterOwner
                    };
                    
                    // QueryTextEditWindow의 TextBox를 읽기 전용으로 설정
                    // (필요시 QueryTextEditWindow에 IsReadOnly 속성 추가)
                    window.ShowDialog();
                    
                    UpdateStatus($"'{query.QueryName}' 쿼리를 확인했습니다.", Colors.Blue);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"쿼리 표시 실패:\n{ex.Message}", "오류",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    UpdateStatus($"쿼리 표시 실패: {ex.Message}", Colors.Red);
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
