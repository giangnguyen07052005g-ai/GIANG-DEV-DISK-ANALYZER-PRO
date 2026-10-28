param(
    [string]$Version = "1.0.4"
)

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "  GIANG DEV DISK ANALYZER PRO - PREPARE GITHUB RELEASE" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "Bắt đầu chuẩn bị gói cập nhật cho phiên bản: v$Version" -ForegroundColor Yellow

$toolProject = "tools\GIANG.Dev.ReleaseHelper\GIANG.Dev.ReleaseHelper.csproj"

dotnet run --project $toolProject -- --version $Version

if ($LASTEXITCODE -ne 0) {
    Write-Host "[!] Đã có lỗi xảy ra trong quá trình chuẩn bị phát hành!" -ForegroundColor Red
    exit 1
}

$releaseDir = "release-output\v$Version"
if (Test-Path $releaseDir) {
    Write-Host "`n[+] Đang mở thư mục chứa 3 file cập nhật trong Windows Explorer..." -ForegroundColor Green
    Start-Process "explorer.exe" -ArgumentList (Resolve-Path $releaseDir)
}

Write-Host "`n==========================================================" -ForegroundColor Cyan
Write-Host "BƯỚC TIẾP THEO ĐỂ ĐƯA LÊN GITHUB:" -ForegroundColor Cyan
Write-Host "1. Mở trình duyệt vào trang GitHub Repository của bạn."
Write-Host "2. Nhấp vào 'Releases' -> 'Draft a new release' (Tạo bản phát hành mới)."
Write-Host "3. Nhập Tag version: v$Version"
Write-Host "4. Kéo thả 3 file vừa tạo trong thư mục '$releaseDir' vào ô 'Attach binaries'."
Write-Host "5. Nhấp nút 'Publish release'!"
Write-Host "==========================================================" -ForegroundColor Cyan
