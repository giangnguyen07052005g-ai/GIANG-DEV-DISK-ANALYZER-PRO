using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GIANG.Dev.DiskAnalyzer.Core.Helpers;
using GIANG.Dev.DiskAnalyzer.Core.Interfaces;
using GIANG.Dev.DiskAnalyzer.Core.Models;
using GIANG.Dev.DiskAnalyzer.Infrastructure.WindowsIntegration;

namespace GIANG.Dev.DiskAnalyzer.App.ViewModels;

public partial class SafeCleanupViewModel : ObservableObject
{
    private readonly ISafeCleanupService _cleanupService;
    private readonly ISettingsService _settingsService;
    private CancellationTokenSource? _cts;

    [ObservableProperty]
    private bool _isScanning;

    [ObservableProperty]
    private bool _isCleaning;

    [ObservableProperty]
    private string _statusMessage = "Sẵn sàng quét dọn dẹp an toàn các vùng file tạm và cache";

    [ObservableProperty]
    private bool _includeUserTemp = true;

    [ObservableProperty]
    private bool _includeThumbnailsCache = true;

    [ObservableProperty]
    private bool _includeAppCache = true;

    [ObservableProperty]
    private bool _includeOldLogs = true;

    [ObservableProperty]
    private int _minAgeDays = 1;

    [ObservableProperty]
    private ObservableCollection<CleanupItem> _cleanupItems = new();

    [ObservableProperty]
    private CleanupItem? _selectedItem;

    [ObservableProperty]
    private long _totalSelectedBytes;

    [ObservableProperty]
    private string _totalSelectedFormatted = "0 B";

    [ObservableProperty]
    private int _selectedCount;

    [ObservableProperty]
    private CleanupResult? _lastResult;

    public SafeCleanupViewModel(ISafeCleanupService cleanupService, ISettingsService settingsService)
    {
        _cleanupService = cleanupService;
        _settingsService = settingsService;
    }

    [RelayCommand]
    public async Task StartScanAsync()
    {
        if (IsScanning || IsCleaning) return;

        IsScanning = true;
        CleanupItems.Clear();
        TotalSelectedBytes = 0;
        TotalSelectedFormatted = "0 B";
        SelectedCount = 0;
        LastResult = null;
        _cts = new CancellationTokenSource();

        var progress = new Progress<ScanProgress>(p =>
        {
            StatusMessage = p.StatusMessage;
        });

        try
        {
            var options = new CleanupOptions
            {
                IncludeUserTemp = IncludeUserTemp,
                IncludeThumbnailsCache = IncludeThumbnailsCache,
                IncludeAppCache = IncludeAppCache,
                IncludeOldLogs = IncludeOldLogs,
                MinimumAgeDays = MinAgeDays
            };

            var items = await _cleanupService.ScanCleanupTargetsAsync(options, progress, _cts.Token);

            foreach (var item in items)
            {
                item.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(CleanupItem.IsSelected))
                    {
                        RecalculateSelectedTotal();
                    }
                };
                CleanupItems.Add(item);
            }

            RecalculateSelectedTotal();
            StatusMessage = $"Đã quét xong: Tìm thấy {CleanupItems.Count} mục ({TotalSelectedFormatted}).";
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Đã dừng quét dọn dẹp.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Lỗi quét: {ex.Message}";
        }
        finally
        {
            IsScanning = false;
        }
    }

    [RelayCommand]
    public void SelectAll()
    {
        foreach (var item in CleanupItems)
        {
            item.IsSelected = true;
        }
        RecalculateSelectedTotal();
    }

    [RelayCommand]
    public void DeselectAll()
    {
        foreach (var item in CleanupItems)
        {
            item.IsSelected = false;
        }
        RecalculateSelectedTotal();
    }

    [RelayCommand]
    public void MarkHighlighted(System.Collections.IList? highlightedItems)
    {
        if (highlightedItems == null) return;
        foreach (var item in highlightedItems.OfType<CleanupItem>())
        {
            item.IsSelected = true;
        }
        RecalculateSelectedTotal();
    }

    [RelayCommand]
    public void UnmarkHighlighted(System.Collections.IList? highlightedItems)
    {
        if (highlightedItems == null) return;
        foreach (var item in highlightedItems.OfType<CleanupItem>())
        {
            item.IsSelected = false;
        }
        RecalculateSelectedTotal();
    }

    public void RecalculateSelectedTotal()
    {
        var selected = CleanupItems.Where(i => i.IsSelected).ToList();
        SelectedCount = selected.Count;
        TotalSelectedBytes = selected.Sum(i => i.SizeBytes);
        TotalSelectedFormatted = ByteSizeFormatter.Format(TotalSelectedBytes);
    }

    [RelayCommand]
    public void OpenInExplorer(CleanupItem? item)
    {
        if (item != null) ShellHelper.OpenInExplorer(item.FullPath);
    }

    [RelayCommand]
    public async Task ExecuteSafeCleanupAsync(System.Collections.IList? highlightedItems = null)
    {
        if (IsCleaning || IsScanning) return;

        var selected = CleanupItems.Where(i => i.IsSelected).ToList();
        if (selected.Count == 0 && highlightedItems != null)
        {
            var highlighted = highlightedItems.OfType<CleanupItem>().ToList();
            if (highlighted.Count > 0)
            {
                foreach (var h in highlighted)
                {
                    h.IsSelected = true;
                }
                selected = highlighted;
                RecalculateSelectedTotal();
            }
        }

        if (selected.Count == 0)
        {
            MessageBox.Show(
                "Không có file nào được chọn hoặc bôi đen để dọn dẹp.",
                "Thông báo",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        // Direct clean without confirmation ("núc xóa ko cần phải hỏi")
        IsCleaning = true;
        _cts = new CancellationTokenSource();

        var progress = new Progress<ScanProgress>(p =>
        {
            StatusMessage = p.StatusMessage;
        });

        try
        {
            bool useRecycleBin = _settingsService.CurrentSettings.UseRecycleBin;
            var result = await _cleanupService.ExecuteCleanupAsync(selected, useRecycleBin, progress, _cts.Token);
            LastResult = result;

            // Remove successfully deleted items from list
            var remaining = CleanupItems.Where(i => !result.SuccessItems.Contains(i.FullPath)).ToList();
            CleanupItems.Clear();
            foreach (var r in remaining)
            {
                CleanupItems.Add(r);
            }

            RecalculateSelectedTotal();

            string summaryMsg =
                $"BÁO CÁO KẾT QUẢ DỌN DẸP:\n\n" +
                $"• Thành công: {result.SuccessCount:N0} files\n" +
                $"• Thất bại / bị khóa: {result.FailedCount:N0} files\n" +
                $"• Bỏ qua (không tồn tại): {result.SkippedCount:N0} files\n" +
                $"• Tổng dung lượng đã giải phóng: {result.TotalFreedFormatted}\n\n" +
                $"Chi tiết kết quả đã được ghi vào nhật ký hệ thống.";

            MessageBox.Show(
                summaryMsg,
                "Hoàn tất dọn dẹp",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Quá trình dọn dẹp đã bị dừng.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Lỗi xử lý: {ex.Message}";
        }
        finally
        {
            IsCleaning = false;
        }
    }

    [RelayCommand]
    public void CancelAction()
    {
        _cts?.Cancel();
    }
}
