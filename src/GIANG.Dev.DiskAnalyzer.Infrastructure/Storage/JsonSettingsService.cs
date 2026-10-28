using System.Text.Json;
using GIANG.Dev.DiskAnalyzer.Core.Interfaces;
using GIANG.Dev.DiskAnalyzer.Core.Models;
using Microsoft.Extensions.Logging;

namespace GIANG.Dev.DiskAnalyzer.Infrastructure.Storage;

public class JsonSettingsService : ISettingsService
{
    private readonly string _settingsFilePath;
    private readonly ILogger<JsonSettingsService>? _logger;
    private AppSettings _currentSettings = new();
    private readonly SemaphoreSlim _fileLock = new(1, 1);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public JsonSettingsService(string? customPath = null, ILogger<JsonSettingsService>? logger = null)
    {
        _logger = logger;
        if (string.IsNullOrWhiteSpace(customPath))
        {
            string appData = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "GiangDevDiskAnalyzer");
            Directory.CreateDirectory(appData);
            _settingsFilePath = Path.Combine(appData, "settings.json");
        }
        else
        {
            _settingsFilePath = customPath;
        }

        // Synchronously load existing or defaults on init
        try
        {
            if (File.Exists(_settingsFilePath))
            {
                string json = File.ReadAllText(_settingsFilePath);
                _currentSettings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
            }
            else
            {
                _currentSettings = new AppSettings();
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Không thể đọc file cấu hình, dùng cấu hình mặc định.");
            _currentSettings = new AppSettings();
        }
    }

    public AppSettings CurrentSettings => _currentSettings;

    public async Task<AppSettings> LoadSettingsAsync()
    {
        await _fileLock.WaitAsync();
        try
        {
            if (File.Exists(_settingsFilePath))
            {
                string json = await File.ReadAllTextAsync(_settingsFilePath);
                _currentSettings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
            }
            else
            {
                _currentSettings = new AppSettings();
                await SaveSettingsInternalAsync(_currentSettings);
            }
            return _currentSettings;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Lỗi đọc file cấu hình tại {Path}", _settingsFilePath);
            _currentSettings = new AppSettings();
            return _currentSettings;
        }
        finally
        {
            _fileLock.Release();
        }
    }

    public async Task SaveSettingsAsync(AppSettings settings)
    {
        await _fileLock.WaitAsync();
        try
        {
            _currentSettings = settings;
            await SaveSettingsInternalAsync(settings);
        }
        finally
        {
            _fileLock.Release();
        }
    }

    public async Task RestoreDefaultsAsync()
    {
        await _fileLock.WaitAsync();
        try
        {
            _currentSettings = new AppSettings();
            await SaveSettingsInternalAsync(_currentSettings);
        }
        finally
        {
            _fileLock.Release();
        }
    }

    private async Task SaveSettingsInternalAsync(AppSettings settings)
    {
        try
        {
            string dir = Path.GetDirectoryName(_settingsFilePath) ?? string.Empty;
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

            string json = JsonSerializer.Serialize(settings, JsonOptions);
            await File.WriteAllTextAsync(_settingsFilePath, json);
            _logger?.LogInformation("Đã lưu cài đặt ứng dụng vào {Path}", _settingsFilePath);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Không thể ghi cài đặt vào {Path}", _settingsFilePath);
        }
    }
}
