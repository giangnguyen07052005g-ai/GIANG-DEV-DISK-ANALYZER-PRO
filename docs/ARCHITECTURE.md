# KIẾN TRÚC PHẦN MỀM (SOFTWARE ARCHITECTURE)

**Phần mềm**: GIANG DEV DISK ANALYZER PRO  
**Ngôn ngữ**: C# 12  
**Nền tảng**: .NET 8 LTS (Windows Desktop x64)  
**Mô hình thiết kế**: Clean MVVM + Dependency Injection + Layered Architecture  

---

## 1. SƠ ĐỒ PHÂN LỚP TỔNG THỂ

```
+-------------------------------------------------------------+
|              GIANG.Dev.DiskAnalyzer.App                     |
|  - Views (WPF XAML)                                         |
|  - ViewModels (CommunityToolkit.Mvvm)                      |
|  - Custom Controls (TreemapControl, CyberProgressBar)       |
|  - Cyberpunk Neon Design Tokens (Colors, Icons, Styles)     |
+-------------------------------------------------------------+
                              | (Phụ thuộc qua DI)
                              v
+-------------------------------------------------------------+
|          GIANG.Dev.DiskAnalyzer.Infrastructure              |
|  - FileSystem (DriveService, ReparsePointHelper)            |
|  - Scanning (StorageScanner, DuplicateFinder, BigFileHunter)|
|  - Hashing (BufferedHasher - Streaming SHA-256)             |
|  - WindowsIntegration (RecycleBinService, ShellHelper)      |
|  - Storage (SqliteScanHistoryRepository, JsonSettingsService)|
|  - Reporting (ReportService - CSV UTF-8 BOM & HTML)         |
|  - Logging (FileLogger, FileLoggerProvider)                 |
+-------------------------------------------------------------+
                              | (Triển khai Interfaces)
                              v
+-------------------------------------------------------------+
|              GIANG.Dev.DiskAnalyzer.Core                    |
|  - Models (DriveItem, FileItem, FolderItem, TreemapNode)    |
|  - Interfaces (IDriveService, IStorageScanner, ...)         |
|  - Enums (ScanType, ScanState, RiskLevel, ...)              |
|  - Helpers (ByteSizeFormatter, PathSafetyValidator)         |
+-------------------------------------------------------------+
```

---

## 2. NGUYÊN TẮC THIẾT KẾ & PATTERNS

### 2.1. Model-View-ViewModel (MVVM)
- Tách biệt hoàn toàn giữa giao diện người dùng (`Views`) và logic nghiệp vụ (`ViewModels` & `Services`).
- Không viết code quét file, tính hash hay xóa file trong file code-behind (`.xaml.cs`).
- Sử dụng `CommunityToolkit.Mvvm` với Source Generators: `[ObservableProperty]`, `[RelayCommand]` giúp code ngắn gọn, hiệu năng biên dịch cao và không sinh overhead reflection runtime.

### 2.2. Dependency Injection (DI)
- Sử dụng thư viện chuẩn của Microsoft: `Microsoft.Extensions.DependencyInjection`.
- Toàn bộ service và ViewModel được đăng ký trong `App.xaml.cs`.
- Mọi ViewModel nhận các Service thông qua Constructor Injection, cho phép viết Unit Test dễ dàng với Mock / Test Doubles.

### 2.3. Pipeline Tìm Kiếm File Trùng Lặp 3 Giai Đoạn (Duplicate Finder Pipeline)
Để xử lý hàng trăm nghìn file mà không làm tràn RAM hay tốn tài nguyên đĩa vô ích:
1. **Stage 1 (Size Bucketing)**: Lọc và phân nhóm tất cả file theo độ dài byte. Các file có độ dài độc nhất lập tức bị loại bỏ khỏi danh sách kiểm tra tiếp theo.
2. **Stage 2 (Fast Prefix Hash)**: Đối với các file có cùng kích thước, chỉ đọc 4 KB đầu tiên để tính hash tiền tố. Nếu 4 KB đầu khác nhau, file chắc chắn không trùng nhau, loại bỏ ngay.
3. **Stage 3 (Full SHA-256 Streaming)**: Chỉ các file có cùng kích thước VÀ cùng hash tiền tố mới được đưa vào tính SHA-256 toàn diện qua luồng bộ đệm 64 KB (`FileStream` + `IncrementalHash`).
4. **Stage 4 (Byte-by-Byte Verification)**: So sánh trực tiếp chuỗi byte với nhau để bảo đảm tính chính xác 100%, chống va chạm hash (hash collision).

### 2.4. Thuật toán Biểu đồ Treemap (Squarified Layout Algorithm)
- Được triển khai trong `FolderAnalyzer.cs` và vẽ trực tiếp qua `TreemapControl` trên WPF DrawingContext.
- Chia diện tích canvas thành các hình chữ nhật tỷ lệ thuận với dung lượng thư mục con.
- Sử dụng bảng mã màu Neon palette xoay vòng để tạo chiều sâu thị giác mang phong cách Cyberpunk.

---

## 3. KIẾN TRÚC AN TOÀN DỮ LIỆU & RECYCLE BIN

```
[Thao tác xóa]
       |
       v
[PathSafetyValidator.IsSafeForCleanup]
       |
       +---> [Từ chối] ---> Ghi log cảnh báo & Thông báo vi phạm an toàn
       |
       +---> [Hợp lệ]
                 |
                 v
       [Hộp thoại xác nhận của người dùng (User Final Confirmation)]
                 |
                 +---> [Hủy] ---> Giữ nguyên file
                 |
                 +---> [Đồng ý]
                           |
                           v
                 [RecycleBinService (Win32 SHFileOperationW)]
                           |
                           v
                 [Chuyển an toàn vào Thùng rác hệ thống (Recycle Bin)]
```

- Không dùng lệnh xóa vĩnh viễn `File.Delete` mặc định mà chuyển qua API Windows Shell `SHFileOperationW` với cờ `FOF_ALLOWUNDO`.
- Khi chuyển vào Recycle Bin, người dùng có thể mở Thùng rác trên màn hình Desktop và chọn "Restore" để khôi phục lại bất kỳ lúc nào nếu đổi ý.

---

## 4. XỬ LÝ NGOẠI LỆ & LOGGING

- Ghi log cuốn chiếu (Rolling file log) tại `%LOCALAPPDATA%\GiangDevDiskAnalyzer\Logs\analyzer-{yyyy-MM-dd}.log`.
- Giới hạn kích thước file log dưới 10 MB, tự động phân tách khi vượt ngưỡng.
- Bắt và xử lý toàn cục tại 3 chốt chặn:
  1. `Application.Current.DispatcherUnhandledException` (Ngoại lệ luồng giao diện).
  2. `AppDomain.CurrentDomain.UnhandledException` (Ngoại lệ runtime).
  3. `TaskScheduler.UnobservedTaskException` (Ngoại lệ tác vụ bất đồng bộ).
- Không nuốt exception âm thầm; luôn hiển thị thông điệp thân thiện với người dùng và ghi chi tiết stack trace vào file log để chẩn đoán.
