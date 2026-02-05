using System;
using System.IO;
using System.Text.Json;

namespace FACTOVA_QueryHelper.Services
{
    /// <summary>
    /// 빌드 시에도 유지되는 사용자 설정을 관리하는 서비스
    /// AppData\Roaming\FACTOVA_QueryHelper\settings.json 에 저장
    /// </summary>
    public class UserSettingsService
    {
        private static readonly string SettingsDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "FACTOVA_QueryHelper");

        private static readonly string SettingsFilePath = Path.Combine(
            SettingsDirectory,
            "settings.json");

        /// <summary>
        /// 사용자 설정 데이터
        /// </summary>
        public class UserSettings
        {
            public string TnsFilePath { get; set; } = string.Empty;
            public string LastTns { get; set; } = string.Empty;
            public string LastUserId { get; set; } = string.Empty;
            public string LastPassword { get; set; } = string.Empty;
        }

        /// <summary>
        /// 설정 로드
        /// </summary>
        public static UserSettings Load()
        {
            try
            {
                if (!File.Exists(SettingsFilePath))
                {
return new UserSettings();
                }

                var json = File.ReadAllText(SettingsFilePath);
                var settings = JsonSerializer.Deserialize<UserSettings>(json);


                return settings ?? new UserSettings();
            }
            catch (Exception ex)
            {
return new UserSettings();
            }
        }

        /// <summary>
        /// 설정 저장
        /// </summary>
        public static void Save(UserSettings settings)
        {
            try
            {
                // 디렉토리가 없으면 생성
                if (!Directory.Exists(SettingsDirectory))
                {
                    Directory.CreateDirectory(SettingsDirectory);
}

                var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                File.WriteAllText(SettingsFilePath, json);

            }
            catch (Exception ex)
            {
throw;
            }
        }

        /// <summary>
        /// 설정 파일 경로 반환 (디버깅용)
        /// </summary>
        public static string GetSettingsFilePath()
        {
            return SettingsFilePath;
        }
    }
}
