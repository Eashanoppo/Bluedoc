using System.Collections.Generic;

namespace BlueDoc.Application.KnowledgeBase
{
    public class KnowledgeBaseArticle
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Severity { get; set; } = "Warning"; // Critical, Warning, Informational
        public List<string> Symptoms { get; set; } = new List<string>();
        public List<string> AffectedHardware { get; set; } = new List<string>();
        public string TechnicalExplanation { get; set; } = string.Empty;
        public List<string> StepByStepRemediation { get; set; } = new List<string>();
        public List<string> RelatedRuleIds { get; set; } = new List<string>();
        public string RecommendedRepairActionId { get; set; } = string.Empty;
    }
}

