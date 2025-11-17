using Oracle.ManagedDataAccess.Client;
using System;
using System.Data;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FACTOVA_QueryHelper.Database
{
    public class OracleDatabase
    {
        public static async Task<DataTable> ExecuteQueryAsync(string connectionString, string userId, string password, string query)
        {
            var dataTable = new DataTable();

            try
            {
                await Task.Run(() =>
                {
                    // connectionString이 이미 "Data Source=..."로 시작하는지 확인
                    string fullConnectionString;
                    if (connectionString.StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase))
                    {
                        // 이미 "Data Source="가 있으면 User Id, Password만 추가
                        fullConnectionString = $"{connectionString}User Id={userId};Password={password};";
                    }
                    else
                    {
                        // TNS 형식인 경우 전체 연결 문자열 생성
                        fullConnectionString = $"Data Source={connectionString};User Id={userId};Password={password};";
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"Connection String: {fullConnectionString.Replace(password, "***")}");
                    
                    // 🔥 쿼리에 ROWNUM 제한이 없으면 자동으로 2000건 제한 추가
                    string processedQuery = ApplyRowLimitIfNeeded(query);
                    
                    if (processedQuery != query)
                    {
                        System.Diagnostics.Debug.WriteLine("⚠️ 쿼리에 ROWNUM 제한이 없어서 자동으로 2000건 제한을 추가했습니다.");
                    }
                    
                    // 🔍 실행될 전체 쿼리 로깅
                    System.Diagnostics.Debug.WriteLine("=== 실행될 쿼리 ===");
                    System.Diagnostics.Debug.WriteLine(processedQuery);
                    System.Diagnostics.Debug.WriteLine("==================");
                        
                    using var connection = new OracleConnection(fullConnectionString);
                    connection.Open();

                    using var command = new OracleCommand(processedQuery, connection);
                    command.CommandTimeout = 300; // 5분 타임아웃

                    using var adapter = new OracleDataAdapter(command);
                    adapter.Fill(dataTable);
                    
                    // 디버그: 반환된 컬럼명 확인
                    System.Diagnostics.Debug.WriteLine("=== Oracle에서 반환된 컬럼명 ===");
                    foreach (DataColumn col in dataTable.Columns)
                    {
                        System.Diagnostics.Debug.WriteLine($"  - {col.ColumnName}");
                    }
                });
            }
            catch (OracleException ex)
            {
                // Oracle 특정 오류 처리
                if (ex.Number == 1017) // ORA-01017: invalid username/password; logon denied
                {
                    throw new Exception("Oracle 연결 실패: 사용자 ID 또는 비밀번호가 올바르지 않습니다.", ex);
                }
                else if (ex.Number == 12154) // ORA-12154: TNS:could not resolve the connect identifier specified
                {
                    throw new Exception("Oracle 연결 실패: TNS 이름을 찾을 수 없습니다.", ex);
                }
                else if (ex.Number == 12514) // ORA-12514: TNS:listener does not currently know of service requested
                {
                    throw new Exception("Oracle 연결 실패: 서비스를 찾을 수 없습니다.", ex);
                }
                else if (ex.Number == 12541) // ORA-12541: TNS:no listener
                {
                    throw new Exception("Oracle 연결 실패: 리스너가 응답하지 않습니다.", ex);
                }
                else
                {
                    throw new Exception($"Oracle 오류 (ORA-{ex.Number:D5}): {ex.Message}", ex);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"데이터베이스 연결 또는 쿼리 실행 중 오류가 발생했습니다: {ex.Message}", ex);
            }

            return dataTable;
        }

        /// <summary>
        /// 쿼리에 ROWNUM 또는 RN 제한이 없으면 자동으로 2000건 제한을 추가합니다.
        /// </summary>
        private static string ApplyRowLimitIfNeeded(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return query;

            // 🔥 ROWNUM, RN, ROW_NUMBER(), FETCH FIRST, OFFSET, LIMIT 등 행 제한 키워드가 있는지 확인 (대소문자 무시)
            bool hasRowLimit = Regex.IsMatch(query, 
                @"\b(ROWNUM|RN|ROW_NUMBER|FETCH\s+FIRST|OFFSET|LIMIT)\b", 
                RegexOptions.IgnoreCase);

            if (hasRowLimit)
            {
                // 이미 행 제한이 있으면 그대로 반환
                return query;
            }

            // 🔥 행 제한이 없으면 2000건 제한 추가
            // WHERE 절이 있는지 확인하여 적절한 위치에 ROWNUM 조건 추가
            string trimmedQuery = query.Trim();
            
            // ORDER BY 절이 있는지 확인
            Match orderByMatch = Regex.Match(trimmedQuery, @"\bORDER\s+BY\b", RegexOptions.IgnoreCase);
            
            if (orderByMatch.Success)
            {
                // ORDER BY가 있으면 그 앞에 WHERE ROWNUM <= 2000 추가
                int orderByIndex = orderByMatch.Index;
                string beforeOrderBy = trimmedQuery.Substring(0, orderByIndex).TrimEnd();
                string orderByPart = trimmedQuery.Substring(orderByIndex);
                
                // WHERE 절이 이미 있는지 확인
                bool hasWhere = Regex.IsMatch(beforeOrderBy, @"\bWHERE\b", RegexOptions.IgnoreCase);
                
                if (hasWhere)
                {
                    return $"{beforeOrderBy}\n  AND ROWNUM <= 2000\n{orderByPart}";
                }
                else
                {
                    return $"{beforeOrderBy}\nWHERE ROWNUM <= 2000\n{orderByPart}";
                }
            }
            else
            {
                // ORDER BY가 없으면 마지막에 WHERE ROWNUM <= 2000 추가
                bool hasWhere = Regex.IsMatch(trimmedQuery, @"\bWHERE\b", RegexOptions.IgnoreCase);
                
                if (hasWhere)
                {
                    return $"{trimmedQuery}\n  AND ROWNUM <= 2000";
                }
                else
                {
                    return $"{trimmedQuery}\nWHERE ROWNUM <= 2000";
                }
            }
        }

        public static async Task<bool> TestConnectionAsync(string connectionString, string userId, string password)
        {
            try
            {
                await Task.Run(() =>
                {
                    // connectionString이 이미 "Data Source=..."로 시작하는지 확인
                    string fullConnectionString;
                    if (connectionString.StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase))
                    {
                        fullConnectionString = $"{connectionString}User Id={userId};Password={password};";
                    }
                    else
                    {
                        fullConnectionString = $"Data Source={connectionString};User Id={userId};Password={password};";
                    }

                    using var connection = new OracleConnection(fullConnectionString);
                    connection.Open();
                    connection.Close();
                });

                return true;
            }
            catch (OracleException ex)
            {
                // 디버그용 로깅 (운영에서는 제외)
                System.Diagnostics.Debug.WriteLine($"Oracle 연결 테스트 실패 (ORA-{ex.Number:D5}): {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                // 일반 오류 로깅
                System.Diagnostics.Debug.WriteLine($"연결 테스트 실패: {ex.Message}");
                return false;
            }
        }
    }
}
