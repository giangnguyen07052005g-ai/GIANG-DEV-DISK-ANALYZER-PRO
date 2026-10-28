using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using GIANG.Dev.DiskAnalyzer.App.ViewModels;
using GIANG.Dev.DiskAnalyzer.Core.Configuration;
using GIANG.Dev.DiskAnalyzer.Core.Interfaces;
using GIANG.Dev.DiskAnalyzer.Core.Models;
using GIANG.Dev.DiskAnalyzer.Infrastructure.Updates;
using Xunit;

namespace GIANG.Dev.DiskAnalyzer.Tests;

public class GitHubUpdateTests
{
    private readonly VersionComparisonService _versionComparison = new();

    #region 1. Semantic Versioning & Downgrade Prevention Tests

    [Theory]
    [InlineData("1.0.3", "1.0.4", true)]
    [InlineData("1.0.3", "1.1.0", true)]
    [InlineData("1.0.3", "2.0.0", true)]
    [InlineData("v1.0.3", "v1.0.4", true)]
    [InlineData("1.0.3", "v1.0.4", true)]
    [InlineData("1.0.3", "1.0.3", false)]         // Không có bản mới
    [InlineData("1.0.4", "1.0.3", false)]         // Chống hạ cấp (Downgrade protection)
    [InlineData("2.0.0", "1.99.99", false)]       // Chống hạ cấp
    [InlineData("1.0.0-beta", "1.0.0", true)]     // Bản stable mới hơn bản beta
    [InlineData("1.0.0", "1.0.0-beta", false)]    // Không hạ cấp về beta
    public void VersionComparison_ComparesSemanticVersionsCorrectly(string current, string candidate, bool expectedNewer)
    {
        bool isNewer = _versionComparison.IsNewerVersion(current, candidate);
        Assert.Equal(expectedNewer, isNewer);
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("v.1..2")]
    public void VersionComparison_MalformedVersionTags_HandledGracefully(string? malformedVersion)
    {
        bool isValid = _versionComparison.IsValidVersion(malformedVersion!);
        Assert.False(isValid);

        bool isNewer = _versionComparison.IsNewerVersion("1.0.3", malformedVersion!);
        Assert.False(isNewer);
    }

    #endregion

    #region 2. Cryptographic Manifest Signature & Anti-Tampering Tests

    [Fact]
    public void UpdateManifestVerifier_ValidEcdsaSignature_VerifiesSuccessfully()
    {
        // 1. Tạo cặp khóa ECDSA P-256 hợp lệ cho chủ dự án
        using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        string publicKeyBase64 = Convert.ToBase64String(ecdsa.ExportSubjectPublicKeyInfo());

        var options = new GitHubUpdateOptions { PublicKeyBase64 = publicKeyBase64 };
        var verifier = new UpdateManifestVerifier(options, _versionComparison);

        // 2. Tạo nội dung manifest
        string manifestJson = JsonSerializer.Serialize(new UpdateManifest
        {
            Version = "1.0.4",
            AssetName = "GIANG-DEV-DISK-ANALYZER-PRO-Setup.exe",
            Architecture = "win-x64",
            Sha256 = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
            MinSupportedVersion = "1.0.0",
            ReleaseUrl = "https://github.com/YOUR_GITHUB_USERNAME/GIANG-DEV-DISK-ANALYZER-PRO/releases/tag/v1.0.4"
        });

        // 3. Ký số manifest bằng Khóa riêng (Private Key)
        byte[] signatureBytes = ecdsa.SignData(Encoding.UTF8.GetBytes(manifestJson.Trim()), HashAlgorithmName.SHA256);
        string signatureBase64 = Convert.ToBase64String(signatureBytes);

        // 4. Xác minh bằng Khóa công khai nhúng trong ứng dụng
        bool isValid = verifier.VerifyManifestSignature(manifestJson, signatureBase64, out string error);

        Assert.True(isValid);
        Assert.Empty(error);
    }

