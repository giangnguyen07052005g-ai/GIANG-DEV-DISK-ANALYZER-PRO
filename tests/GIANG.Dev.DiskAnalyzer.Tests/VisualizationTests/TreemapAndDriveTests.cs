using GIANG.Dev.DiskAnalyzer.Core.Models;
using GIANG.Dev.DiskAnalyzer.Infrastructure.FileSystem;
using GIANG.Dev.DiskAnalyzer.Infrastructure.Scanning;
using Xunit;

namespace GIANG.Dev.DiskAnalyzer.Tests.VisualizationTests;

public class TreemapAndDriveTests
{
    // 10. Ổ đĩa bị ngắt kết nối hoặc không sẵn sàng (DriveService handles safely)
    [Fact]
    public async Task DriveService_GetDrivesAsync_ReturnsSystemDrivesWithoutThrowing()
    {
        var driveService = new DriveService();
        var drives = await driveService.GetDrivesAsync();

        Assert.NotNull(drives);
        Assert.NotEmpty(drives);

        // At least system drive C: should be present
        Assert.Contains(drives, d => d.Name.StartsWith("C:", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void DriveService_InvalidDriveLetter_ReturnsNullGracefully()
    {
        var driveService = new DriveService();
        var invalidDrive = driveService.GetDrive("Z9:");
        Assert.Null(invalidDrive);
    }

    // 18. Treemap calculation logic & proportional area distribution
    [Fact]
    public void FolderAnalyzer_BuildTreemapHierarchy_CalculatesProportionalRectangles()
    {
        var analyzer = new FolderAnalyzer();
        var root = new FolderItem
        {
            Name = "Root",
            FullPath = @"C:\Root",
            TotalSizeBytes = 1000,
            Children = new List<FolderItem>
            {
                new() { Name = "Child1", FullPath = @"C:\Root\Child1", TotalSizeBytes = 700 },
                new() { Name = "Child2", FullPath = @"C:\Root\Child2", TotalSizeBytes = 300 }
            }
        };

        double canvasWidth = 800;
        double canvasHeight = 400;

        var treemapNodes = analyzer.BuildTreemapHierarchy(root, canvasWidth, canvasHeight);

        Assert.Equal(2, treemapNodes.Count);
        Assert.Equal(70.0, treemapNodes[0].Percentage);
        Assert.Equal(30.0, treemapNodes[1].Percentage);
        Assert.True(treemapNodes[0].Width > 0);
        Assert.True(treemapNodes[1].Width > 0);
    }
}
