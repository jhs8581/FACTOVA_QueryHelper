using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using FACTOVA_QueryHelper.Models;

namespace FACTOVA_QueryHelper.Services
{
    /// <summary>
    /// 4000개 테이블의 메타데이터를 로컬에 캐싱하는 서비스
    /// </summary>
    public class MetadataCacheService
    {
        private static readonly string CacheDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "FACTOVA_QueryHelper",
            "MetadataCache"
        );

        private readonly string _cacheFilePath;
        private readonly OracleDbService _dbService;
        private DatabaseMetadata? _metadata;

        public MetadataCacheService(OracleDbService dbService, string databaseIdentifier)
        {
            _dbService = dbService;
            _cacheFilePath = Path.Combine(CacheDirectory, $"{databaseIdentifier}_metadata.json");
            
            // 캐시 디렉토리 생성
            Directory.CreateDirectory(CacheDirectory);
        }

        /// <summary>
        /// 캐시 파일이 존재하는지 확인
        /// </summary>
        public bool CacheExists() => File.Exists(_cacheFilePath);

        /// <summary>
        /// 캐시 파일의 마지막 수정 시간
        /// </summary>
        public DateTime? GetCacheDate()
        {
            if (!CacheExists()) return null;
            return File.GetLastWriteTime(_cacheFilePath);
        }

