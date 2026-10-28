using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GIANG.Dev.DiskAnalyzer.Core.Interfaces;
using GIANG.Dev.DiskAnalyzer.Core.Models;
using Microsoft.Win32;
using Path = System.IO.Path;

namespace GIANG.Dev.DiskAnalyzer.App.ViewModels;

public partial class ScanHistoryViewModel : ObservableObject
{
    private readonly IScanHistoryRepository _historyRepo;
    private readonly IReportService _reportService;

    [ObservableProperty]
    private ObservableCollection<ScanHistoryEntry> _historyEntries = new();

    [ObservableProperty]
    private ScanHistoryEntry? _selectedEntry;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusMessage = "Lịch sử quét ổ đĩa";

    public ScanHistoryViewModel(IScanHistoryRepository historyRepo, IReportService reportService)
    {
        _historyRepo = historyRepo;
        _reportService = reportService;
    }

    [RelayCommand]
    public async Task LoadHistoryAsync()
    {
        IsLoading = true;
        try
        {
            var entries = await _historyRepo.GetAllAsync(200);
            HistoryEntries.Clear();
            foreach (var e in entries)
            {
                HistoryEntries.Add(e);
            }
            StatusMessage = $"Có {HistoryEntries.Count} bản ghi lịch sử quét.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task DeleteEntryAsync(ScanHistoryEntry? entry)
    {
        if (entry == null) return;

        var confirm = MessageBox.Show(
            $"Bạn có muốn xóa bản ghi lịch sử ngày {entry.StartTimeFormatted} ({entry.TargetPath}) không?",
            "Xóa lịch sử",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (confirm == MessageBoxResult.Yes)
        {
            await _historyRepo.DeleteAsync(entry.Id);
            HistoryEntries.Remove(entry);
            StatusMessage = "Đã xóa bản ghi.";
        }
    }

    [RelayCommand]
    public async Task ClearAllAsync()
    {
        if (HistoryEntries.Count == 0) return;

        var confirm = MessageBox.Show(
            "Bạn có chắc chắn muốn xóa toàn bộ lịch sử quét không?",
            "Xóa toàn bộ",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (confirm == MessageBoxResult.Yes)
        {
            await _historyRepo.ClearAllAsync();
            HistoryEntries.Clear();
            StatusMessage = "Đã xóa toàn bộ lịch sử quét.";
        }
    }

    [RelayCommand]
    public async Task ExportCsvAsync()
    {
        if (HistoryEntries.Count == 0) return;

        var saveDialog = new SaveFileDialog
        {
            Filter = "CSV Files (*.csv)|*.csv",
            FileName = $"ScanHistory_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
        };

        if (saveDialog.ShowDialog() == true)
        {
            await _reportService.ExportCsvAsync(HistoryEntries, "Scan History", saveDialog.FileName);
            StatusMessage = $"Đã xuất lịch sử ra {Path.GetFileName(saveDialog.FileName)}";
        }
    }
}
