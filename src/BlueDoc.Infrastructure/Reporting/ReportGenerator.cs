using System;
using System.IO;
using System.Text;
using BlueDoc.Core.Models;
using Newtonsoft.Json;

namespace BlueDoc.Infrastructure.Reporting
{
    public class ReportGenerator
    {
        private readonly PdfReportGenerator _pdfGenerator = new PdfReportGenerator();

        public byte[] GeneratePdf(DiagnosticReport report)
        {
            return _pdfGenerator.GeneratePdf(report);
        }

        public string GeneratePlainText(DiagnosticReport report)
        {
            var sb = new StringBuilder();
            sb.AppendLine("================================================================================");
            sb.AppendLine("                             BLUEDOC DIAGNOSTIC REPORT                          ");
            sb.AppendLine("                             A Bluetooth Doctor for Windows                     ");
            sb.AppendLine("                             Developed by UNLEFT LLC                            ");
            sb.AppendLine("================================================================================");
            sb.AppendLine($"Report ID:         {report.ReportId}");
            sb.AppendLine($"Generated (UTC):   {report.GeneratedAtUtc:yyyy-MM-dd HH:mm:ss} UTC");
            sb.AppendLine($"Machine Name:      {report.MachineName}");
            sb.AppendLine($"App Version:       {report.ApplicationVersion}");
            sb.AppendLine($"OS Environment:    {report.OsVersion}");
            sb.AppendLine("--------------------------------------------------------------------------------");
            sb.AppendLine();

            sb.AppendLine("[1. HARDWARE ADAPTER INFORMATION]");
            if (report.Adapter != null)
            {
                sb.AppendLine($"  Adapter Name:      {report.Adapter.FriendlyName}");
                sb.AppendLine($"  Manufacturer:      {report.Adapter.Manufacturer}");
                sb.AppendLine($"  Hardware ID:       {report.Adapter.HardwareId}");
                sb.AppendLine($"  Vendor ID:         {report.Adapter.HardwareDetails?.VendorId}");
                sb.AppendLine($"  Product ID:        {report.Adapter.HardwareDetails?.ProductId}");
                sb.AppendLine($"  Device Status:     {report.Adapter.StatusDisplay}");
                sb.AppendLine($"  Driver Provider:   {report.Adapter.Driver?.DriverProvider}");
                sb.AppendLine($"  Driver Version:    {report.Adapter.Driver?.DriverVersion}");
                sb.AppendLine($"  Driver Date:       {report.Adapter.Driver?.DriverDate}");
            }
            else
            {
                sb.AppendLine("  No active Bluetooth adapter detected.");
            }
            sb.AppendLine();

            sb.AppendLine("[2. WINDOWS SERVICE STATUS]");
            if (report.Service != null)
            {
                sb.AppendLine($"  Service Name:      {report.Service.ServiceName} ({report.Service.DisplayName})");
                sb.AppendLine($"  Service Status:    {report.Service.Status}");
                sb.AppendLine($"  Start Type:        {report.Service.StartupType}");
            }
            sb.AppendLine();

            sb.AppendLine("[3. DIAGNOSTIC SUITE RESULTS]");
            if (report.Session?.Results != null && report.Session.Results.Count > 0)
            {
                sb.AppendLine($"  Overall Session Status: {report.Session.OverallStatus.ToString().ToUpper()}");
                sb.AppendLine();
                foreach (var res in report.Session.Results)
                {
                    sb.AppendLine($"  * [{res.Status.ToString().ToUpper()}] {res.TestName}");
                    sb.AppendLine($"    Summary:         {res.Summary}");
                    if (!string.IsNullOrWhiteSpace(res.TechnicalDetails))
                        sb.AppendLine($"    Technical:       {res.TechnicalDetails}");
                    if (!string.IsNullOrWhiteSpace(res.Evidence))
                        sb.AppendLine($"    Evidence:        {res.Evidence}");
                    if (!string.IsNullOrWhiteSpace(res.PossibleCause))
                        sb.AppendLine($"    Possible Cause:  {res.PossibleCause}");
                    if (!string.IsNullOrWhiteSpace(res.RecommendedAction))
                        sb.AppendLine($"    Recommendation:  {res.RecommendedAction}");
                    sb.AppendLine();
                }
            }
            else
            {
                sb.AppendLine("  No diagnostic suite tests executed in this session.");
            }
            sb.AppendLine();

            sb.AppendLine("[4. RECENT BLUETOOTH SYSTEM EVENTS]");
            if (report.RelevantEvents != null && report.RelevantEvents.Count > 0)
            {
                foreach (var evt in report.RelevantEvents)
                {
                    sb.AppendLine($"  [{evt.LocalTimeDisplay}] [{evt.LevelDisplayName.ToUpper()}] Provider: {evt.ProviderName} (Event ID: {evt.EventId}) (Occurrences: {evt.OccurrenceCount})");
                    sb.AppendLine($"    Analysis:        {evt.HumanAnalysis}");
                    if (!string.IsNullOrWhiteSpace(evt.PossibleCause))
                        sb.AppendLine($"    Possible Cause:  {evt.PossibleCause}");
                    sb.AppendLine();
                }
            }
            else
            {
                sb.AppendLine("  No abnormal Bluetooth system event records identified.");
            }
            sb.AppendLine();

            sb.AppendLine("[5. DISCOVERED & CONNECTED DEVICES]");
            if (report.Devices != null && report.Devices.Count > 0)
            {
                foreach (var dev in report.Devices)
                {
                    sb.AppendLine($"  - {dev.DisplayName} [{dev.BluetoothAddress}] | Status: {dev.StatusDisplay} | Type: {dev.CategoryName}");
                }
            }
            else
            {
                sb.AppendLine("  No Bluetooth devices enumerated.");
            }
            sb.AppendLine();

            sb.AppendLine("================================================================================");
            sb.AppendLine("End of BlueDoc Diagnostic Report. UNLEFT LLC - All Rights Reserved.");
            sb.AppendLine("================================================================================");

            return sb.ToString();
        }

