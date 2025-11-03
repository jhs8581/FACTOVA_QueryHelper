using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace FACTOVA_QueryHelper.Database
{
    /// <summary>
    /// 占쏙옙占쏙옙 占쏙옙占쏙옙 占쏙옙 占쏙옙占?占쏙옙占쏙옙占쏙옙 占쏙옙占쏙옙求占?클占쏙옙占쏙옙
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
        /// TNS 占쏙옙트占쏙옙 占쏙옙占쏙옙占?占쏙옙占쏙옙占쏙옙트占쌌니댐옙.
        /// </summary>
        public void UpdateTnsEntries(List<TnsEntry> tnsEntries)
        {
            _tnsEntries = tnsEntries ?? throw new ArgumentNullException(nameof(tnsEntries));
        }

        /// <summary>
        /// 占쏙옙占쏙옙占쏙옙 占쏙옙占쏙옙占쏙옙트占쌌니댐옙.
        /// </summary>
        public void UpdateSettings(AppSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        /// <summary>
        /// 占쏙옙占쏙옙 占쏙옙占쏙옙 占쏙옙占?
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
        /// 占쏙옙占쏙옙 占쏙옙占쏙옙占쏙옙 占쏙옙占쏙옙占쌌니댐옙.
        /// </summary>
        public async Task<ExecutionResult> ExecuteQueriesAsync(List<QueryItem> queries)
        {
            if (queries == null || queries.Count == 0)
            {
                throw new ArgumentException("占쏙옙占쏙옙占쏙옙 占쏙옙占쏙옙占쏙옙 占쏙옙占쏙옙占싹댐옙.", nameof(queries));
            }

            var result = new ExecutionResult
            {
                StartTime = DateTime.Now
            };

            // 占쏙옙占쏙옙 占쏙옙占?占쏙옙 占십깍옙화
            _resultTabControl.Items.Clear();

            // 占쌜억옙 占싸깍옙 占쏙옙占?占쌩곤옙
            result.ExecutionLogs.Add($"占쌜억옙 占쏙옙占쏙옙 占시곤옙: {result.StartTime:yyyy-MM-dd HH:mm:ss}");
            result.ExecutionLogs.Add($"占쏙옙占시듸옙 占쏙옙占쏙옙 占쏙옙: {queries.Count}占쏙옙");
            result.ExecutionLogs.Add(new string('=', 80));
            result.ExecutionLogs.Add("");

            // 占쏙옙占쌨뱄옙占쏙옙 占쏙옙占쏙옙占쏙옙 占쌓댐옙占?占쏙옙占쏙옙 (占쏙옙占싶몌옙占쏙옙 MainWindow占쏙옙占쏙옙 처占쏙옙占쏙옙)
            var queriesToExecute = queries;

            result.ExecutionLogs.Add($"占쏙옙占쏙옙 占쏙옙占?占쏙옙占쏙옙: {queriesToExecute.Count}占쏙옙");
            result.ExecutionLogs.Add("");

            for (int i = 0; i < queriesToExecute.Count; i++)
            {
                var queryItem = queriesToExecute[i];

                _updateStatusCallback(
                    $"占쏙옙占쏙옙 占쏙옙占쏙옙 占쏙옙... ({i + 1}/{queriesToExecute.Count}) - {queryItem.QueryName}",
                    Colors.Blue);

                var logEntry = new StringBuilder();
                logEntry.AppendLine($"[{i + 1}/{queriesToExecute.Count}] {queryItem.QueryName}");
                logEntry.AppendLine($"  占쏙옙占쏙옙 占시곤옙: {DateTime.Now:HH:mm:ss}");

                try
                {
                    var queryResult = await ExecuteSingleQueryAsync(queryItem, logEntry);

                    if (queryResult.IsSuccess)
                    {
                        // 占싯몌옙 占쌩곤옙 占쏙옙 占쏙옙占쏙옙 占싯몌옙 占쏙옙占쏙옙 占쏙옙占쏙옙
                        int notificationsBefore = result.Notifications.Count;

                        // 占쏙옙占?占실쇽옙 체크 占쏙옙 占싯몌옙
                        CheckResultCountAndNotify(queryItem, queryResult.Result!.Rows.Count, result.Notifications);

                        // 특占쏙옙 占시뤄옙 占쏙옙 체크 占쏙옙 占싯몌옙
                        CheckColumnValuesAndNotify(queryItem, queryResult.Result!, result.Notifications);

                        // 占싱뱄옙 占쏙옙占쏙옙占쏙옙占쏙옙 占쌩곤옙占쏙옙 占싯몌옙 占쏙옙占쏙옙 占쏙옙占?
                        int newNotifications = result.Notifications.Count - notificationsBefore;
                        
                        if (newNotifications > 0)
                        {
                            logEntry.AppendLine($"  [占싯몌옙] 占싯몌옙: {newNotifications}占쏙옙");
                            // 占싯몌옙 占쏙옙占쏙옙 占싸그울옙 占쌩곤옙
                            for (int n = notificationsBefore; n < result.Notifications.Count; n++)
                            {
                                logEntry.AppendLine($"    - {result.Notifications[n].Replace($"[{queryItem.QueryName}] ", "")}");
                            }
                            
                            // 占싯몌옙占쏙옙 占쌩삼옙占쏙옙 占쏙옙占쏙옙 占싱몌옙 占쌩곤옙
                            if (!result.NotifiedQueryNames.Contains(queryItem.QueryName))
                            {
                                result.NotifiedQueryNames.Add(queryItem.QueryName);
                            }
                        }

                        logEntry.AppendLine($"  [占쏙옙占쏙옙] 占쏙옙占쏙옙");
                        
                        // 占쏙옙占?占쏙옙 占쏙옙占쏙옙
                        _createResultTabCallback(queryItem, queryResult.Result, queryResult.Duration, null);
                        
                        result.SuccessCount++;
                    }
                    else
                    {
                        logEntry.AppendLine($"  [占쏙옙占쏙옙] 占쏙옙占쏙옙: {queryResult.ErrorMessage}");
                        
                        // 占쏙옙占쏙옙 占쏙옙 占쏙옙占쏙옙
                        _createResultTabCallback(queryItem, null, 0, queryResult.ErrorMessage);
                        
                        result.FailCount++;
                    }
                }
                catch (Exception ex)
                {
                    logEntry.AppendLine($"  [占쏙옙占쏙옙] 占쏙옙占쏙옙: {ex.Message}");
                    
                    // 占쏙옙占쏙옙 占쏙옙 占쏙옙占쏙옙
                    _createResultTabCallback(queryItem, null, 0, ex.Message);
                    
                    result.FailCount++;
                }

                result.ExecutionLogs.Add(logEntry.ToString());
            }

            result.TotalDuration = (DateTime.Now - result.StartTime).TotalSeconds;

            // 占쌜억옙 占쏙옙占?占쌩곤옙
            result.ExecutionLogs.Add(new string('=', 80));
            result.ExecutionLogs.Add("");
            result.ExecutionLogs.Add("[占쌜억옙 占쏙옙占?");
            result.ExecutionLogs.Add($"  占쏙옙 占쏙옙占쏙옙 占시곤옙: {result.TotalDuration:F2}占쏙옙");
            result.ExecutionLogs.Add($"  占쏙옙占쏙옙: {result.SuccessCount}占쏙옙");
            result.ExecutionLogs.Add($"  占쏙옙占쏙옙: {result.FailCount}占쏙옙");
            result.ExecutionLogs.Add($"  占싯몌옙: {result.Notifications.Count}占쏙옙");
            result.ExecutionLogs.Add("");
            result.ExecutionLogs.Add($"占쌜억옙 占싹뤄옙 占시곤옙: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

            return result;
        }

        /// <summary>
        /// 占쏙옙占쏙옙 占쏙옙占쏙옙 占쏙옙占쏙옙 占쏙옙占?
        /// </summary>
        private class SingleQueryResult
        {
            public bool IsSuccess { get; set; }
            public DataTable? Result { get; set; }
            public double Duration { get; set; }
            public string? ErrorMessage { get; set; }
        }

        /// <summary>
        /// 占쏙옙占쏙옙 占쏙옙占쏙옙占쏙옙 占쏙옙占쏙옙占쌌니댐옙.
        /// </summary>
        private async Task<SingleQueryResult> ExecuteSingleQueryAsync(QueryItem queryItem, StringBuilder logEntry)
        {
            var result = new SingleQueryResult();
            string connectionString;

            try
            {
                // 占쏙옙占쏙옙 占쏙옙占쏙옙 占쏙옙占쏙옙占쏙옙 占쌍댐옙占쏙옙 확占쏙옙
                if (!string.IsNullOrWhiteSpace(queryItem.Host) &&
                    !string.IsNullOrWhiteSpace(queryItem.Port) &&
                    !string.IsNullOrWhiteSpace(queryItem.ServiceName))
                {
                    connectionString = $"Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST={queryItem.Host})(PORT={queryItem.Port}))(CONNECT_DATA=(SERVICE_NAME={queryItem.ServiceName})));";
                    logEntry.AppendLine($"  占쏙옙占쏙옙: {queryItem.Host}:{queryItem.Port}/{queryItem.ServiceName}");
                }
                else
                {
                    // TNS 占쏙옙占쏙옙 찾占쏙옙
                    var selectedTns = _tnsEntries.FirstOrDefault(t =>
                        t.Name.Equals(queryItem.TnsName, StringComparison.OrdinalIgnoreCase));

                    if (selectedTns == null)
                    {
                        var availableTns = string.Join(", ", _tnsEntries.Select(t => t.Name));
                        throw new Exception($"TNS '{queryItem.TnsName}'占쏙옙 찾占쏙옙 占쏙옙 占쏙옙占쏙옙占싹댐옙.\n\n" +
                            $"[占쌔곤옙 占쏙옙占?\n" +
                            $"1. Excel A占쏙옙占쏙옙 占쏙옙확占쏙옙 TNS 占싱몌옙 占쌉뤄옙\n" +
                            $"2. 占실댐옙 Host:Port:ServiceName 占쏙옙占쏙옙占쏙옙占쏙옙 占쌉뤄옙\n" +
                            $"   占쏙옙) 192.168.1.10:1521:ORCL\n\n" +
                            $"占쏙옙占?占쏙옙占쏙옙占쏙옙 TNS 占쏙옙占?\n{availableTns}\n\n" +
                            $"tnsnames.ora 占쏙옙占쏙옙 占쏙옙占?\n{_settings.TnsPath}");
                    }

                    connectionString = selectedTns.ConnectionString;
                    logEntry.AppendLine($"  TNS: {queryItem.TnsName}");
                }

                // User ID占쏙옙 Password 占쏙옙占쏙옙
                if (string.IsNullOrWhiteSpace(queryItem.UserId))
                    throw new Exception("User ID占쏙옙 占쏙옙占쏙옙占쏙옙占쏙옙 占십았쏙옙占싹댐옙.");

                if (string.IsNullOrWhiteSpace(queryItem.Password))
                    throw new Exception("Password占쏙옙 占쏙옙占쏙옙占쏙옙占쏙옙 占십았쏙옙占싹댐옙.");

                logEntry.AppendLine($"  占쏙옙占쏙옙占? {queryItem.UserId}");

                var startTime = DateTime.Now;

                // 占쏙옙占쏙옙 占쏙옙占쏙옙
                result.Result = await OracleDatabase.ExecuteQueryAsync(
                    connectionString,
                    queryItem.UserId,
                    queryItem.Password,
                    queryItem.Query);

                var endTime = DateTime.Now;
                result.Duration = (endTime - startTime).TotalSeconds;

                logEntry.AppendLine($"  占싹뤄옙 占시곤옙: {endTime:HH:mm:ss}");
                logEntry.AppendLine($"  占쌀울옙 占시곤옙: {result.Duration:F2}占쏙옙");
                logEntry.AppendLine($"  占쏙옙占? {result.Result.Rows.Count}占쏙옙 占쏙옙 {result.Result.Columns.Count}占쏙옙");

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
        /// 占쏙옙占?占실쇽옙占쏙옙 체크占싹곤옙 占싯몌옙占쏙옙 占쌩곤옙占쌌니댐옙.
        /// </summary>
        private void CheckResultCountAndNotify(QueryItem queryItem, int rowCount, List<string> notifications)
        {
            // H占쏙옙占쏙옙 'Y'占쏙옙 占싣니몌옙 占싯몌옙占쏙옙 占쌩곤옙占쏙옙占쏙옙 占쏙옙占쏙옙
            if (queryItem.NotifyFlag != "Y")
            {
                System.Diagnostics.Debug.WriteLine($"[{queryItem.QueryName}] NotifyFlag != 'Y', 占싯몌옙 占실너띰옙 (NotifyFlag={queryItem.NotifyFlag})");
                return;
            }

            System.Diagnostics.Debug.WriteLine($"[{queryItem.QueryName}] 占싯몌옙 체크 占쏙옙占쏙옙 - 占쏙옙占?占실쇽옙: {rowCount}占쏙옙");

            // I占쏙옙: 占싱삼옙占쏙옙 占쏙옙
            if (!string.IsNullOrWhiteSpace(queryItem.CountGreaterThan) &&
                int.TryParse(queryItem.CountGreaterThan, out int greaterThan))
            {
                System.Diagnostics.Debug.WriteLine($"  - I占쏙옙(占싱삼옙) 체크: {rowCount} >= {greaterThan} ?");
                if (rowCount >= greaterThan)
                {
                    var msg = $"[{queryItem.QueryName}] 占쏙옙회 占쏙옙占?{rowCount}占쏙옙 (占쏙옙占쏙옙: {greaterThan}占쏙옙 占싱삼옙)";
                    notifications.Add(msg);
                    System.Diagnostics.Debug.WriteLine($"  [占싯몌옙 占쌩곤옙] 占싯몌옙 占쌩곤옙: {msg}");
                }
            }

            // J占쏙옙: 占쏙옙占쏙옙 占쏙옙
            if (!string.IsNullOrWhiteSpace(queryItem.CountEquals) &&
                int.TryParse(queryItem.CountEquals, out int equals))
            {
                System.Diagnostics.Debug.WriteLine($"  - J占쏙옙(占쏙옙占쏙옙) 체크: {rowCount} == {equals} ?");
                if (rowCount == equals)
                {
                    var msg = $"[{queryItem.QueryName}] 占쏙옙회 占쏙옙占?{rowCount}占쏙옙 (占쏙옙占쏙옙: {equals}占실곤옙 占쏙옙占쏙옙)";
                    notifications.Add(msg);
                    System.Diagnostics.Debug.WriteLine($"  [占싯몌옙 占쌩곤옙] 占싯몌옙 占쌩곤옙: {msg}");
                }
            }

            // K占쏙옙: 占쏙옙占쏙옙占쏙옙 占쏙옙
            if (!string.IsNullOrWhiteSpace(queryItem.CountLessThan) &&
                int.TryParse(queryItem.CountLessThan, out int lessThan))
            {
                System.Diagnostics.Debug.WriteLine($"  - K占쏙옙(占쏙옙占쏙옙) 체크: {rowCount} <= {lessThan} ?");
                if (rowCount <= lessThan)
                {
                    var msg = $"[{queryItem.QueryName}] 占쏙옙회 占쏙옙占?{rowCount}占쏙옙 (占쏙옙占쏙옙: {lessThan}占쏙옙 占쏙옙占쏙옙)";
                    notifications.Add(msg);
                    System.Diagnostics.Debug.WriteLine($"  [占싯몌옙 占쌩곤옙] 占싯몌옙 占쌩곤옙: {msg}");
                }
            }
        }

        /// <summary>
        /// 占시뤄옙 占쏙옙占쏙옙 체크占싹곤옙 占싯몌옙占쏙옙 占쌩곤옙占쌌니댐옙.
        /// </summary>
        private void CheckColumnValuesAndNotify(QueryItem queryItem, DataTable result, List<string> notifications)
        {
            System.Diagnostics.Debug.WriteLine($"[{queryItem.QueryName}] CheckColumnValuesAndNotify 占쏙옙占쏙옙");
            System.Diagnostics.Debug.WriteLine($"  - NotifyFlag: '{queryItem.NotifyFlag}'");
            System.Diagnostics.Debug.WriteLine($"  - ColumnNames: '{queryItem.ColumnNames}'");
            System.Diagnostics.Debug.WriteLine($"  - ColumnValues: '{queryItem.ColumnValues}'");

            // H占쏙옙占쏙옙 'Y'占쏙옙 占싣니몌옙 占싯몌옙占쏙옙 占쌩곤옙占쏙옙占쏙옙 占쏙옙占쏙옙
            if (queryItem.NotifyFlag != "Y")
            {
                System.Diagnostics.Debug.WriteLine($"[{queryItem.QueryName}] NotifyFlag != 'Y', 占시뤄옙 체크 占실너띰옙");
                return;
            }

            // L占쏙옙占쏙옙 M占쏙옙 체크
            if (string.IsNullOrWhiteSpace(queryItem.ColumnNames) ||
                string.IsNullOrWhiteSpace(queryItem.ColumnValues))
            {
                System.Diagnostics.Debug.WriteLine($"[{queryItem.QueryName}] L占쏙옙/M占쏙옙占쏙옙 占쏙옙占쏙옙占쏙옙占? 占시뤄옙 체크 占실너띰옙");
                System.Diagnostics.Debug.WriteLine($"  - ColumnNames IsNullOrWhiteSpace: {string.IsNullOrWhiteSpace(queryItem.ColumnNames)}");
                System.Diagnostics.Debug.WriteLine($"  - ColumnValues IsNullOrWhiteSpace: {string.IsNullOrWhiteSpace(queryItem.ColumnValues)}");
                return;
            }

            System.Diagnostics.Debug.WriteLine($"[{queryItem.QueryName}] 占시뤄옙 占쏙옙 체크 占쏙옙占쏙옙");
            System.Diagnostics.Debug.WriteLine($"  - L占쏙옙(占시뤄옙占쏙옙): '{queryItem.ColumnNames}'");
            System.Diagnostics.Debug.WriteLine($"  - M占쏙옙(占쏙옙): '{queryItem.ColumnValues}'");

            var columnNames = queryItem.ColumnNames.Split(',').Select(c => c.Trim()).ToList();
            var columnValues = queryItem.ColumnValues.Split(',').Select(v => v.Trim()).ToList();

            System.Diagnostics.Debug.WriteLine($"  - 占식싱듸옙 占시뤄옙占쏙옙 占쏙옙占쏙옙: {columnNames.Count}, 占쏙옙: [{string.Join(", ", columnNames)}]");
            System.Diagnostics.Debug.WriteLine($"  - 占식싱듸옙 占쏙옙 占쏙옙占쏙옙: {columnValues.Count}, 占쏙옙: [{string.Join(", ", columnValues)}]");

            if (columnNames.Count != columnValues.Count)
            {
                System.Diagnostics.Debug.WriteLine($"  [占쏙옙占? 占시뤄옙占쏙옙 占쏙옙占쏙옙({columnNames.Count})占쏙옙 占쏙옙 占쏙옙占쏙옙({columnValues.Count})占쏙옙 占쌕몌옙");
                return;
            }

            System.Diagnostics.Debug.WriteLine($"  - 占쏙옙 {result.Rows.Count}占쏙옙 占쏙옙 占싯삼옙");
            System.Diagnostics.Debug.WriteLine($"  - 占쏙옙占?占쏙옙占싱븝옙 占시뤄옙 占쏙옙占? [{string.Join(", ", result.Columns.Cast<DataColumn>().Select(c => c.ColumnName))}]");

            int mismatchCount = 0;
            for (int i = 0; i < result.Rows.Count; i++)
            {
                var row = result.Rows[i];
                bool anyMismatch = false;

                System.Diagnostics.Debug.WriteLine($"    占쏙옙 {i + 1} 占싯삼옙 占쏙옙占쏙옙:");

                // OR 占쏙옙占쏙옙占쏙옙占쏙옙 占쏙옙占쏙옙: 占싹놂옙占쏙옙 占쏙옙占쏙옙치占싹몌옙 占싯몌옙
                for (int j = 0; j < columnNames.Count; j++)
                {
                    string columnName = columnNames[j];
                    string expectedValue = columnValues[j];

                    if (!result.Columns.Contains(columnName))
                    {
                        System.Diagnostics.Debug.WriteLine($"      占시뤄옙 '{columnName}' 占쏙옙占쏙옙");
                        continue;
                    }

                    var actualValue = row[columnName]?.ToString()?.Trim() ?? "";
                    bool isMatch = actualValue == expectedValue;
                    System.Diagnostics.Debug.WriteLine($"      占시뤄옙 '{columnName}': 占쏙옙占쏙옙占쏙옙='{actualValue}', 占쏙옙諛?'{expectedValue}', 占쏙옙치={isMatch}");
                    
                    // OR 占쏙옙占쏙옙: 占싹놂옙占쏙옙 占쏙옙占쏙옙치占싹몌옙 anyMismatch = true
                    if (!isMatch)
                    {
                        anyMismatch = true;
                        System.Diagnostics.Debug.WriteLine($"      [占쏙옙占쏙옙치] 占쏙옙占쏙옙치 占쌩곤옙: {columnName}");
                    }
                }

                if (anyMismatch)
                {
                    mismatchCount++;
                    var checkInfo = string.Join(", ", columnNames.Zip(columnValues, (n, v) => $"{n}={v}"));
                    System.Diagnostics.Debug.WriteLine($"    [占쏙옙占쏙옙치] 占쏙옙 {i + 1}: 占쏙옙占쏙옙 占쏙옙占쏙옙치 占쌩곤옙 - 占쏙옙諛? {checkInfo}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"    [占쏙옙치] 占쏙옙 {i + 1}: 占쏙옙占?占쏙옙占쏙옙 占쏙옙치 (占싯몌옙 占쏙옙占쏙옙)");
                }
            }

            // 占쏙옙占실울옙 占쏙옙占쏙옙치占싹댐옙 占쏙옙占쏙옙 占싹놂옙占쏙옙 占쏙옙占쏙옙占쏙옙 占싯몌옙 占쌩곤옙
            if (mismatchCount > 0)
            {
                var checkInfo = string.Join(", ", columnNames.Zip(columnValues, (n, v) => $"{n}={v}"));
                var msg = $"[{queryItem.QueryName}] 占쏙옙占쏙옙 占쏙옙占쏙옙치 占쌩곤옙: {mismatchCount}占쏙옙 占쏙옙 (占쏙옙諛? {checkInfo})";
                notifications.Add(msg);
                System.Diagnostics.Debug.WriteLine($"  [占싯몌옙 占쌩곤옙] 占싯몌옙 占쌩곤옙: {msg}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"  [占쏙옙占쏙옙] 占쏙옙占?占쏙옙占쏙옙 占쏙옙占실곤옙 占쏙옙치 - 占싯몌옙 占쏙옙占쏙옙");
            }

            System.Diagnostics.Debug.WriteLine($"  - 占시뤄옙 占쏙옙 체크 占싹뤄옙, 占쏙옙 占쏙옙占쏙옙치 占쏙옙: {mismatchCount}占쏙옙");
        }
    }
}
