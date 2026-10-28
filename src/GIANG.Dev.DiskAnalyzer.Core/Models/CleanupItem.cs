using System.ComponentModel;
using GIANG.Dev.DiskAnalyzer.Core.Enums;
using GIANG.Dev.DiskAnalyzer.Core.Helpers;

namespace GIANG.Dev.DiskAnalyzer.Core.Models;

public class CleanupItem : INotifyPropertyChanged
{
    private bool _isSelected = true;

    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string FullPath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public CleanupCategory Category { get; set; }
    public RiskLevel RiskLevel { get; set; }

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected != value)
            {
                _isSelected = value;
                OnPropertyChanged(nameof(IsSelected));
            }
        }
    }

    public bool Exists { get; set; } = true;
    public string Description { get; set; } = string.Empty;
    public DateTime LastModifiedAt { get; set; }

    public string SizeFormatted => ByteSizeFormatter.Format(SizeBytes);
    public string LastModifiedFormatted => LastModifiedAt.ToString("yyyy-MM-dd HH:mm");

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged(string propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    public string CategoryFormatted => Category switch
    {
        CleanupCategory.UserTemp => "File tạm người dùng (User Temp)",
        CleanupCategory.WindowsTemp => "File tạm hệ thống (Windows Temp)",
        CleanupCategory.ThumbnailsCache => "Cache hình thu nhỏ (Thumbnails)",
        CleanupCategory.ApplicationCache => "Cache ứng dụng (App Cache)",
        CleanupCategory.LogFiles => "File nhật ký cũ (.log)",
        _ => "Tùy chỉnh"
    };

    public string RiskFormatted => RiskLevel switch
    {
        RiskLevel.Safe => "Tuyệt đối an toàn",
        RiskLevel.Low => "Rủi ro thấp",
        RiskLevel.Medium => "Cần lưu ý",
        RiskLevel.High => "Rủi ro cao",
        _ => "Chưa xác định"
    };
}

public class CleanupResult
{
    public int TotalScannedCount { get; set; }
    public int ProcessedCount { get; set; }
    public int SuccessCount { get; set; }
    public int FailedCount { get; set; }
    public int SkippedCount { get; set; }
    public long TotalBytesFreed { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> SuccessItems { get; set; } = new();

    public string TotalFreedFormatted => ByteSizeFormatter.Format(TotalBytesFreed);
}
