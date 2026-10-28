using System.Text;
using GIANG.Dev.DiskAnalyzer.Core.Models;
using GIANG.Dev.DiskAnalyzer.Infrastructure.Hashing;
using GIANG.Dev.DiskAnalyzer.Infrastructure.Scanning;
using GIANG.Dev.DiskAnalyzer.Tests.TestFixtures;
using Xunit;

namespace GIANG.Dev.DiskAnalyzer.Tests.HashingTests;

public class DuplicateFinderTests
{
    private readonly StorageScanner _scanner = new();

    // 5. File trùng nội dung nhưng khác tên (SHA-256 matches despite different filenames)
    [Fact]
    public async Task FindDuplicates_SameContentDifferentNames_IdentifiesDuplicateGroup()
    {
        using var tempDir = new TempTestDirectory();
        byte[] identicalContent = Encoding.UTF8.GetBytes("GIANG DEV DISK ANALYZER - Identical Content Test 2026");

        tempDir.CreateFile("Original_Document.docx", identicalContent);
        tempDir.CreateFile("Copy_Of_Document_Different_Name.docx", identicalContent);
        tempDir.CreateFile("Another_Copy.dat", identicalContent);

        var finder = new DuplicateFinder(_scanner);
        var options = new DuplicateScanOptions
        {
            TargetPath = tempDir.Path,
            FastHashFirst = true,
            VerifyByteByByte = true
        };

        var groups = await finder.FindDuplicatesAsync(tempDir.Path, options, null, CancellationToken.None);

        Assert.Single(groups);
        var group = groups[0];
        Assert.Equal(3, group.Files.Count);
        Assert.Equal(identicalContent.Length, group.SizeBytes);

        // Recoverable savings must be (3 - 1) * size
        long expectedWasted = (3 - 1) * identicalContent.Length;
        Assert.Equal(expectedWasted, group.WastedSizeBytes);
    }

    // 6. File cùng kích thước nhưng nội dung khác (Avoids false positives)
    [Fact]
    public async Task FindDuplicates_SameSizeDifferentContent_NoDuplicateReported()
    {
        using var tempDir = new TempTestDirectory();
        byte[] content1 = new byte[1024];
        byte[] content2 = new byte[1024];
        Array.Fill(content1, (byte)'A');
        Array.Fill(content2, (byte)'B');

        tempDir.CreateFile("file_A.dat", content1);
        tempDir.CreateFile("file_B.dat", content2);

        var finder = new DuplicateFinder(_scanner);
        var options = new DuplicateScanOptions
        {
            TargetPath = tempDir.Path,
            FastHashFirst = true,
            VerifyByteByByte = true
        };

        var groups = await finder.FindDuplicatesAsync(tempDir.Path, options, null, CancellationToken.None);

        Assert.Empty(groups); // Different SHA-256 hashes, therefore 0 duplicate groups
    }

    // 20. Kiểm tra lỗi I/O trong khi tính SHA-256 (Streaming buffered hash)
    [Fact]
    public async Task BufferedHasher_ValidFile_ComputesSha256Successfully()
    {
        using var tempDir = new TempTestDirectory();
        string testFile = tempDir.CreateTextFile("hash_test.txt", "Testing SHA-256 streaming");

        string hash = await BufferedHasher.ComputeFullSha256Async(testFile, CancellationToken.None);

        Assert.NotNull(hash);
        Assert.Equal(64, hash.Length); // 64 hex characters for SHA-256
    }

    [Fact]
    public async Task BufferedHasher_PrefixHash_MatchesForIdenticalPrefix()
    {
        using var tempDir = new TempTestDirectory();
        byte[] prefix = new byte[8192];
        new Random(42).NextBytes(prefix);

        string f1 = tempDir.CreateFile("f1.dat", prefix);
        string f2 = tempDir.CreateFile("f2.dat", prefix);

        string h1 = await BufferedHasher.ComputePrefixHashAsync(f1, CancellationToken.None);
        string h2 = await BufferedHasher.ComputePrefixHashAsync(f2, CancellationToken.None);

        Assert.Equal(h1, h2);
    }
}
