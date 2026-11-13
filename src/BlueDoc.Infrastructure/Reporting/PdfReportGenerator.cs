using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using BlueDoc.Core.Enums;
using BlueDoc.Core.Models;

namespace BlueDoc.Infrastructure.Reporting
{
    public class PdfReportGenerator
    {
        private class PdfPage
        {
            public StringBuilder Content { get; } = new StringBuilder();
        }

        public byte[] GeneratePdf(DiagnosticReport report)
        {
            var pages = new List<PdfPage>();
            var currentPage = new PdfPage();
            pages.Add(currentPage);

            double pageWidth = 612;
            double pageHeight = 792;
            double marginLeft = 40;
            double contentWidth = pageWidth - (marginLeft * 2); // 532 pt
            double currentY = pageHeight - 50;

            Action startNewPageIfNeeded = () =>
            {
                if (currentY < 65)
                {
                    currentPage = new PdfPage();
                    pages.Add(currentPage);
                    currentY = pageHeight - 50;
                }
            };

            // Cover Header Banner
            AppendRect(currentPage.Content, marginLeft, currentY - 50, contentWidth, 50, 0.06, 0.10, 0.16); // Dark Navy
            AppendText(currentPage.Content, "/F2", 18, marginLeft + 14, currentY - 26, "BlueDoc Diagnostic Report", 0.22, 0.74, 0.97); // Cyan
            AppendText(currentPage.Content, "/F1", 10, marginLeft + 14, currentY - 42, "A Bluetooth Doctor for Windows • UNLEFT LLC • Golam Morshed Eashan", 0.58, 0.64, 0.72);

            // Overall Status Badge on top right
            string overall = report.Session?.OverallStatus.ToString().ToUpperInvariant() ?? "UNKNOWN";
            double badgeWidth = 90;
            double badgeX = marginLeft + contentWidth - badgeWidth - 14;
            if (overall.Contains("PASS"))
            {
                AppendRect(currentPage.Content, badgeX, currentY - 38, badgeWidth, 24, 0.09, 0.40, 0.20);
                AppendText(currentPage.Content, "/F2", 11, badgeX + 16, currentY - 26, "PASSED", 0.52, 0.94, 0.67);
            }
            else if (overall.Contains("WARN"))
            {
                AppendRect(currentPage.Content, badgeX, currentY - 38, badgeWidth, 24, 0.52, 0.30, 0.05);
                AppendText(currentPage.Content, "/F2", 11, badgeX + 14, currentY - 26, "WARNING", 0.98, 0.88, 0.28);
            }
            else
            {
                AppendRect(currentPage.Content, badgeX, currentY - 38, badgeWidth, 24, 0.60, 0.11, 0.11);
                AppendText(currentPage.Content, "/F2", 11, badgeX + 18, currentY - 26, "FAILED", 0.99, 0.65, 0.65);
            }

            currentY -= 65;

            // Metadata summary
            AppendText(currentPage.Content, "/F2", 9, marginLeft, currentY, "AUDIT METADATA:", 0.22, 0.74, 0.97);
            currentY -= 14;
            string meta1 = $"Report ID: {report.ReportId}   •   Generated: {report.GeneratedAtUtc:yyyy-MM-dd HH:mm:ss} UTC";
            string meta2 = $"Machine: {report.MachineName}   •   OS: {report.OsVersion}";
            AppendText(currentPage.Content, "/F1", 9, marginLeft, currentY, meta1, 0.2, 0.2, 0.2);
            currentY -= 12;
            AppendText(currentPage.Content, "/F1", 9, marginLeft, currentY, meta2, 0.2, 0.2, 0.2);
            currentY -= 16;
            AppendLine(currentPage.Content, marginLeft, currentY, marginLeft + contentWidth, currentY, 0.8, 0.8, 0.8);
            currentY -= 18;

            // Section 1: Hardware & Driver
            AppendText(currentPage.Content, "/F2", 12, marginLeft, currentY, "1. HARDWARE ADAPTER & DRIVER SPECIFICATIONS", 0.08, 0.15, 0.27);
            currentY -= 14;

            if (report.Adapter != null)
            {
                var props = new List<Tuple<string, string>>
                {
                    Tuple.Create("Adapter Name", report.Adapter.FriendlyName ?? "Unknown"),
                    Tuple.Create("Manufacturer", report.Adapter.Manufacturer ?? "Standard Controller"),
                    Tuple.Create("Hardware ID", report.Adapter.HardwareId ?? "Not Exposed"),
                    Tuple.Create("Device Status", report.Adapter.StatusDisplay ?? "Operational"),
                    Tuple.Create("Driver Version", $"{report.Adapter.Driver?.DriverVersion} ({report.Adapter.Driver?.DriverDate})"),
                    Tuple.Create("Driver Provider", report.Adapter.Driver?.DriverProvider ?? "Unknown")
                };

                foreach (var p in props)
                {
                    AppendText(currentPage.Content, "/F2", 9, marginLeft + 6, currentY, p.Item1 + ":", 0.3, 0.3, 0.3);
                    AppendText(currentPage.Content, "/F1", 9, marginLeft + 120, currentY, Truncate(p.Item2, 70), 0.1, 0.1, 0.1);
                    currentY -= 13;
                }
            }
            else
            {
                AppendText(currentPage.Content, "/F1", 9, marginLeft + 6, currentY, "No physical or USB Bluetooth adapter detected on this system.", 0.6, 0.1, 0.1);
                currentY -= 14;
            }

            currentY -= 10;
            AppendLine(currentPage.Content, marginLeft, currentY, marginLeft + contentWidth, currentY, 0.8, 0.8, 0.8);
            currentY -= 18;

            // Section 2: Windows Service Status
            AppendText(currentPage.Content, "/F2", 12, marginLeft, currentY, "2. CORE WINDOWS BLUETOOTH SERVICES", 0.08, 0.15, 0.27);
            currentY -= 14;

            if (report.Service != null)
            {
                AppendText(currentPage.Content, "/F2", 9, marginLeft + 6, currentY, "Bluetooth Support Service:", 0.3, 0.3, 0.3);
                AppendText(currentPage.Content, "/F1", 9, marginLeft + 150, currentY, $"{report.Service.ServiceName} (Status: {report.Service.Status}, Startup: {report.Service.StartupType})", 0.1, 0.1, 0.1);
                currentY -= 14;
            }

            currentY -= 10;
            AppendLine(currentPage.Content, marginLeft, currentY, marginLeft + contentWidth, currentY, 0.8, 0.8, 0.8);
            currentY -= 18;

            // Section 3: Diagnostic Tests (All 12 Tests)
            AppendText(currentPage.Content, "/F2", 12, marginLeft, currentY, "3. DIAGNOSTIC SUITE EVALUATION RESULTS", 0.08, 0.15, 0.27);
            currentY -= 16;

            if (report.Session?.Results != null && report.Session.Results.Count > 0)
            {
                foreach (var r in report.Session.Results)
                {
                    startNewPageIfNeeded();

                    // Status pill
                    string statusStr = r.Status.ToString().ToUpperInvariant();
                    if (statusStr == "PASSED")
                    {
                        AppendRect(currentPage.Content, marginLeft, currentY - 10, 60, 13, 0.09, 0.40, 0.20);
                        AppendText(currentPage.Content, "/F2", 8, marginLeft + 10, currentY - 2, "PASSED", 1, 1, 1);
                    }
                    else if (statusStr == "WARNING")
                    {
                        AppendRect(currentPage.Content, marginLeft, currentY - 10, 60, 13, 0.60, 0.40, 0.05);
                        AppendText(currentPage.Content, "/F2", 8, marginLeft + 8, currentY - 2, "WARNING", 1, 1, 1);
                    }
                    else if (statusStr == "FAILED")
                    {
                        AppendRect(currentPage.Content, marginLeft, currentY - 10, 60, 13, 0.70, 0.10, 0.10);
                        AppendText(currentPage.Content, "/F2", 8, marginLeft + 12, currentY - 2, "FAILED", 1, 1, 1);
                    }
                    else
                    {
                        AppendRect(currentPage.Content, marginLeft, currentY - 10, 60, 13, 0.40, 0.45, 0.50);
                        AppendText(currentPage.Content, "/F2", 8, marginLeft + 4, currentY - 2, statusStr, 1, 1, 1);
                    }

                    // Test Name
                    AppendText(currentPage.Content, "/F2", 9, marginLeft + 68, currentY - 2, r.TestName, 0.1, 0.1, 0.1);
                    currentY -= 15;

                    // Summary & Recommendation
                    if (!string.IsNullOrWhiteSpace(r.Summary))
                    {
                        AppendText(currentPage.Content, "/F1", 8, marginLeft + 15, currentY, "Summary: " + Truncate(r.Summary, 95), 0.25, 0.25, 0.25);
                        currentY -= 11;
                    }
                    if (!string.IsNullOrWhiteSpace(r.RecommendedAction))
                    {
                        AppendText(currentPage.Content, "/F1", 8, marginLeft + 15, currentY, "Action:  " + Truncate(r.RecommendedAction, 95), 0.1, 0.4, 0.1);
                        currentY -= 11;
                    }

                    currentY -= 4;
                }
            }
            else
            {
                AppendText(currentPage.Content, "/F1", 9, marginLeft + 6, currentY, "No diagnostic rules evaluated in this session.", 0.4, 0.4, 0.4);
                currentY -= 14;
            }

            currentY -= 10;
            AppendLine(currentPage.Content, marginLeft, currentY, marginLeft + contentWidth, currentY, 0.8, 0.8, 0.8);
            currentY -= 18;

            // Section 4: Relevant Windows System Events
            startNewPageIfNeeded();
            AppendText(currentPage.Content, "/F2", 12, marginLeft, currentY, "4. HISTORICAL WINDOWS SYSTEM EVENT TELEMETRY", 0.08, 0.15, 0.27);
            currentY -= 16;

            if (report.RelevantEvents != null && report.RelevantEvents.Count > 0)
            {
                int maxEvt = Math.Min(report.RelevantEvents.Count, 10);
                for (int i = 0; i < maxEvt; i++)
                {
                    startNewPageIfNeeded();
                    var evt = report.RelevantEvents[i];
                    string evtHeader = $"[{evt.LocalTimeDisplay}] [{evt.LevelDisplayName.ToUpperInvariant()}] {evt.ProviderName} (ID {evt.EventId})";
                    if (evt.OccurrenceCount > 1) evtHeader += $" x{evt.OccurrenceCount}";

                    AppendText(currentPage.Content, "/F2", 8, marginLeft + 6, currentY, evtHeader, 0.15, 0.15, 0.15);
                    currentY -= 11;

                    if (!string.IsNullOrWhiteSpace(evt.HumanAnalysis))
                    {
                        AppendText(currentPage.Content, "/F1", 8, marginLeft + 14, currentY, Truncate(evt.HumanAnalysis, 95), 0.3, 0.3, 0.3);
                        currentY -= 11;
                    }

                    currentY -= 3;
                }
            }
            else
            {
                AppendText(currentPage.Content, "/F1", 9, marginLeft + 6, currentY, "No abnormal Bluetooth system event records identified in event log.", 0.2, 0.5, 0.2);
                currentY -= 14;
            }

            currentY -= 10;
            AppendLine(currentPage.Content, marginLeft, currentY, marginLeft + contentWidth, currentY, 0.8, 0.8, 0.8);
            currentY -= 18;

            // Section 5: Discovered / Paired Devices
            startNewPageIfNeeded();
            AppendText(currentPage.Content, "/F2", 12, marginLeft, currentY, "5. BLUETOOTH PERIPHERAL INVENTORY", 0.08, 0.15, 0.27);
            currentY -= 16;

            if (report.Devices != null && report.Devices.Count > 0)
            {
                foreach (var d in report.Devices)
                {
                    startNewPageIfNeeded();
                    string devLine = $"• {d.DisplayName}   MAC: {d.BluetoothAddress}   Category: {d.CategoryName}   Status: {d.StatusDisplay}";
                    AppendText(currentPage.Content, "/F1", 8, marginLeft + 6, currentY, Truncate(devLine, 95), 0.1, 0.1, 0.1);
                    currentY -= 12;
                }
            }
            else
            {
                AppendText(currentPage.Content, "/F1", 9, marginLeft + 6, currentY, "No registered or discovered Bluetooth devices found.", 0.4, 0.4, 0.4);
                currentY -= 14;
            }

            // Running Page Footers on all pages
            int totalPages = pages.Count;
            for (int p = 0; p < totalPages; p++)
            {
                var page = pages[p];
                AppendLine(page.Content, marginLeft, 35, marginLeft + contentWidth, 35, 0.85, 0.85, 0.85);
                AppendText(page.Content, "/F1", 8, marginLeft, 25, "BlueDoc v1.0 • UNLEFT LLC • Confidential System Diagnostics Audit", 0.5, 0.5, 0.5);
                AppendText(page.Content, "/F1", 8, marginLeft + contentWidth - 60, 25, $"Page {p + 1} of {totalPages}", 0.5, 0.5, 0.5);
            }

            return BuildPdfBinary(pages, pageWidth, pageHeight);
        }

