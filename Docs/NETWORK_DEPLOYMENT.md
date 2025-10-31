# 사내 네트워크 배포 가이드

## 개요
Private GitHub 저장소 대신 **사내 네트워크 공유 폴더**를 사용한 배포 방법입니다.

## 배포 폴더 구조

```
\\서버\FACTOVA_Apps\QueryHelper\
├── latest\                    # 최신 버전
│   ├── FACTOVA_QueryHelper.exe
│   └── version.txt           # 버전 정보
├── v1.0.0\                   # 버전별 백업
│   └── FACTOVA_QueryHelper.exe
└── v1.1.0\
    └── FACTOVA_QueryHelper.exe
```

## 자동 배포 스크립트

### deploy.ps1 (PowerShell)
```powershell
# 설정
$version = "1.0.0"
$deployPath = "\\서버\FACTOVA_Apps\QueryHelper"
$latestPath = "$deployPath\latest"
$versionPath = "$deployPath\v$version"

# 빌드
Write-Host "Building version $version..." -ForegroundColor Cyan
dotnet publish -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -p:EnableCompressionInSingleFile=true `
  --output ./publish

# 배포
Write-Host "Deploying to network share..." -ForegroundColor Cyan

# 버전별 폴더 생성 및 복사
New-Item -ItemType Directory -Force -Path $versionPath | Out-Null
Copy-Item "./publish/FACTOVA_QueryHelper.exe" -Destination $versionPath -Force

# latest 폴더 업데이트
New-Item -ItemType Directory -Force -Path $latestPath | Out-Null
Copy-Item "./publish/FACTOVA_QueryHelper.exe" -Destination $latestPath -Force

# 버전 정보 파일 생성
@{
    version = $version
    date = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    notes = "업데이트 내용을 여기에 작성"
} | ConvertTo-Json | Out-File "$latestPath\version.json" -Encoding UTF8

Write-Host "Deployment completed!" -ForegroundColor Green
Write-Host "Latest: $latestPath"
Write-Host "Backup: $versionPath"
```

## 네트워크 기반 업데이트 확인

UpdateChecker를 네트워크 공유 폴더용으로 수정:

```csharp
public class NetworkUpdateChecker
{
    private const string NetworkPath = @"\\서버\FACTOVA_Apps\QueryHelper\latest";
    
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

## 배포 절차

1. **프로젝트 버전 업데이트**
   ```xml
   <Version>1.0.0</Version>
   ```

2. **배포 스크립트 실행**
   ```powershell
   .\deploy.ps1
   ```

3. **사용자에게 공지**
   - 이메일 또는 메신저로 업데이트 공지
   - 프로그램 재시작 시 자동 확인

## 장점
- ? GitHub 접근 권한 불필요
- ? 사내 네트워크만으로 배포 가능
- ? 빠른 다운로드 속도
- ? 외부 의존성 없음

## 단점
- ? 사무실 외부에서 접근 불가 (VPN 필요)
- ? 수동 배포 필요
- ? 네트워크 공유 폴더 권한 설정 필요
