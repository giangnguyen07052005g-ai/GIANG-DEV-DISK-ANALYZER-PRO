using System.Diagnostics;
using System.IO;
using System.Net.Http;
using GIANG.Dev.DiskAnalyzer.Core.Configuration;
using GIANG.Dev.DiskAnalyzer.Core.Helpers;
using GIANG.Dev.DiskAnalyzer.Core.Interfaces;
using GIANG.Dev.DiskAnalyzer.Core.Models;
using Microsoft.Extensions.Logging;

namespace GIANG.Dev.DiskAnalyzer.Infrastructure.Updates;

/// <summary>
/// Dịch vụ tải gói cài đặt cập nhật với kiểm soát an toàn bảo mật Windows:
/// Xác thực máy chủ trong danh sách trắng (Whitelist), giới hạn dung lượng tải, báo cáo tiến độ và tốc độ mạng thời gian thực.
/// </summary>
public class UpdateDownloadService : IUpdateDownloadService
{
    private readonly HttpClient _httpClient;
    private readonly GitHubUpdateOptions _options;
    private readonly ILogger<UpdateDownloadService>? _logger;

    public UpdateDownloadService(HttpClient httpClient, GitHubUpdateOptions options, ILogger<UpdateDownloadService>? logger = null)
    {
        _httpClient = httpClient;
        _options = options;
        _logger = logger;
    }

    public async Task<string> DownloadUpdateAsync(
        string downloadUrl,
        string targetFileName,
        long expectedSize,
        IProgress<UpdateDownloadProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        // 1. Kiểm tra an toàn URL (Bảo vệ Windows)
        ValidateDownloadUrl(downloadUrl);

        string updateDir = Path.Combine(Path.GetTempPath(), "GiangDevDiskAnalyzer", "Updates");
        Directory.CreateDirectory(updateDir);

        string sanitizedFileName = Path.GetFileName(targetFileName);
        if (string.IsNullOrWhiteSpace(sanitizedFileName))
        {
            sanitizedFileName = "update_setup.exe";
        }

        string tempFilePath = Path.Combine(updateDir, $"{Guid.NewGuid():N}_{sanitizedFileName}");

        _logger?.LogInformation("Bắt đầu tải bản cập nhật từ: {Url} về tệp tạm: {TempPath}", downloadUrl, tempFilePath);

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, downloadUrl);
            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();

            long? totalBytes = response.Content.Headers.ContentLength ?? (expectedSize > 0 ? expectedSize : null);

            // Kiểm tra giới hạn dung lượng tải tối đa (chống tấn công làm tràn đĩa)
            if (totalBytes.HasValue && totalBytes.Value > _options.MaxDownloadSizeBytes)
            {
                throw new InvalidOperationException(
                    $"Kích thước tệp cập nhật ({ByteSizeFormatter.Format(totalBytes.Value)}) vượt quá giới hạn an toàn cho phép ({ByteSizeFormatter.Format(_options.MaxDownloadSizeBytes)}).");
            }

            using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var fileStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true);

            byte[] buffer = new byte[81920]; // 80 KB buffer
            long totalRead = 0;
            int bytesRead;

            var stopwatch = Stopwatch.StartNew();
            long lastReportBytes = 0;
            var lastReportTime = stopwatch.Elapsed;

            while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
            {
                await fileStream.WriteAsync(buffer, 0, bytesRead, cancellationToken);
                totalRead += bytesRead;

                if (totalRead > _options.MaxDownloadSizeBytes)
                {
                    throw new InvalidOperationException("Dung lượng tải thực tế đã vượt quá hạn mức tối đa cho phép.");
                }

                var elapsed = stopwatch.Elapsed;
                var delta = (elapsed - lastReportTime).TotalSeconds;

                if (delta >= 0.2 || totalRead == totalBytes)
                {
                    double speed = (totalRead - lastReportBytes) / Math.Max(0.001, delta);
                    double percent = totalBytes.HasValue && totalBytes.Value > 0
                        ? (double)totalRead / totalBytes.Value * 100.0
                        : 0;

                    progress?.Report(new UpdateDownloadProgress
                    {
                        BytesDownloaded = totalRead,
                        TotalBytes = totalBytes ?? totalRead,
                        ProgressPercent = Math.Min(100.0, percent),
                        SpeedBytesPerSec = speed,
                        StatusMessage = $"Đang tải: {ByteSizeFormatter.Format(totalRead)} / {ByteSizeFormatter.Format(totalBytes ?? totalRead)} ({percent:F1}%)"
                    });

                    lastReportBytes = totalRead;
                    lastReportTime = elapsed;
                }
            }

            progress?.Report(new UpdateDownloadProgress
            {
                BytesDownloaded = totalRead,
                TotalBytes = totalRead,
                ProgressPercent = 100,
                SpeedBytesPerSec = 0,
                StatusMessage = "Tải xuống hoàn tất! Đang tiến hành xác minh an toàn..."
            });

            _logger?.LogInformation("Tải xuống thành công: {Path} ({Bytes} bytes)", tempFilePath, totalRead);
            return tempFilePath;
        }
        catch (OperationCanceledException)
        {
            _logger?.LogWarning("Người dùng đã hủy quá trình tải tệp cập nhật.");
            SafeDeleteFile(tempFilePath);
            throw;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Lỗi xảy ra trong quá trình tải bản cập nhật: {Url}", downloadUrl);
            SafeDeleteFile(tempFilePath);
            throw;
        }
    }

    public void CleanupTempUpdates()
    {
        try
        {
            string updateDir = Path.Combine(Path.GetTempPath(), "GiangDevDiskAnalyzer", "Updates");
            if (Directory.Exists(updateDir))
            {
                var files = Directory.GetFiles(updateDir);
                foreach (var file in files)
                {
                    SafeDeleteFile(file);
                }
            }
        }
        catch
        {
            // Bỏ qua lỗi dọn dẹp thư mục tạm
        }
    }

    private void ValidateDownloadUrl(string downloadUrl)
    {
        if (string.IsNullOrWhiteSpace(downloadUrl) || !Uri.TryCreate(downloadUrl, UriKind.Absolute, out var uri))
        {
            throw new ArgumentException("Đường dẫn tải tệp không phải là một URL hợp lệ.", nameof(downloadUrl));
        }

        if (uri.Scheme != Uri.UriSchemeHttps)
        {
            throw new InvalidOperationException("Chỉ chấp nhận các kết nối bảo mật HTTPS để tải gói cập nhật.");
        }

        bool isAllowedHost = _options.AllowedDownloadHosts.Any(host =>
            uri.Host.Equals(host, StringComparison.OrdinalIgnoreCase) ||
            uri.Host.EndsWith("." + host, StringComparison.OrdinalIgnoreCase));

        if (!isAllowedHost)
        {
            throw new InvalidOperationException($"Máy chủ lưu trữ '{uri.Host}' không nằm trong danh sách an toàn được phê duyệt của GitHub.");
        }
    }

    private static void SafeDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path)) File.Delete(path);
        }
        catch { }
    }
}
