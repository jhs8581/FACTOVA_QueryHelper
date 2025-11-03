using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace FACTOVA_QueryHelper
{
    public class TnsEntry
    {
        public string Name { get; set; } = string.Empty;
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; }
        public string ServiceName { get; set; } = string.Empty;
        public string ConnectionString { get; set; } = string.Empty;

        public override string ToString() => Name;
    }

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
                // UTF-8, UTF-8 BOM, ANSI 占쏙옙 占쏙옙占쏙옙 占쏙옙占쌘듸옙占쏙옙占쏙옙 占시듸옙
                string content = ReadFileWithEncoding(filePath);
                
                // TNS 占쏙옙트占쏙옙 占식쏙옙 (占싱몌옙 = 占쏙옙占쏙옙 占쏙옙占쏙옙占싹댐옙 占쏙옙 占쏙옙占쏙옙)
                // 占쏙옙占쏙옙, 占쏙옙 占쏙옙占쏙옙 占쏙옙占쏙옙構占?占쏙옙拈占쏙옙占?占쏙옙占쏙옙 占쏙옙占쏙옙 占쏙옙칭
                var matches = Regex.Matches(content, @"^[\s]*([^\s=]+)[\s]*=[\s]*\(DESCRIPTION", 
                    RegexOptions.IgnoreCase | RegexOptions.Multiline);

                foreach (Match match in matches)
                {
                    string tnsName = match.Groups[1].Value.Trim();
                    
                    // 占쌔댐옙 TNS 占쏙옙占쏙옙 占쏙옙占쏙옙
                    int startIndex = match.Index;
                    int endIndex = FindMatchingParenthesis(content, startIndex);
                    
                    if (endIndex > startIndex)
                    {
                        string block = content.Substring(startIndex, endIndex - startIndex + 1);
                        
                        var entry = new TnsEntry { Name = tnsName };
                        
                        // HOST 占쏙옙占쏙옙
                        var hostMatch = Regex.Match(block, @"HOST[\s]*=[\s]*([^\)]+)", RegexOptions.IgnoreCase);
                        if (hostMatch.Success)
                        {
                            entry.Host = hostMatch.Groups[1].Value.Trim();
                        }
                        
                        // PORT 占쏙옙占쏙옙
                        var portMatch = Regex.Match(block, @"PORT[\s]*=[\s]*(\d+)", RegexOptions.IgnoreCase);
                        if (portMatch.Success)
                        {
                            int.TryParse(portMatch.Groups[1].Value, out int port);
                            entry.Port = port;
                        }
                        
                        // SERVICE_NAME 占실댐옙 SID 占쏙옙占쏙옙
                        var serviceMatch = Regex.Match(block, @"SERVICE_NAME[\s]*=[\s]*([^\)]+)", RegexOptions.IgnoreCase);
                        if (serviceMatch.Success)
                        {
                            entry.ServiceName = serviceMatch.Groups[1].Value.Trim();
                        }
                        else
                        {
                            // SID占싸듸옙 占시듸옙
                            var sidMatch = Regex.Match(block, @"SID[\s]*=[\s]*([^\)]+)", RegexOptions.IgnoreCase);
                            if (sidMatch.Success)
                            {
                                entry.ServiceName = sidMatch.Groups[1].Value.Trim();
                            }
                        }
                        
                        // 占쏙옙占쏙옙 占쏙옙占쌘울옙 占쏙옙占쏙옙
                        entry.ConnectionString = $"Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST={entry.Host})(PORT={entry.Port}))(CONNECT_DATA=(SERVICE_NAME={entry.ServiceName})));";
                        
                        entries.Add(entry);
                    }
                }
            }
            catch (Exception)
            {
                // 占식쏙옙 占쏙옙占쏙옙 占쏙옙 占쏙옙 占쏙옙占쏙옙트 占쏙옙환
            }

            return entries;
        }

        private static string ReadFileWithEncoding(string filePath)
        {
            try
            {
                // 占쏙옙占쏙옙 UTF-8占쏙옙 占시듸옙
                return File.ReadAllText(filePath, System.Text.Encoding.UTF8);
            }
            catch
            {
                try
                {
                    // ANSI (Default)占쏙옙 占시듸옙
                    return File.ReadAllText(filePath, System.Text.Encoding.Default);
                }
                catch
                {
                    // ASCII占쏙옙 占시듸옙
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
        /// TNS 占쏙옙占쏙옙占쏙옙 占쏙옙占?占쏙옙트占쏙옙 占싱몌옙占쏙옙 占쏙옙占쏙옙 (占쏙옙占쏙옙占쏙옙)
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
                // 占쏙옙占쏙옙 占쏙옙 占쏙옙 占쏙옙占쏙옙트 占쏙옙환
            }

            return names;
        }
    }
}
