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
        public bool CheckUpdateOnStartup { get; set; } = false; // 시작 시 업데이트 확인 (기본값: false로 변경!)
        public string UpdateSource { get; set; } = "GitHub"; // 업데이트 소스: "GitHub" 또는 "Network"
        public string NetworkUpdatePath { get; set; } = @"\\서버\FACTOVA_Apps\QueryHelper\latest"; // 네트워크 업데이트 경로
        
        // GMES 정보 조회 입력값
        public string GmesFactory { get; set; } = string.Empty; // 대표공장
        public string GmesOrg { get; set; } = string.Empty; // ORG
        public DateTime? GmesDateFrom { get; set; } = null; // 조회 시작 날짜
        public DateTime? GmesDateTo { get; set; } = null; // 조회 종료 날짜
        public string GmesWipLineId { get; set; } = string.Empty; // 윕라인 ID
        public string GmesEquipLineId { get; set; } = string.Empty; // 설비라인 ID
        public string GmesFacility { get; set; } = string.Empty; // FACILITY
        public string GmesWorkOrder { get; set; } = string.Empty; // W/O
        public string GmesWorkOrderName { get; set; } = string.Empty; // W/O 명
        
        // GMES 그리드별 선택된 쿼리 (최대 20개)
        public int GmesGridCount { get; set; } = 6; // 그리드 개수 (기본값: 6)
        public string GmesPlanQueryName { get; set; } = string.Empty; // 계획정보 쿼리
        public string GmesGrid1QueryName { get; set; } = string.Empty; // 그리드 1 쿼리
        public string GmesGrid2QueryName { get; set; } = string.Empty; // 그리드 2 쿼리
        public string GmesGrid3QueryName { get; set; } = string.Empty; // 그리드 3 쿼리
        public string GmesGrid4QueryName { get; set; } = string.Empty; // 그리드 4 쿼리
        public string GmesGrid5QueryName { get; set; } = string.Empty; // 그리드 5 쿼리
        public string GmesGrid6QueryName { get; set; } = string.Empty; // 그리드 6 쿼리
        public string GmesGrid7QueryName { get; set; } = string.Empty; // 그리드 7 쿼리
        public string GmesGrid8QueryName { get; set; } = string.Empty; // 그리드 8 쿼리
        public string GmesGrid9QueryName { get; set; } = string.Empty; // 그리드 9 쿼리
        public string GmesGrid10QueryName { get; set; } = string.Empty; // 그리드 10 쿼리
        public string GmesGrid11QueryName { get; set; } = string.Empty; // 그리드 11 쿼리
        public string GmesGrid12QueryName { get; set; } = string.Empty; // 그리드 12 쿼리
        public string GmesGrid13QueryName { get; set; } = string.Empty; // 그리드 13 쿼리
        public string GmesGrid14QueryName { get; set; } = string.Empty; // 그리드 14 쿼리
        public string GmesGrid15QueryName { get; set; } = string.Empty; // 그리드 15 쿼리
        public string GmesGrid16QueryName { get; set; } = string.Empty; // 그리드 16 쿼리
        public string GmesGrid17QueryName { get; set; } = string.Empty; // 그리드 17 쿼리
        public string GmesGrid18QueryName { get; set; } = string.Empty; // 그리드 18 쿼리
        public string GmesGrid19QueryName { get; set; } = string.Empty; // 그리드 19 쿼리
        public string GmesGrid20QueryName { get; set; } = string.Empty; // 그리드 20 쿼리
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
