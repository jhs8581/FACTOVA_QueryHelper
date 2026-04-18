using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Xml;
using System.Reflection;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using FACTOVA_QueryHelper.Services;

namespace FACTOVA_QueryHelper.Controls
{
    public partial class QueryMonitoringBizTransformView : UserControl
    {
        private readonly OracleDbService _dbService;
        private SharedDataContext? _sharedData;

        public QueryMonitoringBizTransformView()
        {
            InitializeComponent();

            _dbService = new OracleDbService();

            // SQL 구문 강조 적용
            LoadSqlSyntaxHighlighting();

            // AvalonEdit Search Panel 활성화 (Ctrl+F)
            ICSharpCode.AvalonEdit.Search.SearchPanel.Install(InputQueryTextBox);
        }

        /// <summary>
        /// SharedDataContext 초기화
        /// </summary>
        public void Initialize(SharedDataContext sharedData)
        {
            _sharedData = sharedData;
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

                using var stream = assembly.GetManifestResourceStream(resourceName);
                if (stream == null) return;

                using var reader = new XmlTextReader(stream);
                var highlightingDefinition = HighlightingLoader.Load(reader, HighlightingManager.Instance);
                InputQueryTextBox.SyntaxHighlighting = highlightingDefinition;
            }
            catch
            {
            }
        }

        /// <summary>
        /// 쿼리 변환 버튼 클릭
        /// </summary>
        private void TransformButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var inputText = InputQueryTextBox.Text;

