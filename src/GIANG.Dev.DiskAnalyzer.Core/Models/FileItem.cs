using System.ComponentModel;
using GIANG.Dev.DiskAnalyzer.Core.Enums;
using GIANG.Dev.DiskAnalyzer.Core.Helpers;

namespace GIANG.Dev.DiskAnalyzer.Core.Models;

public class FileItem : INotifyPropertyChanged
{
    private bool _isSelected;

    public string FullPath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string Extension { get; set; } = string.Empty;
    public string DirectoryPath { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastModifiedAt { get; set; }
    public DateTime LastAccessedAt { get; set; }
    public FileCategory Category { get; set; }
    public bool IsReadOnly { get; set; }
    public bool IsHidden { get; set; }
    public bool IsSystem { get; set; }

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

    public string SizeFormatted => ByteSizeFormatter.Format(SizeBytes);
    public string LastModifiedFormatted => LastModifiedAt.ToString("yyyy-MM-dd HH:mm:ss");

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged(string propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    public static FileCategory CategorizeExtension(string ext)
    {
        if (string.IsNullOrEmpty(ext)) return FileCategory.Other;
        ext = ext.ToLowerInvariant();

        return ext switch
        {
            ".doc" or ".docx" or ".pdf" or ".txt" or ".xls" or ".xlsx" or ".ppt" or ".pptx" or ".csv" or ".rtf" => FileCategory.Document,
            ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp" or ".webp" or ".svg" or ".ico" or ".tiff" => FileCategory.Image,
            ".mp4" or ".mkv" or ".avi" or ".mov" or ".wmv" or ".flv" or ".webm" or ".m4v" => FileCategory.Video,
            ".mp3" or ".wav" or ".flac" or ".aac" or ".ogg" or ".m4a" or ".wma" => FileCategory.Audio,
            ".zip" or ".rar" or ".7z" or ".tar" or ".gz" or ".iso" or ".bz2" or ".xz" => FileCategory.Archive,
            ".cs" or ".js" or ".ts" or ".html" or ".css" or ".json" or ".xml" or ".py" or ".cpp" or ".h" or ".java" or ".sql" => FileCategory.Code,
            ".exe" or ".msi" or ".dll" or ".sys" or ".bat" or ".cmd" or ".ps1" => FileCategory.Executable,
            _ => FileCategory.Other
        };
    }
}
