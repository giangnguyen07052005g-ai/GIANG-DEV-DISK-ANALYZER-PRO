namespace GIANG.Dev.DiskAnalyzer.Core.Helpers;

public static class PathSafetyValidator
{
    private static readonly HashSet<string> ProtectedDirectoryNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "windows",
        "system32",
        "syswow64",
        "winsxs",
        "program files",
        "program files (x86)",
        "system volume information",
        "$recycle.bin",
        "recovery",
        "boot",
        "documents and settings"
    };

    private static readonly HashSet<string> DangerousExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".sys",
        ".drv",
        ".ocx",
        ".cpl",
        ".efi",
        ".msc",
        ".bat",
        ".cmd",
        ".ps1",
        ".vbs"
    };

    public static bool IsSystemProtectedPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return true;

        try
        {
            string normalized = Path.GetFullPath(path).TrimEnd('\\', '/');

            // Check against Windows Directory
            string winDir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            if (!string.IsNullOrEmpty(winDir) && normalized.StartsWith(winDir, StringComparison.OrdinalIgnoreCase))
                return true;

            // Check against Program Files
            string progFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            if (!string.IsNullOrEmpty(progFiles) && normalized.StartsWith(progFiles, StringComparison.OrdinalIgnoreCase))
                return true;

            string progFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            if (!string.IsNullOrEmpty(progFilesX86) && normalized.StartsWith(progFilesX86, StringComparison.OrdinalIgnoreCase))
                return true;

            // Check drive root system directories (e.g. C:\System Volume Information or D:\$Recycle.Bin)
            string[] segments = normalized.Split(new[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length >= 2 && ProtectedDirectoryNames.Contains(segments[1]))
            {
                return true;
            }

            // Check root drive itself (e.g. C:\ or C:)
            var root = Path.GetPathRoot(normalized);
            if (string.Equals(root?.TrimEnd('\\', '/'), normalized, StringComparison.OrdinalIgnoreCase))
                return true;

            return false;
        }
        catch
        {
            // If path parsing fails, consider it protected for safety!
            return true;
        }
    }

    public static bool IsSafeForCleanup(string filePath, out string rejectionReason)
    {
        rejectionReason = string.Empty;

        if (string.IsNullOrWhiteSpace(filePath))
        {
            rejectionReason = "Đường dẫn không hợp lệ hoặc rỗng.";
            return false;
        }

        string tempPath = Path.GetTempPath().TrimEnd('\\', '/');
        string winTemp = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Temp").TrimEnd('\\', '/');
        string fullPath;
        try
        {
            fullPath = Path.GetFullPath(filePath);
        }
        catch (Exception ex)
        {
            rejectionReason = $"Không thể chuẩn hóa đường dẫn: {ex.Message}";
            return false;
        }

        string ext = Path.GetExtension(filePath);
        if (DangerousExtensions.Contains(ext))
        {
            rejectionReason = $"Đuôi file ({ext}) được bảo vệ tránh rủi ro hệ thống.";
            return false;
        }

        // Windows\Temp contains safe temp files, so allow it even though it's inside Windows folder
        bool isInWinTemp = fullPath.StartsWith(winTemp + "\\", StringComparison.OrdinalIgnoreCase) ||
                           string.Equals(fullPath, winTemp, StringComparison.OrdinalIgnoreCase);

        if (!isInWinTemp && IsSystemProtectedPath(filePath))
        {
            rejectionReason = "File thuộc phân vùng hệ thống Windows hoặc Program Files được bảo vệ.";
            return false;
        }

        // Safe cleanup target check
        bool isInUserTemp = fullPath.StartsWith(tempPath, StringComparison.OrdinalIgnoreCase);
        bool isThumbCache = fullPath.Contains("Explorer\\thumbcache", StringComparison.OrdinalIgnoreCase);
        bool isAppCache = fullPath.Contains("\\Cache\\", StringComparison.OrdinalIgnoreCase) ||
                          fullPath.Contains("\\GPUCache\\", StringComparison.OrdinalIgnoreCase);
        bool isLogOrDump = ext.Equals(".log", StringComparison.OrdinalIgnoreCase) || 
                           ext.Equals(".dmp", StringComparison.OrdinalIgnoreCase) ||
                           ext.Equals(".tmp", StringComparison.OrdinalIgnoreCase);

        if (isInUserTemp || isInWinTemp || isThumbCache || isAppCache || isLogOrDump)
        {
            return true;
        }

        rejectionReason = "File không nằm trong các khu vực dọn dẹp an toàn đã được chứng thực (Temp/Cache).";
        return false;
    }

    public static bool IsSubpathOf(string candidatePath, string parentPath)
    {
        try
        {
            string normalizedCandidate = Path.GetFullPath(candidatePath).TrimEnd('\\', '/');
            string normalizedParent = Path.GetFullPath(parentPath).TrimEnd('\\', '/');

            return normalizedCandidate.StartsWith(normalizedParent + "\\", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(normalizedCandidate, normalizedParent, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }
}
