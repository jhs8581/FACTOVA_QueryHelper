using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using FACTOVA_QueryHelper.Models;
using FACTOVA_QueryHelper.Services;
using FACTOVA_QueryHelper.Utilities; // 🔥 DataGridHelper 추가

namespace FACTOVA_QueryHelper.Controls
{
    public partial class QueryExecutorControl : UserControl
    {
        private ObservableCollection<BindVariable> _bindVariables;
        private ObservableCollection<ConnectionInfo> _connectionInfos;
        private OracleDbService? _dbService;
        private bool _isTemporaryConnection = false;
        private DataTable? _currentDataTable;
        private Dictionary<string, object?> _editedCells = new Dictionary<string, object?>();
        // 🔥 SharedDataContext 추가 - TnsEntries 접근용
        private SharedDataContext? _sharedData;

        // 🔥 오프라인 모드용 캐시 서비스
        private MetadataCacheService? _cacheService;

        // 🔥 현재 정렬 상태 저장
        private Button? _currentSortButton = null;
        private enum SortState { None, Ascending, Descending }
        private SortState _currentSortState = SortState.None;

        public QueryExecutorControl()
        {
            InitializeComponent();
            
            _bindVariables = new ObservableCollection<BindVariable>();
            _connectionInfos = new ObservableCollection<ConnectionInfo>();
            
            BindVariablesDataGrid.ItemsSource = _bindVariables;
            ConnectionComboBox.ItemsSource = _connectionInfos;
            
            // 🔥 행 번호 표시 활성화
            DataGridHelper.EnableRowNumbers(QueryResultDataGrid);
            DataGridHelper.EnableRowNumbers(BindVariablesDataGrid);
            
            // 🔥 ConnectionInfo 로드는 SetSharedDataContext 이후로 이동
            // LoadConnectionInfos();
            
            // 🔥 SqlEditorControl의 TextChanged 이벤트 연결
            QueryTextBox.TextChanged += QueryTextBox_TextChanged;
            
            // 🔥 Ctrl+H (찾기/바꾸기) 단축키 추가
            this.KeyDown += QueryExecutorControl_KeyDown;
        }

        /// <summary>
        /// SharedDataContext 설정 (TnsEntries 접근용)
        /// </summary>
        public void SetSharedDataContext(SharedDataContext sharedData)
        {
            _sharedData = sharedData;
            
            // 🔥 SharedData 설정 후 ConnectionInfo 로드
            LoadConnectionInfos();
            
            // 🔥 SqlEditorControl에 단축어 서비스 초기화
            QueryTextBox.InitializeShortcutService(sharedData.Settings.DatabasePath);
            
            // 🔥 접속 정보 변경 이벤트 구독
            if (_sharedData != null)
            {
                _sharedData.ConnectionInfosChanged += OnConnectionInfosChanged;
                System.Diagnostics.Debug.WriteLine("✅ QueryExecutorControl subscribed to ConnectionInfosChanged event");
            }
        }
        
        /// <summary>
        /// 🔥 접속 정보 변경 이벤트 핸들러
        /// </summary>
        private void OnConnectionInfosChanged(object? sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("🔄 QueryExecutorControl: Refreshing connection infos...");
            
            // 접속 정보 새로고침
            RefreshConnectionInfos();
        }

        /// <summary>
        /// 키보드 단축키 처리
        /// </summary>
        private void QueryExecutorControl_KeyDown(object sender, KeyEventArgs e)
        {
            // Ctrl+Enter 또는 F8: 쿼리 실행
            if ((e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.Control) || e.Key == Key.F8)
            {
                if (ExecuteQueryButton.IsEnabled)
                {
                    ExecuteQueryButton_Click(this, new RoutedEventArgs());
                    e.Handled = true;
                }
            }
        }

