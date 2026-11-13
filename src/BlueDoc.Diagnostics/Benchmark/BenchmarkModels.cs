using System;
using System.Collections.Generic;

namespace BlueDoc.Diagnostics.Benchmark
{
    public class BenchmarkMetric
    {
        public string Name { get; set; } = string.Empty;
        public string ValueDisplay { get; set; } = string.Empty;
        public string TargetThreshold { get; set; } = string.Empty;
        public bool IsOptimal { get; set; }
        public string Interpretation { get; set; } = string.Empty;
    }

    public class BenchmarkResult
    {
        public Guid BenchmarkId { get; set; } = Guid.NewGuid();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public double OverallScore { get; set; }
        public string Rating { get; set; } = "Inconclusive"; // Optimal, Adequate, Degraded, Substandard
        public long EnumerationLatencyMs { get; set; }
        public long ServiceQueryLatencyMs { get; set; }
        public long RadioInquiryLatencyMs { get; set; }
        public int DetectedAdapterCount { get; set; }
        public string PrimaryAdapterName { get; set; } = "None Detected";
        public string Manufacturer { get; set; } = "Unknown";
        public string HardwareSpecGrade { get; set; } = "Ungraded";
        public string SummaryInterpretation { get; set; } = string.Empty;
        public List<BenchmarkMetric> Metrics { get; set; } = new List<BenchmarkMetric>();
    }
}

