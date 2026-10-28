using System.Diagnostics;
using GIANG.Dev.DiskAnalyzer.Core.Enums;
using GIANG.Dev.DiskAnalyzer.Core.Interfaces;
using GIANG.Dev.DiskAnalyzer.Core.Models;
using GIANG.Dev.DiskAnalyzer.Infrastructure.FileSystem;
using Microsoft.Extensions.Logging;

namespace GIANG.Dev.DiskAnalyzer.Infrastructure.Scanning;

public class StorageScanner : IStorageScanner
{
    private readonly ILogger<StorageScanner>? _logger;
    private readonly ManualResetEventSlim _pauseEvent = new(true);
    private ScanState _state = ScanState.Idle;
    private readonly object _stateLock = new();

    public StorageScanner(ILogger<StorageScanner>? logger = null)
    {
        _logger = logger;
    }

    public ScanState State
    {
        get
        {
            lock (_stateLock)
            {
                return _state;
            }
        }
        private set
        {
            lock (_stateLock)
            {
                _state = value;
            }
        }
    }

    public void Pause()
    {
        lock (_stateLock)
        {
            if (_state == ScanState.Scanning)
            {
                _pauseEvent.Reset();
                _state = ScanState.Paused;
                _logger?.LogInformation("Quá trình quét đã được tạm dừng.");
            }
        }
    }

    public void Resume()
    {
        lock (_stateLock)
        {
            if (_state == ScanState.Paused)
            {
                _pauseEvent.Set();
                _state = ScanState.Scanning;
                _logger?.LogInformation("Quá trình quét tiếp tục.");
            }
        }
    }

