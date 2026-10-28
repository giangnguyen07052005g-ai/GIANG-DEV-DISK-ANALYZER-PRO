using System.Diagnostics;
using System.IO;
using GIANG.Dev.DiskAnalyzer.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace GIANG.Dev.DiskAnalyzer.Infrastructure.Updates;

/// <summary>
/// Dịch vụ khởi chạy an toàn trình cài đặt bản cập nhật Windows.
/// Đảm bảo quyền nâng cao (UAC) được yêu cầu minh bạch và không tự ý tắt ứng dụng nếu cài đặt thất bại.
/// </summary>
public class UpdateInstallerService : IUpdateInstallerService
{
    private readonly ILogger<UpdateInstallerService>? _logger;
    private readonly Action? _shutdownAction;

    public UpdateInstallerService(ILogger<UpdateInstallerService>? logger = null, Action? shutdownAction = null)
    {
        _logger = logger;
        _shutdownAction = shutdownAction;
    }

    public bool LaunchInstaller(string installerPath, out string errorMessage)
    {
        errorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(installerPath) || !File.Exists(installerPath))
        {
            errorMessage = "Không tìm thấy tệp trình cài đặt trên hệ thống.";
            return false;
        }

        string ext = Path.GetExtension(installerPath).ToLowerInvariant();
        if (ext != ".exe" && ext != ".msi")
        {
            errorMessage = $"Định dạng tệp không được hỗ trợ để chạy cài đặt: '{ext}'. Chỉ chấp nhận .exe hoặc .msi.";
            return false;
        }

        try
        {
            _logger?.LogInformation("Khởi chạy trình cài đặt cập nhật: {Path}", installerPath);

            var startInfo = new ProcessStartInfo
            {
                FileName = installerPath,
                UseShellExecute = true,
                Verb = "runas" // Yêu cầu quyền quản trị viên UAC minh bạch từ Windows
            };

            var process = Process.Start(startInfo);
            if (process == null)
            {
                errorMessage = "Không thể khởi động tiến trình cài đặt từ hệ điều hành.";
                return false;
            }

            _logger?.LogInformation("Trình cài đặt đã được kích hoạt thành công (PID: {Pid}). Đang chuẩn bị đóng ứng dụng để nâng cấp...", process.Id);

            // Đóng ứng dụng an toàn
            if (_shutdownAction != null)
            {
                _shutdownAction();
            }
            else
            {
                Environment.Exit(0);
            }

            return true;
        }
        catch (System.ComponentModel.Win32Exception ex) when (ex.NativeErrorCode == 1223)
        {
            // Mã lỗi 1223: ERROR_CANCELLED (Người dùng bấm 'No' trên hộp thoại UAC)
            errorMessage = "Thao tác cài đặt đã bị người dùng từ chối trên hộp thoại phân quyền Windows UAC.";
            _logger?.LogWarning("Người dùng từ chối xác thực UAC.");
            return false;
        }
        catch (Exception ex)
        {
            errorMessage = $"Lỗi khi kích hoạt trình cài đặt: {ex.Message}";
            _logger?.LogError(ex, "Lỗi kích hoạt trình cài đặt: {Path}", installerPath);
            return false;
        }
    }
}
