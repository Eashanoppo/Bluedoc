using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using BlueDoc.Core.Enums;
using BlueDoc.Core.Models;
using BlueDoc.Infrastructure.Hardware;
using BlueDoc.Infrastructure.Native;
using BlueDoc.Infrastructure.Platform;
using BlueDoc.Infrastructure.Services;

namespace BlueDoc.Diagnostics.Benchmark
{
    public class BluetoothBenchmarkEngine
    {
        public Task<BenchmarkResult> RunBenchmarkAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.Run(() =>
            {
                var result = new BenchmarkResult();
                var sw = new Stopwatch();

                // 1. Benchmark Hardware Enumeration Latency (SetupAPI)
                var hwInspector = new SetupApiHardwareInspector();
                sw.Start();
                var adapters = hwInspector.EnumerateAdapters();
                sw.Stop();
                result.EnumerationLatencyMs = sw.ElapsedMilliseconds;
                result.DetectedAdapterCount = adapters.Count;

                var primary = adapters.FirstOrDefault(a => a.IsPresent) ?? adapters.FirstOrDefault();
                if (primary != null)
                {
                    result.PrimaryAdapterName = primary.FriendlyName;
                    result.Manufacturer = !string.IsNullOrEmpty(primary.Manufacturer)
                        ? primary.Manufacturer
                        : primary.HardwareDetails?.IdentifiedManufacturer ?? "Unknown";
                }

                // 2. Benchmark Service Control Manager Latency (bthserv + Audio Gateway)
                var svcInspector = new WindowsServiceInspector();
                sw.Restart();
                var bthserv = svcInspector.GetServiceInfo("bthserv");
                var bthHf = svcInspector.GetServiceInfo("BthHFSrv");
                var btag = svcInspector.GetServiceInfo("BTAGService");
                sw.Stop();
                result.ServiceQueryLatencyMs = sw.ElapsedMilliseconds;

                // 3. Benchmark Win32 Radio Discovery & Handle Acquisition Latency
                sw.Restart();
                long radioLatency = 0;
                bool radioHandleAcquired = false;
                try
                {
                    var p = new BluetoothNative.BLUETOOTH_FIND_RADIO_PARAMS();
                    p.dwSize = (uint)Marshal.SizeOf(typeof(BluetoothNative.BLUETOOTH_FIND_RADIO_PARAMS));
                    IntPtr hRadio = IntPtr.Zero;
                    IntPtr hFind = BluetoothNative.BluetoothFindFirstRadio(ref p, out hRadio);
                    if (hFind != IntPtr.Zero)
                    {
                        radioHandleAcquired = true;
                        if (hRadio != IntPtr.Zero)
                        {
                            BluetoothNative.CloseHandle(hRadio);
                        }
                        BluetoothNative.BluetoothFindRadioClose(hFind);
                    }
                }
                catch
                {
                    radioHandleAcquired = false;
                }
                sw.Stop();
                radioLatency = sw.ElapsedMilliseconds;
                result.RadioInquiryLatencyMs = radioLatency;

                // 4. Calculate Composite Benchmark Score (0 - 100)
                double score = 0;

                // Latency Scoring Component (Max 40 pts)
                // - Enumeration Latency (Max 20 pts)
                if (result.EnumerationLatencyMs < 100) score += 20;
                else if (result.EnumerationLatencyMs < 250) score += 16;
                else if (result.EnumerationLatencyMs < 500) score += 12;
                else if (result.EnumerationLatencyMs < 1000) score += 8;
                else score += 4;

                // - SCM Turnaround Latency (Max 10 pts)
                if (result.ServiceQueryLatencyMs < 60) score += 10;
                else if (result.ServiceQueryLatencyMs < 150) score += 7;
                else if (result.ServiceQueryLatencyMs < 300) score += 4;
                else score += 1;

                // - Radio Inquiry Latency (Max 10 pts)
                if (radioHandleAcquired)
                {
                    if (result.RadioInquiryLatencyMs < 120) score += 10;
                    else if (result.RadioInquiryLatencyMs < 300) score += 7;
                    else score += 4;
                }

                // Hardware Status Component (Max 40 pts)
                if (primary != null)
                {
                    if (primary.Status == AdapterStatus.Operational && primary.ProblemCode == 0)
                    {
                        score += 25;
                    }
                    else if (primary.Status == AdapterStatus.Disabled)
                    {
                        score += 10; // Penalize for disabled
                    }
                    else
                    {
                        score += 0; // Device error (Code 10/43)
                    }

                    if (primary.Driver != null && primary.Driver.IsDriverInstalled && !string.IsNullOrEmpty(primary.Driver.DriverVersion))
                    {
                        score += 15;
                    }
                }

                // Subsystem & Service Capabilities (Max 20 pts)
                if (bthserv != null && bthserv.IsRunning)
                {
                    score += 10;
                }
                var os = OperatingSystemInspector.GetOperatingSystemInfo();
                if (os.Platform == WindowsPlatformType.Windows10 || os.Platform == WindowsPlatformType.Windows11)
                {
                    score += 10; // Modern WinRT BLE & GATT support
                }
                else
                {
                    score += 5; // Windows 7 classic baseline
                }

                result.OverallScore = Math.Round(Math.Min(100.0, Math.Max(0.0, score)), 1);

                // Hardware Spec Grading
                if (result.DetectedAdapterCount == 0 || primary == null || primary.Status == AdapterStatus.DeviceError)
                {
                    result.HardwareSpecGrade = "Grade F (Non-Functional / Missing)";
                }
                else if (os.Platform == WindowsPlatformType.Windows11 || os.Platform == WindowsPlatformType.Windows10)
                {
                    result.HardwareSpecGrade = "Grade A (Modern Bluetooth & BLE 4.0+)";
                }
                else
                {
                    result.HardwareSpecGrade = "Grade B (Classic Bluetooth / Legacy Stack)";
                }

                // Rating Categorization
                if (result.OverallScore >= 85)
                {
                    result.Rating = "Optimal";
                    result.SummaryInterpretation = "Bluetooth subsystem is operating at peak responsiveness with minimal driver and bus latencies.";
                }
                else if (result.OverallScore >= 70)
                {
                    result.Rating = "Adequate";
                    result.SummaryInterpretation = "Bluetooth subsystem is functional with acceptable responsiveness. Standard operations will perform normally.";
                }
                else if (result.OverallScore >= 50)
                {
                    result.Rating = "Degraded";
                    result.SummaryInterpretation = "Elevated latency or degraded service state detected. Device discovery and connection times may be noticeably delayed.";
                }
                else
                {
                    result.Rating = "Substandard";
                    result.SummaryInterpretation = "Severe controller, driver, or service bottleneck detected. Safe Repair intervention is recommended.";
                }

                // Detailed Metrics Catalog
                result.Metrics.Add(new BenchmarkMetric
                {
                    Name = "SetupAPI Enumeration Latency",
                    ValueDisplay = $"{result.EnumerationLatencyMs} ms",
                    TargetThreshold = "< 250 ms",
                    IsOptimal = result.EnumerationLatencyMs < 250,
                    Interpretation = result.EnumerationLatencyMs < 250 ? "Fast PnP bus response" : "Delayed device manager enumeration"
                });

                result.Metrics.Add(new BenchmarkMetric
                {
                    Name = "SCM Services Query Latency",
                    ValueDisplay = $"{result.ServiceQueryLatencyMs} ms",
                    TargetThreshold = "< 150 ms",
                    IsOptimal = result.ServiceQueryLatencyMs < 150,
                    Interpretation = result.ServiceQueryLatencyMs < 150 ? "Responsive Service Control Manager" : "Service database query delay"
                });

                result.Metrics.Add(new BenchmarkMetric
                {
                    Name = "Radio Handle Acquisition",
                    ValueDisplay = radioHandleAcquired ? $"{result.RadioInquiryLatencyMs} ms" : "Unresponsive",
                    TargetThreshold = "< 300 ms",
                    IsOptimal = radioHandleAcquired && result.RadioInquiryLatencyMs < 300,
                    Interpretation = radioHandleAcquired ? "Win32 Radio API responsive" : "Radio handle query returned null or timed out"
                });

                result.Metrics.Add(new BenchmarkMetric
                {
                    Name = "Hardware Controller Status",
                    ValueDisplay = primary != null ? primary.Status.ToString() : "Missing",
                    TargetThreshold = "Operational",
                    IsOptimal = primary != null && primary.Status == AdapterStatus.Operational,
                    Interpretation = primary != null && primary.Status == AdapterStatus.Operational
                        ? $"Operating normally (Code {primary.ProblemCode})"
                        : $"Abnormal state: {primary?.StatusDisplay ?? "No adapter found"}"
                });

                result.Metrics.Add(new BenchmarkMetric
                {
                    Name = "Bluetooth Core Service State",
                    ValueDisplay = bthserv != null ? bthserv.Status : "Missing",
                    TargetThreshold = "Running",
                    IsOptimal = bthserv != null && bthserv.IsRunning,
                    Interpretation = bthserv != null && bthserv.IsRunning ? "bthserv active in memory" : "bthserv stopped or disabled"
                });

                return result;
            }, cancellationToken);
        }
    }
}
