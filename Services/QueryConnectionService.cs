using System;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using FACTOVA_QueryHelper.Database;
using FACTOVA_QueryHelper.Models;
using FACTOVA_QueryHelper.Controls; // 🔥 SharedDataContext를 위한 using 추가

namespace FACTOVA_QueryHelper.Services
{
    /// <summary>
    /// 쿼리 실행을 위한 연결 문자열 생성 및 쿼리 실행을 담당하는 서비스
    /// </summary>
    public class QueryConnectionService
    {
        private readonly SharedDataContext _sharedData;

        public QueryConnectionService(SharedDataContext sharedData)
        {
            _sharedData = sharedData ?? throw new ArgumentNullException(nameof(sharedData));
        }

        /// <summary>
        /// 연결 정보 결과
        /// </summary>
        public class ConnectionResult
        {
            public string ConnectionString { get; set; } = string.Empty;
            public string UserId { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
            public string ConnectionType { get; set; } = string.Empty; // "ConnectionInfo", "Host/Port/Service", "TNS"
        }

        /// <summary>
        /// QueryItem으로부터 연결 정보를 가져옵니다.
        /// </summary>
        public ConnectionResult GetConnectionInfo(QueryItem queryItem)
        {
            // 1순위: ConnectionInfoId
            if (queryItem.ConnectionInfoId.HasValue && queryItem.ConnectionInfoId.Value > 0)
            {
                return GetConnectionFromConnectionInfo(queryItem.ConnectionInfoId.Value);
            }

            // 2순위: Host/Port/ServiceName
            if (!string.IsNullOrWhiteSpace(queryItem.Host) &&
                !string.IsNullOrWhiteSpace(queryItem.Port) &&
                !string.IsNullOrWhiteSpace(queryItem.ServiceName))
            {
                return GetConnectionFromHostPort(queryItem);
            }

            // 3순위: TNS 이름
            if (!string.IsNullOrWhiteSpace(queryItem.TnsName))
            {
                return GetConnectionFromTns(queryItem.TnsName, queryItem.UserId, queryItem.Password);
            }

            throw new Exception("연결 정보가 없습니다. 쿼리에 TNS 또는 접속 정보를 설정해주세요.");
        }

        /// <summary>
        /// ConnectionInfoId로부터 연결 정보를 가져옵니다.
        /// </summary>
        private ConnectionResult GetConnectionFromConnectionInfo(int connectionInfoId)
        {
            var connectionInfoService = new ConnectionInfoService(_sharedData.Settings.DatabasePath);
            var allConnections = connectionInfoService.GetAll();
            var connectionInfo = allConnections.FirstOrDefault(c => c.Id == connectionInfoId);

            if (connectionInfo == null)
            {
                throw new Exception($"접속 정보 ID {connectionInfoId}를 찾을 수 없습니다.\n" +
                    "접속 정보 관리에서 해당 정보가 삭제되었을 수 있습니다.");
            }

            string connectionString;

            // TNS를 사용하는 경우
            if (!string.IsNullOrWhiteSpace(connectionInfo.TNS))
            {
                var selectedTns = _sharedData.TnsEntries.FirstOrDefault(t =>
                    t.Name.Equals(connectionInfo.TNS, StringComparison.OrdinalIgnoreCase));

                if (selectedTns == null)
                {
                    throw new Exception($"TNS '{connectionInfo.TNS}'를 찾을 수 없습니다.");
                }

                connectionString = selectedTns.GetConnectionString();
            }
            // Host/Port/Service를 직접 사용하는 경우
            else if (!string.IsNullOrWhiteSpace(connectionInfo.Host))
            {
                connectionString = $"Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST={connectionInfo.Host})(PORT={connectionInfo.Port}))(CONNECT_DATA=(SERVICE_NAME={connectionInfo.Service})));";
            }
            else
            {
                throw new Exception($"접속 정보 '{connectionInfo.Name}'에 TNS 또는 Host 정보가 없습니다.");
            }

            return new ConnectionResult
            {
                ConnectionString = connectionString,
                UserId = connectionInfo.UserId,
                Password = connectionInfo.Password,
                ConnectionType = "ConnectionInfo"
            };
        }

