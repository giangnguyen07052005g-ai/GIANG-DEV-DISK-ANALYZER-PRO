# GIANG DEV DISK ANALYZER PRO — BÁO CÁO KIỂM THỬ PHẦN MỀM (TEST REPORT)

**Tác giả:** GIANG DEV  
**Ngày kiểm thử:** 28/10/2026  
**Phiên bản:** v1.0.1 PRO (Production Ready)  
**Công cụ kiểm thử:** xUnit v2.9.2, Microsoft.NET.Test.Sdk v17.11.1  
**Môi trường thử nghiệm:** Windows 10/11 x64, .NET 8.0 LTS  

---

## 1. TỔNG HỢP KẾT QUẢ KIỂM THỬ TỰ ĐỘNG

| Dự án kiểm thử | Tổng số test | Số test đạt (Passed) | Thất bại (Failed) | Bỏ qua (Skipped) | Thời gian chạy |
| :--- | :--- | :--- | :--- | :--- | :--- |
| `GIANG.Dev.DiskAnalyzer.Tests` | **51** | **51** | **0** | **0** | **661 ms** |

---

## 2. DANH MỤC CÁC BÀI TEST CHI TIẾT THEO PHÂN HỆ

### 2.1. Phân hệ Quét File Hệ Thống (Scanning & Enumeration)
1. `StorageScannerTests.ScanDirectoryAsync_DiscoversAllFiles`: Xác minh bộ quét đệ quy tìm thấy đầy đủ mọi file trong thư mục giả lập.
2. `StorageScannerTests.ScanDirectoryAsync_CalculatesCorrectSizes`: Kiểm tra độ chính xác của tổng dung lượng byte được tính toán.
3. `StorageScannerTests.ScanDirectoryAsync_ReportsProgress`: Xác minh bộ đếm IProgress nhận được thông tin cập nhật số file và đường dẫn đang quét theo thời gian thực.
4. `StorageScannerTests.ScanDirectoryAsync_CancelsGracefully`: Xác minh phản ứng hủy quét lập tức khi CancellationToken được kích hoạt.
5. `StorageScannerTests.PauseAndResume_WorksCorrectly`: Kiểm tra cơ chế `ManualResetEventSlim` tạm dừng luồng quét và tiếp tục quét bình thường.
6. `StorageScannerTests.ExclusionList_SkipsConfiguredPaths`: Đảm bảo các thư mục nằm trong danh sách loại trừ không bị quét.
7. `BigFileHunterTests.FindBigFilesAsync_ReturnsOnlyFilesAboveThreshold`: Xác minh thuật toán chỉ lọc các file có dung lượng lớn hơn hoặc bằng ngưỡng quy định.
8. `BigFileHunterTests.FindBigFilesAsync_SortsDescendingBySize`: Đảm bảo danh sách file lớn được sắp xếp từ lớn nhất xuống nhỏ nhất.

### 2.2. Phân hệ Băm File & Phát hiện trùng lặp (SHA-256 Duplicate Finder)
9. `DuplicateFinderTests.FindDuplicatesAsync_FindsIdenticalFiles`: Xác minh phát hiện chính xác các file có nội dung trùng lặp nhau.
10. `DuplicateFinderTests.FindDuplicatesAsync_IgnoresDifferentFiles`: Đảm bảo các file có nội dung khác nhau không bị xếp chung nhóm.
11. `DuplicateFinderTests.FindDuplicatesAsync_CalculatesWastedSpaceAccurately`: Kiểm tra công thức tính dung lượng lãng phí = `(số file - 1) * dung lượng file`.
12. `BufferedHasherTests.ComputeFullSha256_ProducesStandardHash`: Xác minh mã băm SHA-256 sinh ra khớp 100% với chuẩn quốc tế.
13. `BufferedHasherTests.ComputePrefixHash_ProcessesFirst16Kb`: Xác minh băm nhanh 16KB đầu tiên để tối ưu hóa hiệu năng quét hàng triệu file.
14. `DuplicateGroupTests.MarkKeepOldest_SelectsAllExceptOldest`: Kiểm tra tính năng tự động đánh dấu xóa bản sao mới, giữ lại bản sao cũ nhất.
15. `DuplicateGroupTests.MarkKeepNewest_SelectsAllExceptNewest`: Kiểm tra tính năng tự động đánh dấu xóa bản sao cũ, giữ lại bản sao mới nhất.

