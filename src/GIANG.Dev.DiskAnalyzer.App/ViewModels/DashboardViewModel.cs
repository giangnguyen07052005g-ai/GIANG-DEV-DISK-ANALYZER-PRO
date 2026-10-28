using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GIANG.Dev.DiskAnalyzer.Core.Helpers;
using GIANG.Dev.DiskAnalyzer.Core.Interfaces;
using GIANG.Dev.DiskAnalyzer.Core.Models;

namespace GIANG.Dev.DiskAnalyzer.App.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    private readonly IDriveService _driveService;
    public Action<string, string?>? NavigateToAction { get; set; }

    [ObservableProperty]
    private ObservableCollection<DriveItem> _drives = new();

    [ObservableProperty]
    private DriveItem? _selectedDrive;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _totalStorageFormatted = "0 GB";

    [ObservableProperty]
    private string _totalUsedFormatted = "0 GB";

    [ObservableProperty]
    private string _totalFreeFormatted = "0 GB";

    [ObservableProperty]
    private double _overallUsedPercentage;

    [ObservableProperty]
    private string _lastScanTime = "Chưa có lượt quét nào";

    [ObservableProperty]
    private long _totalFilesScannedSession;

    [ObservableProperty]
    private string _largestFileFound = "Chưa phát hiện";

    public DashboardViewModel(IDriveService driveService, Action<string, string?>? navigateToAction = null)
    {
        _driveService = driveService;
        NavigateToAction = navigateToAction;
    }

    [RelayCommand]
    public async Task RefreshDrivesAsync()
    {
        IsLoading = true;
        try
        {
            var drivesList = await _driveService.GetDrivesAsync();
            Drives.Clear();

            long totalBytes = 0;
            long usedBytes = 0;
            long freeBytes = 0;

            foreach (var d in drivesList)
            {
                Drives.Add(d);
                if (d.IsReady)
                {
                    totalBytes += d.TotalSizeBytes;
                    usedBytes += d.UsedSizeBytes;
                    freeBytes += d.FreeSizeBytes;
                }
            }

            TotalStorageFormatted = ByteSizeFormatter.Format(totalBytes);
            TotalUsedFormatted = ByteSizeFormatter.Format(usedBytes);
            TotalFreeFormatted = ByteSizeFormatter.Format(freeBytes);
            OverallUsedPercentage = totalBytes > 0 ? (double)usedBytes / totalBytes * 100.0 : 0;

            if (SelectedDrive == null || !Drives.Contains(SelectedDrive))
            {
                SelectedDrive = Drives.FirstOrDefault(d => d.IsReady);
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public void ScanDrive(DriveItem? drive)
    {
        if (drive != null && drive.IsReady)
        {
            NavigateToAction?.Invoke("StorageScan", drive.Name);
        }
    }

    [RelayCommand]
    public void QuickAction(string destination)
    {
        NavigateToAction?.Invoke(destination, SelectedDrive?.Name);
    }

    public void UpdateSessionStats(long filesScanned, string largestFile, TimeSpan duration)
    {
        TotalFilesScannedSession += filesScanned;
        if (!string.IsNullOrEmpty(largestFile))
        {
            LargestFileFound = largestFile;
        }
        LastScanTime = $"{DateTime.Now:HH:mm:ss} (thời gian: {duration:mm\\:ss})";
    }
}
