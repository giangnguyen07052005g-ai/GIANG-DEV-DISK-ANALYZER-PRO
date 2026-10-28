using GIANG.Dev.DiskAnalyzer.Core.Helpers;

namespace GIANG.Dev.DiskAnalyzer.Core.Models;

public class DuplicateGroup
{
    public string GroupId { get; set; } = Guid.NewGuid().ToString("N");
    public string ContentHash { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public List<FileItem> Files { get; set; } = new();

    public int FileCount => Files.Count;

    // Potential recoverable space: saving (count - 1) files while keeping at least 1 file
    public long WastedSizeBytes => Files.Count > 1 ? (Files.Count - 1) * SizeBytes : 0L;

    public string SizeFormatted => ByteSizeFormatter.Format(SizeBytes);
    public string WastedFormatted => ByteSizeFormatter.Format(WastedSizeBytes);

    public void MarkKeepOldest()
    {
        if (Files.Count <= 1) return;
        var oldest = Files.OrderBy(f => f.LastModifiedAt).First();
        foreach (var file in Files)
        {
            file.IsSelected = (file != oldest);
        }
    }

    public void MarkKeepNewest()
    {
        if (Files.Count <= 1) return;
        var newest = Files.OrderByDescending(f => f.LastModifiedAt).First();
        foreach (var file in Files)
        {
            file.IsSelected = (file != newest);
        }
    }

    public void ClearSelection()
    {
        foreach (var file in Files)
        {
            file.IsSelected = false;
        }
    }
}
