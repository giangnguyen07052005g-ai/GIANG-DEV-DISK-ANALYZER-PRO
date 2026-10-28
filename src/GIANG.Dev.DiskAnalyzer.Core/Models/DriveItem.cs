using GIANG.Dev.DiskAnalyzer.Core.Helpers;

namespace GIANG.Dev.DiskAnalyzer.Core.Models;

public class DriveItem
{
    public string Name { get; set; } = string.Empty; // e.g. "C:\"
    public string VolumeLabel { get; set; } = string.Empty;
    public string DriveType { get; set; } = string.Empty;
    public string DriveFormat { get; set; } = string.Empty;
    public long TotalSizeBytes { get; set; }
    public long FreeSizeBytes { get; set; }
    public long UsedSizeBytes => Math.Max(0, TotalSizeBytes - FreeSizeBytes);

    public double UsedPercentage
    {
        get => TotalSizeBytes > 0 ? (double)UsedSizeBytes / TotalSizeBytes * 100.0 : 0.0;
        set { }
    }
    public double FreePercentage
    {
        get => TotalSizeBytes > 0 ? (double)FreeSizeBytes / TotalSizeBytes * 100.0 : 0.0;
        set { }
    }

    public bool IsReady { get; set; }

    public string DisplayTitle => string.IsNullOrWhiteSpace(VolumeLabel) 
        ? $"Ổ đĩa ({Name.TrimEnd('\\')})" 
        : $"{VolumeLabel} ({Name.TrimEnd('\\')})";

    public string TotalFormatted => ByteSizeFormatter.Format(TotalSizeBytes);
    public string UsedFormatted => ByteSizeFormatter.Format(UsedSizeBytes);
    public string FreeFormatted => ByteSizeFormatter.Format(FreeSizeBytes);
}
