using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace FACTOVA_Palletizing_Analysis
{
    /// <summary>
    /// ���� ���� �� ��� ������ ����ϴ� Ŭ����
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
        /// TNS ��Ʈ�� ����� ������Ʈ�մϴ�.
        /// </summary>
        public void UpdateTnsEntries(List<TnsEntry> tnsEntries)
        {
            _tnsEntries = tnsEntries ?? throw new ArgumentNullException(nameof(tnsEntries));
        }

        /// <summary>
        /// ������ ������Ʈ�մϴ�.
        /// </summary>
        public void UpdateSettings(AppSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        /// <summary>
        /// ���� ���� ���
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
        /// ���� ������ �����մϴ�.
        /// </summary>
        public async Task<ExecutionResult> ExecuteQueriesAsync(List<QueryItem> queries)
        {
            if (queries == null || queries.Count == 0)
            {
                throw new ArgumentException("������ ������ �����ϴ�.", nameof(queries));
            }

            var result = new ExecutionResult
            {
                StartTime = DateTime.Now
            };

            // ���� ��� �� �ʱ�ȭ
            _resultTabControl.Items.Clear();

            // �۾� �α� ��� �߰�
            result.ExecutionLogs.Add($"�۾� ���� �ð�: {result.StartTime:yyyy-MM-dd HH:mm:ss}");
            result.ExecutionLogs.Add($"��?�ɵ� ���� ��: {queries.Count}��");
            result.ExecutionLogs.Add(new string('=', 80));
            result.ExecutionLogs.Add("");

            // G���� 'Y'�� ������ ���͸�
            var queriesToExecute = queries.Where(q =>
                string.IsNullOrWhiteSpace(q.EnabledFlag) || q.EnabledFlag == "Y").ToList();

            result.ExecutionLogs.Add($"���� ��� ����: {queriesToExecute.Count}��");
            result.ExecutionLogs.Add("");

            for (int i = 0; i < queriesToExecute.Count; i++)
            {
                var queryItem = queriesToExecute[i];

                _updateStatusCallback(
                    $"���� ���� ��... ({i + 1}/{queriesToExecute.Count}) - {queryItem.QueryName}",
                    Colors.Blue);

                var logEntry = new StringBuilder();
                logEntry.AppendLine($"[{i + 1}/{queriesToExecute.Count}] {queryItem.QueryName}");
                logEntry.AppendLine($"  ���� �ð�: {DateTime.Now:HH:mm:ss}");

                try
                {
                    var queryResult = await ExecuteSingleQueryAsync(queryItem, logEntry);

                    if (queryResult.IsSuccess)
                    {
                        // ��� �Ǽ� üũ �� �˸�
                        CheckResultCountAndNotify(queryItem, queryResult.Result!.Rows.Count, result.Notifications);

                        // Ư�� �÷� �� üũ �� �˸�
                        CheckColumnValuesAndNotify(queryItem, queryResult.Result!, result.Notifications);

                        if (result.Notifications.Count > 0)
                        {
                            logEntry.AppendLine($"  ?? �˸�: {result.Notifications.Count}��");
                        }

                        logEntry.AppendLine($"  ? ����");
                        
                        // ��� �� ����
                        _createResultTabCallback(queryItem, queryResult.Result, queryResult.Duration, null);
                        
                        result.SuccessCount++;
                    }
                    else
                    {
                        logEntry.AppendLine($"  ? ����: {queryResult.ErrorMessage}");
                        
                        // ���� �� ����
                        _createResultTabCallback(queryItem, null, 0, queryResult.ErrorMessage);
                        
                        result.FailCount++;
                    }
                }
                catch (Exception ex)
                {
                    logEntry.AppendLine($"  ? ����: {ex.Message}");
                    
                    // ���� �� ����
                    _createResultTabCallback(queryItem, null, 0, ex.Message);
                    
                    result.FailCount++;
                }

                result.ExecutionLogs.Add(logEntry.ToString());
            }

            result.TotalDuration = (DateTime.Now - result.StartTime).TotalSeconds;

            // �۾� ��� �߰�
            result.ExecutionLogs.Add(new string('=', 80));
            result.ExecutionLogs.Add("");
            result.ExecutionLogs.Add("?? �۾� ���");
            result.ExecutionLogs.Add($"  �� ���� �ð�: {result.TotalDuration:F2}��");
            result.ExecutionLogs.Add($"  ����: {result.SuccessCount}��");
            result.ExecutionLogs.Add($"  ����: {result.FailCount}��");
            result.ExecutionLogs.Add($"  �˸�: {result.Notifications.Count}��");
            result.ExecutionLogs.Add("");
            result.ExecutionLogs.Add($"�۾� �Ϸ� �ð�: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

            return result;
        }

        /// <summary>
        /// ���� ���� ���� ���
        /// </summary>
        private class SingleQueryResult
        {
            public bool IsSuccess { get; set; }
            public DataTable? Result { get; set; }
            public double Duration { get; set; }
            public string? ErrorMessage { get; set; }
        }

        /// <summary>
        /// ���� ������ �����մϴ�.
        /// </summary>
        private async Task<SingleQueryResult> ExecuteSingleQueryAsync(QueryItem queryItem, StringBuilder logEntry)
        {
            var result = new SingleQueryResult();
            string connectionString;

            try
            {
                // ���� ���� ������ �ִ��� Ȯ��
                if (!string.IsNullOrWhiteSpace(queryItem.Host) &&
                    !string.IsNullOrWhiteSpace(queryItem.Port) &&
                    !string.IsNullOrWhiteSpace(queryItem.ServiceName))
                {
                    connectionString = $"Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST={queryItem.Host})(PORT={queryItem.Port}))(CONNECT_DATA=(SERVICE_NAME={queryItem.ServiceName})));";
                    logEntry.AppendLine($"  ����: {queryItem.Host}:{queryItem.Port}/{queryItem.ServiceName}");
                }
                else
                {
                    // TNS ���� ã��
                    var selectedTns = _tnsEntries.FirstOrDefault(t =>
                        t.Name.Equals(queryItem.TnsName, StringComparison.OrdinalIgnoreCase));

                    if (selectedTns == null)
                    {
                        var availableTns = string.Join(", ", _tnsEntries.Select(t => t.Name));
                        throw new Exception($"TNS '{queryItem.TnsName}'�� ã�� �� �����ϴ�.\n\n" +
                            $"?? �ذ� ���:\n" +
                            $"1. Excel A���� ��Ȯ�� TNS �̸� �Է�\n" +
                            $"2. �Ǵ� Host:Port:ServiceName �������� �Է�\n" +
                            $"   ��) 192.168.1.10:1521:ORCL\n\n" +
                            $"��� ������ TNS ���:\n{availableTns}\n\n" +
                            $"tnsnames.ora ���� ���:\n{_settings.TnsPath}");
                    }

                    connectionString = selectedTns.ConnectionString;
                    logEntry.AppendLine($"  TNS: {queryItem.TnsName}");
                }

                // User ID�� Password ����
                if (string.IsNullOrWhiteSpace(queryItem.UserId))
                    throw new Exception("User ID�� �������� �ʾҽ��ϴ�.");

                if (string.IsNullOrWhiteSpace(queryItem.Password))
                    throw new Exception("Password�� �������� �ʾҽ��ϴ�.");

                logEntry.AppendLine($"  �����: {queryItem.UserId}");

                var startTime = DateTime.Now;

                // ���� ����
                result.Result = await OracleDatabase.ExecuteQueryAsync(
                    connectionString,
                    queryItem.UserId,
                    queryItem.Password,
                    queryItem.Query);

                var endTime = DateTime.Now;
                result.Duration = (endTime - startTime).TotalSeconds;

                logEntry.AppendLine($"  �Ϸ� �ð�: {endTime:HH:mm:ss}");
                logEntry.AppendLine($"  �ҿ� �ð�: {result.Duration:F2}��");
                logEntry.AppendLine($"  ���: {result.Result.Rows.Count}�� �� {result.Result.Columns.Count}��");

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
        /// ��� �Ǽ��� üũ�ϰ� �˸��� �߰��մϴ�.
        /// </summary>
        private void CheckResultCountAndNotify(QueryItem queryItem, int rowCount, List<string> notifications)
        {
            if (queryItem.NotifyFlag != "Y")
                return;

            // I��: �̻��� ��
            if (!string.IsNullOrWhiteSpace(queryItem.CountGreaterThan) &&
                int.TryParse(queryItem.CountGreaterThan, out int greaterThan))
            {
                if (rowCount >= greaterThan)
                {
                    notifications.Add($"[{queryItem.QueryName}] ��ȸ ��� {rowCount}�� (����: {greaterThan}�� �̻�)");
                }
            }

            // J��: ���� ��
            if (!string.IsNullOrWhiteSpace(queryItem.CountEquals) &&
                int.TryParse(queryItem.CountEquals, out int equals))
            {
                if (rowCount == equals)
                {
                    notifications.Add($"[{queryItem.QueryName}] ��ȸ ��� {rowCount}�� (����: {equals}�ǰ� ����)");
                }
            }

            // K��: ������ ��
            if (!string.IsNullOrWhiteSpace(queryItem.CountLessThan) &&
                int.TryParse(queryItem.CountLessThan, out int lessThan))
            {
                if (rowCount <= lessThan)
                {
                    notifications.Add($"[{queryItem.QueryName}] ��ȸ ��� {rowCount}�� (����: {lessThan}�� ����)");
                }
            }
        }

        /// <summary>
        /// �÷� ���� üũ�ϰ� �˸��� �߰��մϴ�.
        /// </summary>
        private void CheckColumnValuesAndNotify(QueryItem queryItem, DataTable result, List<string> notifications)
        {
            if (queryItem.NotifyFlag != "Y")
                return;

            if (string.IsNullOrWhiteSpace(queryItem.ColumnNames) ||
                string.IsNullOrWhiteSpace(queryItem.ColumnValues))
            {
                return;
            }

            var columnNames = queryItem.ColumnNames.Split(',').Select(c => c.Trim()).ToList();
            var columnValues = queryItem.ColumnValues.Split(',').Select(v => v.Trim()).ToList();

            if (columnNames.Count != columnValues.Count)
                return;

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
                    notifications.Add($"[{queryItem.QueryName}] ���� ��ġ �߰� (�� {i + 1}): {matchInfo}");
                }
            }
        }
    }
}
