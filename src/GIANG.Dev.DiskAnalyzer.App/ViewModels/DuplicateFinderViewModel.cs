using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GIANG.Dev.DiskAnalyzer.Core.Helpers;
using GIANG.Dev.DiskAnalyzer.Core.Interfaces;
using GIANG.Dev.DiskAnalyzer.Core.Models;
using GIANG.Dev.DiskAnalyzer.Infrastructure.WindowsIntegration;
using Path = System.IO.Path;

namespace GIANG.Dev.DiskAnalyzer.App.ViewModels;

public partial class DuplicateFinderViewModel : ObservableObject
{
    private readonly IDuplicateFinder _duplicateFinder;
    private readonly RecycleBinService _recycleBinService;
    private readonly IFeatureService _featureService;
    private CancellationTokenSource? _cts;

    [ObservableProperty]
    private string _targetPath = @"C:\";

    [ObservableProperty]
    private bool _isScanning;

    [ObservableProperty]
    private string _statusMessage = "Sẵn sàng tìm kiếm file trùng lặp nội dung";

    [ObservableProperty]
    private double _progressPercent;

    [ObservableProperty]
    private string _currentProgressPath = string.Empty;

    [ObservableProperty]
    private ObservableCollection<DuplicateGroup> _duplicateGroups = new();

    [ObservableProperty]
    private DuplicateGroup? _selectedGroup;

    [ObservableProperty]
    private long _totalPotentialSavingsBytes;

    [ObservableProperty]
    private string _totalPotentialSavingsFormatted = "0 B";

    [ObservableProperty]
    private int _totalDuplicateFilesCount;

    public DuplicateFinderViewModel(
        IDuplicateFinder duplicateFinder,
        RecycleBinService recycleBinService,
        IFeatureService featureService)
    {
        _duplicateFinder = duplicateFinder;
        _recycleBinService = recycleBinService;
        _featureService = featureService;
    }

    [RelayCommand]
    public void BrowseFolder()
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog
        {
            Title = "Chọn thư mục tìm file trùng lặp",
            InitialDirectory = TargetPath
        };

