using System;
using System.Collections.Generic;
using System.Data;
using FACTOVA_QueryHelper.Models;
using Microsoft.Data.Sqlite;

namespace FACTOVA_QueryHelper.Services
{
    /// <summary>
    /// 테이블 단축어 서비스
    /// </summary>
    public class TableShortcutService
    {
        private readonly string _databasePath;

        public TableShortcutService(string? databasePath = null)
        {
            _databasePath = databasePath ?? Database.QueryDatabase.GetDefaultDatabasePath();
            InitializeDatabase();
        }

        /// <summary>
        /// 데이터베이스 초기화 (테이블 생성)
        /// </summary>
        private void InitializeDatabase()
        {
            try
            {
                using var connection = new SqliteConnection($"Data Source={_databasePath}");
                connection.Open();

                var createTableSql = @"
                    CREATE TABLE IF NOT EXISTS TableShortcuts (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Shortcut TEXT NOT NULL,
                        TableName TEXT NOT NULL,
                        Description TEXT,
                        UNIQUE(Shortcut)
                    )";

                using var command = new SqliteCommand(createTableSql, connection);
                command.ExecuteNonQuery();
}
            catch (Exception ex)
            {
}
        }

        /// <summary>
        /// 모든 테이블 단축어 조회
        /// </summary>
        public List<TableShortcut> GetAll()
        {
            var shortcuts = new List<TableShortcut>();

            try
            {
                using var connection = new SqliteConnection($"Data Source={_databasePath}");
                connection.Open();

                var sql = "SELECT Shortcut, FullTableName, Description FROM TableShortcuts ORDER BY Shortcut";
                using var command = new SqliteCommand(sql, connection);
                using var reader = command.ExecuteReader();

                while (reader.Read())
                {
                    shortcuts.Add(new TableShortcut
                    {
                        Shortcut = reader.GetString(0),
                        FullTableName = reader.GetString(1),
                        Description = reader.IsDBNull(2) ? string.Empty : reader.GetString(2)
                    });
                }
}
            catch (Exception ex)
            {
}

            return shortcuts;
        }

        /// <summary>
        /// 단축어로 테이블명 조회
        /// </summary>
        public string? GetTableName(string shortcut)
        {
            try
            {
                using var connection = new SqliteConnection($"Data Source={_databasePath}");
                connection.Open();

                var sql = "SELECT TableName FROM TableShortcuts WHERE Shortcut = @Shortcut";
                using var command = new SqliteCommand(sql, connection);
                command.Parameters.AddWithValue("@Shortcut", shortcut);

                var result = command.ExecuteScalar();
                return result?.ToString();
            }
            catch (Exception ex)
            {
return null;
            }
        }

        /// <summary>
        /// 테이블 단축어 추가
        /// </summary>
        public void Add(TableShortcut shortcut)
        {
            if (string.IsNullOrWhiteSpace(shortcut.Shortcut))
                throw new ArgumentException("단축어는 필수입니다.");
            if (string.IsNullOrWhiteSpace(shortcut.FullTableName))
                throw new ArgumentException("테이블명은 필수입니다.");

            using var connection = new SqliteConnection($"Data Source={_databasePath}");
            connection.Open();

            var sql = "INSERT INTO TableShortcuts (Shortcut, FullTableName, Description) VALUES (@shortcut, @tableName, @description)";
            using var command = new SqliteCommand(sql, connection);
            command.Parameters.AddWithValue("@shortcut", shortcut.Shortcut);
            command.Parameters.AddWithValue("@tableName", shortcut.FullTableName);
            command.Parameters.AddWithValue("@description", shortcut.Description ?? string.Empty);
            command.ExecuteNonQuery();
        }

        /// <summary>
        /// 테이블 단축어 수정
        /// </summary>
        public void Update(TableShortcut shortcut)
        {
            if (string.IsNullOrWhiteSpace(shortcut.Shortcut))
                throw new ArgumentException("단축어는 필수입니다.");
            if (string.IsNullOrWhiteSpace(shortcut.FullTableName))
                throw new ArgumentException("테이블명은 필수입니다.");

            using var connection = new SqliteConnection($"Data Source={_databasePath}");
            connection.Open();

            var sql = "UPDATE TableShortcuts SET FullTableName = @tableName, Description = @description WHERE Shortcut = @shortcut";
            using var command = new SqliteCommand(sql, connection);
            command.Parameters.AddWithValue("@shortcut", shortcut.Shortcut);
            command.Parameters.AddWithValue("@tableName", shortcut.FullTableName);
            command.Parameters.AddWithValue("@description", shortcut.Description ?? string.Empty);
            command.ExecuteNonQuery();
        }

        /// <summary>
        /// 테이블 단축어 삭제
        /// </summary>
        public void Delete(string shortcut)
        {
            using var connection = new SqliteConnection($"Data Source={_databasePath}");
            connection.Open();

            var sql = "DELETE FROM TableShortcuts WHERE Shortcut = @shortcut";
            using var command = new SqliteCommand(sql, connection);
            command.Parameters.AddWithValue("@shortcut", shortcut);
            command.ExecuteNonQuery();
        }
    }
}