        private static void AppendRect(StringBuilder sb, double x, double y, double w, double h, double r, double g, double b)
        {
            sb.AppendFormat(System.Globalization.CultureInfo.InvariantCulture, "{0:F2} {1:F2} {2:F2} rg\n", r, g, b);
            sb.AppendFormat(System.Globalization.CultureInfo.InvariantCulture, "{0:F2} {1:F2} {2:F2} {3:F2} re f\n", x, y, w, h);
        }

        private static void AppendLine(StringBuilder sb, double x1, double y1, double x2, double y2, double r, double g, double b, double lineWidth = 0.5)
        {
            sb.AppendFormat(System.Globalization.CultureInfo.InvariantCulture, "{0:F2} {1:F2} {2:F2} RG\n", r, g, b);
            sb.AppendFormat(System.Globalization.CultureInfo.InvariantCulture, "{0:F2} w\n", lineWidth);
            sb.AppendFormat(System.Globalization.CultureInfo.InvariantCulture, "{0:F2} {1:F2} m {2:F2} {3:F2} l S\n", x1, y1, x2, y2);
        }

        private static void AppendText(StringBuilder sb, string fontKey, double size, double x, double y, string text, double r, double g, double b)
        {
            if (string.IsNullOrEmpty(text)) return;
            string escaped = EscapePdfText(text);

            sb.Append("BT\n");
            sb.AppendFormat(System.Globalization.CultureInfo.InvariantCulture, "{0} {1:F2} Tf\n", fontKey, size);
            sb.AppendFormat(System.Globalization.CultureInfo.InvariantCulture, "{0:F2} {1:F2} {2:F2} rg\n", r, g, b);
            sb.AppendFormat(System.Globalization.CultureInfo.InvariantCulture, "{0:F2} {1:F2} Td\n", x, y);
            sb.AppendFormat("({0}) Tj\n", escaped);
            sb.Append("ET\n");
        }

