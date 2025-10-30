using System;
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

        static UpdateChecker()
        {
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "FACTOVA_QueryHelper");
            _httpClient.Timeout = TimeSpan.FromSeconds(10); // Ÿ�Ӿƿ� ����
        }

        /// <summary>
        /// �ֽ� ������ Ȯ���մϴ�.
        /// </summary>
        public static async Task<UpdateInfo> CheckForUpdatesAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync(UpdateUrl);
                
                // ���� �ڵ� Ȯ��
                if (!response.IsSuccessStatusCode)
                {
                    var statusCode = (int)response.StatusCode;
                    System.Diagnostics.Debug.WriteLine($"GitHub API ���� ����: {statusCode} {response.ReasonPhrase}");
                    
                    return new UpdateInfo 
                    { 
                        HasUpdate = false, 
                        ErrorMessage = $"������Ʈ Ȯ�� ���� (HTTP {statusCode})" 
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
                    // .exe ���� ã��
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
                System.Diagnostics.Debug.WriteLine($"��Ʈ��ũ ����: {ex.Message}");
                return new UpdateInfo 
                { 
                    HasUpdate = false, 
                    ErrorMessage = "��Ʈ��ũ ������ Ȯ�����ּ���." 
                };
            }
            catch (TaskCanceledException)
            {
                System.Diagnostics.Debug.WriteLine("������Ʈ Ȯ�� �ð� �ʰ�");
                return new UpdateInfo 
                { 
                    HasUpdate = false, 
                    ErrorMessage = "������Ʈ Ȯ�� �ð� �ʰ�" 
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"������Ʈ Ȯ�� ����: {ex.Message}");
                return new UpdateInfo { HasUpdate = false, ErrorMessage = $"����: {ex.Message}" };
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
