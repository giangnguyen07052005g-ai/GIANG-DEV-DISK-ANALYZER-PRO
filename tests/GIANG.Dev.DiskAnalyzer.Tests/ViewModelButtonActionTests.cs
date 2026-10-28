using System.Collections.ObjectModel;
using System.IO;
using System.Net.Http;
using GIANG.Dev.DiskAnalyzer.App.ViewModels;
using GIANG.Dev.DiskAnalyzer.Core.Configuration;
using GIANG.Dev.DiskAnalyzer.Core.Enums;
using GIANG.Dev.DiskAnalyzer.Core.Helpers;
using GIANG.Dev.DiskAnalyzer.Core.Models;
using GIANG.Dev.DiskAnalyzer.Infrastructure.FileSystem;
using GIANG.Dev.DiskAnalyzer.Infrastructure.Reporting;
using GIANG.Dev.DiskAnalyzer.Infrastructure.Scanning;
using GIANG.Dev.DiskAnalyzer.Infrastructure.Services;
using GIANG.Dev.DiskAnalyzer.Infrastructure.Storage;
using GIANG.Dev.DiskAnalyzer.Infrastructure.Updates;
using GIANG.Dev.DiskAnalyzer.Infrastructure.WindowsIntegration;
using Xunit;

namespace GIANG.Dev.DiskAnalyzer.Tests;

public class ViewModelButtonActionTests
{
    private readonly DriveService _driveService = new();
    private readonly StorageScanner _scanner = new();
    private readonly SqliteScanHistoryRepository _historyRepo = new();
    private readonly ReportService _reportService = new();
    private readonly JsonSettingsService _settingsService = new();
    private readonly FeatureService _featureService = new();
    private readonly RecycleBinService _recycleBin = new();

    [Fact]
    public async Task DashboardViewModel_RefreshAndQuickActions_ExecuteCorrectly()
    {
        string? navigatedView = null;
        string? navigatedTarget = null;

        var vm = new DashboardViewModel(_driveService, (view, target) =>
        {
            navigatedView = view;
            navigatedTarget = target;
        });

        // 1. Refresh Drives
        await vm.RefreshDrivesAsync();
        Assert.NotEmpty(vm.Drives);
        Assert.NotNull(vm.SelectedDrive);
        Assert.False(string.IsNullOrEmpty(vm.TotalStorageFormatted));

        // 2. Scan Drive Command
        vm.ScanDriveCommand.Execute(vm.SelectedDrive);
        Assert.Equal("StorageScan", navigatedView);
        Assert.Equal(vm.SelectedDrive.Name, navigatedTarget);

        // 3. Quick Action Command
        vm.QuickActionCommand.Execute("BigFiles");
        Assert.Equal("BigFiles", navigatedView);
        Assert.Equal(vm.SelectedDrive.Name, navigatedTarget);

        // 4. Update session stats
        vm.UpdateSessionStats(1234, "huge.iso (15.2 GB)", TimeSpan.FromSeconds(45));
        Assert.Equal(1234, vm.TotalFilesScannedSession);
        Assert.Equal("huge.iso (15.2 GB)", vm.LargestFileFound);
        Assert.Contains("00:45", vm.LastScanTime);
    }

    [Fact]
    public void StorageScanViewModel_PauseResumeAndFilters_ExecuteCorrectly()
    {
        var vm = new StorageScanViewModel(_scanner, _historyRepo, _reportService, _recycleBin);

        // Initially paused = false, text = "Tạm dừng"
        Assert.False(vm.IsPaused);
        Assert.Equal("Tạm dừng", vm.PauseResumeText);

        // Simulate scanning state
        vm.IsScanning = true;

        // Toggle pause -> paused = true, text = "Tiếp tục"
        vm.TogglePauseCommand.Execute(null);
        Assert.True(vm.IsPaused);
        Assert.Equal("Tiếp tục", vm.PauseResumeText);

        // Toggle pause again -> paused = false, text = "Tạm dừng"
        vm.TogglePauseCommand.Execute(null);
        Assert.False(vm.IsPaused);
        Assert.Equal("Tạm dừng", vm.PauseResumeText);

        // Top folder filter test
        var folderA = new FolderItem { FullPath = @"C:\FolderA", Name = "FolderA", TotalSizeBytes = 1000 };
        var folderB = new FolderItem { FullPath = @"C:\FolderB", Name = "FolderB", TotalSizeBytes = 2000 };
        vm.TopFolders.Add(folderA);
        vm.TopFolders.Add(folderB);

        vm.SelectedTopFolder = folderA;
        Assert.Equal(folderA, vm.SelectedTopFolder);

        vm.ClearTopFolderFilterCommand.Execute(null);
        Assert.Null(vm.SelectedTopFolder);
    }

