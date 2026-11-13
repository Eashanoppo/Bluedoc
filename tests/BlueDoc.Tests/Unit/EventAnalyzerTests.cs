using System;
using BlueDoc.Infrastructure.EventLogs;

namespace BlueDoc.Tests.Unit
{
    public class EventAnalyzerTests
    {
        public static void RunAllTests()
        {
            Console.WriteLine("[TEST SUITE] Bluetooth Event Analyzer Heuristics Tests");

            var analyzer = new BluetoothEventAnalyzer();

            // Test BTHUSB Event 3
            var ev3 = analyzer.InterpretEvent("BTHUSB", 3, "Error", "A command sent to the adapter has timed out.");
            if (string.IsNullOrEmpty(ev3.HumanAnalysis) || !ev3.HumanAnalysis.Contains("timed out"))
                throw new Exception("BTHUSB Event 3 interpretation failed.");
            if (string.IsNullOrEmpty(ev3.PossibleCause) || !ev3.PossibleCause.Contains("USB"))
                throw new Exception("BTHUSB Event 3 cause correlation failed.");
            Console.WriteLine("  ✓ BTHUSB Event 3 (HCI Command Timeout) interpreted correctly.");

            // Test BTHUSB Event 5
            var ev5 = analyzer.InterpretEvent("BTHUSB", 5, "Error", "Initialization failed.");
            if (!ev5.HumanAnalysis.Contains("initialization"))
                throw new Exception("BTHUSB Event 5 interpretation failed.");
            Console.WriteLine("  ✓ BTHUSB Event 5 (Initialization Failure) interpreted correctly.");

            // Test bthserv Event
            var evSvc = analyzer.InterpretEvent("bthserv", 1000, "Error", "Service terminated unexpectedly.");
            if (!evSvc.HumanAnalysis.Contains("Bluetooth Support Service"))
                throw new Exception("bthserv Event interpretation failed.");
            Console.WriteLine("  ✓ bthserv service error interpreted correctly.");
        }
    }
}
