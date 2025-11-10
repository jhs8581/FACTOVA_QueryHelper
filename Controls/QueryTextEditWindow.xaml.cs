using System;
using System.Windows;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using System.Xml;
using System.IO;

namespace FACTOVA_QueryHelper.Controls
{
    public partial class QueryTextEditWindow : Window
    {
        public string QueryText { get; private set; } = string.Empty;

        public QueryTextEditWindow(string initialQuery = "")
        {
            InitializeComponent();
            
            // SQL 구문 강조 정의 로드
            LoadSqlSyntaxHighlighting();
            
            // 초기 텍스트 설정
            if (!string.IsNullOrEmpty(initialQuery))
            {
                QueryTextEditor.Text = initialQuery;
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
                System.Diagnostics.Debug.WriteLine($"구문 강조 로드 실패: {ex.Message}");
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            QueryText = QueryTextEditor.Text;
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
