using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace FACTOVA_Palletizing_Analysis
{
    /// <summary>
    /// SFC ���� ���� �� ��� ó���� ����ϴ� Ŭ����
    /// </summary>
    public class SfcQueryManager
    {
        private readonly List<TnsEntry> _tnsEntries;

        public SfcQueryManager(List<TnsEntry> tnsEntries)
        {
            _tnsEntries = tnsEntries ?? throw new ArgumentNullException(nameof(tnsEntries));
        }

        /// <summary>
        /// TNS ��Ʈ�� ����� ������Ʈ�մϴ�.
        /// </summary>
        public void UpdateTnsEntries(List<TnsEntry> tnsEntries)
        {
            _tnsEntries.Clear();
            _tnsEntries.AddRange(tnsEntries);
        }

        /// <summary>
        /// SFC ������ �����ϰ� ����� ��ȯ�մϴ�.
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
                throw new Exception($"TNS '{tnsName}'�� ã�� �� �����ϴ�.");
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
        /// SFC ���� ����� ó���Ͽ� ���� ���¸� ������Ʈ�մϴ�.
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
        /// CONFIG_JSON���� BIZACTOR�� �����մϴ�.
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
                System.Diagnostics.Debug.WriteLine($"JSON �Ľ� ����: {ex.Message}");
            }

            return "";
        }

        /// <summary>
        /// SFC ���� ��� ���
        /// </summary>
        public class QueryResultSummary
        {
            public int OnCount { get; set; }
            public int OffCount { get; set; }
            public int TotalCount { get; set; }

            public string GetSummaryMessage()
            {
                return $"��ȸ �Ϸ� - ON: {OnCount}��, OFF: {OffCount}��";
            }
        }

        /// <summary>
        /// ���� ��� ����� �����մϴ�.
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
