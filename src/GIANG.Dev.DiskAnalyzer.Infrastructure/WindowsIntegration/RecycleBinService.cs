using System.Runtime.InteropServices;
using GIANG.Dev.DiskAnalyzer.Core.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic.FileIO;
using VbFileSystem = Microsoft.VisualBasic.FileIO.FileSystem;

namespace GIANG.Dev.DiskAnalyzer.Infrastructure.WindowsIntegration;

public class RecycleBinService
{
    private readonly ILogger<RecycleBinService>? _logger;

    public RecycleBinService(ILogger<RecycleBinService>? logger = null)
    {
        _logger = logger;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct SHFILEOPSTRUCT
    {
        public IntPtr hwnd;
        public uint wFunc;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string pFrom;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string? pTo;
        public ushort fFlags;
        [MarshalAs(UnmanagedType.Bool)]
        public bool fAnyOperationsAborted;
        public IntPtr hNameMappings;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string? lpszProgressTitle;
    }

    private const uint FO_DELETE = 0x0003;
    private const ushort FOF_ALLOWUNDO = 0x0040;
    private const ushort FOF_NOCONFIRMATION = 0x0010;
    private const ushort FOF_SILENT = 0x0004;
    private const ushort FOF_NOERRORUI = 0x0400;

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern int SHFileOperation(ref SHFILEOPSTRUCT lpFileOp);

    public bool SendToRecycleBin(string filePath, out string errorMessage)
    {
        errorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(filePath))
        {
            errorMessage = "Đường dẫn không hợp lệ.";
            return false;
        }

        if (PathSafetyValidator.IsSystemProtectedPath(filePath))
        {
            errorMessage = "Từ chối thực thi: File hoặc thư mục thuộc vùng hệ thống được bảo vệ.";
            return false;
        }

        bool isDir = Directory.Exists(filePath);
        bool isFile = File.Exists(filePath);

        if (!isFile && !isDir)
        {
            errorMessage = "Mục không còn tồn tại trên đĩa.";
            return false;
        }

        try
        {
            // Clear ReadOnly attribute so Windows Shell doesn't block deletion
            if (isFile)
            {
                var attrs = File.GetAttributes(filePath);
                if ((attrs & FileAttributes.ReadOnly) != 0)
                {
                    File.SetAttributes(filePath, attrs & ~FileAttributes.ReadOnly);
                }
            }

            // Primary: Use standard Microsoft.VisualBasic.FileIO.FileSystem
            if (isDir)
            {
                VbFileSystem.DeleteDirectory(
                    filePath,
                    UIOption.OnlyErrorDialogs,
                    RecycleOption.SendToRecycleBin);
            }
            else
            {
                VbFileSystem.DeleteFile(
                    filePath,
                    UIOption.OnlyErrorDialogs,
                    RecycleOption.SendToRecycleBin);
            }

            _logger?.LogInformation("Đã chuyển thành công vào Thùng rác: {Path}", filePath);
            return true;
        }
        catch (OperationCanceledException)
        {
            errorMessage = "Thao tác đưa vào Thùng rác đã bị người dùng hủy.";
            return false;
        }
        catch (UnauthorizedAccessException uaEx)
        {
            errorMessage = $"Không có đủ quyền xóa file (Access Denied): {uaEx.Message}";
            _logger?.LogWarning(uaEx, "Không đủ quyền xóa {Path}", filePath);
            return false;
        }
        catch (IOException ioEx)
        {
            errorMessage = $"File đang được một tiến trình khác sử dụng hoặc bị khóa: {ioEx.Message}";
            _logger?.LogWarning(ioEx, "File đang bị khóa {Path}", filePath);
            return false;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "FileSystem.DeleteFile thất bại cho {Path}, đang thử SHFileOperation fallback...", filePath);
            return TryShFileOperationFallback(filePath, isDir, out errorMessage);
        }
    }

    public bool DeletePermanently(string filePath, out string errorMessage)
    {
        errorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(filePath))
        {
            errorMessage = "Đường dẫn không hợp lệ.";
            return false;
        }

        if (PathSafetyValidator.IsSystemProtectedPath(filePath))
        {
            errorMessage = "Từ chối thực thi: File hoặc thư mục thuộc vùng hệ thống được bảo vệ.";
            return false;
        }

        bool isDir = Directory.Exists(filePath);
        bool isFile = File.Exists(filePath);

        if (!isFile && !isDir)
        {
            errorMessage = "Mục không còn tồn tại trên đĩa.";
            return false;
        }

        try
        {
            if (isFile)
            {
                var attrs = File.GetAttributes(filePath);
                if ((attrs & FileAttributes.ReadOnly) != 0)
                {
                    File.SetAttributes(filePath, attrs & ~FileAttributes.ReadOnly);
                }
                File.Delete(filePath);
            }
            else
            {
                Directory.Delete(filePath, recursive: true);
            }

            _logger?.LogInformation("Đã xóa vĩnh viễn file/thư mục: {Path}", filePath);
            return true;
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            _logger?.LogError(ex, "Lỗi khi xóa vĩnh viễn {Path}", filePath);
            return false;
        }
    }

    private bool TryShFileOperationFallback(string filePath, bool isDir, out string errorMessage)
    {
        errorMessage = string.Empty;
        try
        {
            string doubleNullTerminated = filePath + '\0' + '\0';
            var fileOp = new SHFILEOPSTRUCT
            {
                wFunc = FO_DELETE,
                pFrom = doubleNullTerminated,
                fFlags = FOF_ALLOWUNDO | FOF_NOCONFIRMATION | FOF_SILENT | FOF_NOERRORUI
            };

            int result = SHFileOperation(ref fileOp);
            if (result != 0)
            {
                errorMessage = $"Mã lỗi Windows Shell: {result} (Không thể đưa vào Thùng rác)";
                return false;
            }

            if (fileOp.fAnyOperationsAborted)
            {
                errorMessage = "Thao tác đưa vào Thùng rác đã bị người dùng hủy.";
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            return false;
        }
    }
}