### 2.3. Phân hệ An toàn Dữ liệu & Xóa File (Safety & Windows Recycle Bin)
16. `SafetyRulesTests.IsSystemCriticalDirectory_BlocksWindowsAndProgramFiles`: Xác minh bảo vệ nghiêm ngặt chống xóa tại `C:\Windows`, `C:\Program Files`.
17. `SafetyRulesTests.IsSystemCriticalDirectory_AllowsSafeTempDirectories`: Đảm bảo các thư mục tạm hợp lệ như `%TEMP%` được phép xử lý.
18. `RecycleBinServiceTests.SendToRecycleBin_SendsFileToRecycleBin`: Xác minh API Win32 `SHFileOperationW` chuyển file vào Recycle Bin an toàn.
19. `SafeCleanupTests.ScanCleanupTargets_FindsTempAndCacheFiles`: Xác minh tìm kiếm đầy đủ file trong `%TEMP%` và cache.
20. `SafeCleanupTests.ExecuteCleanup_MovesOnlySelectedFilesToRecycleBin`: Đảm bảo chỉ những file được tích chọn mới được chuyển vào Thùng rác.

### 2.4. Phân hệ Phân tích Thư mục & Treemap (Folder Hierarchy & Visualization)
21. `FolderAnalyzerTests.AnalyzeFolderAsync_BuildsHierarchicalTree`: Xác minh cấu trúc cây thư mục đa tầng cha-con chính xác.
22. `FolderAnalyzerTests.AnalyzeFolderAsync_ComputesFolderSizesRecursively`: Đảm bảo dung lượng thư mục cha bằng tổng dung lượng các thư mục con và file.
23. `FolderAnalyzerTests.BuildTreemapHierarchy_GeneratesGeometricRectangles`: Xác minh thuật toán chia hình chữ nhật không gian Canvas 800x360 không bị đè nhau.

### 2.5. Phân hệ Cơ sở Dữ liệu SQLite (Scan History Database)
24. `SqliteScanHistoryRepositoryTests.AddAndRetrieve_StoresHistoryEntry`: Xác minh ghi và đọc lịch sử quét thành công từ SQLite `scanhistory.db`.
25. `SqliteScanHistoryRepositoryTests.GetAllAsync_SortsByStartTimeDescending`: Đảm bảo thứ tự hiển thị lịch sử từ mới nhất đến cũ nhất.
26. `SqliteScanHistoryRepositoryTests.DeleteAsync_RemovesEntry`: Xác minh xóa một bản ghi cụ thể.
27. `SqliteScanHistoryRepositoryTests.ClearAllAsync_DeletesAllRecords`: Xác minh xóa sạch toàn bộ lịch sử khi người dùng yêu cầu.

### 2.6. Phân hệ Báo cáo & Cài đặt (Reporting & Settings)
28. `ReportServiceTests.GenerateHtmlReport_OutputsCyberpunkHtml`: Kiểm tra định dạng HTML chứa đầy đủ bảng màu Cyberpunk Neon và số liệu thống kê.
29. `ReportServiceTests.ExportCsv_WritesValidCsvFile`: Xác minh xuất tệp tin CSV hợp lệ với mã hóa UTF-8 BOM hiển thị tiếng Việt trên Excel.
30. `SettingsServiceTests.SaveAndLoad_PersistsConfiguration`: Xác minh lưu và nạp cấu hình người dùng từ tệp JSON.
31. `FeatureServiceTests.GetTier_ReturnsCommercialPro`: Kiểm tra bản quyền phiên bản Pro mở khóa toàn bộ giới hạn quét.

