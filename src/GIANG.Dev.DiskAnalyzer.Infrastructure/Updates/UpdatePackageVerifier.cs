using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using GIANG.Dev.DiskAnalyzer.Core.Configuration;
using GIANG.Dev.DiskAnalyzer.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace GIANG.Dev.DiskAnalyzer.Infrastructure.Updates;

/// <summary>
/// Dịch vụ xác minh mã băm SHA-256 và chữ ký số Authenticode của tệp cài đặt cập nhật.
/// Tự động xóa bỏ hoặc cách ly tệp nếu phát hiện bất kỳ sự sai lệch toàn vẹn nào.
/// </summary>
public class UpdatePackageVerifier : IUpdatePackageVerifier
{
    private readonly GitHubUpdateOptions _options;
    private readonly ILogger<UpdatePackageVerifier>? _logger;

    public UpdatePackageVerifier(GitHubUpdateOptions options, ILogger<UpdatePackageVerifier>? logger = null)
    {
        _options = options;
        _logger = logger;
    }

    public async Task<bool> VerifyPackageHashAsync(string filePath, string expectedSha256, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            _logger?.LogError("Không tìm thấy tệp cập nhật để kiểm tra băm: {Path}", filePath);
            return false;
        }

        if (string.IsNullOrWhiteSpace(expectedSha256))
        {
            _logger?.LogError("Mã băm SHA-256 kỳ vọng rỗng.");
            SafeDeleteFile(filePath);
            return false;
        }

        try
        {
            string computedHash = await ComputeFileSha256Async(filePath, cancellationToken);
            bool isMatch = string.Equals(computedHash, expectedSha256.Trim(), StringComparison.OrdinalIgnoreCase);

            if (!isMatch)
            {
                _logger?.LogError("Mã băm SHA-256 KHÔNG KHỚP! Tính toán: {Computed}, Kỳ vọng: {Expected}. Đang hủy tệp lỗi...", computedHash, expectedSha256);
                SafeDeleteFile(filePath);
                return false;
            }

            _logger?.LogInformation("Xác minh mã băm SHA-256 thành công ({Hash})", computedHash);
            return true;
        }
        catch (OperationCanceledException)
        {
            SafeDeleteFile(filePath);
            throw;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Lỗi khi tính toán mã băm SHA-256 cho tệp: {Path}", filePath);
            SafeDeleteFile(filePath);
            return false;
        }
    }

    public bool VerifyAuthenticode(string filePath, out string publisherInfo, out string errorMessage)
    {
        publisherInfo = string.Empty;
        errorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            errorMessage = "Tệp cài đặt không tồn tại.";
            return false;
        }

        try
        {
            // Kiểm tra chữ ký Authenticode nhúng trong PE binary
            using var rawCert = X509Certificate.CreateFromSignedFile(filePath);
            if (rawCert == null)
            {
                errorMessage = "Tệp cài đặt không chứa chữ ký số Authenticode.";
                return false;
            }

            using var cert = new X509Certificate2(rawCert);
            publisherInfo = cert.Subject;

            // Kiểm tra nhà phát hành tin cậy nếu có cấu hình
            if (!string.IsNullOrWhiteSpace(_options.TrustedPublisherSubject))
            {
                if (!cert.Subject.Contains(_options.TrustedPublisherSubject, StringComparison.OrdinalIgnoreCase))
                {
                    errorMessage = $"Nhà phát hành trong chứng thư ({cert.Subject}) không khớp với cấu hình tin cậy ({_options.TrustedPublisherSubject}).";
                    return false;
                }
            }

            if (!string.IsNullOrWhiteSpace(_options.TrustedPublisherThumbprint))
            {
                if (!string.Equals(cert.Thumbprint, _options.TrustedPublisherThumbprint, StringComparison.OrdinalIgnoreCase))
                {
                    errorMessage = $"Mã băm chứng thư ({cert.Thumbprint}) không khớp với Thumbprint tin cậy.";
                    return false;
                }
            }

            return true;
        }
        catch (CryptographicException)
        {
            // Tệp chưa được ký Authenticode hoặc chứng thư không đọc được
            errorMessage = "Tệp cài đặt chưa được ký bằng Chứng thư số Authenticode (Chế độ phát triển hoặc chưa cài đặt mã ký Code Signing).";
            return false;
        }
        catch (Exception ex)
        {
            errorMessage = $"Lỗi kiểm tra Authenticode: {ex.Message}";
            return false;
        }
    }

    private static async Task<string> ComputeFileSha256Async(string filePath, CancellationToken cancellationToken)
    {
        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, true);
        using var sha256 = SHA256.Create();
        byte[] hashBytes = await sha256.ComputeHashAsync(stream, cancellationToken);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    private static void SafeDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // Tránh văng exception khi xóa file tạm lỗi
        }
    }
}
