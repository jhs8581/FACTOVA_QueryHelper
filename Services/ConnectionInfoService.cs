using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using FACTOVA_QueryHelper.Models;

namespace FACTOVA_QueryHelper.Services
{
    /// <summary>
    /// SQLite를 사용한 접속 정보 관리 서비스
    /// QueryDatabase와 동일한 DB 파일 사용 (FACTOVA_QueryHelper.db)
    /// </summary>
    public class ConnectionInfoService
    {
        private readonly string _dbPath;
        private readonly string _encryptionKey = "FACTOVA_QueryHelper_2025"; // 실제로는 더 안전한 키 관리 필요

        public ConnectionInfoService()
        {
            // 🔥 QueryDatabase와 동일한 경로 사용
            _dbPath = GetDefaultDatabasePath();
            InitializeDatabase();
        }

        /// <summary>
        /// 데이터베이스 초기화
        /// Connections 테이블만 생성 (Queries 테이블은 QueryDatabase에서 생성)
        /// </summary>
        private void InitializeDatabase()
        {
            string directory = Path.GetDirectoryName(_dbPath) ?? "";
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using var connection = new SqliteConnection($"Data Source={_dbPath}");
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
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
            command.ExecuteNonQuery();
        }

        /// <summary>
        /// 비밀번호 암호화
        /// </summary>
        private string EncryptPassword(string password)
        {
            if (string.IsNullOrEmpty(password))
                return string.Empty;

            using var aes = Aes.Create();
            aes.Key = Encoding.UTF8.GetBytes(_encryptionKey.PadRight(32).Substring(0, 32));
            aes.IV = new byte[16];

            using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            var plainBytes = Encoding.UTF8.GetBytes(password);
            var encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

            return Convert.ToBase64String(encryptedBytes);
        }

        /// <summary>
        /// 비밀번호 복호화
        /// </summary>
        private string DecryptPassword(string encryptedPassword)
        {
            if (string.IsNullOrEmpty(encryptedPassword))
                return string.Empty;

            try
            {
                using var aes = Aes.Create();
                aes.Key = Encoding.UTF8.GetBytes(_encryptionKey.PadRight(32).Substring(0, 32));
                aes.IV = new byte[16];

                using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
                var encryptedBytes = Convert.FromBase64String(encryptedPassword);
                var decryptedBytes = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);

                return Encoding.UTF8.GetString(decryptedBytes);
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// 모든 접속 정보 조회
        /// </summary>
        public List<ConnectionInfo> GetAllConnections()
        {
            var connections = new List<ConnectionInfo>();

            using var connection = new SqliteConnection($"Data Source={_dbPath}");
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM Connections ORDER BY Id ASC";

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                connections.Add(new ConnectionInfo
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    TNS = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                    Host = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                    Port = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                    Service = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                    UserId = reader.GetString(6),
                    Password = DecryptPassword(reader.GetString(7)),
                    SQLQuery = reader.IsDBNull(8) ? string.Empty : reader.GetString(8),
                    IsActive = reader.GetInt32(9) == 1,
                    IsFavorite = reader.GetInt32(10) == 1
                });
            }

            return connections;
        }

        /// <summary>
        /// 모든 접속 정보 조회
        /// </summary>
        public List<ConnectionInfo> GetAll() => GetAllConnections();

        /// <summary>
        /// 접속 정보 추가
        /// </summary>
        public int AddConnection(ConnectionInfo info)
        {
            using var connection = new SqliteConnection($"Data Source={_dbPath}");
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO Connections (Name, TNS, Host, Port, Service, UserId, Password, SQLQuery, IsActive, IsFavorite)
                VALUES (@name, @tns, @host, @port, @service, @userId, @password, @sqlQuery, @isActive, @isFavorite);
                SELECT last_insert_rowid();";

            command.Parameters.AddWithValue("@name", info.Name);
            command.Parameters.AddWithValue("@tns", info.TNS ?? string.Empty);
            command.Parameters.AddWithValue("@host", info.Host ?? string.Empty);
            command.Parameters.AddWithValue("@port", info.Port ?? string.Empty);
            command.Parameters.AddWithValue("@service", info.Service ?? string.Empty);
            command.Parameters.AddWithValue("@userId", info.UserId);
            command.Parameters.AddWithValue("@password", EncryptPassword(info.Password));
            command.Parameters.AddWithValue("@sqlQuery", info.SQLQuery ?? string.Empty);
            command.Parameters.AddWithValue("@isActive", info.IsActive ? 1 : 0);
            command.Parameters.AddWithValue("@isFavorite", info.IsFavorite ? 1 : 0);

            var result = command.ExecuteScalar();
            return Convert.ToInt32(result);
        }

        /// <summary>
        /// 접속 정보 수정
        /// </summary>
        public void UpdateConnection(ConnectionInfo info)
        {
            using var connection = new SqliteConnection($"Data Source={_dbPath}");
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                UPDATE Connections
                SET Name = @name, TNS = @tns, Host = @host, Port = @port, Service = @service,
                    UserId = @userId, Password = @password, SQLQuery = @sqlQuery,
                    IsActive = @isActive, IsFavorite = @isFavorite, UpdatedAt = CURRENT_TIMESTAMP
                WHERE Id = @id";

            command.Parameters.AddWithValue("@id", info.Id);
            command.Parameters.AddWithValue("@name", info.Name);
            command.Parameters.AddWithValue("@tns", info.TNS ?? string.Empty);
            command.Parameters.AddWithValue("@host", info.Host ?? string.Empty);
            command.Parameters.AddWithValue("@port", info.Port ?? string.Empty);
            command.Parameters.AddWithValue("@service", info.Service ?? string.Empty);
            command.Parameters.AddWithValue("@userId", info.UserId);
            command.Parameters.AddWithValue("@password", EncryptPassword(info.Password));
            command.Parameters.AddWithValue("@sqlQuery", info.SQLQuery ?? string.Empty);
            command.Parameters.AddWithValue("@isActive", info.IsActive ? 1 : 0);
            command.Parameters.AddWithValue("@isFavorite", info.IsFavorite ? 1 : 0);

            command.ExecuteNonQuery();
        }

        /// <summary>
        /// 접속 정보 삭제
        /// </summary>
        public void DeleteConnection(int id)
        {
            using var connection = new SqliteConnection($"Data Source={_dbPath}");
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM Connections WHERE Id = @id";
            command.Parameters.AddWithValue("@id", id);

            command.ExecuteNonQuery();
        }

        /// <summary>
        /// 데이터베이스 파일 경로 가져오기
        /// </summary>
        public string GetDatabasePath() => _dbPath;

        /// <summary>
        /// 기본 데이터베이스 경로를 반환합니다 (정적 메서드).
        /// QueryDatabase와 동일한 경로 사용
        /// </summary>
        public static string GetDefaultDatabasePath()
        {
            // 🔥 프로그램 실행 파일이 있는 디렉토리 경로
            var exeDirectory = AppDomain.CurrentDomain.BaseDirectory;
            return Path.Combine(exeDirectory, "FACTOVA_QueryHelper.db");
        }
    }
}
