# FACTOVA Query Helper 배포 가이드

## 목차
1. [버전 관리](#버전-관리)
2. [배포 방법](#배포-방법)
3. [자동 업데이트 설정](#자동-업데이트-설정)
4. [수동 배포](#수동-배포)

---

## 버전 관리

### 버전 번호 규칙
프로젝트는 [Semantic Versioning](https://semver.org/lang/ko/)을 따릅니다:
- **MAJOR.MINOR.PATCH** 형식 (예: 1.0.0, 1.1.0, 2.0.0)
  - **MAJOR**: 하위 호환성이 없는 API 변경
  - **MINOR**: 하위 호환성이 있는 기능 추가
  - **PATCH**: 하위 호환성이 있는 버그 수정

### 버전 변경 방법
`FACTOVA_QueryHelper.csproj` 파일에서 버전을 변경합니다:

```xml
<PropertyGroup>
  <Version>1.0.0</Version>
  <AssemblyVersion>1.0.0.0</AssemblyVersion>
  <FileVersion>1.0.0.0</FileVersion>
</PropertyGroup>
```

---

## 배포 방법

### 1. GitHub Actions를 통한 자동 배포 (권장)

#### 사전 준비
- GitHub 저장소에 코드 푸시가 완료된 상태여야 합니다.
- GitHub Actions가 활성화되어 있어야 합니다.

#### 배포 절차

**Step 1: 버전 변경**
```bash
# 1. 프로젝트 파일에서 버전 업데이트
# FACTOVA_QueryHelper.csproj 파일 수정

# 2. 변경사항 커밋
git add .
git commit -m "Release v1.0.0"
```

**Step 2: Git 태그 생성 및 푸시**
```bash
# 태그 생성
git tag v1.0.0

# 태그 푸시 (자동 빌드 트리거)
git push origin v1.0.0
```

**Step 3: GitHub Actions 확인**
1. GitHub 저장소의 **Actions** 탭으로 이동
2. **Build and Release** 워크플로우가 실행 중인지 확인
3. 빌드 완료 후 **Releases** 페이지에서 새 릴리즈 확인

**Step 4: 릴리즈 노트 편집**
1. GitHub **Releases** 페이지로 이동
2. 자동 생성된 릴리즈를 찾아 **Edit** 클릭
3. 변경 사항을 상세히 작성:
   ```markdown
   ## v1.0.0 - 2024-01-15
   
   ### 새로운 기능
   - 자동 업데이트 확인 기능 추가
   - 쿼리 관리 UI 개선
   
   ### 버그 수정
   - TNS 파일 로드 오류 수정
   - 메모리 누수 문제 해결
   
   ### 개선 사항
   - 쿼리 실행 속도 향상
   - UI 반응성 개선
   ```

#### 자동 생성되는 파일
- `FACTOVA_QueryHelper.exe` - 단일 실행 파일 (자체 포함)
- `FACTOVA_QueryHelper_v1.0.0.zip` - 전체 패키지 (백업용)

---

### 2. 수동 배포 (로컬 빌드)

#### Visual Studio에서 빌드
```bash
# 1. Release 모드로 빌드
dotnet build -c Release

# 2. 단일 실행 파일 생성
dotnet publish -c Release -r win-x64 --self-contained true ^
  -p:PublishSingleFile=true ^
  -p:IncludeNativeLibrariesForSelfExtract=true ^
  -p:EnableCompressionInSingleFile=true ^
  --output ./publish
```

#### PowerShell 배포 스크립트
```powershell
# deploy.ps1
$version = "1.0.0"
$outputPath = ".\publish"
$deployPath = "\\네트워크경로\FACTOVA_QueryHelper"

# 빌드
Write-Host "Building version $version..." -ForegroundColor Cyan
dotnet publish -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -p:EnableCompressionInSingleFile=true `
  --output $outputPath

# 배포 폴더 생성
Write-Host "Deploying to $deployPath..." -ForegroundColor Cyan
New-Item -ItemType Directory -Force -Path $deployPath | Out-Null

# 파일 복사
Copy-Item "$outputPath\*" -Destination $deployPath -Recurse -Force

# 버전 파일 생성
$version | Out-File "$deployPath\version.txt"

Write-Host "Deployment completed!" -ForegroundColor Green
```

---

## 자동 업데이트 설정

### 사용자 관점
1. 프로그램 시작 시 자동으로 업데이트 확인
2. 새 버전이 있으면 알림 창 표시
3. **다운로드** 버튼 클릭 → 브라우저에서 다운로드 페이지 열림
4. **나중에** 버튼으로 건너뛰기 가능

### 설정 변경
- **설정** 탭 → **업데이트 설정**
- "프로그램 시작 시 자동으로 업데이트 확인" 체크박스
- "지금 업데이트 확인" 버튼으로 수동 확인 가능

### 업데이트 확인 비활성화
```csharp
// AppSettings에서 설정
settings.CheckUpdateOnStartup = false;
```

---

## 배포 체크리스트

### 릴리즈 전 확인사항
- [ ] 모든 기능 테스트 완료
- [ ] 버전 번호 업데이트 (`FACTOVA_QueryHelper.csproj`)
- [ ] 릴리즈 노트 작성
- [ ] 데이터베이스 스키마 변경사항 확인
- [ ] 설정 파일 호환성 확인
- [ ] Oracle Client 의존성 확인

### 릴리즈 후 확인사항
- [ ] GitHub Release 페이지에서 파일 다운로드 테스트
- [ ] 새 버전 실행 테스트
- [ ] 자동 업데이트 알림 테스트
- [ ] 이전 버전에서 업그레이드 테스트

---

## 문제 해결

### GitHub Actions 빌드 실패
```bash
# 로컬에서 빌드 테스트
dotnet restore
dotnet build -c Release

# 로그 확인
# GitHub Actions 탭에서 빌드 로그 확인
```

### 업데이트 확인 실패
- 인터넷 연결 확인
- GitHub API 제한 확인 (시간당 60회)
- 방화벽 설정 확인

### 버전 감지 오류
```csharp
// 현재 버전 확인
var version = Assembly.GetExecutingAssembly().GetName().Version;
Console.WriteLine($"Current Version: {version}");
```

---

## 배포 예시

### 1.0.0 → 1.1.0 업데이트 예시

```bash
# 1. 코드 변경 및 테스트
git add .
git commit -m "Add query filter feature"

# 2. 버전 변경
# FACTOVA_QueryHelper.csproj에서 <Version>1.1.0</Version>로 변경

# 3. 커밋 및 태그
git add FACTOVA_QueryHelper.csproj
git commit -m "Bump version to 1.1.0"
git tag v1.1.0
git push origin master
git push origin v1.1.0

# 4. GitHub Actions가 자동으로 빌드 및 릴리즈 생성
# 5. GitHub Releases 페이지에서 릴리즈 노트 편집
# 6. 사용자는 프로그램 시작 시 자동으로 업데이트 알림 받음
```

---

## 참고 자료
- [GitHub Actions Documentation](https://docs.github.com/en/actions)
- [Semantic Versioning](https://semver.org/lang/ko/)
- [.NET Publishing Options](https://learn.microsoft.com/en-us/dotnet/core/deploying/)
