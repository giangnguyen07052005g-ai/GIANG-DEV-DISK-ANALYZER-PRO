namespace GIANG.Dev.DiskAnalyzer.Core.Helpers;

public static class ByteSizeFormatter
{
    private static readonly string[] Units = { "B", "KB", "MB", "GB", "TB", "PB" };

    public static string Format(long bytes)
    {
        if (bytes < 0)
        {
            return "-" + Format(-bytes);
        }

        if (bytes == 0)
        {
            return "0 B";
        }

        double val = bytes;
        int unitIndex = 0;

        while (val >= 1024 && unitIndex < Units.Length - 1)
        {
            val /= 1024;
            unitIndex++;
        }

        if (unitIndex == 0)
        {
            return $"{bytes:N0} B";
        }

        return $"{val:0.##} {Units[unitIndex]}";
    }

    public static string FormatWithExact(long bytes)
    {
        return $"{Format(bytes)} ({bytes:N0} bytes)";
    }
}
