using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace BlueDoc.Infrastructure.Diagnostics
{
    public class HciTraceSession
    {
        public bool IsActive { get; set; }
        public DateTime? StartedAt { get; set; }
        public string TraceFilePath { get; set; } = string.Empty;
        public string Status { get; set; } = "Idle";
        public string LastOutput { get; set; } = string.Empty;
    }

    public class HciTraceAssistant
    {
        private HciTraceSession _currentSession = new HciTraceSession();

        public HciTraceSession CurrentSession => _currentSession;

        public Task<HciTraceSession> StartTraceAsync(string outputPath = null)
        {
            return Task.Run(() =>
            {
                if (_currentSession.IsActive)
                {
                    _currentSession.Status = "Trace is already actively running.";
                    return _currentSession;
                }

                if (string.IsNullOrWhiteSpace(outputPath))
                {
                    string tempDir = Path.Combine(Path.GetTempPath(), "BlueDoc_Traces");
                    if (!Directory.Exists(tempDir)) Directory.CreateDirectory(tempDir);
                    outputPath = Path.Combine(tempDir, $"Bluetooth_HCI_Trace_{DateTime.Now:yyyyMMdd_HHmmss}.etl");
                }

                _currentSession.TraceFilePath = outputPath;

                // Formulate netsh trace command
                string args = $"trace start scenario=Bluetooth capture=yes report=no tracefile=\"{outputPath}\" maxSize=250";

                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = "netsh.exe",
                        Arguments = args,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    };

                    using (var proc = Process.Start(psi))
                    {
                        string output = proc.StandardOutput.ReadToEnd();
                        string error = proc.StandardError.ReadToEnd();
                        proc.WaitForExit(8000);

                        if (proc.ExitCode == 0 || output.Contains("Running") || output.Contains("Trace configuration"))
                        {
                            _currentSession.IsActive = true;
                            _currentSession.StartedAt = DateTime.UtcNow;
                            _currentSession.Status = $"HCI Trace actively recording to: {outputPath}";
                            _currentSession.LastOutput = output;
                        }
                        else
                        {
                            _currentSession.IsActive = false;
                            _currentSession.Status = $"Failed to start trace (Exit code {proc.ExitCode}). Administrator privileges required.";
                            _currentSession.LastOutput = string.IsNullOrWhiteSpace(error) ? output : error;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _currentSession.IsActive = false;
                    _currentSession.Status = $"Execution error: {ex.Message}";
                }

                return _currentSession;
            });
        }

        public Task<HciTraceSession> StopTraceAsync()
        {
            return Task.Run(() =>
            {
                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = "netsh.exe",
                        Arguments = "trace stop",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    };

                    using (var proc = Process.Start(psi))
                    {
                        string output = proc.StandardOutput.ReadToEnd();
                        proc.WaitForExit(15000);

                        _currentSession.IsActive = false;
                        _currentSession.Status = $"Trace captured successfully. File: {_currentSession.TraceFilePath}";
                        _currentSession.LastOutput = output;
                    }
                }
                catch (Exception ex)
                {
                    _currentSession.IsActive = false;
                    _currentSession.Status = $"Error stopping trace: {ex.Message}";
                }

                return _currentSession;
            });
        }

        public string GetWiresharkAnalysisGuide(string etlPath)
        {
            return "HOW TO ANALYZE WINDOWS BLUETOOTH HCI TRACES IN WIRESHARK:\n" +
                   "----------------------------------------------------------\n" +
                   $"1. Trace File Location: {etlPath}\n" +
                   "2. Windows records raw HCI packets in Microsoft Event Trace Log (.etl) format.\n" +
                   "3. To convert the .etl log into standard Wireshark .pcapng format:\n" +
                   "   a) Download the open-source 'etl2pcapng' tool from Microsoft GitHub (https://github.com/microsoft/etl2pcapng).\n" +
                   $"   b) Run in terminal: etl2pcapng.exe \"{etlPath}\" \"{Path.ChangeExtension(etlPath, ".pcapng")}\"\n" +
                   "4. Open the generated .pcapng in Wireshark.\n" +
                   "5. Filter by 'btsnoop' to inspect HCI Command, Event, ACL Data, and SCO audio packets with complete millisecond timing.";
        }
    }
}

