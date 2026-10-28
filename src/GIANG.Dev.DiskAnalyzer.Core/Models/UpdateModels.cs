using System.Text.Json.Serialization;
using GIANG.Dev.DiskAnalyzer.Core.Helpers;

namespace GIANG.Dev.DiskAnalyzer.Core.Models;

/// <summary>
/// Trạng thái xác thực chữ ký và tính toàn vẹn của gói cập nhật.
/// </summary>
public enum UpdateVerificationStatus
{
    NotChecked,
    PendingVerification,
    VerifiedValid,
    SignatureInvalid,
    HashMismatch,
    UntrustedPublisher,
    ManifestInvalid,
    ArchitectureMismatch,
    DowngradeAttemptBlocked,
    DevModeUnsigned
}

/// <summary>
/// Dữ liệu phản hồi phát hành từ GitHub Releases API.
/// </summary>
public class GitHubRelease
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("tag_name")]
    public string TagName { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("body")]
    public string Body { get; set; } = string.Empty;

    [JsonPropertyName("draft")]
    public bool Draft { get; set; }

    [JsonPropertyName("prerelease")]
    public bool Prerelease { get; set; }

    [JsonPropertyName("published_at")]
    public DateTime? PublishedAt { get; set; }

    [JsonPropertyName("html_url")]
    public string HtmlUrl { get; set; } = string.Empty;

    [JsonPropertyName("assets")]
    public List<GitHubAsset> Assets { get; set; } = new();

    public string PublishedAtFormatted => PublishedAt.HasValue
        ? PublishedAt.Value.ToLocalTime().ToString("dd/MM/yyyy HH:mm")
        : "Chưa rõ";
}

/// <summary>
/// Siêu dữ liệu một tài sản đính kèm trong GitHub Release.
/// </summary>
public class GitHubAsset
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("size")]
    public long Size { get; set; }

    [JsonPropertyName("browser_download_url")]
    public string BrowserDownloadUrl { get; set; } = string.Empty;

    [JsonPropertyName("content_type")]
    public string ContentType { get; set; } = string.Empty;

    public string SizeFormatted => ByteSizeFormatter.Format(Size);
}

/// <summary>
/// Siêu dữ liệu của tệp manifest (update-manifest.json) đi kèm bản phát hành.
/// Dùng để ràng buộc phiên bản, kiến trúc, mã băm SHA-256 và chữ ký số.
/// </summary>
public class UpdateManifest
{
    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    [JsonPropertyName("assetName")]
    public string AssetName { get; set; } = string.Empty;

    [JsonPropertyName("architecture")]
    public string Architecture { get; set; } = "win-x64";

    [JsonPropertyName("sha256")]
    public string Sha256 { get; set; } = string.Empty;

    [JsonPropertyName("minSupportedVersion")]
    public string MinSupportedVersion { get; set; } = "1.0.0";

    [JsonPropertyName("releaseUrl")]
    public string ReleaseUrl { get; set; } = string.Empty;

    [JsonPropertyName("releaseDate")]
    public DateTime ReleaseDate { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("changelogSummary")]
    public string ChangelogSummary { get; set; } = string.Empty;
}

/// <summary>
/// Kết quả kiểm tra phiên bản mới từ GitHub Releases.
/// </summary>
public class UpdateCheckResult
{
    public bool IsCheckSuccessful { get; set; }
    public bool IsUpdateAvailable { get; set; }
    public string CurrentVersion { get; set; } = string.Empty;
    public string LatestVersion { get; set; } = string.Empty;
    public GitHubRelease? Release { get; set; }
    public GitHubAsset? InstallerAsset { get; set; }
    public GitHubAsset? ManifestAsset { get; set; }
    public GitHubAsset? SignatureAsset { get; set; }
    public UpdateManifest? Manifest { get; set; }
    public string? ErrorMessage { get; set; }
    public UpdateVerificationStatus VerificationStatus { get; set; } = UpdateVerificationStatus.NotChecked;
}

/// <summary>
/// Tiến trình tải xuống tệp cập nhật theo thời gian thực.
/// </summary>
public class UpdateDownloadProgress
{
    public long BytesDownloaded { get; set; }
    public long TotalBytes { get; set; }
    public double ProgressPercent { get; set; }
    public double SpeedBytesPerSec { get; set; }
    public string StatusMessage { get; set; } = string.Empty;

    public string BytesDownloadedFormatted => ByteSizeFormatter.Format(BytesDownloaded);
    public string TotalBytesFormatted => ByteSizeFormatter.Format(TotalBytes);
    public string SpeedFormatted => $"{ByteSizeFormatter.Format((long)SpeedBytesPerSec)}/s";
}

/// <summary>
/// Thông tin tệp cài đặt sau khi đã tải và xác minh an toàn trong thư mục tạm.
/// </summary>
public class UpdatePackageInfo
{
    public string LocalFilePath { get; set; } = string.Empty;
    public string TargetVersion { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string ComputedSha256 { get; set; } = string.Empty;
    public UpdateVerificationStatus VerificationStatus { get; set; }
    public string VerificationMessage { get; set; } = string.Empty;
    public string? PublisherName { get; set; }

    public string FileSizeFormatted => ByteSizeFormatter.Format(FileSizeBytes);
}
