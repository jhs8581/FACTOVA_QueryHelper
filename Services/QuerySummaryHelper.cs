using System.Collections.Generic;

namespace FACTOVA_QueryHelper
{
    /// <summary>
    /// ���� ��� ������ �����ϴ� Ŭ����
    /// </summary>
    public class QuerySummaryInfo
    {
        public string QueryName { get; set; } = string.Empty;
        public bool IsSuccess { get; set; }
        public bool HasNotification { get; set; }
        public double Duration { get; set; }
    }

    /// <summary>
    /// �α׿��� ���� ��� ������ �Ľ��ϴ� ���� Ŭ����
    /// </summary>
    public static class QuerySummaryHelper
    {
        public static List<QuerySummaryInfo> ParseQuerySummaries(List<string> logs, List<string> notifiedQueryNames)
        {
            var summaries = new List<QuerySummaryInfo>();
            string currentQueryName = "";
            double currentDuration = 0;
            bool foundDuration = false;

            System.Diagnostics.Debug.WriteLine("=== QuerySummaryHelper.ParseQuerySummaries ���� ===");
            System.Diagnostics.Debug.WriteLine($"��ü �α� ��: {logs.Count}");
            System.Diagnostics.Debug.WriteLine($"�˸� ���� ��: {notifiedQueryNames.Count}");

            for (int i = 0; i < logs.Count; i++)
            {
                var log = logs[i];
                System.Diagnostics.Debug.WriteLine($"�α�: {log}");

                // [1/5] ������ ���� �Ľ�
                if (log.Contains("[") && log.Contains("]") && log.Contains("/"))
                {
                    int startBracket = log.IndexOf('[');
                    int endBracket = log.IndexOf(']', startBracket);
                    
                    if (endBracket > startBracket && log.Length > endBracket + 1)
                    {
                        currentQueryName = log.Substring(endBracket + 2).Trim();
                        currentDuration = 0;
                        foundDuration = false;
                        System.Diagnostics.Debug.WriteLine($"������ �Ľ�: {currentQueryName}");
                        
                        // ���� �ٵ��� �̸� �о �ҿ� �ð��� ����/���� ã��
                        for (int j = i + 1; j < logs.Count && j < i + 15; j++)
                        {
                            var nextLog = logs[j];
                            
                            // �ҿ� �ð� �Ľ�
                            if (nextLog.Contains("�ҿ� �ð�:") && nextLog.Contains("��"))
                            {
                                try
                                {
                                    int start = nextLog.IndexOf(":") + 1;
                                    int end = nextLog.IndexOf("��");
                                    if (start > 0 && end > start)
                                    {
                                        string durationStr = nextLog.Substring(start, end - start).Trim();
                                        double.TryParse(durationStr, out currentDuration);
                                        foundDuration = true;
                                        System.Diagnostics.Debug.WriteLine($"�ҿ� �ð� �Ľ�: {currentDuration}��");
                                    }
                                }
                                catch (System.Exception ex) 
                                {
                                    System.Diagnostics.Debug.WriteLine($"�ҿ� �ð� �Ľ� ����: {ex.Message}");
                                }
                            }
                            
                            // ���� �Ľ�
                            if (nextLog.Contains("[����]"))
                            {
                                bool hasNotification = notifiedQueryNames.Contains(currentQueryName);
                                System.Diagnostics.Debug.WriteLine($"���� ���� �߰�: {currentQueryName}, �˸�: {hasNotification}, �ð�: {currentDuration}��");
                                
                                summaries.Add(new QuerySummaryInfo
                                {
                                    QueryName = currentQueryName,
                                    IsSuccess = true,
                                    HasNotification = hasNotification,
                                    Duration = currentDuration
                                });
                                
                                currentQueryName = "";
                                currentDuration = 0;
                                foundDuration = false;
                                break;
                            }
                            
                            // ���� �Ľ�
                            if (nextLog.Contains("[����]"))
                            {
                                System.Diagnostics.Debug.WriteLine($"���� ���� �߰�: {currentQueryName}");
                                
                                summaries.Add(new QuerySummaryInfo
                                {
                                    QueryName = currentQueryName,
                                    IsSuccess = false,
                                    HasNotification = false,
                                    Duration = 0
                                });
                                
                                currentQueryName = "";
                                currentDuration = 0;
                                foundDuration = false;
                                break;
                            }
                            
                            // ���� ������ ������ �ߴ�
                            if (nextLog.Contains("[") && nextLog.Contains("]") && nextLog.Contains("/"))
                            {
                                break;
                            }
                        }
                    }
                }
            }

            System.Diagnostics.Debug.WriteLine($"�Ľ̵� ��� ��: {summaries.Count}");
            System.Diagnostics.Debug.WriteLine("=== QuerySummaryHelper.ParseQuerySummaries �Ϸ� ===");

            return summaries;
        }
    }
}
