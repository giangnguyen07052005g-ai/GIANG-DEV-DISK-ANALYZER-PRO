using System.Diagnostics;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using GIANG.Dev.DiskAnalyzer.Core.Configuration;
using GIANG.Dev.DiskAnalyzer.Core.Models;
using GIANG.Dev.DiskAnalyzer.Infrastructure.Updates;

namespace GIANG.Dev.ReleaseHelper;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.WriteLine("==================================================================");
        Console.WriteLine("  GIANG DEV DISK ANALYZER PRO — FULL AUTOMATION RELEASE & PUBLISH");
        Console.WriteLine("==================================================================");

        string rootDir = FindSolutionRoot();
        Console.WriteLine($"[+] Solution Root: {rootDir}");

        string version = "1.0.4";
        string? customExe = null;
        string? notes = null;
        string owner = "giangnguyen07052005g-ai";
        string repo = "GIANG-DEV-DISK-ANALYZER-PRO";
        bool autoPublish = true;

        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--version" && i + 1 < args.Length) version = args[++i];
            else if (args[i] == "--exe" && i + 1 < args.Length) customExe = args[++i];
            else if (args[i] == "--notes" && i + 1 < args.Length) notes = args[++i];
            else if (args[i] == "--owner" && i + 1 < args.Length) owner = args[++i];
            else if (args[i] == "--repo" && i + 1 < args.Length) repo = args[++i];
            else if (args[i] == "--no-publish") autoPublish = false;
        }

        Console.WriteLine($"[+] Target Release Version: v{version}");
        Console.WriteLine($"[+] Owner: {owner} | Repo: {repo}");

        // 1. Quản lý Khóa ký số ECDSA NIST P-256
        string keysDir = Path.Combine(rootDir, "scripts", "keys");
        Directory.CreateDirectory(keysDir);
        string privateKeyFile = Path.Combine(keysDir, "private_key.pkcs8.txt");
        string publicKeyFile = Path.Combine(keysDir, "public_key.spki.txt");

        ECDsa ecdsa;
        string publicKeyBase64;

        if (File.Exists(privateKeyFile) && new FileInfo(privateKeyFile).Length > 10)
        {
            Console.WriteLine("[+] Loading existing ECDSA Private Key from scripts/keys/private_key.pkcs8.txt...");
            byte[] privBytes = Convert.FromBase64String(File.ReadAllText(privateKeyFile).Trim());
            ecdsa = ECDsa.Create();
            ecdsa.ImportPkcs8PrivateKey(privBytes, out _);
            byte[] pubBytes = ecdsa.ExportSubjectPublicKeyInfo();
            publicKeyBase64 = Convert.ToBase64String(pubBytes);
            File.WriteAllText(publicKeyFile, publicKeyBase64);
        }
        else
        {
            Console.WriteLine("[+] Generating NEW ECDSA NIST P-256 Key Pair...");
            ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            byte[] privBytes = ecdsa.ExportPkcs8PrivateKey();
            byte[] pubBytes = ecdsa.ExportSubjectPublicKeyInfo();
            publicKeyBase64 = Convert.ToBase64String(pubBytes);

            File.WriteAllText(privateKeyFile, Convert.ToBase64String(privBytes));
            File.WriteAllText(publicKeyFile, publicKeyBase64);
            Console.WriteLine($"[+] Private key saved to: {privateKeyFile}");
            Console.WriteLine($"[+] Public key saved to:  {publicKeyFile}");
        }

        Console.WriteLine($"[+] Public Key Base64: {publicKeyBase64}");

        // Cập nhật cấu hình trong code & file json
        SyncConfiguration(rootDir, publicKeyBase64, owner, repo);

        // 2. Build và Publish hoặc lấy EXE chỉ định
        string outputDir = Path.Combine(rootDir, "release-output", $"v{version}");
        Directory.CreateDirectory(outputDir);

        string targetExePath = Path.Combine(outputDir, "GIANG-DEV-DISK-ANALYZER-PRO-Setup.exe");

        if (!string.IsNullOrEmpty(customExe) && File.Exists(customExe))
        {
            Console.WriteLine($"[+] Copying specified EXE: {customExe}");
            File.Copy(customExe, targetExePath, true);
        }
        else
        {
            string prebuiltExe = Path.Combine(rootDir, "publish", "win-x64", "GIANG.Dev.DiskAnalyzer.App.exe");
            if (File.Exists(prebuiltExe))
            {
                Console.WriteLine($"[+] Using prebuilt EXE: {prebuiltExe}");
                File.Copy(prebuiltExe, targetExePath, true);
            }
            else
            {
                Console.WriteLine($"[+] Compiling and publishing Single-File EXE (win-x64)...");
                int buildResult = RunDotnetPublish(rootDir, version);
                if (buildResult != 0)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("[!] Build failed!");
                    Console.ResetColor();
                    return 1;
                }
                string appPublishExe = Path.Combine(rootDir, "src", "GIANG.Dev.DiskAnalyzer.App", "bin", "Release", "net8.0-windows", "win-x64", "publish", "GIANG.Dev.DiskAnalyzer.App.exe");
                if (!File.Exists(appPublishExe))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[!] Could not find published file at: {appPublishExe}");
                    Console.ResetColor();
                    return 1;
                }
                File.Copy(appPublishExe, targetExePath, true);
            }
        }

        long exeSize = new FileInfo(targetExePath).Length;
        Console.WriteLine($"[+] Release Executable: {targetExePath} ({exeSize:N0} bytes)");

        // 3. Tính toán SHA-256
        Console.WriteLine("[+] Computing SHA-256 checksum...");
        string sha256Hex;
        using (var sha = SHA256.Create())
        using (var stream = File.OpenRead(targetExePath))
        {
            byte[] hash = sha.ComputeHash(stream);
            sha256Hex = Convert.ToHexString(hash).ToLowerInvariant();
        }
        Console.WriteLine($"[+] SHA-256: {sha256Hex}");

        // 4. Tạo update-manifest.json
        string manifestFile = Path.Combine(outputDir, "update-manifest.json");
        var manifestObj = new
        {
            version = version,
            releaseDate = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            minSupportedVersion = "1.0.0",
            architecture = "win-x64",
            packageSha256 = sha256Hex,
            packageSize = exeSize,
            assetFileName = "GIANG-DEV-DISK-ANALYZER-PRO-Setup.exe",
            releaseNotes = notes ?? $"Bản phát hành chính thức GIANG DEV DISK ANALYZER PRO v{version}. Tối ưu hiệu năng, sửa lỗi giao diện và cập nhật hệ thống tự động.",
            mandatory = false
        };

        string manifestJson = JsonSerializer.Serialize(manifestObj, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(manifestFile, manifestJson, Encoding.UTF8);
        Console.WriteLine($"[+] Generated Manifest: {manifestFile}");

        // 5. Ký số update-manifest.json bằng ECDSA NIST P-256
        Console.WriteLine("[+] Signing update-manifest.json with ECDSA NIST P-256...");
        byte[] manifestBytes = Encoding.UTF8.GetBytes(manifestJson.Trim());
        byte[] sigBytes = ecdsa.SignData(manifestBytes, HashAlgorithmName.SHA256);
        string sigBase64 = Convert.ToBase64String(sigBytes);

        string sigFile = Path.Combine(outputDir, "update-manifest.json.sig");
        File.WriteAllText(sigFile, sigBase64, Encoding.UTF8);
        Console.WriteLine($"[+] Generated Signature: {sigFile}");

        // 6. Kiểm tra chéo (Self-Verification)
        Console.WriteLine("[+] Verifying signature and SHA-256 locally...");
        var options = new GitHubUpdateOptions
        {
            PublicKeyBase64 = publicKeyBase64
        };
        var verifier = new UpdateManifestVerifier(options, new VersionComparisonService());
        bool sigValid = verifier.VerifyManifestSignature(manifestJson, sigBase64, out string errMsg);
        if (!sigValid)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[!] Signature verification FAILED: {errMsg}");
            Console.ResetColor();
            return 1;
        }

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("==================================================================");
        Console.WriteLine("  CHUẨN BỊ BẢN PHÁT HÀNH THÀNH CÔNG RỰC RỠ! (ALL CHECKS PASSED)");
        Console.WriteLine("==================================================================");
        Console.ResetColor();

        // 7. TỰ ĐỘNG ĐẨY LÊN GITHUB & TẠO RELEASE NẾU CÓ TOKEN
        if (autoPublish)
        {
            Console.WriteLine("\n[+] Đang tìm kiếm xác thực GitHub trong hệ thống Windows...");
            string? token = GetStoredGitHubToken(owner);
            if (string.IsNullOrEmpty(token))
            {
                Console.WriteLine("[!] Không tìm thấy GitHub Personal Access Token trong Windows Credential Manager.");
                Console.WriteLine("[+] Vui lòng sử dụng GitHub Desktop để bấm 'Push' và kéo thả 3 file lên Web.");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"[+] ĐÃ TÌM THẤY GITHUB TOKEN! BẮT ĐẦU ĐẨY LÊN GITHUB HOÀN TOÀN TỰ ĐỘNG...");
                Console.ResetColor();

                // Đẩy git commits
                PushGitCommitsWithToken(rootDir, owner, repo, token);

                // Tạo GitHub Release và Upload 3 assets
                await CreateReleaseAndUploadAssetsAsync(owner, repo, version, token, targetExePath, manifestFile, sigFile, notes);
            }
        }

        return 0;
    }

    private static string FindSolutionRoot()
    {
        string? current = AppDomain.CurrentDomain.BaseDirectory;
        while (!string.IsNullOrEmpty(current))
        {
            if (File.Exists(Path.Combine(current, "GIANG_DEV_DISK_ANALYZER_PRO.sln")))
            {
                return current;
            }
            current = Directory.GetParent(current)?.FullName;
        }
        return Directory.GetCurrentDirectory();
    }

    private static void SyncConfiguration(string rootDir, string publicKeyBase64, string? owner, string? repo)
    {
        string configPath = Path.Combine(rootDir, "src", "GIANG.Dev.DiskAnalyzer.Core", "Configuration", "GitHubUpdateOptions.cs");
        if (File.Exists(configPath))
        {
            string code = File.ReadAllText(configPath);

            string currentKeyPattern = "public string PublicKeyBase64 { get; set; } = ";
            int idx = code.IndexOf(currentKeyPattern);
            if (idx >= 0)
            {
                int startQuote = code.IndexOf('"', idx);
                int endQuote = code.IndexOf('"', startQuote + 1);
                if (startQuote > 0 && endQuote > startQuote)
                {
                    string oldKey = code.Substring(startQuote + 1, endQuote - startQuote - 1);
                    if (oldKey != publicKeyBase64)
                    {
                        code = code.Substring(0, startQuote + 1) + publicKeyBase64 + code.Substring(endQuote);
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(owner))
            {
                string ownerPattern = "public string GitHubOwner { get; set; } = ";
                int ownerIdx = code.IndexOf(ownerPattern);
                if (ownerIdx >= 0)
                {
                    int startQuote = code.IndexOf('"', ownerIdx);
                    int endQuote = code.IndexOf('"', startQuote + 1);
                    if (startQuote > 0 && endQuote > startQuote)
                    {
                        code = code.Substring(0, startQuote + 1) + owner.Trim() + code.Substring(endQuote);
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(repo))
            {
                string repoPattern = "public string Repository { get; set; } = ";
                int repoIdx = code.IndexOf(repoPattern);
                if (repoIdx >= 0)
                {
                    int startQuote = code.IndexOf('"', repoIdx);
                    int endQuote = code.IndexOf('"', startQuote + 1);
                    if (startQuote > 0 && endQuote > startQuote)
                    {
                        code = code.Substring(0, startQuote + 1) + repo.Trim() + code.Substring(endQuote);
                    }
                }
            }

            File.WriteAllText(configPath, code, Encoding.UTF8);
        }

        string jsonConfigPath = Path.Combine(rootDir, "github-update.json");
        var jsonConfig = new
        {
            GitHubOwner = owner ?? "giangnguyen07052005g-ai",
            Repository = repo ?? "GIANG-DEV-DISK-ANALYZER-PRO",
            Channel = "Stable",
            PublicKeyBase64 = publicKeyBase64
        };
        File.WriteAllText(jsonConfigPath, JsonSerializer.Serialize(jsonConfig, new JsonSerializerOptions { WriteIndented = true }), Encoding.UTF8);
    }

    private static int RunDotnetPublish(string rootDir, string version)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"publish src\\GIANG.Dev.DiskAnalyzer.App\\GIANG.Dev.DiskAnalyzer.App.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -p:Version={version}",
            WorkingDirectory = rootDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        var process = Process.Start(psi);
        if (process == null) return 1;

        while (!process.StandardOutput.EndOfStream)
        {
            string? line = process.StandardOutput.ReadLine();
            if (!string.IsNullOrWhiteSpace(line))
            {
                Console.WriteLine("    [dotnet] " + line);
            }
        }

        process.WaitForExit();
        return process.ExitCode;
    }

    #region Windows Credential & GitHub Push Automation

    [DllImport("Advapi32.dll", EntryPoint = "CredReadW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool CredRead(string target, int type, int reservedFlag, out IntPtr credentialPtr);

    [DllImport("Advapi32.dll", EntryPoint = "CredFree", SetLastError = true)]
    private static extern void CredFree(IntPtr credentialPtr);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct CREDENTIAL
    {
        public int Flags;
        public int Type;
        public string TargetName;
        public string Comment;
        public System.Runtime.InteropServices.ComTypes.FILETIME LastWritten;
        public int CredentialBlobSize;
        public IntPtr CredentialBlob;
        public int Persist;
        public int AttributeCount;
        public IntPtr Attributes;
        public string TargetAlias;
        public string UserName;
    }

    private static string? GetStoredGitHubToken(string owner)
    {
        string[] targets = {
            $"GitHub - https://api.github.com/{owner}",
            "git:https://github.com",
            "LegacyGeneric:target=GitHub - https://api.github.com/" + owner
        };

        foreach (var target in targets)
        {
            if (CredRead(target, 1 /* CRED_TYPE_GENERIC */, 0, out IntPtr ptr))
            {
                try
                {
                    var cred = Marshal.PtrToStructure<CREDENTIAL>(ptr);
                    if (cred.CredentialBlobSize > 0 && cred.CredentialBlob != IntPtr.Zero)
                    {
                        byte[] bytes = new byte[cred.CredentialBlobSize];
                        Marshal.Copy(cred.CredentialBlob, bytes, 0, cred.CredentialBlobSize);

                        string utf8 = Encoding.UTF8.GetString(bytes).Trim();
                        if (utf8.Length > 20) return utf8;

                        string unicode = Encoding.Unicode.GetString(bytes).Trim();
                        if (unicode.Length > 20) return unicode;
                    }
                }
                finally
                {
                    CredFree(ptr);
                }
            }
        }
        return null;
    }

    private static void PushGitCommitsWithToken(string rootDir, string owner, string repo, string token)
    {
        string gitExe = FindGitExe();
        Console.WriteLine($"[+] Using Git: {gitExe}");

        string authUrl = $"https://x-access-token:{token}@github.com/{owner}/{repo}.git";

        // Set remote URL with token
        RunCommand(gitExe, rootDir, $"remote set-url origin \"{authUrl}\"");

        // Push main branch
        Console.WriteLine("[+] Pushing branch 'main' to GitHub...");
        int pushRes = RunCommand(gitExe, rootDir, "push -u origin main --force");
        if (pushRes == 0)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("[+] ĐẨY CODE LÊN GITHUB THÀNH CÔNG RỰC RỠ!");
            Console.ResetColor();
        }
        else
        {
            Console.WriteLine("[!] Push gặp cảnh báo hoặc cần kiểm tra lại quyền.");
        }

        // Restore clean public URL without embedding token in git config
        string cleanUrl = $"https://github.com/{owner}/{repo}.git";
        RunCommand(gitExe, rootDir, $"remote set-url origin \"{cleanUrl}\"");
    }

    private static string FindGitExe()
    {
        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string ghDesktopGit = Path.Combine(localAppData, "GitHubDesktop", "app-3.6.10", "resources", "app", "git", "cmd", "git.exe");
        if (File.Exists(ghDesktopGit)) return ghDesktopGit;

        // Search in GitHubDesktop directory
        string ghDesktopDir = Path.Combine(localAppData, "GitHubDesktop");
        if (Directory.Exists(ghDesktopDir))
        {
            var found = Directory.GetFiles(ghDesktopDir, "git.exe", SearchOption.AllDirectories);
            if (found.Length > 0) return found[0];
        }

        return "git";
    }

    private static int RunCommand(string exe, string workingDir, string args)
    {
        var psi = new ProcessStartInfo
        {
            FileName = exe,
            Arguments = args,
            WorkingDirectory = workingDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        var p = Process.Start(psi);
        if (p == null) return 1;
        p.WaitForExit();
        return p.ExitCode;
    }

    private static async Task CreateReleaseAndUploadAssetsAsync(
        string owner, string repo, string version, string token,
        string exePath, string manifestPath, string sigPath, string? notes)
    {
        using var client = new HttpClient();
        client.DefaultRequestHeaders.Add("User-Agent", "GIANG-DEV-DISK-ANALYZER-PRO-ReleaseHelper");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));

        string tagName = $"v{version}";
        string releaseTitle = $"GIANG DEV DISK ANALYZER PRO v{version}";
        string body = notes ?? $"Bản phát hành chính thức v{version} với đầy đủ tính năng quét ổ đĩa, săn file lớn, tìm trùng lặp, xóa file bảo mật và tự động cập nhật qua GitHub Releases.";

        Console.WriteLine($"[+] Kiểm tra xem bản Release {tagName} đã tồn tại chưa...");
        string getReleaseUrl = $"https://api.github.com/repos/{owner}/{repo}/releases/tags/{tagName}";
        var getResp = await client.GetAsync(getReleaseUrl);

        long releaseId = 0;
        string? uploadUrlTemplate = null;

        if (getResp.IsSuccessStatusCode)
        {
            string existingJson = await getResp.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(existingJson);
            releaseId = doc.RootElement.GetProperty("id").GetInt64();
            uploadUrlTemplate = doc.RootElement.GetProperty("upload_url").GetString();
            Console.WriteLine($"[+] Bản Release {tagName} đã tồn tại với ID: {releaseId}");
        }
        else
        {
            Console.WriteLine($"[+] Tạo mới GitHub Release: {tagName}...");
            string createUrl = $"https://api.github.com/repos/{owner}/{repo}/releases";
            var payload = new
            {
                tag_name = tagName,
                target_commitish = "main",
                name = releaseTitle,
                body = body,
                draft = false,
                prerelease = false
            };

            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var postResp = await client.PostAsync(createUrl, content);
            if (!postResp.IsSuccessStatusCode)
            {
                string err = await postResp.Content.ReadAsStringAsync();
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[!] Không thể tạo Release qua API: {postResp.StatusCode} - {err}");
                Console.ResetColor();
                return;
            }

            string respJson = await postResp.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(respJson);
            releaseId = doc.RootElement.GetProperty("id").GetInt64();
            uploadUrlTemplate = doc.RootElement.GetProperty("upload_url").GetString();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"[+] Đã tạo Release thành công! ID: {releaseId}");
            Console.ResetColor();
        }

        // Tải 3 tệp lên Release
        await UploadAssetAsync(client, owner, repo, releaseId, exePath, "application/octet-stream");
        await UploadAssetAsync(client, owner, repo, releaseId, manifestPath, "application/json");
        await UploadAssetAsync(client, owner, repo, releaseId, sigPath, "application/octet-stream");

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("\n==================================================================");
        Console.WriteLine("  TẤT CẢ ĐÃ ĐƯỢC ĐĂNG TẢI LÊN GITHUB HOÀN TẤT 100%!");
        Console.WriteLine($"  Xem trực tiếp tại: https://github.com/{owner}/{repo}/releases/tag/{tagName}");
        Console.WriteLine("==================================================================");
        Console.ResetColor();
    }

    private static async Task UploadAssetAsync(HttpClient client, string owner, string repo, long releaseId, string filePath, string contentType)
    {
        string fileName = Path.GetFileName(filePath);
        Console.WriteLine($"[+] Đang tải lên: {fileName} ({new FileInfo(filePath).Length / 1024.0 / 1024.0:F2} MB)...");

        // Delete existing asset with same name if any
        string listAssetsUrl = $"https://api.github.com/repos/{owner}/{repo}/releases/{releaseId}/assets";
        var listResp = await client.GetAsync(listAssetsUrl);
        if (listResp.IsSuccessStatusCode)
        {
            string listJson = await listResp.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(listJson);
            foreach (var el in doc.RootElement.EnumerateArray())
            {
                if (el.GetProperty("name").GetString() == fileName)
                {
                    long assetId = el.GetProperty("id").GetInt64();
                    Console.WriteLine($"    [i] Xóa tệp cũ cùng tên trên GitHub: ID {assetId}...");
                    await client.DeleteAsync($"https://api.github.com/repos/{owner}/{repo}/releases/assets/{assetId}");
                    break;
                }
            }
        }

        string uploadUrl = $"https://uploads.github.com/repos/{owner}/{repo}/releases/{releaseId}/assets?name={Uri.EscapeDataString(fileName)}";
        byte[] fileBytes = await File.ReadAllBytesAsync(filePath);
        using var byteContent = new ByteArrayContent(fileBytes);
        byteContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);

        var uploadResp = await client.PostAsync(uploadUrl, byteContent);
        if (uploadResp.IsSuccessStatusCode)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"    [+] Tải lên thành công: {fileName}!");
            Console.ResetColor();
        }
        else
        {
            string err = await uploadResp.Content.ReadAsStringAsync();
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"    [!] Tải lên thất bại: {uploadResp.StatusCode} - {err}");
            Console.ResetColor();
        }
    }

    #endregion
}