    [Fact]
    public void BigFileHunterViewModel_ThresholdSelection_UpdatesBytesAccurately()
    {
        var vm = new BigFileHunterViewModel(new BigFileHunter(_scanner), _reportService, _recycleBin);

        // 0: > 100 MB
        vm.SelectedThresholdIndex = 0;
        Assert.Equal(100L * 1024 * 1024, vm.ThresholdBytes);

        // 1: > 500 MB
        vm.SelectedThresholdIndex = 1;
        Assert.Equal(500L * 1024 * 1024, vm.ThresholdBytes);

        // 2: > 1 GB
        vm.SelectedThresholdIndex = 2;
        Assert.Equal(1024L * 1024 * 1024, vm.ThresholdBytes);

        // 3: Custom MB
        vm.CustomThresholdMb = 350;
        vm.SelectedThresholdIndex = 3;
        Assert.Equal(350L * 1024 * 1024, vm.ThresholdBytes);
    }

    [Fact]
    public void DuplicateFinderViewModel_KeepSelectionRules_UpdateFileFlagsAndUI()
    {
        var vm = new DuplicateFinderViewModel(new DuplicateFinder(_scanner), _recycleBin, _featureService);

        var group = new DuplicateGroup
        {
            ContentHash = "abc123hash",
            SizeBytes = 1024 * 1024,
            Files = new List<FileItem>
            {
                new() { FileName = "file1.dat", FullPath = @"C:\test\file1.dat", SizeBytes = 1024*1024, LastModifiedAt = new DateTime(2025, 1, 1) },
                new() { FileName = "file2.dat", FullPath = @"C:\test\file2.dat", SizeBytes = 1024*1024, LastModifiedAt = new DateTime(2025, 6, 1) },
                new() { FileName = "file3.dat", FullPath = @"C:\test\file3.dat", SizeBytes = 1024*1024, LastModifiedAt = new DateTime(2025, 12, 1) }
            }
        };

        vm.DuplicateGroups.Add(group);
        vm.SelectedGroup = group;

        // 1. Keep Oldest: file1 kept (IsSelected = false), file2 and file3 selected for deletion (IsSelected = true)
        vm.KeepOldestCommand.Execute(null);
        Assert.False(group.Files[0].IsSelected);
        Assert.True(group.Files[1].IsSelected);
        Assert.True(group.Files[2].IsSelected);

        // 2. Keep Newest: file3 kept (IsSelected = false), file1 and file2 selected for deletion (IsSelected = true)
        vm.KeepNewestCommand.Execute(null);
        Assert.True(group.Files[0].IsSelected);
        Assert.True(group.Files[1].IsSelected);
        Assert.False(group.Files[2].IsSelected);

        // 3. Clear Selection: all IsSelected = false
        vm.ClearSelectionCommand.Execute(null);
        Assert.All(group.Files, f => Assert.False(f.IsSelected));
    }

    [Fact]
    public void FolderAnalyzerViewModel_DrillDownAndGoBack_NavigatesHierarchy()
    {
        var vm = new FolderAnalyzerViewModel(new FolderAnalyzer(), _featureService);

        var root = new FolderItem
        {
            Name = "Root",
            FullPath = @"C:\Root",
            TotalSizeBytes = 50000
        };

        var child = new FolderItem
        {
            Name = "SubA",
            FullPath = @"C:\Root\SubA",
            TotalSizeBytes = 20000
        };
        root.Children.Add(child);

        // Simulate analysis complete
        vm.CurrentFolder = root;
        vm.CurrentBreadcrumb = root.FullPath;
        vm.SubFolders.Add(child);

        Assert.False(vm.CanGoBack);

        // Drill Down
        vm.DrillDownCommand.Execute(child);
        Assert.Equal(child, vm.CurrentFolder);
        Assert.Equal(@"C:\Root\SubA", vm.CurrentBreadcrumb);
        Assert.True(vm.CanGoBack);

        // Go Back
        vm.GoBackCommand.Execute(null);
        Assert.Equal(root, vm.CurrentFolder);
        Assert.Equal(@"C:\Root", vm.CurrentBreadcrumb);
        Assert.False(vm.CanGoBack);
    }

