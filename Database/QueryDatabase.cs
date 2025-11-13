using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.IO;
using FACTOVA_QueryHelper.Models;

namespace FACTOVA_QueryHelper.Database
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
        /// Queries 테이블과 Connections 테이블을 모두 생성합니다.
        /// </summary>
        private void InitializeDatabase()
        {
            string directory = Path.GetDirectoryName(_databasePath) ?? "";
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            // 🔥 Queries 테이블 생성
            var command = connection.CreateCommand();
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS Queries (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    QueryName TEXT NOT NULL,
                    QueryType TEXT DEFAULT '쿼리 실행',
                    TnsName TEXT,
                    Host TEXT,
                    Port TEXT,
                    ServiceName TEXT,
                    UserId TEXT,
                    Password TEXT,
                    Query TEXT,
                    BizName TEXT,
                    Description2 TEXT,
                    OrderNumber INTEGER DEFAULT 0,
                    QueryBizName TEXT,
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
            
            // 🔥 Connections 테이블 생성 (접속 정보 관리용)
            var connCommand = connection.CreateCommand();
            connCommand.CommandText = @"
                CREATE TABLE IF NOT EXISTS Connections (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL,
                    TNS TEXT,
                    Host TEXT,
                    Port TEXT,
                    Service TEXT,
                    UserId TEXT NOT NULL,
                    Password TEXT NOT NULL,
                    SQLQuery TEXT,
                    IsActive INTEGER DEFAULT 0,
                    IsFavorite INTEGER DEFAULT 0,
                    CreatedAt TEXT DEFAULT CURRENT_TIMESTAMP,
                    UpdatedAt TEXT DEFAULT CURRENT_TIMESTAMP
                )";
            connCommand.ExecuteNonQuery();
            
            // QueryType 컬럼 추가 (기존 테이블 호환성)
            try
            {
                var alterCommand = connection.CreateCommand();
                alterCommand.CommandText = "ALTER TABLE Queries ADD COLUMN QueryType TEXT DEFAULT '쿼리 실행'";
                alterCommand.ExecuteNonQuery();
            }
            catch
            {
                // 컬럼이 이미 존재하면 무시
            }
            
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
            
            // BizName 컬럼 추가 (기존 테이블 호환성)
            try
            {
                var alterCommand = connection.CreateCommand();
                alterCommand.CommandText = "ALTER TABLE Queries ADD COLUMN BizName TEXT";
                alterCommand.ExecuteNonQuery();
            }
            catch
            {
                // 컬럼이 이미 존재하면 무시
            }
            
            // Description2 컬럼 추가 (기존 테이블 호환성)
            try
            {
                var alterCommand = connection.CreateCommand();
                alterCommand.CommandText = "ALTER TABLE Queries ADD COLUMN Description2 TEXT";
                alterCommand.ExecuteNonQuery();
            }
            catch
            {
                // 컬럼이 이미 존재하면 무시
            }
            
            // OrderNumber 컬럼 추가 (기존 테이블 호환성)
            try
            {
                var alterCommand = connection.CreateCommand();
                alterCommand.CommandText = "ALTER TABLE Queries ADD COLUMN OrderNumber INTEGER DEFAULT 0";
                alterCommand.ExecuteNonQuery();
            }
            catch
            {
                // 컬럼이 이미 존재하면 무시
            }
            
            // 🔥 QueryBizName 컬럼 추가 (신규)
            try
            {
                var alterCommand = connection.CreateCommand();
                alterCommand.CommandText = "ALTER TABLE Queries ADD COLUMN QueryBizName TEXT";
                alterCommand.ExecuteNonQuery();
            }
            catch
            {
                // 컬럼이 이미 존재하면 무시
            }
            
            // 🔥 SiteInfo 테이블 생성 (사업장 정보 관리용)
            var siteCommand = connection.CreateCommand();
            siteCommand.CommandText = @"
                CREATE TABLE IF NOT EXISTS SiteInfo (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    SiteName TEXT NOT NULL,
                    RepresentativeFactory TEXT,
                    Organization TEXT,
                    Facility TEXT,
                    WipLineId TEXT,
                    EquipLineId TEXT,
                    IsDefault INTEGER DEFAULT 0,
                    CreatedDate TEXT DEFAULT CURRENT_TIMESTAMP,
                    ModifiedDate TEXT DEFAULT CURRENT_TIMESTAMP
                )";
            siteCommand.ExecuteNonQuery();
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
                    QueryType = reader["QueryType"]?.ToString() ?? "쿼리 실행",
                    TnsName = reader["TnsName"]?.ToString() ?? "",
                    Host = reader["Host"]?.ToString() ?? "",
                    Port = reader["Port"]?.ToString() ?? "",
                    ServiceName = reader["ServiceName"]?.ToString() ?? "",
                    UserId = reader["UserId"]?.ToString() ?? "",
                    Password = reader["Password"]?.ToString() ?? "",
                    Query = reader["Query"]?.ToString() ?? "",
                    BizName = reader["BizName"]?.ToString() ?? "",
                    Description2 = reader["Description2"]?.ToString() ?? "",
                    OrderNumber = reader["OrderNumber"] != DBNull.Value ? Convert.ToInt32(reader["OrderNumber"]) : 0,
                    QueryBizName = reader["QueryBizName"]?.ToString() ?? "",
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
                    QueryName, QueryType, TnsName, Host, Port, ServiceName, UserId, Password, Query,
                    BizName, Description2, OrderNumber, QueryBizName,
                    EnabledFlag, NotifyFlag, CountGreaterThan, CountEquals, CountLessThan,
                    ColumnNames, ColumnValues, ExcludeFlag, DefaultFlag
                ) VALUES (
                    $queryName, $queryType, $tnsName, $host, $port, $serviceName, $userId, $password, $query,
                    $bizName, $description2, $orderNumber, $queryBizName,
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
                    QueryType = $queryType,
                    TnsName = $tnsName,
                    Host = $host,
                    Port = $port,
                    ServiceName = $serviceName,
                    UserId = $userId,
                    Password = $password,
                    Query = $query,
                    BizName = $bizName,
                    Description2 = $description2,
                    OrderNumber = $orderNumber,
                    QueryBizName = $queryBizName,
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
        /// 쿼리 파라미터를 추가하는 헬퍼 메서드
        /// </summary>
        private void AddQueryParameters(SqliteCommand command, QueryItem query)
        {
            command.Parameters.AddWithValue("$queryName", query.QueryName);
            command.Parameters.AddWithValue("$queryType", query.QueryType ?? "쿼리 실행");
            command.Parameters.AddWithValue("$tnsName", query.TnsName ?? "");
            command.Parameters.AddWithValue("$host", query.Host ?? "");
            command.Parameters.AddWithValue("$port", query.Port ?? "");
            command.Parameters.AddWithValue("$serviceName", query.ServiceName ?? "");
            command.Parameters.AddWithValue("$userId", query.UserId ?? "");
            command.Parameters.AddWithValue("$password", query.Password ?? "");
            command.Parameters.AddWithValue("$query", query.Query ?? "");
            command.Parameters.AddWithValue("$bizName", query.BizName ?? "");
            command.Parameters.AddWithValue("$description2", query.Description2 ?? "");
            command.Parameters.AddWithValue("$orderNumber", query.OrderNumber);
            command.Parameters.AddWithValue("$queryBizName", query.QueryBizName ?? "");
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
        /// 프로그램 실행 경로에 FACTOVA_DB.db 파일 생성
        /// </summary>
        public static string GetDefaultDatabasePath()
        {
            // 🔥 프로그램 실행 파일이 있는 디렉토리 경로
            var exeDirectory = AppDomain.CurrentDomain.BaseDirectory;
            return Path.Combine(exeDirectory, "FACTOVA_DB.db");
        }

        #region 사업장 정보 관리

        /// <summary>
        /// 모든 사업장 정보를 조회합니다.
        /// </summary>
        public List<SiteInfo> GetAllSites()
        {
            var sites = new List<SiteInfo>();

            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM SiteInfo ORDER BY IsDefault DESC, SiteName";

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                sites.Add(new SiteInfo
                {
                    Id = Convert.ToInt32(reader["Id"]),
                    SiteName = reader["SiteName"]?.ToString() ?? "",
                    RepresentativeFactory = reader["RepresentativeFactory"]?.ToString() ?? "",
                    Organization = reader["Organization"]?.ToString() ?? "",
                    Facility = reader["Facility"]?.ToString() ?? "",
                    WipLineId = reader["WipLineId"]?.ToString() ?? "",
                    EquipLineId = reader["EquipLineId"]?.ToString() ?? "",
                    IsDefault = Convert.ToInt32(reader["IsDefault"]) == 1
                });
            }

            return sites;
        }

        /// <summary>
        /// 사업장 정보를 추가합니다.
        /// </summary>
        public void AddSite(SiteInfo site)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            // 기본 사업장으로 설정하는 경우 기존 기본 설정 해제
            if (site.IsDefault)
            {
                var clearDefaultCommand = connection.CreateCommand();
                clearDefaultCommand.CommandText = "UPDATE SiteInfo SET IsDefault = 0";
                clearDefaultCommand.ExecuteNonQuery();
            }

            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO SiteInfo (
                    SiteName, RepresentativeFactory, Organization, Facility, 
                    WipLineId, EquipLineId, IsDefault
                ) VALUES (
                    $siteName, $representativeFactory, $organization, $facility,
                    $wipLineId, $equipLineId, $isDefault
                )";

            command.Parameters.AddWithValue("$siteName", site.SiteName);
            command.Parameters.AddWithValue("$representativeFactory", site.RepresentativeFactory ?? "");
            command.Parameters.AddWithValue("$organization", site.Organization ?? "");
            command.Parameters.AddWithValue("$facility", site.Facility ?? "");
            command.Parameters.AddWithValue("$wipLineId", site.WipLineId ?? "");
            command.Parameters.AddWithValue("$equipLineId", site.EquipLineId ?? "");
            command.Parameters.AddWithValue("$isDefault", site.IsDefault ? 1 : 0);

            command.ExecuteNonQuery();
        }

        /// <summary>
        /// 사업장 정보를 수정합니다.
        /// </summary>
        public void UpdateSite(SiteInfo site)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            // 기본 사업장으로 설정하는 경우 기존 기본 설정 해제
            if (site.IsDefault)
            {
                var clearDefaultCommand = connection.CreateCommand();
                clearDefaultCommand.CommandText = "UPDATE SiteInfo SET IsDefault = 0 WHERE Id != $id";
                clearDefaultCommand.Parameters.AddWithValue("$id", site.Id);
                clearDefaultCommand.ExecuteNonQuery();
            }

            var command = connection.CreateCommand();
            command.CommandText = @"
                UPDATE SiteInfo SET
                    SiteName = $siteName,
                    RepresentativeFactory = $representativeFactory,
                    Organization = $organization,
                    Facility = $facility,
                    WipLineId = $wipLineId,
                    EquipLineId = $equipLineId,
                    IsDefault = $isDefault,
                    ModifiedDate = CURRENT_TIMESTAMP
                WHERE Id = $id";

            command.Parameters.AddWithValue("$id", site.Id);
            command.Parameters.AddWithValue("$siteName", site.SiteName);
            command.Parameters.AddWithValue("$representativeFactory", site.RepresentativeFactory ?? "");
            command.Parameters.AddWithValue("$organization", site.Organization ?? "");
            command.Parameters.AddWithValue("$facility", site.Facility ?? "");
            command.Parameters.AddWithValue("$wipLineId", site.WipLineId ?? "");
            command.Parameters.AddWithValue("$equipLineId", site.EquipLineId ?? "");
            command.Parameters.AddWithValue("$isDefault", site.IsDefault ? 1 : 0);

            command.ExecuteNonQuery();
        }

        /// <summary>
        /// 사업장 정보를 삭제합니다.
        /// </summary>
        public void DeleteSite(int id)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM SiteInfo WHERE Id = $id";
            command.Parameters.AddWithValue("$id", id);
            command.ExecuteNonQuery();
        }

        /// <summary>
        /// 기본 사업장 정보를 조회합니다.
        /// </summary>
        public SiteInfo? GetDefaultSite()
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM SiteInfo WHERE IsDefault = 1 LIMIT 1";

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return new SiteInfo
                {
                    Id = Convert.ToInt32(reader["Id"]),
                    SiteName = reader["SiteName"]?.ToString() ?? "",
                    RepresentativeFactory = reader["RepresentativeFactory"]?.ToString() ?? "",
                    Organization = reader["Organization"]?.ToString() ?? "",
                    Facility = reader["Facility"]?.ToString() ?? "",
                    WipLineId = reader["WipLineId"]?.ToString() ?? "",
                    EquipLineId = reader["EquipLineId"]?.ToString() ?? "",
                    IsDefault = true
                };
            }

            return null;
        }

        #endregion
    }
}
