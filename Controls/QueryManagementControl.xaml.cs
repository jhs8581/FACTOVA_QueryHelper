using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using FACTOVA_QueryHelper.Database;

namespace FACTOVA_QueryHelper.Controls
{
    /// <summary>
    /// QueryManagementControl.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class QueryManagementControl : UserControl
    {
        private SharedDataContext? _sharedData;
        private QueryDatabase? _database;
        private QueryItem? _selectedQuery;
        private System.Collections.ObjectModel.ObservableCollection<QueryItem>? _queries;
        
        // 🔥 변경된 항목 추적 (신규 + 수정)
        private HashSet<QueryItem> _modifiedQueries = new HashSet<QueryItem>();
        private bool _hasUnsavedChanges = false;

        public QueryManagementControl()
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
            LoadQueriesFromDatabase();
        }

        /// <summary>
        /// 데이터베이스에서 쿼리 목록을 로드합니다.
        /// </summary>
        private void LoadQueriesFromDatabase()
        {
            if (_database == null) return;

            try
            {
                var queries = _database.GetAllQueries();
                
                // ID(RowNumber) 기준으로 정렬
                queries = queries.OrderBy(q => q.RowNumber).ToList();
                
                _queries = new System.Collections.ObjectModel.ObservableCollection<QueryItem>(queries);
                QueriesDataGrid.ItemsSource = _queries;
                DbQueryCountTextBlock.Text = $"{queries.Count}개";
                
                // 🔥 변경 추적 초기화
                _modifiedQueries.Clear();
                _hasUnsavedChanges = false;
                EditModeBorder.Visibility = Visibility.Collapsed;
                
                UpdateStatus($"{queries.Count}개의 쿼리가 로드되었습니다.", Colors.Green);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"쿼리 로드 실패:\n{ex.Message}", "오류",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                UpdateStatus($"쿼리 로드 실패: {ex.Message}", Colors.Red);
            }
        }

        #region 이벤트 핸들러

        private void LoadFromDbButton_Click(object sender, RoutedEventArgs e)
        {
            // 🔥 저장하지 않은 변경사항 확인
            if (_hasUnsavedChanges)
            {
                var result = MessageBox.Show(
                    "저장하지 않은 변경사항이 있습니다. 새로고침하면 변경사항이 사라집니다.\n계속하시겠습니까?",
                    "경고",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result != MessageBoxResult.Yes)
                    return;
            }

            LoadQueriesFromDatabase();
        }

