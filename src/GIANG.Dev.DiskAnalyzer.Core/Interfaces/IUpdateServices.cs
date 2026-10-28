using GIANG.Dev.DiskAnalyzer.Core.Models;

namespace GIANG.Dev.DiskAnalyzer.Core.Interfaces;

/// <summary>
/// Dịch vụ kết nối và truy vấn GitHub Releases API qua HTTPS.
/// </summary>
public interface IGitHubReleaseService
{
    Task<GitHubRelease?> GetLatestStableReleaseAsync(CancellationToken cancellationToken = default);
    Task<string?> DownloadRawStringAsync(string url, CancellationToken cancellationToken = default);
}

/// <summary>
/// Dịch vụ so sánh phiên bản phần mềm theo chuẩn Semantic Versioning (SemVer).
/// </summary>
public interface IVersionComparisonService
{
    bool IsNewerVersion(string currentVersion, string candidateVersion);
    int Compare(string versionA, string versionB);
    bool IsValidVersion(string version);
    string Normalize(string version);
}

/// <summary>
/// Dịch vụ xác minh chữ ký số mật mã học của tệp Manifest (ECDSA / NIST P-256).
/// </summary>
public interface IUpdateManifestVerifier
{
    bool VerifyManifestSignature(string manifestJson, string signatureBase64, out string errorMessage);
    bool ValidateManifestContent(UpdateManifest manifest, string expectedTag, out string errorMessage);
}

/// <summary>
/// Dịch vụ xác minh mã băm SHA-256 và chữ ký Authenticode của tệp cài đặt tải về.
/// </summary>
public interface IUpdatePackageVerifier
{
    Task<bool> VerifyPackageHashAsync(string filePath, string expectedSha256, CancellationToken cancellationToken = default);
    bool VerifyAuthenticode(string filePath, out string publisherInfo, out string errorMessage);
}

/// <summary>
/// Dịch vụ tải gói cài đặt cập nhật vào thư mục tạm an toàn, hỗ trợ báo cáo tiến độ và hủy tác vụ.
/// </summary>
public interface IUpdateDownloadService
{
    Task<string> DownloadUpdateAsync(
        string downloadUrl,
        string targetFileName,
        long expectedSize,
        IProgress<UpdateDownloadProgress>? progress = null,
        CancellationToken cancellationToken = default);

    void CleanupTempUpdates();
}

/// <summary>
/// Dịch vụ khởi chạy an toàn trình cài đặt cập nhật sau khi người dùng xác nhận.
/// </summary>
public interface IUpdateInstallerService
{
    bool LaunchInstaller(string installerPath, out string errorMessage);
}
