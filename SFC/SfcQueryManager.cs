using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using FACTOVA_QueryHelper.Database;
using FACTOVA_QueryHelper.Models;

namespace FACTOVA_QueryHelper.SFC
{
    /// <summary>
    /// SFC 쿼리 관리 및 결과 처리를 담당하는 클래스
    /// </summary>
    public class SfcQueryManager
    {
        private readonly List<TnsEntry> _tnsEntries;

        public SfcQueryManager(List<TnsEntry> tnsEntries)
        {
            _tnsEntries = tnsEntries ?? throw new ArgumentNullException(nameof(tnsEntries));
        }

        /// <summary>
        /// TNS 엔트리 목록을 업데이트합니다.
        /// </summary>
        public void UpdateTnsEntries(List<TnsEntry> tnsEntries)
        {
            _tnsEntries.Clear();
            _tnsEntries.AddRange(tnsEntries);
        }

        /// <summary>
        /// SFC 쿼리를 실행하고 결과를 반환합니다.
        /// </summary>
        public async Task<DataTable?> ExecuteQueryAsync(
            string tnsName,
            string userId,
            string password,
            string query,
            DateTime queryDate,
            List<SfcEquipmentInfo> equipmentList)
        {
            var selectedTns = _tnsEntries.FirstOrDefault(t => t.Name == tnsName);

            if (selectedTns == null)
            {
                throw new Exception($"TNS '{tnsName}'을 찾을 수 없습니다.");
            }

            string configDate = queryDate.ToString("yyyyMMdd");
            var ipList = string.Join(",", equipmentList.Select(e => $"'{e.IpAddress}'"));

            query = query.Replace("@CONFIG_REGISTER_YMD", configDate);
            query = query.Replace("@PC_IP_ADDR", ipList);

            return await OracleDatabase.ExecuteQueryAsync(
                selectedTns.GetConnectionString(),
                userId,
                password,
                query);
        }

        /// <summary>
        /// SFC 쿼리 결과를 처리하여 장비 상태 정보를 업데이트합니다.
        /// </summary>
        public void ProcessQueryResult(DataTable result, List<SfcEquipmentInfo> equipmentList)
        {
            var registeredData = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (DataRow row in result.Rows)
            {
                if (row["PC_IP_ADDR"] != null && row["PC_IP_ADDR"] != DBNull.Value)
                {
                    string ip = row["PC_IP_ADDR"].ToString()?.Trim() ?? "";
                    string configJson = row["CONFIG_JSON"]?.ToString() ?? "";
                    registeredData[ip] = configJson;
                }
            }

            foreach (var equipment in equipmentList)
            {
                if (registeredData.ContainsKey(equipment.IpAddress))
                {
                    equipment.Status = "ON";
                    equipment.BizActor = ExtractBizActor(registeredData[equipment.IpAddress]);
                }
                else
                {
                    equipment.Status = "OFF";
                    equipment.BizActor = "";
                }
            }
        }

        /// <summary>
        /// CONFIG_JSON에서 BIZACTOR를 추출합니다.
        /// </summary>
        private string ExtractBizActor(string configJson)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(configJson))
                    return "";

                using (JsonDocument doc = JsonDocument.Parse(configJson))
                {
                    if (doc.RootElement.TryGetProperty("MWCONFIG_INFO", out JsonElement mwConfig))
                    {
                        if (mwConfig.TryGetProperty("SQL_QUEUE", out JsonElement sqlQueue))
                        {
                            string sqlQueueValue = sqlQueue.GetString() ?? "";

                            if (sqlQueueValue.Contains("BIZACTOR_"))
                            {
                                int startIndex = sqlQueueValue.IndexOf("BIZACTOR_") + 9;
                                int endIndex = sqlQueueValue.IndexOf("/", startIndex);

                                if (endIndex > startIndex)
                                {
                                    return sqlQueueValue.Substring(startIndex, endIndex - startIndex);
                                }
                                else
                                {
                                    return sqlQueueValue.Substring(startIndex);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
}

            return "";
        }

        /// <summary>
        /// SFC 쿼리 결과 요약
        /// </summary>
        public class QueryResultSummary
        {
            public int OnCount { get; set; }
            public int OffCount { get; set; }
            public int TotalCount { get; set; }

            public string GetSummaryMessage()
            {
                return $"조회 결과 - ON: {OnCount}개, OFF: {OffCount}개";
            }
        }

        /// <summary>
        /// 쿼리 결과 요약을 생성합니다.
        /// </summary>
        public static QueryResultSummary GetResultSummary(List<SfcEquipmentInfo> equipmentList)
        {
            return new QueryResultSummary
            {
                OnCount = equipmentList.Count(e => e.Status == "ON"),
                OffCount = equipmentList.Count(e => e.Status == "OFF"),
                TotalCount = equipmentList.Count
            };
        }
    }
}