    [Fact]
    public void UpdateManifestVerifier_TamperedManifestContent_FailsSignatureVerification()
    {
        using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        string publicKeyBase64 = Convert.ToBase64String(ecdsa.ExportSubjectPublicKeyInfo());

        var options = new GitHubUpdateOptions { PublicKeyBase64 = publicKeyBase64 };
        var verifier = new UpdateManifestVerifier(options, _versionComparison);

        string originalManifest = "{\"version\":\"1.0.4\",\"sha256\":\"e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855\"}";
        byte[] signatureBytes = ecdsa.SignData(Encoding.UTF8.GetBytes(originalManifest.Trim()), HashAlgorithmName.SHA256);
        string signatureBase64 = Convert.ToBase64String(signatureBytes);

        // Giả lập kẻ tấn công sửa phiên bản hoặc hash trong manifest
        string tamperedManifest = "{\"version\":\"1.0.5\",\"sha256\":\"e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855\"}";

        bool isValid = verifier.VerifyManifestSignature(tamperedManifest, signatureBase64, out string error);

        Assert.False(isValid);
        Assert.Contains("KHÔNG HỢP LỆ", error);
    }

    [Fact]
    public void UpdateManifestVerifier_WrongPublicKey_FailsSignatureVerification()
    {
        using var signerEcdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        using var wrongEcdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);

        string wrongPublicKeyBase64 = Convert.ToBase64String(wrongEcdsa.ExportSubjectPublicKeyInfo());

        var options = new GitHubUpdateOptions { PublicKeyBase64 = wrongPublicKeyBase64 };
        var verifier = new UpdateManifestVerifier(options, _versionComparison);

        string manifest = "{\"version\":\"1.0.4\"}";
        byte[] signatureBytes = signerEcdsa.SignData(Encoding.UTF8.GetBytes(manifest.Trim()), HashAlgorithmName.SHA256);
        string signatureBase64 = Convert.ToBase64String(signatureBytes);

        bool isValid = verifier.VerifyManifestSignature(manifest, signatureBase64, out string error);

