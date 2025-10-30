# FACTOVA Query Helper

Oracle 데이터베이스 쿼리 실행 및 모니터링을 위한 WPF 애플리케이션입니다.

## 주요 기능

### 1. 쿼리 관리
- Excel 파일 또는 SQLite 데이터베이스에서 쿼리 로드
- 쿼리 추가/수정/삭제
- 쿼리별 실행 조건 설정 (건수 체크, 컬럼 값 확인 등)

### 2. 자동 쿼리 실행
- 주기적인 쿼리 자동 실행
- 조건에 따른 알림 기능
- 디폴트 폼 자동 선택

### 3. SFC 모니터링
- SFC 시스템 장비 상태 모니터링
- 실시간 상태 업데이트
- 필터링 및 검색 기능

### 4. 자동 업데이트
- 프로그램 시작 시 자동 업데이트 확인
- GitHub Releases를 통한 간편한 업데이트
- 릴리즈 노트 자동 표시

## 시스템 요구사항

- Windows 10/11 (64-bit)
- .NET 8.0 Runtime (자체 포함 빌드 시 불필요)
- Oracle Client (19c 이상 권장)

## 설치 방법

### 방법 1: 릴리즈 다운로드 (권장)
1. [Releases 페이지](https://github.com/jhs8581/FACTOVA_QueryHelper/releases)에서 최신 버전 다운로드
2. `FACTOVA_QueryHelper.exe` 파일 실행
3. 설정 탭에서 TNS 파일 경로 설정

### 방법 2: 소스 빌드
```bash
git clone https://github.com/jhs8581/FACTOVA_QueryHelper.git
cd FACTOVA_QueryHelper
dotnet restore
dotnet build -c Release
dotnet run --project FACTOVA_QueryHelper.csproj
```

## 사용 방법

### 초기 설정
1. **설정** 탭에서 TNS 파일 경로 설정
2. **쿼리 관리** 탭에서 쿼리 등록 또는 Excel 파일 가져오기
3. **로그 분석** 탭에서 쿼리 실행

### 쿼리 등록
1. **쿼리 관리** 탭 선택
2. **DB 로드** 또는 **Excel 가져오기**
3. 쿼리 정보 입력:
   - 쿼리명, SQL 문
   - DB 연결 정보 (TNS 또는 직접 연결)
   - 알림 조건 (선택사항)

### 자동 실행
1. **로그 분석** 탭 선택
2. 실행할 쿼리 선택
3. **자동 실행 활성화** 체크
4. 실행 주기 설정 (초 단위)
5. **실행** 버튼 클릭

## 프로젝트 구조

```
FACTOVA_QueryHelper/
├── Controls/                    # 사용자 컨트롤
│   ├── LogAnalysisControl.*     # 로그 분석 및 쿼리 실행
│   ├── QueryManagementControl.* # 쿼리 관리
│   ├── SfcMonitoringControl.*   # SFC 모니터링
│   └── SettingsControl.*        # 설정
├── ExampleExcel/                # 예제 Excel 파일
├── Icons/                       # 아이콘 리소스
├── .github/workflows/           # GitHub Actions 워크플로우
│   └── release.yml              # 자동 빌드 및 배포
├── MainWindow.*                 # 메인 윈도우
├── QueryDatabase.cs             # SQLite 데이터베이스 관리
├── OracleDatabase.cs            # Oracle 연결 및 쿼리 실행
├── UpdateChecker.cs             # 자동 업데이트 확인
├── UpdateNotificationWindow.*   # 업데이트 알림 창
└── DEPLOYMENT.md                # 배포 가이드
```

## 개발 환경

- Visual Studio 2022
- .NET 8.0
- WPF (Windows Presentation Foundation)
- C# 12.0

### 주요 라이브러리
- **Oracle.ManagedDataAccess.Core** (23.26.0) - Oracle DB 연결
- **Microsoft.Data.Sqlite** (8.0.0) - SQLite 데이터베이스
- **EPPlus** (7.0.5) - Excel 파일 처리
- **System.Management** (8.0.0) - 시스템 정보 수집

## 배포

자세한 배포 방법은 [DEPLOYMENT.md](DEPLOYMENT.md)를 참조하세요.

### 빠른 배포 (GitHub Actions)
```bash
# 버전 변경 후
git tag v1.0.0
git push origin v1.0.0
```

GitHub Actions가 자동으로 빌드하고 릴리즈를 생성합니다.

## 라이선스

이 프로젝트는 FACTOVA 사내 프로젝트입니다.

## 기여

버그 리포트나 기능 제안은 [Issues](https://github.com/jhs8581/FACTOVA_QueryHelper/issues) 페이지를 이용해주세요.

## 변경 로그

### v1.0.0 (2024-01-15)
- 초기 릴리즈
- 쿼리 관리 기능
- 자동 쿼리 실행
- SFC 모니터링
- 자동 업데이트 확인

---

**개발자**: FACTOVA IT Team  
**문의**: [GitHub Issues](https://github.com/jhs8581/FACTOVA_QueryHelper/issues)
