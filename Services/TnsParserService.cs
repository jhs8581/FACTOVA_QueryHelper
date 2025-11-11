using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FACTOVA_QueryHelper.Models;

namespace FACTOVA_QueryHelper.Services
{
    public class TnsParserService
    {
        /// <summary>
        /// tnsnames.ora 파일에서 TNS 엔트리 목록을 추출합니다.
        /// </summary>
        public static List<TnsEntry> ParseTnsFileToEntries(string filePath)
        {
            var tnsEntries = new List<TnsEntry>();

            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                return tnsEntries;
            }

            try
            {
                string content = File.ReadAllText(filePath);
                
                // 주석 제거 (# 로 시작하는 라인)
                var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                var filteredLines = lines.Where(line => !line.TrimStart().StartsWith("#")).ToList();
                
                // TNS 엔트리 추출
                string currentTnsName = "";
                StringBuilder currentBlock = new StringBuilder();
                
                foreach (var line in filteredLines)
                {
                    var trimmedLine = line.Trim();
                    
                    // TNS 이름 찾기 (= 앞에 나오는 이름)
                    if (!trimmedLine.StartsWith("(") && trimmedLine.Contains("="))
                    {
                        // 이전 블록 처리
                        if (!string.IsNullOrEmpty(currentTnsName))
                        {
                            var entry = ParseTnsBlock(currentTnsName, currentBlock.ToString());
                            if (entry != null)
                            {
                                tnsEntries.Add(entry);
                            }
                        }
                        
                        // 새 블록 시작
                        var parts = trimmedLine.Split('=');
                        currentTnsName = parts[0].Trim();
                        currentBlock.Clear();
                        currentBlock.AppendLine(trimmedLine.Substring(trimmedLine.IndexOf('=') + 1));
                    }
                    else if (!string.IsNullOrWhiteSpace(trimmedLine))
                    {
                        currentBlock.AppendLine(trimmedLine);
                    }
                }
                
                // 마지막 블록 처리
                if (!string.IsNullOrEmpty(currentTnsName))
                {
                    var entry = ParseTnsBlock(currentTnsName, currentBlock.ToString());
                    if (entry != null)
                    {
                        tnsEntries.Add(entry);
                    }
                }
                
                System.Diagnostics.Debug.WriteLine($"Total TNS entries parsed: {tnsEntries.Count}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"TNS 파일 파싱 오류: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
            }

            return tnsEntries.OrderBy(x => x.Name).ToList();
        }

