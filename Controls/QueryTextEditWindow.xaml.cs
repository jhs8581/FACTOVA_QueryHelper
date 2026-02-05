using System;
using System.Windows;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using System.Xml;
using System.IO;
using FACTOVA_QueryHelper.Database;
using System.Linq;

namespace FACTOVA_QueryHelper.Controls
{
    public partial class QueryTextEditWindow : Window
    {
        public string QueryText { get; private set; } = string.Empty;
        private int? _queryId;
        private string _databasePath;
        private bool _isReadOnly;
        private string? _replacedQuery; // 🔥 치환된 쿼리
        
        // 🔥 저장 후 콜백 이벤트
        public event EventHandler? QuerySaved;

        // 🔥 기존 생성자 (하위 호환성 유지)
        public QueryTextEditWindow(string initialQuery = "", bool isReadOnly = false)
            : this(initialQuery, isReadOnly, null, string.Empty, null)
        {
        }

        // 🔥 새 생성자 (쿼리 ID와 DB 경로 포함)
        public QueryTextEditWindow(string initialQuery, bool isReadOnly, int? queryId, string databasePath)
            : this(initialQuery, isReadOnly, queryId, databasePath, null)
        {
        }

        // 🔥 치환된 쿼리도 받는 생성자
        public QueryTextEditWindow(string initialQuery, bool isReadOnly, int? queryId, string databasePath, string? replacedQuery)
        {
            InitializeComponent();
            
            _queryId = queryId;
            _databasePath = databasePath;
            _isReadOnly = isReadOnly;
            _replacedQuery = replacedQuery;
            
            // SQL 구문 강조 정의 로드
            LoadSqlSyntaxHighlighting();
            
            // 초기 텍스트 설정
            if (!string.IsNullOrEmpty(initialQuery))
            {
                QueryTextEditor.Text = initialQuery;
            }
            
            // 🔥 치환된 쿼리가 있으면 복사 버튼 표시
            if (!string.IsNullOrEmpty(replacedQuery))
            {
                CopyReplacedQueryButton.Visibility = Visibility.Visible;
            }
            
            // 🔥 읽기 전용 모드여도 쿼리 ID가 있으면 편집 및 저장 가능
            if (isReadOnly && queryId == null)
            {
                // 완전 읽기 전용 (쿼리 ID 없음)
                SaveButton.Visibility = Visibility.Collapsed;
                QueryTextEditor.IsReadOnly = true;
                CancelButton.Content = "닫기";
            }
            else if (queryId.HasValue)
            {
                // 쿼리 ID가 있으면 편집 및 저장 가능
                SaveButton.Visibility = Visibility.Visible;
                SaveButton.Content = "💾 저장";
                QueryTextEditor.IsReadOnly = false;
                CancelButton.Content = "취소";
            }
            else
            {
                // 새 쿼리 작성 모드
                SaveButton.Visibility = Visibility.Visible;
                QueryTextEditor.IsReadOnly = false;
                CancelButton.Content = "취소";
            }
            
            QueryTextEditor.Focus();
        }

