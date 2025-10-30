using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.IO;

namespace FACTOVA_QueryHelper
{
    /// <summary>
    /// SQLite 데이터베이스를 관리하는 클래스
    /// </summary>
    public class QueryDatabase
    {
        private readonly string _connectionString;
        private readonly string _databasePath;

        public QueryDatabase(string? customPath = null)
        {
            // 사용자 지정 경로가 있으면 사용, 없으면 기본 경로 사용
            _databasePath = string.IsNullOrWhiteSpace(customPath) ? GetDefaultDatabasePath() : customPath;
            _connectionString = $"Data Source={_databasePath}";
            InitializeDatabase();
        }

        /// <summary>
        /// 데이터베이스를 초기화합니다.
        /// </summary>
        private void InitializeDatabase()
        {
            string directory = Path.GetDirectoryName(_databasePath) ?? "";
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS Queries (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    QueryName TEXT NOT NULL,
                    TnsName TEXT,
                    Host TEXT,
                    Port TEXT,
                    ServiceName TEXT,
                    UserId TEXT,
                    Password TEXT,
                    Query TEXT,
                    EnabledFlag TEXT DEFAULT 'Y',
                    NotifyFlag TEXT DEFAULT 'N',
                    CountGreaterThan TEXT,
                    CountEquals TEXT,
                    CountLessThan TEXT,
                    ColumnNames TEXT,
                    ColumnValues TEXT,
                    ExcludeFlag TEXT DEFAULT 'N',
                    DefaultFlag TEXT DEFAULT 'N',
                    CreatedDate TEXT DEFAULT CURRENT_TIMESTAMP,
                    ModifiedDate TEXT DEFAULT CURRENT_TIMESTAMP
                )";
            command.ExecuteNonQuery();
            
            // DefaultFlag 컬럼이 없는 기존 테이블에 컬럼 추가
            try
            {
                var alterCommand = connection.CreateCommand();
                alterCommand.CommandText = "ALTER TABLE Queries ADD COLUMN DefaultFlag TEXT DEFAULT 'N'";
                alterCommand.ExecuteNonQuery();
            }
            catch
            {
                // 컬럼이 이미 존재하면 무시
            }
        }

        /// <summary>
        /// 모든 쿼리를 조회합니다.
        /// </summary>
        public List<QueryItem> GetAllQueries()
        {
            var queries = new List<QueryItem>();

            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM Queries ORDER BY QueryName";

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                queries.Add(new QueryItem
                {
                    RowNumber = Convert.ToInt32(reader["Id"]),
                    QueryName = reader["QueryName"]?.ToString() ?? "",
                    TnsName = reader["TnsName"]?.ToString() ?? "",
                    Host = reader["Host"]?.ToString() ?? "",
                    Port = reader["Port"]?.ToString() ?? "",
                    ServiceName = reader["ServiceName"]?.ToString() ?? "",
                    UserId = reader["UserId"]?.ToString() ?? "",
                    Password = reader["Password"]?.ToString() ?? "",
                    Query = reader["Query"]?.ToString() ?? "",
                    EnabledFlag = reader["EnabledFlag"]?.ToString() ?? "Y",
                    NotifyFlag = reader["NotifyFlag"]?.ToString() ?? "N",
                    CountGreaterThan = reader["CountGreaterThan"]?.ToString() ?? "",
                    CountEquals = reader["CountEquals"]?.ToString() ?? "",
                    CountLessThan = reader["CountLessThan"]?.ToString() ?? "",
                    ColumnNames = reader["ColumnNames"]?.ToString() ?? "",
                    ColumnValues = reader["ColumnValues"]?.ToString() ?? "",
                    ExcludeFlag = reader["ExcludeFlag"]?.ToString() ?? "N",
                    DefaultFlag = reader["DefaultFlag"]?.ToString() ?? "N"
                });
            }

            return queries;
        }

        /// <summary>
        /// 쿼리를 추가합니다.
        /// </summary>
        public void AddQuery(QueryItem query)
        {
            InsertQuery(query);
        }

        /// <summary>
        /// 쿼리를 추가합니다.
        /// </summary>
        public void InsertQuery(QueryItem query)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO Queries (
                    QueryName, TnsName, Host, Port, ServiceName, UserId, Password, Query,
                    EnabledFlag, NotifyFlag, CountGreaterThan, CountEquals, CountLessThan,
                    ColumnNames, ColumnValues, ExcludeFlag, DefaultFlag
                ) VALUES (
                    $queryName, $tnsName, $host, $port, $serviceName, $userId, $password, $query,
                    $enabledFlag, $notifyFlag, $countGreaterThan, $countEquals, $countLessThan,
                    $columnNames, $columnValues, $excludeFlag, $defaultFlag
                )";

