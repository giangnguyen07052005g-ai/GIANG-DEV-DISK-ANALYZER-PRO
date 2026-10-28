using System.Text.RegularExpressions;
using GIANG.Dev.DiskAnalyzer.Core.Interfaces;

namespace GIANG.Dev.DiskAnalyzer.Infrastructure.Updates;

/// <summary>
/// Triển khai chuẩn so sánh phiên bản Semantic Versioning (SemVer 2.0).
/// Chống lỗi hạ cấp phiên bản (anti-downgrade) và xử lý linh hoạt các tiền tố 'v' hoặc hậu tố pre-release.
/// </summary>
public class VersionComparisonService : IVersionComparisonService
{
    private static readonly Regex SemVerRegex = new(
        @"^v?(?<major>\d+)(?:\.(?<minor>\d+))?(?:\.(?<patch>\d+))?(?:\.(?<build>\d+))?(?:-(?<prerelease>[0-9A-Za-z.-]+))?(?:\+(?<buildmetadata>[0-9A-Za-z.-]+))?$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public bool IsNewerVersion(string currentVersion, string candidateVersion)
    {
        if (string.IsNullOrWhiteSpace(currentVersion) || string.IsNullOrWhiteSpace(candidateVersion))
            return false;

        int comparison = Compare(candidateVersion, currentVersion);
        // Chỉ chấp nhận bản mới hơn (strictly greater); bằng hoặc nhỏ hơn đều bị từ chối
        return comparison > 0;
    }

    public int Compare(string versionA, string versionB)
    {
        var (parsedA, segsA, preA) = Parse(versionA);
        var (parsedB, segsB, preB) = Parse(versionB);

        if (!parsedA && !parsedB) return 0;
        if (!parsedA) return -1;
        if (!parsedB) return 1;

        // So sánh các phân đoạn số (Major.Minor.Patch.Build)
        for (int i = 0; i < 4; i++)
        {
            if (segsA[i] > segsB[i]) return 1;
            if (segsA[i] < segsB[i]) return -1;
        }

        // Nếu các số bằng nhau, kiểm tra pre-release: Bản không có pre-release có độ ưu tiên cao hơn bản có pre-release
        // Ví dụ: 1.0.0 > 1.0.0-beta
        if (string.IsNullOrEmpty(preA) && !string.IsNullOrEmpty(preB)) return 1;
        if (!string.IsNullOrEmpty(preA) && string.IsNullOrEmpty(preB)) return -1;

        if (!string.IsNullOrEmpty(preA) && !string.IsNullOrEmpty(preB))
        {
            return string.Compare(preA, preB, StringComparison.OrdinalIgnoreCase);
        }

        return 0;
    }

    public bool IsValidVersion(string version)
    {
        if (string.IsNullOrWhiteSpace(version)) return false;
        return SemVerRegex.IsMatch(version.Trim());
    }

    public string Normalize(string version)
    {
        if (string.IsNullOrWhiteSpace(version)) return "0.0.0";
        string trimmed = version.Trim();
        if (trimmed.StartsWith("v", StringComparison.OrdinalIgnoreCase))
        {
            trimmed = trimmed[1..];
        }
        return trimmed;
    }

    private static (bool Success, int[] Segments, string PreRelease) Parse(string version)
    {
        var segs = new int[4]; // Major, Minor, Patch, Build
        if (string.IsNullOrWhiteSpace(version)) return (false, segs, string.Empty);

        var match = SemVerRegex.Match(version.Trim());
        if (!match.Success) return (false, segs, string.Empty);

        int.TryParse(match.Groups["major"].Value, out segs[0]);
        if (match.Groups["minor"].Success) int.TryParse(match.Groups["minor"].Value, out segs[1]);
        if (match.Groups["patch"].Success) int.TryParse(match.Groups["patch"].Value, out segs[2]);
        if (match.Groups["build"].Success) int.TryParse(match.Groups["build"].Value, out segs[3]);

        string preRelease = match.Groups["prerelease"].Success ? match.Groups["prerelease"].Value : string.Empty;
        return (true, segs, preRelease);
    }
}
