using Oracle.ManagedDataAccess.Client;
using System;
using System.Data;
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
                    string fullConnectionString = $"{connectionString}User Id={userId};Password={password};";
                        
                    using var connection = new OracleConnection(fullConnectionString);
                    connection.Open();

                    using var command = new OracleCommand(query, connection);
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

        public static async Task<bool> TestConnectionAsync(string connectionString, string userId, string password)
        {
            try
            {
                await Task.Run(() =>
                {
                    string fullConnectionString = $"{connectionString}User Id={userId};Password={password};";

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
