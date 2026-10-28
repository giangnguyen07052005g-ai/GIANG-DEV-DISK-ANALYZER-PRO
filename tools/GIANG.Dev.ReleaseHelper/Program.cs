using System.Diagnostics;
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
        Console.WriteLine("  GIANG DEV DISK ANALYZER PRO — RELEASE PREPARATION TOOL");
        Console.WriteLine("==================================================================");

        string rootDir = FindSolutionRoot();
        Console.WriteLine($"[+] Solution Root: {rootDir}");

        string version = "1.0.4";
        string? customExe = null;
        string? notes = null;
        string? owner = null;
        string? repo = null;

        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--version" && i + 1 < args.Length) version = args[++i];
            else if (args[i] == "--exe" && i + 1 < args.Length) customExe = args[++i];
            else if (args[i] == "--notes" && i + 1 < args.Length) notes = args[++i];
            else if (args[i] == "--owner" && i + 1 < args.Length) owner = args[++i];
            else if (args[i] == "--repo" && i + 1 < args.Length) repo = args[++i];
        }

        Console.WriteLine($"[+] Target Release Version: v{version}");

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
            // Kiểm tra publish\win-x64\GIANG.Dev.DiskAnalyzer.App.exe trước
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
            architecture = "x64",
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

        Console.WriteLine("\n[+] 3 TỆP CẦN TẢI LÊN GITHUB RELEASE:");
        Console.WriteLine($"  1. {Path.GetFileName(targetExePath)} ({exeSize / 1024.0 / 1024.0:F2} MB)");
        Console.WriteLine($"  2. {Path.GetFileName(manifestFile)}");
        Console.WriteLine($"  3. {Path.GetFileName(sigFile)}");
        Console.WriteLine($"\n[+] Thư mục chứa tệp: {outputDir}");

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

            // Cập nhật PublicKey
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
                        Console.WriteLine("[+] Updating PublicKeyBase64 in GitHubUpdateOptions.cs...");
                        code = code.Substring(0, startQuote + 1) + publicKeyBase64 + code.Substring(endQuote);
                    }
                }
            }

            // Cập nhật Owner nếu được cung cấp
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
                        Console.WriteLine($"[+] Updating GitHubOwner to '{owner}' in GitHubUpdateOptions.cs...");
                        code = code.Substring(0, startQuote + 1) + owner.Trim() + code.Substring(endQuote);
                    }
                }
            }

            // Cập nhật Repo nếu được cung cấp
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
                        Console.WriteLine($"[+] Updating Repository to '{repo}' in GitHubUpdateOptions.cs...");
                        code = code.Substring(0, startQuote + 1) + repo.Trim() + code.Substring(endQuote);
                    }
                }
            }

            File.WriteAllText(configPath, code, Encoding.UTF8);
        }

        // Tạo/Cập nhật github-update.json trong thư mục gốc dự án
        string jsonConfigPath = Path.Combine(rootDir, "github-update.json");
        var jsonConfig = new
        {
            GitHubOwner = owner ?? "YOUR_GITHUB_USERNAME",
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
}
