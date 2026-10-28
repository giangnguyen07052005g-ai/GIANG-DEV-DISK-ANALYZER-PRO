using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GIANG.Dev.DiskAnalyzer.Core.Enums;
using GIANG.Dev.DiskAnalyzer.Core.Helpers;
using GIANG.Dev.DiskAnalyzer.Core.Interfaces;
using GIANG.Dev.DiskAnalyzer.Core.Models;
using GIANG.Dev.DiskAnalyzer.Infrastructure.WindowsIntegration;
using Microsoft.Win32;
using Path = System.IO.Path;

namespace GIANG.Dev.DiskAnalyzer.App.ViewModels;

public partial class StorageScanViewModel : ObservableObject
{
    private readonly IStorageScanner _scanner;
    private readonly IScanHistoryRepository _historyRepo;
    private readonly IReportService _reportService;
    private readonly RecycleBinService? _recycleBinService;
    public Action<long, string, TimeSpan>? OnScanComplete { get; set; }
    private CancellationTokenSource? _cts;
    private List<FileItem> _allFiles = new();

    [ObservableProperty]
    private string _targetPath = @"C:\";

    [ObservableProperty]
    private bool _includeSubdirectories = true;

    [ObservableProperty]
    private bool _isScanning;

    [ObservableProperty]
    private bool _isNotScanning = true;

    [ObservableProperty]
    private bool _isPaused;

    [ObservableProperty]
    private string _pauseResumeText = "Tạm dừng";

    [ObservableProperty]
    private string _statusMessage = "Sẵn sàng quét";

    [ObservableProperty]
    private string _currentPath = string.Empty;

    [ObservableProperty]
    private long _filesScanned;

    [ObservableProperty]
    private string _totalSizeScanned = "0 B";

    [ObservableProperty]
    private string _elapsedTime = "00:00:00";

    [ObservableProperty]
    private ObservableCollection<FileItem> _displayedFiles = new();

    [ObservableProperty]
    private ObservableCollection<FolderItem> _topFolders = new();

    [ObservableProperty]
    private FolderItem? _selectedTopFolder;

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
    private string _selectedExtension = "Tất cả";

    [ObservableProperty]
    private ObservableCollection<string> _availableExtensions = new() { "Tất cả" };

    public StorageScanViewModel(
        IStorageScanner scanner,
        IScanHistoryRepository historyRepo,
        IReportService reportService,
        RecycleBinService? recycleBinService = null,
        Action<long, string, TimeSpan>? onScanComplete = null)
    {
        _scanner = scanner;
        _historyRepo = historyRepo;
        _reportService = reportService;
        _recycleBinService = recycleBinService;
        OnScanComplete = onScanComplete;
    }

