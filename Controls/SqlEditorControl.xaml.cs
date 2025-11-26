using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using FACTOVA_QueryHelper.Models;
using FACTOVA_QueryHelper.Services;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using System.Xml;
using System.Reflection;

namespace FACTOVA_QueryHelper.Controls
{
    /// <summary>
    /// SQL 편집기 컨트롤 - AvalonEdit 기반
    /// 기능: SQL 구문 강조, 자동완성, 컨텍스트 메뉴, 바인드 변수 파싱
    /// </summary>
    public partial class SqlEditorControl : UserControl
    {
        // 🔥 쿼리 내부에서 파싱한 Alias-Table 매핑 (쿼리 작성 탭 전용)
        private Dictionary<string, string> _queryParsedAliases = new Dictionary<string, string>();
        
        // 🔥 쿼리 내부에서 파싱한 Alias의 컬럼 캐시 (쿼리 작성 탭 전용)
        private Dictionary<string, List<ColumnInfo>> _queryColumnsCache = new Dictionary<string, List<ColumnInfo>>();
        
        // 🔥 외부에서 등록된 테이블 컬럼 정보 (Single/Multi Join 버튼 전용)
        private Dictionary<string, List<ColumnInfo>> _externalTableColumnsCache = new Dictionary<string, List<ColumnInfo>>();
        
        // 🔥 테이블 목록 캐시 (TB_ 자동완성용)
        private List<string> _tableNamesCache = new List<string>();
        
        // 🔥 테이블 단축어 캐시 (LOT → TB_LOT_MASTER)
        private Dictionary<string, string> _tableShortcuts = new Dictionary<string, string>();

        // 🔥 오프라인 모드용 캐시 서비스
        private MetadataCacheService? _cacheService;

        // 🔥 DB 서비스 (온라인 모드용)
        private OracleDbService? _dbService;
        
        // 🔥 단축어 서비스
        private TableShortcutService? _shortcutService;


        /// <summary>
        /// SQL 텍스트 속성
        /// </summary>
        public string Text
        {
            get => SqlTextEditor.Text;
            set => SqlTextEditor.Text = value;
        }

        /// <summary>
        /// 텍스트 변경 이벤트
        /// </summary>
        public event EventHandler? TextChanged;

        public SqlEditorControl()
        {
            InitializeComponent();
            
            
            
            // 🔥 SQL 구문 강조 적용
            LoadSqlSyntaxHighlighting();
            
            // 🔥 AvalonEdit Search Panel 활성화 (Ctrl+F)
            ICSharpCode.AvalonEdit.Search.SearchPanel.Install(SqlTextEditor);
            
            // 🔥 Ctrl+H (찾기/바꾸기) 단축키 추가
            SqlTextEditor.PreviewKeyDown += SqlTextEditor_GlobalPreviewKeyDown;
        }

        /// <summary>
        /// 전역 키보드 단축키 처리 (Ctrl+H 등)
        /// </summary>
        private void SqlTextEditor_GlobalPreviewKeyDown(object sender, KeyEventArgs e)
        {
        }

        /// <summary>
        /// SQL 구문 강조 로드
        /// </summary>
        private void LoadSqlSyntaxHighlighting()
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                var resourceName = "FACTOVA_QueryHelper.Resources.SQL.xshd";
                
                using (var stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream != null)
                    {
                        using (var reader = new XmlTextReader(stream))
                        {
                            var highlightingDefinition = HighlightingLoader.Load(reader, HighlightingManager.Instance);
                            SqlTextEditor.SyntaxHighlighting = highlightingDefinition;
                            System.Diagnostics.Debug.WriteLine("✅ SQL Syntax Highlighting loaded successfully");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Failed to load SQL syntax highlighting: {ex.Message}");
            }
        }

        /// <summary>
        /// 오프라인 모드용 캐시 서비스 설정
        /// </summary>
        public void SetCacheService(MetadataCacheService? cacheService)
        {
            _cacheService = cacheService;
        }

        /// <summary>
        /// DB 서비스 설정 (온라인 모드용)
        /// </summary>
        public void SetDbService(OracleDbService? dbService)
        {
            _dbService = dbService;
        }

