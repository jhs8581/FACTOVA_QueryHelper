using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace FACTOVA_QueryHelper
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
        public string SfcTnsName { get; set; } = string.Empty; // SFC TNS 엔트리 이름
        public bool StopOnNotification { get; set; } = true; // 알림 시 자동 실행 중지 (기본값: true)
        public bool EnableAutoExecution { get; set; } = false; // 자동 실행 활성화 (기본값: false)
        public int FontSize { get; set; } = 11; // 폰트 크기 (기본값: 11)
        public string DatabasePath { get; set; } = string.Empty; // DB 파일 경로 (기본값: 빈 문자열)
        public bool CheckUpdateOnStartup { get; set; } = true; // 시작 시 업데이트 확인 (기본값: true)
    }

    public class SettingsManager
    {
        private const string SettingsFileName = "settings.json";
        private static readonly string SettingsFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FACTOVA_QueryHelper",
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
