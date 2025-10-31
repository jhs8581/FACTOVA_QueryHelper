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
    /// GitHub Releases�� ���� �ڵ� ������Ʈ Ȯ�� Ŭ����
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
        
        // ĳ�� ��ȿ �ð� (1�ð�)
        private static readonly TimeSpan CacheValidDuration = TimeSpan.FromHours(1);

        // ?? Rate Limit ���� ����
        private static RateLimitInfo? _lastRateLimitInfo;

        static UpdateChecker()
        {
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "FACTOVA_QueryHelper");
            _httpClient.Timeout = TimeSpan.FromSeconds(10); // Ÿ�Ӿƿ� ����
        }

        /// <summary>
        /// ���������� ���� Rate Limit ������ ��ȯ�մϴ�.
        /// </summary>
        public static RateLimitInfo? GetLastRateLimitInfo()
        {
            return _lastRateLimitInfo;
        }

        /// <summary>
        /// �ֽ� ������ Ȯ���մϴ�.
        /// </summary>
        public static async Task<UpdateInfo> CheckForUpdatesAsync(bool forceCheck = false)
        {
            // ���� Ȯ���� �ƴϸ� ĳ�� Ȯ��
            if (!forceCheck)
            {
                var cachedInfo = LoadCache();
                if (cachedInfo != null && (DateTime.Now - cachedInfo.CheckTime) < CacheValidDuration)
                {
                    System.Diagnostics.Debug.WriteLine($"[UpdateChecker] ĳ�� ��� (������ Ȯ��: {cachedInfo.CheckTime:HH:mm:ss})");
                    return cachedInfo.UpdateInfo;
                }
            }

            try
            {
                System.Diagnostics.Debug.WriteLine($"[UpdateChecker] ������Ʈ Ȯ�� ����: {UpdateUrl}");
                
                var response = await _httpClient.GetAsync(UpdateUrl);
                
                // ?? Rate Limit ���� ����
                ExtractRateLimitInfo(response);
                
                System.Diagnostics.Debug.WriteLine($"[UpdateChecker] ���� �ڵ�: {(int)response.StatusCode} {response.StatusCode}");
                System.Diagnostics.Debug.WriteLine($"[UpdateChecker] ���� ����: {response.IsSuccessStatusCode}");
                
                // ���� �ڵ� Ȯ��
                if (!response.IsSuccessStatusCode)
                {
                    var statusCode = (int)response.StatusCode;
                    var errorContent = await response.Content.ReadAsStringAsync();
                    
                    System.Diagnostics.Debug.WriteLine($"[UpdateChecker] ���� ����: {errorContent}");
                    
                    // ����ڿ��� �� ģ���� �޽���
                    string errorMessage = statusCode switch
                    {
                        403 => "GitHub API ��û ���ѿ� �����߽��ϴ�. ��� �� �ٽ� �õ����ּ���.\n(�� 1�ð� �� �ڵ� ����)",
                        404 => "����� ã�� �� �����ϴ�. ����ҿ� ����� �ִ��� Ȯ���ϼ���.",
                        _ => $"������Ʈ Ȯ�� ���� (HTTP {statusCode})"
                    };
                    
                    var result = new UpdateInfo 
                    { 
                        HasUpdate = false, 
                        ErrorMessage = errorMessage
                    };
                    
                    // �����ص� ĳ�� ���� (��õ� ����)
                    SaveCache(result);
                    
                    return result;
                }
                
                var json = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"[UpdateChecker] ���� ����: {json.Length} bytes");
                
                var release = JsonSerializer.Deserialize<GitHubRelease>(json);

                if (release == null)
                {
                    System.Diagnostics.Debug.WriteLine("[UpdateChecker] ������ �Ľ� ����");
                    return new UpdateInfo { HasUpdate = false };
                }

                System.Diagnostics.Debug.WriteLine($"[UpdateChecker] �ֽ� ����: {release.TagName}");
                
                var currentVersion = GetCurrentVersion();
                var latestVersion = ParseVersion(release.TagName);

                System.Diagnostics.Debug.WriteLine($"[UpdateChecker] ���� ����: {currentVersion}, �ֽ� ����: {latestVersion}");

                UpdateInfo updateInfo;
                
                if (latestVersion > currentVersion)
                {
                    // .exe ���� ã��
                    string? downloadUrl = null;
                    foreach (var asset in release.Assets)
                    {
                        if (asset.Name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                        {
                            downloadUrl = asset.BrowserDownloadUrl;
                            System.Diagnostics.Debug.WriteLine($"[UpdateChecker] �ٿ�ε� URL: {downloadUrl}");
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
                    System.Diagnostics.Debug.WriteLine("[UpdateChecker] �̹� �ֽ� ���� ��� ��");
                    updateInfo = new UpdateInfo { HasUpdate = false };
                }
                
                // ���� �� ĳ�� ����
                SaveCache(updateInfo);
                
                return updateInfo;
            }
            catch (HttpRequestException ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UpdateChecker] ��Ʈ��ũ ����: {ex.Message}");
                return new UpdateInfo 
                { 
                    HasUpdate = false, 
                    ErrorMessage = "��Ʈ��ũ ������ Ȯ�����ּ���." 
                };
            }
            catch (TaskCanceledException)
            {
                System.Diagnostics.Debug.WriteLine("[UpdateChecker] ������Ʈ Ȯ�� �ð� �ʰ�");
                return new UpdateInfo 
                { 
                    HasUpdate = false, 
                    ErrorMessage = "������Ʈ Ȯ�� �ð� �ʰ�" 
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UpdateChecker] ���� �߻�: {ex.GetType().Name} - {ex.Message}");
                return new UpdateInfo { HasUpdate = false, ErrorMessage = $"����: {ex.Message}" };
            }
        }

        /// <summary>
        /// ���� ������� Rate Limit ������ �����մϴ�.
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
                    $"(����: {rateLimitInfo.ResetTime:HH:mm:ss})");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UpdateChecker] Rate Limit ���� ���� ����: {ex.Message}");
            }
        }

        /// <summary>
        /// ĳ�� ����
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
                
                System.Diagnostics.Debug.WriteLine($"[UpdateChecker] ĳ�� ����: {CacheFilePath}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UpdateChecker] ĳ�� ���� ����: {ex.Message}");
            }
        }

        /// <summary>
        /// ĳ�� �ε�
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
                System.Diagnostics.Debug.WriteLine($"[UpdateChecker] ĳ�� �ε� ����: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// ���� ���ø����̼� ������ �����ɴϴ�.
        /// </summary>
        private static Version GetCurrentVersion()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version;
            return version ?? new Version(1, 0, 0);
        }

        /// <summary>
        /// GitHub �±� �̸����� ������ �Ľ��մϴ�. (��: "v1.0.0" -> Version(1,0,0))
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
    /// Rate Limit ����
    /// </summary>
    public class RateLimitInfo
    {
        public int Limit { get; set; } = 60;
        public int Remaining { get; set; } = 0;
        public DateTime ResetTime { get; set; }

        public int MinutesUntilReset => Math.Max(0, (int)(ResetTime - DateTime.Now).TotalMinutes);
    }

    /// <summary>
    /// ������Ʈ ĳ��
    /// </summary>
    public class UpdateCache
    {
        [JsonPropertyName("check_time")]
        public DateTime CheckTime { get; set; }
        
        [JsonPropertyName("update_info")]
        public UpdateInfo UpdateInfo { get; set; } = new UpdateInfo();
    }

    /// <summary>
    /// ������Ʈ ����
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
    /// GitHub Release API ���� ��
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
    /// GitHub Release Asset ��
    /// </summary>
    public class GitHubReleaseAsset
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("browser_download_url")]
        public string BrowserDownloadUrl { get; set; } = string.Empty;
    }
}
