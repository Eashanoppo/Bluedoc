using System;
using BlueDoc.Core.Enums;

namespace BlueDoc.Core.Models
{
    public class DiagnosticResult
    {
        public string RuleId { get; set; } = string.Empty;
        public string TestName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public DiagnosticStatus Status { get; set; } = DiagnosticStatus.NotTested;
        public string Summary { get; set; } = string.Empty;
        public string TechnicalDetails { get; set; } = string.Empty;
        public string Evidence { get; set; } = string.Empty;
        public string PossibleCause { get; set; } = string.Empty;
        public string RecommendedAction { get; set; } = string.Empty;
        public string Confidence { get; set; } = "High"; // Confirmed Fact, Strong Indicator, Hypothesis
        public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;

        public bool RequiresAdmin { get; set; }
    }
}

