# CHANGELOG - GIANG DEV DISK ANALYZER PRO

Tất cả các thay đổi và cập nhật quan trọng của dự án sẽ được ghi lại trong tài liệu này theo nguyên tắc SemVer.

---

## [1.0.4] - 2026-10-28

### Tính Năng Cập Nhật Tự Động Từ Xa Qua GitHub Releases (GitHub Auto Update)

#### 🌐 Cấu Hình GitHub Tập Trung (`GitHubUpdateOptions.cs`)
- **Cấu hình độc lập:** Cho phép chủ dự án tùy biến GitHub Owner, Repository, Kênh cập nhật (`Stable`), mẫu tên tệp cài đặt (`AssetFileNamePattern`), tệp manifest (`update-manifest.json`), tệp chữ ký (`update-manifest.json.sig`) và Khóa công khai ECDSA.
- **Không yêu cầu PAT:** Tự động kết nối tới kho lưu trữ công khai qua GitHub Releases API chính thức mà không yêu cầu người dùng cung cấp Personal Access Token. Tuyệt đối không nhúng token hoặc khóa bí mật vào ứng dụng.

#### 🛡 Mật Mã Học & Chống Giả Mạo Tuyệt Đối
- **Xác thực chữ ký số ECDSA (NIST P-256 / SHA-256):** Tệp manifest phát hành được ký số bởi Khóa riêng của chủ dự án; ứng dụng chỉ chứa Khóa công khai để xác thực. Bất kỳ sự thay đổi nội dung manifest nào đều bị phát hiện và từ chối.
- **Ràng buộc Manifest:** Bắt buộc khớp phiên bản với release tag, kiến trúc `win-x64`, mã băm SHA-256 64 ký tự hex và URL phát hành HTTPS chính thức.
- **Xác minh mã băm SHA-256 của tệp cài đặt:** Tính toán trực tiếp mã băm của tệp tải về trước khi cho phép cài đặt. Nếu hash không khớp, **tệp cài đặt lập tức bị xóa tự động** để bảo vệ máy tính người dùng.
- **Chống tấn công hạ cấp (Anti-Downgrade):** Sử dụng chuẩn Semantic Versioning so sánh phân đoạn Major.Minor.Patch; chỉ chấp nhận các bản phát hành có phiên bản lớn hơn phiên bản đang chạy.
- **Hỗ trợ Authenticode:** Kiểm tra chữ ký số PE và nhà phát hành tin cậy trên tệp `.exe` đã ký.

#### 💻 Bảo Vệ Windows & Tải Xuống An Toàn
- **Danh sách trắng tên miền (Host Whitelist):** Chỉ chấp nhận tải từ các máy chủ chính thức của GitHub (`github.com`, `objects.githubusercontent.com`), chặn đứng hoàn toàn các URL chuyển hướng độc hại.
- **Giới hạn dung lượng tải:** Ngăn chặn stream vô tận làm tràn ổ đĩa với hạn mức an toàn tối đa 500 MB.
- **Tải về thư mục tạm riêng biệt:** Tải vào `%TEMP%\GiangDevDiskAnalyzer\Updates` mà không ảnh hưởng tới tệp thực thi đang chạy hay dữ liệu người dùng.
- **Hỗ trợ hủy tải (Cancellation):** Người dùng có thể nhấn "HỦY TẢI XUỐNG" bất cứ lúc nào, tệp tải dở được xóa sạch khỏi đĩa ngay lập tức.
- **Khởi chạy cài đặt an toàn:** Kiểm tra định dạng tệp bắt buộc là `.exe` hoặc `.msi` (từ chối hoàn toàn file kịch bản `.bat`, `.cmd`, `.ps1`), yêu cầu quyền quản trị UAC minh bạch và **bắt buộc người dùng xác nhận 2 bước** trước khi khởi chạy.

