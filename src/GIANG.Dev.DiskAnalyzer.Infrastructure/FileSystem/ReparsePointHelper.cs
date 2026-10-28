namespace GIANG.Dev.DiskAnalyzer.Infrastructure.FileSystem;

public static class ReparsePointHelper
{
    public static bool IsReparsePoint(FileSystemInfo info)
    {
        try
        {
            return (info.Attributes & FileAttributes.ReparsePoint) != 0;
        }
        catch
        {
            return false;
        }
    }
}
