using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GIANG.Dev.DiskAnalyzer.Core.Enums;
using GIANG.Dev.DiskAnalyzer.Core.Interfaces;
using GIANG.Dev.DiskAnalyzer.Core.Models;

namespace GIANG.Dev.DiskAnalyzer.App.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;
    private readonly IFeatureService _featureService;

    [ObservableProperty]
    private string _theme = "CyberpunkNeon";

    [ObservableProperty]
    private string _visualEffectsLevel = "High";

    [ObservableProperty]
    private string _language = "vi-VN";

    [ObservableProperty]
    private int _maxDegreeOfParallelism = 4;

    [ObservableProperty]
    private int _maxHistoryEntries = 200;

    [ObservableProperty]
    private bool _confirmBeforeDeletion = true;

    [ObservableProperty]
    private bool _useRecycleBin = true;

    [ObservableProperty]
    private int _autoRefreshIntervalSeconds = 15;

    [ObservableProperty]
    private bool _isProTier;

    [ObservableProperty]
    private string _tierDisclaimer = string.Empty;

    [ObservableProperty]
    private ObservableCollection<string> _excludedPaths = new();

    [ObservableProperty]
    private ObservableCollection<string> _excludedExtensions = new();

    [ObservableProperty]
    private string _newExcludedPath = string.Empty;

    [ObservableProperty]
    private string _newExcludedExtension = string.Empty;

    [ObservableProperty]
    private string _statusMessage = "Cài đặt ứng dụng";

    public SettingsViewModel(ISettingsService settingsService, IFeatureService featureService)
    {
        _settingsService = settingsService;
        _featureService = featureService;

        LoadFromSettings();
    }

    private void LoadFromSettings()
    {
        var s = _settingsService.CurrentSettings;
        Theme = s.Theme;
        VisualEffectsLevel = s.VisualEffectsLevel;
        Language = s.Language;
        MaxDegreeOfParallelism = s.MaxDegreeOfParallelism;
        MaxHistoryEntries = s.MaxHistoryEntries;
        ConfirmBeforeDeletion = s.ConfirmBeforeDeletion;
        UseRecycleBin = s.UseRecycleBin;
        AutoRefreshIntervalSeconds = s.AutoRefreshIntervalSeconds;
        IsProTier = _featureService.CurrentTier == FeatureTier.Pro;
        TierDisclaimer = _featureService.TierDisclaimer;

        ExcludedPaths.Clear();
        foreach (var p in s.ExcludedPaths) ExcludedPaths.Add(p);

        ExcludedExtensions.Clear();
        foreach (var e in s.ExcludedExtensions) ExcludedExtensions.Add(e);
    }

    [RelayCommand]
    public async Task SaveSettingsAsync()
    {
        var s = new AppSettings
        {
            Theme = Theme,
            VisualEffectsLevel = VisualEffectsLevel,
            Language = Language,
            MaxDegreeOfParallelism = MaxDegreeOfParallelism,
            MaxHistoryEntries = MaxHistoryEntries,
            ConfirmBeforeDeletion = ConfirmBeforeDeletion,
            UseRecycleBin = UseRecycleBin,
            AutoRefreshIntervalSeconds = AutoRefreshIntervalSeconds,
            ActiveTier = IsProTier ? FeatureTier.Pro : FeatureTier.Free,
            ExcludedPaths = ExcludedPaths.ToList(),
            ExcludedExtensions = ExcludedExtensions.ToList()
        };

        _featureService.CurrentTier = s.ActiveTier;
        await _settingsService.SaveSettingsAsync(s);
        StatusMessage = "Đã lưu toàn bộ cài đặt thành công!";

        MessageBox.Show(
            "Cài đặt đã được lưu thành công vào hệ thống.",
            "Lưu cài đặt",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    [RelayCommand]
    public async Task RestoreDefaultsAsync()
    {
        var confirm = MessageBox.Show(
            "Bạn có chắc muốn khôi phục toàn bộ cài đặt về mặc định ban đầu?",
            "Khôi phục mặc định",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (confirm == MessageBoxResult.Yes)
        {
            await _settingsService.RestoreDefaultsAsync();
            LoadFromSettings();
            StatusMessage = "Đã khôi phục cài đặt mặc định.";
        }
    }

    [RelayCommand]
    public void BrowseExcludedFolder()
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog
        {
            Title = "Chọn thư mục muốn loại trừ khỏi quét"
        };
        if (dialog.ShowDialog() == true)
        {
            NewExcludedPath = dialog.FolderName;
        }
    }

    [RelayCommand]
    public void AddExcludedPath()
    {
        if (!string.IsNullOrWhiteSpace(NewExcludedPath) && !ExcludedPaths.Contains(NewExcludedPath))
        {
            ExcludedPaths.Add(NewExcludedPath.Trim());
            NewExcludedPath = string.Empty;
        }
    }

    [RelayCommand]
    public void RemoveExcludedPath(string? path)
    {
        if (!string.IsNullOrEmpty(path)) ExcludedPaths.Remove(path);
    }

    [RelayCommand]
    public void AddExcludedExtension()
    {
        if (!string.IsNullOrWhiteSpace(NewExcludedExtension))
        {
            string ext = NewExcludedExtension.Trim();
            if (!ext.StartsWith(".")) ext = "." + ext;
            if (!ExcludedExtensions.Contains(ext))
            {
                ExcludedExtensions.Add(ext);
                NewExcludedExtension = string.Empty;
            }
        }
    }

    [RelayCommand]
    public void RemoveExcludedExtension(string? ext)
    {
        if (!string.IsNullOrEmpty(ext)) ExcludedExtensions.Remove(ext);
    }
}
