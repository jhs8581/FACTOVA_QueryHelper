using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace FACTOVA_Palletizing_Analysis
{
    public class AppSettings
    {
        public string TnsPath { get; set; } = string.Empty;
        public List<MonitorTarget> MonitorTargets { get; set; } = new List<MonitorTarget>();
        public int MonitorIntervalSeconds { get; set; } = 30;
        public bool EnableNotifications { get; set; } = true;
        public string ExcelFilePath { get; set; } = string.Empty;
        public int QueryIntervalSeconds { get; set; } = 60;
        public string SfcExcelFilePath { get; set; } = string.Empty;
        public string SfcUserId { get; set; } = string.Empty;
        public string SfcPassword { get; set; } = string.Empty;
        public string SfcTnsName { get; set; } = string.Empty; // SFC TNS 선택값 저장
    }

    public class SettingsManager
    {
        private const string SettingsFileName = "settings.json";
        private static readonly string SettingsFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FACTOVA_Palletizing_Analysis",
            SettingsFileName
        );

        public static string GetDefaultTnsPath()
        {
            string userName = Environment.UserName;
            string defaultPath = $@"C:\app\client\{userName}\product\19.0.0\client_1\network\admin\tnsnames.ora";
            return defaultPath;
        }

        public static AppSettings LoadSettings()
        {
            try
            {
                if (File.Exists(SettingsFilePath))
                {
                    string json = File.ReadAllText(SettingsFilePath);
                    var settings = JsonSerializer.Deserialize<AppSettings>(json);
                    if (settings != null)
                        return settings;
                }
            }
            catch
            {
                // 로드 실패 시 기본 설정 반환
            }

            return new AppSettings { TnsPath = GetDefaultTnsPath() };
        }

        public static void SaveSettings(AppSettings settings)
        {
            try
            {
                string directory = Path.GetDirectoryName(SettingsFilePath) ?? "";
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions 
                { 
                    WriteIndented = true 
                });
                File.WriteAllText(SettingsFilePath, json);
            }
            catch
            {
                // 저장 실패 무시
            }
        }
    }
}