            AddQueryParameters(command, query);
            command.ExecuteNonQuery();
        }

        /// <summary>
        /// 쿼리를 수정합니다.
        /// </summary>
        public void UpdateQuery(QueryItem query)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                UPDATE Queries SET
                    QueryName = $queryName,
                    TnsName = $tnsName,
                    Host = $host,
                    Port = $port,
                    ServiceName = $serviceName,
                    UserId = $userId,
                    Password = $password,
                    Query = $query,
                    EnabledFlag = $enabledFlag,
                    NotifyFlag = $notifyFlag,
                    CountGreaterThan = $countGreaterThan,
                    CountEquals = $countEquals,
                    CountLessThan = $countLessThan,
                    ColumnNames = $columnNames,
                    ColumnValues = $columnValues,
                    ExcludeFlag = $excludeFlag,
                    DefaultFlag = $defaultFlag,
                    ModifiedDate = CURRENT_TIMESTAMP
                WHERE Id = $id";

            command.Parameters.AddWithValue("$id", query.RowNumber);
            AddQueryParameters(command, query);
            command.ExecuteNonQuery();
        }

        /// <summary>
        /// 쿼리를 삭제합니다.
        /// </summary>
        public void DeleteQuery(int id)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM Queries WHERE Id = $id";
            command.Parameters.AddWithValue("$id", id);
            command.ExecuteNonQuery();
        }

        /// <summary>
        /// 모든 쿼리를 삭제합니다.
        /// </summary>
        public void ClearAllQueries()
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM Queries";
            command.ExecuteNonQuery();
        }

        /// <summary>
        /// Excel에서 쿼리를 가져옵니다.
        /// </summary>
        public void ImportFromExcel(List<QueryItem> queries, bool clearExisting = false)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            using var transaction = connection.BeginTransaction();

            try
            {
                if (clearExisting)
                {
                    var clearCommand = connection.CreateCommand();
                    clearCommand.CommandText = "DELETE FROM Queries";
                    clearCommand.ExecuteNonQuery();
                }

                foreach (var query in queries)
                {
                    var command = connection.CreateCommand();
                    command.CommandText = @"
                        INSERT INTO Queries (
                            QueryName, TnsName, Host, Port, ServiceName, UserId, Password, Query,
                            EnabledFlag, NotifyFlag, CountGreaterThan, CountEquals, CountLessThan,
                            ColumnNames, ColumnValues, ExcludeFlag, DefaultFlag
                        ) VALUES (
                            $queryName, $tnsName, $host, $port, $serviceName, $userId, $password, $query,
                            $enabledFlag, $notifyFlag, $countGreaterThan, $countEquals, $countLessThan,
                            $columnNames, $columnValues, $excludeFlag, $defaultFlag
                        )";

                    AddQueryParameters(command, query);
                    command.ExecuteNonQuery();
                }

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        /// <summary>
        /// 쿼리 파라미터를 추가하는 헬퍼 메서드
        /// </summary>
        private void AddQueryParameters(SqliteCommand command, QueryItem query)
        {
            command.Parameters.AddWithValue("$queryName", query.QueryName);
            command.Parameters.AddWithValue("$tnsName", query.TnsName ?? "");
            command.Parameters.AddWithValue("$host", query.Host ?? "");
            command.Parameters.AddWithValue("$port", query.Port ?? "");
            command.Parameters.AddWithValue("$serviceName", query.ServiceName ?? "");
            command.Parameters.AddWithValue("$userId", query.UserId ?? "");
            command.Parameters.AddWithValue("$password", query.Password ?? "");
            command.Parameters.AddWithValue("$query", query.Query ?? "");
            command.Parameters.AddWithValue("$enabledFlag", query.EnabledFlag ?? "Y");
            command.Parameters.AddWithValue("$notifyFlag", query.NotifyFlag ?? "N");
            command.Parameters.AddWithValue("$countGreaterThan", query.CountGreaterThan ?? "");
            command.Parameters.AddWithValue("$countEquals", query.CountEquals ?? "");
            command.Parameters.AddWithValue("$countLessThan", query.CountLessThan ?? "");
            command.Parameters.AddWithValue("$columnNames", query.ColumnNames ?? "");
            command.Parameters.AddWithValue("$columnValues", query.ColumnValues ?? "");
            command.Parameters.AddWithValue("$excludeFlag", query.ExcludeFlag ?? "N");
            command.Parameters.AddWithValue("$defaultFlag", query.DefaultFlag ?? "N");
        }

        /// <summary>
        /// 데이터베이스 경로를 반환합니다.
        /// </summary>
        public string GetDatabasePath()
        {
            return _databasePath;
        }
        
        /// <summary>
        /// 기본 데이터베이스 경로를 반환합니다 (정적 메서드).
        /// </summary>
        public static string GetDefaultDatabasePath()
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "FACTOVA_QueryHelper",
                "queries.db"
            );
        }
    }
}
