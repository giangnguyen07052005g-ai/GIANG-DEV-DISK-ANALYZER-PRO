using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace GIANG.Dev.DiskAnalyzer.App.ViewModels;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private string _currentViewName = "Dashboard";

    [ObservableProperty]
    private object? _currentViewModel;

    [ObservableProperty]
    private string _globalStatusText = "Hệ thống sẵn sàng";

    [ObservableProperty]
    private string _appVersion = "v1.0.2 PRO";

    [ObservableProperty]
    private bool _isAdministrator = GIANG.Dev.DiskAnalyzer.Core.Helpers.AdminHelper.IsRunningAsAdministrator();

    // ViewModels
    public DashboardViewModel DashboardVM { get; }
    public StorageScanViewModel StorageScanVM { get; }
    public BigFileHunterViewModel BigFileHunterVM { get; }
    public DuplicateFinderViewModel DuplicateFinderVM { get; }
    public FolderAnalyzerViewModel FolderAnalyzerVM { get; }
    public SafeCleanupViewModel SafeCleanupVM { get; }
    public ScanHistoryViewModel ScanHistoryVM { get; }
    public ReportsViewModel ReportsVM { get; }
    public SettingsViewModel SettingsVM { get; }
    public AboutViewModel AboutVM { get; }
    public UpdateViewModel UpdateVM { get; }

    public MainViewModel(
        DashboardViewModel dashboardVM,
        StorageScanViewModel storageScanVM,
        BigFileHunterViewModel bigFileHunterVM,
        DuplicateFinderViewModel duplicateFinderVM,
        FolderAnalyzerViewModel folderAnalyzerVM,
        SafeCleanupViewModel safeCleanupVM,
        ScanHistoryViewModel scanHistoryVM,
        ReportsViewModel reportsVM,
        SettingsViewModel settingsVM,
        AboutViewModel aboutVM,
        UpdateViewModel updateVM)
    {
        DashboardVM = dashboardVM;
        DashboardVM.NavigateToAction = (view, target) => NavigateWithTarget(view, target);

        StorageScanVM = storageScanVM;
        StorageScanVM.OnScanComplete = (files, largest, duration) =>
        {
            DashboardVM.UpdateSessionStats(files, largest, duration);
        };

        BigFileHunterVM = bigFileHunterVM;
        DuplicateFinderVM = duplicateFinderVM;
        FolderAnalyzerVM = folderAnalyzerVM;
        SafeCleanupVM = safeCleanupVM;
        ScanHistoryVM = scanHistoryVM;
        ReportsVM = reportsVM;
        SettingsVM = settingsVM;
        AboutVM = aboutVM;
        UpdateVM = updateVM;

        // Default view
        CurrentViewModel = DashboardVM;
        CurrentViewName = "Dashboard";
        _ = DashboardVM.RefreshDrivesAsync();
    }

    [RelayCommand]
    public void Navigate(string viewName)
    {
        CurrentViewName = viewName;
        CurrentViewModel = viewName switch
        {
            "Dashboard" => DashboardVM,
            "StorageScan" => StorageScanVM,
            "BigFiles" => BigFileHunterVM,
            "Duplicates" => DuplicateFinderVM,
            "FolderAnalyzer" => FolderAnalyzerVM,
            "SafeCleanup" => SafeCleanupVM,
            "History" => ScanHistoryVM,
            "Reports" => ReportsVM,
            "Settings" => SettingsVM,
            "About" => AboutVM,
            "Update" => UpdateVM,
            _ => DashboardVM
        };

        if (viewName == "Dashboard")
        {
            _ = DashboardVM.RefreshDrivesAsync();
        }
        else if (viewName == "History")
        {
            _ = ScanHistoryVM.LoadHistoryAsync();
        }
        else if (viewName == "Reports")
        {
            ReportsVM.RefreshReports();
        }
    }

    public void NavigateWithTarget(string viewName, string? targetPath)
    {
        Navigate(viewName);
        if (!string.IsNullOrEmpty(targetPath))
        {
            if (viewName == "StorageScan")
            {
                StorageScanVM.TargetPath = targetPath;
            }
            else if (viewName == "BigFiles")
            {
                BigFileHunterVM.TargetPath = targetPath;
            }
            else if (viewName == "Duplicates")
            {
                DuplicateFinderVM.TargetPath = targetPath;
            }
            else if (viewName == "FolderAnalyzer")
            {
                FolderAnalyzerVM.TargetPath = targetPath;
            }
        }
    }

    [RelayCommand]
    public void MinimizeWindow(Window? window)
    {
        if (window != null) window.WindowState = WindowState.Minimized;
    }

    [RelayCommand]
    public void MaximizeWindow(Window? window)
    {
        if (window != null)
        {
            window.WindowState = window.WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;
        }
    }

    [RelayCommand]
    public void CloseWindow(Window? window)
    {
        window?.Close();
    }
}
