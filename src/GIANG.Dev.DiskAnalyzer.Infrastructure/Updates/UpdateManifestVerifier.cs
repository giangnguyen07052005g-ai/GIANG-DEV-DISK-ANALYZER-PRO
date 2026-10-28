using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using GIANG.Dev.DiskAnalyzer.Core.Configuration;
using GIANG.Dev.DiskAnalyzer.Core.Interfaces;
using GIANG.Dev.DiskAnalyzer.Core.Models;

namespace GIANG.Dev.DiskAnalyzer.Infrastructure.Updates;

/// <summary>
/// Dịch vụ xác minh chữ ký số mật mã học cho tệp manifest (ECDSA NIST P-256).
/// Đảm bảo chống giả mạo manifest và bảo vệ các trường phiên bản, kiến trúc, mã băm SHA-256.
/// </summary>
public class UpdateManifestVerifier : IUpdateManifestVerifier
{
    private readonly GitHubUpdateOptions _options;
    private readonly IVersionComparisonService _versionComparison;
    private static readonly Regex Sha256Regex = new("^[a-fA-F0-9]{64}$", RegexOptions.Compiled);

    public UpdateManifestVerifier(GitHubUpdateOptions options, IVersionComparisonService versionComparison)
    {
        _options = options;
        _versionComparison = versionComparison;
    }

    public bool VerifyManifestSignature(string manifestJson, string signatureBase64, out string errorMessage)
    {
        errorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(manifestJson))
        {
            errorMessage = "Nội dung tệp manifest rỗng hoặc không hợp lệ.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(signatureBase64))
        {
            errorMessage = "Tệp chữ ký số (.sig) rỗng hoặc bị thiếu trên bản phát hành.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(_options.PublicKeyBase64))
        {
            errorMessage = "Chưa cấu hình Khóa công khai (Public Key) của chủ dự án trong ứng dụng.";
            return false;
        }

        try
        {
            byte[] signatureBytes = Convert.FromBase64String(signatureBase64.Trim());
            byte[] manifestBytes = Encoding.UTF8.GetBytes(manifestJson.Trim());
            byte[] publicKeyBytes = Convert.FromBase64String(_options.PublicKeyBase64.Trim());

            using var ecdsa = ECDsa.Create();
            ecdsa.ImportSubjectPublicKeyInfo(publicKeyBytes, out _);

            bool isValid = ecdsa.VerifyData(manifestBytes, signatureBytes, HashAlgorithmName.SHA256);
            if (!isValid)
            {
                errorMessage = "Chữ ký số của tệp manifest KHÔNG HỢP LỆ! Tệp manifest có thể đã bị sửa đổi hoặc tạo bởi nhà phát hành không tin cậy.";
                return false;
            }

            return true;
        }
        catch (FormatException ex)
        {
            errorMessage = $"Định dạng Base64 của chữ ký hoặc khóa công khai không hợp lệ: {ex.Message}";
            return false;
        }
        catch (CryptographicException ex)
        {
            errorMessage = $"Lỗi mật mã học khi xác minh chữ ký số ECDSA: {ex.Message}";
            return false;
        }
        catch (Exception ex)
        {
            errorMessage = $"Lỗi ngoài dự kiến khi xác minh chữ ký manifest: {ex.Message}";
            return false;
        }
    }

    public bool ValidateManifestContent(UpdateManifest manifest, string expectedTag, out string errorMessage)
    {
        errorMessage = string.Empty;

        if (manifest == null)
        {
            errorMessage = "Dữ liệu manifest không tồn tại.";
            return false;
        }

        // 1. Kiểm tra phiên bản manifest phải khớp với tag phát hành
        string normalizedManifestVer = _versionComparison.Normalize(manifest.Version);
        string normalizedTag = _versionComparison.Normalize(expectedTag);

        if (!string.Equals(normalizedManifestVer, normalizedTag, StringComparison.OrdinalIgnoreCase))
        {
            errorMessage = $"Phiên bản trong manifest ({manifest.Version}) không trùng khớp với Tag phát hành ({expectedTag}).";
            return false;
        }

        // 2. Ràng buộc kiến trúc vi xử lý (win-x64)
        if (!string.Equals(manifest.Architecture, "win-x64", StringComparison.OrdinalIgnoreCase))
        {
            errorMessage = $"Kiến trúc của gói cập nhật ({manifest.Architecture}) không tương thích với hệ thống hiện tại (yêu cầu win-x64).";
            return false;
        }

        // 3. Ràng buộc định dạng mã băm SHA-256 (64 ký tự hex)
        if (string.IsNullOrWhiteSpace(manifest.Sha256) || !Sha256Regex.IsMatch(manifest.Sha256))
        {
            errorMessage = "Mã băm SHA-256 trong manifest không hợp lệ hoặc không đúng định dạng 64 ký tự hex.";
            return false;
        }

        // 4. Ràng buộc URL phát hành chính thức
        if (!string.IsNullOrWhiteSpace(manifest.ReleaseUrl))
        {
            if (!Uri.TryCreate(manifest.ReleaseUrl, UriKind.Absolute, out var uri) ||
                uri.Scheme != Uri.UriSchemeHttps ||
                !uri.Host.EndsWith("github.com", StringComparison.OrdinalIgnoreCase))
            {
                errorMessage = "Đường dẫn ReleaseUrl trong manifest không trỏ đến GitHub bảo mật qua HTTPS.";
                return false;
            }
        }

        return true;
    }
}
