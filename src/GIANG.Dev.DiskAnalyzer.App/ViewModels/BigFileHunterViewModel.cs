using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GIANG.Dev.DiskAnalyzer.Core.Helpers;
using GIANG.Dev.DiskAnalyzer.Core.Interfaces;
using GIANG.Dev.DiskAnalyzer.Core.Models;
using GIANG.Dev.DiskAnalyzer.Infrastructure.WindowsIntegration;
using Microsoft.Win32;
using Path = System.IO.Path;

namespace GIANG.Dev.DiskAnalyzer.App.ViewModels;

public partial class BigFileHunterViewModel : ObservableObject
{
    private readonly IBigFileHunter _hunter;
    private readonly IReportService _reportService;
    private readonly RecycleBinService? _recycleBinService;
    private CancellationTokenSource? _cts;
    private List<FileItem> _rawBigFiles = new();

    [ObservableProperty]
    private string _targetPath = @"C:\";

    [ObservableProperty]
    private long _thresholdBytes = 100L * 1024 * 1024; // 100MB default

    [ObservableProperty]
    private int _selectedThresholdIndex = 0; // 0: 100MB, 1: 500MB, 2: 1GB, 3: Custom

    [ObservableProperty]
    private long _customThresholdMb = 250;

    [ObservableProperty]
    private bool _isHunting;

    [ObservableProperty]
    private string _statusMessage = "Sẵn sàng săn tìm file lớn";

    [ObservableProperty]
    private string _currentProgressPath = string.Empty;

    [ObservableProperty]
    private ObservableCollection<FileItem> _bigFiles = new();

    [ObservableProperty]
    private FileItem? _selectedFile;

    [ObservableProperty]
    private int _selectedFilesCount;

    [ObservableProperty]
    private string _selectedFilesSizeFormatted = "0 B";

    [ObservableProperty]
    private bool _isDeleting;

    [ObservableProperty]
    private double _deleteProgressPercent;

    [ObservableProperty]
    private string _deleteProgressStatus = string.Empty;

    [ObservableProperty]
    private string _searchFilter = string.Empty;

    [ObservableProperty]
    private string _extensionFilter = string.Empty;

    public BigFileHunterViewModel(
        IBigFileHunter hunter, 
        IReportService reportService, 
        RecycleBinService? recycleBinService = null)
    {
        _hunter = hunter;
        _reportService = reportService;
        _recycleBinService = recycleBinService;
    }

