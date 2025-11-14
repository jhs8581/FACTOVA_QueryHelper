using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using FACTOVA_QueryHelper.Models;

namespace FACTOVA_QueryHelper.Database
{
    /// <summary>
    /// 쿼리 실행 및 결과 관리를 담당하는 클래스
    /// </summary>
    public class QueryExecutionManager
    {
        private readonly Action<string, Color> _updateStatusCallback;
        private readonly TabControl _resultTabControl;
        private readonly Action<QueryItem, DataTable?, double, string?> _createResultTabCallback;
        private List<TnsEntry> _tnsEntries;
        private AppSettings _settings;

        public QueryExecutionManager(
            Action<string, Color> updateStatusCallback,
            TabControl resultTabControl,
            List<TnsEntry> tnsEntries,
            AppSettings settings,
            Action<QueryItem, DataTable?, double, string?> createResultTabCallback)
        {
            _updateStatusCallback = updateStatusCallback ?? throw new ArgumentNullException(nameof(updateStatusCallback));
            _resultTabControl = resultTabControl ?? throw new ArgumentNullException(nameof(resultTabControl));
            _tnsEntries = tnsEntries ?? throw new ArgumentNullException(nameof(tnsEntries));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _createResultTabCallback = createResultTabCallback ?? throw new ArgumentNullException(nameof(createResultTabCallback));
        }

        /// <summary>
        /// TNS 엔트리 목록을 업데이트합니다.
        /// </summary>
        public void UpdateTnsEntries(List<TnsEntry> tnsEntries)
        {
            _tnsEntries = tnsEntries ?? throw new ArgumentNullException(nameof(tnsEntries));
        }

