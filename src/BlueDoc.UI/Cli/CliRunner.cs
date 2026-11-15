using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using BlueDoc.Application.Cloud;
using BlueDoc.Application.KnowledgeBase;
using BlueDoc.Application.Services;
using BlueDoc.Core.Enums;
using BlueDoc.Diagnostics.Benchmark;
using BlueDoc.Infrastructure.Reporting;

namespace BlueDoc.UI.Cli
{
    public static class CliRunner
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AttachConsole(int dwProcessId);

        private const int ATTACH_PARENT_PROCESS = -1;

        public static void InitializeConsole()
        {
            try
            {
                AttachConsole(ATTACH_PARENT_PROCESS);
                var stdOutStream = Console.OpenStandardOutput();
                var stdOutWriter = new StreamWriter(stdOutStream, System.Text.Encoding.Default) { AutoFlush = true };
                Console.SetOut(stdOutWriter);

                var stdErrStream = Console.OpenStandardError();
                var stdErrWriter = new StreamWriter(stdErrStream, System.Text.Encoding.Default) { AutoFlush = true };
                Console.SetError(stdErrWriter);
            }
            catch { }
        }

        public static bool IsCliInvocation(string[] args)
        {
            if (args == null || args.Length == 0) return false;

            // Only treat as CLI invocation if running directly inside BlueDoc or BlueDoc.Tests process
            try
            {
                var procName = System.Diagnostics.Process.GetCurrentProcess().ProcessName;
                if (!procName.StartsWith("BlueDoc", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }
            catch { }

            var first = args[0].ToLowerInvariant().TrimStart('-', '/');
            return first == "diagnose" || first == "d" ||
                   first == "inspect" || first == "i" ||
                   first == "benchmark" || first == "b" ||
                   first == "kb" || first == "repair" ||
                   first == "silent-patch" ||
                   first == "version" || first == "v" ||
                   first == "help" || first == "h" || first == "?";
        }

        public static async Task<int> RunAsync(string[] args)
        {
            if (args == null || args.Length == 0)
            {
                InitializeConsole();
                PrintHelp();
                return 4;
            }

            var command = args[0].TrimStart('-', '/').ToLowerInvariant();

            // Headless execution: Never allocate console or print banners for silent operations
            if (command == "silent-patch")
            {
                try
                {
                    var inspector = new BlueDoc.Infrastructure.Hardware.SetupApiHardwareInspector();
                    var primary = inspector.GetPrimaryAdapter();
                    if (BlueDoc.Infrastructure.Drivers.WinUsbFirmwarePatcher.IsSiliconUnpatched(primary))
                    {
                        var res = await BlueDoc.Infrastructure.Drivers.WinUsbFirmwarePatcher.ProvisionAndPatchSiliconAsync(primary);
                        return res.Success ? 0 : 2;
                    }
                    return 0;
                }
                catch
                {
                    return 1;
                }
            }

            if (command == "rollback")
            {
                try
                {
                    var inspector = new BlueDoc.Infrastructure.Hardware.SetupApiHardwareInspector();
                    var primary = inspector.GetPrimaryAdapter();
                    bool rolledBack = BlueDoc.Infrastructure.Drivers.WinUsbFirmwarePatcher.RollbackToGenericDriver(primary);
                    return rolledBack ? 0 : 2;
                }
                catch
                {
                    return 1;
                }
            }

            InitializeConsole();

            Console.WriteLine();
            Console.WriteLine("===============================================================");
            Console.WriteLine("  BLUEDOC COMMAND LINE INTERFACE (CLI)");
            Console.WriteLine("  Product: BlueDoc v1.0.0 | UNLEFT LLC");
            Console.WriteLine("  Lead Architect & Developer: Golam Morshed Eashan");
            Console.WriteLine("===============================================================");

            try
            {
                switch (command)
                {
                    case "help":
                    case "h":
                    case "?":
                        PrintHelp();
                        return 0;

                    case "version":
                    case "v":
                        Console.WriteLine("BlueDoc Version: 1.0.0.0");
                        Console.WriteLine("Company: UNLEFT LLC");
                        Console.WriteLine("Mission: Detect the problem. Understand the cause. Guide the repair.");
                        return 0;

                    case "diagnose":
                    case "d":
                        return await ExecuteDiagnoseAsync(args);

                    case "inspect":
                    case "i":
                        return await ExecuteInspectAsync(args);

                    case "benchmark":
                    case "b":
                        return await ExecuteBenchmarkAsync(args);

                    case "kb":
                        return ExecuteKnowledgeBaseSearch(args);

                    case "repair":
                    case "r":
                        return await ExecuteRepairAsync(args);

                    case "silent-patch":
                        var inspector = new BlueDoc.Infrastructure.Hardware.SetupApiHardwareInspector();
                        var primary = inspector.GetPrimaryAdapter();
                        if (BlueDoc.Infrastructure.Drivers.WinUsbFirmwarePatcher.IsSiliconUnpatched(primary))
                        {
                            var res = await BlueDoc.Infrastructure.Drivers.WinUsbFirmwarePatcher.ProvisionAndPatchSiliconAsync(primary);
                            return res.Success ? 0 : 2;
                        }
                        return 0;

                    default:
                        Console.WriteLine($"[ERROR] Unrecognized CLI command: '{args[0]}'");
                        PrintHelp();
                        return 4;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FATAL CLI ERROR] {ex.Message}");
                return 2;
            }
        }

        private static async Task<int> ExecuteDiagnoseAsync(string[] args)
        {
            Console.WriteLine("[CLI] Initializing BlueDoc Diagnostic Engine...");
            var diagService = new DiagnosticsService();
            var session = await diagService.RunDiagnosticsAsync();

            string format = "json";
            string outputPath = null;

            for (int i = 1; i < args.Length; i++)
            {
                var a = args[i].ToLowerInvariant();
                if (a == "--format" && i + 1 < args.Length)
                {
                    format = args[++i].ToLowerInvariant();
                }
                else if (a == "--output" && i + 1 < args.Length)
                {
                    outputPath = args[++i];
                }
            }

            var reportService = new ReportService();
            var adapterService = new AdapterService();
            var adapter = await adapterService.GetPrimaryAdapterAsync();
            var osInfo = BlueDoc.Infrastructure.Platform.OperatingSystemInspector.GetOperatingSystemInfo();

            var report = new BlueDoc.Core.Models.DiagnosticReport
            {
                Session = session,
                Adapter = adapter,
                OsVersion = osInfo,
                OverallHealthSummary = session.OverallStatus.ToString()
            };

            if (format == "pdf")
            {
                var pdfBytes = reportService.GeneratePdf(report);
                if (!string.IsNullOrEmpty(outputPath))
                {
                    File.WriteAllBytes(outputPath, pdfBytes);
                    Console.WriteLine($"[CLI] Binary PDF report saved to: {outputPath}");
                }
                else
                {
                    Console.WriteLine("[CLI] Notice: Specify --output <path.pdf> when generating binary PDF format.");
                }
            }
            else
            {
                string textOutput = format == "html" 
                    ? reportService.GenerateHtml(report) 
                    : (format == "txt" ? reportService.GeneratePlainText(report) : reportService.GenerateJson(report));

                if (!string.IsNullOrEmpty(outputPath))
                {
                    File.WriteAllText(outputPath, textOutput);
                    Console.WriteLine($"[CLI] Diagnostic report written successfully to: {outputPath}");
                }
                else
                {
                    Console.WriteLine(textOutput);
                }
            }

            Console.WriteLine($"\n[CLI] Diagnostic complete. Overall Status: {session.OverallStatus}");

            if (adapter == null || !adapter.IsPresent)
                return 3; // No adapter

            switch (session.OverallStatus)
            {
                case DiagnosticStatus.Passed: return 0;
                case DiagnosticStatus.Warning: return 1;
                default: return 2;
            }
        }

        private static async Task<int> ExecuteInspectAsync(string[] args)
        {
            Console.WriteLine("[CLI] Querying installed Bluetooth adapters & drivers...");
            var adapterService = new AdapterService();
            var adapters = await adapterService.GetAdaptersAsync();

            if (adapters.Count == 0)
            {
                Console.WriteLine("[CLI] No Bluetooth adapters detected on this computer.");
                return 3;
            }

            Console.WriteLine($"[CLI] Found {adapters.Count} Bluetooth adapter(s):");
            foreach (var a in adapters)
            {
                var mfg = !string.IsNullOrEmpty(a.Manufacturer) ? a.Manufacturer : a.HardwareDetails?.IdentifiedManufacturer;
                Console.WriteLine($"  - Device: {a.FriendlyName}");
                Console.WriteLine($"    Manufacturer: {mfg}");
                Console.WriteLine($"    Hardware ID:  {a.HardwareId}");
                Console.WriteLine($"    Vendor ID:    {a.HardwareDetails?.VendorId}, PID: {a.HardwareDetails?.ProductId}");
                Console.WriteLine($"    Status:       {a.Status} (Problem Code: {a.ProblemCode})");
                Console.WriteLine($"    Driver:       {a.Driver?.DriverProvider} (v{a.Driver?.DriverVersion}) Date: {a.Driver?.DriverDateString}");
                Console.WriteLine();
            }

            return 0;
        }

        private static async Task<int> ExecuteBenchmarkAsync(string[] args)
        {
            Console.WriteLine("[CLI] Running Bluetooth Performance & Latency Benchmark...");
            var engine = new BluetoothBenchmarkEngine();
            var bench = await engine.RunBenchmarkAsync();

            Console.WriteLine("\n[BENCHMARK RESULTS]");
            Console.WriteLine($"  Overall Score:           {bench.OverallScore:F1} / 100.0 [{bench.Rating}]");
            Console.WriteLine($"  Hardware Spec Grade:     {bench.HardwareSpecGrade}");
            Console.WriteLine($"  SetupAPI Enumeration:    {bench.EnumerationLatencyMs} ms");
            Console.WriteLine($"  SCM Services Query:      {bench.ServiceQueryLatencyMs} ms");
            Console.WriteLine($"  Radio Inquiry Latency:   {bench.RadioInquiryLatencyMs} ms");
            Console.WriteLine($"  Detected Adapters:       {bench.DetectedAdapterCount}");
            Console.WriteLine($"  Primary Adapter:         {bench.PrimaryAdapterName}");
            Console.WriteLine($"  Interpretation:          {bench.SummaryInterpretation}\n");

            Console.WriteLine("Detailed Metrics:");
            foreach (var m in bench.Metrics)
            {
                var icon = m.IsOptimal ? "✓" : "!";
                Console.WriteLine($"  [{icon}] {m.Name,-30}: {m.ValueDisplay,-12} (Threshold: {m.TargetThreshold})");
            }

            return (bench.OverallScore >= 70) ? 0 : 1;
        }

        private static int ExecuteKnowledgeBaseSearch(string[] args)
        {
            var kb = new OfflineKnowledgeBaseService();
            string query = args.Length > 1 ? string.Join(" ", args.Skip(1)) : string.Empty;

            var results = kb.Search(query).ToList();
            Console.WriteLine($"[KB] Search for '{query}' returned {results.Count} article(s):\n");

            foreach (var r in results)
            {
                Console.WriteLine($"  [{r.Id}] {r.Title} ({r.Category} - {r.Severity})");
                Console.WriteLine($"  Explanation: {r.TechnicalExplanation}");
                Console.WriteLine("  Remediation:");
                foreach (var step in r.StepByStepRemediation)
                {
                    Console.WriteLine($"    {step}");
                }
                Console.WriteLine();
            }

            return 0;
        }

        private static async Task<int> ExecuteRepairAsync(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("[ERROR] Missing repair identifier. Example: BlueDoc.exe --repair RestartBthServ --confirm");
                return 4;
            }

            var repairId = args[1];
            bool confirmed = args.Any(a => a.Equals("--confirm", StringComparison.OrdinalIgnoreCase) || a.Equals("-y", StringComparison.OrdinalIgnoreCase));

            if (!confirmed)
            {
                Console.WriteLine($"[SAFETY GATE] Repair '{repairId}' requires explicit confirmation.");
                Console.WriteLine("Pass the '--confirm' or '-y' flag to proceed in headless mode.");
                return 4;
            }

            Console.WriteLine($"[CLI] Executing safe repair action: {repairId}...");
            var repairService = new RepairService();
            var res = await repairService.ExecuteRepairAsync(repairId);

            bool isSuccess = string.Equals(res.Status, "Success", StringComparison.OrdinalIgnoreCase);
            Console.WriteLine($"[CLI] Result: {(isSuccess ? "SUCCESS" : "FAILED")}");
            Console.WriteLine($"[CLI] Action: {res.ActionTitle}");
            if (!string.IsNullOrEmpty(res.ErrorMessage))
            {
                Console.WriteLine($"[CLI] Error: {res.ErrorMessage}");
            }
            if (!string.IsNullOrEmpty(res.VerificationResult))
            {
                Console.WriteLine($"[CLI] Verification: {res.VerificationResult}");
            }

            return isSuccess ? 0 : 2;
        }

        private static void PrintHelp()
        {
            Console.WriteLine("USAGE:");
            Console.WriteLine("  BlueDoc.exe [options]");
            Console.WriteLine();
            Console.WriteLine("COMMANDS:");
            Console.WriteLine("  --diagnose, -d                  Run full diagnostic test sequence.");
            Console.WriteLine("    --format <txt|html|json|pdf>  Specify report output format (default: json).");
            Console.WriteLine("    --output <path>               Save report directly to file.");
            Console.WriteLine("  --inspect, -i                   Enumerate Bluetooth adapters, hardware IDs, and drivers.");
            Console.WriteLine("  --benchmark, -b                 Run Bluetooth performance and latency benchmark.");
            Console.WriteLine("  --kb [query]                    Search offline troubleshooting knowledge base.");
            Console.WriteLine("  --repair <id> --confirm         Execute safe repair non-interactively.");
            Console.WriteLine("  --version, -v                   Display application version and credits.");
            Console.WriteLine("  --help, -h, /?                  Display this manual.");
            Console.WriteLine();
            Console.WriteLine("EXIT CODES:");
            Console.WriteLine("  0 = All tests passed / Operation succeeded.");
            Console.WriteLine("  1 = Diagnostic warnings detected.");
            Console.WriteLine("  2 = Critical failure detected.");
            Console.WriteLine("  3 = No Bluetooth adapter detected on system.");
            Console.WriteLine("  4 = Syntax error or missing confirmation flag.");
            Console.WriteLine("  5 = Administrator privileges required.");
        }
    }
}
