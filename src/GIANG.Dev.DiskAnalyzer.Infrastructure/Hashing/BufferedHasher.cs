using System.Security.Cryptography;

namespace GIANG.Dev.DiskAnalyzer.Infrastructure.Hashing;

public static class BufferedHasher
{
    private const int BufferSize = 64 * 1024; // 64 KB buffer for streaming
    private const int PrefixSize = 4 * 1024;  // 4 KB for fast prefix matching

    public static async Task<string> ComputePrefixHashAsync(string filePath, CancellationToken cancellationToken = default)
    {
        byte[] buffer = new byte[PrefixSize];
        await using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, BufferSize, useAsync: true);
        int bytesRead = await stream.ReadAsync(buffer.AsMemory(0, PrefixSize), cancellationToken);

        using var sha256 = SHA256.Create();
        byte[] hashBytes = sha256.ComputeHash(buffer, 0, bytesRead);
        return Convert.ToHexString(hashBytes);
    }

    public static async Task<string> ComputeFullSha256Async(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        byte[] buffer = new byte[BufferSize];
        await using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, BufferSize, useAsync: true);
        using var sha256 = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);

        int bytesRead;
        while ((bytesRead = await stream.ReadAsync(buffer.AsMemory(0, BufferSize), cancellationToken)) > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            sha256.AppendData(buffer, 0, bytesRead);
        }

        byte[] hash = sha256.GetHashAndReset();
        return Convert.ToHexString(hash);
    }

    public static async Task<bool> CompareFilesByteByByteAsync(
        string file1,
        string file2,
        CancellationToken cancellationToken = default)
    {
        const int bufferSize = 64 * 1024;
        byte[] buffer1 = new byte[bufferSize];
        byte[] buffer2 = new byte[bufferSize];

        await using var stream1 = new FileStream(file1, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, bufferSize, useAsync: true);
        await using var stream2 = new FileStream(file2, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, bufferSize, useAsync: true);

        if (stream1.Length != stream2.Length)
            return false;

        int bytesRead1;
        while ((bytesRead1 = await stream1.ReadAsync(buffer1.AsMemory(0, bufferSize), cancellationToken)) > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            int bytesRead2 = await stream2.ReadAsync(buffer2.AsMemory(0, bytesRead1), cancellationToken);

            if (bytesRead1 != bytesRead2)
                return false;

            if (!buffer1.AsSpan(0, bytesRead1).SequenceEqual(buffer2.AsSpan(0, bytesRead2)))
                return false;
        }

        return true;
    }
}
