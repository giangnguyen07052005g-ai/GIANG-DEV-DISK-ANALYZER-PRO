# SỔ TAY HƯỚNG DẪN SỬ DỤNG (USER GUIDE)

**GIANG DEV DISK ANALYZER PRO** — Phần mềm phân tích dung lượng ổ đĩa & dọn dẹp hệ thống an toàn trên Windows.

---

## 1. GIAO DIỆN CHÍNH & ĐIỀU HƯỚNG

Khi khởi động ứng dụng, bạn sẽ thấy giao diện đồ họa phong cách **Cyberpunk Neon** với 2 khu vực chính:
1. **Thanh Menu bên trái (Sidebar)**: Chứa logo GIANG DEV và danh sách 10 phân hệ chức năng.
2. **Khu vực hiển thị nội dung (Main Content Area)**: Hiển thị bảng điều khiển, danh sách dữ liệu, biểu đồ và các thanh công cụ tương ứng với phân hệ được chọn.
3. **Thanh Tiêu đề (Custom TitleBar)**: Kéo thả để di chuyển cửa sổ, xem nhịp đập trạng thái hệ thống, thu nhỏ, phóng to và đóng ứng dụng.

---

## 2. HƯỚNG DẪN CHI TIẾT TỪNG PHÂN HỆ

### 2.1. Bảng Điều Khiển (Dashboard)
- **Xem tổng quan lưu trữ**: Hiển thị tổng dung lượng, dung lượng đã sử dụng và còn trống của toàn bộ các ổ đĩa trên máy tính.
- **Danh sách ổ đĩa thực tế**: Xem thẻ thông tin từng ổ (C:, D:,...); bao gồm tên phân vùng, loại ổ đĩa (Fixed, Removable), định dạng tệp (NTFS, FAT32) và thanh phần trăm lưu trữ trực quan.
- **Phân tích nhanh**: Nhấp nút **"Phân tích"** ngay trên thẻ ổ đĩa để chuyển thẳng sang phân hệ quét chi tiết.
- **Nút "Làm mới ổ đĩa"**: Nhấp nút này ở góc phải khi bạn vừa cắm thêm USB hoặc ổ cứng ngoài để hệ thống cập nhật tức thì.

---

### 2.2. Phân Tích Ổ Đĩa (Storage Scan)
- **Chọn phạm vi quét**:
  - Nhập đường dẫn thư mục hoặc nhấp nút **"Chọn thư mục"** để chọn ổ đĩa hoặc một thư mục cụ thể (ví dụ: `D:\Projects`).
  - Tích chọn hoặc bỏ chọn **"Quét thư mục con (Đệ quy)"** tùy nhu cầu.
- **Bắt đầu quét**: Nhấp nút **"Bắt đầu quét"**. Thanh tiến trình màu xanh neon sẽ cập nhật số lượng file, dung lượng và thời gian thực hiện.
- **Tạm dừng & Hủy quét**:
  - Nhấp **"Tạm dừng"** để tạm hoãn quét khi máy tính đang bận.
  - Nhấp **"Hủy quét"** để dừng hoàn toàn quá trình quét.
- **Bảng Thư mục lớn nhất (bên trái)**: Hiển thị Top 10 thư mục chiếm nhiều dung lượng nhất trong phạm vi quét.
- **Bộ lọc & Tìm kiếm (bên phải)**:
  - Gõ từ khóa vào ô tìm kiếm để lọc file theo tên.
  - Chọn đuôi mở rộng từ menu thả xuống (vd: `.mp4`, `.zip`, `.iso`) để lọc định dạng.
- **Thao tác trên danh sách file**:
  - Nhấp chuột phải vào file bất kỳ: Chọn **"Mở vị trí trong File Explorer"** hoặc **"Sao chép đường dẫn file"**.
  - Nhấp nút **"Xuất CSV"** để lưu toàn bộ danh sách kết quả ra file Excel CSV.

---

### 2.3. Săn File Lớn (Big File Hunter)
- **Mục đích**: Tìm ra các file có dung lượng khổng lồ đang âm thầm chiếm dụng tài nguyên đĩa của bạn (ví dụ file cài đặt game, file máy ảo `.vmdk`, file phim 4K).
- **Chọn ngưỡng kích thước**:
  - Chọn nhanh các mốc: `> 100 MB`, `> 500 MB`, `> 1 GB`.
  - Hoặc chọn **Tùy chỉnh (MB)** và gõ số MB mong muốn (ví dụ `250`).
- Nhấp nút **"Bắt đầu săn file"**. Các file lớn nhất sẽ được sắp xếp từ lớn xuống bé.
- *Lưu ý an toàn*: File lớn không nhất thiết là file rác. Hãy kiểm tra kỹ đường dẫn và mục đích trước khi quyết định can thiệp.

---

### 2.4. Tìm File Trùng Lặp (Duplicate Finder)
- **Mục đích**: Tìm chính xác các file có nội dung hoàn toàn giống hệt nhau (kể cả khi chúng được đổi tên thành tên khác).
- **Cách thực hiện**:
  1. Chọn thư mục cần tìm kiếm và nhấp **"Quét trùng lặp"**.
  2. Hệ thống sẽ băm SHA-256 từng file bằng kỹ thuật streaming buffer tiết kiệm RAM.
  3. Khi hoàn tất, danh sách bên trái hiển thị các nhóm trùng lặp kèm mã băm và **dung lượng tiết kiệm tiềm năng**.
  4. Nhấp vào một nhóm bất kỳ để xem danh sách các bản sao ở bảng bên phải.
