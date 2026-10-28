# Standalone Single-file Publish Script for GIANG DEV DISK ANALYZER PRO
Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "  GIANG DEV DISK ANALYZER PRO - WIN-X64 PUBLISH" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

$appProject = "src/GIANG.Dev.DiskAnalyzer.App/GIANG.Dev.DiskAnalyzer.App.csproj"
$outputDir = "publish/win-x64"

Write-Host "Publishing self-contained Single-file EXE..." -ForegroundColor Yellow
dotnet publish $appProject -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o $outputDir

if ($LASTEXITCODE -ne 0) {
    Write-Host "Publish thất bại!" -ForegroundColor Red
    exit 1
}

Write-Host "Publish thành công vào thư mục: $outputDir" -ForegroundColor Green
Write-Host "File thực thi: $outputDir\GIANG.Dev.DiskAnalyzer.App.exe" -ForegroundColor Green
