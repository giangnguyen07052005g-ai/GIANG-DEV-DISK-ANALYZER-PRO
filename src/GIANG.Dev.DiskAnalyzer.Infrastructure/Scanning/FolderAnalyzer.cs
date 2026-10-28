using System.Diagnostics;
using GIANG.Dev.DiskAnalyzer.Core.Enums;
using GIANG.Dev.DiskAnalyzer.Core.Interfaces;
using GIANG.Dev.DiskAnalyzer.Core.Models;
using GIANG.Dev.DiskAnalyzer.Infrastructure.FileSystem;
using Microsoft.Extensions.Logging;

namespace GIANG.Dev.DiskAnalyzer.Infrastructure.Scanning;

public class FolderAnalyzer : IFolderAnalyzer
{
    private readonly ILogger<FolderAnalyzer>? _logger;

    private static readonly string[] NeonPalette =
    {
        "#00F0FF", // Cyan
        "#A855F7", // Purple
        "#10B981", // Emerald
        "#F59E0B", // Amber
        "#EC4899", // Pink
        "#3B82F6", // Blue
        "#6366F1", // Indigo
        "#14B8A6"  // Teal
    };

    public FolderAnalyzer(ILogger<FolderAnalyzer>? logger = null)
    {
        _logger = logger;
    }

    public async Task<FolderItem> AnalyzeFolderAsync(
        string rootPath,
        int maxDepth,
        IProgress<ScanProgress>? progress,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(rootPath) || !Directory.Exists(rootPath))
        {
            throw new DirectoryNotFoundException($"Thư mục không tồn tại: {rootPath}");
        }

        var stopwatch = Stopwatch.StartNew();
        var progressModel = new ScanProgress
        {
            State = ScanState.Scanning,
            IsIndeterminate = true,
            StatusMessage = "Đang phân tích cấu trúc thư mục..."
        };
        progress?.Report(progressModel);

        long totalFiles = 0;
        long totalBytes = 0;
        DateTime lastReport = DateTime.UtcNow;

        FolderItem root = await Task.Run(() =>
        {
            var rootDir = new DirectoryInfo(rootPath);
            return BuildFolderTree(rootDir, 0, maxDepth, ref totalFiles, ref totalBytes, progress, progressModel, stopwatch, ref lastReport, cancellationToken);
        }, cancellationToken);

        stopwatch.Stop();
        progressModel.State = ScanState.Completed;
        progressModel.Elapsed = stopwatch.Elapsed;
        progressModel.FilesScanned = totalFiles;
        progressModel.TotalBytesScanned = totalBytes;
        progressModel.StatusMessage = $"Hoàn tất phân tích {totalFiles:N0} files trong {root.Children.Count} thư mục con.";
        progress?.Report(progressModel);

