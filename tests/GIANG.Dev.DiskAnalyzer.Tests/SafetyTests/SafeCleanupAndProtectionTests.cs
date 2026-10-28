using GIANG.Dev.DiskAnalyzer.Core.Enums;
using GIANG.Dev.DiskAnalyzer.Core.Helpers;
using GIANG.Dev.DiskAnalyzer.Core.Models;
using GIANG.Dev.DiskAnalyzer.Infrastructure.Scanning;
using GIANG.Dev.DiskAnalyzer.Infrastructure.WindowsIntegration;
using GIANG.Dev.DiskAnalyzer.Tests.TestFixtures;
using Xunit;

namespace GIANG.Dev.DiskAnalyzer.Tests.SafetyTests;

public class SafeCleanupAndProtectionTests
{
    // 15. Không cho phép xử lý file ngoài phạm vi đã chọn & bảo vệ Windows/Program Files
    [Theory]
    [InlineData(@"C:\Windows\System32\kernel32.dll")]
    [InlineData(@"C:\Windows\explorer.exe")]
    [InlineData(@"C:\Program Files\Common Files\system.dll")]
    [InlineData(@"C:\Program Files (x86)\Microsoft\Edge\edge.exe")]
    [InlineData(@"C:\$Recycle.Bin\S-1-5-21\desktop.ini")]
    [InlineData(@"C:\System Volume Information\tracking.log")]
    public void PathSafetyValidator_SystemProtectedPaths_AreRejected(string systemPath)
    {
        bool isProtected = PathSafetyValidator.IsSystemProtectedPath(systemPath);
        Assert.True(isProtected);

        bool safeForCleanup = PathSafetyValidator.IsSafeForCleanup(systemPath, out string reason);
        Assert.False(safeForCleanup);
        Assert.NotEmpty(reason);
    }

    [Theory]
    [InlineData("driver.sys")]
    [InlineData("malicious.vbs")]
    [InlineData("script.bat")]
    [InlineData("powershell.ps1")]
    public void PathSafetyValidator_DangerousExtensions_AreRejected(string fileName)
    {
        string fakeTempPath = Path.Combine(Path.GetTempPath(), fileName);
        bool safe = PathSafetyValidator.IsSafeForCleanup(fakeTempPath, out string reason);

        Assert.False(safe);
        Assert.Contains("Đuôi file", reason);
    }

    [Fact]
    public void PathSafetyValidator_UserTempFile_IsAllowed()
    {
        string tempFile = Path.Combine(Path.GetTempPath(), "safe_temp_cache_test.tmp");
        bool safe = PathSafetyValidator.IsSafeForCleanup(tempFile, out string reason);

        Assert.True(safe);
        Assert.Empty(reason);
    }

    // 14. Xác nhận xóa và bỏ qua thao tác xóa
    [Fact]
    public async Task SafeCleanupService_ExecutesOnlySelectedItems_SkipsDeselected()
    {
        using var tempDir = new TempTestDirectory();
        string file1 = tempDir.CreateTextFile("temp_file1.tmp", "temp 1");
        string file2 = tempDir.CreateTextFile("temp_file2.tmp", "temp 2");

        var items = new List<CleanupItem>
        {
            new()
            {
                FullPath = file1,
                FileName = "temp_file1.tmp",
                SizeBytes = 6,
                Category = CleanupCategory.UserTemp,
                RiskLevel = RiskLevel.Safe,
                IsSelected = true // Selected
            },
            new()
            {
                FullPath = file2,
                FileName = "temp_file2.tmp",
                SizeBytes = 6,
                Category = CleanupCategory.UserTemp,
                RiskLevel = RiskLevel.Safe,
                IsSelected = false // Unselected! Must be skipped!
            }
        };

        var cleanupService = new SafeCleanupService(new RecycleBinService());
        var result = await cleanupService.ExecuteCleanupAsync(items, sendToRecycleBin: false, null, CancellationToken.None);

        Assert.Equal(1, result.TotalScannedCount);
        Assert.Equal(1, result.SuccessCount);
        Assert.False(File.Exists(file1)); // Selected file was processed
        Assert.True(File.Exists(file2));  // Deselected file MUST remain untouched!
    }

    [Fact]
    public void RecycleBinService_SendToRecycleBin_ShouldSuccessfullyDeleteFile()
    {
        string testFile = Path.Combine(Path.GetTempPath(), $"recycle_test_{Guid.NewGuid()}.tmp");
        File.WriteAllText(testFile, "test delete content");
        Assert.True(File.Exists(testFile));

        var recycleBinService = new RecycleBinService();
        bool success = recycleBinService.SendToRecycleBin(testFile, out string error);

        Assert.True(success, $"SendToRecycleBin failed with error: {error}");
        Assert.False(File.Exists(testFile));
    }
}