        /// <summary>
        /// SQL 구문 강조 정의를 로드합니다.
        /// </summary>
        private void LoadSqlSyntaxHighlighting()
        {
            // SQL 구문 강조 정의 (XSHD XML 형식)
            string xshdDefinition = @"<?xml version=""1.0""?>
<SyntaxDefinition name=""SQL"" xmlns=""http://icsharpcode.net/sharpdevelop/syntaxdefinition/2008"">
    <Color name=""Comment"" foreground=""Green"" />
    <Color name=""String"" foreground=""#A31515"" />
    <Color name=""Keyword"" foreground=""Blue"" fontWeight=""bold"" />
    <Color name=""Parameter"" foreground=""Orange"" fontWeight=""bold"" />
    
    <RuleSet ignoreCase=""true"">
        <!-- 주석 -->
        <Span color=""Comment"" begin=""--"" />
        <Span color=""Comment"" multiline=""true"" begin=""/\*"" end=""\*/"" />
        
        <!-- 문자열 -->
        <Span color=""String"">
            <Begin>'</Begin>
            <End>'</End>
            <RuleSet>
                <Span begin=""''"" end="""" />
            </RuleSet>
        </Span>
        
        <!-- 🔥 @ 치환 파라미터 (주황색) -->
        <Rule color=""Parameter"">
            @[A-Za-z_][A-Za-z0-9_]*
        </Rule>
        
        <!-- SQL 키워드 -->
        <Keywords color=""Keyword"">
            <Word>SELECT</Word>
            <Word>FROM</Word>
            <Word>WHERE</Word>
            <Word>INSERT</Word>
            <Word>UPDATE</Word>
            <Word>DELETE</Word>
            <Word>INTO</Word>
            <Word>VALUES</Word>
            <Word>SET</Word>
            <Word>ORDER</Word>
            <Word>BY</Word>
            <Word>GROUP</Word>
            <Word>HAVING</Word>
            <Word>DISTINCT</Word>
            <Word>ALL</Word>
            <Word>AS</Word>
            <Word>JOIN</Word>
            <Word>INNER</Word>
            <Word>LEFT</Word>
            <Word>RIGHT</Word>
            <Word>FULL</Word>
            <Word>OUTER</Word>
            <Word>CROSS</Word>
            <Word>ON</Word>
            <Word>AND</Word>
            <Word>OR</Word>
            <Word>NOT</Word>
            <Word>IN</Word>
            <Word>EXISTS</Word>
            <Word>BETWEEN</Word>
            <Word>LIKE</Word>
            <Word>IS</Word>
            <Word>NULL</Word>
            <Word>COUNT</Word>
            <Word>SUM</Word>
            <Word>AVG</Word>
            <Word>MAX</Word>
            <Word>MIN</Word>
            <Word>CREATE</Word>
            <Word>ALTER</Word>
            <Word>DROP</Word>
            <Word>TABLE</Word>
            <Word>VIEW</Word>
            <Word>INDEX</Word>
            <Word>DATABASE</Word>
            <Word>SCHEMA</Word>
            <Word>ADD</Word>
            <Word>MODIFY</Word>
            <Word>COLUMN</Word>
            <Word>CONSTRAINT</Word>
            <Word>PRIMARY</Word>
            <Word>FOREIGN</Word>
            <Word>KEY</Word>
            <Word>UNIQUE</Word>
            <Word>CHECK</Word>
            <Word>DEFAULT</Word>
            <Word>REFERENCES</Word>
            <Word>GRANT</Word>
            <Word>REVOKE</Word>
            <Word>DENY</Word>
            <Word>COMMIT</Word>
            <Word>ROLLBACK</Word>
            <Word>SAVEPOINT</Word>
            <Word>UNION</Word>
            <Word>INTERSECT</Word>
            <Word>EXCEPT</Word>
            <Word>MINUS</Word>
            <Word>CASE</Word>
            <Word>WHEN</Word>
            <Word>THEN</Word>
            <Word>ELSE</Word>
            <Word>END</Word>
            <Word>WITH</Word>
            <Word>RECURSIVE</Word>
            <Word>LIMIT</Word>
            <Word>OFFSET</Word>
            <Word>FETCH</Word>
            <Word>NEXT</Word>
            <Word>FIRST</Word>
            <Word>ROWS</Word>
            <Word>ONLY</Word>
            <Word>TOP</Word>
            <Word>ROWNUM</Word>
            <Word>PARTITION</Word>
            <Word>OVER</Word>
            <Word>ROW_NUMBER</Word>
            <Word>RANK</Word>
            <Word>DENSE_RANK</Word>
            <Word>CONNECT</Word>
            <Word>START</Word>
            <Word>PRIOR</Word>
            <Word>LEVEL</Word>
            <Word>SYSDATE</Word>
            <Word>DUAL</Word>
            <Word>DECODE</Word>
            <Word>NVL</Word>
            <Word>TO_CHAR</Word>
            <Word>TO_DATE</Word>
            <Word>TO_NUMBER</Word>
            <Word>TRUNC</Word>
            <Word>ROUND</Word>
            <Word>SUBSTR</Word>
            <Word>INSTR</Word>
            <Word>REPLACE</Word>
            <Word>TRANSLATE</Word>
            <Word>UPPER</Word>
            <Word>LOWER</Word>
            <Word>TRIM</Word>
            <Word>LTRIM</Word>
            <Word>RTRIM</Word>
            <Word>LPAD</Word>
            <Word>RPAD</Word>
            <Word>CONCAT</Word>
            <Word>LENGTH</Word>
        </Keywords>
    </RuleSet>
</SyntaxDefinition>";

            try
            {
                using (var reader = new XmlTextReader(new StringReader(xshdDefinition)))
                {
                    var definition = HighlightingLoader.Load(reader, HighlightingManager.Instance);
                    QueryTextEditor.SyntaxHighlighting = definition;
                }
            }
            catch (Exception ex)
            {
}
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            QueryText = QueryTextEditor.Text;


// 🔥 쿼리 ID가 있으면 DB에 저장
            if (_queryId.HasValue && !string.IsNullOrEmpty(_databasePath))
            {
                try
                {
// 🔥 DB 파일 존재 여부 확인
                    if (!System.IO.File.Exists(_databasePath))
                    {
MessageBox.Show($"데이터베이스 파일을 찾을 수 없습니다:\n{_databasePath}", "오류",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        return; // 🔥 창을 닫지 않고 리턴
                    }
// 🔥 QueryDatabase 인스턴스 생성 및 DB 작업
                    var database = new QueryDatabase(_databasePath);
var allQueries = database.GetAllQueries();
if (allQueries.Count > 0)
                    {
                        
                    }
                    
                    var query = allQueries.FirstOrDefault(q => q.RowNumber == _queryId.Value);
                    
                    if (query != null)
                    {

query.Query = QueryText;
                        database.UpdateQuery(query);
MessageBox.Show("쿼리가 저장되었습니다.", "저장 완료",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        
                        // 🔥 저장 후 콜백 이벤트 발생
                        QuerySaved?.Invoke(this, EventArgs.Empty);
DialogResult = true;
                        Close();
                    }
                    else
                    {

                        
                        MessageBox.Show($"쿼리를 찾을 수 없습니다.\n\n" +
                            $"쿼리 ID: {_queryId.Value}\n" +
                            $"총 쿼리 수: {allQueries.Count}\n\n" +
                            $"DB가 다른 프로세스에서 수정되었을 수 있습니다.\n" +
                            $"목록을 새로고침해 주세요.", 
                            "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                        return; // 🔥 창을 닫지 않고 리턴
                    }
                }
                catch (UnauthorizedAccessException uaEx)
                {
MessageBox.Show($"데이터베이스 파일에 대한 접근 권한이 없습니다.\n\n" +
                        $"파일: {_databasePath}\n\n" +
                        $"관리자 권한으로 실행하거나 파일 권한을 확인해주세요.\n\n" +
                        $"오류: {uaEx.Message}", 
                        "권한 오류", MessageBoxButton.OK, MessageBoxImage.Error);
                    return; // 🔥 창을 닫지 않고 리턴
                }
                catch (Exception ex)
                {
                    
if (ex.InnerException != null)
                    {
}
                    
                    MessageBox.Show($"쿼리 저장 중 오류가 발생했습니다.\n\n" +
                        $"오류 유형: {ex.GetType().Name}\n" +
                        $"오류 메시지: {ex.Message}\n\n" +
                        $"DB 경로: {_databasePath}\n\n" +
                        $"디버그 출력 창에서 자세한 내용을 확인할 수 있습니다.", 
                        "저장 오류", MessageBoxButton.OK, MessageBoxImage.Error);
                    return; // 🔥 창을 닫지 않고 리턴
                }
            }
            else if (_queryId.HasValue && string.IsNullOrEmpty(_databasePath))
            {
                // 🔥 DB 경로가 없는 경우 경고
MessageBox.Show("데이터베이스 경로가 설정되지 않았습니다.\n\n" +
                    "설정 탭에서 DB 파일 경로를 확인해 주세요.", 
                    "오류", MessageBoxButton.OK, MessageBoxImage.Warning);
                return; // 🔥 창을 닫지 않고 리턴
            }
            else
            {
                // 🔥 쿼리 ID가 없는 경우 - 사용자에게 명확히 알림
                
                
                // 🔥 이 경우는 단순 텍스트 반환 모드이므로 사용자에게 알림
                var result = MessageBox.Show(
                    "쿼리 텍스트만 반환됩니다 (DB에 저장되지 않음).\n\n" +
                    "계속 진행하시겠습니까?",
                    "확인",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);
                
                if (result == MessageBoxResult.Yes)
                {
DialogResult = true;
                    Close();
                }
                else
                {
return; // 🔥 창을 닫지 않음
                }
            }
}

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        /// <summary>
        /// 치환된 쿼리를 클립보드에 복사합니다.
        /// </summary>
        private void CopyReplacedQueryButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_replacedQuery))
            {
                MessageBox.Show("치환된 쿼리가 없습니다.", "알림",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                Clipboard.SetText(_replacedQuery);
                // 🔥 메시지박스 없이 버튼 텍스트로 피드백
                CopyReplacedQueryButton.Content = "✅ 복사됨!";
                
                // 2초 후 원래 텍스트로 복원
                var timer = new System.Windows.Threading.DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(2)
                };
                timer.Tick += (s, args) =>
                {
                    CopyReplacedQueryButton.Content = "📋 치환 쿼리 복사";
                    timer.Stop();
                };
                timer.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"클립보드 복사 실패:\n{ex.Message}", "오류",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
