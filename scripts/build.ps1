# Build script for GIANG DEV DISK ANALYZER PRO
Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "  GIANG DEV DISK ANALYZER PRO - RELEASE BUILD" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

$solution = "GIANG_DEV_DISK_ANALYZER_PRO.sln"

Write-Host "[1/3] Khôi phục NuGet packages..." -ForegroundColor Yellow
dotnet restore $solution

if ($LASTEXITCODE -ne 0) {
    Write-Host "Lỗi khi restore NuGet!" -ForegroundColor Red
    exit 1
}

Write-Host "[2/3] Biên dịch Solution (Release)..." -ForegroundColor Yellow
dotnet build $solution -c Release --no-restore

if ($LASTEXITCODE -ne 0) {
    Write-Host "Lỗi khi build solution!" -ForegroundColor Red
    exit 1
}

Write-Host "[3/3] Build hoàn tất thành công!" -ForegroundColor Green
