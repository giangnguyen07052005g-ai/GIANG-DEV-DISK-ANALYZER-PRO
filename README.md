# GIANG DEV DISK ANALYZER PRO 🚀

> Phần mềm phân tích dung lượng ổ đĩa, săn file lớn, tìm file trùng lặp bằng SHA-256, phân tích cấu trúc thư mục với biểu đồ Treemap và hỗ trợ dọn dẹp hệ thống an toàn trên Windows.

Giao diện đồ họa độc quyền phong cách **Cyberpunk Neon**, kiến trúc MVVM thuần C# trên nền tảng **.NET 8 LTS**.

---

## 🌟 TÍNH NĂNG NỔI BẬT

1. **Dashboard & Thống kê Ổ đĩa Thực tế**:
   - Truy vấn thông số ổ đĩa vật lý qua Windows API (`DriveInfo`), tự động loại trừ hoặc xử lý an toàn ổ đĩa quang, USB ngắt kết nối.
   - Thẻ thống kê trực quan: Tổng dung lượng, Đã sử dụng, Còn trống, Tỷ lệ %, File lớn nhất trong phiên, Số file đã quét.
2. **Smart Storage Scan (Quét Phân tích Bộ nhớ)**:
   - Quét thư mục tùy chọn hoặc toàn bộ ổ đĩa với thuật toán duyệt Queue bất đồng bộ (`async/await`).
   - Hỗ trợ tạm dừng (Pause), tiếp tục (Resume) và hủy tác vụ (Cancel) mượt mà.
   - Bộ lọc theo phần mở rộng, tìm kiếm tên file, sắp xếp theo kích thước, mở thư mục trong File Explorer, sao chép đường dẫn.
   - Bảng phân tích Top 10 thư mục chiếm nhiều dung lượng nhất.
3. **Big File Hunter (Săn File Lớn)**:
   - Lọc nhanh các file vượt ngưỡng: >100 MB, >500 MB, >1 GB hoặc ngưỡng tùy chỉnh.
   - Phân biệt rõ ràng giữa file lớn và file rác, tuyệt đối không tự ý xóa khi chưa xác nhận.
   - Xuất danh sách ra định dạng CSV chuẩn RFC 4180.
4. **Duplicate Finder (Tìm File Trùng Lặp Chuẩn Nội Dung)**:
   - Quy trình băm 3 giai đoạn: Nhóm kích thước -> Băm nhanh tiền tố 4KB -> Băm toàn diện SHA-256 (bộ đệm 64KB, không tải file nặng vào RAM) -> So sánh byte trực tiếp.
   - Tính toán chính xác dung lượng tiết kiệm thực tế theo công thức: `(Số bản sao - 1) * Kích thước`.
   - Nút hỗ trợ nhanh: *Giữ file cũ nhất*, *Giữ file mới nhất*, *Bỏ chọn tất cả*.
   - Khóa an toàn: Ngăn chặn xóa toàn bộ bản sao trong cùng một nhóm.
5. **Folder Analyzer & Treemap (Phân Tích Cấu Trúc Thư Mục)**:
   - Tính toán dung lượng tổng hợp đa cấp, tỷ lệ % so với thư mục cha.
   - Tích hợp biểu đồ **Treemap** tương tác (Squarified Layout Algorithm) với hiệu ứng viền neon.
   - Cho phép nhấp vào ô treemap hoặc nhấp đúp thư mục để đi sâu vào cấp trong (Drill-down) và quay lại cấp trên (Back).
6. **Safe Cleanup (Dọn Dẹp An Toàn)**:
   - Chỉ xử lý các khu vực an toàn đã được chứng thực: `%TEMP%`, Cache hình thu nhỏ Explorer, Cache trình duyệt web (không đụng tài khoản/mật khẩu), File log cũ > 24h.
   - Cơ chế bảo vệ cốt lõi: **Mặc định chuyển toàn bộ file vào Windows Recycle Bin** thông qua Windows Shell API, cho phép người dùng hoàn tác khôi phục khi cần.
   - Tuyệt đối khóa và từ chối xóa thư mục hệ thống Windows, Program Files, file hệ thống (`.sys`, `.dll`, Registry).
   - Báo cáo chi tiết sau khi dọn: Số file thành công, thất bại, bỏ qua và tổng dung lượng giải phóng.
7. **Lịch sử Quét & Cơ sở Dữ liệu SQLite**:
   - Lưu trữ tự động lịch sử quét vào SQLite (`scanhistory.db`), chống khóa cơ sở dữ liệu.
   - Cho phép xem lại, xóa từng mục, xóa toàn bộ và xuất lịch sử ra file CSV.
8. **Xuất Báo Cáo Chuyên Nghiệp (CSV & HTML)**:
   - CSV mã hóa UTF-8 với BOM tương thích 100% với Microsoft Excel tiếng Việt.
   - Báo cáo HTML phong cách Cyberpunk Neon responsive, hiển thị đầy đủ biểu đồ, thống kê và danh sách file lớn.