                if (string.IsNullOrWhiteSpace(inputText))
                {
                    MessageBox.Show("변환할 모니터링 로그를 입력해주세요.",
                        "입력 오류", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var entries = ParseMonitoringEntries(inputText);

                if (entries.Count == 0)
                {
                    MessageBox.Show(
                        "유효한 모니터링 로그가 없습니다.\n\n예상 형식:\n[YYYY-MM-DD_HH:mm:ss.fff] BR_xxx DA_xxx\n{ \"CUR_IN_DATA\": [ { ... } ] }\n------------------------------ SQL Statement\nSELECT ... WHERE ... = ?\n------------------------------ Parameters\nParam 1 : 값",
                        "입력 오류", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 기존 탭 제거 (첫 번째 탭 제외)
                RemoveAllTabsExceptFirst();

                // 1. 통합 탭 먼저 생성
                // 첫 번째 항목으로 변환 결과 탭 생성
                CreateEntryTab(entries[0]);

                // 변환 결과 탭으로 이동
                if (QueryTabControl.Items.Count > 1)
                {
                    QueryTabControl.SelectedIndex = 1;
                }
            }
            catch (Exception ex)
            {
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

                RemoveAllTabsExceptFirst();
                InputQueryTextBox.Text = string.Empty;
                QueryTabControl.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"초기화 중 오류가 발생했습니다:\n{ex.Message}",
                    "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ─────────────────────────────────────────────────────────
        // 모니터링 로그 모델
        // ─────────────────────────────────────────────────────────
        private class MonitoringEntry
        {
            public string TimeStamp { get; set; } = "";
            public string BrName { get; set; } = "";
            public string DaName { get; set; } = "";
            public string JsonBlock { get; set; } = "";
            public string XmlData { get; set; } = "";
            public string OriginalSql { get; set; } = "";
            public List<string> Parameters { get; set; } = new();
            public string TransformedSql { get; set; } = "";

            public string DisplayName =>
                string.IsNullOrEmpty(BrName) ? DaName : $"{BrName} / {DaName}";
        }

        // ─────────────────────────────────────────────────────────
        // 파싱 - 모니터링 로그를 항목 단위로 분리
        // ─────────────────────────────────────────────────────────
        private List<MonitoringEntry> ParseMonitoringEntries(string inputText)
        {
            var entries = new List<MonitoringEntry>();

            // 헤더 패턴: [YYYY-MM-DD_HH:mm:ss.fff] BR_xxx DA_xxx
            // BR/DA 이름은 "_"와 영숫자로 구성된다고 가정
            var headerPattern = @"^\s*\[(?<ts>[^\]]+)\]\s+(?<br>\S+)\s+(?<da>\S+)\s*$";
            var headerRegex = new Regex(headerPattern, RegexOptions.Multiline);

            var matches = headerRegex.Matches(inputText);
            if (matches.Count == 0)
                return entries;

            for (int i = 0; i < matches.Count; i++)
            {
                var match = matches[i];
                var startIndex = match.Index;
                var endIndex = i < matches.Count - 1 ? matches[i + 1].Index : inputText.Length;
                var block = inputText.Substring(startIndex, endIndex - startIndex);

                var entry = new MonitoringEntry
                {
                    TimeStamp = match.Groups["ts"].Value.Trim(),
                    BrName = match.Groups["br"].Value.Trim(),
                    DaName = match.Groups["da"].Value.Trim()
                };

                // 헤더 이후 본문
                var body = block.Substring(match.Length);

                // SQL Statement 분리자 위치
                var sqlSepIndex = FindSeparatorIndex(body, "SQL Statement");
                var paramSepIndex = FindSeparatorIndex(body, "Parameters");

                // JSON 블록 = 헤더 이후 ~ SQL Statement 분리자 직전
                var jsonEnd = sqlSepIndex >= 0 ? sqlSepIndex : body.Length;
                entry.JsonBlock = body.Substring(0, jsonEnd).Trim();

                // SQL = SQL Statement 분리자 다음 ~ Parameters 분리자 직전
                if (sqlSepIndex >= 0)
                {
                    var sqlStart = SkipSeparatorLine(body, sqlSepIndex);
                    var sqlEnd = paramSepIndex >= 0 && paramSepIndex > sqlStart ? paramSepIndex : body.Length;
                    entry.OriginalSql = body.Substring(sqlStart, sqlEnd - sqlStart).Trim();
                }

                // Parameters
                if (paramSepIndex >= 0)
                {
                    var paramStart = SkipSeparatorLine(body, paramSepIndex);
                    var paramText = body.Substring(paramStart);
                    entry.Parameters = ExtractParameters(paramText);
                }

                // 변환
                entry.XmlData = ConvertJsonToXml(entry.JsonBlock);
                entry.TransformedSql = BindParameters(entry.OriginalSql, entry.Parameters);

                entries.Add(entry);
            }

            return entries;
        }

        /// <summary>
        /// "------ Keyword" 형태 분리자의 시작 인덱스를 반환
        /// </summary>
        private int FindSeparatorIndex(string text, string keyword)
        {
            var pattern = $@"^[\s]*-{{2,}}\s*{Regex.Escape(keyword)}\s*$";
            var match = Regex.Match(text, pattern, RegexOptions.Multiline);
            return match.Success ? match.Index : -1;
        }

        /// <summary>
        /// 분리자가 위치한 줄 끝(다음 줄 시작 인덱스)을 반환
        /// </summary>
        private int SkipSeparatorLine(string text, int separatorIndex)
        {
            var newlineIndex = text.IndexOf('\n', separatorIndex);
            return newlineIndex < 0 ? text.Length : newlineIndex + 1;
        }

        /// <summary>
        /// Parameters 블록에서 "Param N : 값" 추출
        /// </summary>
        private List<string> ExtractParameters(string parametersBlock)
        {
            var result = new List<string>();

            var paramPattern = @"^\s*Param\s+(?<idx>\d+)\s*:\s*(?<val>.*)$";
            var matches = Regex.Matches(parametersBlock, paramPattern, RegexOptions.Multiline);

            // 인덱스 순으로 정렬
            var ordered = matches
                .Cast<Match>()
                .Select(m => new
                {
                    Index = int.TryParse(m.Groups["idx"].Value, out var n) ? n : 0,
                    Value = m.Groups["val"].Value.TrimEnd('\r').Trim()
                })
                .OrderBy(p => p.Index);

            foreach (var p in ordered)
            {
                if (p.Value.Equals("null", StringComparison.OrdinalIgnoreCase))
                    result.Add("");
                else
                    result.Add(p.Value);
            }

            return result;
        }

        // ─────────────────────────────────────────────────────────
        // JSON → NewDataSet/XML 변환
        // ─────────────────────────────────────────────────────────
        private string ConvertJsonToXml(string jsonText)
        {
            if (string.IsNullOrWhiteSpace(jsonText))
                return string.Empty;

            try
            {
                using var doc = JsonDocument.Parse(jsonText);
                if (doc.RootElement.ValueKind != JsonValueKind.Object)
                    return $"<!-- JSON root는 객체여야 합니다 -->";

                var sb = new StringBuilder();
                using var writer = XmlWriter.Create(sb, new XmlWriterSettings
                {
                    Indent = true,
                    OmitXmlDeclaration = true
                });

                writer.WriteStartElement("NewDataSet");

                foreach (var prop in doc.RootElement.EnumerateObject())
                {
                    // CUR_ 접두어 제거
                    var elementName = StripCurPrefix(prop.Name);

                    if (prop.Value.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var item in prop.Value.EnumerateArray())
                        {
                            WriteElement(writer, elementName, item);
                        }
                    }
                    else
                    {
                        WriteElement(writer, elementName, prop.Value);
                    }
                }

                writer.WriteEndElement(); // NewDataSet
                writer.Flush();
                return sb.ToString();
            }
            catch (Exception ex)
            {
                return $"<!-- JSON 파싱 오류: {ex.Message} -->\n{jsonText}";
            }
        }

        private static string StripCurPrefix(string name)
        {
            if (string.IsNullOrEmpty(name)) return name;
            return name.StartsWith("CUR_", StringComparison.OrdinalIgnoreCase)
                ? name.Substring(4)
                : name;
        }

        private void WriteElement(XmlWriter writer, string elementName, JsonElement value)
        {
            writer.WriteStartElement(elementName);

            switch (value.ValueKind)
            {
                case JsonValueKind.Object:
                    foreach (var child in value.EnumerateObject())
                    {
                        var childName = StripCurPrefix(child.Name);
                        if (child.Value.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var item in child.Value.EnumerateArray())
                                WriteElement(writer, childName, item);
                        }
                        else
                        {
                            WriteElement(writer, childName, child.Value);
                        }
                    }
                    break;

                case JsonValueKind.Array:
                    foreach (var item in value.EnumerateArray())
                    {
                        WriteElement(writer, "ITEM", item);
                    }
                    break;

                case JsonValueKind.String:
                    writer.WriteString(value.GetString() ?? string.Empty);
                    break;

                case JsonValueKind.Number:
                case JsonValueKind.True:
                case JsonValueKind.False:
                    writer.WriteString(value.ToString());
                    break;

                case JsonValueKind.Null:
                case JsonValueKind.Undefined:
                    // 빈 요소
                    break;
            }

            writer.WriteEndElement();
        }

        // ─────────────────────────────────────────────────────────
        // SQL 의 ? 를 Parameters로 순차 바인딩
        // ─────────────────────────────────────────────────────────
        private string BindParameters(string sql, List<string> parameters)
        {
            if (string.IsNullOrEmpty(sql) || parameters.Count == 0)
                return sql;

            var sb = new StringBuilder(sql.Length + parameters.Count * 16);
            int paramIndex = 0;
            bool inSingleQuote = false;
            bool inDoubleQuote = false;
            bool inLineComment = false;
            bool inBlockComment = false;

            for (int i = 0; i < sql.Length; i++)
            {
                char c = sql[i];
                char next = i + 1 < sql.Length ? sql[i + 1] : '\0';

                // 주석/문자열 상태 추적 (문자열 안의 ? 는 치환하지 않음)
                if (inLineComment)
                {
                    sb.Append(c);
                    if (c == '\n') inLineComment = false;
                    continue;
                }
                if (inBlockComment)
                {
                    sb.Append(c);
                    if (c == '*' && next == '/')
                    {
                        sb.Append(next);
                        i++;
                        inBlockComment = false;
                    }
                    continue;
                }
                if (inSingleQuote)
                {
                    sb.Append(c);
                    if (c == '\'') inSingleQuote = false;
                    continue;
                }
                if (inDoubleQuote)
                {
                    sb.Append(c);
                    if (c == '"') inDoubleQuote = false;
                    continue;
                }

                if (c == '-' && next == '-')
                {
                    inLineComment = true;
                    sb.Append(c);
                    continue;
                }
                if (c == '/' && next == '*')
                {
                    inBlockComment = true;
                    sb.Append(c);
                    continue;
                }
                if (c == '\'')
                {
                    inSingleQuote = true;
                    sb.Append(c);
                    continue;
                }
                if (c == '"')
                {
                    inDoubleQuote = true;
                    sb.Append(c);
                    continue;
                }

                if (c == '?')
                {
                    if (paramIndex < parameters.Count)
                    {
                        var value = parameters[paramIndex];
                        sb.Append(FormatParameterValue(value));
                        paramIndex++;
                    }
                    else
                    {
                        sb.Append(c);
                    }
                    continue;
                }

                sb.Append(c);
            }

            return sb.ToString();
        }

        private static string FormatParameterValue(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "NULL";

            // 숫자라면 그대로
            if (double.TryParse(value, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out _))
            {
                return value;
            }

            // 문자열 → 작은따옴표 이스케이프 후 인용
            return "'" + value.Replace("'", "''") + "'";
        }

        // ─────────────────────────────────────────────────────────
        // 탭 관리
        // ─────────────────────────────────────────────────────────
        private void RemoveAllTabsExceptFirst()
        {
            for (int i = QueryTabControl.Items.Count - 1; i > 0; i--)
            {
                QueryTabControl.Items.RemoveAt(i);
            }
        }

        /// <summary>
        /// 개별 항목 탭
        /// </summary>
        private void CreateEntryTab(MonitoringEntry entry)
        {
            var tabItem = new TabItem { Header = "🔄 쿼리 변환" };

            var sb = new StringBuilder();
            AppendEntryAsComment(sb, entry);
            sb.AppendLine(entry.TransformedSql);

            var executor = new QueryExecutorControl();
            if (_sharedData != null)
                executor.SetSharedDataContext(_sharedData);
            executor.SetDbService(_dbService);
            executor.SetQuery(sb.ToString());

            tabItem.Content = executor;
            QueryTabControl.Items.Add(tabItem);
        }

        /// <summary>
        /// 엔트리 메타정보(타임스탬프/BR/DA/XML 데이터)를 SQL 주석으로 출력
        /// </summary>
        private void AppendEntryAsComment(StringBuilder sb, MonitoringEntry entry)
        {
            sb.AppendLine("/* ========================================");
            if (!string.IsNullOrEmpty(entry.TimeStamp))
                sb.AppendLine($"   [{entry.TimeStamp}]");
            sb.AppendLine($"   BR : {entry.BrName}");
            sb.AppendLine($"   DA : {entry.DaName}");
            sb.AppendLine("   ----- IN DATA (XML) -----");

            if (!string.IsNullOrEmpty(entry.XmlData))
            {
                // 🔥 SQL 주석 안에 들어가므로 XML 엔티티(&lt; &gt; ...)를 원래 문자로 복원
                //   - GetCurrentQuery()가 ';'로 쿼리를 자르는데 &lt; &gt;에 ';'이 포함되어
                //     주석 블록이 잘리면 ExtractBindVariables가 lt/gt를 변수로 인식하는 문제 방지
                //   - 사람이 읽기에도 더 편함
                var displayXml = UnescapeXmlEntities(entry.XmlData);
                foreach (var line in displayXml.Split('\n'))
                {
                    sb.AppendLine("   " + line.TrimEnd('\r'));
                }
            }

            if (entry.Parameters.Count > 0)
            {
                sb.AppendLine("   ----- Parameters -----");
                for (int i = 0; i < entry.Parameters.Count; i++)
                {
                    sb.AppendLine($"   Param {i + 1} : {entry.Parameters[i]}");
                }
            }

            sb.AppendLine("   ======================================== */");
            sb.AppendLine();
        }

        /// <summary>
        /// XML 엔티티(&lt; &gt; &amp; &quot; &apos;)를 원래 문자로 복원
        /// SQL 주석 표시용. ';'을 제거하여 GetCurrentQuery()의 ';' 분리에 영향 없게 함
        /// </summary>
        private static string UnescapeXmlEntities(string xml)
        {
            if (string.IsNullOrEmpty(xml)) return xml;
            return xml
                .Replace("&lt;", "<")
                .Replace("&gt;", ">")
                .Replace("&quot;", "\"")
                .Replace("&apos;", "'")
                .Replace("&amp;", "&");
        }
    }
}
