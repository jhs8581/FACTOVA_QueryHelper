using System.Collections.Generic;

namespace FACTOVA_QueryHelper
{
    /// <summary>
    /// 쿼리 요약 정보를 저장하는 클래스
    /// </summary>
    public class QuerySummaryInfo
    {
        public string QueryName { get; set; } = string.Empty;
        public bool IsSuccess { get; set; }
        public bool HasNotification { get; set; }
        public double Duration { get; set; }
    }

    /// <summary>
    /// 로그에서 쿼리 요약 정보를 파싱하는 헬퍼 클래스
    /// </summary>
    public static class QuerySummaryHelper
    {
        public static List<QuerySummaryInfo> ParseQuerySummaries(List<string> logs, List<string> notifiedQueryNames)
        {
            var summaries = new List<QuerySummaryInfo>();
            string currentQueryName = "";
            double currentDuration = 0;
            bool foundDuration = false;

            System.Diagnostics.Debug.WriteLine("=== QuerySummaryHelper.ParseQuerySummaries 시작 ===");
            System.Diagnostics.Debug.WriteLine($"전체 로그 수: {logs.Count}");
            System.Diagnostics.Debug.WriteLine($"알림 쿼리 수: {notifiedQueryNames.Count}");

            for (int i = 0; i < logs.Count; i++)
            {
                var log = logs[i];
                System.Diagnostics.Debug.WriteLine($"로그: {log}");

                // [1/5] 쿼리명 형식 파싱
                if (log.Contains("[") && log.Contains("]") && log.Contains("/"))
                {
                    int startBracket = log.IndexOf('[');
                    int endBracket = log.IndexOf(']', startBracket);
                    
                    if (endBracket > startBracket && log.Length > endBracket + 1)
                    {
                        currentQueryName = log.Substring(endBracket + 2).Trim();
                        currentDuration = 0;
                        foundDuration = false;
                        System.Diagnostics.Debug.WriteLine($"쿼리명 파싱: {currentQueryName}");
                        
                        // 다음 줄들을 미리 읽어서 소요 시간과 성공/실패 찾기
                        for (int j = i + 1; j < logs.Count && j < i + 15; j++)
                        {
                            var nextLog = logs[j];
                            
                            // 소요 시간 파싱
                            if (nextLog.Contains("소요 시간:") && nextLog.Contains("초"))
                            {
                                try
                                {
                                    int start = nextLog.IndexOf(":") + 1;
                                    int end = nextLog.IndexOf("초");
                                    if (start > 0 && end > start)
                                    {
                                        string durationStr = nextLog.Substring(start, end - start).Trim();
                                        double.TryParse(durationStr, out currentDuration);
                                        foundDuration = true;
                                        System.Diagnostics.Debug.WriteLine($"소요 시간 파싱: {currentDuration}초");
                                    }
                                }
                                catch (System.Exception ex) 
                                {
                                    System.Diagnostics.Debug.WriteLine($"소요 시간 파싱 오류: {ex.Message}");
                                }
                            }
                            
                            // 성공 파싱
                            if (nextLog.Contains("[성공]"))
                            {
                                bool hasNotification = notifiedQueryNames.Contains(currentQueryName);
                                System.Diagnostics.Debug.WriteLine($"성공 쿼리 추가: {currentQueryName}, 알림: {hasNotification}, 시간: {currentDuration}초");
                                
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
                            
                            // 실패 파싱
                            if (nextLog.Contains("[실패]"))
                            {
                                System.Diagnostics.Debug.WriteLine($"실패 쿼리 추가: {currentQueryName}");
                                
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
                            
                            // 다음 쿼리를 만나면 중단
                            if (nextLog.Contains("[") && nextLog.Contains("]") && nextLog.Contains("/"))
                            {
                                break;
                            }
                        }
                    }
                }
            }

            System.Diagnostics.Debug.WriteLine($"파싱된 요약 수: {summaries.Count}");
            System.Diagnostics.Debug.WriteLine("=== QuerySummaryHelper.ParseQuerySummaries 완료 ===");

            return summaries;
        }
    }
}
