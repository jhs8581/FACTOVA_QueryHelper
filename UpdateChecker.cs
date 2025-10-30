using System;
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

        static UpdateChecker()
        {
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "FACTOVA_QueryHelper");
            _httpClient.Timeout = TimeSpan.FromSeconds(10); // 타임아웃 설정
        }

        /// <summary>
        /// 최신 버전을 확인합니다.
        /// </summary>
        public static async Task<UpdateInfo> CheckForUpdatesAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync(UpdateUrl);
                
                // 상태 코드 확인
                if (!response.IsSuccessStatusCode)
                {
                    var statusCode = (int)response.StatusCode;
                    System.Diagnostics.Debug.WriteLine($"GitHub API 응답 실패: {statusCode} {response.ReasonPhrase}");
                    
                    return new UpdateInfo 
                    { 
                        HasUpdate = false, 
                        ErrorMessage = $"업데이트 확인 실패 (HTTP {statusCode})" 
                    };
                }
                
                var json = await response.Content.ReadAsStringAsync();
                var release = JsonSerializer.Deserialize<GitHubRelease>(json);

                if (release == null)
                {
                    return new UpdateInfo { HasUpdate = false };
                }

                var currentVersion = GetCurrentVersion();
                var latestVersion = ParseVersion(release.TagName);

                if (latestVersion > currentVersion)
                {
                    // .exe 파일 찾기
                    string? downloadUrl = null;
                    foreach (var asset in release.Assets)
                    {
                        if (asset.Name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                        {
                            downloadUrl = asset.BrowserDownloadUrl;
                            break;
                        }
                    }

                    return new UpdateInfo
                    {
                        HasUpdate = true,
                        LatestVersion = release.TagName,
                        CurrentVersion = $"v{currentVersion}",
                        DownloadUrl = downloadUrl ?? release.HtmlUrl,
                        ReleaseNotes = release.Body
                    };
                }

                return new UpdateInfo { HasUpdate = false };
            }
            catch (HttpRequestException ex)
            {
                System.Diagnostics.Debug.WriteLine($"네트워크 오류: {ex.Message}");
                return new UpdateInfo 
                { 
                    HasUpdate = false, 
                    ErrorMessage = "네트워크 연결을 확인해주세요." 
                };
            }
            catch (TaskCanceledException)
            {
                System.Diagnostics.Debug.WriteLine("업데이트 확인 시간 초과");
                return new UpdateInfo 
                { 
                    HasUpdate = false, 
                    ErrorMessage = "업데이트 확인 시간 초과" 
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"업데이트 확인 실패: {ex.Message}");
                return new UpdateInfo { HasUpdate = false, ErrorMessage = $"오류: {ex.Message}" };
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
