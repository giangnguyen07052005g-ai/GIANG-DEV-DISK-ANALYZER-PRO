namespace GIANG.Dev.DiskAnalyzer.Core.Enums;

public enum ScanType
{
    FullDrive,
    Folder,
    BigFiles,
    Duplicates,
    FolderAnalyzer,
    SafeCleanup
}

public enum ScanState
{
    Idle,
    Scanning,
    Paused,
    Completed,
    Cancelled,
    Failed
}

public enum FileCategory
{
    Document,
    Image,
    Video,
    Audio,
    Archive,
    Code,
    Executable,
    System,
    Other
}

public enum RiskLevel
{
    Safe,
    Low,
    Medium,
    High
}

public enum CleanupCategory
{
    UserTemp,
    WindowsTemp,
    ThumbnailsCache,
    ApplicationCache,
    LogFiles,
    Custom
}

public enum FeatureTier
{
    Free,
    Pro
}

public enum SortOption
{
    SizeDescending,
    SizeAscending,
    NameAscending,
    NameDescending,
    DateModifiedDescending
}
