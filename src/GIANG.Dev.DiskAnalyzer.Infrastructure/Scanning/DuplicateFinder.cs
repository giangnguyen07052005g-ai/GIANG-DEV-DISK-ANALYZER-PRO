using System.Diagnostics;
using GIANG.Dev.DiskAnalyzer.Core.Enums;
using GIANG.Dev.DiskAnalyzer.Core.Interfaces;
using GIANG.Dev.DiskAnalyzer.Core.Models;
using GIANG.Dev.DiskAnalyzer.Infrastructure.Hashing;
using Microsoft.Extensions.Logging;

namespace GIANG.Dev.DiskAnalyzer.Infrastructure.Scanning;

public class DuplicateFinder : IDuplicateFinder
{
    private readonly IStorageScanner _scanner;
    private readonly ILogger<DuplicateFinder>? _logger;

    public DuplicateFinder(IStorageScanner scanner, ILogger<DuplicateFinder>? logger = null)
    {
        _scanner = scanner;
        _logger = logger;
    }

    public async Task<IReadOnlyList<DuplicateGroup>> FindDuplicatesAsync(
        string path,
        DuplicateScanOptions options,
        IProgress<ScanProgress>? progress,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var progressModel = new ScanProgress
        {
            State = ScanState.Scanning,
            IsIndeterminate = true,
            StatusMessage = "Giai đoạn 1/4: Đang quét danh sách file..."
        };
        progress?.Report(progressModel);

        // Stage 1: Enumerate files in target
        var files = await _scanner.ScanDirectoryAsync(path, options, progress, cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();

        // Stage 2: Group by size and filter out singletons (files with unique sizes cannot be duplicates)
        progressModel.StatusMessage = "Giai đoạn 2/4: Phân nhóm kích thước file...";
        progress?.Report(progressModel);

        var sizeGroups = files
            .Where(f => f.SizeBytes > 0) // zero-byte files can be skipped or handled separately
            .GroupBy(f => f.SizeBytes)
            .Where(g => g.Count() > 1)
            .ToList();

        int candidateFilesCount = sizeGroups.Sum(g => g.Count());
        _logger?.LogInformation("Tìm thấy {GroupsCount} nhóm cùng kích thước ({CandidateCount} files)", sizeGroups.Count, candidateFilesCount);

        if (sizeGroups.Count == 0)
        {
            progressModel.State = ScanState.Completed;
            progressModel.StatusMessage = "Không phát hiện file có cùng kích thước.";
            progress?.Report(progressModel);
            return Array.Empty<DuplicateGroup>();
        }

        // Stage 3 & 4: Fast Prefix Hash + Full SHA-256 for identical candidates
        progressModel.StatusMessage = "Giai đoạn 3/4: Kiểm tra băm SHA-256 nội dung...";
        progressModel.IsIndeterminate = false;
        progress?.Report(progressModel);

        int processedCandidates = 0;
        var duplicateGroups = new List<DuplicateGroup>();
        DateTime lastReport = DateTime.UtcNow;

        foreach (var group in sizeGroups)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var candidateList = group.ToList();

            // Subgroup by prefix hash if enabled and file size > 4KB
            IEnumerable<List<FileItem>> subGroups;
            if (options.FastHashFirst && group.Key > 4096)
            {
                var prefixBuckets = new Dictionary<string, List<FileItem>>();
                foreach (var file in candidateList)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    try
                    {
                        string prefixHash = await BufferedHasher.ComputePrefixHashAsync(file.FullPath, cancellationToken);
                        if (!prefixBuckets.TryGetValue(prefixHash, out var bucket))
                        {
                            bucket = new List<FileItem>();
                            prefixBuckets[prefixHash] = bucket;
                        }
                        bucket.Add(file);
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning(ex, "Bỏ qua file không thể đọc prefix hash: {Path}", file.FullPath);
                    }

                    processedCandidates++;
                    if ((DateTime.UtcNow - lastReport).TotalMilliseconds >= 80)
                    {
                        lastReport = DateTime.UtcNow;
                        progressModel.CurrentPath = file.FullPath;
                        progressModel.EstimatedPercent = (double)processedCandidates / candidateFilesCount * 100.0;
                        progressModel.Elapsed = stopwatch.Elapsed;
                        progressModel.StatusMessage = $"Đang băm nhanh ({processedCandidates}/{candidateFilesCount})...";
                        progress?.Report(progressModel);
                    }
                }

                subGroups = prefixBuckets.Values.Where(b => b.Count > 1).ToList();
            }
            else
            {
                subGroups = new List<List<FileItem>> { candidateList };
            }

            // For each subgroup of identical prefixes, compute full SHA-256
            foreach (var subGroup in subGroups)
            {
                var fullHashBuckets = new Dictionary<string, List<FileItem>>();
                foreach (var file in subGroup)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    try
                    {
                        string fullHash = await BufferedHasher.ComputeFullSha256Async(file.FullPath, cancellationToken);
                        if (!fullHashBuckets.TryGetValue(fullHash, out var bucket))
                        {
                            bucket = new List<FileItem>();
                            fullHashBuckets[fullHash] = bucket;
                        }
                        bucket.Add(file);
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning(ex, "Bỏ qua file không thể đọc SHA-256: {Path}", file.FullPath);
                    }

                    if (!options.FastHashFirst || group.Key <= 4096)
                    {
                        processedCandidates++;
                    }

                    if ((DateTime.UtcNow - lastReport).TotalMilliseconds >= 80)
                    {
                        lastReport = DateTime.UtcNow;
                        progressModel.CurrentPath = file.FullPath;
                        progressModel.EstimatedPercent = (double)processedCandidates / candidateFilesCount * 100.0;
                        progressModel.Elapsed = stopwatch.Elapsed;
                        progressModel.StatusMessage = $"Đang tính SHA-256: {file.FileName}";
                        progress?.Report(progressModel);
                    }
                }

                // Add verified duplicate groups
                foreach (var (hash, matchingFiles) in fullHashBuckets)
                {
                    if (matchingFiles.Count > 1)
                    {
                        // Stage 5 (optional byte-by-byte check)
                        if (options.VerifyByteByByte && matchingFiles.Count == 2)
                        {
                            bool byteMatch = await BufferedHasher.CompareFilesByteByByteAsync(
                                matchingFiles[0].FullPath, matchingFiles[1].FullPath, cancellationToken);
                            if (!byteMatch) continue;
                        }

                        duplicateGroups.Add(new DuplicateGroup
                        {
                            ContentHash = hash,
                            SizeBytes = group.Key,
                            Files = matchingFiles
                        });

                        if (duplicateGroups.Count >= options.MaxResults)
                        {
                            break;
                        }
                    }
                }

                if (duplicateGroups.Count >= options.MaxResults)
                {
                    break;
                }
            }

            if (duplicateGroups.Count >= options.MaxResults)
            {
                break;
            }
        }

        stopwatch.Stop();
        progressModel.State = ScanState.Completed;
        progressModel.EstimatedPercent = 100.0;
        progressModel.Elapsed = stopwatch.Elapsed;
        long totalWasted = duplicateGroups.Sum(g => g.WastedSizeBytes);
        progressModel.StatusMessage = $"Hoàn tất: Tìm thấy {duplicateGroups.Count} nhóm trùng lặp (tiết kiệm tiềm năng: {Core.Helpers.ByteSizeFormatter.Format(totalWasted)}).";
        progress?.Report(progressModel);

        return duplicateGroups.OrderByDescending(g => g.WastedSizeBytes).ToList();
    }
}