        private static string EscapePdfText(string text)
        {
            var sb = new StringBuilder();
            foreach (char c in text)
            {
                if (c == '\\') sb.Append("\\\\");
                else if (c == '(') sb.Append("\\(");
                else if (c == ')') sb.Append("\\)");
                else if (c >= 32 && c <= 126) sb.Append(c);
                else if (c == '•') sb.Append("-");
                else sb.Append(' ');
            }
            return sb.ToString();
        }

        private static string Truncate(string text, int maxChars)
        {
            if (string.IsNullOrEmpty(text)) return "";
            if (text.Length <= maxChars) return text;
            return text.Substring(0, maxChars - 3) + "...";
        }

        private static byte[] BuildPdfBinary(List<PdfPage> pages, double pageWidth, double pageHeight)
        {
            using (var ms = new MemoryStream())
            using (var writer = new StreamWriter(ms, Encoding.ASCII))
            {
                var offsets = new List<long>();

                // 1. PDF Header
                writer.Write("%PDF-1.4\r\n%\xE2\xE3\xCF\xD3\r\n");
                writer.Flush();

                // Object 1: Catalog
                offsets.Add(ms.Position);
                writer.Write("1 0 obj\r\n<< /Type /Catalog /Pages 2 0 R >>\r\nendobj\r\n");
                writer.Flush();

                // Object 2: Pages root
                int pageCount = pages.Count;
                var kidsBuilder = new StringBuilder();
                for (int i = 0; i < pageCount; i++)
                {
                    kidsBuilder.AppendFormat("{0} 0 R ", 3 + (i * 2));
                }

                offsets.Add(ms.Position);
                writer.Write($"2 0 obj\r\n<< /Type /Pages /Kids [{kidsBuilder.ToString().Trim()}] /Count {pageCount} >>\r\nendobj\r\n");
                writer.Flush();

                // Fonts:
                // Font F1 (Helvetica)
                int fontF1Obj = 3 + (pageCount * 2);
                int fontF2Obj = fontF1Obj + 1;

                // Page objects and streams
                for (int i = 0; i < pageCount; i++)
                {
                    int pageObjNum = 3 + (i * 2);
                    int contentObjNum = pageObjNum + 1;

                    offsets.Add(ms.Position);
                    writer.Write($"{pageObjNum} 0 obj\r\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {pageWidth:F0} {pageHeight:F0}] /Contents {contentObjNum} 0 R /Resources << /Font << /F1 {fontF1Obj} 0 R /F2 {fontF2Obj} 0 R >> >> >>\r\nendobj\r\n");
                    writer.Flush();

                    byte[] contentBytes = Encoding.ASCII.GetBytes(pages[i].Content.ToString());
                    offsets.Add(ms.Position);
                    writer.Write($"{contentObjNum} 0 obj\r\n<< /Length {contentBytes.Length} >>\r\nstream\r\n");
                    writer.Flush();

                    ms.Write(contentBytes, 0, contentBytes.Length);
                    writer.Write("\r\nendstream\r\nendobj\r\n");
                    writer.Flush();
                }

                // Font Objects
                offsets.Add(ms.Position);
                writer.Write($"{fontF1Obj} 0 obj\r\n<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>\r\nendobj\r\n");
                writer.Flush();

                offsets.Add(ms.Position);
                writer.Write($"{fontF2Obj} 0 obj\r\n<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold >>\r\nendobj\r\n");
                writer.Flush();

                // Cross-reference table
                long xrefOffset = ms.Position;
                int totalObjects = offsets.Count + 1;
                writer.Write($"xref\r\n0 {totalObjects}\r\n0000000000 65535 f \r\n");
                foreach (var off in offsets)
                {
                    writer.Write($"{off:D10} 00000 n \r\n");
                }

                // Trailer
                writer.Write($"trailer\r\n<< /Size {totalObjects} /Root 1 0 R >>\r\nstartxref\r\n{xrefOffset}\r\n%%EOF\r\n");
                writer.Flush();

                return ms.ToArray();
            }
        }
    }
}

