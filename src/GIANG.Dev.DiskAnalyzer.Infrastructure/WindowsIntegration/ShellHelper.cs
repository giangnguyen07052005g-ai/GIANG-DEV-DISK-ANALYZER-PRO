using System.Diagnostics;

namespace GIANG.Dev.DiskAnalyzer.Infrastructure.WindowsIntegration;

public static class ShellHelper
{
    public static void OpenInExplorer(string fullPath)
    {
        if (string.IsNullOrWhiteSpace(fullPath)) return;

        try
        {
            if (File.Exists(fullPath))
            {
                // Select file in explorer
                Process.Start("explorer.exe", $"/select,\"{fullPath}\"");
            }
            else if (Directory.Exists(fullPath))
            {
                Process.Start("explorer.exe", $"\"{fullPath}\"");
            }
        }
        catch { }
    }

    public static void OpenFileWithDefaultApp(string fullPath)
    {
        if (string.IsNullOrWhiteSpace(fullPath) || !File.Exists(fullPath)) return;

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = fullPath,
                UseShellExecute = true
            };
            Process.Start(psi);
        }
        catch { }
    }
}
