# 깨진 한글을 복구하는 스크립트
# 원본 CP949/EUC-KR로 잘못 저장된 파일을 UTF-8로 재변환

$ErrorActionPreference = "Stop"

Write-Host "=== 한글 인코딩 복구 스크립트 ===" -ForegroundColor Cyan
Write-Host ""

# 깨진 파일 목록
$brokenFiles = @(
    "Database\QueryExecutionManager.cs",
    "Database\OracleDatabase.cs",
    "Database\QueryDatabase.cs",
    "Excel\ExcelManager.cs",
    "Excel\ExcelQueryReader.cs",
    "Services\ValidationHelper.cs",
    "Services\TnsParser.cs",
    "Services\FileDialogManager.cs",
    "Services\QuerySummaryHelper.cs",
    "Monitoring\ProcessMonitor.cs",
    "Monitoring\MonitorLogger.cs",
    "SFC\SfcQueryManager.cs",
    "Updates\UpdateChecker.cs",
    "Updates\UpdateCheckerTest.cs",
    "Models\CheckableComboBoxItem.cs",
    "Controls\SharedDataContext.cs"
)

$fixedCount = 0
$failedCount = 0

foreach ($file in $brokenFiles) {
    if (-not (Test-Path $file)) {
        Write-Host "  ⚠ 파일 없음: $file" -ForegroundColor Yellow
        continue
    }

    Write-Host "처리 중: $file" -ForegroundColor White
    
    try {
        # 파일을 바이트로 읽기
        $bytes = [System.IO.File]::ReadAllBytes($file)
        
        # UTF-8 BOM 제거 (있는 경우)
        if ($bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF) {
            $bytes = $bytes[3..($bytes.Length-1)]
        }
        
        # CP949 (Windows-949, EUC-KR 확장)로 디코딩 시도
        $encoding = [System.Text.Encoding]::GetEncoding(949)
        $text = $encoding.GetString($bytes)
        
        # 제대로 복구되었는지 확인 (한글이 있어야 함)
        if ($text -match "[가-힣]") {
            # UTF-8 BOM으로 저장
            $utf8WithBom = New-Object System.Text.UTF8Encoding $true
            [System.IO.File]::WriteAllText($file, $text, $utf8WithBom)
            
            Write-Host "  ✓ 복구 완료" -ForegroundColor Green
            $fixedCount++
        } else {
            # CP949로도 복구 안 되면 다른 인코딩 시도
            Write-Host "  ⚠ CP949로 복구 실패, 다른 인코딩 시도..." -ForegroundColor Yellow
            
            # EUC-KR 시도
            $encoding = [System.Text.Encoding]::GetEncoding(51949)
            $text = $encoding.GetString($bytes)
            
            if ($text -match "[가-힣]") {
                $utf8WithBom = New-Object System.Text.UTF8Encoding $true
                [System.IO.File]::WriteAllText($file, $text, $utf8WithBom)
                Write-Host "  ✓ 복구 완료 (EUC-KR)" -ForegroundColor Green
                $fixedCount++
            } else {
                Write-Host "  ✗ 복구 실패: 한글을 찾을 수 없음" -ForegroundColor Red
                $failedCount++
            }
        }
    }
    catch {
        Write-Host "  ✗ 오류: $($_.Exception.Message)" -ForegroundColor Red
        $failedCount++
    }
}

Write-Host ""
Write-Host "=== 작업 완료 ===" -ForegroundColor Cyan
Write-Host "복구 성공: $fixedCount 개" -ForegroundColor Green
Write-Host "복구 실패: $failedCount 개" -ForegroundColor $(if ($failedCount -gt 0) { "Red" } else { "Gray" })
Write-Host ""

if ($fixedCount -gt 0) {
    Write-Host "다음 단계:" -ForegroundColor Yellow
    Write-Host "1. 변경사항 확인: git diff" -ForegroundColor White
    Write-Host "2. 빌드 테스트: dotnet build" -ForegroundColor White
    Write-Host "3. 정상 작동 확인 후 커밋" -ForegroundColor White
}
