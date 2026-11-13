using System;
using System.IO;
using System.Threading.Tasks;
using BlueDoc.Core.Models;
using BlueDoc.Infrastructure.Reporting;

namespace BlueDoc.Application.Services
{
    public class ReportService
    {
        private readonly ReportGenerator _generator;

        public ReportService(ReportGenerator generator = null)
        {
            _generator = generator ?? new ReportGenerator();
        }

        public string GeneratePlainText(DiagnosticReport report)
        {
            return _generator.GeneratePlainText(report);
        }

        public string GenerateHtml(DiagnosticReport report)
        {
            return _generator.GenerateHtml(report);
        }

        public string GenerateJson(DiagnosticReport report)
        {
            return _generator.GenerateJson(report);
        }

        public byte[] GeneratePdf(DiagnosticReport report)
        {
            return _generator.GeneratePdf(report);
        }

        public async Task<string> SaveReportToFileAsync(DiagnosticReport report, string filePath, string format = "html")
        {
            string dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            string fmt = format.ToLowerInvariant();
            if (fmt == "pdf")
            {
                byte[] pdfBytes = GeneratePdf(report);
                await Task.Run(() => File.WriteAllBytes(filePath, pdfBytes));
                return filePath;
            }

            string content;
            switch (fmt)
            {
                case "txt":
                case "text":
                    content = GeneratePlainText(report);
                    break;
                case "json":
                    content = GenerateJson(report);
                    break;
                case "html":
                default:
                    content = GenerateHtml(report);
                    break;
            }

            using (var writer = new StreamWriter(filePath, false, System.Text.Encoding.UTF8))
            {
                await writer.WriteAsync(content);
            }

            return filePath;
        }
    }
}