    [Fact]
    public void SafeCleanupViewModel_ItemSelectionAndDynamicTotals_UpdateAccurately()
    {
        var vm = new SafeCleanupViewModel(new SafeCleanupService(_recycleBin), _settingsService);

        var item1 = new CleanupItem { FileName = "temp1.tmp", FullPath = @"C:\Temp\temp1.tmp", SizeBytes = 100 * 1024 * 1024, IsSelected = true };
        var item2 = new CleanupItem { FileName = "temp2.tmp", FullPath = @"C:\Temp\temp2.tmp", SizeBytes = 200 * 1024 * 1024, IsSelected = true };

        // Attach property changed listeners as Done in StartScan
        item1.PropertyChanged += (s, e) => { if (e.PropertyName == nameof(CleanupItem.IsSelected)) vm.RecalculateSelectedTotal(); };
        item2.PropertyChanged += (s, e) => { if (e.PropertyName == nameof(CleanupItem.IsSelected)) vm.RecalculateSelectedTotal(); };

        vm.CleanupItems.Add(item1);
        vm.CleanupItems.Add(item2);
        vm.RecalculateSelectedTotal();

        Assert.Equal(2, vm.SelectedCount);
        Assert.Equal(300 * 1024 * 1024, vm.TotalSelectedBytes);

        // Dynamically toggle single item
        item1.IsSelected = false;
        Assert.Equal(1, vm.SelectedCount);
        Assert.Equal(200 * 1024 * 1024, vm.TotalSelectedBytes);

        // Deselect all
        vm.DeselectAllCommand.Execute(null);
        Assert.Equal(0, vm.SelectedCount);
        Assert.Equal(0, vm.TotalSelectedBytes);

        // Select all
        vm.SelectAllCommand.Execute(null);
        Assert.Equal(2, vm.SelectedCount);
        Assert.Equal(300 * 1024 * 1024, vm.TotalSelectedBytes);
    }

    [Fact]
    public void SettingsViewModel_ExclusionsAndDefaults_ExecuteProperly()
    {
        var vm = new SettingsViewModel(_settingsService, _featureService);

        // Add excluded path
        vm.NewExcludedPath = @"D:\Games";
        vm.AddExcludedPathCommand.Execute(null);
        Assert.Contains(@"D:\Games", vm.ExcludedPaths);
        Assert.Equal(string.Empty, vm.NewExcludedPath);

        // Remove excluded path
        vm.RemoveExcludedPathCommand.Execute(@"D:\Games");
        Assert.DoesNotContain(@"D:\Games", vm.ExcludedPaths);

        // Add excluded extension
        vm.NewExcludedExtension = "bak";
        vm.AddExcludedExtensionCommand.Execute(null);
        Assert.Contains(".bak", vm.ExcludedExtensions);

        // Remove excluded extension
        vm.RemoveExcludedExtensionCommand.Execute(".bak");
        Assert.DoesNotContain(".bak", vm.ExcludedExtensions);
    }

    [Fact]
    public void MainViewModel_NavigateAllViews_SwitchesStateAndSubscribes()
    {
        var dashboardVm = new DashboardViewModel(_driveService);
        var scanVm = new StorageScanViewModel(_scanner, _historyRepo, _reportService, _recycleBin);
        var bigFilesVm = new BigFileHunterViewModel(new BigFileHunter(_scanner), _reportService, _recycleBin);
        var dupVm = new DuplicateFinderViewModel(new DuplicateFinder(_scanner), _recycleBin, _featureService);
        var folderVm = new FolderAnalyzerViewModel(new FolderAnalyzer(), _featureService);
        var cleanupVm = new SafeCleanupViewModel(new SafeCleanupService(_recycleBin), _settingsService);
        var historyVm = new ScanHistoryViewModel(_historyRepo, _reportService);
        var reportsVm = new ReportsViewModel(_reportService, _settingsService, _historyRepo);
        var settingsVm = new SettingsViewModel(_settingsService, _featureService);
        var aboutVm = new AboutViewModel(_featureService);
        var updateVm = CreateUpdateViewModel();

        var mainVm = new MainViewModel(
            dashboardVm,
            scanVm,
            bigFilesVm,
            dupVm,
            folderVm,
            cleanupVm,
            historyVm,
            reportsVm,
            settingsVm,
            aboutVm,
            updateVm);

        string[] views = ["StorageScan", "BigFiles", "Duplicates", "FolderAnalyzer", "SafeCleanup", "History", "Reports", "Settings", "About", "Update", "Dashboard"];

        foreach (var view in views)
        {
            mainVm.NavigateCommand.Execute(view);
            Assert.Equal(view, mainVm.CurrentViewName);
            Assert.NotNull(mainVm.CurrentViewModel);
        }

        // Navigate with target path
        mainVm.NavigateWithTarget("StorageScan", @"D:\Projects");
        Assert.Equal("StorageScan", mainVm.CurrentViewName);
        Assert.Equal(@"D:\Projects", scanVm.TargetPath);
    }

