using GIANG.Dev.DiskAnalyzer.Core.Enums;
using GIANG.Dev.DiskAnalyzer.Core.Models;

namespace GIANG.Dev.DiskAnalyzer.Core.Interfaces;

public interface IStorageScanner
{
    ScanState State { get; }
    Task<IReadOnlyList<FileItem>> ScanDirectoryAsync(
        string path,
        ScanOptions options,
        IProgress<ScanProgress>? progress,
        CancellationToken cancellationToken);
    void Pause();
    void Resume();
}

public interface IDuplicateFinder
{
    Task<IReadOnlyList<DuplicateGroup>> FindDuplicatesAsync(
        string path,
        DuplicateScanOptions options,
        IProgress<ScanProgress>? progress,
        CancellationToken cancellationToken);
}

public interface IBigFileHunter
{
    Task<IReadOnlyList<FileItem>> FindBigFilesAsync(
        string path,
        long minSizeBytes,
        string? extensionFilter,
        IProgress<ScanProgress>? progress,
        CancellationToken cancellationToken);
}

public interface IFolderAnalyzer
{
    Task<FolderItem> AnalyzeFolderAsync(
        string rootPath,
        int maxDepth,
        IProgress<ScanProgress>? progress,
        CancellationToken cancellationToken);

    List<TreemapNode> BuildTreemapHierarchy(FolderItem root, double width, double height);
}

public interface ISafeCleanupService
{
    Task<IReadOnlyList<CleanupItem>> ScanCleanupTargetsAsync(
        CleanupOptions options,
        IProgress<ScanProgress>? progress,
        CancellationToken cancellationToken);

    Task<CleanupResult> ExecuteCleanupAsync(
        IEnumerable<CleanupItem> items,
        bool sendToRecycleBin,
        IProgress<ScanProgress>? progress,
        CancellationToken cancellationToken);
}