        if (dialog.ShowDialog() == true)
        {
            TargetPath = dialog.FolderName;
        }
    }

    [RelayCommand]
    public async Task StartSearchAsync()
    {
        if (IsScanning) return;

        if (string.IsNullOrWhiteSpace(TargetPath) || !Directory.Exists(TargetPath))
        {
            StatusMessage = "Vui lòng chọn thư mục hợp lệ!";
            return;
        }

        IsScanning = true;
        ProgressPercent = 0;
        DuplicateGroups.Clear();
        TotalPotentialSavingsBytes = 0;
        TotalPotentialSavingsFormatted = "0 B";
        TotalDuplicateFilesCount = 0;

        _cts = new CancellationTokenSource();

        var progress = new Progress<ScanProgress>(p =>
        {
            CurrentProgressPath = p.CurrentPath;
            StatusMessage = p.StatusMessage;
            if (p.EstimatedPercent.HasValue)
            {
                ProgressPercent = p.EstimatedPercent.Value;
            }
        });

        try
        {
            int maxResults = _featureService.GetLimit("MaxDuplicateResults");
            var options = new DuplicateScanOptions
            {
                TargetPath = TargetPath,
                FastHashFirst = true,
                VerifyByteByByte = true,
                MaxResults = maxResults
            };

            var results = await _duplicateFinder.FindDuplicatesAsync(TargetPath, options, progress, _cts.Token);

            foreach (var group in results)
            {
                DuplicateGroups.Add(group);
            }

            TotalPotentialSavingsBytes = DuplicateGroups.Sum(g => g.WastedSizeBytes);
            TotalPotentialSavingsFormatted = ByteSizeFormatter.Format(TotalPotentialSavingsBytes);
            TotalDuplicateFilesCount = DuplicateGroups.Sum(g => g.FileCount);

            StatusMessage = $"Phát hiện {DuplicateGroups.Count} nhóm trùng lặp ({TotalDuplicateFilesCount} file). Tiết kiệm tối đa: {TotalPotentialSavingsFormatted}.";
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Đã dừng tìm file trùng.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Lỗi: {ex.Message}";
        }
        finally
        {
            IsScanning = false;
        }
    }

    [RelayCommand]
    public void CancelSearch()
    {
        _cts?.Cancel();
    }

    [RelayCommand]
    public void KeepOldest()
    {
        foreach (var group in DuplicateGroups)
        {
            group.MarkKeepOldest();
        }
        RefreshView();
    }

    [RelayCommand]
    public void KeepNewest()
    {
        foreach (var group in DuplicateGroups)
        {
            group.MarkKeepNewest();
        }
        RefreshView();
    }

    [RelayCommand]
    public void ClearSelection()
    {
        foreach (var group in DuplicateGroups)
        {
            group.ClearSelection();
        }
        RefreshView();
    }

    [RelayCommand]
    public void SelectAllInGroup()
    {
        if (SelectedGroup != null)
        {
            SelectedGroup.MarkKeepOldest();
            RefreshView();
        }
    }

    [RelayCommand]
    public void DeselectInGroup()
    {
        if (SelectedGroup != null)
        {
            SelectedGroup.ClearSelection();
            RefreshView();
        }
    }

    [RelayCommand]
    public void MarkHighlighted(System.Collections.IList? highlightedItems)
    {
        if (highlightedItems == null) return;
        foreach (var item in highlightedItems.OfType<FileItem>())
        {
            item.IsSelected = true;
        }
    }

    [RelayCommand]
    public void UnmarkHighlighted(System.Collections.IList? highlightedItems)
    {
        if (highlightedItems == null) return;
        foreach (var item in highlightedItems.OfType<FileItem>())
        {
            item.IsSelected = false;
        }
    }

    private void RefreshView()
    {
        // Trigger observable update
        var current = SelectedGroup;
        SelectedGroup = null;
        SelectedGroup = (current != null && DuplicateGroups.Contains(current)) ? current : DuplicateGroups.FirstOrDefault();
    }

    [RelayCommand]
    public void OpenInExplorer(FileItem? file)
    {
        if (file != null) ShellHelper.OpenInExplorer(file.FullPath);
    }

    [RelayCommand]
    public async Task DeleteSelectedDuplicatesAsync(System.Collections.IList? highlightedItems = null)
    {
        var selectedFiles = DuplicateGroups
            .SelectMany(g => g.Files)
            .Where(f => f.IsSelected)
            .ToList();

        // If no checkboxes checked, check if rows are highlighted (bôi đen) in the active DataGrid
        if (selectedFiles.Count == 0 && highlightedItems != null)
        {
            var highlighted = highlightedItems.OfType<FileItem>().ToList();
            if (highlighted.Count > 0)
            {
                foreach (var h in highlighted)
                {
                    h.IsSelected = true;
                }
                selectedFiles = highlighted;
            }
        }

        if (selectedFiles.Count == 0)
        {
            MessageBox.Show(
                "Chưa có file trùng lặp nào được đánh dấu chọn để xóa.\nVui lòng tích chọn các bản sao cần xóa, bôi đen các dòng hoặc dùng nút 'Giữ file cũ nhất/mới nhất'.",
                "Chưa chọn file",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        // Safety check: ensure we are NOT deleting ALL files in any group!
        foreach (var group in DuplicateGroups)
        {
            if (group.Files.Count > 0 && group.Files.All(f => f.IsSelected))
            {
                MessageBox.Show(
                    $"Cảnh báo an toàn: Nhóm kích thước {group.SizeFormatted} đang bị chọn xóa TẤT CẢ bản sao!\n" +
                    "Hệ thống yêu cầu phải giữ lại ít nhất một bản gốc. Vui lòng bỏ chọn ít nhất một file trong nhóm.",
                    "Vi phạm nguyên tắc an toàn",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }
        }

        // Direct delete without confirmation ("núc xóa ko cần phải hỏi")
        int success = 0;
        int failed = 0;
        int currentIndex = 0;
        long freedBytes = 0;
        var errorDetails = new List<string>();

        await Task.Run(() =>
        {
            foreach (var f in selectedFiles)
            {
                currentIndex++;
                string err = string.Empty;
                bool ok = _recycleBinService.SendToRecycleBin(f.FullPath, out err);
                if (ok)
                {
                    success++;
                    freedBytes += f.SizeBytes;
                }
                else
                {
                    failed++;
                    if (errorDetails.Count < 5 && !string.IsNullOrEmpty(err))
                    {
                        errorDetails.Add($"• {f.FileName}: {err}");
                    }
                }

                double pct = (double)currentIndex / selectedFiles.Count * 100.0;
                string status = $"Đang xóa bản sao ({currentIndex}/{selectedFiles.Count}) [{(int)pct}%]: {f.FileName} • Đã giải phóng: {ByteSizeFormatter.Format(freedBytes)}";

                Application.Current?.Dispatcher?.InvokeAsync(() =>
                {
                    ProgressPercent = pct;
                    StatusMessage = status;
                }, System.Windows.Threading.DispatcherPriority.Background);
            }
        });

        // Clean up deleted files from groups
        foreach (var group in DuplicateGroups)
        {
            group.Files.RemoveAll(f => f.IsSelected && !File.Exists(f.FullPath));
        }

        DuplicateGroups = new ObservableCollection<DuplicateGroup>(DuplicateGroups.Where(g => g.Files.Count > 1));
        TotalPotentialSavingsBytes = DuplicateGroups.Sum(g => g.WastedSizeBytes);
        TotalPotentialSavingsFormatted = ByteSizeFormatter.Format(TotalPotentialSavingsBytes);
        RefreshView();

        string resultMsg = $"Hoàn tất dọn {success:N0}/{selectedFiles.Count:N0} bản sao • Đã giải phóng: {ByteSizeFormatter.Format(freedBytes)}";
        if (failed > 0)
        {
            resultMsg += $" ({failed} file thất bại do đang bị khóa)";
        }
        StatusMessage = resultMsg;
    }
}
