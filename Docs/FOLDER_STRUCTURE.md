# ?? 프로젝트 폴더 구조

## 개요
이 문서는 FACTOVA_QueryHelper 프로젝트의 폴더 구조와 파일 조직을 설명합니다.

## 폴더 구조

```
FACTOVA_QueryHelper/
├── ?? Controls/           # UI 컨트롤 및 윈도우
│   ├── LogAnalysisControl.xaml/cs      # 쿼리 실행 탭
│   ├── QueryManagementControl.xaml/cs  # 쿼리 관리 탭
│   ├── SfcMonitoringControl.xaml/cs    # SFC 모니터링 탭
│   ├── SettingsControl.xaml/cs         # 설정 탭
│   ├── QueryTextEditWindow.xaml/cs     # 쿼리 편집 윈도우
│   ├── NotificationWindow.xaml/cs      # 알림 윈도우
│   ├── UpdateNotificationWindow.xaml/cs # 업데이트 알림 윈도우
│   └── SharedDataContext.cs            # 공유 데이터 컨텍스트
│
├── ?? Database/           # 데이터베이스 관련
│   ├── OracleDatabase.cs              # Oracle DB 연결 및 쿼리 실행
│   ├── QueryDatabase.cs               # SQLite 쿼리 저장소
│   └── QueryExecutionManager.cs       # 쿼리 실행 매니저
│
├── ?? Excel/              # Excel 파일 처리
│   ├── ExcelManager.cs                # Excel 파일 관리
│   └── ExcelQueryReader.cs            # Excel에서 쿼리 읽기
│
├── ?? Models/             # 데이터 모델
│   ├── CheckableComboBoxItem.cs       # 체크박스 콤보박스 아이템
│   ├── QueryItem.cs                   # 쿼리 아이템 (ExcelQueryReader.cs에 정의)
│   ├── TnsEntry.cs                    # TNS 항목 (TnsParser.cs에 정의)
│   ├── SfcEquipmentInfo.cs            # SFC 설비 정보 (MainWindow.xaml.cs에 정의)
│   └── AppSettings.cs                 # 앱 설정 (SettingsManager.cs에 정의)
│
├── ?? SFC/                # SFC 관련 기능
│   ├── SfcFilterManager.cs            # SFC 필터 관리
│   └── SfcQueryManager.cs             # SFC 쿼리 관리
│
├── ?? Services/           # 각종 서비스 및 유틸리티
│   ├── SettingsManager.cs             # 설정 관리
│   ├── TnsParser.cs                   # TNS 파일 파서
│   ├── FileDialogManager.cs           # 파일 대화상자 관리
│   ├── ValidationHelper.cs            # 유효성 검증 헬퍼
│   └── QuerySummaryHelper.cs          # 쿼리 요약 헬퍼
│
├── ?? Monitoring/         # 모니터링 관련
│   ├── ProcessMonitor.cs              # 프로세스 모니터링
│   └── MonitorLogger.cs               # 모니터링 로거
│
├── ?? Updates/            # 업데이트 관련
│   ├── UpdateChecker.cs               # 업데이트 확인
│   └── UpdateCheckerTest.cs           # 업데이트 테스트
│
├── ?? Icons/              # 아이콘 파일
├── ?? ExampleExcel/       # 예제 Excel 파일
├── ?? 증적성/              # 증적 관련 문서
│
├── App.xaml/cs            # 애플리케이션 진입점
├── MainWindow.xaml/cs     # 메인 윈도우
└── AssemblyInfo.cs        # 어셈블리 정보
```

## 네임스페이스 구조

```csharp
FACTOVA_QueryHelper                    // 루트 네임스페이스
├── FACTOVA_QueryHelper.Controls      // UI 컨트롤 및 윈도우
├── FACTOVA_QueryHelper.Database      // 데이터베이스
├── FACTOVA_QueryHelper.Excel         // Excel 처리
├── FACTOVA_QueryHelper.Models        // 데이터 모델
├── FACTOVA_QueryHelper.SFC           // SFC 기능
├── FACTOVA_QueryHelper.Services      // 서비스
├── FACTOVA_QueryHelper.Monitoring    // 모니터링
└── FACTOVA_QueryHelper.Updates       // 업데이트
```