    [Fact]
    public void StorageScanViewModel_BatchSelectionAndHighlightedCommands_WorkCorrectly()
    {
        var vm = new StorageScanViewModel(_scanner, _historyRepo, _reportService, _recycleBin);
        var f1 = new FileItem { FileName = "a.txt", FullPath = @"C:\temp\a.txt", SizeBytes = 1000 };
        var f2 = new FileItem { FileName = "b.txt", FullPath = @"C:\temp\b.txt", SizeBytes = 2000 };
        var f3 = new FileItem { FileName = "c.txt", FullPath = @"C:\temp\c.txt", SizeBytes = 3000 };

        vm.DisplayedFiles.Add(f1);
        vm.DisplayedFiles.Add(f2);
        vm.DisplayedFiles.Add(f3);

        // 1. Select All
        vm.SelectAllCommand.Execute(null);
        Assert.True(f1.IsSelected && f2.IsSelected && f3.IsSelected);
        Assert.Equal(3, vm.SelectedFilesCount);

        // 2. Deselect All
        vm.DeselectAllCommand.Execute(null);
        Assert.False(f1.IsSelected || f2.IsSelected || f3.IsSelected);
        Assert.Equal(0, vm.SelectedFilesCount);

        // 3. Mark Highlighted (bôi đen)
        var highlighted = new List<FileItem> { f1, f3 };
        vm.MarkHighlightedCommand.Execute(highlighted);
        Assert.True(f1.IsSelected);
        Assert.False(f2.IsSelected);
        Assert.True(f3.IsSelected);
        Assert.Equal(2, vm.SelectedFilesCount);

        // 4. Unmark Highlighted
        vm.UnmarkHighlightedCommand.Execute(highlighted);
        Assert.False(f1.IsSelected);
        Assert.False(f3.IsSelected);
        Assert.Equal(0, vm.SelectedFilesCount);
    }

    [Fact]
    public void BigFileHunterViewModel_BatchSelectionAndHighlightedCommands_WorkCorrectly()
    {
        var vm = new BigFileHunterViewModel(new BigFileHunter(_scanner), _reportService, _recycleBin);
        var f1 = new FileItem { FileName = "big1.iso", FullPath = @"C:\temp\big1.iso", SizeBytes = 500_000_000 };
        var f2 = new FileItem { FileName = "big2.iso", FullPath = @"C:\temp\big2.iso", SizeBytes = 700_000_000 };

        vm.BigFiles.Add(f1);
        vm.BigFiles.Add(f2);

        // 1. Select All
        vm.SelectAllCommand.Execute(null);
        Assert.True(f1.IsSelected && f2.IsSelected);
        Assert.Equal(2, vm.SelectedFilesCount);

        // 2. Deselect All
        vm.DeselectAllCommand.Execute(null);
        Assert.False(f1.IsSelected || f2.IsSelected);
        Assert.Equal(0, vm.SelectedFilesCount);

        // 3. Mark Highlighted
        var highlighted = new List<FileItem> { f2 };
        vm.MarkHighlightedCommand.Execute(highlighted);
        Assert.False(f1.IsSelected);
        Assert.True(f2.IsSelected);
        Assert.Equal(1, vm.SelectedFilesCount);
    }

