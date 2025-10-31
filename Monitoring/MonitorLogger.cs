using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;

namespace FACTOVA_QueryHelper
{
    public class MonitorLog
    {
        public DateTime Timestamp { get; set; }
        public string IpAddress { get; set; } = string.Empty;
        public string ProcessName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public LogLevel Level { get; set; }

        public string DisplayTime => Timestamp.ToString("yyyy-MM-dd HH:mm:ss");
        public string DisplayLevel => Level.ToString();
    }

    public enum LogLevel
    {
        Info,
        Warning,
        Error,
        Success
    }

    public class MonitorLogger
    {
        private static readonly string LogDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FACTOVA_QueryHelper",
            "Logs"
        );

        public static void WriteLog(MonitorLog log)
        {
            try
            {
                if (!Directory.Exists(LogDirectory))
                {
                    Directory.CreateDirectory(LogDirectory);
                }

                string logFileName = $"Monitor_{DateTime.Now:yyyyMMdd}.log";
                string logFilePath = Path.Combine(LogDirectory, logFileName);

                string logEntry = $"[{log.Timestamp:yyyy-MM-dd HH:mm:ss}] [{log.Level}] {log.IpAddress} - {log.ProcessName}: {log.Status} - {log.Message}";

                File.AppendAllText(logFilePath, logEntry + Environment.NewLine, Encoding.UTF8);
            }
            catch
            {
                // �α� ���� ���� ����
            }
        }

        public static void WriteLog(string ipAddress, string processName, string status, string message, LogLevel level)
        {
            var log = new MonitorLog
            {
                Timestamp = DateTime.Now,
                IpAddress = ipAddress,
                ProcessName = processName,
                Status = status,
                Message = message,
                Level = level
            };

            WriteLog(log);
        }

        public static ObservableCollection<MonitorLog> LoadTodayLogs()
        {
            var logs = new ObservableCollection<MonitorLog>();

            try
            {
                string logFileName = $"Monitor_{DateTime.Now:yyyyMMdd}.log";
                string logFilePath = Path.Combine(LogDirectory, logFileName);

                if (File.Exists(logFilePath))
                {
                    var lines = File.ReadAllLines(logFilePath, Encoding.UTF8);
                    foreach (var line in lines)
                    {
                        var log = ParseLogLine(line);
                        if (log != null)
                        {
                            logs.Add(log);
                        }
                    }
                }
            }
            catch
            {
                // �α� �ε� ���� ����
            }

            return logs;
        }

        private static MonitorLog? ParseLogLine(string line)
        {
            try
            {
                // �α� ����: [2025-01-22 10:30:45] [Info] 192.168.1.100 - process.exe: ���� �� - �޽���
                var parts = line.Split(new[] { "] [", "] ", " - " }, StringSplitOptions.None);
                if (parts.Length >= 5)
                {
                    var timestampStr = parts[0].Substring(1); // [ ����
                    var levelStr = parts[1];
                    var ipAddress = parts[2];
                    var processName = parts[3];
                    var remaining = parts.Length > 4 ? string.Join(" - ", parts.Skip(4)) : "";
                    var statusAndMessage = remaining.Split(new[] { " - " }, 2, StringSplitOptions.None);

                    return new MonitorLog
                    {
                        Timestamp = DateTime.Parse(timestampStr),
                        Level = Enum.Parse<LogLevel>(levelStr),
                        IpAddress = ipAddress,
                        ProcessName = processName,
                        Status = statusAndMessage.Length > 0 ? statusAndMessage[0] : "",
                        Message = statusAndMessage.Length > 1 ? statusAndMessage[1] : ""
                    };
                }
            }
            catch
            {
                // �Ľ� ���� ����
            }

            return null;
        }
    }
}
