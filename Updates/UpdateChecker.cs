using System;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace FACTOVA_QueryHelper
{
    /// <summary>
    /// GitHub Releases를 통한 자동 업데이트 확인 클래스
    /// </summary>
    public class UpdateChecker
    {
        private const string UpdateUrl = "https://api.github.com/repos/jhs8581/FACTOVA_QueryHelper/releases/latest";
        private static readonly HttpClient _httpClient = new HttpClient();
        private static readonly string CacheFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FACTOVA_QueryHelper",
            "update_cache.json"
        );
        
        // 캐시 유효 시간 (1시간)
        private static readonly TimeSpan CacheValidDuration = TimeSpan.FromHours(1);

        // ?? Rate Limit 정보 저장
        private static RateLimitInfo? _lastRateLimitInfo;

        static UpdateChecker()
        {
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "FACTOVA_QueryHelper");
            _httpClient.Timeout = TimeSpan.FromSeconds(10); // 타임아웃 설정
        }

        /// <summary>
        /// 마지막으로 받은 Rate Limit 정보를 반환합니다.
        /// </summary>
        public static RateLimitInfo? GetLastRateLimitInfo()
        {
            return _lastRateLimitInfo;
        }

        /// <summary>
        /// 최신 버전을 확인합니다.
        /// </summary>
        public static async Task<UpdateInfo> CheckForUpdatesAsync(bool forceCheck = false)
        {
            // 강제 확인이 아니면 캐시 확인
            if (!forceCheck)
            {
                var cachedInfo = LoadCache();
                if (cachedInfo != null && (DateTime.Now - cachedInfo.CheckTime) < CacheValidDuration)
                {
                    System.Diagnostics.Debug.WriteLine($"[UpdateChecker] 캐시 사용 (마지막 확인: {cachedInfo.CheckTime:HH:mm:ss})");
                    return cachedInfo.UpdateInfo;
                }
            }

            try
            {
                System.Diagnostics.Debug.WriteLine($"[UpdateChecker] 업데이트 확인 시작: {UpdateUrl}");
                
                var response = await _httpClient.GetAsync(UpdateUrl);
                
                // ?? Rate Limit 정보 추출
                ExtractRateLimitInfo(response);
                
                System.Diagnostics.Debug.WriteLine($"[UpdateChecker] 응답 코드: {(int)response.StatusCode} {response.StatusCode}");
                System.Diagnostics.Debug.WriteLine($"[UpdateChecker] 성공 여부: {response.IsSuccessStatusCode}");
                
                // 상태 코드 확인
                if (!response.IsSuccessStatusCode)
                {
                    var statusCode = (int)response.StatusCode;
                    var errorContent = await response.Content.ReadAsStringAsync();
                    
                    System.Diagnostics.Debug.WriteLine($"[UpdateChecker] 오류 응답: {errorContent}");
                    
                    // 사용자에게 더 친절한 메시지
                    string errorMessage = statusCode switch
                    {
                        403 => "GitHub API 요청 제한에 도달했습니다. 잠시 후 다시 시도해주세요.\n(약 1시간 후 자동 복구)",
                        404 => "릴리즈를 찾을 수 없습니다. 저장소에 릴리즈가 있는지 확인하세요.",
                        _ => $"업데이트 확인 실패 (HTTP {statusCode})"
                    };
                    
                    var result = new UpdateInfo 
                    { 
                        HasUpdate = false, 
                        ErrorMessage = errorMessage
                    };
                    
                    // 실패해도 캐시 저장 (재시도 방지)
                    SaveCache(result);
                    
                    return result;
                }
                
                var json = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"[UpdateChecker] 응답 길이: {json.Length} bytes");
                
                var release = JsonSerializer.Deserialize<GitHubRelease>(json);

                if (release == null)
                {
                    System.Diagnostics.Debug.WriteLine("[UpdateChecker] 릴리즈 파싱 실패");
                    return new UpdateInfo { HasUpdate = false };
                }

                System.Diagnostics.Debug.WriteLine($"[UpdateChecker] 최신 버전: {release.TagName}");
                
                var currentVersion = GetCurrentVersion();
                var latestVersion = ParseVersion(release.TagName);

                System.Diagnostics.Debug.WriteLine($"[UpdateChecker] 현재 버전: {currentVersion}, 최신 버전: {latestVersion}");

                UpdateInfo updateInfo;
                
                if (latestVersion > currentVersion)
                {
                    // .exe 파일 찾기
                    string? downloadUrl = null;
                    foreach (var asset in release.Assets)
                    {
                        if (asset.Name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                        {
                            downloadUrl = asset.BrowserDownloadUrl;
                            System.Diagnostics.Debug.WriteLine($"[UpdateChecker] 다운로드 URL: {downloadUrl}");
                            break;
                        }
                    }

                    updateInfo = new UpdateInfo
                    {
                        HasUpdate = true,
                        LatestVersion = release.TagName,
                        CurrentVersion = $"v{currentVersion}",
                        DownloadUrl = downloadUrl ?? release.HtmlUrl,
                        ReleaseNotes = release.Body
                    };
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[UpdateChecker] 이미 최신 버전 사용 중");
                    updateInfo = new UpdateInfo { HasUpdate = false };
                }
                
                // 성공 시 캐시 저장
                SaveCache(updateInfo);
                
                return updateInfo;
            }
            catch (HttpRequestException ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UpdateChecker] 네트워크 오류: {ex.Message}");
                return new UpdateInfo 
                { 
                    HasUpdate = false, 
                    ErrorMessage = "네트워크 연결을 확인해주세요." 
                };
            }
            catch (TaskCanceledException)
            {
                System.Diagnostics.Debug.WriteLine("[UpdateChecker] 업데이트 확인 시간 초과");
                return new UpdateInfo 
                { 
                    HasUpdate = false, 
                    ErrorMessage = "업데이트 확인 시간 초과" 
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UpdateChecker] 예외 발생: {ex.GetType().Name} - {ex.Message}");
                return new UpdateInfo { HasUpdate = false, ErrorMessage = $"오류: {ex.Message}" };
            }
        }

        /// <summary>
        /// 응답 헤더에서 Rate Limit 정보를 추출합니다.
        /// </summary>
        private static void ExtractRateLimitInfo(HttpResponseMessage response)
        {
            try
            {
                var rateLimitInfo = new RateLimitInfo();

                if (response.Headers.TryGetValues("X-RateLimit-Limit", out var limitValues))
                {
                    if (int.TryParse(limitValues.FirstOrDefault(), out int limit))
                        rateLimitInfo.Limit = limit;
                }

                if (response.Headers.TryGetValues("X-RateLimit-Remaining", out var remainingValues))
                {
                    if (int.TryParse(remainingValues.FirstOrDefault(), out int remaining))
                        rateLimitInfo.Remaining = remaining;
                }

                if (response.Headers.TryGetValues("X-RateLimit-Reset", out var resetValues))
                {
                    if (long.TryParse(resetValues.FirstOrDefault(), out long resetTimestamp))
                    {
                        rateLimitInfo.ResetTime = DateTimeOffset.FromUnixTimeSeconds(resetTimestamp).LocalDateTime;
                    }
                }

                _lastRateLimitInfo = rateLimitInfo;

                System.Diagnostics.Debug.WriteLine($"[UpdateChecker] Rate Limit: {rateLimitInfo.Remaining}/{rateLimitInfo.Limit} " +
                    $"(리셋: {rateLimitInfo.ResetTime:HH:mm:ss})");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UpdateChecker] Rate Limit 정보 추출 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// 캐시 저장
        /// </summary>
        private static void SaveCache(UpdateInfo updateInfo)
        {
            try
            {
                var cache = new UpdateCache
                {
                    CheckTime = DateTime.Now,
                    UpdateInfo = updateInfo
                };
                
                var directory = Path.GetDirectoryName(CacheFilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                var json = JsonSerializer.Serialize(cache, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(CacheFilePath, json);
                
                System.Diagnostics.Debug.WriteLine($"[UpdateChecker] 캐시 저장: {CacheFilePath}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UpdateChecker] 캐시 저장 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// 캐시 로드
        /// </summary>
        private static UpdateCache? LoadCache()
        {
            try
            {
                if (!File.Exists(CacheFilePath))
                {
                    return null;
                }
                
                var json = File.ReadAllText(CacheFilePath);
                var cache = JsonSerializer.Deserialize<UpdateCache>(json);
                
                return cache;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UpdateChecker] 캐시 로드 실패: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 현재 애플리케이션 버전을 가져옵니다.
        /// </summary>
        private static Version GetCurrentVersion()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version;
            return version ?? new Version(1, 0, 0);
        }

        /// <summary>
        /// GitHub 태그 이름에서 버전을 파싱합니다. (예: "v1.0.0" -> Version(1,0,0))
        /// </summary>
        private static Version ParseVersion(string tagName)
        {
            var versionString = tagName.TrimStart('v', 'V');
            if (Version.TryParse(versionString, out var version))
            {
                return version;
            }
            return new Version(0, 0, 0);
        }
    }

    /// <summary>
    /// Rate Limit 정보
    /// </summary>
    public class RateLimitInfo
    {
        public int Limit { get; set; } = 60;
        public int Remaining { get; set; } = 0;
        public DateTime ResetTime { get; set; }

        public int MinutesUntilReset => Math.Max(0, (int)(ResetTime - DateTime.Now).TotalMinutes);
    }

    /// <summary>
    /// 업데이트 캐시
    /// </summary>
    public class UpdateCache
    {
        [JsonPropertyName("check_time")]
        public DateTime CheckTime { get; set; }
        
        [JsonPropertyName("update_info")]
        public UpdateInfo UpdateInfo { get; set; } = new UpdateInfo();
    }

    /// <summary>
    /// 업데이트 정보
    /// </summary>
    public class UpdateInfo
    {
        public bool HasUpdate { get; set; }
        public string CurrentVersion { get; set; } = string.Empty;
        public string LatestVersion { get; set; } = string.Empty;
        public string DownloadUrl { get; set; } = string.Empty;
        public string ReleaseNotes { get; set; } = string.Empty;
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// GitHub Release API 응답 모델
    /// </summary>
    public class GitHubRelease
    {
        [JsonPropertyName("tag_name")]
        public string TagName { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("body")]
        public string Body { get; set; } = string.Empty;

        [JsonPropertyName("html_url")]
        public string HtmlUrl { get; set; } = string.Empty;

        [JsonPropertyName("assets")]
        public GitHubReleaseAsset[] Assets { get; set; } = Array.Empty<GitHubReleaseAsset>();
    }

    /// <summary>
    /// GitHub Release Asset 모델
    /// </summary>
    public class GitHubReleaseAsset
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("browser_download_url")]
        public string BrowserDownloadUrl { get; set; } = string.Empty;
    }
}
