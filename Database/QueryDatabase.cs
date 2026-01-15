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
            
            // 🔥 Connections テーブル 생성 (접속 정보 관리용)
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
            
            // 🔥 ConnectionInfoId 컬럼 추가 (접속 정보 참조)
            try
            {
                var alterCommand = connection.CreateCommand();
                alterCommand.CommandText = "ALTER TABLE Queries ADD COLUMN ConnectionInfoId INTEGER";
                alterCommand.ExecuteNonQuery();
            }
            catch
            {
                // 컬럼이 이미 존재하면 무시
            }
            
            // 🔥 Version 컬럼 추가 (버전 정보)
            try
            {
                var alterCommand = connection.CreateCommand();
                alterCommand.CommandText = "ALTER TABLE Queries ADD COLUMN Version TEXT";
                alterCommand.ExecuteNonQuery();
            }
            catch
            {
                // 컬럼이 이미 존재하면 무시
            }
            
            // 🔥 RowColor 컬럼 추가 (행 색상)
            try
            {
                var alterCommand = connection.CreateCommand();
                alterCommand.CommandText = "ALTER TABLE Queries ADD COLUMN RowColor TEXT";
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
                    Division TEXT,
                    IsDefault INTEGER DEFAULT 0,
                    DisplayOrder INTEGER DEFAULT 0,
                    TnsVersionMapping TEXT DEFAULT '{}',
                    CreatedDate TEXT DEFAULT CURRENT_TIMESTAMP,
                    ModifiedDate TEXT DEFAULT CURRENT_TIMESTAMP
                )";
            siteCommand.ExecuteNonQuery();
            
            // 🔥 DisplayOrder 컬럼 추가 (기존 테이블 호환성)
            try
            {
                var alterSiteCommand = connection.CreateCommand();
                alterSiteCommand.CommandText = "ALTER TABLE SiteInfo ADD COLUMN DisplayOrder INTEGER DEFAULT 0";
                alterSiteCommand.ExecuteNonQuery();
            }
            catch
            {
                // 컬럼이 이미 존재하면 무시
            }
            
            // 🔥 TnsVersionMapping 컬럼 추가 (신규)
            try
            {
                var alterTnsCommand = connection.CreateCommand();
                alterTnsCommand.CommandText = "ALTER TABLE SiteInfo ADD COLUMN TnsVersionMapping TEXT DEFAULT '{}'";
                alterTnsCommand.ExecuteNonQuery();
            }
            catch
            {
                // 컬럼이 이미 존재하면 무시
            }
            
            // 🔥 Division 컬럼 추가 (기존 테이블 호환성)
            try
            {
                var alterDivisionCommand = connection.CreateCommand();
                alterDivisionCommand.CommandText = "ALTER TABLE SiteInfo ADD COLUMN Division TEXT";
                alterDivisionCommand.ExecuteNonQuery();
            }
            catch
            {
                // 컬럼이 이미 존재하면 무시
            }
            
            // 🔥 TableShortcuts 테이블 생성 (테이블 단축어 관리용)
            var shortcutCommand = connection.CreateCommand();
            shortcutCommand.CommandText = @"
                CREATE TABLE IF NOT EXISTS TableShortcuts (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Shortcut TEXT NOT NULL UNIQUE,
                    FullTableName TEXT NOT NULL,
                    Description TEXT,
                    CreatedDate TEXT DEFAULT CURRENT_TIMESTAMP,
                    ModifiedDate TEXT DEFAULT CURRENT_TIMESTAMP
                )";
            shortcutCommand.ExecuteNonQuery();
            
            // 🔥 Parameters 테이블 생성 (기준정보 파라미터 관리용)
            var paramCommand = connection.CreateCommand();
            paramCommand.CommandText = @"
                CREATE TABLE IF NOT EXISTS Parameters (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Parameter TEXT NOT NULL,
                    Description TEXT,
                    Value TEXT,
                    DisplayOrder INTEGER DEFAULT 0,
                    CreatedDate TEXT DEFAULT CURRENT_TIMESTAMP,
                    ModifiedDate TEXT DEFAULT CURRENT_TIMESTAMP
                )";
            paramCommand.ExecuteNonQuery();
            
            // 🔥 NoQuotes 컬럼 추가 (파라미터 치환 시 따옴표 제외 여부)
            try
            {
                var alterParamCommand = connection.CreateCommand();
                alterParamCommand.CommandText = "ALTER TABLE Parameters ADD COLUMN NoQuotes INTEGER DEFAULT 0";
                alterParamCommand.ExecuteNonQuery();
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
                    QueryType = reader["QueryType"]?.ToString() ?? "쿼리 실행",
                    ConnectionInfoId = reader["ConnectionInfoId"] != DBNull.Value ? Convert.ToInt32(reader["ConnectionInfoId"]) : (int?)null,
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
                    Version = reader["Version"]?.ToString() ?? "",
                    RowColor = reader["RowColor"]?.ToString() ?? "",
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
        /// 쿼리를 추가하고 생성된 ID를 반환합니다.
        /// </summary>
        public int InsertQuery(QueryItem query)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO Queries (
                    QueryName, QueryType, ConnectionInfoId, TnsName, Host, Port, ServiceName, UserId, Password, Query,
                    BizName, Description2, OrderNumber, QueryBizName, Version, RowColor,
                    EnabledFlag, NotifyFlag, CountGreaterThan, CountEquals, CountLessThan,
                    ColumnNames, ColumnValues, ExcludeFlag, DefaultFlag
                ) VALUES (
                    $queryName, $queryType, $connectionInfoId, $tnsName, $host, $port, $serviceName, $userId, $password, $query,
                    $bizName, $description2, $orderNumber, $queryBizName, $version, $rowColor,
                    $enabledFlag, $notifyFlag, $countGreaterThan, $countEquals, $countLessThan,
                    $columnNames, $columnValues, $excludeFlag, $defaultFlag
                );
                SELECT last_insert_rowid();";

            AddQueryParameters(command, query);
            var newId = Convert.ToInt32(command.ExecuteScalar());
            return newId;
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
                    ConnectionInfoId = $connectionInfoId,
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
                    Version = $version,
                    RowColor = $rowColor,
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
            command.Parameters.AddWithValue("$connectionInfoId", query.ConnectionInfoId.HasValue ? (object)query.ConnectionInfoId.Value : DBNull.Value);
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
            command.Parameters.AddWithValue("$version", query.Version ?? "");
            command.Parameters.AddWithValue("$rowColor", query.RowColor ?? "");
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
            // 🔥 IsDefault > 0인 항목을 우선 정렬하고, 그 다음 IsDefault = 0인 항목 정렬
            // CASE 문을 사용하여 IsDefault = 0이면 999999로 취급하여 맨 뒤로 보냄
            command.CommandText = @"
                SELECT * FROM SiteInfo 
                ORDER BY 
                    CASE WHEN IsDefault = 0 THEN 999999 ELSE IsDefault END,
                    SiteName";

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
                    Division = reader["Division"]?.ToString() ?? "",
                    IsDefault = reader["IsDefault"] != DBNull.Value ? Convert.ToInt32(reader["IsDefault"]) : 0,
                    TnsVersionMapping = reader["TnsVersionMapping"]?.ToString() ?? "{}"
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

            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO SiteInfo (
                    SiteName, RepresentativeFactory, Organization, Facility, 
                    WipLineId, EquipLineId, Division, IsDefault, TnsVersionMapping
                ) VALUES (
                    $siteName, $representativeFactory, $organization, $facility,
                    $wipLineId, $equipLineId, $division, $isDefault, $tnsVersionMapping
                )";

            command.Parameters.AddWithValue("$siteName", site.SiteName);
            command.Parameters.AddWithValue("$representativeFactory", site.RepresentativeFactory ?? "");
            command.Parameters.AddWithValue("$organization", site.Organization ?? "");
            command.Parameters.AddWithValue("$facility", site.Facility ?? "");
            command.Parameters.AddWithValue("$wipLineId", site.WipLineId ?? "");
            command.Parameters.AddWithValue("$equipLineId", site.EquipLineId ?? "");
            command.Parameters.AddWithValue("$division", site.Division ?? "");
            command.Parameters.AddWithValue("$isDefault", site.IsDefault);
            command.Parameters.AddWithValue("$tnsVersionMapping", site.TnsVersionMapping);

            command.ExecuteNonQuery();
        }

        /// <summary>
        /// 사업장 정보를 수정합니다.
        /// </summary>
        public void UpdateSite(SiteInfo site)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                UPDATE SiteInfo SET
                    SiteName = $siteName,
                    RepresentativeFactory = $representativeFactory,
                    Organization = $organization,
                    Facility = $facility,
                    WipLineId = $wipLineId,
                    EquipLineId = $equipLineId,
                    Division = $division,
                    IsDefault = $isDefault,
                    TnsVersionMapping = $tnsVersionMapping,
                    ModifiedDate = CURRENT_TIMESTAMP
                WHERE Id = $id";

            command.Parameters.AddWithValue("$id", site.Id);
            command.Parameters.AddWithValue("$siteName", site.SiteName);
            command.Parameters.AddWithValue("$representativeFactory", site.RepresentativeFactory ?? "");
            command.Parameters.AddWithValue("$organization", site.Organization ?? "");
            command.Parameters.AddWithValue("$facility", site.Facility ?? "");
            command.Parameters.AddWithValue("$wipLineId", site.WipLineId ?? "");
            command.Parameters.AddWithValue("$equipLineId", site.EquipLineId ?? "");
            command.Parameters.AddWithValue("$division", site.Division ?? "");
            command.Parameters.AddWithValue("$isDefault", site.IsDefault);
            command.Parameters.AddWithValue("$tnsVersionMapping", site.TnsVersionMapping);

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
            // 🔥 IsDefault가 0이 아닌 첫 번째 사업장 (표시순번이 가장 작은 것)
            command.CommandText = "SELECT * FROM SiteInfo WHERE IsDefault > 0 ORDER BY IsDefault LIMIT 1";

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
                    IsDefault = reader["IsDefault"] != DBNull.Value ? Convert.ToInt32(reader["IsDefault"]) : 0
                };
            }

            return null;
        }

        #endregion
        
        #region 일괄 계정 변경
        
        /// <summary>
        /// TNS와 User ID를 기준으로 Password를 일괄 변경합니다.
        /// </summary>
        /// <param name="tns">변경 대상 TNS (필수)</param>
        /// <param name="userId">변경 대상 User ID (필수)</param>
        /// <param name="password">변경할 Password (필수)</param>
        /// <returns>변경된 행 수</returns>
        public int BulkUpdateCredentials(string? tns, string? userId, string? password)
        {
            // 필수 파라미터 검증
            if (string.IsNullOrWhiteSpace(tns) || string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(password))
            {
                throw new ArgumentException("TNS, User ID, Password는 모두 필수입니다.");
            }

            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            
            // TNS와 User ID가 일치하는 행의 Password만 업데이트
            command.CommandText = "UPDATE Queries SET Password = $password WHERE TnsName = $tns AND UserId = $userId";
            
            command.Parameters.AddWithValue("$tns", tns);
            command.Parameters.AddWithValue("$userId", userId);
            command.Parameters.AddWithValue("$password", password);

            return command.ExecuteNonQuery();
        }
        
        #endregion
        
        #region 기준정보 파라미터 관리

        /// <summary>
        /// 모든 파라미터 정보를 조회합니다.
        /// </summary>
        public List<Models.ParameterInfo> GetAllParameters()
        {
            var parameters = new List<Models.ParameterInfo>();

            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM Parameters ORDER BY DisplayOrder, Id";

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                parameters.Add(new Models.ParameterInfo
                {
                    Id = Convert.ToInt32(reader["Id"]),
                    Parameter = reader["Parameter"]?.ToString() ?? "",
                    Description = reader["Description"]?.ToString() ?? "",
                    Value = reader["Value"]?.ToString() ?? ""
                });
            }

            return parameters;
        }

        /// <summary>
        /// 파라미터 정보를 추가합니다.
        /// </summary>
        public void AddParameter(Models.ParameterInfo parameter)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO Parameters (Parameter, Description, Value, DisplayOrder)
                VALUES ($parameter, $description, $value, 0)";

            command.Parameters.AddWithValue("$parameter", parameter.Parameter);
            command.Parameters.AddWithValue("$description", parameter.Description ?? "");
            command.Parameters.AddWithValue("$value", parameter.Value ?? "");

            command.ExecuteNonQuery();
        }

        /// <summary>
        /// 파라미터 정보를 수정합니다.
        /// </summary>
        public void UpdateParameter(Models.ParameterInfo parameter)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                UPDATE Parameters SET
                    Parameter = $parameter,
                    Description = $description,
                    Value = $value,
                    ModifiedDate = CURRENT_TIMESTAMP
                WHERE Id = $id";

            command.Parameters.AddWithValue("$id", parameter.Id);
            command.Parameters.AddWithValue("$parameter", parameter.Parameter);
            command.Parameters.AddWithValue("$description", parameter.Description ?? "");
            command.Parameters.AddWithValue("$value", parameter.Value ?? "");

            command.ExecuteNonQuery();
        }

        /// <summary>
        /// 파라미터 정보를 삭제합니다.
        /// </summary>
        public void DeleteParameter(int id)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM Parameters WHERE Id = $id";
            command.Parameters.AddWithValue("$id", id);
            command.ExecuteNonQuery();
        }

        #endregion
    }
}
