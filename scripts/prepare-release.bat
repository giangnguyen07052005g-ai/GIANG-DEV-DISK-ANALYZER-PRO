@echo off
chcp 65001 > nul
echo ==========================================================
echo   GIANG DEV DISK ANALYZER PRO - CHUẨN BỊ BẢN PHÁT HÀNH
echo ==========================================================
set /p VERSION="1. Nhập số phiên bản cần phát hành (Enter để mặc định 1.0.4): "
if "%VERSION%"=="" set VERSION=1.0.4

set /p GH_OWNER="2. Nhập GitHub Username của bạn (Enter để bỏ qua nếu đã cấu hình): "

cd /d "%~dp0.."

if "%GH_OWNER%"=="" (
    dotnet run --project tools\GIANG.Dev.ReleaseHelper\GIANG.Dev.ReleaseHelper.csproj -- --version %VERSION%
) else (
    dotnet run --project tools\GIANG.Dev.ReleaseHelper\GIANG.Dev.ReleaseHelper.csproj -- --version %VERSION% --owner %GH_OWNER%
)

if errorlevel 1 (
    echo [!] Đã có lỗi xảy ra!
    pause
    exit /b 1
)

explorer "release-output\v%VERSION%"
echo ==========================================================
echo [THÀNH CÔNG] Thư mục chứa 3 tệp cập nhật đã được mở.
echo Bạn chỉ cần kéo thả 3 tệp này vào trang GitHub Release!
echo ==========================================================
pause