        Assert.False(isValid);
        Assert.Contains("KHÔNG HỢP LỆ", error);
    }

    [Fact]
    public void UpdateManifestVerifier_MissingOrMalformedSignature_Fails()
    {
        var options = new GitHubUpdateOptions();
        var verifier = new UpdateManifestVerifier(options, _versionComparison);

        bool emptySig = verifier.VerifyManifestSignature("{\"version\":\"1.0.4\"}", "", out string err1);
        Assert.False(emptySig);
        Assert.Contains("rỗng hoặc bị thiếu", err1);

        bool malformedSig = verifier.VerifyManifestSignature("{\"version\":\"1.0.4\"}", "not-valid-base64@@", out string err2);
        Assert.False(malformedSig);
        Assert.Contains("Base64", err2);
    }

    [Fact]
    public void UpdateManifestVerifier_ContentValidation_EnforcesVersionArchitectureAndUrl()
    {
        var options = new GitHubUpdateOptions();
        var verifier = new UpdateManifestVerifier(options, _versionComparison);

        // 1. Version tag mismatch
        var manifest1 = new UpdateManifest
        {
            Version = "1.0.4",
            Architecture = "win-x64",
            Sha256 = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855"
        };
        bool valid1 = verifier.ValidateManifestContent(manifest1, "v1.0.5", out string err1);
        Assert.False(valid1);
        Assert.Contains("không trùng khớp với Tag", err1);

        // 2. Architecture mismatch
        var manifest2 = new UpdateManifest
        {
            Version = "1.0.4",
            Architecture = "linux-arm64",
            Sha256 = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855"
        };
        bool valid2 = verifier.ValidateManifestContent(manifest2, "v1.0.4", out string err2);
        Assert.False(valid2);
        Assert.Contains("Kiến trúc", err2);

        // 3. Invalid SHA-256 length
        var manifest3 = new UpdateManifest
        {
            Version = "1.0.4",
            Architecture = "win-x64",
            Sha256 = "short-hash"
        };
        bool valid3 = verifier.ValidateManifestContent(manifest3, "v1.0.4", out string err3);
        Assert.False(valid3);
        Assert.Contains("Mã băm SHA-256", err3);

        // 4. Insecure non-HTTPS URL
        var manifest4 = new UpdateManifest
        {
            Version = "1.0.4",
            Architecture = "win-x64",
            Sha256 = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
            ReleaseUrl = "http://insecure-site.com/release"
        };
        bool valid4 = verifier.ValidateManifestContent(manifest4, "v1.0.4", out string err4);
        Assert.False(valid4);
        Assert.Contains("HTTPS", err4);
    }

    #endregion

    #region 3. SHA-256 Package Integrity & Auto-Deletion Tests

    [Fact]
    public async Task UpdatePackageVerifier_ExactSha256Match_Succeeds()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), $"pkg_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        string filePath = Path.Combine(tempDir, "setup.exe");

        byte[] content = Encoding.UTF8.GetBytes("GIANG DEV DISK ANALYZER PRO INSTALLER PAYLOAD");
        await File.WriteAllBytesAsync(filePath, content);

        byte[] hashBytes = SHA256.HashData(content);
        string expectedSha256 = Convert.ToHexString(hashBytes).ToLowerInvariant();

        var verifier = new UpdatePackageVerifier(new GitHubUpdateOptions());
        bool isValid = await verifier.VerifyPackageHashAsync(filePath, expectedSha256);

        Assert.True(isValid);
        Assert.True(File.Exists(filePath)); // File an toàn được giữ lại

        if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
    }

    [Fact]
    public async Task UpdatePackageVerifier_CorruptOrMismatchedHash_FailsAndAutoDeletesFile()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), $"pkg_corrupt_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        string filePath = Path.Combine(tempDir, "corrupt_setup.exe");

        byte[] content = Encoding.UTF8.GetBytes("TAMPERED OR CORRUPTED INSTALLER CONTENT");
        await File.WriteAllBytesAsync(filePath, content);

        string wrongSha256 = "0000000000000000000000000000000000000000000000000000000000000000";

        var verifier = new UpdatePackageVerifier(new GitHubUpdateOptions());
        bool isValid = await verifier.VerifyPackageHashAsync(filePath, wrongSha256);

        // BẮT BUỘC: Gói cập nhật lỗi hash phải bị từ chối VÀ tệp lỗi phải bị xóa ngay lập tức
        Assert.False(isValid);
        Assert.False(File.Exists(filePath));

        if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
    }

    #endregion

    #region 4. URL Host Whitelist & Windows Security Protection Tests

    [Fact]
    public async Task UpdateDownloadService_RejectsUntrustedHostUrls()
    {
        var options = new GitHubUpdateOptions();
        var downloadService = new UpdateDownloadService(new HttpClient(), options);

        // 1. URL nằm ngoài Whitelist
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await downloadService.DownloadUpdateAsync(
                "https://untrusted-malicious-domain.com/installer.exe",
                "setup.exe",
                1000);
        });

        // 2. URL không sử dụng HTTPS
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await downloadService.DownloadUpdateAsync(
                "http://github.com/user/repo/releases/download/v1.0.4/setup.exe",
                "setup.exe",
                1000);
        });
    }

    [Fact]
    public async Task UpdateDownloadService_Cancellation_DeletesPartialTempFile()
    {
        var options = new GitHubUpdateOptions();

        // Mock HttpMessageHandler gửi dữ liệu vô tận chậm rãi
        var mockHandler = new MockStreamingHttpMessageHandler();
        var client = new HttpClient(mockHandler);
        var downloadService = new UpdateDownloadService(client, options);

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(50); // Hủy sau 50ms

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await downloadService.DownloadUpdateAsync(
                "https://github.com/test/repo/releases/download/v1.0.4/setup.exe",
                "test_setup.exe",
                100000,
                null,
                cts.Token);
        });

        // Kiểm tra thư mục tạm không còn sót tệp tải dở
        string updateDir = Path.Combine(Path.GetTempPath(), "GiangDevDiskAnalyzer", "Updates");
        if (Directory.Exists(updateDir))
        {
            var files = Directory.GetFiles(updateDir, "*test_setup.exe");
            Assert.Empty(files);
        }
    }

    #endregion

    #region 5. GitHub Releases API Filtering & Rate Limit Handling Tests

    [Fact]
    public async Task GitHubReleaseService_FiltersDraftsAndPrereleases_ReturnsOnlyStable()
    {
        var mockReleases = new List<GitHubRelease>
        {
            new() { TagName = "v2.0.0-draft", Draft = true, Prerelease = false, PublishedAt = DateTime.UtcNow },
            new() { TagName = "v1.9.0-beta", Draft = false, Prerelease = true, PublishedAt = DateTime.UtcNow.AddDays(-1) },
            new() { TagName = "v1.0.4", Draft = false, Prerelease = false, PublishedAt = DateTime.UtcNow.AddDays(-2) },
            new() { TagName = "v1.0.3", Draft = false, Prerelease = false, PublishedAt = DateTime.UtcNow.AddDays(-10) }
        };

        var handler = new MockHttpMessageHandler(HttpStatusCode.OK, JsonSerializer.Serialize(mockReleases));
        var client = new HttpClient(handler);
        var options = new GitHubUpdateOptions();

        var releaseService = new GitHubReleaseService(client, options);
        var latest = await releaseService.GetLatestStableReleaseAsync();

        Assert.NotNull(latest);
        // Bắt buộc bỏ qua Draft (v2.0.0-draft) và Prerelease (v1.9.0-beta), chỉ nhận bản Stable mới nhất (v1.0.4)
        Assert.Equal("v1.0.4", latest.TagName);
    }

    [Fact]
    public async Task GitHubReleaseService_HandlesRateLimitAndHttpErrorsGracefully()
    {
        // 1. Rate Limit 403 Exceeded
        var rateLimitHandler = new MockHttpMessageHandler(HttpStatusCode.Forbidden, "rate limit exceeded", new Dictionary<string, string>
        {
            { "X-RateLimit-Remaining", "0" }
        });
        var client1 = new HttpClient(rateLimitHandler);
        var service1 = new GitHubReleaseService(client1, new GitHubUpdateOptions());

        var ex1 = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await service1.GetLatestStableReleaseAsync();
        });
        Assert.Contains("Rate Limit", ex1.Message);

        // 2. Repo Not Found 404
        var notFoundHandler = new MockHttpMessageHandler(HttpStatusCode.NotFound, "not found");
        var client2 = new HttpClient(notFoundHandler);
        var service2 = new GitHubReleaseService(client2, new GitHubUpdateOptions());

        var ex2 = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await service2.GetLatestStableReleaseAsync();
        });
        Assert.Contains("Không tìm thấy kho lưu trữ", ex2.Message);
    }

    #endregion

    #region 6. UpdateViewModel State Transitions & Flow Verification

    [Fact]
    public async Task UpdateViewModel_NoNewVersion_SetsStatusCorrectly()
    {
        var mockReleases = new List<GitHubRelease>
        {
            new() { TagName = "v1.0.3", Draft = false, Prerelease = false, PublishedAt = DateTime.UtcNow }
        };

        var handler = new MockHttpMessageHandler(HttpStatusCode.OK, JsonSerializer.Serialize(mockReleases));
        var options = new GitHubUpdateOptions();
        var releaseService = new GitHubReleaseService(new HttpClient(handler), options);

        var vm = new UpdateViewModel(
            releaseService,
            _versionComparison,
            new UpdateManifestVerifier(options, _versionComparison),
            new UpdatePackageVerifier(options),
            new UpdateDownloadService(new HttpClient(), options),
            new UpdateInstallerService(),
            options)
        {
            CurrentVersion = "v1.0.3"
        };

        await vm.CheckForUpdatesAsync();

        Assert.False(vm.IsUpdateAvailable);
        Assert.False(vm.IsReadyToInstall);
        Assert.Contains("phiên bản mới nhất", vm.StatusMessage);
        Assert.Equal("Đã cập nhật mới nhất", vm.VerificationStatus);
    }

    [Fact]
    public async Task UpdateViewModel_UpdateAvailable_SetsStatusAndEnablesDownload()
    {
        var mockReleases = new List<GitHubRelease>
        {
            new()
            {
                TagName = "v1.0.4",
                Draft = false,
                Prerelease = false,
                PublishedAt = DateTime.UtcNow,
                Body = "Fixes and improvements in v1.0.4",
                Assets = new List<GitHubAsset>
                {
                    new()
                    {
                        Name = "GIANG-DEV-DISK-ANALYZER-PRO-Setup.exe",
                        Size = 150 * 1024 * 1024,
                        BrowserDownloadUrl = "https://github.com/YOUR_GITHUB_USERNAME/GIANG-DEV-DISK-ANALYZER-PRO/releases/download/v1.0.4/setup.exe"
                    }
                }
            }
        };

        var handler = new MockHttpMessageHandler(HttpStatusCode.OK, JsonSerializer.Serialize(mockReleases));
        var options = new GitHubUpdateOptions();
        var releaseService = new GitHubReleaseService(new HttpClient(handler), options);

        var vm = new UpdateViewModel(
            releaseService,
            _versionComparison,
            new UpdateManifestVerifier(options, _versionComparison),
            new UpdatePackageVerifier(options),
            new UpdateDownloadService(new HttpClient(), options),
            new UpdateInstallerService(),
            options)
        {
            CurrentVersion = "v1.0.3"
        };

        await vm.CheckForUpdatesAsync();

        Assert.True(vm.IsUpdateAvailable);
        Assert.Equal("v1.0.4", vm.LatestVersion);
        Assert.Contains("150 MB", vm.PackageSizeFormatted);
        Assert.Contains("Phát hiện phiên bản mới", vm.StatusMessage);
        Assert.Contains("Fixes and improvements", vm.ReleaseNotes);
    }

    [Fact]
    public void UpdateInstallerService_RejectsInvalidExtensions()
    {
        var installer = new UpdateInstallerService();

        // 1. Từ chối file kịch bản nguy hiểm (.bat, .cmd, .ps1)
        string scriptPath = Path.Combine(Path.GetTempPath(), "malicious.bat");
        File.WriteAllText(scriptPath, "echo off");

        bool launched = installer.LaunchInstaller(scriptPath, out string err);
        Assert.False(launched);
        Assert.Contains("Định dạng tệp không được hỗ trợ", err);

        // 2. Từ chối file không tồn tại
        bool nonExistent = installer.LaunchInstaller(@"C:\Path\Does\Not\Exist\Setup.exe", out string err2);
        Assert.False(nonExistent);
        Assert.Contains("Không tìm thấy", err2);

        if (File.Exists(scriptPath)) File.Delete(scriptPath);
    }

    #endregion
}