- **Các nút hỗ trợ thông minh**:
  - **Giữ file cũ nhất**: Tự động đánh dấu xóa các bản sao mới hơn, giữ lại bản sao tạo sớm nhất.
  - **Giữ file mới nhất**: Giữ lại bản sao mới chỉnh sửa gần nhất.
  - **Bỏ chọn hết**: Xóa trắng toàn bộ các dấu tích chọn.
- **Xóa bản sao**:
  - Nhấp nút **"Xóa bản sao đã chọn"**.
  - Hệ thống sẽ hiển thị tổng số file và dung lượng sắp xóa, yêu cầu bạn xác nhận.
  - *Quy tắc an toàn*: Nếu bạn vô tình đánh dấu xóa toàn bộ bản sao trong 1 nhóm, hệ thống sẽ từ chối và cảnh báo để bạn không bị mất dữ liệu gốc.
  - Các file được xóa sẽ được đưa vào **Thùng rác (Recycle Bin)**.

---

### 2.5. Phân Tích Cấu Trúc Thư Mục & Biểu Đồ Treemap (Folder Analyzer)
- **Mục đích**: Khám phá dung lượng các nhánh thư mục từ trên xuống dưới theo dạng bản đồ nhiệt (Treemap).
- **Tương tác**:
  - Nhấp vào một khối màu trên biểu đồ Treemap hoặc nhấp đúp vào thư mục trong bảng danh sách để đi sâu vào nhánh bên trong (**Drill-down**).
  - Nhấp nút **"← Quay lại cấp trên"** để lùi lại thư mục cha.

---

### 2.6. Dọn Dẹp An Toàn (Safe Cleanup)
- **Mục đích**: Giải phóng nhanh các vùng file tạm và cache mà không gây lỗi ứng dụng hay hỏng hóc hệ điều hành.
- **Các vùng hỗ trợ**:
  - *File tạm người dùng (`%TEMP%`)*: Các file tạm do các phần mềm văn phòng, đồ họa tạo ra khi sử dụng.
  - *Cache hình thu nhỏ (Thumbnails)*: File cache hiển thị trước ảnh trong Windows Explorer (Windows sẽ tự sinh lại khi bạn mở lại ảnh).
  - *Bộ nhớ đệm duyệt web (Browser Cache)*: Dữ liệu tải trang web của Chrome, Edge, Discord (tuyệt đối không đụng vào lịch sử, cookie hay mật khẩu lưu trong trình duyệt).
  - *File nhật ký cũ (.log > 24h)*: Các file ghi log phần mềm đã kết thúc từ ngày hôm trước.
- **Quy trình 3 bước an toàn**:
  1. Tích chọn các mục mong muốn và nhấp **"Quét mục dọn dẹp"**.
  2. Xem danh sách chi tiết các file tìm được. Bạn có thể bỏ tích chọn bất kỳ file nào bạn muốn giữ lại.
  3. Nhấp nút màu đỏ **"Xác nhận dọn dẹp (Recycle Bin)"**. Hộp thoại thông báo sẽ hiển thị rõ số lượng file và tổng dung lượng giải phóng để bạn xác nhận lần cuối.
  4. Tất cả file dọn dẹp đều được chuyển vào Recycle Bin, bạn có thể hoàn tác nếu cần.

---

### 2.7. Lịch Sử Quét (Scan History)
- Xem lại lịch sử các phiên quét trước đây được lưu trữ an toàn trong SQLite.
- Nhấp chuột phải vào một dòng để chọn **"Xóa bản ghi này"** hoặc dùng nút **"Xóa toàn bộ lịch sử"**.
- Nhấp **"Xuất CSV"** để lưu lịch sử thành bảng tính.

---

### 2.8. Xuất & Quản Lý Báo Cáo (Reports)
- Nhấp nút **"Tạo báo cáo HTML Cyberpunk"** để tạo một trang web báo cáo chuyên nghiệp có thể gửi cho quản lý hoặc khách hàng.
- Báo cáo HTML được tự động mở bằng trình duyệt mặc định (Chrome/Edge), hiển thị đầy đủ các thẻ số liệu và danh sách chi tiết.

---

### 2.9. Cài Đặt (Settings)
- Tùy chỉnh số lượng luồng quét song song (mặc định 4 luồng).
- Bật/tắt cơ chế đưa vào Thùng rác (Recycle Bin) và hộp thoại cảnh báo trước khi xóa.
- Quản lý danh sách đường dẫn và định dạng file loại trừ (Exclusions).
- Chuyển đổi giữa chế độ Free và Pro để trải nghiệm cấu hình phân quyền tính năng.
- Nút **"Khôi phục mặc định"** giúp bạn đưa mọi thiết lập về trạng thái xuất xưởng bất kỳ lúc nào.

---

*Mọi thắc mắc kỹ thuật vui lòng gửi phản hồi về nhóm phát triển GIANG DEV.*
