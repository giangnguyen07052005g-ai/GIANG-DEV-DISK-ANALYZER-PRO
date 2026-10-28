using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GIANG.Dev.DiskAnalyzer.Core.Interfaces;
using GIANG.Dev.DiskAnalyzer.Core.Models;
using GIANG.Dev.DiskAnalyzer.Infrastructure.WindowsIntegration;
using Microsoft.Win32;
using Path = System.IO.Path;

namespace GIANG.Dev.DiskAnalyzer.App.ViewModels;

public partial class ReportsViewModel : ObservableObject
{
    private readonly IReportService _reportService;
    private readonly ISettingsService _settingsService;
    private readonly IScanHistoryRepository _historyRepo;

    [ObservableProperty]
    private string _exportDirectory = string.Empty;

    [ObservableProperty]
    private string _statusMessage = "Sẵn sàng tạo và xuất báo cáo phân tích ổ đĩa";

    [ObservableProperty]
    private ObservableCollection<string> _generatedReports = new();

    [ObservableProperty]
    private string? _selectedReportFile;

    public ReportsViewModel(
        IReportService reportService,
        ISettingsService settingsService,
        IScanHistoryRepository historyRepo)
    {
        _reportService = reportService;
        _settingsService = settingsService;
        _historyRepo = historyRepo;

        string defaultReportsDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "GiangDevDiskAnalyzer", "Reports");
        Directory.CreateDirectory(defaultReportsDir);
        _exportDirectory = defaultReportsDir;

        RefreshReports();
    }

    [RelayCommand]
    public void RefreshReports()
    {
        GeneratedReports.Clear();
        if (Directory.Exists(ExportDirectory))
        {
            var files = Directory.GetFiles(ExportDirectory, "*.*")
                .Where(f => f.EndsWith(".html", StringComparison.OrdinalIgnoreCase) ||
                            f.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(f => File.GetCreationTime(f));

            foreach (var file in files)
            {
                GeneratedReports.Add(file);
            }
        }
    }

    [RelayCommand]
    public void BrowseExportDirectory()
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Chọn thư mục lưu báo cáo",
            InitialDirectory = ExportDirectory
        };

        if (dialog.ShowDialog() == true)
        {
            ExportDirectory = dialog.FolderName;
            RefreshReports();
        }
    }

    [RelayCommand]
    public async Task GenerateHtmlReportAsync()
    {
        try
        {
            var history = await _historyRepo.GetAllAsync(50);
            var latest = history.FirstOrDefault();

            var reportData = new ScanReportData
            {
                Title = "Báo cáo tổng hợp dung lượng - GIANG DEV DISK ANALYZER PRO",
                TargetScope = latest?.TargetPath ?? @"C:\",
                ScanType = latest?.ScanTypeFormatted ?? "Toàn diện hệ thống",
                TotalFilesScanned = latest?.TotalFiles ?? 0,
                TotalSizeBytes = latest?.TotalSizeBytes ?? 0,
                Duration = latest != null ? TimeSpan.FromMilliseconds(latest.DurationMs) : TimeSpan.FromSeconds(15),
                ErrorsCount = latest?.ErrorsCount ?? 0
            };

            string filename = $"DiskReport_{DateTime.Now:yyyyMMdd_HHmmss}.html";
            string outputPath = Path.Combine(ExportDirectory, filename);

            await _reportService.ExportHtmlReportAsync(reportData, outputPath);
            GeneratedReports.Insert(0, outputPath);
            SelectedReportFile = outputPath;
            StatusMessage = $"Đã tạo thành công báo cáo HTML: {filename}";

            // Open in browser
            ShellHelper.OpenFileWithDefaultApp(outputPath);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Lỗi tạo báo cáo: {ex.Message}";
        }
    }

    [RelayCommand]
    public void OpenReport(string? filePath)
    {
        if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
        {
            ShellHelper.OpenFileWithDefaultApp(filePath);
        }
    }

    [RelayCommand]
    public void OpenReportsFolder()
    {
        if (Directory.Exists(ExportDirectory))
        {
            ShellHelper.OpenInExplorer(ExportDirectory);
        }
    }
}
