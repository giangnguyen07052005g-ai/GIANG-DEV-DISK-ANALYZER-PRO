using System.Net;
using System.Text;
using GIANG.Dev.DiskAnalyzer.Core.Helpers;
using GIANG.Dev.DiskAnalyzer.Core.Interfaces;
using GIANG.Dev.DiskAnalyzer.Core.Models;
using Microsoft.Extensions.Logging;

namespace GIANG.Dev.DiskAnalyzer.Infrastructure.Reporting;

public class ReportService : IReportService
{
    private readonly ILogger<ReportService>? _logger;

    public ReportService(ILogger<ReportService>? logger = null)
    {
        _logger = logger;
    }

    public async Task<string> ExportCsvAsync<T>(IEnumerable<T> data, string reportTitle, string outputPath)
    {
        string dir = Path.GetDirectoryName(outputPath) ?? string.Empty;
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

        var sb = new StringBuilder();

        // Write UTF-8 BOM so Excel opens Vietnamese Unicode characters correctly
        var properties = typeof(T).GetProperties();

        // Header
        sb.AppendLine(string.Join(",", properties.Select(p => EscapeCsv(p.Name))));

        // Rows
        foreach (var item in data)
        {
            if (item == null) continue;
            var values = properties.Select(p =>
            {
                var val = p.GetValue(item);
                return EscapeCsv(val?.ToString() ?? string.Empty);
            });
            sb.AppendLine(string.Join(",", values));
        }

        await File.WriteAllTextAsync(outputPath, sb.ToString(), new UTF8Encoding(true));
        _logger?.LogInformation("Xuất CSV thành công vào {Path}", outputPath);
        return outputPath;
    }

