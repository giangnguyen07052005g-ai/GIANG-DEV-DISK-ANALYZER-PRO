using System.Diagnostics;
using GIANG.Dev.DiskAnalyzer.Core.Enums;
using GIANG.Dev.DiskAnalyzer.Core.Helpers;
using GIANG.Dev.DiskAnalyzer.Core.Interfaces;
using GIANG.Dev.DiskAnalyzer.Core.Models;
using GIANG.Dev.DiskAnalyzer.Infrastructure.WindowsIntegration;
using Microsoft.Extensions.Logging;

namespace GIANG.Dev.DiskAnalyzer.Infrastructure.Scanning;

public class SafeCleanupService : ISafeCleanupService
{
    private readonly RecycleBinService _recycleBinService;
    private readonly ILogger<SafeCleanupService>? _logger;

    public SafeCleanupService(RecycleBinService recycleBinService, ILogger<SafeCleanupService>? logger = null)
    {
        _recycleBinService = recycleBinService;
        _logger = logger;
    }

    public async Task<IReadOnlyList<CleanupItem>> ScanCleanupTargetsAsync(
        CleanupOptions options,
        IProgress<ScanProgress>? progress,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var items = new List<CleanupItem>();
        var progressModel = new ScanProgress
        {
            State = ScanState.Scanning,
            IsIndeterminate = true,
            StatusMessage = "Đang quét các vùng lưu trữ tạm an toàn..."
        };
        progress?.Report(progressModel);

        await Task.Run(() =>
        {
            var cutoffDate = DateTime.Now.AddDays(-Math.Max(0, options.MinimumAgeDays));

            // 1. User Temp Directory (%TEMP%)
            if (options.IncludeUserTemp)
            {
                string userTemp = Path.GetTempPath();
                ScanDirectoryForCleanup(
                    userTemp,
                    CleanupCategory.UserTemp,
                    RiskLevel.Safe,
                    "File tạm người dùng (%TEMP%)",
                    cutoffDate,
                    items,
                    cancellationToken);
            }

            // 2. Windows Temp (C:\Windows\Temp)
            if (options.IncludeWindowsTemp)
            {
                string winTemp = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Temp");
                if (Directory.Exists(winTemp))
                {
                    ScanDirectoryForCleanup(
                        winTemp,
                        CleanupCategory.WindowsTemp,
                        RiskLevel.Low,
                        "File tạm hệ thống Windows Temp",
                        cutoffDate,
                        items,
                        cancellationToken);
                }
            }

            // 3. Thumbnail Cache
            if (options.IncludeThumbnailsCache)
            {
                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string thumbDir = Path.Combine(localAppData, @"Microsoft\Windows\Explorer");
                if (Directory.Exists(thumbDir))
                {
                    try
                    {
                        var di = new DirectoryInfo(thumbDir);
                        foreach (var fi in di.EnumerateFiles("thumbcache_*.db"))
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            items.Add(new CleanupItem
                            {
                                FullPath = fi.FullName,
                                FileName = fi.Name,
                                SizeBytes = fi.Length,
                                Category = CleanupCategory.ThumbnailsCache,
                                RiskLevel = RiskLevel.Low,
                                Description = "Cache hình thu nhỏ Explorer (Hệ thống sẽ tự tạo lại khi cần)",
                                LastModifiedAt = fi.LastWriteTime,
                                IsSelected = true
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogDebug(ex, "Không thể quét cache hình thu nhỏ");
                    }
                }
            }

            // 4. Known safe application caches (Chrome/Edge Cache directories only, NOT profile or cookie data!)
            if (options.IncludeAppCache)
            {
                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

                var knownCachePaths = new[]
                {
                    Path.Combine(localAppData, @"Google\Chrome\User Data\Default\Cache\Cache_Data"),
                    Path.Combine(localAppData, @"Microsoft\Edge\User Data\Default\Cache\Cache_Data"),
                    Path.Combine(localAppData, @"Discord\Cache\Cache_Data")
                };

                foreach (var cachePath in knownCachePaths)
                {
                    if (Directory.Exists(cachePath))
                    {
                        ScanDirectoryForCleanup(
                            cachePath,
                            CleanupCategory.ApplicationCache,
                            RiskLevel.Low,
                            "Bộ nhớ đệm duyệt web (Cache web, không ảnh hưởng tài khoản/mật khẩu)",
                            cutoffDate,
                            items,
                            cancellationToken,
                            maxDepth: 1);
                    }
                }
            }
        }, cancellationToken);

        stopwatch.Stop();
        long totalBytes = items.Sum(i => i.SizeBytes);
        progressModel.State = ScanState.Completed;
        progressModel.Elapsed = stopwatch.Elapsed;
        progressModel.FilesScanned = items.Count;
        progressModel.TotalBytesScanned = totalBytes;
        progressModel.StatusMessage = $"Phát hiện {items.Count:N0} mục có thể dọn dẹp ({ByteSizeFormatter.Format(totalBytes)}).";
        progress?.Report(progressModel);

        return items;
    }

    private void ScanDirectoryForCleanup(
        string directoryPath,
        CleanupCategory category,
        RiskLevel riskLevel,
        string description,
        DateTime cutoffDate,
        List<CleanupItem> targetList,
        CancellationToken cancellationToken,
        int maxDepth = 2,
        int currentDepth = 0)
    {
        if (!Directory.Exists(directoryPath)) return;

        try
        {
            var di = new DirectoryInfo(directoryPath);
            foreach (var fi in di.EnumerateFiles())
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Skip locked or non-existent files
                long size;
                try { size = fi.Length; } catch { continue; }

                // Check age
                if (fi.LastWriteTime > cutoffDate)
                {
                    continue; // Skip very recent files that might still be open by processes
                }

                // Verify safety with PathSafetyValidator
                if (!PathSafetyValidator.IsSafeForCleanup(fi.FullName, out _))
                {
                    continue;
                }

                targetList.Add(new CleanupItem
                {
                    FullPath = fi.FullName,
                    FileName = fi.Name,
                    SizeBytes = size,
                    Category = category,
                    RiskLevel = riskLevel,
                    Description = description,
                    LastModifiedAt = fi.LastWriteTime,
                    IsSelected = true
                });
            }

            if (currentDepth < maxDepth)
            {
                foreach (var sub in di.EnumerateDirectories())
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    // Skip if reparse point
                    if (FileSystem.ReparsePointHelper.IsReparsePoint(sub)) continue;

                    ScanDirectoryForCleanup(
                        sub.FullName,
                        category,
                        riskLevel,
                        description,
                        cutoffDate,
                        targetList,
                        cancellationToken,
                        maxDepth,
                        currentDepth + 1);
                }
            }
        }
        catch (UnauthorizedAccessException) { }
        catch (Exception ex)
        {
            _logger?.LogDebug(ex, "Lỗi quét thư mục dọn dẹp: {Path}", directoryPath);
        }
    }

