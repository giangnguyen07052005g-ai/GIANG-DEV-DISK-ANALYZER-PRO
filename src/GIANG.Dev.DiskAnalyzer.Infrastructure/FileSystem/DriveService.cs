using GIANG.Dev.DiskAnalyzer.Core.Interfaces;
using GIANG.Dev.DiskAnalyzer.Core.Models;
using Microsoft.Extensions.Logging;

namespace GIANG.Dev.DiskAnalyzer.Infrastructure.FileSystem;

public class DriveService : IDriveService
{
    private readonly ILogger<DriveService>? _logger;

    public DriveService(ILogger<DriveService>? logger = null)
    {
        _logger = logger;
    }

    public Task<IReadOnlyList<DriveItem>> GetDrivesAsync(CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            var result = new List<DriveItem>();
            try
            {
                var drives = DriveInfo.GetDrives();
                foreach (var d in drives)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var item = new DriveItem
                    {
                        Name = d.Name,
                        DriveType = d.DriveType.ToString()
                    };

                    try
                    {
                        if (d.IsReady)
                        {
                            item.IsReady = true;
                            item.VolumeLabel = d.VolumeLabel;
                            item.DriveFormat = d.DriveFormat;
                            item.TotalSizeBytes = d.TotalSize;
                            item.FreeSizeBytes = d.TotalFreeSpace;
                        }
                        else
                        {
                            item.IsReady = false;
                            item.VolumeLabel = "Chưa sẵn sàng";
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning(ex, "Không thể đọc thông số ổ đĩa {DriveName}", d.Name);
                        item.IsReady = false;
                        item.VolumeLabel = "Không có quyền truy cập";
                    }

                    result.Add(item);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Lỗi khi lấy danh sách ổ đĩa hệ thống");
            }

            return (IReadOnlyList<DriveItem>)result;
        }, cancellationToken);
    }

    public DriveItem? GetDrive(string driveLetter)
    {
        if (string.IsNullOrWhiteSpace(driveLetter)) return null;

        try
        {
            string cleanLetter = driveLetter.Trim().ToUpperInvariant();
            if (!cleanLetter.EndsWith("\\")) cleanLetter += "\\";

            var drives = DriveInfo.GetDrives();
            var target = drives.FirstOrDefault(d => string.Equals(d.Name, cleanLetter, StringComparison.OrdinalIgnoreCase));
            if (target == null) return null;

            var item = new DriveItem
            {
                Name = target.Name,
                DriveType = target.DriveType.ToString()
            };

            if (target.IsReady)
            {
                item.IsReady = true;
                item.VolumeLabel = target.VolumeLabel;
                item.DriveFormat = target.DriveFormat;
                item.TotalSizeBytes = target.TotalSize;
                item.FreeSizeBytes = target.TotalFreeSpace;
            }
            else
            {
                item.IsReady = false;
            }

            return item;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Lỗi khi truy vấn ổ đĩa {DriveLetter}", driveLetter);
            return null;
        }
    }
}
