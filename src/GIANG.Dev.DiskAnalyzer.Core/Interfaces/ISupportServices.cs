using GIANG.Dev.DiskAnalyzer.Core.Enums;
using GIANG.Dev.DiskAnalyzer.Core.Models;

namespace GIANG.Dev.DiskAnalyzer.Core.Interfaces;

public interface IScanHistoryRepository
{
    Task<IReadOnlyList<ScanHistoryEntry>> GetAllAsync(int limit = 100);
    Task<ScanHistoryEntry?> GetByIdAsync(long id);
    Task AddAsync(ScanHistoryEntry entry);
    Task DeleteAsync(long id);
    Task ClearAllAsync();
}

public interface ISettingsService
{
    AppSettings CurrentSettings { get; }
    Task<AppSettings> LoadSettingsAsync();
    Task SaveSettingsAsync(AppSettings settings);
    Task RestoreDefaultsAsync();
}

public interface IReportService
{
    Task<string> ExportCsvAsync<T>(IEnumerable<T> data, string reportTitle, string outputPath);
    Task<string> ExportHtmlReportAsync(ScanReportData reportData, string outputPath);
}

public interface IFeatureService
{
    FeatureTier CurrentTier { get; set; }
    bool IsFeatureAllowed(string featureKey);
    int GetLimit(string limitKey);
    string TierDisclaimer { get; }
}
