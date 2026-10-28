using GIANG.Dev.DiskAnalyzer.Core.Models;

namespace GIANG.Dev.DiskAnalyzer.Core.Interfaces;

public interface IDriveService
{
    Task<IReadOnlyList<DriveItem>> GetDrivesAsync(CancellationToken cancellationToken = default);
    DriveItem? GetDrive(string driveLetter);
}
