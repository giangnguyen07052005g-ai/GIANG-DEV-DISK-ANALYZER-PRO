using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using GIANG.Dev.DiskAnalyzer.Core.Configuration;
using GIANG.Dev.DiskAnalyzer.Core.Interfaces;
using GIANG.Dev.DiskAnalyzer.Core.Models;
using Microsoft.Extensions.Logging;

namespace GIANG.Dev.DiskAnalyzer.Infrastructure.Updates;

/// <summary>
/// Dịch vụ kết nối tới GitHub Releases API chính thức qua HTTPS.
/// Không yêu cầu Personal Access Token đối với kho lưu trữ công khai.
/// </summary>
public class GitHubReleaseService : IGitHubReleaseService
{
    private readonly HttpClient _httpClient;
    private readonly GitHubUpdateOptions _options;
    private readonly ILogger<GitHubReleaseService>? _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public GitHubReleaseService(HttpClient httpClient, GitHubUpdateOptions options, ILogger<GitHubReleaseService>? logger = null)
    {
        _httpClient = httpClient;
        _options = options;
        _logger = logger;

        ConfigureDefaultHeaders();
    }

    private void ConfigureDefaultHeaders()
    {
        if (!_httpClient.DefaultRequestHeaders.Contains("User-Agent"))
        {
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "GIANG-DEV-DISK-ANALYZER-PRO-Updater");
        }
        if (!_httpClient.DefaultRequestHeaders.Accept.Any(h => h.MediaType == "application/vnd.github.v3+json"))
        {
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));
        }
        _httpClient.Timeout = TimeSpan.FromSeconds(_options.RequestTimeoutSeconds);
    }

    public async Task<GitHubRelease?> GetLatestStableReleaseAsync(CancellationToken cancellationToken = default)
    {
        string apiUrl = _options.ReleasesApiUrl;
        _logger?.LogInformation("Truy vấn GitHub Releases API: {Url}", apiUrl);

        try
        {
            using var response = await _httpClient.GetAsync(apiUrl, cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                throw new InvalidOperationException(
                    $"Không tìm thấy kho lưu trữ GitHub '{_options.GitHubOwner}/{_options.Repository}'. Vui lòng kiểm tra lại cấu hình tên chủ sở hữu và tên repository.");
            }

            if (response.StatusCode == HttpStatusCode.Forbidden)
            {
                if (response.Headers.TryGetValues("X-RateLimit-Remaining", out var values) && values.FirstOrDefault() == "0")
                {
                    throw new InvalidOperationException("Đã đạt giới hạn tần suất yêu cầu ẩn danh của GitHub API (Rate Limit Exceeded). Vui lòng thử lại sau ít phút.");
                }
                throw new InvalidOperationException("Truy cập GitHub API bị từ chối (403 Forbidden).");
            }

            response.EnsureSuccessStatusCode();

            string json = await response.Content.ReadAsStringAsync(cancellationToken);
            var releases = JsonSerializer.Deserialize<List<GitHubRelease>>(json, JsonOptions);

            if (releases == null || releases.Count == 0)
            {
                _logger?.LogInformation("Kho lưu trữ chưa có bản phát hành (release) nào.");
                return null;
            }

            // Lọc bản phát hành Stable: Bỏ qua Draft và Pre-release
            var latestStable = releases
                .Where(r => !r.Draft && !r.Prerelease)
                .OrderByDescending(r => r.PublishedAt ?? DateTime.MinValue)
                .FirstOrDefault();

            return latestStable;
        }
        catch (HttpRequestException ex)
        {
            _logger?.LogError(ex, "Lỗi kết nối mạng khi gọi GitHub Releases API");
            throw new InvalidOperationException($"Không thể kết nối đến GitHub: {ex.Message}", ex);
        }
        catch (JsonException ex)
        {
            _logger?.LogError(ex, "Lỗi phân tích dữ liệu JSON từ GitHub API");
            throw new InvalidOperationException("Dữ liệu phản hồi từ GitHub API không đúng định dạng.", ex);
        }
    }

    public async Task<string?> DownloadRawStringAsync(string url, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;

        try
        {
            using var response = await _httpClient.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger?.LogWarning("Không tải được tài nguyên từ {Url}: {Status}", url, response.StatusCode);
                return null;
            }

            return await response.Content.ReadAsStringAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Lỗi khi tải chuỗi nội dung từ: {Url}", url);
            return null;
        }
    }
}
