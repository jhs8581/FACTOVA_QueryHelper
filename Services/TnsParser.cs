using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using FACTOVA_QueryHelper.Models;

namespace FACTOVA_QueryHelper
{
    public class TnsParser
    {
        public static List<TnsEntry> ParseTnsFile(string filePath)
        {
            var entries = new List<TnsEntry>();

            if (!File.Exists(filePath))
            {
                return entries;
            }

            try
            {
                // UTF-8, UTF-8 BOM, ANSI 등 다양한 인코딩으로 시도
                string content = ReadFileWithEncoding(filePath);
                
                // TNS 엔트리 추출 (이름 = 으로 시작하는 각 블록)
                // 예제, 각 블록은 이름과 괄호로 시작하는 설명 구성
                var matches = Regex.Matches(content, @"^[\s]*([^\s=]+)[\s]*=[\s]*\(DESCRIPTION", 
                    RegexOptions.IgnoreCase | RegexOptions.Multiline);

                foreach (Match match in matches)
                {
                    string tnsName = match.Groups[1].Value.Trim();
                    
                    // 해당 TNS 블록 추출
                    int startIndex = match.Index;
                    int endIndex = FindMatchingParenthesis(content, startIndex);
                    
                    if (endIndex > startIndex)
                    {
                        string block = content.Substring(startIndex, endIndex - startIndex + 1);
                        
                        var entry = new TnsEntry { Name = tnsName };
                        
                        // HOST 추출
                        var hostMatch = Regex.Match(block, @"HOST[\s]*=[\s]*([^\)]+)", RegexOptions.IgnoreCase);
                        if (hostMatch.Success)
                        {
                            entry.Host = hostMatch.Groups[1].Value.Trim();
                        }
                        
                        // PORT 추출
                        var portMatch = Regex.Match(block, @"PORT[\s]*=[\s]*(\d+)", RegexOptions.IgnoreCase);
                        if (portMatch.Success)
                        {
                            entry.Port = portMatch.Groups[1].Value.Trim();
                        }
                        
                        // SERVICE_NAME 또는 SID 추출
                        var serviceMatch = Regex.Match(block, @"SERVICE_NAME[\s]*=[\s]*([^\)]+)", RegexOptions.IgnoreCase);
                        if (serviceMatch.Success)
                        {
                            entry.ServiceName = serviceMatch.Groups[1].Value.Trim();
                        }
                        else
                        {
                            // SID로 시도
                            var sidMatch = Regex.Match(block, @"SID[\s]*=[\s]*([^\)]+)", RegexOptions.IgnoreCase);
                            if (sidMatch.Success)
                            {
                                entry.ServiceName = sidMatch.Groups[1].Value.Trim();
                            }
                        }
                        
                        entries.Add(entry);
                    }
                }
            }
            catch (Exception)
            {
                // 파싱 실패 시 빈 리스트 반환
            }

            return entries;
        }

        private static string ReadFileWithEncoding(string filePath)
        {
            try
            {
                // 먼저 UTF-8로 시도
                return File.ReadAllText(filePath, System.Text.Encoding.UTF8);
            }
            catch
            {
                try
                {
                    // ANSI (Default)로 시도
                    return File.ReadAllText(filePath, System.Text.Encoding.Default);
                }
                catch
                {
                    // ASCII로 시도
                    return File.ReadAllText(filePath, System.Text.Encoding.ASCII);
                }
            }
        }

        private static int FindMatchingParenthesis(string content, int startIndex)
        {
            int openCount = 0;
            for (int i = startIndex; i < content.Length; i++)
            {
                if (content[i] == '(')
                    openCount++;
                else if (content[i] == ')')
                {
                    openCount--;
                    if (openCount == 0)
                        return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// TNS 파일에서 모든 엔트리 이름만 추출 (간단함)
        /// </summary>
        public static List<string> GetAllEntryNames(string filePath)
        {
            var names = new List<string>();

            if (!File.Exists(filePath))
            {
                return names;
            }

            try
            {
                string content = ReadFileWithEncoding(filePath);
                var matches = Regex.Matches(content, @"^[\s]*([^\s=]+)[\s]*=[\s]*\(DESCRIPTION", 
                    RegexOptions.IgnoreCase | RegexOptions.Multiline);

                foreach (Match match in matches)
                {
                    names.Add(match.Groups[1].Value.Trim());
                }
            }
            catch
            {
                // 실패 시 빈 리스트 반환
            }

            return names;
        }
    }
}