        /// <summary>
        /// 로컬 캐시에서 메타데이터 로드
        /// </summary>
        public async Task<DatabaseMetadata?> LoadFromCacheAsync()
        {
            try
            {
                if (!CacheExists())
                {
return null;
                }
var json = await File.ReadAllTextAsync(_cacheFilePath);

                
                // 🔥 저장할 때와 동일한 JsonSerializerOptions 사용!
                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    PropertyNameCaseInsensitive = true // 대소문자 구분 없이 읽기
                };
                
                _metadata = JsonSerializer.Deserialize<DatabaseMetadata>(json, options);

                if (_metadata != null)
                {

                    
                    // 🔥 첫 5개 테이블 이름 확인
                    if (_metadata.Tables.Count > 0)
                    {
int count = 0;
                        foreach (var tableName in _metadata.Tables.Keys.Take(5))
                        {
count++;
                        }
                    }
                }
                else
                {
}

                return _metadata;
            }
            catch (JsonException jsonEx)
            {

return null;
            }
            catch (Exception ex)
            {

                return null;
            }
        }

        /// <summary>
        /// 데이터베이스에서 모든 메타데이터를 가져와서 캐시에 저장 (4000개 테이블 대응)
        /// </summary>
        public async Task<DatabaseMetadata> BuildAndSaveCacheAsync(IProgress<CacheBuildProgress>? progress = null)
        {
_metadata = new DatabaseMetadata
            {
                CachedDate = DateTime.Now,
                Tables = new Dictionary<string, TableMetadata>()
            };

            try
            {
                // 1단계: 테이블 목록 가져오기
                progress?.Report(new CacheBuildProgress { Stage = "테이블 목록 조회 중...", CurrentTable = 0, TotalTables = 0 });
                var tableNames = await _dbService.GetTablesAsync();
// 2단계: 각 테이블의 컬럼과 인덱스 정보 가져오기
                int processedCount = 0;
                foreach (var tableName in tableNames)
                {
                    try
                    {
                        processedCount++;
                        progress?.Report(new CacheBuildProgress 
                        { 
                            Stage = $"테이블 메타데이터 수집 중...",
                            CurrentTable = processedCount,
                            TotalTables = tableNames.Count,
                            CurrentTableName = tableName
                        });

                        var columns = await _dbService.GetTableColumnsAsync(tableName);
                        var indexes = await _dbService.GetTableIndexesAsync(tableName);

                        _metadata.Tables[tableName] = new TableMetadata
                        {
                            TableName = tableName,
                            Columns = columns.Select(c => new ColumnMetadata
                            {
                                ColumnName = c.ColumnName,
                                DataType = c.DataType,
                                Nullable = c.Nullable,
                                Comments = c.Comments
                            }).ToList(),
                            Indexes = indexes.Select(i => new IndexMetadata
                            {
                                Type = i.Type,
                                Columns = i.Columns
                            }).ToList()
                        };

                        // 100개마다 진행 상황 로그
                        if (processedCount % 100 == 0)
                        {
}
                    }
                    catch (Exception ex)
                    {
// 개별 테이블 오류는 무시하고 계속 진행
                    }
                }

                // 3단계: JSON 파일로 저장
                progress?.Report(new CacheBuildProgress 
                { 
                    Stage = "캐시 파일 저장 중...",
                    CurrentTable = processedCount,
                    TotalTables = tableNames.Count
                });

                await SaveCacheAsync();

if (File.Exists(_cacheFilePath))
                {
                    
                }

                return _metadata;
            }
            catch (Exception ex)
            {
throw;
            }
        }

        /// <summary>
        /// 현재 메타데이터를 캐시 파일로 저장
        /// </summary>
        private async Task SaveCacheAsync()
        {
            if (_metadata == null)
                throw new InvalidOperationException("No metadata to save");

            var options = new JsonSerializerOptions
            {
                WriteIndented = false, // 파일 크기 최소화
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var json = JsonSerializer.Serialize(_metadata, options);
            await File.WriteAllTextAsync(_cacheFilePath, json);
        }

        /// <summary>
        /// 특정 테이블의 컬럼 정보 가져오기 (캐시에서)
        /// </summary>
        public List<ColumnInfo>? GetTableColumns(string tableName)
        {
            if (_metadata == null || !_metadata.Tables.ContainsKey(tableName))
                return null;

            var tableMetadata = _metadata.Tables[tableName];
            return tableMetadata.Columns.Select(c => new ColumnInfo
            {
                ColumnName = c.ColumnName,
                DataType = c.DataType,
                Nullable = c.Nullable,
                Comments = c.Comments
            }).ToList();
        }

        /// <summary>
        /// 특정 테이블의 인덱스 정보 가져오기 (캐시에서)
        /// </summary>
        public List<IndexInfo>? GetTableIndexes(string tableName)
        {
            if (_metadata == null || !_metadata.Tables.ContainsKey(tableName))
                return null;

            var tableMetadata = _metadata.Tables[tableName];
            return tableMetadata.Indexes.Select(i => new IndexInfo
            {
                Type = i.Type,
                Columns = i.Columns
            }).ToList();
        }

        /// <summary>
        /// 모든 테이블 이름 가져오기 (캐시에서)
        /// </summary>
        public List<string> GetAllTableNames()
        {
            return _metadata?.Tables.Keys.OrderBy(t => t).ToList() ?? new List<string>();
        }

        /// <summary>
        /// 캐시 파일 삭제
        /// </summary>
        public void ClearCache()
        {
            if (File.Exists(_cacheFilePath))
            {
                File.Delete(_cacheFilePath);
}
            _metadata = null;
        }

        /// <summary>
        /// 캐시 파일 정보
        /// </summary>
        public CacheInfo GetCacheInfo()
        {
            var info = new CacheInfo
            {
                Exists = CacheExists(),
                TableCount = _metadata?.Tables.Count ?? 0,
                CacheFilePath = _cacheFilePath  // 캐시 파일 경로 추가
            };

            if (info.Exists)
            {
                var fileInfo = new FileInfo(_cacheFilePath);
                info.LastModified = fileInfo.LastWriteTime;
                info.FileSizeMB = fileInfo.Length / 1024.0 / 1024.0;
            }

            return info;
        }

        /// <summary>
        /// 캐시 디렉토리 경로 반환
        /// </summary>
        public static string GetCacheDirectory()
        {
            return CacheDirectory;
        }
    }

    // ==================== 모델 클래스 ====================

    /// <summary>
    /// 전체 데이터베이스 메타데이터
    /// </summary>
    public class DatabaseMetadata
    {
        public DateTime CachedDate { get; set; }
        public Dictionary<string, TableMetadata> Tables { get; set; } = new();
    }

    /// <summary>
    /// 테이블 메타데이터
    /// </summary>
    public class TableMetadata
    {
        public string TableName { get; set; } = string.Empty;
        public List<ColumnMetadata> Columns { get; set; } = new();
        public List<IndexMetadata> Indexes { get; set; } = new();
    }

    /// <summary>
    /// 컬럼 메타데이터 (JSON 직렬화용)
    /// </summary>
    public class ColumnMetadata
    {
        public string ColumnName { get; set; } = string.Empty;
        public string DataType { get; set; } = string.Empty;
        public string Nullable { get; set; } = string.Empty;
        public string Comments { get; set; } = string.Empty;
    }

    /// <summary>
    /// 인덱스 메타데이터 (JSON 직렬화용)
    /// </summary>
    public class IndexMetadata
    {
        public string Type { get; set; } = string.Empty;
        public string Columns { get; set; } = string.Empty;
    }

    /// <summary>
    /// 캐시 빌드 진행 상황
    /// </summary>
    public class CacheBuildProgress
    {
        public string Stage { get; set; } = string.Empty;
        public int CurrentTable { get; set; }
        public int TotalTables { get; set; }
        public string CurrentTableName { get; set; } = string.Empty;
        public double PercentComplete => TotalTables > 0 ? (CurrentTable * 100.0 / TotalTables) : 0;
    }

    /// <summary>
    /// 캐시 정보
    /// </summary>
    public class CacheInfo
    {
        public bool Exists { get; set; }
        public DateTime? LastModified { get; set; }
        public int TableCount { get; set; }
        public double FileSizeMB { get; set; }
        public string CacheFilePath { get; set; } = string.Empty;  // 캐시 파일 경로 추가
    }
}