9. **Mô Hình Phân Quyền Free & Pro Minh Bạch**:
   - Kiến trúc phân quyền `IFeatureService` rõ ràng, sẵn sàng tích hợp license key trong tương lai.
   - Toàn bộ tính năng an toàn cốt lõi đều được mở tự do, không giấu sau paywall.

---

## 🛠 YÊU CẦU HỆ THỐNG

- **Hệ điều hành**: Windows 10 (bản 1809 trở lên) hoặc Windows 11 (64-bit).
- **Phần cứng tối thiểu**: CPU Intel Core i3 / AMD Ryzen 3, RAM 4 GB, dung lượng đĩa trống 200 MB.
- **Độ phân giải màn hình**: 1280×720 trở lên (khuyến nghị 1920×1080).
- **Runtime**: Bản Single-file EXE đã tích hợp sẵn .NET 8 LTS (người dùng không cần cài thêm gì).

---

## 🚀 HƯỚNG DẪN BUILD & CHẠY DÀNH CHO DEVELOPER

### 1. Chuẩn bị môi trường
- Cài đặt [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).
- Visual Studio 2022 (v17.8+) hoặc Visual Studio Code kèm C# Dev Kit.

### 2. Các lệnh PowerShell thực hiện

```powershell
# 1. Di chuyển vào thư mục dự án
cd d:\ToolFB\file-quanly

# 2. Khôi phục các gói thư viện NuGet
dotnet restore

# 3. Biên dịch bản Debug và chạy thử
dotnet build
dotnet run --project src/GIANG.Dev.DiskAnalyzer.App/GIANG.Dev.DiskAnalyzer.App.csproj

# 4. Chạy toàn bộ bộ test kiểm thử (51 test cases - 100% Passed)
dotnet test

# 5. Biên dịch bản tối ưu Release
dotnet build -c Release

# 6. Đóng gói bản EXE độc lập (Self-contained Single-file Win-x64)
dotnet publish src/GIANG.Dev.DiskAnalyzer.App/GIANG.Dev.DiskAnalyzer.App.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish/win-x64
```

Sau khi chạy lệnh publish, file chạy độc lập sẽ nằm tại:
`publish\win-x64\GIANG.Dev.DiskAnalyzer.App.exe`

---

## 📁 CẤU TRÚC SOLUTION

```
GIANG_DEV_DISK_ANALYZER_PRO.sln
│
├── src/
│   ├── GIANG.Dev.DiskAnalyzer.Core/          # Domain Models, Interfaces, Enums, Safe Path Validators
│   ├── GIANG.Dev.DiskAnalyzer.Infrastructure/  # FileSystem, Scanner, SHA-256 Hasher, SQLite, Reports
│   └── GIANG.Dev.DiskAnalyzer.App/           # WPF GUI, Cyberpunk Neon Themes, ViewModels, Controls
│
├── tests/
│   └── GIANG.Dev.DiskAnalyzer.Tests/         # Bộ kiểm thử 20 kịch bản bắt buộc (xUnit)
│
├── installer/
│   └── setup.iss                             # Kịch bản đóng gói bộ cài Inno Setup Windows
│
├── scripts/
│   ├── build.ps1                             # Script build tự động
│   ├── test.ps1                              # Script chạy toàn bộ test
│   └── publish.ps1                           # Script publish Single-file EXE
│
└── docs/
    ├── AUDIT_REPORT.md                       # Báo cáo kiểm toán toàn diện hệ thống
    ├── FEATURE_GAP_REPORT.md                 # Báo cáo khoảng cách và đối chiếu tính năng
    ├── FIX_REPORT.md                         # Báo cáo chi tiết các lỗi đã sửa và nâng cấp
    ├── TEST_REPORT.md                        # Báo cáo chi tiết kết quả 43 bài kiểm thử
    ├── ARCHITECTURE.md                       # Tài liệu kiến trúc phần mềm
    ├── USER_GUIDE_VI.md                      # Sổ tay hướng dẫn sử dụng chi tiết
    └── BUILD_GUIDE_VI.md                     # Hướng dẫn biên dịch & đóng gói
```

---

## 🛡 CAM KẾT AN TOÀN DỮ LIỆU

1. **Tuyệt đối không tự động xóa file**: Mọi thao tác loại bỏ file đều trải qua bước xem trước (preview), hiển thị dung lượng giải phóng, và hộp thoại xác nhận lần cuối.
2. **Ưu tiên Thùng rác (Recycle Bin)**: Tất cả file bị xóa đều được chuyển vào Thùng rác hệ thống Windows qua Shell API, người dùng có thể khôi phục lại dễ dàng.
3. **Danh sách bảo vệ nghiêm ngặt**: Cấm tuyệt đối thao tác can thiệp vào `C:\Windows`, `C:\Program Files`, file driver `.sys`, thư mục hệ thống.
4. **Không thu thập dữ liệu cá nhân**: Ứng dụng không gửi bất kỳ thông tin nào ra ngoài Internet, log chẩn đoán lưu cục bộ tại `%LOCALAPPDATA%\GiangDevDiskAnalyzer\Logs`.

---

© 2026 **GIANG DEV**. All rights reserved.