    public async Task<CleanupResult> ExecuteCleanupAsync(
        IEnumerable<CleanupItem> items,
        bool sendToRecycleBin,
        IProgress<ScanProgress>? progress,
        CancellationToken cancellationToken)
    {
        var result = new CleanupResult();
        var itemsToProcess = items.Where(i => i.IsSelected).ToList();
        result.TotalScannedCount = itemsToProcess.Count;

        var progressModel = new ScanProgress
        {
            State = ScanState.Scanning,
            IsIndeterminate = false,
            StatusMessage = "Đang bắt đầu dọn dẹp an toàn..."
        };
        progress?.Report(progressModel);

        int count = 0;
        DateTime lastReport = DateTime.UtcNow;

        await Task.Run(() =>
        {
            foreach (var item in itemsToProcess)
            {
                cancellationToken.ThrowIfCancellationRequested();
                count++;
                result.ProcessedCount++;

                // 1. Re-verify item exists
                if (!File.Exists(item.FullPath))
                {
                    result.SkippedCount++;
                    _logger?.LogInformation("Bỏ qua file đã không còn tồn tại: {Path}", item.FullPath);
                    continue;
                }

                // 2. Re-verify safety boundary
                if (!PathSafetyValidator.IsSafeForCleanup(item.FullPath, out string reason))
                {
                    result.FailedCount++;
                    result.Errors.Add($"Từ chối xử lý [{item.FileName}]: {reason}");
                    _logger?.LogWarning("Từ chối xử lý vi phạm an toàn: {Path} ({Reason})", item.FullPath, reason);
                    continue;
                }

                // 3. Execute safe deletion via Recycle Bin
                try
                {
                    long itemSize = item.SizeBytes;
                    bool success;
                    string errorMsg;

                    if (sendToRecycleBin)
                    {
                        success = _recycleBinService.SendToRecycleBin(item.FullPath, out errorMsg);
                    }
                    else
                    {
                        // Permanent delete with high caution
                        File.Delete(item.FullPath);
                        success = true;
                        errorMsg = string.Empty;
                    }

                    if (success)
                    {
                        result.SuccessCount++;
                        result.TotalBytesFreed += itemSize;
                        result.SuccessItems.Add(item.FullPath);
                        _logger?.LogInformation("Đã dọn dẹp an toàn: {Path}", item.FullPath);
                    }
                    else
                    {
                        result.FailedCount++;
                        result.Errors.Add($"Lỗi đưa vào thùng rác [{item.FileName}]: {errorMsg}");
                    }
                }
                catch (IOException ioEx)
                {
                    result.FailedCount++;
                    result.Errors.Add($"File đang được sử dụng hoặc bị khóa: {item.FileName}");
                    _logger?.LogWarning(ioEx, "File đang bị khóa: {Path}", item.FullPath);
                }
                catch (UnauthorizedAccessException uaEx)
                {
                    result.FailedCount++;
                    result.Errors.Add($"Không đủ quyền truy cập: {item.FileName}");
                    _logger?.LogWarning(uaEx, "Không đủ quyền: {Path}", item.FullPath);
                }
                catch (Exception ex)
                {
                    result.FailedCount++;
                    result.Errors.Add($"Lỗi không xác định: {item.FileName} - {ex.Message}");
                    _logger?.LogError(ex, "Lỗi dọn file: {Path}", item.FullPath);
                }

                if ((DateTime.UtcNow - lastReport).TotalMilliseconds >= 80)
                {
                    lastReport = DateTime.UtcNow;
                    progressModel.EstimatedPercent = (double)count / itemsToProcess.Count * 100.0;
                    progressModel.StatusMessage = $"Đang dọn ({count}/{itemsToProcess.Count}) [{(int)progressModel.EstimatedPercent}%]: {item.FileName} • Đã giải phóng: {ByteSizeFormatter.Format(result.TotalBytesFreed)}";
                    progress?.Report(progressModel);
                }
            }
        }, cancellationToken);

        progressModel.State = ScanState.Completed;
        progressModel.EstimatedPercent = 100.0;
        progressModel.StatusMessage = $"Dọn dẹp hoàn tất: {result.SuccessCount} thành công, {result.FailedCount} lỗi, giải phóng {result.TotalFreedFormatted}.";
        progress?.Report(progressModel);

        return result;
    }
}