        private void DeleteQueryButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedQuery == null)
            {
                MessageBox.Show("삭제할 쿼리를 선택하세요.", "알림",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var queryName = _selectedQuery.QueryName;
            
            var result = MessageBox.Show(
                $"'{queryName}' 쿼리를 삭제하시겠습니까?\n\n" +
                "이 작업은 되돌릴 수 없습니다!",
                "삭제 확인",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    // 🔥 ID가 있으면 DB에서도 삭제
                    if (_selectedQuery.RowNumber > 0)
                    {
                        _database?.DeleteQuery(_selectedQuery.RowNumber);
                        System.Diagnostics.Debug.WriteLine($"🗑️ DB에서 삭제: {queryName} (ID: {_selectedQuery.RowNumber})");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"🗑️ 신규 항목 삭제 (DB 저장 전): {queryName}");
                    }

                    // 🔥 컬렉션 및 수정 목록에서 제거
                    _queries?.Remove(_selectedQuery);
                    _modifiedQueries.Remove(_selectedQuery);
                    
                    _selectedQuery = null;
                    DbQueryCountTextBlock.Text = $"{_queries?.Count ?? 0}개";
                    
                    UpdateStatus($"'{queryName}' 쿼리가 삭제되었습니다.", Colors.Orange);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"쿼리 삭제 실패:\n{ex.Message}", "오류",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    UpdateStatus($"쿼리 삭제 실패: {ex.Message}", Colors.Red);
                }
            }
        }

        private void ViewDbPathButton_Click(object sender, RoutedEventArgs e)
        {
            var dbPath = _database?.GetDatabasePath() ?? QueryDatabase.GetDefaultDatabasePath();
            var message = new StringBuilder();
            message.AppendLine("데이터베이스 파일 위치:");
            message.AppendLine();
            message.AppendLine(dbPath);
            message.AppendLine();
            message.AppendLine($"파일 존재: {(File.Exists(dbPath) ? "예" : "아니오")}");

            if (File.Exists(dbPath))
            {
                var fileInfo = new FileInfo(dbPath);
                message.AppendLine($"파일 크기: {fileInfo.Length:N0} bytes");
                message.AppendLine($"수정 날짜: {fileInfo.LastWriteTime:yyyy-MM-dd HH:mm:ss}");

                var queries = _database?.GetAllQueries();
                if (queries != null)
                {
                    message.AppendLine($"쿼리 개수: {queries.Count}개");
                }
            }

            MessageBox.Show(message.ToString(), "데이터베이스 정보",
                MessageBoxButton.OK, MessageBoxImage.Information);

            var result = MessageBox.Show("파일 탐색기로 폴더를 열까요?", "확인",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                var directory = Path.GetDirectoryName(dbPath);
                if (!string.IsNullOrEmpty(directory) && Directory.Exists(directory))
                {
                    System.Diagnostics.Process.Start("explorer.exe", directory);
                }
            }
        }

        private void QueriesDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedQuery = QueriesDataGrid.SelectedItem as QueryItem;
            bool hasSelection = _selectedQuery != null;
            
            // 🔥 삭제 버튼만 활성화 제어
            DeleteQueryButton.IsEnabled = hasSelection;
            
            if (hasSelection && _selectedQuery != null)
            {
                UpdateStatus($"선택됨: {_selectedQuery.QueryName}", Colors.Blue);
            }
        }

        private void QueriesDataGrid_BeginningEdit(object sender, System.Windows.Controls.DataGridBeginningEditEventArgs e)
        {
            // 🔥 편집 모드 표시
            EditModeBorder.Visibility = Visibility.Visible;
            UpdateStatus("편집 모드: 변경 후 '💾 변경사항 저장' 버튼을 클릭하세요.", Colors.Orange);
        }

        private void QueriesDataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction == DataGridEditAction.Commit)
            {
                // 🔥 편집된 행을 수정 목록에 추가
                if (e.Row.Item is QueryItem query)
                {
                    _modifiedQueries.Add(query);
                    _hasUnsavedChanges = true;
                    
                    System.Diagnostics.Debug.WriteLine($"📝 쿼리 수정됨: {query.QueryName} (ID: {query.RowNumber})");
                    System.Diagnostics.Debug.WriteLine($"   현재 수정된 항목 수: {_modifiedQueries.Count}");
                }
            }
            
            // 디폴트 체크박스 변경 시 다른 모든 항목의 디폴트를 해제
            if (e.Column != null && e.Column.Header?.ToString() == "디폴트" && !e.Cancel)
            {
                if (e.Row.Item is QueryItem changedQuery && e.EditingElement is CheckBox checkBox)
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        if (changedQuery.DefaultFlagBool && _queries != null)
                        {
                            foreach (var query in _queries)
                            {
                                if (query != changedQuery && query.DefaultFlagBool)
                                {
                                    query.DefaultFlagBool = false;
                                    // 🔥 다른 쿼리도 수정 목록에 추가
                                    _modifiedQueries.Add(query);
                                }
                            }
                            
                            _hasUnsavedChanges = true;
                            QueriesDataGrid.Items.Refresh();
                            UpdateStatus($"'{changedQuery.QueryName}'이(가) 디폴트 폼으로 설정됩니다. '💾 변경사항 저장'을 클릭하세요.", Colors.Orange);
                        }
                    }), System.Windows.Threading.DispatcherPriority.Background);
                }
            }
        }

        private void SaveEditButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 🔥 수정된 항목이 없으면 종료
                if (_modifiedQueries.Count == 0)
                {
                    MessageBox.Show("변경된 항목이 없습니다.", "알림", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                int newCount = 0;
                int updateCount = 0;

                // 🔥 변경된 항목만 처리
                foreach (var query in _modifiedQueries.ToList())
                {
                    // 필수 필드 검증
                    if (string.IsNullOrWhiteSpace(query.QueryName))
                    {
                        MessageBox.Show($"쿼리명을 입력해주세요.\n(ID: {query.RowNumber})", "입력 오류", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    if (string.IsNullOrWhiteSpace(query.UserId))
                    {
                        MessageBox.Show($"'{query.QueryName}'의 User ID를 입력해주세요.", "입력 오류", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    if (string.IsNullOrWhiteSpace(query.Password))
                    {
                        MessageBox.Show($"'{query.QueryName}'의 Password를 입력해주세요.", "입력 오류", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    if (string.IsNullOrWhiteSpace(query.TnsName) && string.IsNullOrWhiteSpace(query.Host))
                    {
                        MessageBox.Show($"'{query.QueryName}'의 TNS 또는 Host를 입력해주세요.", "입력 오류", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // 🔥 ID가 0이면 신규 추가, 아니면 업데이트
                    if (query.RowNumber == 0)
                    {
                        _database?.AddQuery(query);
                        // DB에서 새로 부여된 ID를 가져오기 위해 목록 다시 로드
                        newCount++;
                        System.Diagnostics.Debug.WriteLine($"✅ 신규 저장: {query.QueryName}");
                    }
                    else
                    {
                        _database?.UpdateQuery(query);
                        updateCount++;
                        System.Diagnostics.Debug.WriteLine($"✅ 업데이트: {query.QueryName} (ID: {query.RowNumber})");
                    }
                }

                // 🔥 수정 목록 초기화
                _modifiedQueries.Clear();
                _hasUnsavedChanges = false;
                EditModeBorder.Visibility = Visibility.Collapsed;
                
                // 성공 메시지
                string message = $"저장 완료!\n\n신규: {newCount}개\n수정: {updateCount}개\n총: {newCount + updateCount}개";
                MessageBox.Show(message, "성공", MessageBoxButton.OK, MessageBoxImage.Information);
                
                System.Diagnostics.Debug.WriteLine($"🔔 쿼리 저장 완료 (신규: {newCount}, 수정: {updateCount})");
                
                // 🔥 목록 새로고침 (신규 항목의 ID를 가져오기 위해)
                LoadQueriesFromDatabase();
                
                UpdateStatus($"변경사항이 저장되었습니다. (신규: {newCount}, 수정: {updateCount})", Colors.Green);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"저장 중 오류가 발생했습니다:\n{ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                System.Diagnostics.Debug.WriteLine($"❌ 저장 오류: {ex.Message}");
                UpdateStatus($"저장 실패: {ex.Message}", Colors.Red);
            }
        }

        private void CancelEditButton_Click(object sender, RoutedEventArgs e)
        {
            if (_modifiedQueries.Count == 0)
            {
                MessageBox.Show("변경된 항목이 없습니다.", "알림", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show(
                $"변경된 {_modifiedQueries.Count}개 항목을 취소하고 다시 로드하시겠습니까?",
                "취소 확인",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                LoadQueriesFromDatabase();
                System.Diagnostics.Debug.WriteLine("🔄 변경사항 취소 및 다시 로드");
                UpdateStatus("변경사항이 취소되었습니다.", Colors.Gray);
            }
        }

        private void AddQueryButton_Click(object sender, RoutedEventArgs e)
        {
            if (_queries == null) return;

            // 🔥 신규 쿼리 항목 생성 (ID = 0)
            var newQuery = new QueryItem
            {
                RowNumber = 0, // 신규 항목 표시
                QueryName = "새 쿼리",
                QueryType = "쿼리 실행",
                BizName = "",
                Description2 = "",
                OrderNumber = 0,
                TnsName = "",
                Host = "",
                Port = "",
                ServiceName = "",
                UserId = "",
                Password = "",
                Query = "",
                EnabledFlag = "Y",
                NotifyFlag = "N",
                ExcludeFlag = "N",
                DefaultFlag = "N",
                CountGreaterThan = "",
                CountEquals = "",
                CountLessThan = "",
                ColumnNames = "",
                ColumnValues = ""
            };

            // 컬렉션에 추가
            _queries.Add(newQuery);
            
            // 🔥 수정 목록에 추가 (신규 항목)
            _modifiedQueries.Add(newQuery);
            _hasUnsavedChanges = true;
            EditModeBorder.Visibility = Visibility.Visible;

            DbQueryCountTextBlock.Text = $"{_queries.Count}개";

            // 새로 추가된 행으로 스크롤 및 선택
            QueriesDataGrid.SelectedItem = newQuery;
            QueriesDataGrid.ScrollIntoView(newQuery);
            
            // 첫 번째 편집 가능한 셀로 포커스 이동 (쿼리명)
            QueriesDataGrid.CurrentCell = new System.Windows.Controls.DataGridCellInfo(
                newQuery, 
                QueriesDataGrid.Columns[1]);
            
            QueriesDataGrid.BeginEdit();

            System.Diagnostics.Debug.WriteLine($"✅ 신규 쿼리 추가: {newQuery.QueryName} (ID: {newQuery.RowNumber})");
            UpdateStatus("새 쿼리 항목이 추가되었습니다. 정보를 입력하고 '💾 변경사항 저장'을 클릭하세요.", Colors.Blue);
        }

        private void EditQueryButton_InGrid_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button button && button.Tag is QueryItem query)
            {
                // 신규 항목인 경우
                if (query.RowNumber == 0)
                {
                    MessageBox.Show(
                        "신규 쿼리는 먼저 기본 정보를 입력하고 저장한 후\n" +
                        "쿼리를 편집할 수 있습니다.\n\n" +
                        "순서:\n" +
                        "1. 쿼리명, TNS/Host, User ID, Password 입력\n" +
                        "2. '💾 변경사항 저장' 버튼 클릭\n" +
                        "3. '📝 편집' 버튼 클릭",
                        "안내",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    return;
                }

                var window = new QueryTextEditWindow(query.Query);
                if (window.ShowDialog() == true)
                {
                    query.Query = window.QueryText;
                    
                    // 🔥 수정 목록에 추가
                    _modifiedQueries.Add(query);
                    _hasUnsavedChanges = true;
                    EditModeBorder.Visibility = Visibility.Visible;
                    
                    UpdateStatus($"'{query.QueryName}' 쿼리가 수정되었습니다. '💾 변경사항 저장'을 클릭하세요.", Colors.Orange);
                }
            }
        }

        #endregion

        private void UpdateStatus(string message, Color color)
        {
            StatusTextBlock.Text = $"[{DateTime.Now:HH:mm:ss}] {message}";
            StatusTextBlock.Foreground = new SolidColorBrush(color);

            // 메인 윈도우 상태바도 업데이트
            _sharedData?.UpdateStatusCallback?.Invoke(message, color);
        }
    }
}
