using System.Net.Http;
using GIANG.Dev.DiskAnalyzer.App.ViewModels;
using GIANG.Dev.DiskAnalyzer.App.Views;
using GIANG.Dev.DiskAnalyzer.Core.Configuration;
using GIANG.Dev.DiskAnalyzer.Infrastructure.FileSystem;
using GIANG.Dev.DiskAnalyzer.Infrastructure.Reporting;
using GIANG.Dev.DiskAnalyzer.Infrastructure.Scanning;
using GIANG.Dev.DiskAnalyzer.Infrastructure.Services;
using GIANG.Dev.DiskAnalyzer.Infrastructure.Storage;
using GIANG.Dev.DiskAnalyzer.Infrastructure.Updates;
using GIANG.Dev.DiskAnalyzer.Infrastructure.WindowsIntegration;
using Xunit;

namespace GIANG.Dev.DiskAnalyzer.Tests;

public class MainWindowUITests
{
    [Fact]
    public void AllViews_CanBeConstructedOnStaThread_WithResources()
    {
        Exception? caughtException = null;

        var thread = new Thread(() =>
        {
            try
            {
                if (System.Windows.Application.Current == null)
                {
                    var app = new GIANG.Dev.DiskAnalyzer.App.App();
                    app.InitializeComponent();
                }

                var driveService = new DriveService();
                var scanner = new StorageScanner();
                var historyRepo = new SqliteScanHistoryRepository();
                var reportService = new ReportService();
                var settingsService = new JsonSettingsService();
                var featureService = new FeatureService();
                var recycleBin = new RecycleBinService();

                var dashboardVm = new DashboardViewModel(driveService);
                var scanVm = new StorageScanViewModel(scanner, historyRepo, reportService, recycleBin);
                var bigFilesVm = new BigFileHunterViewModel(new BigFileHunter(scanner), reportService, recycleBin);
                var dupVm = new DuplicateFinderViewModel(new DuplicateFinder(scanner), recycleBin, featureService);
                var folderVm = new FolderAnalyzerViewModel(new FolderAnalyzer(), featureService);
                var cleanupVm = new SafeCleanupViewModel(new SafeCleanupService(recycleBin), settingsService);
                var historyVm = new ScanHistoryViewModel(historyRepo, reportService);
                var reportsVm = new ReportsViewModel(reportService, settingsService, historyRepo);
                var settingsVm = new SettingsViewModel(settingsService, featureService);
                var aboutVm = new AboutViewModel(featureService);

                var updateOptions = new GitHubUpdateOptions();
                var versionService = new VersionComparisonService();
                var updateVm = new UpdateViewModel(
                    new GitHubReleaseService(new HttpClient(), updateOptions),
                    versionService,
                    new UpdateManifestVerifier(updateOptions, versionService),
                    new UpdatePackageVerifier(updateOptions),
                    new UpdateDownloadService(new HttpClient(), updateOptions),
                    new UpdateInstallerService(),
                    updateOptions);

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

                // Test MainWindow
                var window = new MainWindow(mainVm);
                Assert.NotNull(window);
                Assert.Equal("GIANG DEV DISK ANALYZER PRO", window.Title);

                // Test all UserControls
                var dView = new DashboardView { DataContext = dashboardVm };
                Assert.NotNull(dView);

                var sView = new StorageScanView { DataContext = scanVm };
                Assert.NotNull(sView);

                var bView = new BigFileHunterView { DataContext = bigFilesVm };
                Assert.NotNull(bView);

                var dupView = new DuplicateFinderView { DataContext = dupVm };
                Assert.NotNull(dupView);

                var fView = new FolderAnalyzerView { DataContext = folderVm };
                Assert.NotNull(fView);

                var cView = new SafeCleanupView { DataContext = cleanupVm };
                Assert.NotNull(cView);

                var hView = new ScanHistoryView { DataContext = historyVm };
                Assert.NotNull(hView);

                var rView = new ReportsView { DataContext = reportsVm };
                Assert.NotNull(rView);

                var setView = new SettingsView { DataContext = settingsVm };
                Assert.NotNull(setView);

                var aView = new AboutView { DataContext = aboutVm };
                Assert.NotNull(aView);

                var uView = new UpdateView { DataContext = updateVm };
                Assert.NotNull(uView);
            }
            catch (Exception ex)
            {
                caughtException = ex;
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join(10000);

        if (caughtException != null)
        {
            throw new Exception("Lỗi khởi tạo UI Views: " + caughtException.ToString(), caughtException);
        }
    }
}