### 2.7. Phân hệ Giao diện Người dùng & Value Converters (UI & Converters)
32. `MainWindowUITests.AllViews_CanBeConstructedOnStaThread_WithResources`: Xác minh 100% các View và UserControl WPF khởi tạo hợp lệ trên luồng STA Thread mà không thiếu Resource/Style nào.
33. `MainWindowUITests.MainWindow_TitleAndChrome_InitializedCorrectly`: Kiểm tra tiêu đề và thanh tiêu đề tùy chỉnh WindowChrome.
34. `MainWindowUITests.DashboardView_BindsSessionStats`: Xác minh liên kết dữ liệu thống kê trên Dashboard.
35. `MainWindowUITests.StorageScanView_ContainsPauseButton`: Xác minh nút Tạm dừng hiển thị đúng text và trạng thái.
36. `MainWindowUITests.BigFileHunterView_InitializesThresholds`: Kiểm tra khởi tạo ngưỡng săn file lớn.
37. `MainWindowUITests.DuplicateFinderView_BindsGroupList`: Kiểm tra liên kết danh sách nhóm trùng lặp.
38. `MainWindowUITests.FolderAnalyzerView_BindsTreemapControl`: Kiểm tra điều khiển vẽ Treemap.
39. `MainWindowUITests.SafeCleanupView_BindsCategories`: Kiểm tra các danh mục dọn dẹp hệ thống.
40. `ConverterTests.StringEqualsConverter_EvaluatesAndConvertsBackCorrectly`: Xác minh so sánh chuỗi hai chiều cho Sidebar menu.
41. `ConverterTests.IntEqualsConverter_EvaluatesAndConvertsBackCorrectly`: Xác minh liên kết số nguyên hai chiều cho RadioButton ngưỡng file lớn.
42. `ConverterTests.EmptyToVisibilityConverter_ReturnsVisibleWhenEmptyOrZero`: Xác minh hiển thị Empty State khi danh sách rỗng.
43. `ConverterTests.InverseBoolToVisibilityConverter_FlipsProperly`: Xác minh đảo ngược trạng thái Boolean sang Visibility.

### 2.8. Phân hệ Kiểm thử Hành vi Nút Bấm & Tác Vụ ViewModel (Button & Action Workflows)
44. `ViewModelButtonActionTests.DashboardViewModel_RefreshAndQuickActions_ExecuteCorrectly`: Xác minh nút làm mới ổ đĩa, nút phân tích và 4 nút shortcut điều hướng chính xác.
45. `ViewModelButtonActionTests.StorageScanViewModel_PauseResumeAndFilters_ExecuteCorrectly`: Xác minh nút Tạm dừng/Tiếp tục đổi trạng thái và nhãn, nút lọc và hủy lọc thư mục lớn nhất.
46. `ViewModelButtonActionTests.BigFileHunterViewModel_ThresholdSelection_UpdatesBytesAccurately`: Xác minh các radio button chọn ngưỡng >100MB, >500MB, >1GB và tùy chỉnh MB cập nhật ngưỡng byte tức thì.
47. `ViewModelButtonActionTests.DuplicateFinderViewModel_KeepSelectionRules_UpdateFileFlagsAndUI`: Xác minh các nút "Giữ file cũ nhất", "Giữ file mới nhất", "Bỏ chọn hết" cập nhật cờ `IsSelected` chính xác.
48. `ViewModelButtonActionTests.FolderAnalyzerViewModel_DrillDownAndGoBack_NavigatesHierarchy`: Xác minh tương tác nhấp đúp/nhấp nút Drill-down đi sâu vào thư mục con và nút Quay lại cấp trên.
49. `ViewModelButtonActionTests.SafeCleanupViewModel_ItemSelectionAndDynamicTotals_UpdateAccurately`: Xác minh khi tích/bỏ tích bất kỳ file nào trong danh sách thì tổng dung lượng và số lượng được chọn cập nhật thời gian thực.
50. `ViewModelButtonActionTests.SettingsViewModel_ExclusionsAndDefaults_ExecuteProperly`: Xác minh các nút thêm/xóa thư mục loại trừ, thêm/xóa đuôi file loại trừ, lưu và khôi phục mặc định.
51. `ViewModelButtonActionTests.MainViewModel_NavigateAllViews_SwitchesStateAndSubscribes`: Xác minh điều hướng chuyển đổi qua lại giữa tất cả 10 màn hình của ứng dụng mà không gây rò rỉ tài nguyên.

---

## 3. KẾT LUẬN & ĐÁNH GIÁ CHẤT LƯỢNG

- **Tỷ lệ Pass:** **100%** (51/51 tests).
- **Độ tin cậy luồng xử lý:** Không phát sinh bất kỳ Deadlock, rò rỉ bộ nhớ hoặc xung đột tài nguyên đa luồng nào.
- **Tính toàn vẹn giao diện:** 100% ResourceDictionaries, Brushes, Styles, Converters và ControlTemplates khớp hoàn toàn với kiến trúc WPF MVVM chuẩn mực.
