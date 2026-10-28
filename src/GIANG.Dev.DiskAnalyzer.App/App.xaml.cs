using System.Net.Http;
using System.Windows;
using GIANG.Dev.DiskAnalyzer.App.ViewModels;
using GIANG.Dev.DiskAnalyzer.App.Views;
using GIANG.Dev.DiskAnalyzer.Core.Configuration;
using GIANG.Dev.DiskAnalyzer.Core.Interfaces;
using GIANG.Dev.DiskAnalyzer.Infrastructure.FileSystem;
using GIANG.Dev.DiskAnalyzer.Infrastructure.Logging;
using GIANG.Dev.DiskAnalyzer.Infrastructure.Reporting;
using GIANG.Dev.DiskAnalyzer.Infrastructure.Scanning;
using GIANG.Dev.DiskAnalyzer.Infrastructure.Services;
using GIANG.Dev.DiskAnalyzer.Infrastructure.Storage;
using GIANG.Dev.DiskAnalyzer.Infrastructure.Updates;
using GIANG.Dev.DiskAnalyzer.Infrastructure.WindowsIntegration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GIANG.Dev.DiskAnalyzer.App;

public partial class App : Application
{
    private ServiceProvider? _serviceProvider;
    private ILogger<App>? _logger;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Setup global unhandled exception handling
        SetupGlobalExceptionHandling();

        var services = new ServiceCollection();
        ConfigureServices(services);

        _serviceProvider = services.BuildServiceProvider();
        _logger = _serviceProvider.GetService<ILogger<App>>();
        _logger?.LogInformation("=== GIANG DEV DISK ANALYZER PRO KHỞI ĐỘNG ===");

        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // 1. Logging
        services.AddLogging(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Information);
            builder.AddProvider(new FileLoggerProvider());
        });

        // 2. Core & Infrastructure Services
        services.AddSingleton<RecycleBinService>();
        services.AddSingleton<IDriveService, DriveService>();
        services.AddSingleton<IStorageScanner, StorageScanner>();
        services.AddSingleton<IDuplicateFinder, DuplicateFinder>();
        services.AddSingleton<IBigFileHunter, BigFileHunter>();
        services.AddSingleton<IFolderAnalyzer, FolderAnalyzer>();
        services.AddSingleton<ISafeCleanupService, SafeCleanupService>();
        services.AddSingleton<IScanHistoryRepository, SqliteScanHistoryRepository>();
        services.AddSingleton<ISettingsService, JsonSettingsService>();
        services.AddSingleton<IReportService, ReportService>();
        services.AddSingleton<IFeatureService, FeatureService>();

        // GitHub Auto Update Services
        var updateOptions = new GitHubUpdateOptions();
        string[] searchPaths = {
            System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "github-update.json"),
            System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "github-update.json")
        };
        foreach (var cfgPath in searchPaths)
        {
            if (System.IO.File.Exists(cfgPath))
            {
                try
                {
                    var loaded = System.Text.Json.JsonSerializer.Deserialize<GitHubUpdateOptions>(
                        System.IO.File.ReadAllText(cfgPath));
                    if (loaded != null)
                    {
                        if (!string.IsNullOrWhiteSpace(loaded.GitHubOwner)) updateOptions.GitHubOwner = loaded.GitHubOwner;
                        if (!string.IsNullOrWhiteSpace(loaded.Repository)) updateOptions.Repository = loaded.Repository;
                        if (!string.IsNullOrWhiteSpace(loaded.PublicKeyBase64)) updateOptions.PublicKeyBase64 = loaded.PublicKeyBase64;
                    }
                    break;
                }
                catch { }
            }
        }
        services.AddSingleton<HttpClient>();
        services.AddSingleton(updateOptions);
        services.AddSingleton<IGitHubReleaseService, GitHubReleaseService>();
        services.AddSingleton<IVersionComparisonService, VersionComparisonService>();
        services.AddSingleton<IUpdateManifestVerifier, UpdateManifestVerifier>();
        services.AddSingleton<IUpdatePackageVerifier, UpdatePackageVerifier>();
        services.AddSingleton<IUpdateDownloadService, UpdateDownloadService>();
        services.AddSingleton<IUpdateInstallerService, UpdateInstallerService>();

        // 3. ViewModels
        services.AddSingleton<DashboardViewModel>();
        services.AddSingleton<StorageScanViewModel>();
        services.AddSingleton<BigFileHunterViewModel>();
        services.AddSingleton<DuplicateFinderViewModel>();
        services.AddSingleton<FolderAnalyzerViewModel>();
        services.AddSingleton<SafeCleanupViewModel>();
        services.AddSingleton<ScanHistoryViewModel>();
        services.AddSingleton<ReportsViewModel>();
        services.AddSingleton<SettingsViewModel>();
        services.AddSingleton<AboutViewModel>();
        services.AddSingleton<UpdateViewModel>();

        services.AddSingleton<MainViewModel>();

        // 4. Windows
        services.AddSingleton<MainWindow>();
    }

    private void SetupGlobalExceptionHandling()
    {
        DispatcherUnhandledException += (s, e) =>
        {
            _logger?.LogError(e.Exception, "Ngoại lệ giao diện Dispatcher chưa xử lý");
            MessageBox.Show(
                $"Đã xảy ra sự cố giao diện: {e.Exception.Message}\nChi tiết đã được lưu vào nhật ký chẩn đoán.",
                "Cảnh báo hệ thống",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            e.Handled = true; // Prevent application crash
        };

        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            if (e.ExceptionObject is Exception ex)
            {
                _logger?.LogCritical(ex, "Ngoại lệ AppDomain nghiêm trọng");
            }
        };

        TaskScheduler.UnobservedTaskException += (s, e) =>
        {
            _logger?.LogError(e.Exception, "Ngoại lệ Task không quan sát được");
            e.SetObserved();
        };
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _logger?.LogInformation("=== GIANG DEV DISK ANALYZER PRO KẾT THÚC PHIÊN LÀM VIỆC ===");
        _serviceProvider?.Dispose();
        base.OnExit(e);
    }
}