    public static string EscapeCsv(string field)
    {
        if (string.IsNullOrEmpty(field)) return "\"\"";
        if (field.Contains(',') || field.Contains('"') || field.Contains('\n') || field.Contains('\r'))
        {
            return $"\"{field.Replace("\"", "\"\"")}\"";
        }
        return field;
    }

    public static string SafeHtml(string text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        return text
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&#39;");
    }

    public async Task<string> ExportHtmlReportAsync(ScanReportData reportData, string outputPath)
    {
        string dir = Path.GetDirectoryName(outputPath) ?? string.Empty;
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"vi\">");
        sb.AppendLine("<head>");
        sb.AppendLine("  <meta charset=\"UTF-8\" />");
        sb.AppendLine("  <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\" />");
        sb.AppendLine($"  <title>{SafeHtml(reportData.Title)}</title>");
        sb.AppendLine("  <style>");
        sb.AppendLine("    :root {");
        sb.AppendLine("      --bg-dark: #0B0F19;");
        sb.AppendLine("      --card-bg: #111827;");
        sb.AppendLine("      --card-border: #1E293B;");
        sb.AppendLine("      --neon-cyan: #00F0FF;");
        sb.AppendLine("      --neon-purple: #A855F7;");
        sb.AppendLine("      --neon-green: #10B981;");
        sb.AppendLine("      --neon-red: #EF4444;");
        sb.AppendLine("      --text-main: #F8FAFC;");
        sb.AppendLine("      --text-muted: #94A3B8;");
        sb.AppendLine("    }");
        sb.AppendLine("    body {");
        sb.AppendLine("      margin: 0; padding: 24px; font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;");
        sb.AppendLine("      background-color: var(--bg-dark); color: var(--text-main); line-height: 1.6;");
        sb.AppendLine("    }");
        sb.AppendLine("    .container { max-width: 1200px; margin: 0 auto; }");
        sb.AppendLine("    .header {");
        sb.AppendLine("      border-bottom: 2px solid var(--neon-cyan); padding-bottom: 16px; margin-bottom: 28px;");
        sb.AppendLine("      display: flex; justify-content: space-between; align-items: flex-end; flex-wrap: wrap;");
        sb.AppendLine("    }");
        sb.AppendLine("    .brand { font-size: 26px; font-weight: 800; color: var(--neon-cyan); letter-spacing: 1px; }");
        sb.AppendLine("    .badge { background: rgba(0,240,255,0.15); color: var(--neon-cyan); padding: 4px 10px; border-radius: 4px; font-size: 13px; font-weight: 600; border: 1px solid var(--neon-cyan); }");
        sb.AppendLine("    .stats-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(220px, 1fr)); gap: 16px; margin-bottom: 30px; }");
        sb.AppendLine("    .card { background: var(--card-bg); border: 1px solid var(--card-border); border-radius: 8px; padding: 18px; box-shadow: 0 4px 12px rgba(0,0,0,0.4); }");
        sb.AppendLine("    .card-title { font-size: 13px; color: var(--text-muted); text-transform: uppercase; letter-spacing: 0.5px; }");
        sb.AppendLine("    .card-val { font-size: 24px; font-weight: 700; color: var(--neon-cyan); margin-top: 6px; }");
        sb.AppendLine("    .section-title { font-size: 18px; color: var(--neon-purple); border-left: 4px solid var(--neon-purple); padding-left: 10px; margin: 28px 0 14px 0; }");
        sb.AppendLine("    table { width: 100%; border-collapse: collapse; margin-bottom: 24px; background: var(--card-bg); border-radius: 8px; overflow: hidden; border: 1px solid var(--card-border); }");
        sb.AppendLine("    th { background: #1E293B; color: var(--neon-cyan); text-align: left; padding: 12px; font-size: 13px; text-transform: uppercase; }");
        sb.AppendLine("    td { padding: 10px 12px; border-bottom: 1px solid #1E293B; font-size: 13px; word-break: break-all; }");
        sb.AppendLine("    tr:hover { background: rgba(0, 240, 255, 0.05); }");
        sb.AppendLine("    .highlight { color: var(--neon-cyan); font-weight: 600; }");
        sb.AppendLine("    .footer { text-align: center; margin-top: 40px; color: var(--text-muted); font-size: 12px; border-top: 1px solid var(--card-border); padding-top: 16px; }");
        sb.AppendLine("  </style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        sb.AppendLine("  <div class=\"container\">");
        sb.AppendLine("    <div class=\"header\">");
        sb.AppendLine("      <div>");
        sb.AppendLine("        <div class=\"brand\">GIANG DEV DISK ANALYZER PRO</div>");
        sb.AppendLine($"        <div style=\"color: var(--neon-cyan); font-size: 15px; margin: 4px 0;\">{SafeHtml(reportData.Title)}</div>");
        sb.AppendLine($"        <div style=\"color: var(--text-muted); font-size: 14px;\">Mục tiêu quét: {SafeHtml(reportData.TargetScope)}</div>");
        sb.AppendLine("      </div>");
        sb.AppendLine($"      <div><span class=\"badge\">{SafeHtml(reportData.ScanType)}</span> <span style=\"color: var(--text-muted); font-size: 13px; margin-left: 10px;\">Xuất lúc: {reportData.GeneratedAt:yyyy-MM-dd HH:mm:ss}</span></div>");
        sb.AppendLine("    </div>");

        // Stats
        sb.AppendLine("    <div class=\"stats-grid\">");
        sb.AppendLine("      <div class=\"card\">");
        sb.AppendLine("        <div class=\"card-title\">Tổng dung lượng quét</div>");
        sb.AppendLine($"        <div class=\"card-val\">{SafeHtml(reportData.TotalSizeFormatted)}</div>");
        sb.AppendLine("      </div>");
        sb.AppendLine("      <div class=\"card\">");
        sb.AppendLine("        <div class=\"card-title\">Số lượng file đã xử lý</div>");
        sb.AppendLine($"        <div class=\"card-val\">{reportData.TotalFilesScanned:N0}</div>");
        sb.AppendLine("      </div>");
        sb.AppendLine("      <div class=\"card\">");
        sb.AppendLine("        <div class=\"card-title\">Thời gian thực hiện</div>");
        sb.AppendLine($"        <div class=\"card-val\" style=\"color: var(--neon-purple);\">{reportData.Duration:hh\\:mm\\:ss}</div>");
        sb.AppendLine("      </div>");
        sb.AppendLine("      <div class=\"card\">");
        sb.AppendLine("        <div class=\"card-title\">Cảnh báo & Lỗi I/O</div>");
        sb.AppendLine($"        <div class=\"card-val\" style=\"color: {(reportData.ErrorsCount > 0 ? "var(--neon-red)" : "var(--neon-green)")};\">{reportData.ErrorsCount}</div>");
        sb.AppendLine("      </div>");
        sb.AppendLine("    </div>");

        // Top Large Files Table
        if (reportData.TopFiles.Count > 0)
        {
            sb.AppendLine("    <div class=\"section-title\">Danh sách file chiếm nhiều dung lượng nhất</div>");
            sb.AppendLine("    <table>");
            sb.AppendLine("      <thead><tr><th>Tên file</th><th>Định dạng</th><th>Dung lượng</th><th>Ngày chỉnh sửa</th><th>Đường dẫn</th></tr></thead>");
            sb.AppendLine("      <tbody>");
            foreach (var f in reportData.TopFiles.Take(50))
            {
                sb.AppendLine("        <tr>");
                sb.AppendLine($"          <td class=\"highlight\">{SafeHtml(f.FileName)}</td>");
                sb.AppendLine($"          <td>{SafeHtml(f.Extension)}</td>");
                sb.AppendLine($"          <td>{SafeHtml(f.SizeFormatted)}</td>");
                sb.AppendLine($"          <td>{f.LastModifiedAt:yyyy-MM-dd HH:mm}</td>");
                sb.AppendLine($"          <td>{SafeHtml(f.FullPath)}</td>");
                sb.AppendLine("        </tr>");
            }
            sb.AppendLine("      </tbody>");
            sb.AppendLine("    </table>");
        }

        // Duplicate Groups Table
        if (reportData.DuplicateGroups.Count > 0)
        {
            sb.AppendLine("    <div class=\"section-title\">Các nhóm file trùng lặp phát hiện được</div>");
            sb.AppendLine("    <table>");
            sb.AppendLine("      <thead><tr><th>SHA-256 Hash</th><th>Kích thước mỗi file</th><th>Số lượng bản sao</th><th>Dung lượng lãng phí</th><th>Đường dẫn các bản sao</th></tr></thead>");
            sb.AppendLine("      <tbody>");
            foreach (var dg in reportData.DuplicateGroups.Take(50))
            {
                sb.AppendLine("        <tr>");
                sb.AppendLine($"          <td><code>{SafeHtml(dg.ContentHash.Substring(0, Math.Min(16, dg.ContentHash.Length)))}...</code></td>");
                sb.AppendLine($"          <td>{SafeHtml(dg.SizeFormatted)}</td>");
                sb.AppendLine($"          <td>{dg.FileCount}</td>");
                sb.AppendLine($"          <td class=\"highlight\" style=\"color: var(--neon-red);\">{SafeHtml(dg.WastedFormatted)}</td>");
                sb.AppendLine("          <td>");
                foreach (var f in dg.Files)
                {
                    sb.AppendLine($"            <div>• {SafeHtml(f.FullPath)}</div>");
                }
                sb.AppendLine("          </td>");
                sb.AppendLine("        </tr>");
            }
            sb.AppendLine("      </tbody>");
            sb.AppendLine("    </table>");
        }

        sb.AppendLine("    <div class=\"footer\">");
        sb.AppendLine("      Báo cáo được khởi tạo tự động bởi GIANG DEV DISK ANALYZER PRO • Thiết kế Cyberpunk Neon chuyên nghiệp");
        sb.AppendLine("    </div>");
        sb.AppendLine("  </div>");
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        await File.WriteAllTextAsync(outputPath, sb.ToString(), Encoding.UTF8);
        _logger?.LogInformation("Đã xuất báo cáo HTML chuyên nghiệp vào {Path}", outputPath);
        return outputPath;
    }
}