        public string GenerateJson(DiagnosticReport report)
        {
            return JsonConvert.SerializeObject(report, Formatting.Indented);
        }

        public string GenerateHtml(DiagnosticReport report)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html lang=\"en\">");
            sb.AppendLine("<head>");
            sb.AppendLine("  <meta charset=\"UTF-8\">");
            sb.AppendLine("  <title>BlueDoc Diagnostic Report</title>");
            sb.AppendLine("  <style>");
            sb.AppendLine("    body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; background: #0b1120; color: #f8fafc; margin: 0; padding: 24px; }");
            sb.AppendLine("    .container { max-width: 960px; margin: 0 auto; background: #1e293b; border-radius: 12px; padding: 32px; box-shadow: 0 10px 25px rgba(0,0,0,0.5); }");
            sb.AppendLine("    .header { display: flex; justify-content: space-between; align-items: center; border-bottom: 2px solid #334155; padding-bottom: 20px; margin-bottom: 24px; }");
            sb.AppendLine("    .logo { font-size: 26px; font-weight: bold; color: #38bdf8; }");
            sb.AppendLine("    .badge-passed { background: #166534; color: #86efac; padding: 6px 14px; border-radius: 20px; font-weight: 600; font-size: 14px; }");
            sb.AppendLine("    .badge-failed { background: #991b1b; color: #fca5a5; padding: 6px 14px; border-radius: 20px; font-weight: 600; font-size: 14px; }");
            sb.AppendLine("    .badge-warning { background: #854d0e; color: #fde047; padding: 6px 14px; border-radius: 20px; font-weight: 600; font-size: 14px; }");
            sb.AppendLine("    h2 { font-size: 18px; color: #94a3b8; border-bottom: 1px solid #334155; padding-bottom: 8px; margin-top: 28px; }");
            sb.AppendLine("    table { width: 100%; border-collapse: collapse; margin-top: 12px; }");
            sb.AppendLine("    th, td { text-align: left; padding: 10px 12px; border-bottom: 1px solid #334155; font-size: 14px; }");
            sb.AppendLine("    th { background: #0f172a; color: #94a3b8; }");
            sb.AppendLine("    .footer { margin-top: 32px; text-align: center; font-size: 12px; color: #64748b; }");
            sb.AppendLine("  </style>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");
            sb.AppendLine("<div class=\"container\">");