        /// <summary>
        /// 설정을 업데이트합니다.
        /// </summary>
        public void UpdateSettings(AppSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        /// <summary>
        /// 쿼리 실행 결과
        /// </summary>
        public class ExecutionResult
        {
            public int SuccessCount { get; set; }
            public int FailCount { get; set; }
            public List<string> Notifications { get; set; } = new List<string>();
            public List<string> NotifiedQueryNames { get; set; } = new List<string>();
            public List<string> ExecutionLogs { get; set; } = new List<string>();
            public DateTime StartTime { get; set; }
            public double TotalDuration { get; set; }
        }

        /// <summary>
        /// 여러 쿼리를 실행합니다.
        /// </summary>
        public async Task<ExecutionResult> ExecuteQueriesAsync(List<QueryItem> queries)
        {
            if (queries == null || queries.Count == 0)
            {
                throw new ArgumentException("실행할 쿼리가 없습니다.", nameof(queries));
            }

            var result = new ExecutionResult
            {
                StartTime = DateTime.Now
            };

            // 기존 결과 탭 초기화
            _resultTabControl.Items.Clear();

            // 작업 로그 헤더 추가
            result.ExecutionLogs.Add($"작업 시작 시간: {result.StartTime:yyyy-MM-dd HH:mm:ss}");
            result.ExecutionLogs.Add($"선택된 쿼리 수: {queries.Count}개");
            result.ExecutionLogs.Add(new string('=', 80));
            result.ExecutionLogs.Add("");

            // 전달받은 쿼리를 그대로 실행 (필터링은 MainWindow에서 처리됨)
            var queriesToExecute = queries;

            result.ExecutionLogs.Add($"실행 대상 쿼리: {queriesToExecute.Count}개");
            result.ExecutionLogs.Add("");

            for (int i = 0; i < queriesToExecute.Count; i++)
            {
                var queryItem = queriesToExecute[i];

                _updateStatusCallback(
                    $"쿼리 실행 중... ({i + 1}/{queriesToExecute.Count}) - {queryItem.QueryName}",
                    Colors.Blue);

                var logEntry = new StringBuilder();
                logEntry.AppendLine($"[{i + 1}/{queriesToExecute.Count}] {queryItem.QueryName}");
                logEntry.AppendLine($"  시작 시간: {DateTime.Now:HH:mm:ss}");

                try
                {
                    var queryResult = await ExecuteSingleQueryAsync(queryItem, logEntry);

                    if (queryResult.IsSuccess)
                    {
                        // 알림 추가 전 현재 알림 개수 저장
                        int notificationsBefore = result.Notifications.Count;

                        // 결과 건수 체크 및 알림
                        CheckResultCountAndNotify(queryItem, queryResult.Result!.Rows.Count, result.Notifications);

                        // 특정 컬럼 값 체크 및 알림
                        CheckColumnValuesAndNotify(queryItem, queryResult.Result!, result.Notifications);

                        // 이번 쿼리에서 추가된 알림 개수 계산
                        int newNotifications = result.Notifications.Count - notificationsBefore;
                        
                        if (newNotifications > 0)
                        {
                            logEntry.AppendLine($"  [알림] 알림: {newNotifications}개");
                            // 알림 내용 로그에 추가
                            for (int n = notificationsBefore; n < result.Notifications.Count; n++)
                            {
                                logEntry.AppendLine($"    - {result.Notifications[n].Replace($"[{queryItem.QueryName}] ", "")}");
                            }
                            
                            // 알림이 발생한 쿼리 이름 추가
                            if (!result.NotifiedQueryNames.Contains(queryItem.QueryName))
                            {
                                result.NotifiedQueryNames.Add(queryItem.QueryName);
                            }
                        }

                        logEntry.AppendLine($"  [성공] 성공");
                        
                        // 결과 탭 생성
                        _createResultTabCallback(queryItem, queryResult.Result, queryResult.Duration, null);
                        
                        result.SuccessCount++;
                    }
                    else
                    {
                        logEntry.AppendLine($"  [실패] 실패: {queryResult.ErrorMessage}");
                        
                        // 오류 탭 생성
                        _createResultTabCallback(queryItem, null, 0, queryResult.ErrorMessage);
                        
                        result.FailCount++;
                    }
                }
                catch (Exception ex)
                {
                    logEntry.AppendLine($"  [실패] 실패: {ex.Message}");
                    
                    // 오류 탭 생성
                    _createResultTabCallback(queryItem, null, 0, ex.Message);
                    
                    result.FailCount++;
                }

                result.ExecutionLogs.Add(logEntry.ToString());
            }

            result.TotalDuration = (DateTime.Now - result.StartTime).TotalSeconds;

            // 작업 요약 추가
            result.ExecutionLogs.Add(new string('=', 80));
            result.ExecutionLogs.Add("");
            result.ExecutionLogs.Add("[작업 요약]");
            result.ExecutionLogs.Add($"  총 실행 시간: {result.TotalDuration:F2}초");
            result.ExecutionLogs.Add($"  성공: {result.SuccessCount}개");
            result.ExecutionLogs.Add($"  실패: {result.FailCount}개");
            result.ExecutionLogs.Add($"  알림: {result.Notifications.Count}개");
            result.ExecutionLogs.Add("");
            result.ExecutionLogs.Add($"작업 완료 시간: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

            return result;
        }

        /// <summary>
        /// 단일 쿼리 실행 결과
        /// </summary>
        private class SingleQueryResult
        {
            public bool IsSuccess { get; set; }
            public DataTable? Result { get; set; }
            public double Duration { get; set; }
            public string? ErrorMessage { get; set; }
        }

        /// <summary>
        /// 단일 쿼리를 실행합니다.
        /// </summary>
        private async Task<SingleQueryResult> ExecuteSingleQueryAsync(QueryItem queryItem, StringBuilder logEntry)
        {
            var result = new SingleQueryResult();
            string connectionString;
            string userId;
            string password;

            try
            {
                // 🔥 1순위: ConnectionInfoId가 있으면 접속 정보에서 조회
                if (queryItem.ConnectionInfoId.HasValue && queryItem.ConnectionInfoId.Value > 0)
                {
                    var connectionService = new Services.ConnectionInfoService(_settings.DatabasePath);
                    var connectionInfo = connectionService.GetAllConnections()
                        .FirstOrDefault(c => c.Id == queryItem.ConnectionInfoId.Value);

                    if (connectionInfo == null)
                    {
                        throw new Exception($"접속 정보 ID {queryItem.ConnectionInfoId.Value}를 찾을 수 없습니다.\n" +
                            $"접속 정보 관리에서 해당 정보가 삭제되었을 수 있습니다.");
                    }

                    // 접속 정보에서 연결 문자열 생성
                    if (!string.IsNullOrWhiteSpace(connectionInfo.TNS))
                    {
                        var selectedTns = _tnsEntries.FirstOrDefault(t =>
                            t.Name.Equals(connectionInfo.TNS, StringComparison.OrdinalIgnoreCase));

                        if (selectedTns == null)
                        {
                            throw new Exception($"TNS '{connectionInfo.TNS}'를 찾을 수 없습니다.");
                        }

                        connectionString = selectedTns.GetConnectionString();
                        logEntry.AppendLine($"  접속 정보: {connectionInfo.DisplayName} (TNS: {connectionInfo.TNS})");
                    }
                    else if (!string.IsNullOrWhiteSpace(connectionInfo.Host))
                    {
                        connectionString = $"Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST={connectionInfo.Host})(PORT={connectionInfo.Port}))(CONNECT_DATA=(SERVICE_NAME={connectionInfo.Service})));";
                        logEntry.AppendLine($"  접속 정보: {connectionInfo.DisplayName} ({connectionInfo.Host}:{connectionInfo.Port}/{connectionInfo.Service})");
                    }
                    else
                    {
                        throw new Exception($"접속 정보 '{connectionInfo.DisplayName}'에 TNS 또는 Host 정보가 없습니다.");
                    }

                    userId = connectionInfo.UserId;
                    password = connectionInfo.Password;
                }
                // 🔥 2순위: 직접 연결 정보 (Host/Port/ServiceName)
                else if (!string.IsNullOrWhiteSpace(queryItem.Host) &&
                    !string.IsNullOrWhiteSpace(queryItem.Port) &&
                    !string.IsNullOrWhiteSpace(queryItem.ServiceName))
                {
                    connectionString = $"Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST={queryItem.Host})(PORT={queryItem.Port}))(CONNECT_DATA=(SERVICE_NAME={queryItem.ServiceName})));";
                    logEntry.AppendLine($"  연결: {queryItem.Host}:{queryItem.Port}/{queryItem.ServiceName}");
                    
                    userId = queryItem.UserId;
                    password = queryItem.Password;
                }
                // 🔥 3순위: TNS 이름으로 연결
                else
                {
                    var selectedTns = _tnsEntries.FirstOrDefault(t =>
                        t.Name.Equals(queryItem.TnsName, StringComparison.OrdinalIgnoreCase));

                    if (selectedTns == null)
                    {
                        var availableTns = string.Join(", ", _tnsEntries.Select(t => t.Name));
                        throw new Exception($"TNS '{queryItem.TnsName}'를 찾을 수 없습니다.\n\n" +
                            $"[해결 방법]\n" +
                            $"1. 쿼리 관리에서 접속 정보를 선택하세요\n" +
                            $"2. 또는 TNS 이름을 정확히 입력하세요\n" +
                            $"3. 또는 Host:Port:ServiceName 형식으로 입력하세요\n" +
                            $"   예) 192.168.1.10:1521:ORCL\n\n" +
                            $"사용 가능한 TNS 목록:\n{availableTns}\n\n" +
                            $"tnsnames.ora 파일 경로:\n{_settings.TnsPath}");
                    }

                    connectionString = selectedTns.GetConnectionString();
                    logEntry.AppendLine($"  TNS: {queryItem.TnsName}");
                    
                    userId = queryItem.UserId;
                    password = queryItem.Password;
                }

                // User ID와 Password 검증
                if (string.IsNullOrWhiteSpace(userId))
                    throw new Exception("User ID가 지정되지 않았습니다.");

                if (string.IsNullOrWhiteSpace(password))
                    throw new Exception("Password가 지정되지 않았습니다.");

                logEntry.AppendLine($"  사용자: {userId}");

                var startTime = DateTime.Now;

                // 쿼리 실행
                result.Result = await OracleDatabase.ExecuteQueryAsync(
                    connectionString,
                    userId,
                    password,
                    queryItem.Query);

                var endTime = DateTime.Now;
                result.Duration = (endTime - startTime).TotalSeconds;

                logEntry.AppendLine($"  완료 시간: {endTime:HH:mm:ss}");
                logEntry.AppendLine($"  소요 시간: {result.Duration:F2}초");
                logEntry.AppendLine($"  결과: {result.Result.Rows.Count}행 × {result.Result.Columns.Count}열");

                result.IsSuccess = true;
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.ErrorMessage = ex.Message;
            }

            return result;
        }

        /// <summary>
        /// 결과 건수를 체크하고 알림을 추가합니다.
        /// </summary>
        private void CheckResultCountAndNotify(QueryItem queryItem, int rowCount, List<string> notifications)
        {
            // H열이 'Y'가 아니면 알림을 추가하지 않음
            if (queryItem.NotifyFlag != "Y")
            {
                System.Diagnostics.Debug.WriteLine($"[{queryItem.QueryName}] NotifyFlag != 'Y', 알림 건너뜀 (NotifyFlag={queryItem.NotifyFlag})");
                return;
            }

            System.Diagnostics.Debug.WriteLine($"[{queryItem.QueryName}] 알림 체크 시작 - 결과 건수: {rowCount}건");

            // I열: 이상일 때
            if (!string.IsNullOrWhiteSpace(queryItem.CountGreaterThan) &&
                int.TryParse(queryItem.CountGreaterThan, out int greaterThan))
            {
                System.Diagnostics.Debug.WriteLine($"  - I열(이상) 체크: {rowCount} >= {greaterThan} ?");
                if (rowCount >= greaterThan)
                {
                    var msg = $"[{queryItem.QueryName}] 조회 결과 {rowCount}건 (기준: {greaterThan}건 이상)";
                    notifications.Add(msg);
                    System.Diagnostics.Debug.WriteLine($"  [알림 추가] 알림 추가: {msg}");
                }
            }

            // J열: 같을 때
            if (!string.IsNullOrWhiteSpace(queryItem.CountEquals) &&
                int.TryParse(queryItem.CountEquals, out int equals))
            {
                System.Diagnostics.Debug.WriteLine($"  - J열(같음) 체크: {rowCount} == {equals} ?");
                if (rowCount == equals)
                {
                    var msg = $"[{queryItem.QueryName}] 조회 결과 {rowCount}건 (기준: {equals}건과 같음)";
                    notifications.Add(msg);
                    System.Diagnostics.Debug.WriteLine($"  [알림 추가] 알림 추가: {msg}");
                }
            }

            // K열: 이하일 때
            if (!string.IsNullOrWhiteSpace(queryItem.CountLessThan) &&
                int.TryParse(queryItem.CountLessThan, out int lessThan))
            {
                System.Diagnostics.Debug.WriteLine($"  - K열(이하) 체크: {rowCount} <= {lessThan} ?");
                if (rowCount <= lessThan)
                {
                    var msg = $"[{queryItem.QueryName}] 조회 결과 {rowCount}건 (기준: {lessThan}건 이하)";
                    notifications.Add(msg);
                    System.Diagnostics.Debug.WriteLine($"  [알림 추가] 알림 추가: {msg}");
                }
            }
        }

        /// <summary>
        /// 컬럼 값을 체크하고 알림을 추가합니다.
        /// </summary>
        private void CheckColumnValuesAndNotify(QueryItem queryItem, DataTable result, List<string> notifications)
        {
            System.Diagnostics.Debug.WriteLine($"[{queryItem.QueryName}] CheckColumnValuesAndNotify 시작");
            System.Diagnostics.Debug.WriteLine($"  - NotifyFlag: '{queryItem.NotifyFlag}'");
            System.Diagnostics.Debug.WriteLine($"  - ColumnNames: '{queryItem.ColumnNames}'");
            System.Diagnostics.Debug.WriteLine($"  - ColumnValues: '{queryItem.ColumnValues}'");

            // H열이 'Y'가 아니면 알림을 추가하지 않음
            if (queryItem.NotifyFlag != "Y")
            {
                System.Diagnostics.Debug.WriteLine($"[{queryItem.QueryName}] NotifyFlag != 'Y', 컬럼 체크 건너뜀");
                return;
            }

            // L열과 M열 체크
            if (string.IsNullOrWhiteSpace(queryItem.ColumnNames) ||
                string.IsNullOrWhiteSpace(queryItem.ColumnValues))
            {
                System.Diagnostics.Debug.WriteLine($"[{queryItem.QueryName}] L열/M열이 비어있음, 컬럼 체크 건너뜀");
                System.Diagnostics.Debug.WriteLine($"  - ColumnNames IsNullOrWhiteSpace: {string.IsNullOrWhiteSpace(queryItem.ColumnNames)}");
                System.Diagnostics.Debug.WriteLine($"  - ColumnValues IsNullOrWhiteSpace: {string.IsNullOrWhiteSpace(queryItem.ColumnValues)}");
                return;
            }

            System.Diagnostics.Debug.WriteLine($"[{queryItem.QueryName}] 컬럼 값 체크 시작");
            System.Diagnostics.Debug.WriteLine($"  - L열(컬럼명): '{queryItem.ColumnNames}'");
            System.Diagnostics.Debug.WriteLine($"  - M열(값): '{queryItem.ColumnValues}'");

            var columnNames = queryItem.ColumnNames.Split(',').Select(c => c.Trim()).ToList();
            var columnValues = queryItem.ColumnValues.Split(',').Select(v => v.Trim()).ToList();

            System.Diagnostics.Debug.WriteLine($"  - 파싱된 컬럼명 개수: {columnNames.Count}, 값: [{string.Join(", ", columnNames)}]");
            System.Diagnostics.Debug.WriteLine($"  - 파싱된 값 개수: {columnValues.Count}, 값: [{string.Join(", ", columnValues)}]");

            if (columnNames.Count != columnValues.Count)
            {
                System.Diagnostics.Debug.WriteLine($"  [경고] 컬럼명 개수({columnNames.Count})와 값 개수({columnValues.Count})가 다름");
                return;
            }

            System.Diagnostics.Debug.WriteLine($"  - 총 {result.Rows.Count}개 행 검사");
            System.Diagnostics.Debug.WriteLine($"  - 결과 테이블 컬럼 목록: [{string.Join(", ", result.Columns.Cast<DataColumn>().Select(c => c.ColumnName))}]");

            int mismatchCount = 0;
            for (int i = 0; i < result.Rows.Count; i++)
            {
                var row = result.Rows[i];
                bool anyMismatch = false;

                System.Diagnostics.Debug.WriteLine($"    행 {i + 1} 검사 시작:");

                // OR 조건으로 변경: 하나라도 불일치하면 알림
                for (int j = 0; j < columnNames.Count; j++)
                {
                    string columnName = columnNames[j];
                    string expectedValue = columnValues[j];

                    if (!result.Columns.Contains(columnName))
                    {
                        System.Diagnostics.Debug.WriteLine($"      컬럼 '{columnName}' 없음");
                        continue;
                    }

                    var actualValue = row[columnName]?.ToString()?.Trim() ?? "";
                    bool isMatch = actualValue == expectedValue;
                    System.Diagnostics.Debug.WriteLine($"      컬럼 '{columnName}': 실제값='{actualValue}', 기대값='{expectedValue}', 일치={isMatch}");
                    
                    // OR 조건: 하나라도 불일치하면 anyMismatch = true
                    if (!isMatch)
                    {
                        anyMismatch = true;
                        System.Diagnostics.Debug.WriteLine($"      [불일치] 불일치 발견: {columnName}");
                    }
                }

                if (anyMismatch)
                {
                    mismatchCount++;
                    var checkInfo = string.Join(", ", columnNames.Zip(columnValues, (n, v) => $"{n}={v}"));
                    System.Diagnostics.Debug.WriteLine($"    [불일치] 행 {i + 1}: 조건 불일치 발견 - 기대값: {checkInfo}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"    [일치] 행 {i + 1}: 모든 조건 일치 (알림 없음)");
                }
            }

            // 조건에 불일치하는 행이 하나라도 있으면 알림 추가
            if (mismatchCount > 0)
            {
                var checkInfo = string.Join(", ", columnNames.Zip(columnValues, (n, v) => $"{n}={v}"));
                var msg = $"[{queryItem.QueryName}] 조건 불일치 발견: {mismatchCount}개 행 (기대값: {checkInfo})";
                notifications.Add(msg);
                System.Diagnostics.Debug.WriteLine($"  [알림 추가] 알림 추가: {msg}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"  [정보] 모든 행이 조건과 일치 - 알림 없음");
            }

            System.Diagnostics.Debug.WriteLine($"  - 컬럼 값 체크 완료, 총 불일치 행: {mismatchCount}개");
        }
    }
}
