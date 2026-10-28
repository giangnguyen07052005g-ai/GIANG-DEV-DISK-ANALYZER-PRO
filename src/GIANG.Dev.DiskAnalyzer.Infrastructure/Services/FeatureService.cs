using GIANG.Dev.DiskAnalyzer.Core.Enums;
using GIANG.Dev.DiskAnalyzer.Core.Interfaces;

namespace GIANG.Dev.DiskAnalyzer.Infrastructure.Services;

public class FeatureService : IFeatureService
{
    private FeatureTier _currentTier = FeatureTier.Pro;

    public FeatureTier CurrentTier
    {
        get => _currentTier;
        set => _currentTier = value;
    }

    public string TierDisclaimer =>
        "Phiên bản hiện tại chưa tích hợp hệ thống máy chủ bản quyền hay kích hoạt trực tuyến thương mại. " +
        "Chế độ Free/Pro hiện được quản lý ở mức cấu hình kiến trúc ứng dụng phục vụ kiểm thử và trải nghiệm đầy đủ tính năng.";

    public bool IsFeatureAllowed(string featureKey)
    {
        // Core safety features are ALWAYS allowed in both Free and Pro!
        if (featureKey is "SafeCleanup" or "RecycleBin" or "DriveDashboard" or "BigFileHunter")
        {
            return true;
        }

        if (CurrentTier == FeatureTier.Pro)
        {
            return true;
        }

        // Free tier restrictions
        return featureKey switch
        {
            "AdvancedFolderAnalyzer" => false,
            "DeepTreemapDrillDown" => false,
            "UnlimitedDuplicates" => false,
            _ => true
        };
    }

    public int GetLimit(string limitKey)
    {
        if (CurrentTier == FeatureTier.Pro)
        {
            return int.MaxValue;
        }

        // Free Tier Limits
        return limitKey switch
        {
            "MaxDuplicateResults" => 200,
            "MaxHistoryEntries" => 50,
            "MaxFolderDepth" => 3,
            _ => 1000
        };
    }
}
