using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using System.Xml;
using System.Reflection;
using FACTOVA_QueryHelper.Services;

namespace FACTOVA_QueryHelper.Controls
{
    public partial class QueryBizTransformView : UserControl
    {
        private readonly OracleDbService _dbService;
        
        public QueryBizTransformView()
        {
            InitializeComponent();
            
            _dbService = new OracleDbService();
            
            // 🔥 SQL 구문 강조 적용
            LoadSqlSyntaxHighlighting();
            
            // 🔥 AvalonEdit Search Panel 활성화 (Ctrl+F)
            ICSharpCode.AvalonEdit.Search.SearchPanel.Install(InputQueryTextBox);
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
                            InputQueryTextBox.SyntaxHighlighting = highlightingDefinition;
                            System.Diagnostics.Debug.WriteLine("✅ SQL Syntax Highlighting loaded successfully");
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("⚠️ SQL.xshd resource not found");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Failed to load SQL syntax highlighting: {ex.Message}");
            }
        }

        /// <summary>
        /// 쿼리 변환 버튼 클릭
        /// </summary>
        private void TransformButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var inputQuery = InputQueryTextBox.Text;
                
                if (string.IsNullOrWhiteSpace(inputQuery))
                {
                    MessageBox.Show("변환할 쿼리를 입력해주세요.", 
                        "입력 오류", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 🔥 비즈 쿼리 파싱
                var queries = ParseBizQueries(inputQuery);
                
                if (queries.Count == 0)
                {
                    MessageBox.Show("유효한 쿼리가 없습니다.\n\n예상 형식:\n[1] [DA:쿼리이름]\n[SQL Statement]\nSELECT ...\n[Parameters Start]\nParam 1 : 값\n[Parameters End]", 
                        "입력 오류", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"📊 Found {queries.Count} queries to transform");

                // 🔥 기존 탭 제거 (첫 번째 탭 제외)
                RemoveAllTabsExceptFirst();

                // 🔥 각 쿼리별로 탭 생성
                for (int i = 0; i < queries.Count; i++)
                {
                    CreateQueryTab(queries[i].Name, queries[i].TransformedQuery, queries[i].Name);
                }

                // 🔥 두 번째 탭으로 이동 (첫 번째 생성된 쿼리 탭)
                if (QueryTabControl.Items.Count > 1)
                {
                    QueryTabControl.SelectedIndex = 1;
                }

                System.Diagnostics.Debug.WriteLine($"✅ {queries.Count} queries transformed successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error in TransformButton_Click: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"   Stack: {ex.StackTrace}");
                MessageBox.Show($"쿼리 변환 중 오류가 발생했습니다:\n{ex.Message}", 
                    "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 전체 초기화 버튼 클릭
        /// </summary>
        private void ClearAllButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var result = MessageBox.Show(
                    "모든 변환된 탭을 삭제하고 초기 상태로 돌아가시겠습니까?",
                    "초기화 확인",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                    return;

                // 🔥 기존 탭 제거 (첫 번째 탭 제외)
                RemoveAllTabsExceptFirst();

                // 🔥 입력 쿼리 초기화
                InputQueryTextBox.Text = string.Empty;

                // 🔥 첫 번째 탭으로 이동
                QueryTabControl.SelectedIndex = 0;

                System.Diagnostics.Debug.WriteLine("✅ All tabs cleared, back to initial state");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error in ClearAllButton_Click: {ex.Message}");
                MessageBox.Show($"초기화 중 오류가 발생했습니다:\n{ex.Message}", 
                    "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 비즈 쿼리 정보
        /// </summary>
        private class BizQueryInfo
        {
            public string Name { get; set; } = "";
            public string OriginalQuery { get; set; } = "";
            public List<string> Parameters { get; set; } = new List<string>();
            public string TransformedQuery { get; set; } = "";
        }

        /// <summary>
        /// 비즈 쿼리 파싱 및 변환
        /// </summary>
        private List<BizQueryInfo> ParseBizQueries(string inputText)
        {
            var queries = new List<BizQueryInfo>();
            
            try
            {
                // 🔥 패턴: [숫자] [DA:이름]
                // 예: [1] [DA:DA_CUS_SEL_ALL_INFO_BY_EQUIP]
                var queryHeaderPattern = @"\[(\d+)\]\s*\[DA:([^\]]+)\]";
                var matches = Regex.Matches(inputText, queryHeaderPattern);
                
                System.Diagnostics.Debug.WriteLine($"🔍 Found {matches.Count} query headers");

                for (int i = 0; i < matches.Count; i++)
                {
                    var match = matches[i];
                    var queryNumber = match.Groups[1].Value;
                    var queryName = match.Groups[2].Value;
                    
                    System.Diagnostics.Debug.WriteLine($"\n📝 Processing Query {queryNumber}: {queryName}");
                    
                    // 🔥 현재 쿼리의 시작 위치
                    var startIndex = match.Index;
                    
                    // 🔥 다음 쿼리의 시작 위치 (또는 끝)
                    var endIndex = i < matches.Count - 1 ? matches[i + 1].Index : inputText.Length;
                    
                    // 🔥 현재 쿼리 블록 추출
                    var queryBlock = inputText.Substring(startIndex, endIndex - startIndex);
                    
                    // 🔥 SQL Statement 추출
                    var sqlStatement = ExtractSqlStatement(queryBlock);
                    
                    if (string.IsNullOrWhiteSpace(sqlStatement))
                    {
                        System.Diagnostics.Debug.WriteLine($"   ⚠️ No SQL statement found for {queryName}");
                        continue;
                    }
                    
                    // 🔥 Parameters 추출
                    var parameters = ExtractParameters(queryBlock);
                    
                    System.Diagnostics.Debug.WriteLine($"   📊 Found {parameters.Count} parameters");
                    foreach (var param in parameters)
                    {
                        System.Diagnostics.Debug.WriteLine($"      - {param}");
                    }
                    
                    // 🔥 쿼리 변환 (? → 파라미터 값)
                    var transformedQuery = TransformQuery(sqlStatement, parameters);
                    
                    queries.Add(new BizQueryInfo
                    {
                        Name = $"[{queryNumber}] [DA:{queryName}]",
                        OriginalQuery = sqlStatement,
                        Parameters = parameters,
                        TransformedQuery = transformedQuery
                    });
                    
                    System.Diagnostics.Debug.WriteLine($"   ✅ Query transformed successfully");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error parsing biz queries: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"   Stack: {ex.StackTrace}");
            }
            
            return queries;
        }

        /// <summary>
        /// SQL Statement 추출
        /// </summary>
        private string ExtractSqlStatement(string queryBlock)
        {
            try
            {
                // [SQL Statement] 이후부터 [Parameters Start] 이전까지
                var sqlStartPattern = @"\[SQL Statement\]";
                var parametersStartPattern = @"\[Parameters Start\]";
                
                var sqlStartMatch = Regex.Match(queryBlock, sqlStartPattern);
                if (!sqlStartMatch.Success)
                    return "";
                
                var startIndex = sqlStartMatch.Index + sqlStartMatch.Length;
                
                var parametersStartMatch = Regex.Match(queryBlock, parametersStartPattern);
                var endIndex = parametersStartMatch.Success ? parametersStartMatch.Index : queryBlock.Length;
                
                var sqlStatement = queryBlock.Substring(startIndex, endIndex - startIndex);
                
                // 🔥 주석 제거 (/* ... */ 형태)
                sqlStatement = Regex.Replace(sqlStatement, @"/\*.*?\*/", "", RegexOptions.Singleline);
                
                return sqlStatement.Trim();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error extracting SQL statement: {ex.Message}");
                return "";
            }
        }

        /// <summary>
        /// Parameters 추출
        /// </summary>
        private List<string> ExtractParameters(string queryBlock)
        {
            var parameters = new List<string>();
            
            try
            {
                // [Parameters Start] ~ [Parameters End] 사이의 Param 추출
                var parametersPattern = @"\[Parameters Start\](.*?)\[Parameters End\]";
                var match = Regex.Match(queryBlock, parametersPattern, RegexOptions.Singleline);
                
                if (!match.Success)
                    return parameters;
                
                var parametersBlock = match.Groups[1].Value;
                
                // Param N : 값 형태 파싱
                var paramPattern = @"Param\s+\d+\s*:\s*(.*)";
                var paramMatches = Regex.Matches(parametersBlock, paramPattern);
                
                foreach (Match paramMatch in paramMatches)
                {
                    var value = paramMatch.Groups[1].Value.Trim();
                    
                    // 🔥 "null" 문자열은 실제 null로 처리하지 않고 빈 문자열로
                    if (value.Equals("null", StringComparison.OrdinalIgnoreCase))
                    {
                        parameters.Add("");
                    }
                    else
                    {
                        parameters.Add(value);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error extracting parameters: {ex.Message}");
            }
            
            return parameters;
        }

        /// <summary>
        /// 쿼리 변환 - ? 를 파라미터로 치환
        /// </summary>
        private string TransformQuery(string query, List<string> parameters)
        {
            try
            {
                var result = query;
                var paramIndex = 0;
                
                // 🔥 ? 를 순서대로 파라미터 값으로 치환
                while (result.Contains("?") && paramIndex < parameters.Count)
                {
                    var paramValue = parameters[paramIndex];
                    
                    // 🔥 빈 문자열이나 null은 그대로 표시
                    var replacement = string.IsNullOrEmpty(paramValue) ? "NULL" : $"'{paramValue}'";
                    
                    // 첫 번째 ? 만 치환
                    var questionMarkIndex = result.IndexOf('?');
                    if (questionMarkIndex >= 0)
                    {
                        result = result.Substring(0, questionMarkIndex) + 
                                replacement + 
                                result.Substring(questionMarkIndex + 1);
                    }
                    
                    paramIndex++;
                }
                
                // 🔥 남은 ? 가 있으면 경고
                if (result.Contains("?"))
                {
                    System.Diagnostics.Debug.WriteLine($"   ⚠️ Warning: Query still contains '?' after transformation. Params: {parameters.Count}, Remaining: ?");
                }
                
                return result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error transforming query: {ex.Message}");
                return query;
            }
        }

        /// <summary>
        /// 첫 번째 탭을 제외한 모든 탭 제거
        /// </summary>
        private void RemoveAllTabsExceptFirst()
        {
            try
            {
                // 역순으로 제거 (인덱스 문제 방지)
                for (int i = QueryTabControl.Items.Count - 1; i > 0; i--)
                {
                    QueryTabControl.Items.RemoveAt(i);
                    System.Diagnostics.Debug.WriteLine($"🗑️ Removed tab at index {i}");
                }

                System.Diagnostics.Debug.WriteLine($"✅ All tabs removed except first. Remaining tabs: {QueryTabControl.Items.Count}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error removing tabs: {ex.Message}");
            }
        }

        /// <summary>
        /// 쿼리 탭 생성 - QueryExecutorControl 사용
        /// </summary>
        private void CreateQueryTab(string tabName, string query, string bizName)
        {
            try
            {
                // 🔥 새 TabItem 생성
                var tabItem = new TabItem
                {
                    Header = tabName
                };

                // 🔥 비즈명 주석 추가
                var queryWithComment = $"/* {bizName} */\n{query}";

                // 🔥 QueryExecutorControl 사용 (Connection + Query Results 포함)
                var queryExecutor = new QueryExecutorControl();
                queryExecutor.SetDbService(_dbService);
                queryExecutor.SetQuery(queryWithComment);

                tabItem.Content = queryExecutor;

                // 🔥 TabControl에 추가
                QueryTabControl.Items.Add(tabItem);

                System.Diagnostics.Debug.WriteLine($"✅ Created tab '{tabName}' with QueryExecutorControl");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error creating tab: {ex.Message}");
            }
        }
    }
}
