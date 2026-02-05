using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace FACTOVA_QueryHelper.Utils
{
    /// <summary>
    /// 쿼리 주석에서 버전 정보를 파싱하는 유틸리티
    /// </summary>
    public static class QueryVersionParser
    {
        /// <summary>
        /// 쿼리 주석에서 버전 정보 추출
        /// 예: -- @VERSION: 2.0 또는 /* @VERSION: 1.0 */
        /// </summary>
        /// <param name="query">SQL 쿼리</param>
        /// <returns>버전 문자열 (예: "1.0", "2.0"), 없으면 기본값 "2.0"</returns>
        public static string ExtractVersion(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return "2.0"; // 기본값
            
            // 다양한 주석 패턴 지원
            var patterns = new[]
            {
                @"--\s*@VERSION:\s*([0-9.]+)",           // -- @VERSION: 2.0
                @"/\*.*?@VERSION:\s*([0-9.]+).*?\*/",    // /* @VERSION: 2.0 */
                @"--\s*VERSION:\s*([0-9.]+)",            // -- VERSION: 2.0
                @"/\*.*?VERSION:\s*([0-9.]+).*?\*/",     // /* VERSION: 1.0 */
            };
            
            foreach (var pattern in patterns)
            {
                var match = Regex.Match(query, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
                if (match.Success)
                {
                    var version = match.Groups[1].Value.Trim();
return version;
                }
            }
            
            // 버전 정보 없으면 기본값 2.0
return "2.0";
        }
        
        /// <summary>
        /// 쿼리 주석에서 대상 사업장 추출 (선택사항)
        /// 예: -- @SITE: KC,LV,CP
        /// </summary>
        /// <param name="query">SQL 쿼리</param>
        /// <returns>사업장 코드 목록</returns>
        public static List<string> ExtractTargetSites(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return new List<string>();
            
            var patterns = new[]
            {
                @"--\s*@SITE:\s*([A-Z,\s]+)",           // -- @SITE: KC,LV
                @"/\*.*?@SITE:\s*([A-Z,\s]+).*?\*/",    // /* @SITE: KC,LV,CP */
            };
            
            foreach (var pattern in patterns)
            {
                var match = Regex.Match(query, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
                if (match.Success)
                {
                    var sites = match.Groups[1].Value
                        .Split(',')
                        .Select(s => s.Trim().ToUpper())
                        .Where(s => !string.IsNullOrEmpty(s))
                        .ToList();
                    
                    
                    return sites;
                }
            }
            
            return new List<string>();
        }
        
        /// <summary>
        /// 쿼리에 버전 주석 추가
        /// </summary>
        /// <param name="query">SQL 쿼리</param>
        /// <param name="version">버전 (예: "2.0")</param>
        /// <returns>버전 주석이 추가된 쿼리</returns>
        public static string AddVersionComment(string query, string version)
        {
            if (string.IsNullOrWhiteSpace(query))
                return query;
            
            // 이미 버전 주석이 있는지 확인
            if (Regex.IsMatch(query, @"@VERSION:", RegexOptions.IgnoreCase))
            {
                // 이미 있으면 그대로 반환
                return query;
            }
            
            // 쿼리 시작 부분에 버전 주석 추가
            return $"-- @VERSION: {version}\n{query}";
        }
    }
}
