using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Oracle.ManagedDataAccess.Client;
using FACTOVA_QueryHelper.Models;

namespace FACTOVA_QueryHelper.Services
{
    public class OracleDbService : IDisposable
    {
        private string? _connectionString;
        private bool _isConfigured = false;
        private CancellationTokenSource? _currentQueryCancellation;
        private OracleCommand? _currentCommand;

        /// <summary>
        /// 현재 연결 설정 상태
        /// </summary>
        public bool IsConfigured => _isConfigured && !string.IsNullOrEmpty(_connectionString);

        /// <summary>
        /// 쿼리 실행 중 여부
        /// </summary>
        public bool IsQueryRunning => _currentQueryCancellation != null && !_currentQueryCancellation.IsCancellationRequested;

        /// <summary>
        /// TnsEntry 객체를 사용한 연결 설정
        /// </summary>
        public async Task<bool> ConfigureAsync(TnsEntry tnsEntry, string userId, string password)
        {
            try
            {
                string dataSource = tnsEntry.GetConnectionString();
                _connectionString = $"Data Source={dataSource};User Id={userId};Password={password};";
                
                System.Diagnostics.Debug.WriteLine($"=== Connection Configuration ===");
                System.Diagnostics.Debug.WriteLine($"TNS Name: {tnsEntry.Name}");
                System.Diagnostics.Debug.WriteLine($"Host: {tnsEntry.Host}");
                System.Diagnostics.Debug.WriteLine($"Port: {tnsEntry.Port}");
                System.Diagnostics.Debug.WriteLine($"Service: {tnsEntry.ServiceName}");
                System.Diagnostics.Debug.WriteLine($"User: {userId}");
                System.Diagnostics.Debug.WriteLine($"Connection String: {_connectionString.Replace(password, "***")}");
                
                // 연결 테스트
                using var connection = new OracleConnection(_connectionString);
                await connection.OpenAsync();
                // using 블록 종료 시 자동으로 연결 닫힘
                
                _isConfigured = true;
                
                System.Diagnostics.Debug.WriteLine("✅ Connection configured and tested successfully!");
                System.Diagnostics.Debug.WriteLine("🔌 Connection closed (will reconnect when needed)");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Configuration Error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                }
                _connectionString = null;
                _isConfigured = false;
                throw;
            }
        }