        /// <summary>
        /// Host/Port/ServiceName으로부터 연결 정보를 생성합니다.
        /// </summary>
        private ConnectionResult GetConnectionFromHostPort(QueryItem queryItem)
        {
            var connectionString = $"Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST={queryItem.Host})(PORT={queryItem.Port}))(CONNECT_DATA=(SERVICE_NAME={queryItem.ServiceName})));";

            return new ConnectionResult
            {
                ConnectionString = connectionString,
                UserId = queryItem.UserId,
                Password = queryItem.Password,
                ConnectionType = "Host/Port/Service"
            };
        }

        /// <summary>
        /// TNS 이름으로부터 연결 정보를 가져옵니다.
        /// </summary>
        private ConnectionResult GetConnectionFromTns(string tnsName, string userId, string password)
        {
            var selectedTns = _sharedData.TnsEntries.FirstOrDefault(t =>
                t.Name.Equals(tnsName, StringComparison.OrdinalIgnoreCase));

            if (selectedTns == null)
            {
                var availableTns = string.Join(", ", _sharedData.TnsEntries.Select(t => t.Name));
                throw new Exception($"TNS '{tnsName}'를 찾을 수 없습니다.\n\n" +
                    $"[해결 방법]\n" +
                    $"1. 쿼리 관리에서 접속 정보를 선택하세요\n" +
                    $"2. 또는 TNS 이름을 정확히 입력하세요\n" +
                    $"3. 또는 Host:Port:ServiceName 형식으로 입력하세요\n" +
                    $"   예) 192.168.1.10:1521:ORCL\n\n" +
                    $"사용 가능한 TNS 목록:\n{availableTns}\n\n" +
                    $"tnsnames.ora 파일 경로:\n{_sharedData.Settings.TnsPath}");
            }

            return new ConnectionResult
            {
                ConnectionString = selectedTns.GetConnectionString(),
                UserId = userId,
                Password = password,
                ConnectionType = "TNS"
            };
        }

        /// <summary>
        /// 쿼리를 실행하고 결과를 반환합니다.
        /// </summary>
        public async Task<DataTable> ExecuteQueryAsync(QueryItem queryItem, string processedQuery)
        {
            var connectionInfo = GetConnectionInfo(queryItem);

            // User ID와 Password 검증
            if (string.IsNullOrWhiteSpace(connectionInfo.UserId))
            {
                throw new Exception("User ID가 지정되지 않았습니다.");
            }

            if (string.IsNullOrWhiteSpace(connectionInfo.Password))
            {
                throw new Exception("Password가 지정되지 않았습니다.");
            }

            System.Diagnostics.Debug.WriteLine($"🔌 Connection Type: {connectionInfo.ConnectionType}");
            System.Diagnostics.Debug.WriteLine($"   User: {connectionInfo.UserId}");

            return await OracleDatabase.ExecuteQueryAsync(
                connectionInfo.ConnectionString,
                connectionInfo.UserId,
                connectionInfo.Password,
                processedQuery);
        }

        /// <summary>
        /// QueryItem의 연결 정보를 검증합니다.
        /// </summary>
        public bool ValidateQueryItem(QueryItem queryItem, out string errorMessage)
        {
            errorMessage = string.Empty;

            try
            {
                var connectionInfo = GetConnectionInfo(queryItem);

                if (string.IsNullOrWhiteSpace(connectionInfo.UserId))
                {
                    errorMessage = "User ID가 지정되지 않았습니다.";
                    return false;
                }

                if (string.IsNullOrWhiteSpace(connectionInfo.Password))
                {
                    errorMessage = "Password가 지정되지 않았습니다.";
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return false;
            }
        }
    }
}