## 주요 파일 설명

### Controls (UI 컨트롤 및 윈도우)
- **LogAnalysisControl**: 쿼리 실행 및 결과 표시
- **QueryManagementControl**: 쿼리 CRUD 관리
- **SfcMonitoringControl**: SFC 설비 모니터링
- **SettingsControl**: 애플리케이션 설정
- **QueryTextEditWindow**: 쿼리 SQL 편집 윈도우
- **NotificationWindow**: 알림 팝업 윈도우
- **UpdateNotificationWindow**: 업데이트 알림 윈도우
- **SharedDataContext**: 탭 간 데이터 공유

### Database
- **OracleDatabase**: Oracle 데이터베이스 연결 및 쿼리 실행
- **QueryDatabase**: SQLite를 사용한 쿼리 저장소
- **QueryExecutionManager**: 여러 쿼리 실행 및 알림 처리

### Excel
- **ExcelManager**: Excel 파일 읽기/쓰기
- **ExcelQueryReader**: Excel에서 쿼리 정보 추출

### Services
- **SettingsManager**: 애플리케이션 설정 저장/로드
- **TnsParser**: tnsnames.ora 파일 파싱
- **ValidationHelper**: 입력 유효성 검증
- **FileDialogManager**: 파일 선택 대화상자 관리
- **QuerySummaryHelper**: 쿼리 결과 요약

### SFC
- **SfcQueryManager**: SFC 관련 쿼리 실행
- **SfcFilterManager**: SFC 데이터 필터링

### Monitoring
- **ProcessMonitor**: 외부 프로세스 모니터링
- **MonitorLogger**: 모니터링 로그 기록

### Updates
- **UpdateChecker**: GitHub 릴리스에서 업데이트 확인
- **UpdateCheckerTest**: 업데이트 기능 테스트

## 참조 관계

```
MainWindow
  ├── Controls (모든 UI 컨트롤 및 윈도우)
  │     ├── Database (OracleDatabase, QueryDatabase)
  │     ├── Excel (ExcelManager, ExcelQueryReader)
  │     ├── SFC (SfcQueryManager, SfcFilterManager)
  │     └── Services (SettingsManager, ValidationHelper, etc.)
  │
  └── Updates (UpdateChecker)
```

## 최근 변경사항

### 2025-01-31: 폴더 구조 정리
- ? **Controls와 Windows 폴더 통합**: 모든 UI 관련 파일을 Controls 폴더로 통합
- ? **Database 폴더 생성**: Oracle, SQLite, 쿼리 실행 관련 파일 분리
- ? **Excel 폴더 생성**: Excel 처리 관련 파일 분리
- ? **SFC 폴더 생성**: SFC 관련 기능 분리
- ? **Monitoring 폴더 생성**: 모니터링 관련 파일 분리
- ? **Updates 폴더 생성**: 업데이트 관련 파일 분리

## 코딩 규칙

1. **네임스페이스**: 폴더 구조와 일치하는 네임스페이스 사용
2. **파일명**: 클래스명과 동일한 파일명 사용 (PascalCase)
3. **접근 제한자**: 가능한 한 제한적으로 사용 (internal, private)
4. **주석**: XML 문서 주석 사용 (`/// <summary>`)

## 향후 개선 사항

- [ ] Models 폴더에 별도 클래스 파일로 모델 분리
- [ ] Interfaces 폴더 생성하여 인터페이스 분리
- [ ] Tests 폴더 생성하여 단위 테스트 추가
- [ ] Resources 폴더에서 리소스 관리
- [ ] Converters 폴더에서 WPF 컨버터 관리
