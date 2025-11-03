using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Net.NetworkInformation;
using System.Threading.Tasks;

namespace FACTOVA_QueryHelper
{
    public class MonitorTarget
    {
        public string IpAddress { get; set; } = string.Empty;
        public string ProcessName { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool IsEnabled { get; set; } = true;
    }

    public class ProcessInfo
    {
        public string ProcessName { get; set; } = string.Empty;
        public int ProcessId { get; set; }
        public string ExecutablePath { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public long WorkingSetSize { get; set; } // �޸� ��뷮 (bytes)
    }

    public class MonitorResult
    {
        public string IpAddress { get; set; } = string.Empty;
        public string ProcessName { get; set; } = string.Empty;
        public bool IsRunning { get; set; }
        public DateTime CheckTime { get; set; }
        public string Status { get; set; } = string.Empty;
        public List<ProcessInfo> Processes { get; set; } = new List<ProcessInfo>();
        public string ErrorMessage { get; set; } = string.Empty;
    }

    public class ProcessMonitor
    {
        public static async Task<bool> CheckHostAvailability(string ipAddress)
        {
            return await Task.Run(() =>
            {
                try
                {
                    using (var ping = new Ping())
                    {
                        var reply = ping.Send(ipAddress, 1000);
                        return reply.Status == IPStatus.Success;
                    }
                }
                catch
                {
                    return false;
                }
            });
        }

        public static async Task<MonitorResult> CheckProcessAsync(MonitorTarget target)
        {
            var result = new MonitorResult
            {
                IpAddress = target.IpAddress,
                ProcessName = target.ProcessName,
                CheckTime = DateTime.Now
            };

            try
            {
                // ���� ȣ��Ʈ�� ����ִ��� Ȯ��
                bool isHostAlive = await CheckHostAvailability(target.IpAddress);
                if (!isHostAlive)
                {
                    result.IsRunning = false;
                    result.Status = "ȣ��Ʈ ���� �Ұ�";
                    result.ErrorMessage = "ȣ��Ʈ�� ������ �� �����ϴ�. ��Ʈ��ũ ������ Ȯ���ϼ���.";
                    return result;
                }

                // WMI�� ���� ���� ���μ��� Ȯ��
                var processes = await GetRemoteProcessesAsync(target);
                result.Processes = processes;

                if (processes.Count > 0)
                {
                    result.IsRunning = true;
                    result.Status = $"���� �� ({processes.Count}�� ���μ���)";
                }
                else
                {
                    result.IsRunning = false;
                    result.Status = "���� ���� �ƴ�";
                }
            }
            catch (UnauthorizedAccessException)
            {
                result.IsRunning = false;
                result.Status = "���� ���� ����";
                result.ErrorMessage = "��� �ý��ۿ� ������ ������ �����ϴ�. ������ ������ Ȯ���ϼ���.";
            }
            catch (Exception ex)
            {
                result.IsRunning = false;
                result.Status = "���� �߻�";
                result.ErrorMessage = ex.Message;
            }

            return result;
        }

        private static async Task<List<ProcessInfo>> GetRemoteProcessesAsync(MonitorTarget target)
        {
            return await Task.Run(() =>
            {
                var processes = new List<ProcessInfo>();

                try
                {
                    ConnectionOptions options = new ConnectionOptions();
                    if (!string.IsNullOrEmpty(target.Username) && !string.IsNullOrEmpty(target.Password))
                    {
                        options.Username = target.Username;
                        options.Password = target.Password;
                        options.Authority = $"ntlmdomain:{target.IpAddress}";
                    }
                    options.Timeout = TimeSpan.FromSeconds(5);

                    ManagementScope scope = new ManagementScope($"\\\\{target.IpAddress}\\root\\cimv2", options);
                    scope.Connect();

                    // ���μ��������� .exe Ȯ���� ����
                    string processNameWithoutExt = target.ProcessName.Replace(".exe", "");

                    ObjectQuery query = new ObjectQuery($"SELECT * FROM Win32_Process WHERE Name = '{target.ProcessName}'");
                    ManagementObjectSearcher searcher = new ManagementObjectSearcher(scope, query);

                    foreach (ManagementObject obj in searcher.Get())
                    {
                        try
                        {
                            var processInfo = new ProcessInfo
                            {
                                ProcessName = obj["Name"]?.ToString() ?? "",
                                ProcessId = Convert.ToInt32(obj["ProcessId"]),
                                ExecutablePath = obj["ExecutablePath"]?.ToString() ?? "",
                                WorkingSetSize = Convert.ToInt64(obj["WorkingSetSize"] ?? 0)
                            };

                            // CreationDate �Ľ�
                            string creationDate = obj["CreationDate"]?.ToString() ?? "";
                            if (!string.IsNullOrEmpty(creationDate))
                            {
                                processInfo.StartTime = ManagementDateTimeConverter.ToDateTime(creationDate);
                            }

                            processes.Add(processInfo);
                        }
                        catch
                        {
                            // ���� ���μ��� ���� �Ľ� ���� �� ����
                        }
                    }
                }
                catch
                {
                    throw;
                }

                return processes;
            });
        }

        public static string FormatBytes(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }
    }
}