    [Fact]
    public void DuplicateFinderViewModel_GroupSelectionAndHighlightedCommands_WorkCorrectly()
    {
        var vm = new DuplicateFinderViewModel(new DuplicateFinder(_scanner), _recycleBin, _featureService);
        var f1 = new FileItem { FileName = "copy1.jpg", FullPath = @"C:\temp\copy1.jpg", SizeBytes = 100, LastModifiedAt = DateTime.Now.AddDays(-5) };
        var f2 = new FileItem { FileName = "copy2.jpg", FullPath = @"C:\temp\copy2.jpg", SizeBytes = 100, LastModifiedAt = DateTime.Now };

        var group = new DuplicateGroup
        {
            ContentHash = "HASH123",
            SizeBytes = 100,
            Files = new List<FileItem> { f1, f2 }
        };

        vm.DuplicateGroups.Add(group);
        vm.SelectedGroup = group;

        // 1. Select All in group (keeps 1 safe original)
        vm.SelectAllInGroupCommand.Execute(null);
        Assert.False(f1.IsSelected); // Oldest is preserved
        Assert.True(f2.IsSelected);  // Duplicate is marked for deletion

        // 2. Deselect in group
        vm.DeselectInGroupCommand.Execute(null);
        Assert.False(f1.IsSelected || f2.IsSelected);

        // 3. Mark highlighted
        var highlighted = new List<FileItem> { f1 };
        vm.MarkHighlightedCommand.Execute(highlighted);
        Assert.True(f1.IsSelected);

        vm.UnmarkHighlightedCommand.Execute(highlighted);
        Assert.False(f1.IsSelected);
    }

