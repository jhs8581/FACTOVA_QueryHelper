using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

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
            _database = new QueryDatabase();
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

        private void BrowseExcelButton_Click(object sender, RoutedEventArgs e)
        {
            string? filePath = FileDialogManager.OpenExcelFileDialog("Excel 쿼리 파일 선택");
            if (filePath != null)
            {
                ExcelFilePathTextBox.Text = filePath;
                ImportFromExcelButton.IsEnabled = true;
                UpdateStatus("Excel 파일이 선택되었습니다. '가져오기' 버튼을 클릭하세요.", Colors.Blue);
            }
        }

        private void ImportFromExcelButton_Click(object sender, RoutedEventArgs e)
        {
            if (_database == null) return;

            string filePath = ExcelFilePathTextBox.Text;
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                MessageBox.Show("유효한 Excel 파일을 선택하세요.", "알림",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show(
                "기존 데이터베이스의 쿼리를 모두 삭제하고 Excel 파일에서 가져오시겠습니까?\n\n" +
                "• 예: 기존 데이터 삭제 후 가져오기 (덮어쓰기)\n" +
                "• 아니오: 기존 데이터에 추가 (병합)\n" +
                "• 취소: 작업 중단",
                "가져오기 옵션",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Cancel) return;

            try
            {
                UpdateStatus("Excel 파일에서 쿼리를 가져오는 중...", Colors.Orange);

                // Excel 파일에서 쿼리 로드 (시작 행: 2)
                var queries = ExcelManager.LoadQueries(filePath, null, 2);
                bool clearExisting = result == MessageBoxResult.Yes;

                _database.ImportFromExcel(queries, clearExisting);
                LoadQueriesFromDatabase();

                string mode = clearExisting ? "덮어쓰기" : "병합";
                UpdateStatus($"Excel에서 {queries.Count}개 쿼리를 가져왔습니다 ({mode}).", Colors.Green);
                MessageBox.Show($"{queries.Count}개의 쿼리를 성공적으로 가져왔습니다.\n\n모드: {mode}", "완료",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Excel 가져오기 실패:\n{ex.Message}", "오류",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                UpdateStatus($"Excel 가져오기 실패: {ex.Message}", Colors.Red);
            }
        }

        private void LoadFromDbButton_Click(object sender, RoutedEventArgs e)
        {
            LoadQueriesFromDatabase();
        }

        private void ExportToExcelButton_Click(object sender, RoutedEventArgs e)
        {
            if (_database == null) return;

            var queries = _database.GetAllQueries();
            if (queries.Count == 0)
            {
                MessageBox.Show("내보낼 쿼리가 없습니다.", "알림",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                Title = "Excel 파일로 내보내기",
                FileName = $"queries_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
            };

            if (dialog.ShowDialog() != true) return;

            try
            {
                ExcelManager.ExportQueries(queries, dialog.FileName);

                UpdateStatus($"{queries.Count}개 쿼리를 Excel로 내보냈습니다.", Colors.Green);
                MessageBox.Show($"{queries.Count}개의 쿼리를 성공적으로 내보냈습니다.\n\n{dialog.FileName}", "완료",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                // 파일 탐색기로 열기 확인
                var result = MessageBox.Show("파일 탐색기로 파일을 열까요?", "확인",
                    MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{dialog.FileName}\"");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Excel 내보내기 실패:\n{ex.Message}", "오류",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                UpdateStatus($"Excel 내보내기 실패: {ex.Message}", Colors.Red);
            }
        }

        private void DeleteQueryButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedQuery == null)
            {
                MessageBox.Show("삭제할 쿼리를 선택하세요.", "알림",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show(
                $"'{_selectedQuery.QueryName}' 쿼리를 삭제하시겠습니까?\n\n" +
                "이 작업은 되돌릴 수 없습니다!",
                "삭제 확인",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    _database?.DeleteQuery(_selectedQuery.RowNumber);
                    LoadQueriesFromDatabase();
                    UpdateStatus($"'{_selectedQuery.QueryName}' 쿼리가 삭제되었습니다.", Colors.Orange);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"쿼리 삭제 실패:\n{ex.Message}", "오류",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    UpdateStatus($"쿼리 삭제 실패: {ex.Message}", Colors.Red);
                }
            }
        }

        private void ClearAllButton_Click(object sender, RoutedEventArgs e)
        {
            var queries = _database?.GetAllQueries();
            if (queries == null || queries.Count == 0)
            {
                MessageBox.Show("삭제할 쿼리가 없습니다.", "알림",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show(
                $"모든 쿼리({queries.Count}개)를 삭제하시겠습니까?\n\n" +
                "⚠️ 이 작업은 되돌릴 수 없습니다!\n\n" +
                "삭제 전에 'Excel 내보내기'로 백업하는 것을 권장합니다.",
                "전체 삭제 확인",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    _database?.ClearAllQueries();
                    LoadQueriesFromDatabase();
                    UpdateStatus("모든 쿼리가 삭제되었습니다.", Colors.Orange);
                    MessageBox.Show("모든 쿼리가 삭제되었습니다.", "완료",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"전체 삭제 실패:\n{ex.Message}", "오류",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    UpdateStatus($"전체 삭제 실패: {ex.Message}", Colors.Red);
                }
            }
        }

        private void ViewDbPathButton_Click(object sender, RoutedEventArgs e)
        {
            var dbPath = QueryDatabase.GetDatabasePath();
            var message = new StringBuilder();
            message.AppendLine("📁 데이터베이스 파일 위치:");
            message.AppendLine();
            message.AppendLine(dbPath);
            message.AppendLine();
            message.AppendLine($"파일 존재 여부: {(File.Exists(dbPath) ? "✅ 예" : "❌ 아니오")}");

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

            // 폴더 열기 확인
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
            
            EditQueryButton.IsEnabled = hasSelection;
            DeleteQueryButton.IsEnabled = hasSelection;
            
            if (hasSelection)
            {
                UpdateStatus($"선택됨: {_selectedQuery!.QueryName}", Colors.Blue);
            }
        }

        private void EditQueryButton_InGrid_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button button && button.Tag is QueryItem query)
            {
                // 신규 항목인 경우 (ID가 0)
                if (query.RowNumber == 0)
                {
                    MessageBox.Show(
                        "신규 쿼리는 먼저 기본 정보를 입력하고 저장한 후\n" +
                        "쿼리를 편집할 수 있습니다.\n\n" +
                        "순서:\n" +
                        "1. 쿼리명, TNS/Host, User ID, Password 입력\n" +
                        "2. Enter 또는 다른 셀 클릭으로 저장\n" +
                        "3. '📝 쿼리 편집' 버튼 클릭",
                        "안내",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    return;
                }

                var window = new QueryTextEditWindow(query.Query);
                if (window.ShowDialog() == true)
                {
                    query.Query = window.QueryText;
                    
                    try
                    {
                        _database?.UpdateQuery(query);
                        UpdateStatus($"'{query.QueryName}' 쿼리가 수정되었습니다.", Colors.Green);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"쿼리 저장 실패:\n{ex.Message}", "오류",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        UpdateStatus($"쿼리 저장 실패: {ex.Message}", Colors.Red);
                        LoadQueriesFromDatabase(); // 원래 상태로 복원
                    }
                }
            }
        }

        private void QueriesDataGrid_InitializingNewItem(object sender, System.Windows.Controls.InitializingNewItemEventArgs e)
        {
            if (e.NewItem is QueryItem newQuery)
            {
                // 신규 항목 기본값 설정
                newQuery.RowNumber = 0; // 아직 저장되지 않음
                newQuery.EnabledFlag = "Y";
                newQuery.NotifyFlag = "N";
                newQuery.ExcludeFlag = "N";
                newQuery.Port = "1521";
            }
        }

        private void PasswordEditBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.PasswordBox passwordBox && passwordBox.Tag is QueryItem query)
            {
                query.Password = passwordBox.Password;
            }
        }

        private void QueriesDataGrid_CellEditEnding(object sender, System.Windows.Controls.DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction == System.Windows.Controls.DataGridEditAction.Commit)
            {
                // 편집이 완료된 후 DB에 저장
                Dispatcher.InvokeAsync(() =>
                {
                    if (e.Row.Item is QueryItem query)
                    {
                        try
                        {
                            // 신규 항목인 경우
                            if (query.RowNumber == 0)
                            {
                                // 필수 필드 검증
                                if (string.IsNullOrWhiteSpace(query.QueryName))
                                {
                                    MessageBox.Show("쿼리명을 입력하세요.", "입력 오류",
                                        MessageBoxButton.OK, MessageBoxImage.Warning);
                                    _queries?.Remove(query);
                                    return;
                                }

                                if (string.IsNullOrWhiteSpace(query.UserId))
                                {
                                    MessageBox.Show("User ID를 입력하세요.", "입력 오류",
                                        MessageBoxButton.OK, MessageBoxImage.Warning);
                                    _queries?.Remove(query);
                                    return;
                                }

                                if (string.IsNullOrWhiteSpace(query.TnsName) && string.IsNullOrWhiteSpace(query.Host))
                                {
                                    MessageBox.Show("TNS 또는 Host를 입력하세요.", "입력 오류",
                                        MessageBoxButton.OK, MessageBoxImage.Warning);
                                    _queries?.Remove(query);
                                    return;
                                }

                                // DB에 추가
                                _database?.AddQuery(query);
                                
                                // 목록 새로고침하여 ID 가져오기
                                LoadQueriesFromDatabase();
                                
                                UpdateStatus($"'{query.QueryName}' 쿼리가 추가되었습니다.", Colors.Green);
                                MessageBox.Show(
                                    $"'{query.QueryName}' 쿼리가 성공적으로 추가되었습니다.\n\n" +
                                    "이제 '📝 쿼리 편집' 버튼을 클릭하여\n" +
                                    "SQL 쿼리를 입력할 수 있습니다.",
                                    "추가 완료",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Information);
                            }
                            else
                            {
                                // 기존 항목 업데이트
                                _database?.UpdateQuery(query);
                                UpdateStatus($"'{query.QueryName}' 쿼리가 수정되었습니다.", Colors.Green);
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"쿼리 저장 실패:\n{ex.Message}", "오류",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                            UpdateStatus($"쿼리 저장 실패: {ex.Message}", Colors.Red);
                            LoadQueriesFromDatabase(); // 원래 상태로 복원
                        }
                    }
                }, System.Windows.Threading.DispatcherPriority.Background);
            }
        }

        private void AddQueryButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "신규 쿼리 추가 방법:\n\n" +
                "1. DataGrid 하단의 빈 행(*)을 클릭\n" +
                "2. 쿼리명, TNS/Host, User ID, Password 입력\n" +
                "3. Enter 또는 다른 셀 클릭으로 저장\n" +
                "4. 저장 후 '📝 쿼리 편집' 버튼으로 SQL 입력\n\n" +
                "필수 입력:\n" +
                "- 쿼리명\n" +
                "- TNS 또는 Host\n" +
                "- User ID\n" +
                "- Password",
                "쿼리 추가 안내",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void EditQueryButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedQuery == null)
            {
                MessageBox.Show("수정할 쿼리를 선택하세요.", "알림",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // 선택된 쿼리의 '쿼리 편집' 버튼 클릭과 동일한 동작
            var window = new QueryTextEditWindow(_selectedQuery.Query);
            if (window.ShowDialog() == true)
            {
                _selectedQuery.Query = window.QueryText;
                
                try
                {
                    _database?.UpdateQuery(_selectedQuery);
                    UpdateStatus($"'{_selectedQuery.QueryName}' 쿼리가 수정되었습니다.", Colors.Green);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"쿼리 저장 실패:\n{ex.Message}", "오류",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    UpdateStatus($"쿼리 저장 실패: {ex.Message}", Colors.Red);
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