        /// <summary>
        /// 외부에서 테이블 컬럼 정보를 등록 (Single/Multi Join 버튼 전용)
        /// 쿼리 작성 탭의 자동완성과는 독립적으로 동작
        /// </summary>
        public void RegisterTableColumns(string alias, string tableName, List<ColumnInfo> columns)
        {
            var key = alias.ToUpper();
            _externalTableColumnsCache[key] = columns;
            System.Diagnostics.Debug.WriteLine($"🔧 [External] Registered columns for alias '{key}': {columns.Count} columns (for Single/Multi Join buttons only)");
        }

        /// <summary>
        /// 외부에서 테이블 목록을 등록 (TB_ 자동완성용)
        /// </summary>
        public void RegisterTableNames(List<string> tableNames)
        {
            _tableNamesCache = tableNames;
            System.Diagnostics.Debug.WriteLine($"🔧 Registered {tableNames.Count} table names for autocomplete");
        }
        
        /// <summary>
        /// 🔥 단축어 서비스 초기화 (DB 경로 지정)
        /// </summary>
        public void InitializeShortcutService(string databasePath)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"🔍 InitializeShortcutService called with DB: {databasePath}");
                
                _shortcutService = new TableShortcutService(databasePath);
                LoadShortcuts();
                System.Diagnostics.Debug.WriteLine($"✅ TableShortcutService initialized with DB: {databasePath}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Failed to initialize shortcut service: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"   Stack: {ex.StackTrace}");
            }
        }
        
        /// <summary>
        /// 🔥 단축어 목록 로드
        /// </summary>
        private void LoadShortcuts()
        {
            try
            {
                if (_shortcutService == null)
                {
                    System.Diagnostics.Debug.WriteLine("⚠️ LoadShortcuts: _shortcutService is null");
                    return;
                }

                _tableShortcuts.Clear();
                var shortcuts = _shortcutService.GetAll();
                
                System.Diagnostics.Debug.WriteLine($"🔍 LoadShortcuts: Retrieved {shortcuts.Count} shortcuts from DB");
                
                foreach (var shortcut in shortcuts)
                {
                    _tableShortcuts[shortcut.Shortcut.ToUpper()] = shortcut.FullTableName.ToUpper();
                    System.Diagnostics.Debug.WriteLine($"   📌 Added: {shortcut.Shortcut} → {shortcut.FullTableName}");
                }

                System.Diagnostics.Debug.WriteLine($"✅ Loaded {_tableShortcuts.Count} table shortcuts into cache");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Failed to load shortcuts: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"   Stack: {ex.StackTrace}");
            }
        }
        
        /// <summary>
        /// 현재 커서 위치의 쿼리만 추출 (세미콜론으로 구분)
        /// </summary>
        public string GetCurrentQuery()
        {
            var fullText = SqlTextEditor.Text;
            var caretOffset = SqlTextEditor.CaretOffset;

            if (string.IsNullOrWhiteSpace(fullText))
                return string.Empty;

            // 세미콜론 바로 뒤에 커서가 있는지 확인
            if (caretOffset > 0 && caretOffset <= fullText.Length && fullText[caretOffset - 1] == ';')
            {
                caretOffset--;
            }

            // 세미콜론으로 쿼리 분리
            var queries = fullText.Split(new[] { ';' }, StringSplitOptions.None);

            // 현재 커서가 위치한 쿼리 찾기
            int currentPosition = 0;
            foreach (var query in queries)
            {
                int queryEnd = currentPosition + query.Length;
                
                if (caretOffset >= currentPosition && caretOffset <= queryEnd)
                {
                    return query.Trim();
                }
                
                currentPosition = queryEnd + 1;
            }

            return fullText.Trim();
        }

        /// <summary>
        /// 쿼리에서 바인드 변수 추출 (& 또는 @ 문자 인식)
        /// </summary>
        public List<string> ExtractBindVariables(string query)
        {
            var variables = new List<string>();
            if (string.IsNullOrWhiteSpace(query))
                return variables;

            // 🔥 & 또는 @ 문자 뒤의 변수명 추출
            var regex = new System.Text.RegularExpressions.Regex(@"[&@](\w+)");
            var matches = regex.Matches(query);

            var uniqueVariables = new HashSet<string>();
            foreach (System.Text.RegularExpressions.Match match in matches)
            {
                var varName = match.Groups[1].Value;
                if (uniqueVariables.Add(varName))
                {
                    variables.Add(varName);
                    System.Diagnostics.Debug.WriteLine($"  📌 Found bind variable: {varName}");
                }
            }

            System.Diagnostics.Debug.WriteLine($"✅ Extracted {variables.Count} bind variables");
            return variables;
        }

        #region AvalonEdit 이벤트 핸들러

        /// <summary>
        /// 텍스트 변경 시 자동완성 체크
        /// </summary>
        private void SqlTextEditor_TextChanged(object sender, EventArgs e)
        {
            // 부모에게 텍스트 변경 알림
            TextChanged?.Invoke(this, e);
            
            try
            {
                var caretOffset = SqlTextEditor.CaretOffset;
                var text = SqlTextEditor.Text;

                if (caretOffset <= 0 || string.IsNullOrEmpty(text))
                {
                    AutocompletePopup.IsOpen = false;
                    return;
                }

                // 🔥 현재 커서 위치의 쿼리만 추출하여 파싱 (세미콜론 기준)
                var currentQuery = GetCurrentQuery();
                
                // 🔥 디버그 로그: 어떤 쿼리가 파싱되는지 확인
                System.Diagnostics.Debug.WriteLine($"📍 Caret at position {caretOffset}");
                System.Diagnostics.Debug.WriteLine($"📝 Current query being parsed: {currentQuery.Substring(0, Math.Min(100, currentQuery.Length))}...");
                
                ParseQueryForTableAliases(currentQuery);

                // 현재 커서 위치의 단어 찾기
                var lineStart = text.LastIndexOf('\n', Math.Max(0, caretOffset - 1)) + 1;
                var currentLine = text.Substring(lineStart, caretOffset - lineStart);
                
                var wordStart = currentLine.LastIndexOfAny(new[] { ' ', ',', '(', '\t' }) + 1;
                var currentWord = currentLine.Substring(wordStart).Trim();

                // 우선순위 1: Alias.Column 패턴 (쿼리 내부에서 파싱한 것만 사용)
                var dotIndex = currentWord.IndexOf('.');
                if (dotIndex > 0)
                {
                    var alias = currentWord.Substring(0, dotIndex).ToUpper();
                    var filterText = dotIndex < currentWord.Length - 1 ? currentWord.Substring(dotIndex + 1).ToUpper() : "";
                    
                    System.Diagnostics.Debug.WriteLine($"🔍 Looking for alias '{alias}' with filter '{filterText}'");
                    System.Diagnostics.Debug.WriteLine($"   Cached aliases: {string.Join(", ", _queryParsedAliases.Keys)}");
                    
                    // 🔥 쿼리에서 파싱한 alias인지 확인
                    if (_queryColumnsCache.ContainsKey(alias))
                    {
                        var columns = _queryColumnsCache[alias];
                        
                        if (!string.IsNullOrEmpty(filterText))
                        {
                            columns = columns.Where(c => c.ColumnName.ToUpper().Contains(filterText)).ToList();
                        }
                        
                        if (columns.Count > 0)
                        {
                            AutocompleteHeaderText.Text = $"Columns for '{alias}' ({columns.Count} items)";
                            AutocompleteListBox.ItemsSource = columns;
                            ShowPopup();
                            System.Diagnostics.Debug.WriteLine($"✅ Showing {columns.Count} columns for alias '{alias}'");
                            return;
                        }
                    }
                }

                // 우선순위 2: TB_ 테이블 이름 자동완성 (개선)
                // 🔥 TB_로 시작하거나, TB_를 포함하거나, 테이블 이름의 일부를 입력한 경우
                if (!string.IsNullOrEmpty(currentWord) && currentWord.Length >= 2)
                {
                    var filterText = currentWord.ToUpper();
                    
                    // 🔥 우선순위 1: 단축어 매칭 (부분 일치 포함)
                    var matchedShortcuts = _tableShortcuts
                        .Where(kvp => kvp.Key.StartsWith(filterText))
                        .OrderBy(kvp => kvp.Key.Length) // 짧은 것부터 (완전 일치 우선)
                        .Take(10)
                        .Select(kvp => new ColumnInfo
                        {
                            ColumnName = kvp.Value,
                            DataType = "TABLE",
                            Comments = $"Shortcut: {kvp.Key}"
                        })
                        .ToList();
                    
                    if (matchedShortcuts.Count > 0)
                    {
                        AutocompleteHeaderText.Text = $"Shortcuts ({matchedShortcuts.Count} items)";
                        AutocompleteListBox.ItemsSource = matchedShortcuts;
                        ShowPopup();
                        System.Diagnostics.Debug.WriteLine($"✅ Showing {matchedShortcuts.Count} shortcut matches for '{filterText}'");
                        return;
                    }
                    
                    // 🔥 우선순위 2: 테이블명 검색
                    var matchedTables = _tableNamesCache
                        .Where(t => 
                        {
                            var upperTable = t.ToUpper();
                            // TB_로 시작하는 경우
                            if (upperTable.StartsWith(filterText))
                                return true;
                            // TB_를 포함하는 경우
                            if (upperTable.Contains(filterText))
                                return true;
                            // filterText가 TB_로 시작하지 않지만 테이블명에 포함된 경우
                            // 예: "LOT" 입력 → "TB_LOT_INFO" 검색
                            if (!filterText.StartsWith("TB_") && upperTable.Contains(filterText))
                                return true;
                            
                            return false;
                        })
                        .OrderBy(t =>
                        {
                            var upperTable = t.ToUpper();
                            // 우선순위: StartsWith > TB_ + filterText > Contains
                            if (upperTable.StartsWith(filterText))
                                return 0;
                            if (upperTable.StartsWith("TB_" + filterText))
                                return 1;
                            return 2;
                        })
                        .Take(50)
                        .Select(t => new ColumnInfo { ColumnName = t, DataType = "TABLE", Comments = "Table" })
                        .ToList();
                    
                    if (matchedTables.Count > 0)
                    {
                        AutocompleteHeaderText.Text = $"Tables ({matchedTables.Count} items)";
                        AutocompleteListBox.ItemsSource = matchedTables;
                        ShowPopup();
                        System.Diagnostics.Debug.WriteLine($"✅ Showing {matchedTables.Count} tables for '{filterText}' (total cache: {_tableNamesCache.Count})");
                        return;
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"⚠️ No tables matched for '{filterText}' (total cache: {_tableNamesCache.Count})");
                    }
                }
              
                AutocompletePopup.IsOpen = false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error in SqlTextEditor_TextChanged: {ex.Message}");
            }
        }

        /// <summary>
        /// 키보드 입력 처리 (자동완성 내비게이션)
        /// </summary>
        private void SqlTextEditor_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (!AutocompletePopup.IsOpen)
                return;

            switch (e.Key)
            {
                case Key.Down:
                    if (AutocompleteListBox.Items.Count > 0)
                    {
                        if (AutocompleteListBox.SelectedIndex < AutocompleteListBox.Items.Count - 1)
                            AutocompleteListBox.SelectedIndex++;
                        else
                            AutocompleteListBox.SelectedIndex = 0;
                        
                        AutocompleteListBox.ScrollIntoView(AutocompleteListBox.SelectedItem);
                        e.Handled = true;
                    }
                    break;

                case Key.Up:
                    if (AutocompleteListBox.Items.Count > 0)
                    {
                        if (AutocompleteListBox.SelectedIndex > 0)
                            AutocompleteListBox.SelectedIndex--;
                        else
                            AutocompleteListBox.SelectedIndex = AutocompleteListBox.Items.Count - 1;
                        
                        AutocompleteListBox.ScrollIntoView(AutocompleteListBox.SelectedItem);
                        e.Handled = true;
                    }
                    break;

                case Key.Enter:
                case Key.Tab:
                    if (AutocompleteListBox.SelectedItem is ColumnInfo selectedColumn)
                    {
                        InsertAutocompleteText(selectedColumn.ColumnName);
                        AutocompletePopup.IsOpen = false;
                        e.Handled = true;
                    }
                    break;

                case Key.Escape:
                    AutocompletePopup.IsOpen = false;
                    e.Handled = true;
                    break;
            }
        }

        /// <summary>
        /// 우클릭 이벤트
        /// </summary>
        private void SqlTextEditor_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"🖱️ Right-click - Selected: '{SqlTextEditor.SelectedText}'");
        }

        #endregion

        #region 자동완성 로직

        /// <summary>
        /// 쿼리 텍스트에서 FROM 절 파싱하여 Alias-Table 매핑 생성
        /// 쿼리 작성 탭 전용 - 외부 TableSelector와 독립적으로 동작
        /// </summary>
        private void ParseQueryForTableAliases(string query)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(query))
                    return;

                // 🔥 쿼리 파싱 캐시 초기화 (매번 새로 파싱)
                _queryParsedAliases.Clear();
                // 🔥 컬럼 캐시도 초기화 (이전 쿼리의 alias와 혼동 방지)
                _queryColumnsCache.Clear();

                System.Diagnostics.Debug.WriteLine($"🔄 Cleared query cache, parsing new query");

                var patterns = new[]
                {
                    @"\bFROM\s+(\w+)\s+(?:AS\s+)?(\w+)",
                    @",\s*(\w+)\s+(?:AS\s+)?(\w+)",
                    @"\bJOIN\s+(\w+)\s+(?:AS\s+)?(\w+)"
                };

                var sqlKeywords = new[] { "WHERE", "ORDER", "GROUP", "HAVING", "UNION", "INNER", "LEFT", "RIGHT", "OUTER", "JOIN", "ON" };

                foreach (var pattern in patterns)
                {
                    var regex = new System.Text.RegularExpressions.Regex(pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                    var matches = regex.Matches(query);

                    foreach (System.Text.RegularExpressions.Match match in matches)
                    {
                        if (match.Groups.Count >= 3)
                        {
                            var tableName = match.Groups[1].Value.ToUpper();
                            var alias = match.Groups[2].Value.ToUpper();

                            if (sqlKeywords.Contains(alias, StringComparer.OrdinalIgnoreCase))
                                continue;

                            // 🔥 쿼리 내부 alias 매핑 저장
                            _queryParsedAliases[alias] = tableName;
                            System.Diagnostics.Debug.WriteLine($"   📌 Found: {tableName} AS {alias}");

                            // 🔥 쿼리 캐시에 컬럼 로드 (alias 덮어쓰기)
                            LoadColumnsForQueryAlias(alias, tableName);
                        }
                    }
                }

                System.Diagnostics.Debug.WriteLine($"✅ Parsed aliases: {string.Join(", ", _queryParsedAliases.Select(kvp => $"{kvp.Key}→{kvp.Value}"))}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error parsing query for aliases: {ex.Message}");
            }
        }

        /// <summary>
        /// 쿼리 내부 Alias에 대한 테이블 컬럼 정보 로드 (쿼리 작성 탭 전용)
        /// </summary>
        private void LoadColumnsForQueryAlias(string alias, string tableName)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"🔍 [Query] Loading columns for alias '{alias}' -> table '{tableName}'");

                // 🔥 오프라인 모드 우선 (캐시 서비스)
                if (_cacheService != null)
                {
                    var columns = _cacheService.GetTableColumns(tableName);
                    if (columns != null && columns.Count > 0)
                    {
                        _queryColumnsCache[alias] = columns;
                        System.Diagnostics.Debug.WriteLine($"✅ [Query] Loaded {columns.Count} columns from CACHE for alias '{alias}'");
                        return;
                    }
                }

                // 온라인 모드 (DB 조회)
                if (_dbService != null && _dbService.IsConfigured)
                {
                    var columns = System.Threading.Tasks.Task.Run(async () =>
                        await _dbService.GetTableColumnsAsync(tableName)).Result;

                    if (columns != null && columns.Count > 0)
                    {
                        _queryColumnsCache[alias] = columns;
                        System.Diagnostics.Debug.WriteLine($"✅ [Query] Loaded {columns.Count} columns from DB for alias '{alias}'");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Could not load columns for query alias '{alias}': {ex.Message}");
            }
        }

        /// <summary>
        /// 팝업 위치 계산 및 표시
        /// </summary>
        private void ShowPopup()
        {
            var caretPosition = SqlTextEditor.TextArea.Caret.CalculateCaretRectangle();
            var editorPosition = SqlTextEditor.TextArea.TextView.TranslatePoint(new System.Windows.Point(0, 0), this);

            AutocompletePopup.HorizontalOffset = editorPosition.X + caretPosition.Left;
            AutocompletePopup.VerticalOffset = editorPosition.Y + caretPosition.Bottom + 5;

            AutocompletePopup.IsOpen = true;

            if (AutocompleteListBox.Items.Count > 0)
            {
                AutocompleteListBox.SelectedIndex = 0;
                AutocompleteListBox.ScrollIntoView(AutocompleteListBox.SelectedItem);
            }
        }

        /// <summary>
        /// 자동완성 텍스트 삽입
        /// </summary>
        private void InsertAutocompleteText(string columnName)
        {
            try
            {
                var caretOffset = SqlTextEditor.CaretOffset;
                var text = SqlTextEditor.Text;

                var lineStart = text.LastIndexOf('\n', Math.Max(0, caretOffset - 1)) + 1;
                var currentLine = text.Substring(lineStart, caretOffset - lineStart);
                var wordStart = currentLine.LastIndexOfAny(new[] { ' ', ',', '(', '\t' }) + 1;
                var currentWord = currentLine.Substring(wordStart).Trim();

                int replaceStart = lineStart + wordStart;
                int replaceLength = currentWord.Length;

                string insertText;
                var dotIndex = currentWord.IndexOf('.');
                if (dotIndex > 0)
                {
                    var alias = currentWord.Substring(0, dotIndex);
                    insertText = $"{alias}.{columnName}";
                }
                else if (currentWord.StartsWith("TB_", StringComparison.OrdinalIgnoreCase))
                {
                    insertText = columnName;
                }
                else
                {
                    insertText = columnName;
                }

                SqlTextEditor.Document.Replace(replaceStart, replaceLength, insertText);
                SqlTextEditor.CaretOffset = replaceStart + insertText.Length;
                SqlTextEditor.Focus();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error in InsertAutocompleteText: {ex.Message}");
            }
        }

        #endregion

        #region 자동완성 ListBox 이벤트

        private void AutocompleteListBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Enter:
                case Key.Tab:
                    if (AutocompleteListBox.SelectedItem is ColumnInfo selectedColumn)
                    {
                        InsertAutocompleteText(selectedColumn.ColumnName);
                        AutocompletePopup.IsOpen = false;
                        e.Handled = true;
                    }
                    break;

                case Key.Escape:
                    AutocompletePopup.IsOpen = false;
                    SqlTextEditor.Focus();
                    e.Handled = true;
                    break;
            }
        }

        private void AutocompleteListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (AutocompleteListBox.SelectedItem is ColumnInfo selectedColumn)
            {
                InsertAutocompleteText(selectedColumn.ColumnName);
                AutocompletePopup.IsOpen = false;
            }
        }

        #endregion

        #region 컨텍스트 메뉴 이벤트

        /// <summary>
        /// IN 조건 변환
        /// </summary>
        private void InConditionTransform_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedText = SqlTextEditor.SelectedText;

                if (string.IsNullOrWhiteSpace(selectedText))
                {
                    MessageBox.Show("변환할 텍스트를 선택해주세요.\n\n예시:\nvalue1\nvalue2\nvalue3\n\n→ 'value1',\n'value2',\n'value3'",
                        "선택 오류", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 🔥 줄 단위로 분리
                var lines = selectedText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                
                if (lines.Length == 0)
                {
                    MessageBox.Show("유효한 데이터가 없습니다.",
                        "입력 오류", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 🔥 각 줄을 트림하고 작은따옴표로 감싸기
                var transformedValues = lines
                    .Select(line => line.Trim())
                    .Where(line => !string.IsNullOrWhiteSpace(line))
                    .Select(line => $"'{line}'")
                    .ToList();

                if (transformedValues.Count == 0)
                {
                    MessageBox.Show("유효한 데이터가 없습니다.",
                        "입력 오류", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 🔥 쉼표로 연결
                var transformedText = string.Join(",\n", transformedValues);

                // 🔥 선택된 텍스트를 변환된 텍스트로 교체
                var selectionStart = SqlTextEditor.SelectionStart;
                var selectionLength = SqlTextEditor.SelectionLength;

                SqlTextEditor.Document.Replace(selectionStart, selectionLength, transformedText);
                SqlTextEditor.Select(selectionStart, transformedText.Length);
                SqlTextEditor.Focus();
                
                System.Diagnostics.Debug.WriteLine($"✅ IN 조건 변환 완료: {transformedValues.Count}개 값");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error in InConditionTransform_Click: {ex.Message}");
                MessageBox.Show($"IN 조건 변환 중 오류가 발생했습니다:\n{ex.Message}",
                    "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// MULTI_ITEM 변환 (TABLE(MES_MGR.FN_SOM_MULTI_ITEM(',',@변수)))
        /// </summary>
        private void MultiItemTransform_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedText = SqlTextEditor.SelectedText;

                if (string.IsNullOrWhiteSpace(selectedText))
                {
                    MessageBox.Show("변환할 변수명을 선택해주세요.\n\n예시:\n@ORGANIZATION_ID\n\n→ SELECT ITEM_VALUE\n  FROM TABLE(MES_MGR.FN_SOM_MULTI_ITEM(',',@변수))",
                        "선택 오류", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 변수명에서 공백 제거
                var variableName = selectedText.Trim();

                // @ 기호가 없으면 추가
                if (!variableName.StartsWith("@") && !variableName.StartsWith("&"))
                {
                    variableName = "@" + variableName;
                }

                // 🔥 MULTI_ITEM 쿼리 생성
                var transformedText = $@"SELECT ITEM_VALUE
  FROM TABLE(MES_MGR.FN_SOM_MULTI_ITEM(',',{variableName}))";

                // 🔥 선택된 텍스트를 변환된 텍스트로 교체
                var selectionStart = SqlTextEditor.SelectionStart;
                var selectionLength = SqlTextEditor.SelectionLength;

                SqlTextEditor.Document.Replace(selectionStart, selectionLength, transformedText);
                SqlTextEditor.Select(selectionStart, transformedText.Length);
                SqlTextEditor.Focus();
                
                System.Diagnostics.Debug.WriteLine($"✅ MULTI_ITEM 변환 완료: {variableName}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error in MultiItemTransform_Click: {ex.Message}");
                MessageBox.Show($"MULTI_ITEM 변환 중 오류가 발생했습니다:\n{ex.Message}",
                    "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 다국어 변환 (fn_get_name_by_langid)
        /// </summary>
        private void MultiLangTransform_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedText = SqlTextEditor.SelectedText;

                if (string.IsNullOrWhiteSpace(selectedText))
                {
                    MessageBox.Show("변환할 텍스트를 선택해주세요.",
                        "선택 오류", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Alias와 점(.) 제거
                var columnNameOnly = selectedText;
                var dotIndex = selectedText.IndexOf('.');
                if (dotIndex > 0 && dotIndex < selectedText.Length - 1)
                {
                    columnNameOnly = selectedText.Substring(dotIndex + 1);
                }

                var transformedText = $"FN_GET_NAME_BY_LANGID('ko-KR', {selectedText}) AS {columnNameOnly}";

                var selectionStart = SqlTextEditor.SelectionStart;
                var selectionLength = SqlTextEditor.SelectionLength;

                SqlTextEditor.Document.Replace(selectionStart, selectionLength, transformedText);
                SqlTextEditor.CaretOffset = selectionStart + transformedText.Length;
                SqlTextEditor.Focus();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"다국어 변환 중 오류가 발생했습니다:\n{ex.Message}",
                    "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 다국어 단어 (fn_get_multi_lang_dict)
        /// </summary>
        private void MultiLangDict_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedText = SqlTextEditor.SelectedText;

                if (string.IsNullOrWhiteSpace(selectedText))
                {
                    MessageBox.Show("변환할 텍스트를 선택해주세요.",
                        "선택 오류", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Alias와 점(.) 제거
                var columnNameOnly = selectedText;
                var dotIndex = selectedText.IndexOf('.');
                if (dotIndex > 0 && dotIndex < selectedText.Length - 1)
                {
                    columnNameOnly = selectedText.Substring(dotIndex + 1);
                }

                var transformedText = $"FN_GET_MULTI_LANG_DICT('ko-KR', {selectedText}) AS {columnNameOnly}";

                var selectionStart = SqlTextEditor.SelectionStart;
                var selectionLength = SqlTextEditor.SelectionLength;

                SqlTextEditor.Document.Replace(selectionStart, selectionLength, transformedText);
                SqlTextEditor.CaretOffset = selectionStart + transformedText.Length;
                SqlTextEditor.Focus();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"다국어 단어 변환 중 오류가 발생했습니다:\n{ex.Message}",
                    "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 찾기 메뉴
        /// </summary>
        private void FindMenuItem_Click(object sender, RoutedEventArgs e)
        {
            SqlTextEditor.Focus();
        }

        #endregion
    }
}