        private void LoadConnectionInfos()
        {
            try
            {
                // 🔥 SharedData에서 DB 경로 가져오기
                string? dbPath = _sharedData?.Settings.DatabasePath;
                
                var connectionInfoService = new ConnectionInfoService(dbPath);
                var allConnections = connectionInfoService.GetAll();
                
                _connectionInfos.Clear();
                foreach (var conn in allConnections)
                {
                    _connectionInfos.Add(conn);
                }
                
                // 🔥 자동 선택 제거 - 미선택 상태로 유지
                // if (_connectionInfos.Count > 0)
                // {
                //     ConnectionComboBox.SelectedIndex = 0;
                // }
                
                System.Diagnostics.Debug.WriteLine($"✅ Loaded {_connectionInfos.Count} connection infos from: {dbPath ?? "default path"}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Failed to load connection infos: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"   Stack: {ex.StackTrace}");
            }
        }

        public void RefreshConnectionInfos()
        {
            var selectedConnection = ConnectionComboBox.SelectedItem as ConnectionInfo;
            int? selectedId = selectedConnection?.Id;

            LoadConnectionInfos();

            if (selectedId.HasValue)
            {
                var item = _connectionInfos.FirstOrDefault(c => c.Id == selectedId.Value);
                if (item != null)
                {
                    ConnectionComboBox.SelectedItem = item;
                }
            }
            else if (_connectionInfos.Count > 0)
            {
                ConnectionComboBox.SelectedIndex = 0;
            }
        }

        public void SetDbService(OracleDbService dbService)
        {
            _dbService = dbService;
            QueryTextBox.SetDbService(dbService);
        }

        public void SetCacheService(MetadataCacheService? cacheService)
        {
            _cacheService = cacheService;
            QueryTextBox.SetCacheService(cacheService);
        }

        public void SetQuery(string query)
        {
            QueryTextBox.Text = query;
            ParseBindVariables();
        }

        public string GetQuery()
        {
            return QueryTextBox.Text;
        }
        
        public void SetTabHeader(string header)
        {
            var parent = this.Parent;
            while (parent != null && !(parent is TabItem))
            {
                if (parent is FrameworkElement fe)
                    parent = fe.Parent;
                else
                    break;
            }

            if (parent is TabItem tabItem)
            {
                var escapedHeader = header.Replace("_", "__");
                tabItem.Header = escapedHeader;
            }
        }

        private void ParseVariablesButton_Click(object sender, RoutedEventArgs e)
        {
            ParseBindVariables();
        }

        /// <summary>
        /// Input Data 버튼 클릭 - JSON 형식으로 바인드 변수 값 입력
        /// </summary>
        private void InputDataButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // JSON 입력 다이얼로그 열기
                var dialog = new JsonInputDialog
                {
                    Owner = Window.GetWindow(this)
                };

                if (dialog.ShowDialog() == true && dialog.ParsedData != null)
                {
                    int matchedCount = 0;
                    int totalCount = dialog.ParsedData.Count;

                    // 파싱된 JSON 데이터를 바인드 변수에 매핑
                    foreach (var kvp in dialog.ParsedData)
                    {
                        var bindVar = _bindVariables.FirstOrDefault(b => 
                            b.Name.Equals(kvp.Key, StringComparison.OrdinalIgnoreCase));

                        if (bindVar != null)
                        {
                            bindVar.Value = kvp.Value;
                            matchedCount++;
                            System.Diagnostics.Debug.WriteLine($"  ✅ Matched: {kvp.Key} = {kvp.Value}");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"  ⚠️ Not found in bind variables: {kvp.Key}");
                        }
                    }

                    // 결과 메시지
                    if (matchedCount > 0)
                    {
                        MessageBox.Show(
                            $"✅ {matchedCount}개의 바인드 변수 값이 입력되었습니다.\n\n" +
                            $"총 {totalCount}개 중 {matchedCount}개 매칭",
                            "Input Data 완료",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show(
                            "⚠️ 일치하는 바인드 변수가 없습니다.\n\n" +
                            "먼저 'Parse Variables' 버튼을 눌러 바인드 변수를 파싱하세요.",
                            "Input Data",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                    }

                    System.Diagnostics.Debug.WriteLine($"✅ Input Data completed: {matchedCount}/{totalCount} matched");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error in InputDataButton_Click: {ex.Message}");
                MessageBox.Show(
                    $"Input Data 중 오류가 발생했습니다:\n\n{ex.Message}",
                    "오류",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void ParseBindVariables()
        {
            try
            {
                var query = QueryTextBox.GetCurrentQuery();

                if (string.IsNullOrWhiteSpace(query))
                {
                    _bindVariables.Clear();
                    return;
                }

                var bindVars = QueryTextBox.ExtractBindVariables(query);
                var existingValues = _bindVariables.ToDictionary(v => v.Name, v => v.Value);

                _bindVariables.Clear();

                foreach (var varName in bindVars)
                {
                    var bindVar = new BindVariable
                    {
                        Name = varName,
                        Value = existingValues.ContainsKey(varName) ? existingValues[varName] : ""
                    };
                    _bindVariables.Add(bindVar);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error parsing bind variables: {ex.Message}");
            }
        }

        private async void ExecuteQueryButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var query = QueryTextBox.GetCurrentQuery();

                if (string.IsNullOrWhiteSpace(query))
                {
                    MessageBox.Show("실행할 쿼리가 없습니다.", "입력 오류", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (_dbService == null)
                {
                    MessageBox.Show("데이터베이스 서비스가 초기화되지 않았습니다.", "오류", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (ConnectionComboBox.SelectedItem == null)
                {
                    MessageBox.Show("연결 정보를 선택해주세요.", "입력 오류", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var selectedConnection = ConnectionComboBox.SelectedItem as ConnectionInfo;
                if (selectedConnection == null)
                {
                    MessageBox.Show("유효한 연결 정보를 선택해주세요.", "입력 오류", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // DB 연결
                if (!_dbService.IsConfigured)
                {
                    ResultStatusText.Text = "🔌 Connecting to database...";
                    ResultStatusText.Foreground = new SolidColorBrush(Colors.Orange);

                    try
                    {
                        // 🔥 1, 2번째 탭과 동일한 방식으로 연결 - TNS Entry 객체 찾기
                        if (_sharedData == null)
                        {
                            MessageBox.Show("공유 데이터가 초기화되지 않았습니다.", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                            ResultStatusText.Text = "❌ Shared data not initialized";
                            ResultStatusText.Foreground = new SolidColorBrush(Colors.Red);
                            return;
                        }

                        var selectedTns = _sharedData.TnsEntries.FirstOrDefault(t =>
                            t.Name.Equals(selectedConnection.TNS, StringComparison.OrdinalIgnoreCase));

                        if (selectedTns == null)
                        {
                            MessageBox.Show(
                                $"TNS '{selectedConnection.TNS}'를 찾을 수 없습니다.\n\n" +
                                $"확인 사항:\n" +
                                "1. TNS 이름이 tnsnames.ora 파일에 정의되어 있는지 확인\n" +
                                "2. 설정 탭에서 tnsnames.ora 파일 경로를 확인",
                                "TNS 오류",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
                            ResultStatusText.Text = "❌ TNS not found";
                            ResultStatusText.Foreground = new SolidColorBrush(Colors.Red);
                            return;
                        }

                        System.Diagnostics.Debug.WriteLine($"🔌 Connecting with TNS Entry: {selectedTns.Name}");
                        System.Diagnostics.Debug.WriteLine($"   Host: {selectedTns.Host}:{selectedTns.Port}");
                        System.Diagnostics.Debug.WriteLine($"   Service: {selectedTns.ServiceName}");
                        System.Diagnostics.Debug.WriteLine($"   User: {selectedConnection.UserId}");

                        // 🔥 TnsEntry 객체를 사용하여 연결 (1, 2번째 탭과 동일)
                        bool connected = await _dbService.ConfigureAsync(
                            selectedTns,
                            selectedConnection.UserId,
                            selectedConnection.Password);

                        if (!connected)
                        {
                            MessageBox.Show(
                                $"데이터베이스 연결에 실패했습니다.\n\n" +
                                $"TNS 이름: {selectedConnection.TNS}\n" +
                                $"Host: {selectedTns.Host}:{selectedTns.Port}\n" +
                                $"Service: {selectedTns.ServiceName}\n\n" +
                                "확인 사항:\n" +
                                "1. 호스트와 포트가 올바른지 확인\n" +
                                "2. 사용자 ID와 비밀번호가 올바른지 확인\n" +
                                "3. 네트워크 연결 확인",
                                "연결 오류",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
                            ResultStatusText.Text = "❌ Connection failed";
                            ResultStatusText.Foreground = new SolidColorBrush(Colors.Red);
                            return;
                        }

                        _isTemporaryConnection = true;
                        System.Diagnostics.Debug.WriteLine("✅ Connected successfully");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"❌ Connection error: {ex.Message}");
                        MessageBox.Show(
                            $"데이터베이스 연결 중 오류가 발생했습니다:\n\n{ex.Message}\n\n" +
                            $"TNS 이름: {selectedConnection.TNS}\n\n" +
                            "확인 사항:\n" +
                            "1. tnsnames.ora 파일에 TNS 이름이 정의되어 있는지 확인\n" +
                            "2. Oracle Client 설치 확인\n" +
                            "3. 네트워크 연결 확인",
                            "연결 오류",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                        ResultStatusText.Text = $"❌ Connection error";
                        ResultStatusText.Foreground = new SolidColorBrush(Colors.Red);
                        return;
                    }
                }

                ExecuteQueryButton.IsEnabled = false;
                CancelQueryButton.IsEnabled = true;
                ExecuteQueryButton.Content = "⏳ Executing...";
                ResultStatusText.Text = "⏳ Executing query...";
                ResultStatusText.Foreground = new SolidColorBrush(Colors.Orange);
                QueryResultDataGrid.ItemsSource = null;

                var bindVars = QueryTextBox.ExtractBindVariables(query);
                DataTable dataTable;

                if (bindVars.Count > 0)
                {
                    var parameters = new Dictionary<string, object>();
                    foreach (var varName in bindVars)
                    {
                        var bindVar = _bindVariables.FirstOrDefault(b => b.Name == varName);
                        parameters[varName] = bindVar?.Value ?? "";
                    }
                    dataTable = await _dbService.ExecuteQueryWithParametersAsync(query, parameters);
                }
                else
                {
                    dataTable = await _dbService.ExecuteQueryAsync(query);
                }

                QueryResultDataGrid.ItemsSource = dataTable.DefaultView;
                ResultStatusText.Text = $"✅ Query executed successfully! Rows: {dataTable.Rows.Count}";
                ResultStatusText.Foreground = new SolidColorBrush(Colors.Green);
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
                if (_isTemporaryConnection && _dbService != null)
                {
                    _dbService.Disconnect();
                    _isTemporaryConnection = false;
                }
                
                ExecuteQueryButton.IsEnabled = true;
                CancelQueryButton.IsEnabled = false;
                ExecuteQueryButton.Content = "▶️ Execute Query";
            }
        }

        private void CancelQueryButton_Click(object sender, RoutedEventArgs e)
        {
            if (_dbService != null)
            {
                _dbService.CancelQuery();
                ResultStatusText.Text = "🛑 Cancelling query...";
                ResultStatusText.Foreground = new SolidColorBrush(Colors.Red);
                CancelQueryButton.IsEnabled = false;
            }
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var result = MessageBox.Show(
                    "쿼리 결과와 상태를 초기화하시겠습니까?",
                    "초기화 확인",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                    return;

                QueryResultDataGrid.ItemsSource = null;
                _currentDataTable = null;
                _editedCells.Clear();
                _bindVariables.Clear();

                ResultStatusText.Text = "⏸️ Ready to execute query";
                ResultStatusText.Foreground = new SolidColorBrush(Colors.Gray);

                ExecuteQueryButton.IsEnabled = true;
                CancelQueryButton.IsEnabled = false;
                ExecuteQueryButton.Content = "▶️ Execute Query";

                if (_isTemporaryConnection && _dbService != null)
                {
                    _dbService.Disconnect();
                    _isTemporaryConnection = false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"초기화 중 오류가 발생했습니다:\n{ex.Message}", 
                    "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void QueryResultDataGrid_BeginningEdit(object sender, DataGridBeginningEditEventArgs e)
        {
            // 편집 시작
        }

        private void QueryResultDataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction == DataGridEditAction.Commit)
            {
                var rowIndex = e.Row.GetIndex();
                var columnName = e.Column.Header?.ToString() ?? "";
                var editingElement = e.EditingElement as TextBox;
                var newValue = editingElement?.Text;

                var key = $"{rowIndex}_{columnName}";
                _editedCells[key] = newValue;
            }
        }

        private void QueryResultDataGrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            // 정렬 비활성화 (커스텀 정렬 버튼 사용)
            e.Column.CanUserSort = false;
            
            // 긴 텍스트를 위한 설정
            if (e.Column is DataGridTextColumn textColumn)
            {
                // 읽기 모드: 텍스트 래핑 활성화
                var displayStyle = new Style(typeof(TextBlock));
                displayStyle.Setters.Add(new Setter(TextBlock.TextWrappingProperty, TextWrapping.Wrap));
                displayStyle.Setters.Add(new Setter(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Top));
                displayStyle.Setters.Add(new Setter(TextBlock.PaddingProperty, new Thickness(3)));
                textColumn.ElementStyle = displayStyle;
                
                // 편집 모드: TextBox 스타일 설정
                var editStyle = new Style(typeof(TextBox));
                editStyle.Setters.Add(new Setter(TextBox.TextWrappingProperty, TextWrapping.Wrap));
                editStyle.Setters.Add(new Setter(TextBox.AcceptsReturnProperty, true));
                editStyle.Setters.Add(new Setter(TextBox.VerticalScrollBarVisibilityProperty, ScrollBarVisibility.Auto));
                editStyle.Setters.Add(new Setter(TextBox.MaxHeightProperty, 200.0));
                editStyle.Setters.Add(new Setter(TextBox.MinWidthProperty, 100.0));
                textColumn.EditingElementStyle = editStyle;
                
                // 최소 너비 설정
                e.Column.MinWidth = 100;
                // 최대 너비를 제한하지 않음 (사용자가 조절 가능)
                e.Column.Width = new DataGridLength(1, DataGridLengthUnitType.Auto);
            }
        }

        // 🔥 컬럼 헤더 더블클릭 감지용
        private DateTime _lastHeaderClickTime = DateTime.MinValue;
        private DataGridColumnHeader? _lastClickedHeader = null;
        private const int DoubleClickMilliseconds = 500;

        private void ColumnHeader_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (sender is Border border && border.Tag is DataGridColumnHeader header)
                {
                    var now = DateTime.Now;
                    var timeSinceLastClick = (now - _lastHeaderClickTime).TotalMilliseconds;
                    
                    // 더블클릭 감지 (같은 헤더를 짧은 시간 내에 두 번 클릭)
                    if (timeSinceLastClick < DoubleClickMilliseconds && _lastClickedHeader == header)
                    {
                        // 더블클릭: 컬럼 전체 선택
                        QueryResultDataGrid.SelectedCells.Clear();
                        
                        for (int i = 0; i < QueryResultDataGrid.Items.Count; i++)
                        {
                            var item = QueryResultDataGrid.Items[i];
                            var cellInfo = new DataGridCellInfo(item, header.Column);
                            QueryResultDataGrid.SelectedCells.Add(cellInfo);
                        }
                        
                        e.Handled = true;
                        _lastHeaderClickTime = DateTime.MinValue; // 리셋
                        _lastClickedHeader = null;
                    }
                    else
                    {
                        // 첫 번째 클릭: 시간과 헤더 기록 (드래그 가능하도록 이벤트 전파)
                        _lastHeaderClickTime = now;
                        _lastClickedHeader = header;
                        // e.Handled = false; 로 두어 드래그 제스처가 시작되도록 함
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error selecting column: {ex.Message}");
            }
        }

        private void SortButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button button && button.Tag is DataGridColumnHeader header)
                {
                    var displayColumnName = header.Column.Header?.ToString();
                    if (string.IsNullOrEmpty(displayColumnName))
                        return;

                    bool isSameButton = (_currentSortButton == button);
                    
                    SortState newState;
                    if (!isSameButton)
                    {
                        newState = SortState.Ascending;
                    }
                    else
                    {
                        switch (_currentSortState)
                        {
                            case SortState.Ascending:
                                newState = SortState.Descending;
                                break;
                            case SortState.Descending:
                                newState = SortState.None;
                                break;
                            default:
                                newState = SortState.Ascending;
                                break;
                        }
                    }
                    
                    if (QueryResultDataGrid.ItemsSource is DataView dataView)
                    {
                        var actualColumnName = FindActualColumnName(dataView.Table, displayColumnName);
                        
                        if (string.IsNullOrEmpty(actualColumnName))
                            return;
                        
                        string buttonContent;
                        
                        if (newState == SortState.None)
                        {
                            dataView.Sort = string.Empty;
                            buttonContent = "⇅";
                        }
                        else
                        {
                            var direction = newState == SortState.Ascending ? "ASC" : "DESC";
                            var directionSymbol = newState == SortState.Ascending ? "↑" : "↓";
                            
                            dataView.Sort = $"[{actualColumnName}] {direction}";
                            buttonContent = directionSymbol;
                        }
                        
                        button.Content = buttonContent;
                    }
                    
                    if (_currentSortButton != null && _currentSortButton != button)
                    {
                        _currentSortButton.Content = "⇅";
                    }
                    
                    _currentSortButton = button;
                    _currentSortState = newState;
                    
                    e.Handled = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"정렬 중 오류가 발생했습니다:\n{ex.Message}", 
                    "정렬 오류", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private string? FindActualColumnName(DataTable dataTable, string displayName)
        {
            if (dataTable == null || string.IsNullOrEmpty(displayName))
                return null;

            if (dataTable.Columns.Contains(displayName))
                return displayName;

            var unescapedName = displayName.Replace("__", "_");
            if (dataTable.Columns.Contains(unescapedName))
                return unescapedName;

            foreach (DataColumn column in dataTable.Columns)
            {
                var escapedColumnName = column.ColumnName.Replace("_", "__");
                if (escapedColumnName == displayName)
                    return column.ColumnName;
            }

            return null;
        }

        public void RegisterTableColumns(string alias, string tableName, List<ColumnInfo> columns)
        {
            QueryTextBox.RegisterTableColumns(alias, tableName, columns);
        }

        public void RegisterTableNames(List<string> tableNames)
        {
            QueryTextBox.RegisterTableNames(tableNames);
        }
        
        /// <summary>
        /// 🔥 테이블 단축어 재로드
        /// </summary>
        public void ReloadShortcuts(string databasePath)
        {
            QueryTextBox.InitializeShortcutService(databasePath);
            System.Diagnostics.Debug.WriteLine($"✅ Shortcuts reloaded for QueryExecutor with DB: {databasePath}");
        }

        private void QueryTextBox_TextChanged(object sender, EventArgs e)
        {
            // SqlEditorControl의 텍스트 변경 시 처리
        }

        /// <summary>
        /// DataGrid 복사 시 첫 번째 행의 앞 공백 제거
        /// </summary>
        private void QueryResultDataGrid_CopyingRowClipboardContent(object sender, DataGridRowClipboardEventArgs e)
        {
            try
            {
                // 첫 번째 행인 경우에만 앞 공백 제거
                if (e.ClipboardRowContent.Count > 0)
                {
                    var firstItem = e.ClipboardRowContent[0];
                    if (firstItem.Content != null)
                    {
                        var content = firstItem.Content.ToString();
                        if (!string.IsNullOrEmpty(content) && content.StartsWith(" "))
                        {
                            // 앞 공백 제거
                            var trimmedContent = content.TrimStart();
                            
                            // 새로운 DataGridClipboardCellContent 생성
                            e.ClipboardRowContent.Clear();
                            e.ClipboardRowContent.Add(new DataGridClipboardCellContent(
                                firstItem.Item,
                                firstItem.Column,
                                trimmedContent));
                            
                            System.Diagnostics.Debug.WriteLine($"✂️ Trimmed leading space from clipboard content");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error in CopyingRowClipboardContent: {ex.Message}");
            }
        }

        /// <summary>
        /// Connection ComboBox 선택 변경 시 Grid 초기화
        /// </summary>
        private void ConnectionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                // 선택된 연결 정보에 따라 Grid 초기화
                if (ConnectionComboBox.SelectedItem != null)
                {
                    var selectedConnection = ConnectionComboBox.SelectedItem as ConnectionInfo;
                    
                    // 로딩 중 표시
                    ResultStatusText.Text = "🔄 Loading...";
                    QueryResultDataGrid.ItemsSource = null;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error in ConnectionComboBox_SelectionChanged: {ex.Message}");
            }
        }
    }
}
