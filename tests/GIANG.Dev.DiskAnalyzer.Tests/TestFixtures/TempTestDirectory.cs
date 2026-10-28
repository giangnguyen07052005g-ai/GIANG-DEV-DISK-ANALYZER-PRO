namespace GIANG.Dev.DiskAnalyzer.Tests.TestFixtures;

public class TempTestDirectory : IDisposable
{
    public string Path { get; }

    public TempTestDirectory(string prefix = "DiskAnalyzerTest_")
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"{prefix}{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path);
    }

    public string CreateFile(string relativePath, byte[] content)
    {
        string fullPath = System.IO.Path.Combine(Path, relativePath);
        string dir = System.IO.Path.GetDirectoryName(fullPath) ?? string.Empty;
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        File.WriteAllBytes(fullPath, content);
        return fullPath;
    }

    public string CreateTextFile(string relativePath, string text)
    {
        string fullPath = System.IO.Path.Combine(Path, relativePath);
        string dir = System.IO.Path.GetDirectoryName(fullPath) ?? string.Empty;
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        File.WriteAllText(fullPath, text, System.Text.Encoding.UTF8);
        return fullPath;
    }

    public string CreateSubdirectory(string relativePath)
    {
        string fullPath = System.IO.Path.Combine(Path, relativePath);
        Directory.CreateDirectory(fullPath);
        return fullPath;
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
        catch
        {
            // Ignore cleanup errors during test teardown
        }
    }
}
