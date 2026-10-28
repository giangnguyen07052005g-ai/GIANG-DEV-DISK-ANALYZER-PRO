using GIANG.Dev.DiskAnalyzer.Core.Enums;
using GIANG.Dev.DiskAnalyzer.Core.Helpers;

namespace GIANG.Dev.DiskAnalyzer.Core.Models;

public class ScanProgress
{
    public string CurrentPath { get; set; } = string.Empty;
    public long FilesScanned { get; set; }
    public long FoldersScanned { get; set; }
    public long TotalBytesScanned { get; set; }
    public TimeSpan Elapsed { get; set; }
    public double? EstimatedPercent { get; set; }
    public bool IsIndeterminate { get; set; } = true;
    public string StatusMessage { get; set; } = string.Empty;
    public ScanState State { get; set; } = ScanState.Scanning;

    public string TotalBytesFormatted => ByteSizeFormatter.Format(TotalBytesScanned);
    public string ElapsedFormatted => $"{Elapsed.Hours:D2}:{Elapsed.Minutes:D2}:{Elapsed.Seconds:D2}";
}

public class ScanOptions
{
    public string TargetPath { get; set; } = string.Empty;
    public bool IncludeSubdirectories { get; set; } = true;
    public bool FollowSymlinks { get; set; } = false;
    public long MinSizeBytes { get; set; } = 0;
    public string? ExtensionFilter { get; set; }
    public HashSet<string> ExcludedDirectories { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public HashSet<string> ExcludedExtensions { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public class DuplicateScanOptions : ScanOptions
{
    public bool FastHashFirst { get; set; } = true; // Use fast 4KB partial hash to eliminate mismatch
    public bool VerifyByteByByte { get; set; } = true; // Full check before flagging duplicate
    public int MaxResults { get; set; } = int.MaxValue;
}

public class CleanupOptions
{
    public bool IncludeUserTemp { get; set; } = true;
    public bool IncludeWindowsTemp { get; set; } = false; // requires admin, disabled by default
    public bool IncludeThumbnailsCache { get; set; } = true;
    public bool IncludeAppCache { get; set; } = true;
    public bool IncludeOldLogs { get; set; } = true;
    public int MinimumAgeDays { get; set; } = 1; // Only clean items older than 24h
}

public class ScanHistoryEntry
{
    public long Id { get; set; }
    public ScanType ScanType { get; set; }
    public string TargetPath { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public long DurationMs { get; set; }
    public long TotalFiles { get; set; }
    public long TotalSizeBytes { get; set; }
    public int ErrorsCount { get; set; }
    public string SoftwareVersion { get; set; } = "1.0.0 Pro";
    public string SummaryNotes { get; set; } = string.Empty;

    public string TotalSizeFormatted => ByteSizeFormatter.Format(TotalSizeBytes);
    public string StartTimeFormatted => StartTime.ToString("yyyy-MM-dd HH:mm:ss");
    public string DurationFormatted => TimeSpan.FromMilliseconds(DurationMs).ToString(@"hh\:mm\:ss");
    public string ScanTypeFormatted => ScanType switch
    {
        ScanType.FullDrive => "Quét toàn bộ ổ đĩa",
        ScanType.Folder => "Quét thư mục tùy chọn",
        ScanType.BigFiles => "Săn file lớn (Big File Hunter)",
        ScanType.Duplicates => "Tìm file trùng lặp (Duplicate Finder)",
        ScanType.FolderAnalyzer => "Phân tích thư mục (Folder Analyzer)",
        ScanType.SafeCleanup => "Dọn dẹp an toàn (Safe Cleanup)",
        _ => ScanType.ToString()
    };
}

public class AppSettings
{
    public string Theme { get; set; } = "CyberpunkNeon";
    public string VisualEffectsLevel { get; set; } = "High"; // High, Medium, Low
    public string Language { get; set; } = "vi-VN";
    public string ReportExportDirectory { get; set; } = string.Empty;
    public int MaxDegreeOfParallelism { get; set; } = 4;
    public int MaxHistoryEntries { get; set; } = 200;
    public bool ConfirmBeforeDeletion { get; set; } = true;
    public bool UseRecycleBin { get; set; } = true;
    public int AutoRefreshIntervalSeconds { get; set; } = 15;
    public FeatureTier ActiveTier { get; set; } = FeatureTier.Pro;

    public List<string> ExcludedPaths { get; set; } = new()
    {
        @"C:\Windows",
        @"C:\$Recycle.Bin",
        @"System Volume Information"
    };

    public List<string> ExcludedExtensions { get; set; } = new()
    {
        ".sys",
        ".dll"
    };
}
