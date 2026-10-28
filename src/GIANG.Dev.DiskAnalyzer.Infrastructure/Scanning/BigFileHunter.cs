using GIANG.Dev.DiskAnalyzer.Core.Interfaces;
using GIANG.Dev.DiskAnalyzer.Core.Models;
using Microsoft.Extensions.Logging;

namespace GIANG.Dev.DiskAnalyzer.Infrastructure.Scanning;

public class BigFileHunter : IBigFileHunter
{
    private readonly IStorageScanner _scanner;
    private readonly ILogger<BigFileHunter>? _logger;

    public BigFileHunter(IStorageScanner scanner, ILogger<BigFileHunter>? logger = null)
    {
        _scanner = scanner;
        _logger = logger;
    }

    public async Task<IReadOnlyList<FileItem>> FindBigFilesAsync(
        string path,
        long minSizeBytes,
        string? extensionFilter,
        IProgress<ScanProgress>? progress,
        CancellationToken cancellationToken)
    {
        var options = new ScanOptions
        {
            TargetPath = path,
            IncludeSubdirectories = true,
            MinSizeBytes = minSizeBytes,
            ExtensionFilter = extensionFilter
        };

        _logger?.LogInformation("Bắt đầu săn file lớn trong {Path} với ngưỡng >= {MinSize} bytes", path, minSizeBytes);

        var files = await _scanner.ScanDirectoryAsync(path, options, progress, cancellationToken);

        return files.OrderByDescending(f => f.SizeBytes).ToList();
    }
}
