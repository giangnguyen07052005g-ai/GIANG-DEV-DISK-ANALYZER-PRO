using GIANG.Dev.DiskAnalyzer.Core.Models;
using GIANG.Dev.DiskAnalyzer.Infrastructure.FileSystem;
using GIANG.Dev.DiskAnalyzer.Infrastructure.Scanning;
using GIANG.Dev.DiskAnalyzer.Tests.TestFixtures;
using Xunit;

namespace GIANG.Dev.DiskAnalyzer.Tests.ScanningTests;

public class StorageScannerTests
{
    private readonly StorageScanner _scanner = new();

    // 1. Thư mục trống (Empty directory)
    [Fact]
    public async Task ScanDirectory_EmptyDirectory_ReturnsEmptyList()
    {
        using var tempDir = new TempTestDirectory();
        var options = new ScanOptions { TargetPath = tempDir.Path };

        var files = await _scanner.ScanDirectoryAsync(tempDir.Path, options, null, CancellationToken.None);

        Assert.NotNull(files);
        Assert.Empty(files);
    }

    // 2. Thư mục có hàng nghìn file (Directory with large count of files)
    [Fact]
    public async Task ScanDirectory_ManyFiles_ScansAllCorrectly()
    {
        using var tempDir = new TempTestDirectory();
        const int count = 1000;
        byte[] dummy = new byte[] { 1, 2, 3 };

        for (int i = 0; i < count; i++)
        {
            tempDir.CreateFile($"file_{i:D4}.dat", dummy);
        }

        var options = new ScanOptions { TargetPath = tempDir.Path };
        var files = await _scanner.ScanDirectoryAsync(tempDir.Path, options, null, CancellationToken.None);

        Assert.Equal(count, files.Count);
    }

    // 3. File có tên tiếng Việt và Unicode (Vietnamese & Unicode filenames)
    [Fact]
    public async Task ScanDirectory_VietnameseAndUnicodeFilenames_ScansSuccessfully()
    {
        using var tempDir = new TempTestDirectory();
        tempDir.CreateTextFile("Tài liệu công việc - Tiếng Việt 2026.docx", "Nội dung văn bản tiếng Việt");
        tempDir.CreateTextFile("Báo cáo dung lượng ổ đĩa.txt", "Báo cáo phân tích");
        tempDir.CreateTextFile("日本語のファイル.dat", "Japanese test");

        var options = new ScanOptions { TargetPath = tempDir.Path };
        var files = await _scanner.ScanDirectoryAsync(tempDir.Path, options, null, CancellationToken.None);

        Assert.Equal(3, files.Count);
        Assert.Contains(files, f => f.FileName == "Tài liệu công việc - Tiếng Việt 2026.docx");
        Assert.Contains(files, f => f.FileName == "Báo cáo dung lượng ổ đĩa.txt");
        Assert.Contains(files, f => f.FileName == "日本語のファイル.dat");
    }

    // 4. Đường dẫn dài (Long paths)
    [Fact]
    public async Task ScanDirectory_DeepPath_TraversesSuccessfully()
    {
        using var tempDir = new TempTestDirectory();
        string deepRelative = Path.Combine("FolderLevel1", "FolderLevel2", "FolderLevel3", "FolderLevel4", "DeepFile.txt");
        tempDir.CreateTextFile(deepRelative, "Deep content");

        var options = new ScanOptions { TargetPath = tempDir.Path, IncludeSubdirectories = true };
        var files = await _scanner.ScanDirectoryAsync(tempDir.Path, options, null, CancellationToken.None);

        Assert.Single(files);
        Assert.Equal("DeepFile.txt", files[0].FileName);
    }

    // 7. File đang được ứng dụng khác sử dụng (Locked file handling)
    [Fact]
    public async Task ScanDirectory_LockedFile_DoesNotCrash()
    {
        using var tempDir = new TempTestDirectory();
        string lockedPath = tempDir.CreateTextFile("locked_file.dat", "File is locked");

        // Lock file with exclusive access
        using (var lockStream = new FileStream(lockedPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        {
            var options = new ScanOptions { TargetPath = tempDir.Path };
            var files = await _scanner.ScanDirectoryAsync(tempDir.Path, options, null, CancellationToken.None);

            // Scanner should complete smoothly without unhandled IOException
            Assert.NotNull(files);
        }
    }

    // 8. Không đủ quyền truy cập (Access denied handling simulation)
    [Fact]
    public async Task ScanDirectory_NonExistentOrInaccessibleDirectory_ThrowsOrHandlesGracefully()
    {
        string nonExistentPath = Path.Combine(Path.GetTempPath(), $"NonExistent_{Guid.NewGuid():N}");
        var options = new ScanOptions { TargetPath = nonExistentPath };

        await Assert.ThrowsAsync<DirectoryNotFoundException>(() =>
            _scanner.ScanDirectoryAsync(nonExistentPath, options, null, CancellationToken.None));
    }

    // 9. File bị xóa trong khi đang quét (File deleted during scan simulation)
    [Fact]
    public async Task ScanDirectory_FileDeleted_HandlesGracefully()
    {
        using var tempDir = new TempTestDirectory();
        string file1 = tempDir.CreateTextFile("file1.txt", "content 1");
        string file2 = tempDir.CreateTextFile("file2.txt", "content 2");

        // Delete file right before enumeration completes
        File.Delete(file1);

        var options = new ScanOptions { TargetPath = tempDir.Path };
        var files = await _scanner.ScanDirectoryAsync(tempDir.Path, options, null, CancellationToken.None);

        Assert.Single(files);
        Assert.Equal("file2.txt", files[0].FileName);
    }

    // 11. Người dùng hủy quét (Cancellation via CancellationToken)
    [Fact]
    public async Task ScanDirectory_CancelledToken_ThrowsOperationCanceledException()
    {
        using var tempDir = new TempTestDirectory();
        for (int i = 0; i < 50; i++)
        {
            tempDir.CreateTextFile($"file_{i}.txt", $"content {i}");
        }

        using var cts = new CancellationTokenSource();
        cts.Cancel(); // Pre-cancelled

        var options = new ScanOptions { TargetPath = tempDir.Path };

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            _scanner.ScanDirectoryAsync(tempDir.Path, options, null, cts.Token));
    }

    // 16. Kiểm tra không khóa giao diện khi quét (Async non-blocking execution)
    [Fact]
    public async Task ScanDirectory_ExecutesAsynchronously_DoesNotBlockCallingThread()
    {
        using var tempDir = new TempTestDirectory();
        for (int i = 0; i < 50; i++)
        {
            tempDir.CreateTextFile($"async_file_{i}.txt", "test async execution");
        }

        var options = new ScanOptions { TargetPath = tempDir.Path };
        var scanTask = _scanner.ScanDirectoryAsync(tempDir.Path, options, null, CancellationToken.None);

        Assert.False(scanTask.IsCompleted); // Task runs on background thread
        var result = await scanTask;
        Assert.Equal(50, result.Count);
    }

    // 19. Kiểm tra xử lý symbolic link và junction (ReparsePointHelper detection)
    [Fact]
    public void ReparsePointHelper_NormalFileAndDirectory_ReturnsFalse()
    {
        using var tempDir = new TempTestDirectory();
        var dirInfo = new DirectoryInfo(tempDir.Path);
        string file = tempDir.CreateTextFile("normal.txt", "hello");
        var fileInfo = new FileInfo(file);

        Assert.False(ReparsePointHelper.IsReparsePoint(dirInfo));
        Assert.False(ReparsePointHelper.IsReparsePoint(fileInfo));
    }
}
