using GIANG.Dev.DiskAnalyzer.Core.Helpers;

namespace GIANG.Dev.DiskAnalyzer.Core.Models;

public class FolderItem
{
    public string FullPath { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public long TotalSizeBytes { get; set; }
    public int FileCount { get; set; }
    public int SubFolderCount { get; set; }
    public int Level { get; set; }
    public string ParentPath { get; set; } = string.Empty;
    public double PercentageOfParent { get; set; }

    public List<FolderItem> Children { get; set; } = new();
    public List<FileItem> LargestFiles { get; set; } = new();

    public string SizeFormatted => ByteSizeFormatter.Format(TotalSizeBytes);
    public string PercentageFormatted => $"{PercentageOfParent:F1}%";
}

public class TreemapNode
{
    public string Name { get; set; } = string.Empty;
    public string FullPath { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string SizeFormatted => ByteSizeFormatter.Format(SizeBytes);
    public double Percentage { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
    public bool IsDirectory { get; set; }
    public string AccentColor { get; set; } = "#00F0FF";
    public List<TreemapNode> Children { get; set; } = new();
}
