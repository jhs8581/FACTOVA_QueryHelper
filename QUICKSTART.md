# 첫 배포 빠른 가이드

이 문서는 **처음으로 릴리즈를 배포**하는 방법을 간단히 설명합니다.

## ?? 사전 체크리스트

- [ ] Visual Studio에서 프로젝트가 정상 빌드됨
- [ ] GitHub 저장소에 코드가 푸시됨
- [ ] GitHub Actions가 활성화됨 (저장소 Settings → Actions → Allow all actions)

## ?? 배포 단계

### 1단계: 프로젝트 버전 확인/수정

`FACTOVA_QueryHelper.csproj` 파일을 열고 버전을 확인합니다:

```xml
<Version>1.0.0</Version>
```

첫 배포라면 `1.0.0`으로 설정하세요.

### 2단계: 코드 커밋

```bash
# 모든 변경사항 추가
git add .

# 커밋
git commit -m "Release v1.0.0"

# GitHub에 푸시
git push origin master
```

### 3단계: Git 태그 생성 및 푸시

```bash
# 태그 생성 (v접두사 필수!)
git tag v1.0.0

# 태그 푸시 (이 명령이 자동 빌드를 시작합니다)
git push origin v1.0.0
```

### 4단계: GitHub에서 빌드 확인

1. 브라우저에서 GitHub 저장소로 이동
2. **Actions** 탭 클릭
3. **Build and Release** 워크플로우가 실행 중인지 확인
4. 녹색 체크 표시가 나타날 때까지 대기 (약 3-5분)

### 5단계: 릴리즈 확인 및 편집

1. **Releases** 탭 클릭
2. `v1.0.0` 릴리즈가 생성되었는지 확인
3. **Edit** 버튼 클릭
4. 릴리즈 노트 작성:

```markdown
## FACTOVA Query Helper v1.0.0

### 주요 기능
- Oracle 데이터베이스 쿼리 실행
- 자동 쿼리 실행 및 모니터링
- SFC 장비 상태 모니터링
- 자동 업데이트 확인

### 설치 방법
1. FACTOVA_QueryHelper.exe 파일을 다운로드합니다.
2. 원하는 위치에 저장하고 실행합니다.
3. 설정 탭에서 TNS 파일 경로를 지정합니다.

### 시스템 요구사항
- Windows 10/11 (64-bit)
- Oracle Client 19c 이상
```

5. **Update release** 버튼 클릭

### 6단계: 다운로드 테스트

1. **Releases** 페이지에서 `FACTOVA_QueryHelper.exe` 다운로드
2. 실행하여 정상 작동 확인
3. **도움말** 메뉴 → **정보** 에서 버전 확인

## ? 완료!

이제 사용자들이 프로그램을 실행하면 자동으로 업데이트 알림을 받게 됩니다.

## ?? 다음 업데이트 배포하기

두 번째 릴리즈부터는 더 간단합니다:

```bash
# 1. 버전 변경 (FACTOVA_QueryHelper.csproj)
<Version>1.1.0</Version>

# 2. 커밋 및 푸시
git add .
git commit -m "Release v1.1.0"
git push origin master

# 3. 태그 생성 및 푸시
git tag v1.1.0
git push origin v1.1.0

# 4. GitHub에서 자동 빌드 완료 대기
# 5. 릴리즈 노트 편집
```

## ? 문제 해결

### "GitHub Actions 워크플로우가 실행되지 않아요"
- 저장소 Settings → Actions → Allow all actions 확인
- `.github/workflows/release.yml` 파일이 master 브랜치에 있는지 확인

### "빌드가 실패했어요"
1. Actions 탭에서 실패한 워크플로우 클릭
2. 로그에서 오류 메시지 확인
3. 로컬에서 `dotnet build -c Release` 실행하여 문제 확인

### "업데이트 알림이 표시되지 않아요"
- 인터넷 연결 확인
- 설정 → 업데이트 설정에서 "자동 확인" 체크박스 확인
- GitHub Releases 페이지에 릴리즈가 public으로 설정되어 있는지 확인

## ?? 추가 자료

- 상세한 배포 가이드: [DEPLOYMENT.md](DEPLOYMENT.md)
- 프로젝트 정보: [README.md](README.md)
- 문제 리포트: [GitHub Issues](https://github.com/jhs8581/FACTOVA_QueryHelper/issues)
