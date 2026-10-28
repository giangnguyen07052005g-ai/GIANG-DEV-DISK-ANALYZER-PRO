# Automated Test Script for GIANG DEV DISK ANALYZER PRO
Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "  GIANG DEV DISK ANALYZER PRO - TEST RUNNER" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

$testProject = "tests/GIANG.Dev.DiskAnalyzer.Tests/GIANG.Dev.DiskAnalyzer.Tests.csproj"

Write-Host "Đang thực thi toàn bộ 20 kịch bản kiểm thử (38 test cases)..." -ForegroundColor Yellow
dotnet test $testProject -c Release --verbosity normal

if ($LASTEXITCODE -ne 0) {
    Write-Host "Có test thất bại!" -ForegroundColor Red
    exit 1
}

Write-Host "Tất cả test cases đã VƯỢT QUA thành công (100% Passed)!" -ForegroundColor Green