#region Test HTTP Message Handlers for Mocking

internal class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly HttpStatusCode _statusCode;
    private readonly string _content;
    private readonly Dictionary<string, string>? _headers;

    public MockHttpMessageHandler(HttpStatusCode statusCode, string content, Dictionary<string, string>? headers = null)
    {
        _statusCode = statusCode;
        _content = content;
        _headers = headers;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = new HttpResponseMessage(_statusCode)
        {
            Content = new StringContent(_content, Encoding.UTF8, "application/json")
        };

        if (_headers != null)
        {
            foreach (var kvp in _headers)
            {
                response.Headers.TryAddWithoutValidation(kvp.Key, kvp.Value);
            }
        }

        return Task.FromResult(response);
    }
}

internal class MockStreamingHttpMessageHandler : HttpMessageHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var stream = new SlowInfiniteMemoryStream();
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StreamContent(stream)
        };
        response.Content.Headers.ContentLength = 100000;
        return response;
    }

    private class SlowInfiniteMemoryStream : Stream
    {
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => 100000;
        public override long Position { get; set; }

        public override void Flush() { }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            await Task.Delay(20, cancellationToken);
            int bytes = Math.Min(count, 1024);
            Array.Fill(buffer, (byte)0x41, offset, bytes);
            return bytes;
        }

        public override int Read(byte[] buffer, int offset, int count) => ReadAsync(buffer, offset, count).GetAwaiter().GetResult();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}

#endregion