        return root;
    }

    private FolderItem BuildFolderTree(
        DirectoryInfo currentDir,
        int currentDepth,
        int maxDepth,
        ref long totalFiles,
        ref long totalBytes,
        IProgress<ScanProgress>? progress,
        ScanProgress progressModel,
        Stopwatch stopwatch,
        ref DateTime lastReport,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var node = new FolderItem
        {
            FullPath = currentDir.FullName,
            Name = currentDir.Name,
            Level = currentDepth,
            ParentPath = currentDir.Parent?.FullName ?? string.Empty
        };

        long folderTotalBytes = 0;
        int folderFileCount = 0;
        var directFiles = new List<FileItem>();

        // Enumerate direct files
        try
        {
            var files = currentDir.EnumerateFiles();
            foreach (var fi in files)
            {
                cancellationToken.ThrowIfCancellationRequested();
                long size = 0;
                try { size = fi.Length; } catch { continue; }

                folderTotalBytes += size;
                folderFileCount++;
                totalFiles++;
                totalBytes += size;

                var fItem = new FileItem
                {
                    FullPath = fi.FullName,
                    FileName = fi.Name,
                    Extension = fi.Extension,
                    SizeBytes = size,
                    LastModifiedAt = fi.LastWriteTime,
                    Category = FileItem.CategorizeExtension(fi.Extension)
                };
                directFiles.Add(fItem);

                if ((DateTime.UtcNow - lastReport).TotalMilliseconds >= 80)
                {
                    lastReport = DateTime.UtcNow;
                    progressModel.CurrentPath = currentDir.FullName;
                    progressModel.FilesScanned = totalFiles;
                    progressModel.TotalBytesScanned = totalBytes;
                    progressModel.Elapsed = stopwatch.Elapsed;
                    progressModel.StatusMessage = $"Đang phân tích: {currentDir.Name}";
                    progress?.Report(progressModel);
                }
            }
        }
        catch (UnauthorizedAccessException) { }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Lỗi đọc file tại {Dir}", currentDir.FullName);
        }

        node.LargestFiles = directFiles.OrderByDescending(f => f.SizeBytes).Take(10).ToList();

        // Enumerate subfolders
        if (currentDepth < maxDepth)
        {
            try
            {
                var subDirs = currentDir.EnumerateDirectories();
                foreach (var sub in subDirs)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    // Skip reparse points (symlinks/junctions)
                    if (ReparsePointHelper.IsReparsePoint(sub)) continue;

                    var childNode = BuildFolderTree(
                        sub,
                        currentDepth + 1,
                        maxDepth,
                        ref totalFiles,
                        ref totalBytes,
                        progress,
                        progressModel,
                        stopwatch,
                        ref lastReport,
                        cancellationToken);

                    folderTotalBytes += childNode.TotalSizeBytes;
                    folderFileCount += childNode.FileCount;
                    node.Children.Add(childNode);
                }
            }
            catch (UnauthorizedAccessException) { }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Lỗi duyệt thư mục con tại {Dir}", currentDir.FullName);
            }
        }

        node.TotalSizeBytes = folderTotalBytes;
        node.FileCount = folderFileCount;
        node.SubFolderCount = node.Children.Count;

        // Calculate % of parent for children
        if (folderTotalBytes > 0)
        {
            foreach (var child in node.Children)
            {
                child.PercentageOfParent = (double)child.TotalSizeBytes / folderTotalBytes * 100.0;
            }
        }

        node.Children = node.Children.OrderByDescending(c => c.TotalSizeBytes).ToList();
        return node;
    }

    public List<TreemapNode> BuildTreemapHierarchy(FolderItem root, double width, double height)
    {
        if (root == null || width <= 10 || height <= 10)
        {
            return new List<TreemapNode>();
        }

        var result = new List<TreemapNode>();
        var items = root.Children.Where(c => c.TotalSizeBytes > 0).ToList();

        if (items.Count == 0 && root.TotalSizeBytes > 0)
        {
            // Root has only direct files
            result.Add(new TreemapNode
            {
                Name = root.Name,
                FullPath = root.FullPath,
                SizeBytes = root.TotalSizeBytes,
                Percentage = 100.0,
                X = 0,
                Y = 0,
                Width = width,
                Height = height,
                IsDirectory = true,
                AccentColor = NeonPalette[0]
            });
            return result;
        }

        long totalSize = items.Sum(i => i.TotalSizeBytes);
        if (totalSize <= 0) return result;

        // Squarified Treemap layout algorithm
        LayoutTreemap(items, 0, 0, width, height, totalSize, result, 0);
        return result;
    }

    private void LayoutTreemap(
        List<FolderItem> items,
        double x,
        double y,
        double w,
        double h,
        long totalSize,
        List<TreemapNode> result,
        int colorOffset)
    {
        if (items.Count == 0 || w <= 0 || h <= 0) return;

        bool isHorizontal = w >= h;
        double currentOffset = 0;

        for (int i = 0; i < items.Count; i++)
        {
            var item = items[i];
            double ratio = (double)item.TotalSizeBytes / totalSize;

            double nodeX, nodeY, nodeW, nodeH;

            if (isHorizontal)
            {
                nodeX = x + currentOffset;
                nodeY = y;
                nodeW = w * ratio;
                nodeH = h;
                currentOffset += nodeW;
            }
            else
            {
                nodeX = x;
                nodeY = y + currentOffset;
                nodeW = w;
                nodeH = h * ratio;
                currentOffset += nodeH;
            }

            var node = new TreemapNode
            {
                Name = item.Name,
                FullPath = item.FullPath,
                SizeBytes = item.TotalSizeBytes,
                Percentage = ratio * 100.0,
                X = nodeX,
                Y = nodeY,
                Width = Math.Max(0, nodeW - 2), // 2px margin for neon border
                Height = Math.Max(0, nodeH - 2),
                IsDirectory = true,
                AccentColor = NeonPalette[(colorOffset + i) % NeonPalette.Length]
            };

            result.Add(node);
        }
    }
}