        /// <summary>
        /// 문자열을 사용한 연결 설정 (수동 입력용)
        /// </summary>
        public async Task<bool> ConfigureAsync(string tnsString, string userId, string password)
        {
            try
            {
                string connectionString;
                
                if (tnsString.Contains("DESCRIPTION") || tnsString.Contains("("))
                {
                    connectionString = $"Data Source={tnsString};User Id={userId};Password={password};";
                }
                else
                {
                    connectionString = $"Data Source={tnsString};User Id={userId};Password={password};";
                }
                
                _connectionString = connectionString;
                
                System.Diagnostics.Debug.WriteLine($"Configuring connection with manual TNS: {connectionString.Replace(password, "***")}");
                
                // 연결 테스트
                using var connection = new OracleConnection(_connectionString);
                await connection.OpenAsync();
                // using 블록 종료 시 자동으로 연결 닫힘
                
                _isConfigured = true;
                
                System.Diagnostics.Debug.WriteLine("✅ Connection configured and tested successfully!");
                System.Diagnostics.Debug.WriteLine("🔌 Connection closed (will reconnect when needed)");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Configuration Error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                }
                _connectionString = null;
                _isConfigured = false;
                throw;
            }
        }

        /// <summary>
        /// 필요할 때만 임시 연결을 생성
        /// </summary>
        private async Task<OracleConnection> CreateConnectionAsync()
        {
            if (!IsConfigured)
            {
                throw new InvalidOperationException("Database not configured. Call ConfigureAsync first.");
            }

            var sw = System.Diagnostics.Stopwatch.StartNew();
            System.Diagnostics.Debug.WriteLine("🔌 Creating temporary connection...");
            
            var connection = new OracleConnection(_connectionString);
            await connection.OpenAsync();
            System.Diagnostics.Debug.WriteLine($"  ⏱️ Connection opened: {sw.ElapsedMilliseconds}ms");
            
            // 🔥 NLS 설정을 비동기로 실행
            await Task.Run(() => SetSessionNlsSettings(connection));
            System.Diagnostics.Debug.WriteLine($"  ⏱️ NLS settings applied: {sw.ElapsedMilliseconds}ms");
            
            System.Diagnostics.Debug.WriteLine($"✅ Temporary connection opened (total: {sw.ElapsedMilliseconds}ms)");
            return connection;
        }

        /// <summary>
        /// 🔥 Oracle 세션의 NLS 설정을 통일 (OracleDatabase.cs와 동일)
        /// </summary>
        private void SetSessionNlsSettings(OracleConnection connection)
        {
            try
            {
                using var command = connection.CreateCommand();
                
                // 🔥 모든 세션에서 동일한 날짜 형식, 언어, 지역 설정 적용
                command.CommandText = @"
                    BEGIN
                        EXECUTE IMMEDIATE 'ALTER SESSION SET NLS_DATE_FORMAT = ''YYYY-MM-DD HH24:MI:SS''';
                        EXECUTE IMMEDIATE 'ALTER SESSION SET NLS_TIMESTAMP_FORMAT = ''YYYY-MM-DD HH24:MI:SS.FF''';
                        EXECUTE IMMEDIATE 'ALTER SESSION SET NLS_LANGUAGE = ''AMERICAN''';
                        EXECUTE IMMEDIATE 'ALTER SESSION SET NLS_TERRITORY = ''AMERICA''';
                        EXECUTE IMMEDIATE 'ALTER SESSION SET NLS_NUMERIC_CHARACTERS = ''.,''';
                        EXECUTE IMMEDIATE 'ALTER SESSION SET NLS_SORT = ''BINARY''';
                        EXECUTE IMMEDIATE 'ALTER SESSION SET NLS_COMP = ''BINARY''';
                    END;
                ";
                
                command.ExecuteNonQuery();
                
                System.Diagnostics.Debug.WriteLine("✅ NLS settings applied to session");
            }
            catch (Exception ex)
            {
                // NLS 설정 실패는 경고만 하고 계속 진행
                System.Diagnostics.Debug.WriteLine($"⚠️ Failed to set NLS settings: {ex.Message}");
            }
        }

        /// <summary>
        /// 연결 해제 (설정만 초기화)
        /// </summary>
        public void Disconnect()
        {
            // 실행 중인 쿼리가 있으면 취소
            CancelQuery();
            
            _connectionString = null;
            _isConfigured = false;
            System.Diagnostics.Debug.WriteLine("🔌 Connection configuration cleared");
        }

        /// <summary>
        /// 현재 실행 중인 쿼리 취소
        /// </summary>
        public void CancelQuery()
        {
            try
            {
                if (_currentCommand != null)
                {
                    System.Diagnostics.Debug.WriteLine("❌ Cancelling Oracle command...");
                    _currentCommand.Cancel();
                    System.Diagnostics.Debug.WriteLine("✅ Oracle command cancelled successfully");
                }
                
                if (_currentQueryCancellation != null && !_currentQueryCancellation.IsCancellationRequested)
                {
                    System.Diagnostics.Debug.WriteLine("❌ Cancelling query token...");
                    _currentQueryCancellation.Cancel();
                    System.Diagnostics.Debug.WriteLine("✅ Query token cancelled successfully");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Error during cancellation: {ex.Message}");
            }
        }

        /// <summary>
        /// 모든 테이블 목록 조회 (MES_MGR 소유)
        /// </summary>
        public async Task<List<string>> GetTablesAsync()
        {
            if (!IsConfigured)
            {
                System.Diagnostics.Debug.WriteLine("⚠️ Not configured");
                return new List<string>();
            }

            var tables = new List<string>();

            try
            {
                using var connection = await CreateConnectionAsync();
                
                var query = @"
                    SELECT TABLE_NAME 
                    FROM ALL_TABLES 
                    WHERE OWNER = 'MES_MGR'
                    ORDER BY TABLE_NAME";

                using var command = new OracleCommand(query, connection);
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    tables.Add(reader.GetString(0));
                }

                System.Diagnostics.Debug.WriteLine($"✅ Loaded {tables.Count} tables (connection auto-closed)");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting tables: {ex.Message}");
            }

            return tables;
        }

        public async Task<List<ColumnInfo>> GetTableColumnsAsync(string tableName)
        {
            if (!IsConfigured || string.IsNullOrEmpty(tableName))
            {
                System.Diagnostics.Debug.WriteLine("⚠️ Not configured or table name is empty");
                return new List<ColumnInfo>();
            }

            var columns = new List<ColumnInfo>();

            try
            {
                using var connection = await CreateConnectionAsync();
                
                var query = @"
                    SELECT 
                        c.COLUMN_NAME,
                        c.DATA_TYPE,
                        c.NULLABLE,
                        cc.COMMENTS
                    FROM ALL_TAB_COLUMNS c
                    LEFT JOIN ALL_COL_COMMENTS cc ON c.OWNER = cc.OWNER AND c.TABLE_NAME = cc.TABLE_NAME AND c.COLUMN_NAME = cc.COLUMN_NAME
                    WHERE c.OWNER = 'MES_MGR'
                    AND c.TABLE_NAME = :tableName
                    ORDER BY c.COLUMN_ID";

                using var command = new OracleCommand(query, connection);
                command.Parameters.Add(new OracleParameter("tableName", tableName));

                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    columns.Add(new ColumnInfo
                    {
                        ColumnName = reader.GetString(0),
                        DataType = reader.GetString(1),
                        Nullable = reader.GetString(2),
                        Comments = reader.IsDBNull(3) ? "" : reader.GetString(3)
                    });
                }

                System.Diagnostics.Debug.WriteLine($"✅ Loaded {columns.Count} columns for {tableName} (connection auto-closed)");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting columns: {ex.Message}");
            }

            return columns;
        }

        public async Task<List<IndexInfo>> GetTableIndexesAsync(string tableName)
        {
            if (!IsConfigured || string.IsNullOrEmpty(tableName))
            {
                System.Diagnostics.Debug.WriteLine("⚠️ Not configured or table name is empty");
                return new List<IndexInfo>();
            }

            var indexes = new List<IndexInfo>();

            try
            {
                using var connection = await CreateConnectionAsync();

                // Primary Key 조회
                var pkQuery = @"
                    SELECT 
                        LISTAGG(cc.COLUMN_NAME, ', ') WITHIN GROUP (ORDER BY cc.POSITION) as COLUMNS
                    FROM ALL_CONSTRAINTS c
                    JOIN ALL_CONS_COLUMNS cc ON c.OWNER = cc.OWNER AND c.CONSTRAINT_NAME = cc.CONSTRAINT_NAME
                    WHERE c.OWNER = 'MES_MGR'
                    AND c.TABLE_NAME = :tableName 
                    AND c.CONSTRAINT_TYPE = 'P'
                    GROUP BY c.CONSTRAINT_NAME";

                using var pkCommand = new OracleCommand(pkQuery, connection);
                pkCommand.Parameters.Add(new OracleParameter("tableName", tableName));

                using var pkReader = await pkCommand.ExecuteReaderAsync();
                if (await pkReader.ReadAsync())
                {
                    indexes.Add(new IndexInfo
                    {
                        Type = "PK",
                        Columns = pkReader.IsDBNull(0) ? "" : pkReader.GetString(0)
                    });
                }

                // Index 조회
                var indexQuery = @"
                    SELECT 
                        i.INDEX_NAME,
                        LISTAGG(ic.COLUMN_NAME, ', ') WITHIN GROUP (ORDER BY ic.COLUMN_POSITION) as COLUMNS
                    FROM ALL_INDEXES i
                    JOIN ALL_IND_COLUMNS ic ON i.OWNER = ic.INDEX_OWNER AND i.INDEX_NAME = ic.INDEX_NAME
                    WHERE i.OWNER = 'MES_MGR'
                    AND i.TABLE_NAME = :tableName 
                    AND i.INDEX_NAME NOT IN (
                        SELECT CONSTRAINT_NAME 
                        FROM ALL_CONSTRAINTS 
                        WHERE OWNER = 'MES_MGR' 
                        AND TABLE_NAME = :tableName 
                        AND CONSTRAINT_TYPE = 'P'
                    )
                    GROUP BY i.INDEX_NAME
                    ORDER BY i.INDEX_NAME";

                using var indexCommand = new OracleCommand(indexQuery, connection);
                indexCommand.Parameters.Add(new OracleParameter("tableName", tableName));
                indexCommand.Parameters.Add(new OracleParameter("tableName", tableName));

                using var indexReader = await indexCommand.ExecuteReaderAsync();
                int indexCount = 1;
                while (await indexReader.ReadAsync())
                {
                    indexes.Add(new IndexInfo
                    {
                        Type = $"Index {indexCount++}",
                        Columns = indexReader.IsDBNull(1) ? "" : indexReader.GetString(1)
                    });
                }

                System.Diagnostics.Debug.WriteLine($"✅ Loaded {indexes.Count} indexes for {tableName} (connection auto-closed)");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting indexes: {ex.Message}");
            }

            return indexes;
        }

        public void Dispose()
        {
            Disconnect();
        }

        /// <summary>
        /// 쿼리를 실행하고 DataTable로 결과 반환 - 타임아웃 10초, 취소 가능, 최대 2000건
        /// </summary>
        public async Task<DataTable> ExecuteQueryAsync(string query)
        {
            if (!IsConfigured)
            {
                throw new InvalidOperationException("Database not configured. Call ConfigureAsync first.");
            }

            if (string.IsNullOrWhiteSpace(query))
            {
                throw new ArgumentException("Query cannot be empty.", nameof(query));
            }

            var dataTable = new DataTable();
            
            // 새로운 CancellationTokenSource 생성 (10초 타임아웃)
            _currentQueryCancellation?.Dispose();
            _currentQueryCancellation = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var cancellationToken = _currentQueryCancellation.Token; // 🔥 로컬 변수로 복사
            _currentCommand = null;

            OracleConnection? connection = null;

            try
            {
                System.Diagnostics.Debug.WriteLine($"📊 Starting query execution...");
                connection = await CreateConnectionAsync();
                
                // 쿼리에 ROWNUM 제한 추가 (서브쿼리로 감싸지 않음)
                var limitedQuery = WrapQueryWithRowLimit(query, 2000);
                System.Diagnostics.Debug.WriteLine($"Original query: {query}");
                System.Diagnostics.Debug.WriteLine($"Limited query: {limitedQuery}");
                
                var command = new OracleCommand(limitedQuery, connection);
                command.CommandTimeout = 10; // 10초 타임아웃
                _currentCommand = command; // 현재 명령 저장
                
                // 바인드 변수 처리 (&변수명)
                var bindVariables = System.Text.RegularExpressions.Regex.Matches(query, @"&(\w+)");
                foreach (System.Text.RegularExpressions.Match match in bindVariables)
                {
                    var paramName = match.Groups[1].Value;
                    // 기본값으로 빈 문자열 설정 (사용자가 입력할 수 있도록)
                    command.Parameters.Add(new OracleParameter(paramName, ""));
                }

                // 취소 토큰 등록 (🔥 로컬 변수 사용)
                using var registration = cancellationToken.Register(() =>
                {
                    System.Diagnostics.Debug.WriteLine("🛑 Cancellation requested - attempting to cancel command");
                    try
                    {
                        command.Cancel();
                        System.Diagnostics.Debug.WriteLine("✅ Command.Cancel() called successfully");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"⚠️ Error calling Command.Cancel(): {ex.Message}");
                    }
                });

                System.Diagnostics.Debug.WriteLine("⏳ Executing Oracle query...");
                
                // 🔥 OracleDataReader를 직접 사용하여 중복 컬럼명 자동 처리
                await Task.Run(async () => 
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    using var reader = await command.ExecuteReaderAsync();
                    BuildDataTableFromReader(reader, dataTable);
                }, cancellationToken);

                System.Diagnostics.Debug.WriteLine($"✅ Query executed successfully. Rows: {dataTable.Rows.Count} (max 2000)");
            }
            catch (OperationCanceledException)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Query execution cancelled or timed out (10 seconds)");
                throw new TimeoutException("쿼리 실행이 취소되었거나 10초를 초과했습니다.");
            }
            catch (OracleException oex) when (oex.Number == 1013) // ORA-01013: user requested cancel of current operation
            {
                System.Diagnostics.Debug.WriteLine($"✅ Query successfully cancelled by user (ORA-01013)");
                throw new OperationCanceledException("쿼리가 사용자에 의해 취소되었습니다.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Query execution error: {ex.Message}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"  Inner Exception: {ex.InnerException.Message}");
                }
                throw;
            }
            finally
            {
                _currentCommand = null;
                _currentQueryCancellation?.Dispose();
                _currentQueryCancellation = null;
                
                if (connection != null)
                {
                    try
                    {
                        connection.Close();
                        connection.Dispose();
                        System.Diagnostics.Debug.WriteLine("🔌 Connection closed");
                    }
                    catch { }
                }
            }

            return dataTable;
        }

        /// <summary>
        /// 바인드 변수가 있는 쿼리를 실행 (매개변수 값 제공) - 타임아웃 10초, 취소 가능, 최대 2000건
        /// </summary>
        public async Task<DataTable> ExecuteQueryWithParametersAsync(string query, Dictionary<string, object> parameters)
        {
            if (!IsConfigured)
            {
                throw new InvalidOperationException("Database not configured. Call ConfigureAsync first.");
            }

            if (string.IsNullOrWhiteSpace(query))
            {
                throw new ArgumentException("Query cannot be empty.", nameof(query));
            }

            var dataTable = new DataTable();
            
            // 새로운 CancellationTokenSource 생성 (10초 타임아웃)
            _currentQueryCancellation?.Dispose();
            _currentQueryCancellation = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var cancellationToken = _currentQueryCancellation.Token; // 🔥 로컬 변수로 복사
            _currentCommand = null;

            OracleConnection? connection = null;

            try
            {
                System.Diagnostics.Debug.WriteLine($"📊 Starting query execution...");
                connection = await CreateConnectionAsync();
                
                // 쿼리에 ROWNUM 제한 추가 (서브쿼리로 감싸지 않음)
                var limitedQuery = WrapQueryWithRowLimit(query, 2000);
                
                // 🔥 &변수명 또는 @변수명을 :변수명으로 변환 (모든 발생 위치)
                var oracleQuery = System.Text.RegularExpressions.Regex.Replace(
                    limitedQuery,
                    @"[&@](\w+)",
                    m => $":{m.Groups[1].Value}"
                );
                
                System.Diagnostics.Debug.WriteLine($"Original query: {query}");
                System.Diagnostics.Debug.WriteLine($"Oracle query: {oracleQuery}");
                
                var command = new OracleCommand(oracleQuery, connection);
                command.CommandTimeout = 10; // 10초 타임아웃
                command.BindByName = true; // 🔥 변수명으로 바인딩 (동일 변수가 여러 번 사용될 때 필수)
                _currentCommand = command; // 현재 명령 저장
                
                // 바인드 변수 추가
                if (parameters != null)
                {
                    foreach (var param in parameters)
                    {
                        command.Parameters.Add(new OracleParameter(param.Key, param.Value ?? DBNull.Value));
                        System.Diagnostics.Debug.WriteLine($"  Binding: :{param.Key} = '{param.Value}'");
                    }
                }

                // 취소 토큰 등록 (🔥 로컬 변수 사용)
                using var registration = cancellationToken.Register(() =>
                {
                    System.Diagnostics.Debug.WriteLine("🛑 Cancellation requested - attempting to cancel command");
                    try
                    {
                        command.Cancel();
                        System.Diagnostics.Debug.WriteLine("✅ Command.Cancel() called successfully");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"⚠️ Error calling Command.Cancel(): {ex.Message}");
                    }
                });

                System.Diagnostics.Debug.WriteLine("⏳ Executing Oracle query...");
                
                // 🔥 OracleDataReader를 직접 사용하여 중복 컬럼명 자동 처리
                await Task.Run(async () => 
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    using var reader = await command.ExecuteReaderAsync();
                    BuildDataTableFromReader(reader, dataTable);
                }, cancellationToken);

                System.Diagnostics.Debug.WriteLine($"✅ Query with parameters executed successfully. Rows: {dataTable.Rows.Count} (max 2000)");
            }
            catch (OperationCanceledException)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Query execution cancelled or timed out (10 seconds)");
                throw new TimeoutException("쿼리 실행이 취소되었거나 10초를 초과했습니다.");
            }
            catch (OracleException oex) when (oex.Number == 1013) // ORA-01013: user requested cancel of current operation
            {
                System.Diagnostics.Debug.WriteLine($"✅ Query successfully cancelled by user (ORA-01013)");
                throw new OperationCanceledException("쿼리가 사용자에 의해 취소되었습니다.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Query execution error: {ex.Message}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"  Inner Exception: {ex.InnerException.Message}");
                }
                throw;
            }
            finally
            {
                _currentCommand = null;
                _currentQueryCancellation?.Dispose();
                _currentQueryCancellation = null;
                
                if (connection != null)
                {
                    try
                    {
                        connection.Close();
                        connection.Dispose();
                        System.Diagnostics.Debug.WriteLine("🔌 Connection closed");
                    }
                    catch { }
                }
            }

            return dataTable;
        }

        /// <summary>
        /// 🔥 OracleDataReader에서 DataTable을 직접 구성하여 중복 컬럼명 자동 처리
        /// PL/SQL Developer처럼 중복 컬럼명에 자동으로 순번 부여
        /// </summary>
        private void BuildDataTableFromReader(OracleDataReader reader, DataTable dataTable)
        {
            // 1️⃣ 컬럼 스키마 읽기 (중복 컬럼명 자동 처리)
            var columnNames = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var schemaTable = reader.GetSchemaTable();
            
            if (schemaTable == null)
            {
                throw new InvalidOperationException("Unable to read schema from query result.");
            }

            foreach (DataRow schemaRow in schemaTable.Rows)
            {
                var originalName = schemaRow["ColumnName"].ToString() ?? "Column";
                var dataType = (Type)schemaRow["DataType"];

                // 🔥 WPF 액셀러레이터 키 문제 해결: '_'를 '__'로 변경
                var escapedName = originalName.Replace("_", "__");
                
                string finalColumnName;
                if (columnNames.ContainsKey(escapedName))
                {
                    // 중복된 컬럼명 → 순번 추가 (PL/SQL Developer 방식)
                    columnNames[escapedName]++;
                    finalColumnName = $"{escapedName}__{columnNames[escapedName]}";
                    System.Diagnostics.Debug.WriteLine($"🔧 Duplicate column detected: {originalName} → {finalColumnName}");
                }
                else
                {
                    finalColumnName = escapedName;
                    columnNames[escapedName] = 1;
                    
                    if (escapedName != originalName)
                    {
                        System.Diagnostics.Debug.WriteLine($"🔧 Escaped column name: {originalName} → {escapedName}");
                    }
                }

                dataTable.Columns.Add(finalColumnName, dataType);
            }

            // 2️⃣ 데이터 읽기
            while (reader.Read())
            {
                var row = dataTable.NewRow();
                for (int i = 0; i < dataTable.Columns.Count; i++)
                {
                    row[i] = reader.IsDBNull(i) ? DBNull.Value : reader.GetValue(i);
                }
                dataTable.Rows.Add(row);
            }

            System.Diagnostics.Debug.WriteLine($"✅ DataTable built with {dataTable.Columns.Count} columns, {dataTable.Rows.Count} rows");
        }

        /// <summary>
        /// 🔥 중복 컬럼명 문제 해결: 쿼리를 서브쿼리로 감싸고 각 컬럼에 자동 alias 부여
        /// PL/SQL Developer처럼 중복 컬럼명이 있어도 실행 가능하도록 처리
        /// </summary>
        private string WrapQueryWithColumnAliases(string query)
        {
            var trimmedQuery = query.Trim();
            
            // SELECT 문이 아니면 그대로 반환
            if (!trimmedQuery.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
            {
                return query;
            }

            // 🔥 서브쿼리로 감싸서 Oracle이 자동으로 컬럼 alias를 생성하도록 유도
            // 바깥쪽 SELECT *는 Oracle이 각 컬럼에 순번을 자동으로 부여
            return $@"SELECT * FROM (
    {query}
) SUBQUERY_AUTO_ALIAS";
        }

        /// <summary>
        /// 쿼리에 ROWNUM 제한을 추가 (서브쿼리로 감싸지 않고 WHERE 절에 직접 추가)
        /// 중복 컬럼명이 있는 쿼리도 실행 가능하도록 처리
        /// </summary>
        private string WrapQueryWithRowLimit(string query, int maxRows)
        {
            // 쿼리가 이미 ROWNUM 제한이 있는지 확인 (대소문자 무시)
            if (query.Contains("ROWNUM", StringComparison.OrdinalIgnoreCase))
            {
                return query; // 이미 ROWNUM 제한이 있으면 그대로 반환
            }

            var trimmedQuery = query.Trim().TrimEnd(';'); // 끝의 세미콜론 제거

            // SELECT 문인지 확인
            if (!trimmedQuery.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
            {
                return query; // SELECT 문이 아니면 그대로 반환
            }

            // 🔥 WHERE 절이 있는지 확인 (정규식으로 마지막 WHERE 찾기)
            var whereMatch = System.Text.RegularExpressions.Regex.Match(
                trimmedQuery, 
                @"\bWHERE\b", 
                System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.RightToLeft
            );

            if (whereMatch.Success)
            {
                // WHERE 절이 있으면 AND ROWNUM 추가
                var insertPosition = whereMatch.Index + whereMatch.Length;
                return trimmedQuery.Insert(insertPosition, $" ROWNUM <= {maxRows} AND");
            }
            else
            {
                // WHERE 절이 없으면 끝에 추가 (ORDER BY, GROUP BY 고려)
                var orderByMatch = System.Text.RegularExpressions.Regex.Match(
                    trimmedQuery,
                    @"\b(ORDER\s+BY|GROUP\s+BY)\b",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase
                );

                if (orderByMatch.Success)
                {
                    // ORDER BY나 GROUP BY 앞에 WHERE 추가
                    return trimmedQuery.Insert(orderByMatch.Index, $" WHERE ROWNUM <= {maxRows} ");
                }
                else
                {
                    // 그냥 끝에 추가
                    return $"{trimmedQuery} WHERE ROWNUM <= {maxRows}";
                }
            }
        }
    }
}