#### 🎨 Giao Diện Cập Nhật Phần Mềm Cyberpunk Neon (`UpdateView.xaml`)
- **Thẻ KPI tổng quan:** Hiển thị phiên bản đang chạy, phiên bản Stable mới nhất và trạng thái bảo mật/chữ ký.
- **Trình đọc Release Notes:** Hiển thị trực quan toàn bộ nhật ký thay đổi và tính năng mới từ bản phát hành GitHub.
- **Thanh tiến trình thời gian thực:** Hiển thị tỷ lệ phần trăm (0 - 100%), dung lượng đã tải và tốc độ mạng (`MB/s`).
- **Thanh điều hướng Sidebar:** Bổ sung mục "Cập nhật" với biểu tượng `IconUpdate` đồng bộ phong cách Cyberpunk Neon của toàn hệ thống.

#### 🧪 Kiểm Thử Tự Động Toàn Diện
- Bổ sung bộ kiểm thử chuyên sâu `GitHubUpdateTests.cs` gồm 28 kịch bản kiểm thử (mock API, SemVer, ECDSA, SHA-256, URL Whitelist, Rate Limiting, Cancellation, Tampering).
- Toàn bộ **88/88 test cases** chạy thành công 100% (`dotnet test -c Release`).
- Xuất bản bản đóng gói độc lập mới nhất: `publish/win-x64/GIANG.Dev.DiskAnalyzer.App.exe` (164.5 MB).

---

## [1.0.3] - 2026-10-28

### Quyền Administrator Tự Động, Xóa Trực Tiếp Không Popup, Tiến Độ Real-time & Thiết Kế Logo Thương Hiệu

#### 🛡 Quyền Administrator Tự Động (Administrator Privilege Elevation)
- **Tích hợp `app.manifest`:** Khai báo `<requestedExecutionLevel level="requireAdministrator" uiAccess="false" />` giúp ứng dụng tự động yêu cầu quyền Quản trị viên (UAC Administrator) ngay khi khởi động, tránh triệt để lỗi phân quyền và từ chối truy cập khi phân tích hoặc xóa file ở các thư mục sâu.
- **Tiện ích kiểm tra `AdminHelper.cs`:** Triển khai hàm `AdminHelper.IsRunningAsAdministrator()` xác thực vai trò quản trị thông qua `WindowsPrincipal`.
- **Huy hiệu Admin trên giao diện:** Bổ sung huy hiệu `ADMIN` phát sáng viền neon trên thanh TitleBar và trong màn hình `AboutView` ("Chế độ: Quản trị viên (Full Access)").

#### ⚡ Xóa Trực Tiếp Không Hỏi Popup ("Lúc xóa không cần phải hỏi")
- **Loại bỏ hộp thoại xác nhận cản trở:** Loại bỏ hoàn toàn các cửa sổ `MessageBox.Show(..., MessageBoxButton.YesNo)` gây gián đoạn khi bấm xóa trên tất cả các màn hình:
  - Phân tích ổ đĩa (`StorageScanViewModel.cs`)
  - Tìm file lớn (`BigFileHunterViewModel.cs`)
  - Tìm file trùng lặp (`DuplicateFinderViewModel.cs`)
  - Dọn dẹp an toàn (`SafeCleanupViewModel.cs`)
- **Bảo toàn an toàn cốt lõi:** Thao tác xóa vẫn được kiểm duyệt chặt chẽ qua `PathSafetyValidator.cs` để chống xóa nhầm hệ điều hành và file cốt lõi, đồng thời đưa file trực tiếp vào Thùng rác (Recycle Bin) để người dùng có thể khôi phục khi cần.

#### 📊 Cập Nhật Tiến Độ Xóa Thời Gian Thực ("Xóa được bao nhiêu, làm đến đâu")
- **Banner trạng thái động Cyber Red / Neon:** Tự động hiện banner khi tiến trình xóa đang chạy trên giao diện `StorageScanView` và `BigFileHunterView`.
- **Thanh tiến trình phần trăm (0 - 100%):** Hiển thị trực quan tốc độ xử lý xóa file.
- **Thống kê chi tiết Real-time:**
  - Số lượng file đã hoàn thành / Tổng số file cần xóa (vd: `Đang xóa (15/42) [35%]`).
  - Tên file đang được xử lý tại thời điểm hiện tại.
  - Tổng dung lượng byte đã giải phóng vào Thùng rác được định dạng chuẩn (vd: `Đã giải phóng: 3.25 GB`).
  - Kết quả tổng kết hoàn tất hiển thị rõ số file thành công, file thất bại (nếu có) và tổng dung lượng thu hồi được.

