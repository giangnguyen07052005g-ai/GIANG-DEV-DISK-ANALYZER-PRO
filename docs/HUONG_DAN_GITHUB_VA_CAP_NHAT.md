# HƯỚNG DẪN CHI TIẾT: ĐƯA SOURCE CODE LÊN GITHUB & TẠO BẢN CẬP NHẬT TỰ ĐỘNG

Tài liệu này được biên soạn dành riêng cho chủ dự án **GIANG DEV DISK ANALYZER PRO**. Hướng dẫn từng bước từ việc tạo kho lưu trữ GitHub, đưa mã nguồn lên mạng, cho đến cách phát hành bản cập nhật để ứng dụng tự động kiểm tra, tải về và cài đặt mà không cần gõ các câu lệnh phức tạp.

---

## MỤC LỤC
1. [Khái niệm cơ bản & Cơ chế hoạt động](#1-khái-niệm-cơ-bản--cơ-chế-hoạt-động)
2. [Phần 1: Cách đưa mã nguồn lên GitHub lần đầu](#phần-1-cách-đưa-mã-nguồn-lên-github-lần-đầu)
   - [Cách A: Dùng GitHub Desktop (Khuyên dùng - 100% giao diện chuột)](#cách-a-dùng-github-desktop-khuyên-dùng---dễ-nhất)
   - [Cách B: Dùng Git dòng lệnh (Dành cho ai quen CLI)](#cách-b-dùng-git-command-line)
3. [Phần 2: Cấu hình tài khoản GitHub của bạn vào ứng dụng](#phần-2-cấu-hình-tài-khoản-github-của-bạn)
4. [Phần 3: Cách tạo bản cập nhật (Release) chỉ với 1 Click](#phần-3-cách-tạo-bản-cập-nhật-release-chỉ-với-1-click)
5. [Phần 4: Kiểm tra và trải nghiệm tính năng tự cập nhật](#phần-4-kiểm-tra-và-trải-nghiệm-tính-năng-tự-cập-nhật)

---

## 1. KHÁI NIỆM CƠ BẢN & CƠ CHẾ HOẠT ĐỘNG

Hệ thống cập nhật của **GIANG DEV DISK ANALYZER PRO** hoạt động theo chuẩn bảo mật cấp thương mại:
1. Khi bấm **"KIỂM TRA CẬP NHẬT"**, phần mềm kết nối bảo mật HTTPS tới GitHub:
   `https://api.github.com/repos/{Tên-GitHub-Của-Bạn}/GIANG-DEV-DISK-ANALYZER-PRO/releases/latest`
2. Phần mềm đọc file `update-manifest.json` và kiểm tra chữ ký số mật mã học `update-manifest.json.sig` bằng khóa công khai ECDSA NIST P-256 tích hợp trong ứng dụng.
3. Nếu chữ ký hợp lệ và có phiên bản mới hơn, nút **"TẢI XUỐNG VÀ CẬP NHẬT"** sáng lên.
4. Tải xong, phần mềm tự kiểm tra mã băm SHA-256 của file `GIANG-DEV-DISK-ANALYZER-PRO-Setup.exe`. Nếu khớp 100% (không bị virus hay hacker can thiệp), nút **"CÀI ĐẶT NGAY"** sẽ chạy bản mới và đóng bản cũ an toàn.

---

## PHẦN 1: CÁCH ĐƯA MÃ NGUỒN LÊN GITHUB LẦN ĐẦU

### Cách A: Dùng GitHub Desktop (Khuyên dùng - Dễ nhất)

GitHub Desktop là phần mềm chính thức của GitHub, có giao diện đồ họa trực quan giúp bạn đẩy code lên mạng chỉ bằng vài cú click chuột:

1. **Tải và cài đặt GitHub Desktop**:
   - Truy cập: [https://desktop.github.com](https://desktop.github.com)
   - Tải về và cài đặt. Đăng nhập bằng tài khoản GitHub của bạn (nếu chưa có tài khoản, hãy đăng ký miễn phí tại [github.com](https://github.com)).
2. **Thêm thư mục dự án vào GitHub Desktop**:
   - Mở GitHub Desktop -> Chọn **File** -> **Add Local Repository...** (hoặc bấm `Ctrl + O`).
   - Chọn đường dẫn đến thư mục dự án: `d:\ToolFB\file-quanly`.
   - Nếu GitHub Desktop thông báo *"This directory does not appear to be a Git repository"*, hãy bấm vào dòng chữ màu xanh **create a repository here**.
   - Trong cửa sổ hiện ra:
     - Name: `GIANG-DEV-DISK-ANALYZER-PRO`
     - Bấm **Create Repository**.
3. **Đẩy mã nguồn lên GitHub (Publish repository)**:
   - Trên thanh công cụ trên cùng của GitHub Desktop, bạn sẽ thấy nút **Publish repository**.
   - Bấm vào nút **Publish repository**.
   - **Lưu ý quan trọng**: Bỏ tích ô *"Keep this code private"* nếu bạn muốn kho lưu trữ là Public (công khai) để người dùng có thể tự do tải cập nhật mà không cần đăng nhập tài khoản.
   - Bấm nút **Publish Repository**.
   - Chờ vài chục giây, toàn bộ mã nguồn của bạn đã nằm an toàn trên GitHub!

---

### Cách B: Dùng Git Command Line

Nếu máy tính đã cài Git, mở PowerShell trong thư mục `d:\ToolFB\file-quanly` và chạy lần lượt:

```powershell
git init
git branch -M main
git add .
git commit -m "Bản phát hành đầu tiên GIANG DEV DISK ANALYZER PRO"
git remote add origin https://github.com/TÊN_GITHUB_CỦA_BẠN/GIANG-DEV-DISK-ANALYZER-PRO.git
git push -u origin main
```

*(Dự án đã có sẵn file `.gitignore` an toàn, tự động loại trừ các file rác và tuyệt đối không đẩy private key lên mạng).*

---

## PHẦN 2: CẤU HÌNH TÀI KHOẢN GITHUB CỦA BẠN

Để phần mềm biết nó cần tìm bản cập nhật từ tài khoản GitHub nào của bạn:

### Cách 1: Chỉnh sửa file `github-update.json` (Nhanh nhất)
Mở file [github-update.json](file:///d:/ToolFB/file-quanly/github-update.json) ở thư mục gốc dự án:
```json
{
  "GitHubOwner": "TÊN_GITHUB_CỦA_BẠN",
  "Repository": "GIANG-DEV-DISK-ANALYZER-PRO",
  "Channel": "Stable"
}
```
*Thay `"TÊN_GITHUB_CỦA_BẠN"` bằng username GitHub thật của bạn (ví dụ: `giangdev123`).*

### Cách 2: Chạy công cụ tự động
Khi chạy công cụ `scripts\prepare-release.bat`, hệ thống sẽ tự hỏi tên GitHub Username của bạn và tự động lưu lại vào code lẫn file cấu hình.

---

## PHẦN 3: CÁCH TẠO BẢN CẬP NHẬT (RELEASE) CHỈ VỚI 1 CLICK

Tôi đã tạo sẵn công cụ tự động hóa toàn diện **`prepare-release.bat`**. Bạn không cần phải tự tính SHA-256 hay làm bất kỳ phép toán ký số nào!

### Bước 1: Chạy công cụ đóng gói
1. Vào thư mục `d:\ToolFB\file-quanly\scripts\`.
2. Nhấp đúp chuột vào file **`prepare-release.bat`**.
3. Màn hình đen hiện ra:
   - Nhập phiên bản mới (ví dụ: `1.0.4`, hoặc bấm `Enter` để dùng mặc định `1.0.4`).
   - Nhập tên GitHub Username của bạn (hoặc bấm `Enter`).
4. Tool sẽ tự động:
   - Build file cài đặt độc lập `GIANG-DEV-DISK-ANALYZER-PRO-Setup.exe`.
   - Tính toán mã băm SHA-256 chính xác từng byte.
   - Tạo khóa ECDSA NIST P-256 bảo mật và ký số file manifest.
   - Tạo ra 3 tệp cập nhật và **tự động mở thư mục chứa 3 tệp đó trên màn hình**.

### 3 tệp được tạo trong thư mục `release-output\v1.0.4\`:
1. `GIANG-DEV-DISK-ANALYZER-PRO-Setup.exe` (File chạy cài đặt phần mềm).
2. `update-manifest.json` (File thông tin phiên bản và mã băm SHA-256).
3. `update-manifest.json.sig` (Chữ ký điện tử chứng minh tệp do chính bạn tạo ra, chống giả mạo).

---

### Bước 2: Đăng bản cập nhật lên GitHub (Kéo thả chuột trên Web)
1. Mở trình duyệt web, vào đường link repository của bạn:
   `https://github.com/TÊN_GITHUB_CỦA_BẠN/GIANG-DEV-DISK-ANALYZER-PRO`
2. Ở cột bên phải, tìm mục **Releases** -> Bấm vào chữ **Draft a new release** (hoặc truy cập `https://github.com/TÊN_GITHUB_CỦA_BẠN/GIANG-DEV-DISK-ANALYZER-PRO/releases/new`).
3. Điền thông tin:
   - **Choose a tag**: Nhập `v1.0.4` rồi bấm *Create new tag: v1.0.4 on publish*.
   - **Release title**: Nhập `GIANG DEV DISK ANALYZER PRO v1.0.4`.
   - **Description**: Ghi các điểm mới (ví dụ: *Tối ưu hiệu năng, sửa lỗi giao diện, hỗ trợ auto update*).
4. **Tải 3 tệp lên**:
   - Ở phía dưới có ô viền nét đứt: *"Attach binaries by dropping them here or selecting them"*.
   - Mở thư mục `release-output\v1.0.4\` (vừa được công cụ mở sẵn).
   - Chọn cả 3 tệp (`GIANG-DEV-DISK-ANALYZER-PRO-Setup.exe`, `update-manifest.json`, `update-manifest.json.sig`) rồi kéo thả vào ô đó.
   - Chờ thanh tải lên đạt 100%.
5. Bấm nút màu xanh **Publish release**.

---

## PHẦN 4: KIỂM TRA VÀ TRẢI NGHIỆM TÍNH NĂNG TỰ CẬP NHẬT

Bây giờ bạn có thể trải nghiệm thành quả:

1. Mở phần mềm **GIANG DEV DISK ANALYZER PRO** (đang ở bản cũ ví dụ `v1.0.3`).
2. Nhấp vào mục **Cập nhật** ở menu bên trái (biểu tượng mũi tên nâng cấp).
3. Bấm **KIỂM TRA CẬP NHẬT**:
   - Ứng dụng sẽ kết nối GitHub và phát hiện bản mới `v1.0.4`.
   - Hiển thị đầy đủ ghi chú phát hành và dung lượng gói cài đặt.
4. Bấm **TẢI XUỐNG VÀ CẬP NHẬT**:
   - Tiến trình tải xuống chạy kèm tốc độ mạng thực tế (MB/s).
   - Phần mềm tự động kiểm tra chữ ký số ECDSA NIST P-256 và mã băm SHA-256.
   - Huy hiệu xanh: *"Bản cập nhật hợp lệ & toàn vẹn"*.
5. Bấm **CÀI ĐẶT NGAY**:
   - Ứng dụng tự động khởi chạy gói cài đặt mới với quyền Administrator và tự đóng phiên bản cũ an toàn!