    [RelayCommand]
    public void BrowseFolder()
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Chọn thư mục hoặc ổ đĩa để phân tích",
            InitialDirectory = TargetPath
        };

        if (dialog.ShowDialog() == true)
        {
            TargetPath = dialog.FolderName;
        }
    }

    [RelayCommand]
    public async Task StartScanAsync()
    {
        if (IsScanning) return;

        if (string.IsNullOrWhiteSpace(TargetPath) || !Directory.Exists(TargetPath))
        {
            StatusMessage = "Vui lòng chọn đường dẫn hợp lệ!";
            return;
        }

        IsScanning = true;
        IsPaused = false;
        FilesScanned = 0;
        TotalSizeScanned = "0 B";
        DisplayedFiles.Clear();
        TopFolders.Clear();
        _allFiles.Clear();

        _cts = new CancellationTokenSource();
        var startTime = DateTime.Now;

        var progress = new Progress<ScanProgress>(p =>
        {
            CurrentPath = p.CurrentPath;
            FilesScanned = p.FilesScanned;
            TotalSizeScanned = p.TotalBytesFormatted;
            ElapsedTime = p.ElapsedFormatted;
            StatusMessage = p.StatusMessage;
        });

        try
        {
            var options = new ScanOptions
            {
                TargetPath = TargetPath,
                IncludeSubdirectories = IncludeSubdirectories
            };

            var files = await _scanner.ScanDirectoryAsync(TargetPath, options, progress, _cts.Token);
            _allFiles = files.ToList();

            // Populate top folders
            var folderGroups = _allFiles
                .GroupBy(f => f.DirectoryPath)
                .Select(g => new FolderItem
                {
                    FullPath = g.Key,
                    Name = Path.GetFileName(g.Key),
                    TotalSizeBytes = g.Sum(f => f.SizeBytes),
                    FileCount = g.Count()
                })
                .OrderByDescending(f => f.TotalSizeBytes)
                .Take(10)
                .ToList();

            TopFolders.Clear();
            foreach (var tf in folderGroups)
            {
                TopFolders.Add(tf);
            }

            // Populate extensions
            var exts = _allFiles
                .Select(f => f.Extension.ToLowerInvariant())
                .Where(e => !string.IsNullOrEmpty(e))
                .Distinct()
                .OrderBy(e => e)
                .ToList();

            AvailableExtensions.Clear();
            AvailableExtensions.Add("Tất cả");
            foreach (var ext in exts)
            {
                AvailableExtensions.Add(ext);
            }

            ApplyFilters();

            var duration = DateTime.Now - startTime;
            var largestFile = _allFiles.OrderByDescending(f => f.SizeBytes).FirstOrDefault();
            OnScanComplete?.Invoke(_allFiles.Count, largestFile != null ? $"{largestFile.FileName} ({largestFile.SizeFormatted})" : string.Empty, duration);

            // Record into SQLite Scan History
            await _historyRepo.AddAsync(new ScanHistoryEntry
            {
                ScanType = ScanType.FullDrive,
                TargetPath = TargetPath,
                StartTime = startTime,
                EndTime = DateTime.Now,
                DurationMs = (long)duration.TotalMilliseconds,
                TotalFiles = _allFiles.Count,
                TotalSizeBytes = _allFiles.Sum(f => f.SizeBytes),
                ErrorsCount = 0,
                SoftwareVersion = "1.0.0 Pro",
                SummaryNotes = $"Quét thành công {_allFiles.Count} files."
            });
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Quá trình quét đã được người dùng hủy.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Lỗi quét: {ex.Message}";
        }
        finally
        {
            IsScanning = false;
            IsPaused = false;
        }
    }

    [RelayCommand]
    public void TogglePause()
    {
        if (!IsScanning) return;

        if (IsPaused)
        {
            _scanner.Resume();
            IsPaused = false;
            StatusMessage = "Tiếp tục quét...";
        }
        else
        {
            _scanner.Pause();
            IsPaused = true;
            StatusMessage = "Đang tạm dừng quét";
        }
    }

    [RelayCommand]
    public void CancelScan()
    {
        if (IsScanning && _cts != null && !_cts.IsCancellationRequested)
        {
            _scanner.Resume();
            _cts.Cancel();
            StatusMessage = "Đang dừng quét...";
        }
    }

    [RelayCommand]
    public void ApplyFilters()
    {
        var query = _allFiles.AsEnumerable();

        if (SelectedTopFolder != null)
        {
            query = query.Where(f => f.DirectoryPath.StartsWith(SelectedTopFolder.FullPath, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(SearchFilter))
        {
            query = query.Where(f => f.FileName.Contains(SearchFilter, StringComparison.OrdinalIgnoreCase) ||
                                     f.FullPath.Contains(SearchFilter, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(SelectedExtension) && SelectedExtension != "Tất cả")
        {
            query = query.Where(f => string.Equals(f.Extension, SelectedExtension, StringComparison.OrdinalIgnoreCase));
        }

        DisplayedFiles.Clear();
        foreach (var item in query.OrderByDescending(f => f.SizeBytes).Take(500))
        {
            item.PropertyChanged -= Item_PropertyChanged;
            item.PropertyChanged += Item_PropertyChanged;
            DisplayedFiles.Add(item);
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
        var selected = DisplayedFiles.Where(f => f.IsSelected).ToList();
        SelectedFilesCount = selected.Count;
        SelectedFilesSizeFormatted = ByteSizeFormatter.Format(selected.Sum(f => f.SizeBytes));
    }

    [RelayCommand]
    public void SelectAll()
    {
        foreach (var file in DisplayedFiles)
        {
            file.IsSelected = true;
        }
        UpdateSelectedCount();
    }

    [RelayCommand]
    public void DeselectAll()
    {
        foreach (var file in DisplayedFiles)
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
    public void ClearTopFolderFilter()
    {
        SelectedTopFolder = null;
    }

    [RelayCommand]
    public void OpenInExplorer(FileItem? file)
    {
        if (file != null)
        {
            ShellHelper.OpenInExplorer(file.FullPath);
        }
    }

    [RelayCommand]
    public void CopyPath(FileItem? file)
    {
        if (file != null)
        {
            Clipboard.SetText(file.FullPath);
            StatusMessage = $"Đã sao chép đường dẫn: {file.FileName}";
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
        // 1. Gather files with checkbox IsSelected == true
        var targets = DisplayedFiles.Where(f => f.IsSelected).ToList();

        // 2. If no checkboxes are checked, fall back to highlighted (bôi đen) rows
        if (targets.Count == 0 && highlightedItems != null)
        {
            targets = highlightedItems.OfType<FileItem>().ToList();
        }

        // 3. If still empty, fall back to SelectedFile
        if (targets.Count == 0 && SelectedFile != null)
        {
            targets = new List<FileItem> { SelectedFile };
        }

        if (targets.Count == 0)
        {
            MessageBox.Show(
                "Vui lòng tích chọn hoặc bôi đen các file cần xóa trong bảng kết quả.",
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

        // Remove deleted items from collections
        foreach (var file in successfullyDeleted)
        {
            DisplayedFiles.Remove(file);
            _allFiles.Remove(file);
        }

        FilesScanned = _allFiles.Count;
        TotalSizeScanned = ByteSizeFormatter.Format(_allFiles.Sum(f => f.SizeBytes));
        UpdateSelectedCount();

        IsDeleting = false;
        DeleteProgressPercent = 100;

        string finalResult = $"Hoàn tất xóa {successCount:N0}/{targets.Count:N0} files • Đã giải phóng: {ByteSizeFormatter.Format(currentFreedBytes)} vào Thùng rác";
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
        if (DisplayedFiles.Count == 0) return;

        var saveDialog = new SaveFileDialog
        {
            Filter = "CSV Files (*.csv)|*.csv",
            FileName = $"ScanResult_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
        };

        if (saveDialog.ShowDialog() == true)
        {
            await _reportService.ExportCsvAsync(DisplayedFiles, "Storage Scan Result", saveDialog.FileName);
            StatusMessage = $"Đã xuất kết quả ra {Path.GetFileName(saveDialog.FileName)}";
        }
    }

    partial void OnSearchFilterChanged(string value) => ApplyFilters();
    partial void OnSelectedExtensionChanged(string value) => ApplyFilters();
    partial void OnIsScanningChanged(bool value) => IsNotScanning = !value;
    partial void OnIsPausedChanged(bool value) => PauseResumeText = value ? "Tiếp tục" : "Tạm dừng";
    partial void OnSelectedTopFolderChanged(FolderItem? value) => ApplyFilters();
}