#### 🎨 Thiết Kế Logo Thương Hiệu Độc Quyền GIANG DEV
- **Logo Graphic Asset (`logo.png`):** Tạo mẫu logo Cyberpunk Neon chất lượng cao với biểu tượng đĩa kim loại tương lai, khiên bảo vệ lục giác neon cyan/purple và typography góc cạnh GIANG DEV.
- **Component Vector WPF Thuần (`GiangDevLogo.xaml` / `.cs`):** Thiết kế control vector tương thích mọi độ phân giải DPI, vẽ bằng các đường viền gradient đa sắc Neon Cyan (`#00F0FF`) và Neon Purple (`#A855F7`), lồng ghép chữ "GD" Monogram hiện đại.
- **Tích hợp đồng bộ toàn diện:** Nhúng logo vào TitleBar (thanh tiêu đề), Sidebar Header (thương hiệu trên thanh điều hướng), Dashboard (trang chủ) và AboutView (màn hình giới thiệu sản phẩm).

#### 🧪 Kiểm Thử & Đóng Gói Hoàn Thiện
- Bổ sung test case kiểm thử `AdminHelper`, kiểm tra xóa trực tiếp không popup và tiến độ real-time trong `ViewModelButtonActionTests.cs`.
- Toàn bộ **60/60 test cases** chạy thành công 100% (`dotnet test -c Release`).
- Xuất bản bản đóng gói Single-File hoàn thiện: `publish/win-x64/GIANG.Dev.DiskAnalyzer.App.exe` (164.5 MB).

---

## [1.0.2] - 2026-10-28

### Nâng cấp Cơ chế Xóa File & Hỗ trợ Chọn Hàng Loạt (Deletion Overhaul & Batch Selection)

#### 🛡 Sửa đổi & Nâng cấp Cơ chế Xóa File (Deletion Engine Overhaul)
- **Chuẩn hóa Windows Recycle Bin (`RecycleBinService.cs`):** Loại bỏ P/Invoke `SHFileOperation` dễ bị lỗi căn chỉnh bộ nhớ trên x64. Chuyển sang sử dụng `Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile` và `DeleteDirectory` tích hợp sẵn trong .NET Desktop Runtime, bảo đảm 100% tương thích với Windows Recycle Bin trên Windows 10/11 x64.
- **Tự động gỡ thuộc tính Read-Only:** Trước khi xóa, hệ thống tự động kiểm tra và gỡ bỏ cờ `FileAttributes.ReadOnly`, loại bỏ hoàn toàn lỗi `UnauthorizedAccessException` khi xóa các file tải về hoặc file tạm bị khóa thuộc tính.
- **Chuẩn hóa Bộ lọc An toàn Đường dẫn (`PathSafetyValidator.cs`):** Sửa lỗi chặn nhầm các thư mục con có tên "windows" hoặc "boot" trên các ổ đĩa dữ liệu (chỉ áp dụng chặn gốc ổ đĩa và thư mục hệ thống thực sự). Cho phép dọn dẹp các file tạm trong `C:\Windows\Temp` và các file nhật ký `.log`, `.dmp` trong Safe Cleanup.
- **Báo cáo lỗi xóa chi tiết:** Nếu file bị tiến trình khác chiếm dụng hoặc không đủ quyền, thông báo hiển thị chi tiết lý do cụ thể thay vì thất bại ngầm.

