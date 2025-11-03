using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using FACTOVA_QueryHelper.Database;

namespace FACTOVA_QueryHelper.SFC
{
    /// <summary>
    /// SFC 荑쇰━ 愿由?諛?寃곌낵 泥섎━瑜??대떦?섎뒗 ?대옒??
    /// </summary>
    public class SfcQueryManager
    {
        private readonly List<TnsEntry> _tnsEntries;

        public SfcQueryManager(List<TnsEntry> tnsEntries)
        {
            _tnsEntries = tnsEntries ?? throw new ArgumentNullException(nameof(tnsEntries));
        }

        /// <summary>
        /// TNS 占쏙옙트占쏙옙 占쏙옙占쏙옙占?占쏙옙占쏙옙占쏙옙트占쌌니댐옙.
        /// </summary>
        public void UpdateTnsEntries(List<TnsEntry> tnsEntries)
        {
            _tnsEntries.Clear();
            _tnsEntries.AddRange(tnsEntries);
        }

        /// <summary>
        /// SFC 占쏙옙占쏙옙占쏙옙 占쏙옙占쏙옙占싹곤옙 占쏙옙占쏙옙占?占쏙옙환占쌌니댐옙.
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
                throw new Exception($"TNS '{tnsName}'占쏙옙 찾占쏙옙 占쏙옙 占쏙옙占쏙옙占싹댐옙.");
            }

            string configDate = queryDate.ToString("yyyyMMdd");
            var ipList = string.Join(",", equipmentList.Select(e => $"'{e.IpAddress}'"));

            query = query.Replace("@CONFIG_REGISTER_YMD", configDate);
            query = query.Replace("@PC_IP_ADDR", ipList);

            return await OracleDatabase.ExecuteQueryAsync(
                selectedTns.ConnectionString,
                userId,
                password,
                query);
        }

        /// <summary>
        /// SFC 占쏙옙占쏙옙 占쏙옙占쏙옙占?처占쏙옙占싹울옙 占쏙옙占쏙옙 占쏙옙占승몌옙 占쏙옙占쏙옙占쏙옙트占쌌니댐옙.
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
        /// CONFIG_JSON占쏙옙占쏙옙 BIZACTOR占쏙옙 占쏙옙占쏙옙占쌌니댐옙.
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
                System.Diagnostics.Debug.WriteLine($"JSON 占식쏙옙 占쏙옙占쏙옙: {ex.Message}");
            }

            return "";
        }

        /// <summary>
        /// SFC 占쏙옙占쏙옙 占쏙옙占?占쏙옙占?
        /// </summary>
        public class QueryResultSummary
        {
            public int OnCount { get; set; }
            public int OffCount { get; set; }
            public int TotalCount { get; set; }

            public string GetSummaryMessage()
            {
                return $"占쏙옙회 占싹뤄옙 - ON: {OnCount}占쏙옙, OFF: {OffCount}占쏙옙";
            }
        }

        /// <summary>
        /// 占쏙옙占쏙옙 占쏙옙占?占쏙옙占쏙옙占?占쏙옙占쏙옙占쌌니댐옙.
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