            sb.AppendLine("  <div class=\"header\">");
            sb.AppendLine("    <div>");
            sb.AppendLine("      <div class=\"logo\">BlueDoc Diagnostic Report</div>");
            sb.AppendLine("      <div style=\"color:#94a3b8; font-size:13px; margin-top:4px;\">UNLEFT LLC &bull; A Bluetooth Doctor for Windows</div>");
            sb.AppendLine("    </div>");
            string statusBadgeClass = report.Session?.OverallStatus == Core.Enums.DiagnosticStatus.Passed ? "badge-passed" : (report.Session?.OverallStatus == Core.Enums.DiagnosticStatus.Warning ? "badge-warning" : "badge-failed");
            sb.AppendLine($"    <div class=\"{statusBadgeClass}\">{report.Session?.OverallStatus.ToString().ToUpper()}</div>");
            sb.AppendLine("  </div>");

            sb.AppendLine("  <p style=\"color:#cbd5e1; font-size:14px;\">");
            sb.AppendLine($"    <strong>Generated:</strong> {report.GeneratedAtUtc:yyyy-MM-dd HH:mm:ss} UTC &bull; <strong>Machine:</strong> {report.MachineName} &bull; <strong>OS:</strong> {report.OsVersion}");
            sb.AppendLine("  </p>");

            sb.AppendLine("  <h2>1. Bluetooth Hardware & Driver</h2>");
            sb.AppendLine("  <table>");
            sb.AppendLine("    <tr><th style=\"width:25%;\">Property</th><th>Details</th></tr>");
            if (report.Adapter != null)
            {
                sb.AppendLine($"    <tr><td>Adapter Name</td><td><strong>{report.Adapter.FriendlyName}</strong></td></tr>");
                sb.AppendLine($"    <tr><td>Manufacturer</td><td>{report.Adapter.Manufacturer}</td></tr>");
                sb.AppendLine($"    <tr><td>Hardware ID</td><td><code>{report.Adapter.HardwareId}</code></td></tr>");
                sb.AppendLine($"    <tr><td>Device Status</td><td>{report.Adapter.StatusDisplay}</td></tr>");
                sb.AppendLine($"    <tr><td>Driver Version</td><td>{report.Adapter.Driver?.DriverVersion} ({report.Adapter.Driver?.DriverDate})</td></tr>");
            }
            else
            {
                sb.AppendLine("    <tr><td colspan=\"2\">No Bluetooth adapter detected.</td></tr>");
            }
            sb.AppendLine("  </table>");

            sb.AppendLine("  <h2>2. Diagnostic Tests</h2>");
            sb.AppendLine("  <table>");
            sb.AppendLine("    <tr><th>Test</th><th>Status</th><th>Summary</th><th>Recommended Action</th></tr>");
            if (report.Session?.Results != null)
            {
                foreach (var r in report.Session.Results)
                {
                    string color = r.Status == Core.Enums.DiagnosticStatus.Passed ? "#4ade80" : (r.Status == Core.Enums.DiagnosticStatus.Warning ? "#facc15" : "#f87171");
                    sb.AppendLine($"    <tr>");
                    sb.AppendLine($"      <td><strong>{r.TestName}</strong></td>");
                    sb.AppendLine($"      <td style=\"color:{color}; font-weight:bold;\">{r.Status}</td>");
                    sb.AppendLine($"      <td>{r.Summary}</td>");
                    sb.AppendLine($"      <td>{r.RecommendedAction}</td>");
                    sb.AppendLine($"    </tr>");
                }
            }
            sb.AppendLine("  </table>");

            sb.AppendLine("  <h2>3. Relevant Events</h2>");
            sb.AppendLine("  <table>");
            sb.AppendLine("    <tr><th>Time</th><th>Severity</th><th>Provider</th><th>Analysis</th></tr>");
            if (report.RelevantEvents != null && report.RelevantEvents.Count > 0)
            {
                foreach (var e in report.RelevantEvents)
                {
                    sb.AppendLine($"    <tr><td>{e.LocalTimeDisplay}</td><td>{e.LevelDisplayName}</td><td>{e.ProviderName} (ID {e.EventId})</td><td>{e.HumanAnalysis}</td></tr>");
                }
            }
            else
            {
                sb.AppendLine("    <tr><td colspan=\"4\">No abnormal Bluetooth system events identified.</td></tr>");
            }
            sb.AppendLine("  </table>");

            sb.AppendLine("  <div class=\"footer\">BlueDoc &copy; UNLEFT LLC &bull; Developer: Golam Morshed Eashan</div>");
            sb.AppendLine("</div>");
            sb.AppendLine("</body>");
            sb.AppendLine("</html>");

            return sb.ToString();
        }
    }
}