        /// <summary>
        /// TNS 블록에서 상세 정보 추출
        /// </summary>
        private static TnsEntry? ParseTnsBlock(string tnsName, string block)
        {
            try
            {
                var entry = new TnsEntry { Name = tnsName };
                
                // HOST 추출
                var hostMatch = Regex.Match(block, @"HOST\s*=\s*([^\)]+)", RegexOptions.IgnoreCase);
                if (hostMatch.Success)
                {
                    entry.Host = hostMatch.Groups[1].Value.Trim();
                }
                
                // PORT 추출
                var portMatch = Regex.Match(block, @"PORT\s*=\s*(\d+)", RegexOptions.IgnoreCase);
                if (portMatch.Success)
                {
                    entry.Port = portMatch.Groups[1].Value.Trim();
                }
                
                // SERVICE_NAME 추출
                var serviceMatch = Regex.Match(block, @"SERVICE_NAME\s*=\s*([^\)]+)", RegexOptions.IgnoreCase);
                if (serviceMatch.Success)
                {
                    entry.ServiceName = serviceMatch.Groups[1].Value.Trim();
                }
                else
                {
                    // SID도 확인
                    var sidMatch = Regex.Match(block, @"SID\s*=\s*([^\)]+)", RegexOptions.IgnoreCase);
                    if (sidMatch.Success)
                    {
                        entry.ServiceName = sidMatch.Groups[1].Value.Trim();
                    }
                }
                
                // PROTOCOL 추출 (선택사항, 기본값 TCP)
                var protocolMatch = Regex.Match(block, @"PROTOCOL\s*=\s*([^\)]+)", RegexOptions.IgnoreCase);
                if (protocolMatch.Success)
                {
                    entry.Protocol = protocolMatch.Groups[1].Value.Trim();
                }
                
                System.Diagnostics.Debug.WriteLine($"Parsed TNS Entry: {entry.GetDetailString()}");
                
                // 필수 정보가 있는지 확인
                if (!string.IsNullOrEmpty(entry.Host) && !string.IsNullOrEmpty(entry.Port))
                {
                    return entry;
                }
                
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error parsing TNS block '{tnsName}': {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// tnsnames.ora 파일에서 TNS 이름 목록을 추출합니다. (하위 호환성 유지)
        /// </summary>
        public static List<string> ParseTnsFile(string filePath)
        {
            var entries = ParseTnsFileToEntries(filePath);
            return entries.Select(e => e.Name).ToList();
        }

        /// <summary>
        /// 기본 TNS 파일 경로를 반환합니다.
        /// 1순위: 사용자가 설정에서 지정한 경로 (UserSettingsService - 빌드 시에도 유지됨)
        /// 2순위: ORACLE_HOME 환경변수
        /// 3순위: 일반적인 설치 경로들 자동 탐색
        /// </summary>
        public static string GetDefaultTnsPath()
        {
            // 🔥 1순위: 사용자가 설정한 경로 (빌드 후에도 유지되는 고정 위치에 저장)
            try
            {
                var userSettings = UserSettingsService.Load();
                var userPath = userSettings.TnsFilePath;
                
                if (!string.IsNullOrEmpty(userPath) && File.Exists(userPath))
                {
                    System.Diagnostics.Debug.WriteLine($"✅ Using user-specified TNS path: {userPath}");
                    return userPath;
                }
                else if (!string.IsNullOrEmpty(userPath))
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ User-specified TNS path not found: {userPath}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Error reading user TNS path: {ex.Message}");
            }

            // 🔥 2순위: ORACLE_HOME 환경변수
            string oracleHome = Environment.GetEnvironmentVariable("ORACLE_HOME") ?? "";
            
            if (!string.IsNullOrEmpty(oracleHome))
            {
                string tnsPath = Path.Combine(oracleHome, "network", "admin", "tnsnames.ora");
                if (File.Exists(tnsPath))
                {
                    System.Diagnostics.Debug.WriteLine($"✅ Using ORACLE_HOME TNS path: {tnsPath}");
                    return tnsPath;
                }
            }

            // 🔥 3순위: 일반적인 설치 경로들 시도
            System.Diagnostics.Debug.WriteLine("🔍 Searching for tnsnames.ora in common locations...");
            
            string[] defaultPaths = new[]
            {
                @"C:\oracle\product\21c\dbhome_1\network\admin\tnsnames.ora",  // Oracle 21c
                @"C:\oracle\product\19c\dbhome_1\network\admin\tnsnames.ora",  // Oracle 19c
                @"C:\oracle\product\18c\dbhome_1\network\admin\tnsnames.ora",  // Oracle 18c
                @"C:\app\oracle\product\21c\dbhome_1\network\admin\tnsnames.ora",
                @"C:\app\oracle\product\19c\dbhome_1\network\admin\tnsnames.ora",
                @"C:\app\oracle\product\18c\dbhome_1\network\admin\tnsnames.ora",
                @"C:\oracle\product\11.2.0\client_1\network\admin\tnsnames.ora"
            };

            foreach (var path in defaultPaths)
            {
                if (File.Exists(path))
                {
                    System.Diagnostics.Debug.WriteLine($"✅ Found TNS file: {path}");
                    return path;
                }
            }

            System.Diagnostics.Debug.WriteLine("⚠️ No tnsnames.ora file found in any location");
            return string.Empty;
        }
    }
}