#### ✨ Nâng cấp Chọn Hàng Loạt & Bôi Đen Để Chọn (Batch & Multi-Selection UX)
- **Nút "Chọn tất cả" & "Bỏ chọn" toàn diện:** Bổ sung trên thanh công cụ của `StorageScanView`, `BigFileHunterView`, `DuplicateFinderView` và `SafeCleanupView`.
- **Hỗ trợ "Bôi đen để chọn" (Highlight Multi-Select):** Cấu hình `SelectionMode="Extended"` và `SelectionUnit="FullRow"` trên tất cả các DataGrid. Cho phép người dùng kéo chuột hoặc giữ Shift/Ctrl để bôi đen nhiều dòng.
- **Nút "Chọn mục bôi đen":** Cho phép tích chọn hàng loạt tất cả các dòng đang được bôi đen chỉ bằng 1 cú nhấp chuột.
- **Đảo chọn bằng phím Space (Spacebar toggle):** Khi đang bôi đen nhiều dòng trong bảng, nhấn phím Space sẽ tự động đảo trạng thái chọn (`IsSelected`) của tất cả các dòng đang bôi đen.
- **Phím tắt Delete:** Nhấn phím Delete khi đang chọn/bôi đen sẽ mở ngay hộp thoại xác nhận xóa an toàn.
- **Nút "Xóa file đã chọn" dạng hàng loạt:** Bổ sung trực tiếp trên thanh công cụ của `StorageScanView` và `BigFileHunterView` với phong cách Cyber Red, kèm huy hiệu hiển thị số lượng file và tổng dung lượng đã chọn theo thời gian thực.
- **Menu chuột phải thông minh:** Bổ sung các lệnh "✓ Đánh dấu chọn (các dòng bôi đen)", "✗ Bỏ đánh dấu chọn (các dòng bôi đen)", "Chuyển vào Thùng rác (các file đã chọn / bôi đen)".

#### 🧪 Kiểm thử & Xác minh (Verification)
- Bổ sung các test case kiểm tra chọn hàng loạt, bôi đen và xóa file thực tế vào `ViewModelButtonActionTests.cs` và `SafeCleanupAndProtectionTests.cs`.
- Toàn bộ **56/56 test cases** chạy thành công trong **684 ms** (`dotnet test -c Release`).
- Tái xuất bản tệp tin đơn độc lập Release: `publish/win-x64/GIANG.Dev.DiskAnalyzer.App.exe` (164.5 MB).

---

## [1.0.1] - 2026-10-28

### Nâng cấp Kiểm toán Toàn diện & Sửa lỗi UI/UX (Full System Audit & UI Upgrade)