    [RelayCommand]
    public void BrowseFolder()
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Chọn phạm vi săn file lớn",
            InitialDirectory = TargetPath
        };

        if (dialog.ShowDialog() == true)
        {
            TargetPath = dialog.FolderName;
        }
    }

    [RelayCommand]
    public async Task StartHuntingAsync()
    {
        if (IsHunting) return;

        if (string.IsNullOrWhiteSpace(TargetPath) || !Directory.Exists(TargetPath))
        {
            StatusMessage = "Vui lòng chọn thư mục hợp lệ!";
            return;
        }

        long minBytes = SelectedThresholdIndex switch
        {
            0 => 100L * 1024 * 1024,
            1 => 500L * 1024 * 1024,
            2 => 1024L * 1024 * 1024,
            3 => Math.Max(1, CustomThresholdMb) * 1024 * 1024,
            _ => 100L * 1024 * 1024
        };

        ThresholdBytes = minBytes;
        IsHunting = true;
        BigFiles.Clear();
        _rawBigFiles.Clear();
        _cts = new CancellationTokenSource();

        var progress = new Progress<ScanProgress>(p =>
        {
            CurrentProgressPath = p.CurrentPath;
            StatusMessage = p.StatusMessage;
        });

        try
        {
            var results = await _hunter.FindBigFilesAsync(TargetPath, minBytes, null, progress, _cts.Token);
            _rawBigFiles = results.ToList();
            ApplyFilters();
            StatusMessage = $"Tìm thấy {_rawBigFiles.Count} file vượt ngưỡng {ByteSizeFormatter.Format(minBytes)}.";
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Đã dừng săn file lớn.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Lỗi: {ex.Message}";
        }
        finally
        {
            IsHunting = false;
        }
    }

    [RelayCommand]
    public void CancelHunting()
    {
        _cts?.Cancel();
    }

    [RelayCommand]
    public void ApplyFilters()
    {
        var query = _rawBigFiles.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SearchFilter))
        {
            query = query.Where(f => f.FileName.Contains(SearchFilter, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(ExtensionFilter))
        {
            string ext = ExtensionFilter.StartsWith(".") ? ExtensionFilter : "." + ExtensionFilter;
            query = query.Where(f => string.Equals(f.Extension, ext, StringComparison.OrdinalIgnoreCase));
        }

        BigFiles.Clear();
        foreach (var item in query.Take(500))
        {
            item.PropertyChanged -= Item_PropertyChanged;
            item.PropertyChanged += Item_PropertyChanged;
            BigFiles.Add(item);
        }
        UpdateSelectedCount();
    }

    private void Item_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(FileItem.IsSelected))
        {
            UpdateSelectedCount();
        }
    }

    public void UpdateSelectedCount()
    {
        var selected = BigFiles.Where(f => f.IsSelected).ToList();
        SelectedFilesCount = selected.Count;
        SelectedFilesSizeFormatted = ByteSizeFormatter.Format(selected.Sum(f => f.SizeBytes));
    }

    [RelayCommand]
    public void SelectAll()
    {
        foreach (var file in BigFiles)
        {
            file.IsSelected = true;
        }
        UpdateSelectedCount();
    }

    [RelayCommand]
    public void DeselectAll()
    {
        foreach (var file in BigFiles)
        {
            file.IsSelected = false;
        }
        UpdateSelectedCount();
    }

    [RelayCommand]
    public void MarkHighlighted(System.Collections.IList? highlightedItems)
    {
        if (highlightedItems == null) return;
        foreach (var item in highlightedItems.OfType<FileItem>())
        {
            item.IsSelected = true;
        }
        UpdateSelectedCount();
    }

    [RelayCommand]
    public void UnmarkHighlighted(System.Collections.IList? highlightedItems)
    {
        if (highlightedItems == null) return;
        foreach (var item in highlightedItems.OfType<FileItem>())
        {
            item.IsSelected = false;
        }
        UpdateSelectedCount();
    }

    [RelayCommand]
    public void OpenInExplorer(FileItem? file)
    {
        if (file != null) ShellHelper.OpenInExplorer(file.FullPath);
    }

    [RelayCommand]
    public void CopyPath(FileItem? file)
    {
        if (file != null)
        {
            Clipboard.SetText(file.FullPath);
            StatusMessage = $"Đã sao chép: {file.FileName}";
        }
    }

    [RelayCommand]
    public async Task DeleteFileAsync(FileItem? file)
    {
        if (file == null) return;
        await DeleteSelectedFilesAsync(new List<FileItem> { file });
    }

    [RelayCommand]
    public async Task DeleteSelectedFilesAsync(System.Collections.IList? highlightedItems)
    {
        var targets = BigFiles.Where(f => f.IsSelected).ToList();

        if (targets.Count == 0 && highlightedItems != null)
        {
            targets = highlightedItems.OfType<FileItem>().ToList();
        }

        if (targets.Count == 0 && SelectedFile != null)
        {
            targets = new List<FileItem> { SelectedFile };
        }

        if (targets.Count == 0)
        {
            MessageBox.Show(
                "Vui lòng tích chọn hoặc bôi đen các file cần xóa trong danh sách file lớn.",
                "Chưa chọn file",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        // Direct deletion without confirmation dialog ("núc xóa ko cần phải hỏi")
        IsDeleting = true;
        DeleteProgressPercent = 0;
        int currentIndex = 0;
        int successCount = 0;
        int failedCount = 0;
        long currentFreedBytes = 0;
        var errorDetails = new List<string>();
        var successfullyDeleted = new List<FileItem>();

        await Task.Run(() =>
        {
            foreach (var file in targets)
            {
                currentIndex++;
                string err = string.Empty;
                bool ok = _recycleBinService != null && _recycleBinService.SendToRecycleBin(file.FullPath, out err);

                if (ok)
                {
                    successCount++;
                    currentFreedBytes += file.SizeBytes;
                    successfullyDeleted.Add(file);
                }
                else
                {
                    failedCount++;
                    if (errorDetails.Count < 5 && !string.IsNullOrEmpty(err))
                    {
                        errorDetails.Add($"• {file.FileName}: {err}");
                    }
                }

                double pct = (double)currentIndex / targets.Count * 100.0;
                string status = $"Đang xóa ({currentIndex}/{targets.Count}) [{(int)pct}%]: {file.FileName} • Đã giải phóng: {ByteSizeFormatter.Format(currentFreedBytes)}";

                Application.Current?.Dispatcher?.InvokeAsync(() =>
                {
                    DeleteProgressPercent = pct;
                    DeleteProgressStatus = status;
                    StatusMessage = status;
                }, System.Windows.Threading.DispatcherPriority.Background);
            }
        });

        foreach (var file in successfullyDeleted)
        {
            BigFiles.Remove(file);
            _rawBigFiles.Remove(file);
        }

        UpdateSelectedCount();

        IsDeleting = false;
        DeleteProgressPercent = 100;

        string finalResult = $"Hoàn tất xóa {successCount:N0}/{targets.Count:N0} files lớn • Đã giải phóng: {ByteSizeFormatter.Format(currentFreedBytes)} vào Thùng rác";
        if (failedCount > 0)
        {
            finalResult += $" ({failedCount} file thất bại do đang bị khóa)";
        }
        StatusMessage = finalResult;
        DeleteProgressStatus = finalResult;
    }

    [RelayCommand]
    public async Task ExportCsvAsync()
    {
        if (BigFiles.Count == 0) return;

        var saveDialog = new SaveFileDialog
        {
            Filter = "CSV Files (*.csv)|*.csv",
            FileName = $"BigFiles_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
        };

        if (saveDialog.ShowDialog() == true)
        {
            await _reportService.ExportCsvAsync(BigFiles, "Big File Hunter Results", saveDialog.FileName);
            StatusMessage = $"Đã xuất CSV ra {Path.GetFileName(saveDialog.FileName)}";
        }
    }

    partial void OnSearchFilterChanged(string value) => ApplyFilters();
    partial void OnExtensionFilterChanged(string value) => ApplyFilters();
    partial void OnSelectedThresholdIndexChanged(int value)
    {
        if (value == 0) ThresholdBytes = 100L * 1024 * 1024;
        else if (value == 1) ThresholdBytes = 500L * 1024 * 1024;
        else if (value == 2) ThresholdBytes = 1024L * 1024 * 1024;
        else if (value == 3) ThresholdBytes = CustomThresholdMb * 1024 * 1024;
    }
}
