namespace GIANG.Dev.DiskAnalyzer.Core.Models;

public class ScanReportData
{
    public string Title { get; set; } = "GIANG DEV DISK ANALYZER PRO - Báo cáo phân tích";
    public DateTime GeneratedAt { get; set; } = DateTime.Now;
    public string TargetScope { get; set; } = string.Empty;
    public string ScanType { get; set; } = string.Empty;
    public long TotalFilesScanned { get; set; }
    public long TotalSizeBytes { get; set; }
    public string TotalSizeFormatted => Helpers.ByteSizeFormatter.Format(TotalSizeBytes);
    public TimeSpan Duration { get; set; }
    public int ErrorsCount { get; set; }

    public List<FileItem> TopFiles { get; set; } = new();
    public List<FolderItem> TopFolders { get; set; } = new();
    public List<DuplicateGroup> DuplicateGroups { get; set; } = new();
    public List<string> ErrorsAndWarnings { get; set; } = new();
}