#### 🛠 Sửa lỗi cốt lõi (Core Bug Fixes)
- **ProgressBar Binding Exception (BUG-01):** Sửa lỗi `XamlParseException` liên kết hai chiều vào thuộc tính chỉ đọc `UsedPercentage` trong `DashboardView.xaml` bằng cách thiết lập `Mode=OneWay` tường minh và bổ sung accessor `set { }` bảo vệ trong `DriveItem.cs`. Loại bỏ triệt để ngoại lệ Dispatcher lúc khởi động.
- **Sidebar Menu Navigation Desync (BUG-02):** Khắc phục lỗi lệch trạng thái sáng menu điều hướng bên trái bằng cách triển khai `StringEqualsConverter` với binding hai chiều `IsChecked` kết nối tới `CurrentViewName`, đồng thời hợp nhất 10 mục menu vào cùng nhóm `GroupName="SidebarNav"`.
- **StorageScan Pause Button Display (BUG-03):** Sửa lỗi hiển thị chuỗi boolean thô `"False"/"True"` trên nút Tạm dừng/Tiếp tục bằng việc bổ sung thuộc tính `PauseResumeText` và biểu tượng `IconPause` / `IconPlay`.
- **Watermark TextBoxes (BUG-04):** Nâng cấp template `CyberTextBox` trong `Themes/Styles.xaml`, bổ sung `TextBlock` watermark chìm phản hồi tự động với thuộc tính `Tag` khi ô nhập liệu trống.
- **DataGrid Empty States (BUG-05):** Bổ sung `EmptyToVisibilityConverter` và các panel Empty State phong cách Cyberpunk Neon với biểu tượng và văn bản hướng dẫn chi tiết trên tất cả 7 bảng dữ liệu.
- **BigFileHunter MVVM Binding (BUG-06):** Thay thế toàn bộ mã code-behind click handler trong `BigFileHunterView.xaml.cs` bằng `IntEqualsConverter` liên kết trực tiếp hai chiều với `SelectedThresholdIndex`.
- **Top Folders Interactive Filter (BUG-07):** Bổ sung tương tác lọc danh sách file theo Top thư mục lớn nhất trong `StorageScanView` kèm nút bỏ lọc nhanh.
- **Dynamic Selection Totals (BUG-08):** Triển khai `INotifyPropertyChanged` trên `CleanupItem` và `FileItem`, tự động cập nhật dung lượng đã chọn tức thì khi người dùng tích/bỏ tích checkbox trên bảng.
- **Folder Analyzer Double-Click & Drill-down (BUG-09):** Bổ sung `SelectedSubFolder`, gắn `MouseBinding` nhấp đúp và menu ngữ cảnh để người dùng nhanh chóng đi sâu vào các thư mục con.
- **Duplicate Finder Orphan Group Fix (BUG-10):** Sửa lỗi gán tham chiếu `SelectedGroup` trỏ đến nhóm bị loại bỏ sau khi dọn dẹp các bản sao.
- **Reports List Auto-Reload (BUG-11):** Bổ sung nút "Làm mới" và tự động nạp danh sách báo cáo HTML/CSV khi người dùng chuyển tới trang Báo cáo.
- **Exclusion Folder Browser Dialog (BUG-12):** Tích hợp hộp thoại chọn thư mục Windows `OpenFolderDialog` cho phép chọn nhanh đường dẫn muốn loại trừ trong trang Cài đặt.
- **Recycle Bin Safe Deletion on Big Files & Scanned Files (BUG-13):** Bổ sung tùy chọn chuyển file vào Thùng rác trong menu ngữ cảnh của `StorageScanView` và `BigFileHunterView` với hộp thoại xác nhận 2 bước.
- **Treemap Rendering Performance (BUG-14):** Đóng băng (`Freeze()`) toàn bộ cọ vẽ và bút vẽ trong hàm `OnRender` của `TreemapControl`, tối ưu hóa hiệu năng và giảm áp lực bộ nhớ.

#### 🧪 Kiểm thử & Xác minh
- Bổ sung `ConverterTests.cs` và `ViewModelButtonActionTests.cs` kiểm tra toàn bộ luồng tương tác và chuyển đổi dữ liệu.
- Nâng tổng số test tự động lên **51/51 tests Passed 100%**.
- Tái xuất bản tệp tin đơn độc lập Release: `publish/win-x64/GIANG.Dev.DiskAnalyzer.App.exe` (164.5 MB).

---

## [1.0.0] - 2026-10-28

### Đã phát hành chính thức (Initial Commercial Ready Release)

#### 🚀 Giao diện & Trải nghiệm người dùng (Cyberpunk Neon UI/UX)
- Thiết kế hệ thống bảng màu Cyberpunk Neon chuyên nghiệp (Đen xanh đậm `#0B0F19`, Cyan neon `#00F0FF`, Tím neon `#A855F7`, Xanh lục neon `#10B981`, Đỏ cảnh báo `#EF4444`).
- Tích hợp TitleBar hiện đại với khả năng kéo thả, các nút thu nhỏ, phóng to, đóng cửa sổ và hiển thị nhịp đập trạng thái thời gian thực.
- Thanh Menu Sidebar bên trái với hiệu ứng neon bar phát sáng khi lựa chọn từng phân hệ.
- Bảng điều khiển Dashboard hiển thị thông số vật lý trực tiếp từ các ổ đĩa thực tế trong máy tính.

