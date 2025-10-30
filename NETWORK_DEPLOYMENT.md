# �系 ��Ʈ��ũ ���� ���̵�

## ����
Private GitHub ����� ��� **�系 ��Ʈ��ũ ���� ����**�� ����� ���� ����Դϴ�.

## ���� ���� ����

```
\\����\FACTOVA_Apps\QueryHelper\
������ latest\                    # �ֽ� ����
��   ������ FACTOVA_QueryHelper.exe
��   ������ version.txt           # ���� ����
������ v1.0.0\                   # ������ ���
��   ������ FACTOVA_QueryHelper.exe
������ v1.1.0\
    ������ FACTOVA_QueryHelper.exe
```

## �ڵ� ���� ��ũ��Ʈ

### deploy.ps1 (PowerShell)
```powershell
# ����
$version = "1.0.0"
$deployPath = "\\����\FACTOVA_Apps\QueryHelper"
$latestPath = "$deployPath\latest"
$versionPath = "$deployPath\v$version"

# ����
Write-Host "Building version $version..." -ForegroundColor Cyan
dotnet publish -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -p:EnableCompressionInSingleFile=true `
  --output ./publish

# ����
Write-Host "Deploying to network share..." -ForegroundColor Cyan

# ������ ���� ���� �� ����
New-Item -ItemType Directory -Force -Path $versionPath | Out-Null
Copy-Item "./publish/FACTOVA_QueryHelper.exe" -Destination $versionPath -Force

# latest ���� ������Ʈ
New-Item -ItemType Directory -Force -Path $latestPath | Out-Null
Copy-Item "./publish/FACTOVA_QueryHelper.exe" -Destination $latestPath -Force

# ���� ���� ���� ����
@{
    version = $version
    date = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    notes = "������Ʈ ������ ���⿡ �ۼ�"
} | ConvertTo-Json | Out-File "$latestPath\version.json" -Encoding UTF8

Write-Host "Deployment completed!" -ForegroundColor Green
Write-Host "Latest: $latestPath"
Write-Host "Backup: $versionPath"
```

## ��Ʈ��ũ ��� ������Ʈ Ȯ��

UpdateChecker�� ��Ʈ��ũ ���� ���������� ����:

```csharp
public class NetworkUpdateChecker
{
    private const string NetworkPath = @"\\����\FACTOVA_Apps\QueryHelper\latest";
    
    public static async Task<UpdateInfo> CheckForUpdatesAsync()
    {
        try
        {
            var versionFile = Path.Combine(NetworkPath, "version.json");
            
            if (!File.Exists(versionFile))
            {
                return new UpdateInfo { HasUpdate = false };
            }
            
            var json = await File.ReadAllTextAsync(versionFile);
            var versionInfo = JsonSerializer.Deserialize<NetworkVersionInfo>(json);
            
            var currentVersion = GetCurrentVersion();
            var latestVersion = Version.Parse(versionInfo.Version);
            
            if (latestVersion > currentVersion)
            {
                return new UpdateInfo
                {
                    HasUpdate = true,
                    LatestVersion = $"v{versionInfo.Version}",
                    CurrentVersion = $"v{currentVersion}",
                    DownloadUrl = Path.Combine(NetworkPath, "FACTOVA_QueryHelper.exe"),
                    ReleaseNotes = versionInfo.Notes
                };
            }
            
            return new UpdateInfo { HasUpdate = false };
        }
        catch (Exception ex)
        {
            return new UpdateInfo { HasUpdate = false, ErrorMessage = ex.Message };
        }
    }
}

public class NetworkVersionInfo
{
    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;
    
    [JsonPropertyName("date")]
    public string Date { get; set; } = string.Empty;
    
    [JsonPropertyName("notes")]
    public string Notes { get; set; } = string.Empty;
}
```

## ���� ����

1. **������Ʈ ���� ������Ʈ**
   ```xml
   <Version>1.0.0</Version>
   ```

2. **���� ��ũ��Ʈ ����**
   ```powershell
   .\deploy.ps1
   ```

3. **����ڿ��� ����**
   - �̸��� �Ǵ� �޽����� ������Ʈ ����
   - ���α׷� ����� �� �ڵ� Ȯ��

## ����
- ? GitHub ���� ���� ���ʿ�
- ? �系 ��Ʈ��ũ������ ���� ����
- ? ���� �ٿ�ε� �ӵ�
- ? �ܺ� ������ ����

## ����
- ? �繫�� �ܺο��� ���� �Ұ� (VPN �ʿ�)
- ? ���� ���� �ʿ�
- ? ��Ʈ��ũ ���� ���� ���� ���� �ʿ�
