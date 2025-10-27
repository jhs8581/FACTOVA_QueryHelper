﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace FACTOVA_QueryHelper
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
                            logEntry.AppendLine($"  🔔 알림: {newNotifications}개");
                            // 알림 내용 로그에 추가
                            for (int n = notificationsBefore; n < result.Notifications.Count; n++)
                            {
                                logEntry.AppendLine($"    - {result.Notifications[n].Replace($"[{queryItem.QueryName}] ", "")}");
                            }
                        }

                        logEntry.AppendLine($"  ✅ 성공");
                        
                        // 결과 탭 생성
                        _createResultTabCallback(queryItem, queryResult.Result, queryResult.Duration, null);
                        
                        result.SuccessCount++;
                    }
                    else
                    {
                        logEntry.AppendLine($"  ❌ 실패: {queryResult.ErrorMessage}");
                        
                        // 오류 탭 생성
                        _createResultTabCallback(queryItem, null, 0, queryResult.ErrorMessage);
                        
                        result.FailCount++;
                    }
                }
                catch (Exception ex)
                {
                    logEntry.AppendLine($"  ❌ 실패: {ex.Message}");
                    
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
            result.ExecutionLogs.Add("📊 작업 요약");
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

            try
            {
                // 직접 연결 정보가 있는지 확인
                if (!string.IsNullOrWhiteSpace(queryItem.Host) &&
                    !string.IsNullOrWhiteSpace(queryItem.Port) &&
                    !string.IsNullOrWhiteSpace(queryItem.ServiceName))
                {
                    connectionString = $"Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST={queryItem.Host})(PORT={queryItem.Port}))(CONNECT_DATA=(SERVICE_NAME={queryItem.ServiceName})));";
                    logEntry.AppendLine($"  연결: {queryItem.Host}:{queryItem.Port}/{queryItem.ServiceName}");
                }
                else
                {
                    // TNS 정보 찾기
                    var selectedTns = _tnsEntries.FirstOrDefault(t =>
                        t.Name.Equals(queryItem.TnsName, StringComparison.OrdinalIgnoreCase));

                    if (selectedTns == null)
                    {
                        var availableTns = string.Join(", ", _tnsEntries.Select(t => t.Name));
                        throw new Exception($"TNS '{queryItem.TnsName}'를 찾을 수 없습니다.\n\n" +
                            $"💡 해결 방법:\n" +
                            $"1. Excel A열에 정확한 TNS 이름 입력\n" +
                            $"2. 또는 Host:Port:ServiceName 형식으로 입력\n" +
                            $"   예) 192.168.1.10:1521:ORCL\n\n" +
                            $"사용 가능한 TNS 목록:\n{availableTns}\n\n" +
                            $"tnsnames.ora 파일 경로:\n{_settings.TnsPath}");
                    }

                    connectionString = selectedTns.ConnectionString;
                    logEntry.AppendLine($"  TNS: {queryItem.TnsName}");
                }

                // User ID와 Password 검증
                if (string.IsNullOrWhiteSpace(queryItem.UserId))
                    throw new Exception("User ID가 지정되지 않았습니다.");

                if (string.IsNullOrWhiteSpace(queryItem.Password))
                    throw new Exception("Password가 지정되지 않았습니다.");

                logEntry.AppendLine($"  사용자: {queryItem.UserId}");

                var startTime = DateTime.Now;

                // 쿼리 실행
                result.Result = await OracleDatabase.ExecuteQueryAsync(
                    connectionString,
                    queryItem.UserId,
                    queryItem.Password,
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
                    System.Diagnostics.Debug.WriteLine($"  ✅ 알림 추가: {msg}");
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
                    System.Diagnostics.Debug.WriteLine($"  ✅ 알림 추가: {msg}");
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
                    System.Diagnostics.Debug.WriteLine($"  ✅ 알림 추가: {msg}");
                }
            }
        }

        /// <summary>
        /// 컬럼 값을 체크하고 알림을 추가합니다.
        /// </summary>
        private void CheckColumnValuesAndNotify(QueryItem queryItem, DataTable result, List<string> notifications)
        {
            // H열이 'Y'가 아니면 알림을 추가하지 않음
            if (queryItem.NotifyFlag != "Y")
                return;

            // L열과 M열 체크
            if (string.IsNullOrWhiteSpace(queryItem.ColumnNames) ||
                string.IsNullOrWhiteSpace(queryItem.ColumnValues))
            {
                System.Diagnostics.Debug.WriteLine($"[{queryItem.QueryName}] L열/M열이 비어있음, 컬럼 체크 건너뜀");
                return;
            }

            System.Diagnostics.Debug.WriteLine($"[{queryItem.QueryName}] 컬럼 값 체크 시작");
            System.Diagnostics.Debug.WriteLine($"  - L열(컬럼명): {queryItem.ColumnNames}");
            System.Diagnostics.Debug.WriteLine($"  - M열(값): {queryItem.ColumnValues}");

            var columnNames = queryItem.ColumnNames.Split(',').Select(c => c.Trim()).ToList();
            var columnValues = queryItem.ColumnValues.Split(',').Select(v => v.Trim()).ToList();

            if (columnNames.Count != columnValues.Count)
            {
                System.Diagnostics.Debug.WriteLine($"  ⚠️ 컬럼명 개수({columnNames.Count})와 값 개수({columnValues.Count})가 다름");
                return;
            }

            System.Diagnostics.Debug.WriteLine($"  - 총 {result.Rows.Count}개 행 검사");

            for (int i = 0; i < result.Rows.Count; i++)
            {
                var row = result.Rows[i];
                bool allMatch = true;

                for (int j = 0; j < columnNames.Count; j++)
                {
                    string columnName = columnNames[j];
                    string expectedValue = columnValues[j];

                    if (!result.Columns.Contains(columnName))
                    {
                        System.Diagnostics.Debug.WriteLine($"    행{i + 1}: 컬럼 '{columnName}' 없음");
                        allMatch = false;
                        break;
                    }

                    var actualValue = row[columnName]?.ToString()?.Trim() ?? "";
                    if (actualValue != expectedValue)
                    {
                        allMatch = false;
                        break;
                    }
                }

                if (allMatch)
                {
                    var matchInfo = string.Join(", ", columnNames.Zip(columnValues, (n, v) => $"{n}={v}"));
                    var msg = $"[{queryItem.QueryName}] 조건 일치 발견 (행 {i + 1}): {matchInfo}";
                    notifications.Add(msg);
                    System.Diagnostics.Debug.WriteLine($"  ✅ 알림 추가: {msg}");
                }
            }

            System.Diagnostics.Debug.WriteLine($"  - 컬럼 값 체크 완료");
        }
    }
}
