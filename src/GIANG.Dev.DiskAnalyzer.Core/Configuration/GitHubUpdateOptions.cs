namespace GIANG.Dev.DiskAnalyzer.Core.Configuration;

/// <summary>
/// Cấu hình tập trung cho tính năng Cập nhật tự động qua GitHub Releases (GitHub Auto Update).
/// ĐÁNH DẤU RÕ MỌI GIÁ TRỊ CẦN CHỦ DỰ ÁN CẤU HÌNH KHI TRIỂN KHAI THỰC TẾ.
/// </summary>
public class GitHubUpdateOptions
{
    /// <summary>
    /// [CẦN CẤU HÌNH] Tên tài khoản hoặc tổ chức sở hữu GitHub Repository.
    /// Mặc định: "giangnguyen07052005g-ai".
    /// </summary>
    public string GitHubOwner { get; set; } = "giangnguyen07052005g-ai";

    /// <summary>
    /// [CẦN CẤU HÌNH] Tên GitHub Repository lưu trữ các bản phát hành.
    /// Mặc định: "GIANG-DEV-DISK-ANALYZER-PRO".
    /// </summary>
    public string Repository { get; set; } = "GIANG-DEV-DISK-ANALYZER-PRO";

    /// <summary>
    /// Kênh cập nhật (mặc định: "Stable"). Bỏ qua các bản Draft và Pre-release.
    /// </summary>
    public string Channel { get; set; } = "Stable";

    /// <summary>
    /// Mẫu tên tệp cài đặt phát hành chính thức (Asset File Name).
    /// </summary>
    public string AssetFileNamePattern { get; set; } = "GIANG-DEV-DISK-ANALYZER-PRO-Setup.exe";

    /// <summary>
    /// Tên tệp manifest chứa siêu dữ liệu xác thực bản cập nhật (SHA-256, kiến trúc, min version).
    /// </summary>
    public string ManifestFileName { get; set; } = "update-manifest.json";

    /// <summary>
    /// Tên tệp chữ ký số đi kèm manifest (Base64 signature file).
    /// </summary>
    public string SignatureFileName { get; set; } = "update-manifest.json.sig";

    /// <summary>
    /// [CẦN CẤU HÌNH] Khóa công khai ECDSA (NIST P-256 / secp256r1) dạng Base64 SubjectPublicKeyInfo (SPKI).
    /// CHÚ Ý: CHỈ NHÚNG KHÓA CÔNG KHAI (PUBLIC KEY). TUYỆT ĐỐI KHÔNG NHÚNG KHÓA RIÊNG (PRIVATE KEY).
    /// Dưới đây là khóa công khai mặc định dùng cho môi trường xác thực mẫu và kiểm thử tự động.
    /// Khi phát hành thương mại, chủ dự án cần thay bằng Khóa công khai chính thức của mình.
    /// </summary>
    public string PublicKeyBase64 { get; set; } = 
        "MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAElFlp4OwA+Y7je+epFZwJ6xezzQ06Ig+7Nbh+/2aQhN2A2kkOZrOhNDRGj9U9kjUanYUSLpbYY1O9J8khCIuM3g==";

    /// <summary>
    /// [TÙY CHỌN] Tên Subject của chứng thư Authenticode thương mại nếu tệp cài đặt được ký số (Code Signing Certificate).
    /// Ví dụ: "CN=GIANG DEV, O=GIANG DEV Software, C=VN".
    /// </summary>
    public string? TrustedPublisherSubject { get; set; } = "GIANG DEV";

    /// <summary>
    /// [TÙY CHỌN] Mã băm SHA-1 hoặc SHA-256 (Thumbprint) của chứng thư ký số đáng tin cậy.
    /// </summary>
    public string? TrustedPublisherThumbprint { get; set; } = null;

    /// <summary>
    /// Danh sách máy chủ được phép tải tệp xuống (URL Host Whitelist) nhằm bảo vệ Windows chống URL độc hại.
    /// </summary>
    public string[] AllowedDownloadHosts { get; set; } = new[]
    {
        "github.com",
        "api.github.com",
        "objects.githubusercontent.com",
        "github-releases.githubusercontent.com"
    };

    /// <summary>
    /// Giới hạn dung lượng tải xuống tối đa (byte). Mặc định 500 MB để ngăn ngừa stream bất thường.
    /// </summary>
    public long MaxDownloadSizeBytes { get; set; } = 500L * 1024 * 1024;

    /// <summary>
    /// Thời gian chờ kết nối tối đa (giây). Mặc định 30 giây.
    /// </summary>
    public int RequestTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Thời gian tối thiểu giữa các lần kiểm tra tự động (phút) để tránh vượt quá Rate Limit của GitHub API.
    /// </summary>
    public int CheckCooldownMinutes { get; set; } = 5;

    /// <summary>
    /// Lấy URL gọi GitHub Releases API.
    /// </summary>
    public string ReleasesApiUrl => $"https://api.github.com/repos/{GitHubOwner}/{Repository}/releases";

    /// <summary>
    /// Lấy URL gọi GitHub Latest Release API.
    /// </summary>
    public string LatestReleaseApiUrl => $"https://api.github.com/repos/{GitHubOwner}/{Repository}/releases/latest";
}
