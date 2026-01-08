using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using FACTOVA_QueryHelper.Database;
using FACTOVA_QueryHelper.Models;
using FACTOVA_QueryHelper.Services;

namespace FACTOVA_QueryHelper.Controls
{
    public partial class NerpValidationControl : UserControl
    {
        private ObservableCollection<SelectableConnectionInfo> _connectionInfos;
        private SharedDataContext? _sharedData;
        private QueryDatabase? _database;
        private CancellationTokenSource? _cancellationTokenSource;
        private List<QueryItem> _queries = new List<QueryItem>();
        private Dictionary<string, List<QueryItem>> _queriesByGroup = new Dictionary<string, List<QueryItem>>();
        private string _currentQuery = string.Empty;
        private QueryItem? _selectedQueryItem = null;

        public NerpValidationControl()
        {
            InitializeComponent();
            
            _connectionInfos = new ObservableCollection<SelectableConnectionInfo>();
            
            ConnectionMultiComboBox.ItemsSource = _connectionInfos;
            
            this.KeyDown += NerpValidationControl_KeyDown;
            
            // 🔥 오늘부터 +5일까지로 기본값 설정
            DateFromPicker.SelectedDate = DateTime.Today;
            DateToPicker.SelectedDate = DateTime.Today.AddDays(5);
        }

        public void SetSharedDataContext(SharedDataContext sharedData)
        {
            _sharedData = sharedData;
            _database = new QueryDatabase(sharedData.Settings.DatabasePath);
            
            LoadConnectionInfos();
            LoadQueries();
        }

        private void LoadConnectionInfos()
        {
            if (_sharedData == null) return;

            try
            {
                var connectionService = new ConnectionInfoService(_sharedData.Settings.DatabasePath);
                var allConnections = connectionService.GetAllConnections();
                
                _connectionInfos.Clear();
                foreach (var conn in allConnections.OrderBy(c => c.Name))
                {
                    _connectionInfos.Add(new SelectableConnectionInfo(conn));
                }
                
                System.Diagnostics.Debug.WriteLine($"✅ Loaded {_connectionInfos.Count} connection infos for NERP validation");
                
                // 🔥 초기 텍스트 설정
                UpdateConnectionComboBoxText();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Failed to load connection infos: {ex.Message}");
            }
        }

        private void LoadQueries()
        {
            if (_database == null) return;

            try
            {
                var allQueries = _database.GetAllQueries();
                
                System.Diagnostics.Debug.WriteLine($"===== All Queries Count: {allQueries.Count} =====");
                
                // "NERP 검증" 타입 쿼리만 필터링
                _queries = allQueries
                    .Where(q => q.QueryType == "NERP 검증")
                    .OrderBy(q => q.QueryName)  // 🔥 그룹명 기준 정렬
                    .ThenBy(q => q.Version)
                    .ToList();
                
                System.Diagnostics.Debug.WriteLine($"===== NERP 검증 Queries Count: {_queries.Count} =====");
                
                // 🔥 디버그: 각 쿼리의 QueryName과 Version 출력
                foreach (var q in _queries)
                {
                    System.Diagnostics.Debug.WriteLine($"  - QueryName(그룹명): '{q.QueryName}', BizName: '{q.BizName}', Version: '{q.Version}'");
                }
                
                // 🔥 QueryName(그룹명) 기준으로 그룹화 (BizName 대신 QueryName 사용)
                _queriesByGroup = _queries
                    .GroupBy(q => string.IsNullOrWhiteSpace(q.QueryName) ? "(미지정)" : q.QueryName)
                    .ToDictionary(g => g.Key, g => g.ToList());
                
                // 🔥 디버그: 그룹별 쿼리 개수 출력
                foreach (var group in _queriesByGroup)
                {
                    System.Diagnostics.Debug.WriteLine($"  그룹명 '{group.Key}': {group.Value.Count} queries");
                    foreach (var q in group.Value)
                    {
                        System.Diagnostics.Debug.WriteLine($"    - Version '{q.Version}': {q.BizName}");
                    }
                }
                
                // 🔥 그룹 ComboBox에 그룹명(QueryName) 목록 설정
                var groupNames = _queriesByGroup.Keys.OrderBy(k => k).ToList();
                
                System.Diagnostics.Debug.WriteLine($"📋 그룹명(QueryName) for ComboBox: {string.Join(", ", groupNames)}");
                
                QueryGroupComboBox.ItemsSource = groupNames;
                
                if (groupNames.Count > 0)
                {
                    QueryGroupComboBox.SelectedIndex = 0;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("⚠️ No 'NERP 검증' type queries found!");
                }
                
                System.Diagnostics.Debug.WriteLine($"✅ Loaded {_queries.Count} NERP validation queries in {_queriesByGroup.Count} groups (by QueryName)");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Failed to load queries: {ex.Message}");
            }
        }

