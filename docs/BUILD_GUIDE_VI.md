# HƯỚNG DẪN BIÊN DỊCH & ĐÓNG GÓI (BUILD & DEPLOYMENT GUIDE)

Tài liệu hướng dẫn chi tiết dành cho lập trình viên và kỹ sư QA khi xây dựng, kiểm thử và đóng gói phần mềm **GIANG DEV DISK ANALYZER PRO**.

---

## 1. YÊU CẦU MÔI TRƯỜNG PHÁT TRIỂN

1. **Hệ điều hành**: Windows 10/11 x64.
2. **.NET SDK**: .NET 8.0 SDK (phiên bản LTS, ví dụ 8.0.427 hoặc mới hơn).
   - Kiểm tra bằng lệnh: `dotnet --version`
3. **IDE khuyến nghị**:
   - Visual Studio 2022 (Community / Professional / Enterprise) với Workload: **.NET Desktop Development**.
   - Hoặc Visual Studio Code kèm extension **C# Dev Kit**.
4. **Công cụ tạo bộ cài (Tùy chọn)**:
   - [Inno Setup 6](https://jrsoftware.org/isdl.php) (dùng để biên dịch file kịch bản `installer/setup.iss` thành bộ cài setup EXE).

---

## 2. QUY TRÌNH BIÊN DỊCH DỰ ÁN

Mở terminal PowerShell tại thư mục gốc của solution (`d:\ToolFB\file-quanly`):

### Bước 1: Khôi phục NuGet Packages
```powershell
dotnet restore
```

### Bước 2: Biên dịch chế độ Debug
```powershell
dotnet build
```

### Bước 3: Chạy ứng dụng trực tiếp
```powershell
dotnet run --project src/GIANG.Dev.DiskAnalyzer.App/GIANG.Dev.DiskAnalyzer.App.csproj
```

### Bước 4: Thực thi bộ kiểm thử tự động (Unit & Integration Tests)
```powershell
dotnet test --verbosity normal
```
*Kết quả kỳ vọng*: Toàn bộ 38 bài test vượt qua (Passed: 38, Failed: 0, Skipped: 0).

---

## 3. ĐÓNG GÓI PHÁT HÀNH (RELEASE & PUBLISH)

### 3.1. Biên dịch tối ưu Release
```powershell
dotnet build -c Release
```

### 3.2. Đóng gói Single-File Self-Contained EXE
Lệnh sau sẽ xuất bản ứng dụng thành một file EXE duy nhất (`GIANG.Dev.DiskAnalyzer.App.exe`), tích hợp toàn bộ .NET 8 runtime và các thư viện cần thiết để người dùng có thể chạy ngay trên bất kỳ máy Windows 64-bit nào mà không cần cài đặt .NET:

```powershell
dotnet publish src/GIANG.Dev.DiskAnalyzer.App/GIANG.Dev.DiskAnalyzer.App.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish/win-x64
```

Đường dẫn file sau khi đóng gói:
`publish\win-x64\GIANG.Dev.DiskAnalyzer.App.exe`

### 3.3. Sử dụng script tự động hóa có sẵn
Trong thư mục `scripts/`, dự án đã chuẩn bị sẵn các script PowerShell:
- Chạy build: `.\scripts\build.ps1`
- Chạy test: `.\scripts\test.ps1`
- Chạy publish: `.\scripts\publish.ps1`

---

## 4. TẠO BỘ CÀI ĐẶT WINDOWS (INNO SETUP)

Nếu máy tính đã cài đặt **Inno Setup 6**:
1. Mở phần mềm Inno Setup Compiler.
2. Mở file kịch bản: `installer\setup.iss`.
3. Nhấp nút **Compile** (hoặc nhấn phím `Ctrl + F9`).
4. File bộ cài `GiangDev_DiskAnalyzer_Pro_v1.0.0_Setup.exe` sẽ được tạo ra tại thư mục `installer_output\`.

Hoặc biên dịch qua dòng lệnh PowerShell:
```powershell
& "C:\Program Files (x86)\Inno Setup 6\iscc.exe" installer/setup.iss
```

---

## 5. VỊ TRÍ LƯU TRỮ DỮ LIỆU & FILE LOG

Khi phần mềm hoạt động trên máy tính người dùng:
1. **File cấu hình người dùng (Settings)**:
   `%LOCALAPPDATA%\GiangDevDiskAnalyzer\settings.json`
2. **Cơ sở dữ liệu lịch sử quét (SQLite)**:
   `%LOCALAPPDATA%\GiangDevDiskAnalyzer\scanhistory.db`
3. **Thư mục nhật ký chẩn đoán (Logs)**:
   `%LOCALAPPDATA%\GiangDevDiskAnalyzer\Logs\analyzer-{yyyy-MM-dd}.log`
4. **Thư mục xuất báo cáo mặc định**:
   `%USERPROFILE%\Documents\GiangDevDiskAnalyzer\Reports\`

---

## 6. XỬ LÝ SỰ CỐ THƯỜNG GẶP (TROUBLESHOOTING)

1. **Lỗi `dotnet : The term 'dotnet' is not recognized`**:
   - Nguyên nhân: Chưa thêm thư mục cài đặt .NET SDK vào biến môi trường PATH.
   - Khắc phục: Cài đặt .NET 8.0 SDK và khởi động lại terminal.
2. **Lỗi `Access to the path ... is denied` khi build**:
   - Nguyên nhân: File EXE đang được chạy hoặc bị khóa bởi tiến trình trước.
   - Khắc phục: Đóng ứng dụng đang mở trước khi chạy lệnh build lại.
3. **Lỗi cảnh báo Antivirus (False Positive)**:
   - Sản phẩm hoàn toàn sạch mã độc, sử dụng API Windows chuẩn (`DriveInfo`, `SHFileOperationW`).
   - Với các phiên bản thương mại, nên sử dụng chứng chỉ số (Code Signing Certificate) để ký số lên file EXE trước khi phát hành rộng rãi.
