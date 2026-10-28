using GIANG.Dev.DiskAnalyzer.Core.Enums;
using GIANG.Dev.DiskAnalyzer.Core.Models;
using GIANG.Dev.DiskAnalyzer.Infrastructure.Storage;
using GIANG.Dev.DiskAnalyzer.Tests.TestFixtures;
using Xunit;

namespace GIANG.Dev.DiskAnalyzer.Tests.StorageTests;

public class SqliteAndSettingsTests
{
    // 12. Cơ sở dữ liệu SQLite: khởi tạo schema, ghi, đọc và xóa bản ghi
    [Fact]
    public async Task SqliteScanHistoryRepository_CrudOperations_WorkCorrectly()
    {
        using var tempDir = new TempTestDirectory();
        string dbPath = Path.Combine(tempDir.Path, "test_history.db");

        var repo = new SqliteScanHistoryRepository(dbPath);

        var entry = new ScanHistoryEntry
        {
            ScanType = ScanType.FullDrive,
            TargetPath = @"D:\TestScan",
            StartTime = DateTime.Now.AddMinutes(-5),
            EndTime = DateTime.Now,
            DurationMs = 300000,
            TotalFiles = 1250,
            TotalSizeBytes = 1024 * 1024 * 500, // 500MB
            ErrorsCount = 0,
            SoftwareVersion = "1.0.0 Pro",
            SummaryNotes = "Kiểm thử SQLite persistence"
        };

        // Add
        await repo.AddAsync(entry);
        Assert.True(entry.Id > 0);

        // Read
        var all = await repo.GetAllAsync();
        Assert.Single(all);
        Assert.Equal(@"D:\TestScan", all[0].TargetPath);

        // Get by ID
        var byId = await repo.GetByIdAsync(entry.Id);
        Assert.NotNull(byId);
        Assert.Equal(entry.TotalFiles, byId.TotalFiles);

        // Delete
        await repo.DeleteAsync(entry.Id);
        var afterDelete = await repo.GetAllAsync();
        Assert.Empty(afterDelete);
    }

    // 17. Mở và đóng ứng dụng nhiều lần (Settings persistence life cycle)
    [Fact]
    public async Task JsonSettingsService_SaveAndReload_PersistsCorrectly()
    {
        using var tempDir = new TempTestDirectory();
        string settingsFile = Path.Combine(tempDir.Path, "settings.json");

        // First session: create and change settings
        var service1 = new JsonSettingsService(settingsFile);
        var settings1 = await service1.LoadSettingsAsync();
        settings1.MaxDegreeOfParallelism = 8;
        settings1.Language = "vi-VN";
        settings1.UseRecycleBin = true;
        await service1.SaveSettingsAsync(settings1);

        // Second session: re-instantiate service to simulate app restart
        var service2 = new JsonSettingsService(settingsFile);
        var settings2 = await service2.LoadSettingsAsync();

        Assert.Equal(8, settings2.MaxDegreeOfParallelism);
        Assert.Equal("vi-VN", settings2.Language);
        Assert.True(settings2.UseRecycleBin);

        // Test restore defaults
        await service2.RestoreDefaultsAsync();
        Assert.Equal(4, service2.CurrentSettings.MaxDegreeOfParallelism); // Default is 4
    }
}
