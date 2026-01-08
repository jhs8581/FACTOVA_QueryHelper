# 모든 .cs 파일을 UTF-8 BOM으로 변환하는 스크립트

$ErrorActionPreference = "Stop"

Write-Host "=== C# 파일을 UTF-8 BOM으로 변환 중... ===" -ForegroundColor Green

$csFiles = Get-ChildItem -Path . -Filter *.cs -Recurse | Where-Object { $_.FullName -notlike "*\obj\*" -and $_.FullName -notlike "*\bin\*" }

$count = 0
foreach ($file in $csFiles) {
    try {
        # 파일 내용 읽기 (현재 인코딩 자동 감지)
        $content = Get-Content -Path $file.FullName -Raw 
        
        # UTF-8 BOM으로 저장
        $utf8Bom = New-Object System.Text.UTF8Encoding $true
        [System.IO.File]::WriteAllText($file.FullName, $content, $utf8Bom)
        
        $count++
        Write-Host "  ? $($file.FullName)" -ForegroundColor Gray
    }
    catch {
        Write-Host "  ? 실패: $($file.FullName) - $($_.Exception.Message)" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "=== 변환 완료: $count 개 파일 ===" -ForegroundColor Green
Write-Host ""
Write-Host "다음 단계:" -ForegroundColor Yellow
Write-Host "1. 변경사항을 확인하세요" -ForegroundColor White
Write-Host "2. 빌드 테스트를 실행하세요: dotnet build" -ForegroundColor White
Write-Host "3. Git에 커밋하세요: git add . && git commit -m 'Fix: Convert source files to UTF-8 BOM'" -ForegroundColor White
