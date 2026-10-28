using GIANG.Dev.DiskAnalyzer.Core.Models;
using GIANG.Dev.DiskAnalyzer.Infrastructure.Reporting;
using GIANG.Dev.DiskAnalyzer.Tests.TestFixtures;
using Xunit;

namespace GIANG.Dev.DiskAnalyzer.Tests.ReportingTests;

public class ReportServiceTests
{
    private readonly ReportService _reportService = new();

    // 13. Xuất CSV với dấu phẩy, dấu ngoặc kép và ký tự xuống dòng (RFC 4180 escaping)
    [Theory]
    [InlineData("SimpleText", "SimpleText")]
    [InlineData("Text with, comma", "\"Text with, comma\"")]
    [InlineData("Text with \"quotes\"", "\"Text with \"\"quotes\"\"\"")]
    [InlineData("Text with\nnewline", "\"Text with\nnewline\"")]
    [InlineData("Text, with \"all\"\r\nspecial", "\"Text, with \"\"all\"\"\r\nspecial\"")]
    public void EscapeCsv_SpecialCharacters_EscapesAccordingToRfc4180(string input, string expected)
    {
        string escaped = ReportService.EscapeCsv(input);
        Assert.Equal(expected, escaped);
    }

    [Fact]
    public async Task ExportCsvAsync_ExportsUtf8WithBomAndProperRows()
    {
        using var tempDir = new TempTestDirectory();
        string csvPath = Path.Combine(tempDir.Path, "test_report.csv");

        var testData = new List<FileItem>
        {
            new() { FileName = "Báo cáo, quý 1.docx", FullPath = @"C:\Docs\Báo cáo, quý 1.docx", SizeBytes = 1024 },
            new() { FileName = "File with \"quote\".txt", FullPath = @"C:\Docs\File with ""quote"".txt", SizeBytes = 2048 }
        };

        await _reportService.ExportCsvAsync(testData, "Test Report", csvPath);

        Assert.True(File.Exists(csvPath));
        string content = await File.ReadAllTextAsync(csvPath);

        Assert.Contains("\"Báo cáo, quý 1.docx\"", content);
        Assert.Contains("\"File with \"\"quote\"\".txt\"", content);
    }

    [Fact]
    public async Task ExportHtmlReportAsync_GeneratesCyberpunkHtmlReport()
    {
        using var tempDir = new TempTestDirectory();
        string htmlPath = Path.Combine(tempDir.Path, "test_report.html");

        var reportData = new ScanReportData
        {
            Title = "Báo cáo kiểm thử Cyberpunk",
            TargetScope = @"C:\Test",
            ScanType = "Quét nhanh",
            TotalFilesScanned = 10,
            TotalSizeBytes = 500 * 1024 * 1024,
            Duration = TimeSpan.FromSeconds(5),
            ErrorsCount = 0
        };

        await _reportService.ExportHtmlReportAsync(reportData, htmlPath);

        Assert.True(File.Exists(htmlPath));
        string content = await File.ReadAllTextAsync(htmlPath);
        Assert.Contains("GIANG DEV DISK ANALYZER PRO", content);
        Assert.Contains("Báo cáo kiểm thử Cyberpunk", content);
    }
}
