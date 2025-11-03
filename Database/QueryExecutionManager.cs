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
            public List<string> NotifiedQueryNames { get; set; } = new List<string>();
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
            result.ExecutionLogs.Add($"���õ� ���� ��: {queries.Count}��");
            result.ExecutionLogs.Add(new string('=', 80));
            result.ExecutionLogs.Add("");

            // ���޹��� ������ �״�� ���� (���͸��� MainWindow���� ó����)
            var queriesToExecute = queries;

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
                        // �˸� �߰� �� ���� �˸� ���� ����
                        int notificationsBefore = result.Notifications.Count;

                        // ��� �Ǽ� üũ �� �˸�
                        CheckResultCountAndNotify(queryItem, queryResult.Result!.Rows.Count, result.Notifications);

                        // Ư�� �÷� �� üũ �� �˸�
                        CheckColumnValuesAndNotify(queryItem, queryResult.Result!, result.Notifications);

                        // �̹� �������� �߰��� �˸� ���� ���
                        int newNotifications = result.Notifications.Count - notificationsBefore;
                        
                        if (newNotifications > 0)
                        {
                            logEntry.AppendLine($"  [�˸�] �˸�: {newNotifications}��");
                            // �˸� ���� �α׿� �߰�
                            for (int n = notificationsBefore; n < result.Notifications.Count; n++)
                            {
                                logEntry.AppendLine($"    - {result.Notifications[n].Replace($"[{queryItem.QueryName}] ", "")}");
                            }
                            
                            // �˸��� �߻��� ���� �̸� �߰�
                            if (!result.NotifiedQueryNames.Contains(queryItem.QueryName))
                            {
                                result.NotifiedQueryNames.Add(queryItem.QueryName);
                            }
                        }

                        logEntry.AppendLine($"  [����] ����");
                        
                        // ��� �� ����
                        _createResultTabCallback(queryItem, queryResult.Result, queryResult.Duration, null);
                        
                        result.SuccessCount++;
                    }
                    else
                    {
                        logEntry.AppendLine($"  [����] ����: {queryResult.ErrorMessage}");
                        
                        // ���� �� ����
                        _createResultTabCallback(queryItem, null, 0, queryResult.ErrorMessage);
                        
                        result.FailCount++;
                    }
                }
                catch (Exception ex)
                {
                    logEntry.AppendLine($"  [����] ����: {ex.Message}");
                    
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
            result.ExecutionLogs.Add("[�۾� ���]");
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
                            $"[�ذ� ���]\n" +
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
            // H���� 'Y'�� �ƴϸ� �˸��� �߰����� ����
            if (queryItem.NotifyFlag != "Y")
            {
                System.Diagnostics.Debug.WriteLine($"[{queryItem.QueryName}] NotifyFlag != 'Y', �˸� �ǳʶ� (NotifyFlag={queryItem.NotifyFlag})");
                return;
            }

            System.Diagnostics.Debug.WriteLine($"[{queryItem.QueryName}] �˸� üũ ���� - ��� �Ǽ�: {rowCount}��");

            // I��: �̻��� ��
            if (!string.IsNullOrWhiteSpace(queryItem.CountGreaterThan) &&
                int.TryParse(queryItem.CountGreaterThan, out int greaterThan))
            {
                System.Diagnostics.Debug.WriteLine($"  - I��(�̻�) üũ: {rowCount} >= {greaterThan} ?");
                if (rowCount >= greaterThan)
                {
                    var msg = $"[{queryItem.QueryName}] ��ȸ ��� {rowCount}�� (����: {greaterThan}�� �̻�)";
                    notifications.Add(msg);
                    System.Diagnostics.Debug.WriteLine($"  [�˸� �߰�] �˸� �߰�: {msg}");
                }
            }

            // J��: ���� ��
            if (!string.IsNullOrWhiteSpace(queryItem.CountEquals) &&
                int.TryParse(queryItem.CountEquals, out int equals))
            {
                System.Diagnostics.Debug.WriteLine($"  - J��(����) üũ: {rowCount} == {equals} ?");
                if (rowCount == equals)
                {
                    var msg = $"[{queryItem.QueryName}] ��ȸ ��� {rowCount}�� (����: {equals}�ǰ� ����)";
                    notifications.Add(msg);
                    System.Diagnostics.Debug.WriteLine($"  [�˸� �߰�] �˸� �߰�: {msg}");
                }
            }

            // K��: ������ ��
            if (!string.IsNullOrWhiteSpace(queryItem.CountLessThan) &&
                int.TryParse(queryItem.CountLessThan, out int lessThan))
            {
                System.Diagnostics.Debug.WriteLine($"  - K��(����) üũ: {rowCount} <= {lessThan} ?");
                if (rowCount <= lessThan)
                {
                    var msg = $"[{queryItem.QueryName}] ��ȸ ��� {rowCount}�� (����: {lessThan}�� ����)";
                    notifications.Add(msg);
                    System.Diagnostics.Debug.WriteLine($"  [�˸� �߰�] �˸� �߰�: {msg}");
                }
            }
        }

        /// <summary>
        /// �÷� ���� üũ�ϰ� �˸��� �߰��մϴ�.
        /// </summary>
        private void CheckColumnValuesAndNotify(QueryItem queryItem, DataTable result, List<string> notifications)
        {
            System.Diagnostics.Debug.WriteLine($"[{queryItem.QueryName}] CheckColumnValuesAndNotify ����");
            System.Diagnostics.Debug.WriteLine($"  - NotifyFlag: '{queryItem.NotifyFlag}'");
            System.Diagnostics.Debug.WriteLine($"  - ColumnNames: '{queryItem.ColumnNames}'");
            System.Diagnostics.Debug.WriteLine($"  - ColumnValues: '{queryItem.ColumnValues}'");

            // H���� 'Y'�� �ƴϸ� �˸��� �߰����� ����
            if (queryItem.NotifyFlag != "Y")
            {
                System.Diagnostics.Debug.WriteLine($"[{queryItem.QueryName}] NotifyFlag != 'Y', �÷� üũ �ǳʶ�");
                return;
            }

            // L���� M�� üũ
            if (string.IsNullOrWhiteSpace(queryItem.ColumnNames) ||
                string.IsNullOrWhiteSpace(queryItem.ColumnValues))
            {
                System.Diagnostics.Debug.WriteLine($"[{queryItem.QueryName}] L��/M���� �������, �÷� üũ �ǳʶ�");
                System.Diagnostics.Debug.WriteLine($"  - ColumnNames IsNullOrWhiteSpace: {string.IsNullOrWhiteSpace(queryItem.ColumnNames)}");
                System.Diagnostics.Debug.WriteLine($"  - ColumnValues IsNullOrWhiteSpace: {string.IsNullOrWhiteSpace(queryItem.ColumnValues)}");
                return;
            }

            System.Diagnostics.Debug.WriteLine($"[{queryItem.QueryName}] �÷� �� üũ ����");
            System.Diagnostics.Debug.WriteLine($"  - L��(�÷���): '{queryItem.ColumnNames}'");
            System.Diagnostics.Debug.WriteLine($"  - M��(��): '{queryItem.ColumnValues}'");

            var columnNames = queryItem.ColumnNames.Split(',').Select(c => c.Trim()).ToList();
            var columnValues = queryItem.ColumnValues.Split(',').Select(v => v.Trim()).ToList();

            System.Diagnostics.Debug.WriteLine($"  - �Ľ̵� �÷��� ����: {columnNames.Count}, ��: [{string.Join(", ", columnNames)}]");
            System.Diagnostics.Debug.WriteLine($"  - �Ľ̵� �� ����: {columnValues.Count}, ��: [{string.Join(", ", columnValues)}]");

            if (columnNames.Count != columnValues.Count)
            {
                System.Diagnostics.Debug.WriteLine($"  [���] �÷��� ����({columnNames.Count})�� �� ����({columnValues.Count})�� �ٸ�");
                return;
            }

            System.Diagnostics.Debug.WriteLine($"  - �� {result.Rows.Count}�� �� �˻�");
            System.Diagnostics.Debug.WriteLine($"  - ��� ���̺� �÷� ���: [{string.Join(", ", result.Columns.Cast<DataColumn>().Select(c => c.ColumnName))}]");

            int mismatchCount = 0;
            for (int i = 0; i < result.Rows.Count; i++)
            {
                var row = result.Rows[i];
                bool anyMismatch = false;

                System.Diagnostics.Debug.WriteLine($"    �� {i + 1} �˻� ����:");

                // OR �������� ����: �ϳ��� ����ġ�ϸ� �˸�
                for (int j = 0; j < columnNames.Count; j++)
                {
                    string columnName = columnNames[j];
                    string expectedValue = columnValues[j];

                    if (!result.Columns.Contains(columnName))
                    {
                        System.Diagnostics.Debug.WriteLine($"      �÷� '{columnName}' ����");
                        continue;
                    }

                    var actualValue = row[columnName]?.ToString()?.Trim() ?? "";
                    bool isMatch = actualValue == expectedValue;
                    System.Diagnostics.Debug.WriteLine($"      �÷� '{columnName}': ������='{actualValue}', ��밪='{expectedValue}', ��ġ={isMatch}");
                    
                    // OR ����: �ϳ��� ����ġ�ϸ� anyMismatch = true
                    if (!isMatch)
                    {
                        anyMismatch = true;
                        System.Diagnostics.Debug.WriteLine($"      [����ġ] ����ġ �߰�: {columnName}");
                    }
                }

                if (anyMismatch)
                {
                    mismatchCount++;
                    var checkInfo = string.Join(", ", columnNames.Zip(columnValues, (n, v) => $"{n}={v}"));
                    System.Diagnostics.Debug.WriteLine($"    [����ġ] �� {i + 1}: ���� ����ġ �߰� - ��밪: {checkInfo}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"    [��ġ] �� {i + 1}: ��� ���� ��ġ (�˸� ����)");
                }
            }

            // ���ǿ� ����ġ�ϴ� ���� �ϳ��� ������ �˸� �߰�
            if (mismatchCount > 0)
            {
                var checkInfo = string.Join(", ", columnNames.Zip(columnValues, (n, v) => $"{n}={v}"));
                var msg = $"[{queryItem.QueryName}] ���� ����ġ �߰�: {mismatchCount}�� �� (��밪: {checkInfo})";
                notifications.Add(msg);
                System.Diagnostics.Debug.WriteLine($"  [�˸� �߰�] �˸� �߰�: {msg}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"  [����] ��� ���� ���ǰ� ��ġ - �˸� ����");
            }

            System.Diagnostics.Debug.WriteLine($"  - �÷� �� üũ �Ϸ�, �� ����ġ ��: {mismatchCount}��");
        }
    }
}
