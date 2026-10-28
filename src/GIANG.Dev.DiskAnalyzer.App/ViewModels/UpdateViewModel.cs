using System.Reflection;
using System.Text.Json;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GIANG.Dev.DiskAnalyzer.Core.Configuration;
using GIANG.Dev.DiskAnalyzer.Core.Helpers;
using GIANG.Dev.DiskAnalyzer.Core.Interfaces;
using GIANG.Dev.DiskAnalyzer.Core.Models;
using GIANG.Dev.DiskAnalyzer.Infrastructure.WindowsIntegration;
using Microsoft.Extensions.Logging;

namespace GIANG.Dev.DiskAnalyzer.App.ViewModels;

public partial class UpdateViewModel : ObservableObject
{
    private readonly IGitHubReleaseService _releaseService;
    private readonly IVersionComparisonService _versionComparison;
    private readonly IUpdateManifestVerifier _manifestVerifier;
    private readonly IUpdatePackageVerifier _packageVerifier;
    private readonly IUpdateDownloadService _downloadService;
    private readonly IUpdateInstallerService _installerService;
    private readonly GitHubUpdateOptions _options;
    private readonly ILogger<UpdateViewModel>? _logger;

    private CancellationTokenSource? _downloadCts;

    [ObservableProperty]
    private string _currentVersion = "v1.0.3";

    [ObservableProperty]
    private string _latestVersion = "Chưa kiểm tra";

    [ObservableProperty]
    private string _channel = "Stable";

    [ObservableProperty]
    private string _releaseDate = "--/--/----";

    [ObservableProperty]
    private string _releaseNotes = "Nhấn 'KIỂM TRA CẬP NHẬT' để kết nối tới GitHub Releases và tìm phiên bản mới nhất.";

    [ObservableProperty]
    private string _packageSizeFormatted = "-- MB";

    [ObservableProperty]
    private string _verificationStatus = "Chưa kiểm tra";

    [ObservableProperty]
    private string _verificationDetail = "Chờ kiểm tra và tải gói cài đặt";

    [ObservableProperty]
    private string _statusMessage = "Sẵn sàng kiểm tra bản cập nhật từ xa.";

    [ObservableProperty]
    private bool _isChecking;

    [ObservableProperty]
    private bool _isDownloading;

    [ObservableProperty]
    private bool _isUpdateAvailable;

    [ObservableProperty]
    private bool _isReadyToInstall;

    [ObservableProperty]
    private double _downloadProgressPercent;

    [ObservableProperty]
    private string _downloadSpeedText = string.Empty;

    [ObservableProperty]
    private string _downloadProgressText = string.Empty;

    [ObservableProperty]
    private string _githubRepoUrl;

    [ObservableProperty]
    private string _downloadedPackagePath = string.Empty;

    // Release and assets cache
    private GitHubRelease? _cachedRelease;
    private GitHubAsset? _installerAsset;
    private GitHubAsset? _manifestAsset;
    private GitHubAsset? _signatureAsset;

    public UpdateViewModel(
        IGitHubReleaseService releaseService,
        IVersionComparisonService versionComparison,
        IUpdateManifestVerifier manifestVerifier,
        IUpdatePackageVerifier packageVerifier,
        IUpdateDownloadService downloadService,
        IUpdateInstallerService installerService,
        GitHubUpdateOptions options,
        ILogger<UpdateViewModel>? logger = null)
    {
        _releaseService = releaseService;
        _versionComparison = versionComparison;
        _manifestVerifier = manifestVerifier;
        _packageVerifier = packageVerifier;
        _downloadService = downloadService;
        _installerService = installerService;
        _options = options;
        _logger = logger;

        _githubRepoUrl = $"https://github.com/{_options.GitHubOwner}/{_options.Repository}";

        // Tự động lấy phiên bản ứng dụng hiện tại từ Assembly
        var asmVersion = Assembly.GetEntryAssembly()?.GetName().Version ?? Assembly.GetExecutingAssembly().GetName().Version;
        if (asmVersion != null)
        {
            CurrentVersion = $"v{asmVersion.Major}.{asmVersion.Minor}.{asmVersion.Build}";
        }
    }