        private void QueryGroupComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // 그룹 선택만 하고, 실제 쿼리 매칭은 실행 시 DB Version에 따라 자동으로 처리
            if (QueryGroupComboBox.SelectedItem is string groupName)
            {
                System.Diagnostics.Debug.WriteLine($"📋 Selected Group: '{groupName}' - Queries will be auto-matched by DB version");
            }
        }

        public void SetQuery(string query)
        {
            _currentQuery = query;
        }

        public string GetQuery()
        {
            return _currentQuery;
        }

        private void NerpValidationControl_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if ((e.Key == System.Windows.Input.Key.Enter && System.Windows.Input.Keyboard.Modifiers == System.Windows.Input.ModifierKeys.Control) 
                || e.Key == System.Windows.Input.Key.F8)
            {
                if (ExecuteQueryButton.IsEnabled)
                {
                    ExecuteQueryButton_Click(this, new RoutedEventArgs());
                    e.Handled = true;
                }
            }
        }

        private void ConnectionCheckBox_Click(object sender, RoutedEventArgs e)
        {
            UpdateConnectionComboBoxText();
        }

        private void SelectAllConnections_Click(object sender, RoutedEventArgs e)
        {
            foreach (var conn in _connectionInfos)
            {
                conn.IsSelected = true;
            }
            UpdateConnectionComboBoxText();
        }

        private void DeselectAllConnections_Click(object sender, RoutedEventArgs e)
        {
            foreach (var conn in _connectionInfos)
            {
                conn.IsSelected = false;
            }
            UpdateConnectionComboBoxText();
        }

        private void ConnectionMultiComboBox_DropDownClosed(object sender, EventArgs e)
        {
            UpdateConnectionComboBoxText();
        }

        private void UpdateConnectionComboBoxText()
        {
            var selectedConnections = _connectionInfos.Where(c => c.IsSelected).ToList();
            
            if (selectedConnections.Count == 0)
            {
                ConnectionMultiComboBox.Text = "DB 정보를 선택하세요";
            }
            else if (selectedConnections.Count == _connectionInfos.Count)
            {
                ConnectionMultiComboBox.Text = "전체";
            }
            else if (selectedConnections.Count == 1)
            {
                // 🔥 언더스코어를 이스케이프하여 표시 (_ → __)
                ConnectionMultiComboBox.Text = selectedConnections[0].ConnectionName.Replace("_", "__");
            }
            else
            {
                // 🔥 언더스코어를 이스케이프하여 표시 (_ → __)
                var firstConnName = selectedConnections[0].ConnectionName.Replace("_", "__");
                ConnectionMultiComboBox.Text = $"{firstConnName} 외 {selectedConnections.Count - 1}개";
            }
        }

        private async void ExecuteQueryButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 🔥 선택된 그룹명 가져오기
                var selectedGroupName = QueryGroupComboBox.SelectedItem as string;
                
                if (string.IsNullOrWhiteSpace(selectedGroupName))
                {
                    MessageBox.Show("쿼리 그룹을 선택해주세요.", "입력 오류", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var selectedConnections = _connectionInfos.Where(c => c.IsSelected).ToList();
                
                if (selectedConnections.Count == 0)
                {
                    MessageBox.Show("하나 이상의 DB 정보를 선택해주세요.", "입력 오류", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (_sharedData == null)
                {
                    MessageBox.Show("공유 데이터가 초기화되지 않았습니다.", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (!DateFromPicker.SelectedDate.HasValue || !DateToPicker.SelectedDate.HasValue)
                {
                    MessageBox.Show("조회 기간을 선택해주세요.", "입력 오류", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 🔥 선택된 그룹의 모든 버전 쿼리 가져오기
                if (!_queriesByGroup.TryGetValue(selectedGroupName, out var groupQueries))
                {
                    MessageBox.Show("선택된 그룹의 쿼리를 찾을 수 없습니다.", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                ExecuteQueryButton.IsEnabled = false;
                ExecuteQueryButton.Content = "⏳ Executing...";
                ResultStatusText.Text = $"⏳ Executing '{selectedGroupName}' on {selectedConnections.Count} database(s) with version auto-matching...";
                ResultStatusText.Foreground = new SolidColorBrush(Colors.Orange);
                QueryResultDataGrid.ItemsSource = null;

                _cancellationTokenSource = new CancellationTokenSource();
                var cancellationToken = _cancellationTokenSource.Token;

                // 통합 결과 테이블 생성
                DataTable unifiedResults = new DataTable();
                int totalRows = 0;
                int successCount = 0;
                int errorCount = 0;
                int skippedCount = 0;
                List<string> errors = new List<string>();
                Dictionary<string, int> versionExecutionCount = new Dictionary<string, int>();

                System.Diagnostics.Debug.WriteLine($"📋 Query Group: '{selectedGroupName}' with {groupQueries.Count} version(s)");

                foreach (var selectableConnection in selectedConnections)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    var connection = selectableConnection.ConnectionInfo;
                    var dbVersion = connection.Version?.Trim() ?? "";
                    
                    System.Diagnostics.Debug.WriteLine($"🔌 DB: {connection.Name}, Version: '{dbVersion}'");
                    
                    // 🔥 DB Version에 맞는 쿼리 찾기
                    var matchedQuery = groupQueries.FirstOrDefault(q => 
                        (string.IsNullOrWhiteSpace(q.Version) ? "" : q.Version.Trim()).Equals(dbVersion, StringComparison.OrdinalIgnoreCase));
                    
                    // 버전이 빈 문자열인 경우 버전 없는 쿼리 찾기
                    if (matchedQuery == null && string.IsNullOrWhiteSpace(dbVersion))
                    {
                        matchedQuery = groupQueries.FirstOrDefault(q => string.IsNullOrWhiteSpace(q.Version));
                    }
                    
                    if (matchedQuery == null)
                    {
                        System.Diagnostics.Debug.WriteLine($"⏭️ Skipped {connection.Name}: No query found for version '{dbVersion}'");
                        skippedCount++;
                        continue;
                    }
                    
                    // 버전별 실행 카운트
                    var versionKey = string.IsNullOrWhiteSpace(dbVersion) ? "(버전 없음)" : dbVersion;
                    if (!versionExecutionCount.ContainsKey(versionKey))
                    {
                        versionExecutionCount[versionKey] = 0;
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"✅ Matched query version '{matchedQuery.Version}' for DB '{connection.Name}'");
                    
                    try
                    {
                        ResultStatusText.Text = $"⏳ Executing on {connection.Name} (Ver: {versionKey})...";
                        
                        // 파라미터 치환 (ORG 포함)
                        var processedQuery = ReplaceQueryParameters(matchedQuery.Query, connection.Org);
                        
                        var dbService = new OracleDbService();
                        
                        // TNS 정보 가져오기
                        var tnsName = connection.TNS;
                        
                        if (string.IsNullOrEmpty(tnsName))
                        {
                            errors.Add($"❌ {connection.Name}: TNS 설정이 없습니다");
                            errorCount++;
                            continue;
                        }

                        var selectedTns = _sharedData.TnsEntries.FirstOrDefault(t =>
                            t.Name.Equals(tnsName, StringComparison.OrdinalIgnoreCase));

                        if (selectedTns == null)
                        {
                            errors.Add($"❌ {connection.Name}: TNS '{tnsName}' not found");
                            errorCount++;
                            continue;
                        }

                        bool connected = await dbService.ConfigureAsync(
                            selectedTns,
                            connection.UserId,
                            connection.Password);

                        if (!connected)
                        {
                            errors.Add($"❌ {connection.Name}: Connection failed");
                            errorCount++;
                            continue;
                        }

                        DataTable resultTable = await dbService.ExecuteQueryAsync(processedQuery);
                        
                        dbService.Disconnect();

                        // 첫 번째 결과인 경우 스키마 생성
                        if (unifiedResults.Columns.Count == 0)
                        {
                            // DB_NAME 컬럼 추가
                            unifiedResults.Columns.Add("DB_NAME", typeof(string));
                            
                            // 나머지 컬럼 복사
                            foreach (DataColumn col in resultTable.Columns)
                            {
                                unifiedResults.Columns.Add(col.ColumnName, col.DataType);
                            }
                        }

                        // 결과 데이터를 통합 테이블에 추가
                        foreach (DataRow row in resultTable.Rows)
                        {
                            var newRow = unifiedResults.NewRow();
                            newRow["DB_NAME"] = connection.Name;
                            
                            foreach (DataColumn col in resultTable.Columns)
                            {
                                if (unifiedResults.Columns.Contains(col.ColumnName))
                                {
                                    newRow[col.ColumnName] = row[col.ColumnName];
                                }
                            }
                            
                            unifiedResults.Rows.Add(newRow);
                        }

                        totalRows += resultTable.Rows.Count;
                        successCount++;
                        versionExecutionCount[versionKey]++;
                        
                        System.Diagnostics.Debug.WriteLine($"✅ {connection.Name} (Ver: {versionKey}): {resultTable.Rows.Count} rows");
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"❌ {connection.Name}: {ex.Message}");
                        errorCount++;
                        System.Diagnostics.Debug.WriteLine($"❌ Error executing on {connection.Name}: {ex.Message}");
                    }
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    ResultStatusText.Text = "🛑 Query execution cancelled";
                    ResultStatusText.Foreground = new SolidColorBrush(Colors.Red);
                }
                else
                {
                    QueryResultDataGrid.ItemsSource = unifiedResults.DefaultView;
                    
                    // 🔥 버전별 실행 통계를 포함한 상태 메시지
                    var versionStats = string.Join(", ", versionExecutionCount.Select(kvp => $"{kvp.Key}: {kvp.Value}"));
                    string statusMessage = $"✅ '{selectedGroupName}' executed on {successCount}/{selectedConnections.Count} database(s) [{versionStats}]. Total rows: {totalRows}";
                    
                    if (skippedCount > 0)
                    {
                        statusMessage += $" | ⏭️ {skippedCount} skipped (no matching version)";
                    }
                    
                    if (errorCount > 0)
                    {
                        statusMessage += $" | ⚠️ {errorCount} error(s)";
                        ResultStatusText.Foreground = new SolidColorBrush(Colors.Orange);
                    }
                    else
                    {
                        ResultStatusText.Foreground = new SolidColorBrush(Colors.Green);
                    }
                    
                    ResultStatusText.Text = statusMessage;

                    if (errors.Count > 0)
                    {
                        var errorMessage = string.Join("\n", errors);
                        MessageBox.Show(
                            $"일부 DB에서 오류가 발생했습니다:\n\n{errorMessage}",
                            "실행 결과",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                ResultStatusText.Text = $"❌ Error: {ex.Message}";
                ResultStatusText.Foreground = new SolidColorBrush(Colors.Red);
                QueryResultDataGrid.ItemsSource = null;
                MessageBox.Show($"쿼리 실행 중 오류가 발생했습니다:\n\n{ex.Message}", 
                    "쿼리 오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                ExecuteQueryButton.IsEnabled = true;
                ExecuteQueryButton.Content = "▶️ 조회";
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
        }

        private string ReplaceQueryParameters(string query, string org)
        {
            string result = query;

            // ORG 파라미터 치환
            result = result.Replace("@ORG", $"'{org}'");
            
            // 일자 파라미터 치환
            result = result.Replace("@DATE_FROM", $"'{DateFromPicker.SelectedDate?.ToString("yyyyMMdd") ?? ""}'");
            result = result.Replace("@DATE_TO", $"'{DateToPicker.SelectedDate?.ToString("yyyyMMdd") ?? ""}'");

            return result;
        }

        private void QueryResultDataGrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            e.Column.CanUserSort = true;
            
            // 🔥 숫자 타입 컬럼인지 확인
            bool isNumericColumn = e.PropertyType == typeof(int) || 
                                   e.PropertyType == typeof(long) || 
                                   e.PropertyType == typeof(decimal) || 
                                   e.PropertyType == typeof(double) || 
                                   e.PropertyType == typeof(float) ||
                                   e.PropertyType == typeof(short) ||
                                   e.PropertyType == typeof(Int16) ||
                                   e.PropertyType == typeof(Int32) ||
                                   e.PropertyType == typeof(Int64);
            
            if (e.Column is DataGridTextColumn textColumn)
            {
                var displayStyle = new Style(typeof(TextBlock));
                displayStyle.Setters.Add(new Setter(TextBlock.TextWrappingProperty, TextWrapping.Wrap));
                displayStyle.Setters.Add(new Setter(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center));
                displayStyle.Setters.Add(new Setter(TextBlock.PaddingProperty, new Thickness(5, 3, 5, 3)));
                
                // 🔥 숫자 컬럼은 오른쪽 정렬
                if (isNumericColumn)
                {
                    displayStyle.Setters.Add(new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Right));
                    displayStyle.Setters.Add(new Setter(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Right));
                    
                    // 🔥 숫자 3자리 콤마 포맷 적용
                    textColumn.Binding = new Binding(e.PropertyName)
                    {
                        StringFormat = "#,##0.######" // 소수점 있는 경우도 처리
                    };
                }
                
                textColumn.ElementStyle = displayStyle;
                
                e.Column.MinWidth = 80;
                e.Column.Width = new DataGridLength(1, DataGridLengthUnitType.Auto);
            }

            // DB_NAME 컬럼 스타일 강조
            if (e.PropertyName == "DB__NAME" || e.PropertyName == "DB_NAME")
            {
                if (e.Column is DataGridTextColumn dbNameColumn)
                {
                    var style = new Style(typeof(TextBlock));
                    style.Setters.Add(new Setter(TextBlock.FontWeightProperty, System.Windows.FontWeights.Bold));
                    style.Setters.Add(new Setter(TextBlock.ForegroundProperty, new SolidColorBrush(Color.FromRgb(44, 90, 160))));
                    style.Setters.Add(new Setter(TextBlock.PaddingProperty, new Thickness(5, 3, 5, 3)));
                    style.Setters.Add(new Setter(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center));
                    dbNameColumn.ElementStyle = style;
                }
            }

            // 🔥 GMES_와 NERP_ 접두사를 가진 컬럼 쌍을 자동으로 비교하여 색상 적용
            // 언더스코어 이스케이프 적용 후: GMES__ 또는 NERP__
            string? pairColumnName = null;
            
            // 현재 컬럼이 NERP__로 시작하면 GMES__ 쌍 찾기
            if (e.PropertyName.StartsWith("NERP__"))
            {
                pairColumnName = "GMES__" + e.PropertyName.Substring(6); // "NERP__" 길이 = 6
            }
            // 현재 컬럼이 GMES__로 시작하면 NERP__ 쌍 찾기
            else if (e.PropertyName.StartsWith("GMES__"))
            {
                pairColumnName = "NERP__" + e.PropertyName.Substring(6); // "GMES__" 길이 = 6
            }

            // 쌍이 있으면 비교 스타일 적용
            if (!string.IsNullOrEmpty(pairColumnName))
            {
                System.Diagnostics.Debug.WriteLine($"🎨 Setting up comparison: {e.PropertyName} ↔ {pairColumnName}");
                
                // 🔥 DataTrigger를 사용하여 셀 배경색 설정
                var cellStyle = new Style(typeof(DataGridCell));
                cellStyle.Setters.Add(new Setter(DataGridCell.BackgroundProperty, Brushes.White));
                // 🔥 선택 시에도 검정색 글자 유지
                cellStyle.Setters.Add(new Setter(DataGridCell.ForegroundProperty, Brushes.Black));
                
                // MultiBinding을 사용하여 두 컬럼 값 비교
                var trigger = new DataTrigger
                {
                    Binding = new MultiBinding
                    {
                        Converter = new ColumnPairComparisonConverter(),
                        Bindings =
                        {
                            new Binding(e.PropertyName),
                            new Binding(pairColumnName)
                        }
                    },
                    Value = "MATCH"
                };
                trigger.Setters.Add(new Setter(DataGridCell.BackgroundProperty, new SolidColorBrush(Color.FromRgb(200, 255, 200))));
                trigger.Setters.Add(new Setter(DataGridCell.ForegroundProperty, Brushes.Black));
                cellStyle.Triggers.Add(trigger);
                
                var mismatchTrigger = new DataTrigger
                {
                    Binding = new MultiBinding
                    {
                        Converter = new ColumnPairComparisonConverter(),
                        Bindings =
                        {
                            new Binding(e.PropertyName),
                            new Binding(pairColumnName)
                        }
                    },
                    Value = "MISMATCH"
                };
                mismatchTrigger.Setters.Add(new Setter(DataGridCell.BackgroundProperty, new SolidColorBrush(Color.FromRgb(255, 200, 200))));
                mismatchTrigger.Setters.Add(new Setter(DataGridCell.ForegroundProperty, Brushes.Black));
                cellStyle.Triggers.Add(mismatchTrigger);
                
                // 🔥 선택 시 스타일 (배경색 유지, 글자색 검정)
                var selectedTrigger = new Trigger
                {
                    Property = DataGridCell.IsSelectedProperty,
                    Value = true
                };
                selectedTrigger.Setters.Add(new Setter(DataGridCell.ForegroundProperty, Brushes.Black));
                cellStyle.Triggers.Add(selectedTrigger);
                
                e.Column.CellStyle = cellStyle;
            }
            else
            {
                // 🔥 일반 컬럼도 선택 시 글자색 검정 유지
                var cellStyle = new Style(typeof(DataGridCell));
                cellStyle.Setters.Add(new Setter(DataGridCell.ForegroundProperty, Brushes.Black));
                
                var selectedTrigger = new Trigger
                {
                    Property = DataGridCell.IsSelectedProperty,
                    Value = true
                };
                selectedTrigger.Setters.Add(new Setter(DataGridCell.ForegroundProperty, Brushes.Black));
                selectedTrigger.Setters.Add(new Setter(DataGridCell.BackgroundProperty, new SolidColorBrush(Color.FromRgb(173, 216, 230)))); // 연한 파란색
                cellStyle.Triggers.Add(selectedTrigger);
                
                e.Column.CellStyle = cellStyle;
            }
        }

        private void QueryResultDataGrid_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            // 행 단위 색상은 제거하고 기본 배경색 유지
            e.Row.Background = Brushes.White;
        }

        private void RefreshQueryButton_Click(object sender, RoutedEventArgs e)
        {
            LoadQueries();
        }

        /// <summary>
        /// 엑셀 다운로드 버튼 클릭
        /// </summary>
        private void ExportExcelButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (QueryResultDataGrid.ItemsSource == null)
                {
                    MessageBox.Show("다운로드할 데이터가 없습니다.\n먼저 조회를 실행하세요.", "알림",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var dataView = QueryResultDataGrid.ItemsSource as DataView;
                if (dataView == null || dataView.Count == 0)
                {
                    MessageBox.Show("다운로드할 데이터가 없습니다.", "알림",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // 파일 저장 대화상자
                var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "Excel Files (*.xlsx)|*.xlsx",
                    FileName = $"NERP검증_{QueryGroupComboBox.SelectedItem}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
                    DefaultExt = ".xlsx"
                };

                if (saveFileDialog.ShowDialog() != true)
                    return;

                OfficeOpenXml.ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;

                using (var package = new OfficeOpenXml.ExcelPackage())
                {
                    var worksheet = package.Workbook.Worksheets.Add("NERP 검증 결과");
                    var dataTable = dataView.ToTable();

                    // 헤더 작성
                    for (int col = 0; col < dataTable.Columns.Count; col++)
                    {
                        var columnName = dataTable.Columns[col].ColumnName;
                        // 언더스코어 이스케이프 복원 (__ → _)
                        worksheet.Cells[1, col + 1].Value = columnName.Replace("__", "_");
                    }

                    // 헤더 스타일
                    using (var range = worksheet.Cells[1, 1, 1, dataTable.Columns.Count])
                    {
                        range.Style.Font.Bold = true;
                        range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                        range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(44, 90, 160));
                        range.Style.Font.Color.SetColor(System.Drawing.Color.White);
                        range.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                        range.Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin);
                    }

                    // 데이터 작성
                    for (int row = 0; row < dataTable.Rows.Count; row++)
                    {
                        for (int col = 0; col < dataTable.Columns.Count; col++)
                        {
                            var cellValue = dataTable.Rows[row][col];
                            var cell = worksheet.Cells[row + 2, col + 1];

                            if (cellValue != null && cellValue != DBNull.Value)
                            {
                                // 숫자 타입 처리
                                if (cellValue is decimal || cellValue is double || cellValue is float ||
                                    cellValue is int || cellValue is long)
                                {
                                    cell.Value = cellValue;
                                    cell.Style.Numberformat.Format = "#,##0";
                                    cell.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Right;
                                }
                                else if (cellValue is DateTime dateTime)
                                {
                                    cell.Value = dateTime;
                                    cell.Style.Numberformat.Format = "yyyy-mm-dd hh:mm:ss";
                                }
                                else
                                {
                                    cell.Value = cellValue.ToString();
                                }
                            }

                            cell.Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin);
                        }

                        // GMES/NERP 비교 - 불일치 시 빨간 배경
                        HighlightMismatchRow(worksheet, dataTable, row);
                    }

                    // 열 너비 자동 조정
                    worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                    // 최소/최대 열 너비 설정
                    for (int col = 1; col <= dataTable.Columns.Count; col++)
                    {
                        var column = worksheet.Column(col);
                        if (column.Width < 10)
                            column.Width = 10;
                        else if (column.Width > 50)
                            column.Width = 50;
                    }

                    // 틀 고정 (헤더 행)
                    worksheet.View.FreezePanes(2, 1);

                    // Excel 파일 저장
                    var fileInfo = new System.IO.FileInfo(saveFileDialog.FileName);
                    package.SaveAs(fileInfo);

                    MessageBox.Show($"Excel 파일이 저장되었습니다.\n\n파일: {fileInfo.Name}\n행 수: {dataTable.Rows.Count}건",
                        "완료", MessageBoxButton.OK, MessageBoxImage.Information);

                    // 파일 열기 여부 확인
                    var result = MessageBox.Show("저장된 Excel 파일을 여시겠습니까?", "확인",
                        MessageBoxButton.YesNo, MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = fileInfo.FullName,
                            UseShellExecute = true
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Excel 파일 생성 실패:\n{ex.Message}", "오류",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// GMES/NERP 불일치 행 하이라이트
        /// </summary>
        private void HighlightMismatchRow(OfficeOpenXml.ExcelWorksheet worksheet, DataTable dataTable, int rowIndex)
        {
            // GMES_ 와 NERP_ 컬럼 쌍 찾기
            var gmesColumns = dataTable.Columns.Cast<DataColumn>()
                .Where(c => c.ColumnName.StartsWith("GMES__"))
                .ToList();

            foreach (var gmesCol in gmesColumns)
            {
                var suffix = gmesCol.ColumnName.Substring(6); // "GMES__" 이후
                var nerpColName = "NERP__" + suffix;

                if (!dataTable.Columns.Contains(nerpColName))
                    continue;

                var gmesValue = dataTable.Rows[rowIndex][gmesCol.ColumnName];
                var nerpValue = dataTable.Rows[rowIndex][nerpColName];

                // NULL 또는 빈 값이면 스킵
                if (gmesValue == DBNull.Value || nerpValue == DBNull.Value)
                    continue;

                var gmesStr = gmesValue.ToString()?.Trim() ?? "";
                var nerpStr = nerpValue.ToString()?.Trim() ?? "";

                if (string.IsNullOrEmpty(gmesStr) || string.IsNullOrEmpty(nerpStr))
                    continue;

                bool isMatch;
                if (decimal.TryParse(gmesStr, out decimal gmesNum) && decimal.TryParse(nerpStr, out decimal nerpNum))
                {
                    isMatch = gmesNum == nerpNum;
                }
                else
                {
                    isMatch = gmesStr == nerpStr;
                }

                // 불일치 시 해당 셀들 빨간 배경
                if (!isMatch)
                {
                    var gmesColIndex = dataTable.Columns.IndexOf(gmesCol.ColumnName) + 1;
                    var nerpColIndex = dataTable.Columns.IndexOf(nerpColName) + 1;

                    worksheet.Cells[rowIndex + 2, gmesColIndex].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    worksheet.Cells[rowIndex + 2, gmesColIndex].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(255, 200, 200));

                    worksheet.Cells[rowIndex + 2, nerpColIndex].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    worksheet.Cells[rowIndex + 2, nerpColIndex].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(255, 200, 200));
                }
            }
        }

        public void ReloadShortcuts(string databasePath)
        {
            // QueryTextBox.InitializeShortcutService(databasePath);
        }

        // 🔥 Backward compatibility stub for MainWindow.xaml.cs
        public void RefreshConnectionInfos()
        {
            LoadConnectionInfos();
            LoadQueries();
        }
    }

    /// <summary>
    /// 선택 가능한 ConnectionInfo 래퍼 클래스
    /// </summary>
    public class SelectableConnectionInfo : INotifyPropertyChanged
    {
        private bool _isSelected;

        public ConnectionInfo ConnectionInfo { get; set; }
        
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged(nameof(IsSelected));
                }
            }
        }

        public string ConnectionName => ConnectionInfo.DisplayName;

        public SelectableConnectionInfo(ConnectionInfo connectionInfo)
        {
            ConnectionInfo = connectionInfo;
            _isSelected = false;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// 두 컬럼 값을 비교하여 MATCH/MISMATCH/NONE 반환하는 MultiValueConverter
    /// </summary>
    public class ColumnPairComparisonConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length != 2)
                return "NONE";

            var value1 = values[0];
            var value2 = values[1];

            // DependencyProperty.UnsetValue 체크
            if (value1 == DependencyProperty.UnsetValue || value2 == DependencyProperty.UnsetValue)
                return "NONE";

            // NULL 또는 DBNull 체크
            if (value1 == null || value2 == null || value1 == DBNull.Value || value2 == DBNull.Value)
                return "NONE";

            var str1 = value1.ToString()?.Trim() ?? "";
            var str2 = value2.ToString()?.Trim() ?? "";

            // 빈 문자열 체크
            if (string.IsNullOrEmpty(str1) || string.IsNullOrEmpty(str2))
                return "NONE";

            // 숫자 비교 시도
            if (decimal.TryParse(str1, out decimal num1) && decimal.TryParse(str2, out decimal num2))
            {
                return num1 == num2 ? "MATCH" : "MISMATCH";
            }

            // 문자열 비교
            return str1 == str2 ? "MATCH" : "MISMATCH";
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 컬럼 쌍 비교를 위한 범용 Converter (GMES_/NERP_ 자동 비교) - 레거시, 사용 안 함
    /// </summary>
    public class ColumnComparisonColorConverter : IValueConverter
    {
        private readonly string _currentColumnName;
        private readonly string _pairColumnName;

        public ColumnComparisonColorConverter(string currentColumnName, string pairColumnName)
        {
            _currentColumnName = currentColumnName;
            _pairColumnName = pairColumnName;
        }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is DataGridCell cell && cell.DataContext is DataRowView rowView)
            {
                var row = rowView.Row;

                // 두 컬럼이 모두 존재하는지 확인
                if (row.Table.Columns.Contains(_currentColumnName) && row.Table.Columns.Contains(_pairColumnName))
                {
                    var currentValue = row[_currentColumnName];
                    var pairValue = row[_pairColumnName];

                    // NULL 체크
                    if (currentValue == DBNull.Value || pairValue == DBNull.Value)
                    {
                        return Brushes.White; // 기본 배경색
                    }

                    // 문자열로 변환하여 비교
                    var currentStr = currentValue.ToString()?.Trim() ?? "";
                    var pairStr = pairValue.ToString()?.Trim() ?? "";

                    // 🔥 숫자 형태인지 확인 후 숫자 비교 (소수점, 정수 모두 처리)
                    if (decimal.TryParse(currentStr, out decimal currentNum) &&
                        decimal.TryParse(pairStr, out decimal pairNum))
                    {
                        // 숫자 비교
                        if (currentNum == pairNum)
                        {
                            return "MATCH"; // 일치
                        }
                        else
                        {
                            return "MISMATCH"; // 불일치
                        }
                    }
                    else
                    {
                        // 문자열 비교 (대소문자 구분)
                        if (currentStr == pairStr)
                        {
                            return "MATCH"; // 일치
                        }
                        else if (!string.IsNullOrEmpty(currentStr) && !string.IsNullOrEmpty(pairStr))
                        {
                            return "MISMATCH"; // 불일치
                        }
                    }
                }
            }

            // 기본 배경색 (흰색)
            return Brushes.White;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