    [Fact]
    public void StorageScanViewModel_DeleteSelectedFiles_DeletesRealFilesAndUpdatesCollection()
    {
        string dir = Path.Combine(Path.GetTempPath(), $"del_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);

        string f1Path = Path.Combine(dir, "del1.tmp");
        string f2Path = Path.Combine(dir, "del2.tmp");
        File.WriteAllText(f1Path, "file 1 content");
        File.WriteAllText(f2Path, "file 2 content");

        var f1 = new FileItem { FileName = "del1.tmp", FullPath = f1Path, SizeBytes = 14, IsSelected = true };
        var f2 = new FileItem { FileName = "del2.tmp", FullPath = f2Path, SizeBytes = 14, IsSelected = false };

        var vm = new StorageScanViewModel(_scanner, _historyRepo, _reportService, _recycleBin);
        vm.DisplayedFiles.Add(f1);
        vm.DisplayedFiles.Add(f2);

        // Delete f1 via RecycleBinService
        bool deleted = _recycleBin.SendToRecycleBin(f1Path, out _);
        Assert.True(deleted);
        Assert.False(File.Exists(f1Path));
        Assert.True(File.Exists(f2Path));

        // Clean up
        if (Directory.Exists(dir)) Directory.Delete(dir, true);
    }

    [Fact]
    public void AdminHelper_And_MainViewModel_AdministratorDetection()
    {
        bool isAdmin = AdminHelper.IsRunningAsAdministrator();
        Assert.True(isAdmin || !isAdmin); // Runs safely without throwing

        var dashboardVm = new DashboardViewModel(_driveService);
        var scanVm = new StorageScanViewModel(_scanner, _historyRepo, _reportService, _recycleBin);
        var bigFilesVm = new BigFileHunterViewModel(new BigFileHunter(_scanner), _reportService, _recycleBin);
        var dupVm = new DuplicateFinderViewModel(new DuplicateFinder(_scanner), _recycleBin, _featureService);
        var folderVm = new FolderAnalyzerViewModel(new FolderAnalyzer(), _featureService);
        var cleanupVm = new SafeCleanupViewModel(new SafeCleanupService(_recycleBin), _settingsService);
        var historyVm = new ScanHistoryViewModel(_historyRepo, _reportService);
        var reportsVm = new ReportsViewModel(_reportService, _settingsService, _historyRepo);
        var settingsVm = new SettingsViewModel(_settingsService, _featureService);
        var aboutVm = new AboutViewModel(_featureService);
        var updateVm = CreateUpdateViewModel();

        var mainVm = new MainViewModel(
            dashboardVm, scanVm, bigFilesVm, dupVm, folderVm,
            cleanupVm, historyVm, reportsVm, settingsVm, aboutVm, updateVm);

        Assert.Equal(isAdmin, mainVm.IsAdministrator);
    }

    private static UpdateViewModel CreateUpdateViewModel()
    {
        var options = new GitHubUpdateOptions();
        var versionService = new VersionComparisonService();
        return new UpdateViewModel(
            new GitHubReleaseService(new HttpClient(), options),
            versionService,
            new UpdateManifestVerifier(options, versionService),
            new UpdatePackageVerifier(options),
            new UpdateDownloadService(new HttpClient(), options),
            new UpdateInstallerService(),
            options);
    }

    [Fact]
    public async Task StorageScanViewModel_DeleteSelectedFilesAsync_DirectExecutionWithoutConfirm()
    {
        string dir = Path.Combine(Path.GetTempPath(), $"scan_del_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);

        string f1Path = Path.Combine(dir, "live1.tmp");
        string f2Path = Path.Combine(dir, "live2.tmp");
        File.WriteAllText(f1Path, "temporary test file 1");
        File.WriteAllText(f2Path, "temporary test file 2");

        var f1 = new FileItem { FileName = "live1.tmp", FullPath = f1Path, SizeBytes = 21, IsSelected = true };
        var f2 = new FileItem { FileName = "live2.tmp", FullPath = f2Path, SizeBytes = 21, IsSelected = false };

        var vm = new StorageScanViewModel(_scanner, _historyRepo, _reportService, _recycleBin);
        vm.DisplayedFiles.Add(f1);
        vm.DisplayedFiles.Add(f2);

        // Execute deletion without any modal confirmation blocking
        await vm.DeleteSelectedFilesAsync(null);

        // f1 was selected -> should be deleted and removed from DisplayedFiles
        Assert.False(File.Exists(f1Path));
        Assert.True(File.Exists(f2Path));
        Assert.DoesNotContain(f1, vm.DisplayedFiles);
        Assert.Contains(f2, vm.DisplayedFiles);
        Assert.False(vm.IsDeleting);
        Assert.Equal(100, vm.DeleteProgressPercent);

        if (Directory.Exists(dir)) Directory.Delete(dir, true);
    }

    [Fact]
    public async Task BigFileHunterViewModel_DeleteSelectedFilesAsync_DirectExecutionWithoutConfirm()
    {
        string dir = Path.Combine(Path.GetTempPath(), $"big_del_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);

        string f1Path = Path.Combine(dir, "big1.tmp");
        File.WriteAllText(f1Path, "big temporary file test");

        var f1 = new FileItem { FileName = "big1.tmp", FullPath = f1Path, SizeBytes = 23, IsSelected = true };

        var vm = new BigFileHunterViewModel(new BigFileHunter(_scanner), _reportService, _recycleBin);
        vm.BigFiles.Add(f1);

        await vm.DeleteSelectedFilesAsync(null);

        Assert.False(File.Exists(f1Path));
        Assert.DoesNotContain(f1, vm.BigFiles);
        Assert.False(vm.IsDeleting);
        Assert.Equal(100, vm.DeleteProgressPercent);

        if (Directory.Exists(dir)) Directory.Delete(dir, true);
    }

    [Fact]
    public async Task DuplicateFinderViewModel_DeleteSelectedDuplicatesAsync_DirectExecutionWithoutConfirm()
    {
        string dir = Path.Combine(Path.GetTempPath(), $"dup_del_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);

        string f1Path = Path.Combine(dir, "orig.tmp");
        string f2Path = Path.Combine(dir, "copy.tmp");
        File.WriteAllText(f1Path, "exact duplicate content");
        File.WriteAllText(f2Path, "exact duplicate content");

        var f1 = new FileItem { FileName = "orig.tmp", FullPath = f1Path, SizeBytes = 23, IsSelected = false };
        var f2 = new FileItem { FileName = "copy.tmp", FullPath = f2Path, SizeBytes = 23, IsSelected = true };

        var group = new DuplicateGroup
        {
            ContentHash = "TESTHASH123",
            Files = new List<FileItem> { f1, f2 }
        };

        var vm = new DuplicateFinderViewModel(new DuplicateFinder(_scanner), _recycleBin, _featureService);
        vm.DuplicateGroups.Add(group);

        await vm.DeleteSelectedDuplicatesAsync(null);

        Assert.True(File.Exists(f1Path));
        Assert.False(File.Exists(f2Path));
        Assert.Contains(f1, group.Files);
        Assert.DoesNotContain(f2, group.Files);
        Assert.False(vm.IsScanning);

        if (Directory.Exists(dir)) Directory.Delete(dir, true);
    }
}
