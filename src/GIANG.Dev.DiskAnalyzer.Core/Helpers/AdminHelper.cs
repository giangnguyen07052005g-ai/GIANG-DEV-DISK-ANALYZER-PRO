using System.Security.Principal;

namespace GIANG.Dev.DiskAnalyzer.Core.Helpers;

public static class AdminHelper
{
    public static bool IsRunningAsAdministrator()
    {
        try
        {
            if (OperatingSystem.IsWindows())
            {
                using var identity = WindowsIdentity.GetCurrent();
                var principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            return false;
        }
        catch
        {
            return false;
        }
    }
}