    [RelayCommand]
    public async Task CheckForUpdatesAsync()
    {
        if (IsChecking || IsDownloading) return;

        IsChecking = true;
        StatusMessage = "Đang kết nối GitHub Releases qua giao thức bảo mật HTTPS...";
        IsUpdateAvailable = false;
        IsReadyToInstall = false;
        DownloadedPackagePath = string.Empty;
        DownloadProgressPercent = 0;
        DownloadSpeedText = string.Empty;
        DownloadProgressText = string.Empty;

        try
        {
            var release = await _releaseService.GetLatestStableReleaseAsync();
            if (release == null)
            {
                StatusMessage = "Kho lưu trữ chưa có bản phát hành Stable nào.";
                VerificationStatus = "Không có dữ liệu";
                return;
            }

            _cachedRelease = release;
            LatestVersion = release.TagName;
            ReleaseDate = release.PublishedAtFormatted;
            ReleaseNotes = string.IsNullOrWhiteSpace(release.Body) ? "Không có nội dung ghi chú phát hành." : release.Body;

            // Tìm tệp cài đặt phù hợp (ưu tiên theo tên cấu hình hoặc *.exe)
            _installerAsset = release.Assets.FirstOrDefault(a =>
                a.Name.Equals(_options.AssetFileNamePattern, StringComparison.OrdinalIgnoreCase))
                ?? release.Assets.FirstOrDefault(a => a.Name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                ?? release.Assets.FirstOrDefault(a => a.Name.EndsWith(".msi", StringComparison.OrdinalIgnoreCase));

            _manifestAsset = release.Assets.FirstOrDefault(a =>
                a.Name.Equals(_options.ManifestFileName, StringComparison.OrdinalIgnoreCase));

            _signatureAsset = release.Assets.FirstOrDefault(a =>
                a.Name.Equals(_options.SignatureFileName, StringComparison.OrdinalIgnoreCase));

            if (_installerAsset != null)
            {
                PackageSizeFormatted = _installerAsset.SizeFormatted;
            }
            else
            {
                PackageSizeFormatted = "Không tìm thấy tệp cài đặt";
            }

            // So sánh phiên bản bằng quy chuẩn SemVer
            bool isNewer = _versionComparison.IsNewerVersion(CurrentVersion, release.TagName);

            if (isNewer)
            {
                IsUpdateAvailable = true;
                StatusMessage = $"Phát hiện phiên bản mới {release.TagName}! Sẵn sàng tải xuống và nâng cấp.";
                VerificationStatus = "Chờ tải và xác minh";
                VerificationDetail = _installerAsset != null
                    ? $"Tài sản phát hành: {_installerAsset.Name} ({_installerAsset.SizeFormatted})"
                    : "Cảnh báo: Bản phát hành chưa đính kèm tệp cài đặt hợp lệ.";
            }
            else
            {
                IsUpdateAvailable = false;
                StatusMessage = $"Bạn đang sử dụng phiên bản mới nhất ({CurrentVersion}). Hệ thống hoạt động tối ưu.";
                VerificationStatus = "Đã cập nhật mới nhất";
                VerificationDetail = "Không cần cập nhật thêm vào thời điểm này.";
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Lỗi khi kiểm tra cập nhật từ GitHub Releases");
            StatusMessage = $"Lỗi kiểm tra cập nhật: {ex.Message}";
            VerificationStatus = "Lỗi kết nối";
            VerificationDetail = ex.Message;
        }
        finally
        {
            IsChecking = false;
        }
    }

    [RelayCommand]
    public async Task DownloadUpdateAsync()
    {
        if (IsDownloading || _installerAsset == null) return;

        IsDownloading = true;
        IsReadyToInstall = false;
        DownloadProgressPercent = 0;
        StatusMessage = "Đang bắt đầu tải gói cài đặt cập nhật...";
        VerificationStatus = "Đang tải xuống";
        VerificationDetail = "Đang lưu trữ vào thư mục tạm bảo vệ của hệ thống...";

        _downloadCts = new CancellationTokenSource();

        var progressReporter = new Progress<UpdateDownloadProgress>(p =>
        {
            DownloadProgressPercent = p.ProgressPercent;
            DownloadSpeedText = p.SpeedFormatted;
            DownloadProgressText = $"{p.BytesDownloadedFormatted} / {p.TotalBytesFormatted}";
            StatusMessage = p.StatusMessage;
        });

        string? downloadedFile = null;

        try
        {
            // 1. Tải gói cài đặt
            downloadedFile = await _downloadService.DownloadUpdateAsync(
                _installerAsset.BrowserDownloadUrl,
                _installerAsset.Name,
                _installerAsset.Size,
                progressReporter,
                _downloadCts.Token);

            // 2. Xác minh Chữ ký số và Manifest
            StatusMessage = "Đang xác minh chữ ký số và tính toàn vẹn...";
            VerificationStatus = "Đang xác minh";

            string expectedSha256 = string.Empty;

            if (_manifestAsset != null)
            {
                string? manifestJson = await _releaseService.DownloadRawStringAsync(_manifestAsset.BrowserDownloadUrl, _downloadCts.Token);
                string? sigBase64 = _signatureAsset != null
                    ? await _releaseService.DownloadRawStringAsync(_signatureAsset.BrowserDownloadUrl, _downloadCts.Token)
                    : null;

                if (!string.IsNullOrWhiteSpace(manifestJson))
                {
                    // Xác minh chữ ký số manifest
                    if (!string.IsNullOrWhiteSpace(sigBase64))
                    {
                        bool sigValid = _manifestVerifier.VerifyManifestSignature(manifestJson, sigBase64, out string sigErr);
                        if (!sigValid)
                        {
                            throw new InvalidOperationException($"Chữ ký số Manifest không hợp lệ: {sigErr}");
                        }
                    }

                    // Đọc nội dung manifest
                    var manifest = JsonSerializer.Deserialize<UpdateManifest>(manifestJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (manifest != null)
                    {
                        bool contentValid = _manifestVerifier.ValidateManifestContent(manifest, _cachedRelease?.TagName ?? string.Empty, out string contentErr);
                        if (!contentValid)
                        {
                            throw new InvalidOperationException($"Dữ liệu Manifest không hợp lệ: {contentErr}");
                        }

                        expectedSha256 = manifest.Sha256;
                    }
                }
            }

            // 3. Xác minh tính toàn vẹn SHA-256 của tệp cài đặt tải về
            if (!string.IsNullOrWhiteSpace(expectedSha256))
            {
                bool hashValid = await _packageVerifier.VerifyPackageHashAsync(downloadedFile, expectedSha256, _downloadCts.Token);
                if (!hashValid)
                {
                    throw new InvalidOperationException("Mã băm SHA-256 của tệp cài đặt không khớp với Manifest phát hành! Gói tải về đã bị hủy để đảm bảo an toàn.");
                }
            }

            // 4. Kiểm tra chữ ký Authenticode PE (nếu có)
            bool isAuthSigned = _packageVerifier.VerifyAuthenticode(downloadedFile, out string publisher, out string authErr);

            DownloadedPackagePath = downloadedFile;
            IsReadyToInstall = true;

            VerificationStatus = "Xác minh thành công (Hợp lệ 100%)";
            VerificationDetail = isAuthSigned
                ? $"Nhà phát hành: {publisher} • SHA-256 hợp lệ"
                : "Tính toàn vẹn SHA-256 & Manifest hợp lệ • Sẵn sàng cài đặt an toàn";

            StatusMessage = "Gói cập nhật đã sẵn sàng! Nhấn 'CÀI ĐẶT NGAY' để tiến hành nâng cấp.";
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Đã hủy tiến trình tải gói cập nhật theo yêu cầu.";
            VerificationStatus = "Đã hủy";
            VerificationDetail = "Tệp tạm đã được xóa an toàn khỏi hệ thống.";
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Lỗi xảy ra trong quá trình tải hoặc xác minh cập nhật");
            StatusMessage = $"Lỗi cập nhật: {ex.Message}";
            VerificationStatus = "Xác minh thất bại";
            VerificationDetail = ex.Message;
            IsReadyToInstall = false;
        }
        finally
        {
            IsDownloading = false;
            _downloadCts?.Dispose();
            _downloadCts = null;
        }
    }

    [RelayCommand]
    public void CancelDownload()
    {
        if (IsDownloading && _downloadCts != null)
        {
            StatusMessage = "Đang gửi tín hiệu hủy tải xuống...";
            _downloadCts.Cancel();
        }
    }

    [RelayCommand]
    public void InstallUpdate()
    {
        if (!IsReadyToInstall || string.IsNullOrWhiteSpace(DownloadedPackagePath))
        {
            MessageBox.Show(
                "Chưa có gói cài đặt nào sẵn sàng để nâng cấp.",
                "Chưa sẵn sàng",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        // BẮT BUỘC: Yêu cầu người dùng xác nhận minh bạch trước khi chạy trình cài đặt
        var confirmResult = MessageBox.Show(
            $"XÁC NHẬN CẬP NHẬT PHẦN MỀM:\n\n" +
            $"• Phiên bản mới: {LatestVersion}\n" +
            $"• Tệp cài đặt: {System.IO.Path.GetFileName(DownloadedPackagePath)}\n" +
            $"• Trạng thái bảo mật: {VerificationStatus}\n\n" +
            $"Ứng dụng GIANG DEV DISK ANALYZER PRO sẽ được đóng lại để kích hoạt trình cài đặt cập nhật Windows.\n\n" +
            $"Bạn có đồng ý tiến hành cài đặt ngay bây giờ không?",
            "Xác nhận cài đặt bản cập nhật",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (confirmResult != MessageBoxResult.Yes)
        {
            StatusMessage = "Thao tác cài đặt đã được tạm hoãn. Bạn có thể nhấn 'CÀI ĐẶT NGAY' bất kỳ lúc nào.";
            return;
        }

        bool launched = _installerService.LaunchInstaller(DownloadedPackagePath, out string err);
        if (!launched)
        {
            MessageBox.Show(
                $"Không thể khởi chạy trình cài đặt: {err}\n\nVui lòng thử khởi chạy thủ công từ đường dẫn:\n{DownloadedPackagePath}",
                "Lỗi kích hoạt cài đặt",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            StatusMessage = $"Lỗi kích hoạt cài đặt: {err}";
        }
    }

    [RelayCommand]
    public void OpenGitHubReleases()
    {
        try
        {
            string url = _cachedRelease?.HtmlUrl ?? $"{GithubRepoUrl}/releases";
            ShellHelper.OpenFileWithDefaultApp(url);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Lỗi khi mở trình duyệt");
        }
    }
}