    public async Task<IReadOnlyList<FileItem>> ScanDirectoryAsync(
        string path,
        ScanOptions options,
        IProgress<ScanProgress>? progress,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
        {
            throw new DirectoryNotFoundException($"Thư mục quét không tồn tại: {path}");
        }

        State = ScanState.Scanning;
        _pauseEvent.Set();

        var stopwatch = Stopwatch.StartNew();
        var filesList = new List<FileItem>();
        var dirQueue = new Queue<string>();
        dirQueue.Enqueue(path);

        long filesScanned = 0;
        long foldersScanned = 0;
        long totalBytesScanned = 0;
        DateTime lastReportTime = DateTime.MinValue;

        var progressModel = new ScanProgress
        {
            State = ScanState.Scanning,
            IsIndeterminate = true,
            StatusMessage = "Đang quét..."
        };

        try
        {
            await Task.Run(() =>
            {
                while (dirQueue.Count > 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    _pauseEvent.Wait(cancellationToken);

                    string currentDir = dirQueue.Dequeue();
                    foldersScanned++;

                    DirectoryInfo dirInfo;
                    try
                    {
                        dirInfo = new DirectoryInfo(currentDir);
                        if (!dirInfo.Exists) continue;
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning(ex, "Không thể đọc thông tin thư mục: {Path}", currentDir);
                        continue;
                    }

                    // Reparse point check (Junction / Symlink)
                    if (!options.FollowSymlinks && ReparsePointHelper.IsReparsePoint(dirInfo))
                    {
                        continue;
                    }

                    // Check excluded directories
                    if (options.ExcludedDirectories.Contains(dirInfo.Name))
                    {
                        continue;
                    }

                    // Enumerate files in current directory
                    try
                    {
                        IEnumerable<FileInfo> fileInfos;
                        try
                        {
                            fileInfos = dirInfo.EnumerateFiles();
                        }
                        catch (UnauthorizedAccessException)
                        {
                            _logger?.LogDebug("Từ chối quyền truy cập thư mục: {Path}", currentDir);
                            continue;
                        }
                        catch (PathTooLongException)
                        {
                            _logger?.LogWarning("Đường dẫn quá dài vượt giới hạn: {Path}", currentDir);
                            continue;
                        }
                        catch (Exception ex)
                        {
                            _logger?.LogWarning(ex, "Lỗi đọc file từ thư mục: {Path}", currentDir);
                            continue;
                        }

                        foreach (var fi in fileInfos)
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            _pauseEvent.Wait(cancellationToken);

                            long fileSize = 0;
                            try
                            {
                                fileSize = fi.Length;
                            }
                            catch
                            {
                                // File might have been deleted or locked
                                continue;
                            }

                            filesScanned++;
                            totalBytesScanned += fileSize;

                            string ext = fi.Extension;
                            if (options.ExcludedExtensions.Contains(ext))
                            {
                                continue;
                            }

                            if (options.MinSizeBytes > 0 && fileSize < options.MinSizeBytes)
                            {
                                continue;
                            }

                            if (!string.IsNullOrEmpty(options.ExtensionFilter) &&
                                !string.Equals(ext, options.ExtensionFilter, StringComparison.OrdinalIgnoreCase))
                            {
                                continue;
                            }

                            var item = new FileItem
                            {
                                FullPath = fi.FullName,
                                FileName = fi.Name,
                                Extension = ext,
                                DirectoryPath = currentDir,
                                SizeBytes = fileSize,
                                CreatedAt = SafeGetCreationTime(fi),
                                LastModifiedAt = SafeGetLastWriteTime(fi),
                                LastAccessedAt = SafeGetLastAccessTime(fi),
                                Category = FileItem.CategorizeExtension(ext),
                                IsReadOnly = (fi.Attributes & FileAttributes.ReadOnly) != 0,
                                IsHidden = (fi.Attributes & FileAttributes.Hidden) != 0,
                                IsSystem = (fi.Attributes & FileAttributes.System) != 0
                            };

                            filesList.Add(item);

                            // Throttle progress notifications to ~80ms intervals
                            if ((DateTime.UtcNow - lastReportTime).TotalMilliseconds >= 80)
                            {
                                lastReportTime = DateTime.UtcNow;
                                progressModel.CurrentPath = fi.FullName;
                                progressModel.FilesScanned = filesScanned;
                                progressModel.FoldersScanned = foldersScanned;
                                progressModel.TotalBytesScanned = totalBytesScanned;
                                progressModel.Elapsed = stopwatch.Elapsed;
                                progressModel.StatusMessage = $"Đang quét: {fi.Name}";
                                progress?.Report(progressModel);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning(ex, "Lỗi xử lý file trong thư mục: {Path}", currentDir);
                    }

                    // Enumerate subdirectories if recursive is enabled
                    if (options.IncludeSubdirectories)
                    {
                        try
                        {
                            var subDirs = dirInfo.EnumerateDirectories();
                            foreach (var sub in subDirs)
                            {
                                if (!options.FollowSymlinks && ReparsePointHelper.IsReparsePoint(sub))
                                {
                                    continue;
                                }

                                if (options.ExcludedDirectories.Contains(sub.Name))
                                {
                                    continue;
                                }

                                dirQueue.Enqueue(sub.FullName);
                            }
                        }
                        catch (UnauthorizedAccessException)
                        {
                            _logger?.LogDebug("Từ chối quyền truy cập thư mục con: {Path}", currentDir);
                        }
                        catch (Exception ex)
                        {
                            _logger?.LogWarning(ex, "Lỗi khi duyệt thư mục con: {Path}", currentDir);
                        }
                    }
                }
            }, cancellationToken);

            State = ScanState.Completed;
            progressModel.State = ScanState.Completed;
            progressModel.FilesScanned = filesScanned;
            progressModel.FoldersScanned = foldersScanned;
            progressModel.TotalBytesScanned = totalBytesScanned;
            progressModel.Elapsed = stopwatch.Elapsed;
            progressModel.StatusMessage = $"Hoàn tất: Đã quét {filesScanned:N0} files ({Core.Helpers.ByteSizeFormatter.Format(totalBytesScanned)}).";
            progress?.Report(progressModel);

            return filesList;
        }
        catch (OperationCanceledException)
        {
            State = ScanState.Cancelled;
            progressModel.State = ScanState.Cancelled;
            progressModel.StatusMessage = "Quá trình quét đã bị người dùng hủy.";
            progress?.Report(progressModel);
            throw;
        }
        catch (Exception ex)
        {
            State = ScanState.Failed;
            progressModel.State = ScanState.Failed;
            progressModel.StatusMessage = $"Quét thất bại: {ex.Message}";
            progress?.Report(progressModel);
            _logger?.LogError(ex, "Quét thư mục thất bại: {Path}", path);
            throw;
        }
        finally
        {
            stopwatch.Stop();
        }
    }

    private static DateTime SafeGetCreationTime(FileInfo fi)
    {
        try { return fi.CreationTime; } catch { return DateTime.MinValue; }
    }

    private static DateTime SafeGetLastWriteTime(FileInfo fi)
    {
        try { return fi.LastWriteTime; } catch { return DateTime.MinValue; }
    }

    private static DateTime SafeGetLastAccessTime(FileInfo fi)
    {
        try { return fi.LastAccessTime; } catch { return DateTime.MinValue; }
    }
}