#### ⚡ Động cơ Quét & Hiệu năng cao (Smart Storage Scanner)
- Duyệt thư mục đệ quy qua cấu trúc Queue bất đồng bộ (`async/await`) chống tràn ngăn xếp.
- Hỗ trợ đầy đủ bộ ba điều khiển: Bắt đầu (Start), Tạm dừng (Pause), Tiếp tục (Resume) và Hủy bỏ (Cancel) với `ManualResetEventSlim` và `CancellationToken`.
- Điều tiết nhịp thông báo UI (Throttling ~80ms) bảo đảm UI thread không bao giờ bị giật lag khi quét thư mục có hàng trăm nghìn file.
- Cơ chế xử lý Reparse Point / Junction chống lặp vô tận và tự động xử lý ngoại lệ `UnauthorizedAccessException`, `PathTooLongException`, `IOException`.

#### 🎯 Săn File Lớn (Big File Hunter)
- Lọc theo các ngưỡng kích thước thông dụng: >100 MB, >500 MB, >1 GB và ngưỡng MB tùy chỉnh.
- Sắp xếp giảm dần theo kích thước, lọc theo đuôi file, tìm kiếm tên file.
- Xuất danh sách file lớn ra định dạng CSV chuẩn RFC 4180.

#### 🔍 Phát hiện File Trùng Lặp Chuẩn Nội Dung (SHA-256 Duplicate Finder)
- Quy trình băm 3 tầng:
  1. Phân nhóm theo kích thước byte chính xác (loại bỏ ngay các file độc nhất).
  2. Băm nhanh tiền tố 4KB (loại bỏ nhanh các file khác biệt nội dung đầu).
  3. Băm toàn diện SHA-256 qua streaming buffer 64KB (không ngốn RAM).
  4. Xác minh đối chiếu byte-by-byte đối với các cặp trùng lặp.
- Tính toán chính xác dung lượng lãng phí có thể tiết kiệm: `(Số bản sao - 1) * Dung lượng`.
- Các công cụ thông minh: Giữ file cũ nhất, Giữ file mới nhất, Bỏ chọn tất cả.
- Chốt chặn bảo vệ: Cấm xóa toàn bộ bản sao trong nhóm nhằm tránh mất dữ liệu gốc.

#### 📊 Phân Tích Thư Mục & Treemap (Folder Analyzer)
- Phân tích đa cấp độ sâu thư mục và tỷ lệ % đóng góp vào thư mục gốc.
- Tự vẽ biểu đồ Treemap phân bố diện tích trực quan theo giải thuật Squarified.
- Cho phép tương tác nhấp đúp để đào sâu (Drill-down) và nút điều hướng lùi lại cấp trước (Back).

#### 🛡 Dọn Dẹp An Toàn (Safe Cleanup)
- Chỉ quét và gợi ý các vùng dữ liệu tạm an toàn: `%TEMP%`, Thumbnail cache, Browser cache dữ liệu web, File log cũ.
- Tích hợp trực tiếp với Windows Shell Recycle Bin (`SHFileOperation`), bảo đảm toàn bộ file xóa đều được đưa vào Thùng rác để có thể phục hồi.
- Cơ chế từ chối nghiêm ngặt đối với thư mục hệ thống Windows, Program Files, và các đuôi mở rộng nhạy cảm (`.sys`, `.dll`, Registry).
- Hộp thoại cảnh báo và bản tóm tắt số file xử lý thành công, thất bại, bỏ qua cùng tổng dung lượng giải phóng.

#### 💾 Lưu Trữ, Lịch Sử & Báo Cáo
- Tích hợp SQLite database lưu trữ lịch sử quét (`scanhistory.db`).
- Tích hợp JSON Settings với khả năng khôi phục mặc định.
- Xuất báo cáo CSV UTF-8 kèm BOM cho Excel và xuất báo cáo HTML giao diện Cyberpunk Neon responsive.

#### 🧪 Kiểm Thử & Đóng Gói
- Hoàn thành bộ 38 bài test tự động (xUnit) phủ toàn bộ 20 kịch bản kiểm thử bắt buộc (100% Passed).
- Tạo kịch bản đóng gói installer Inno Setup (`installer/setup.iss`).
- Xuất bản thành công file chạy độc lập Single-file EXE Windows x64 (`GIANG.Dev.DiskAnalyzer.App.exe`).
