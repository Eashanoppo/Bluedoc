using System.Threading;
using System.Threading.Tasks;
using BlueDoc.Core.Models;

namespace BlueDoc.Diagnostics.Rules
{
    public interface IDiagnosticRule
    {
        string RuleId { get; }
        string RuleName { get; }
        string Category { get; }
        string Description { get; }

        Task<DiagnosticResult> EvaluateAsync(DiagnosticContext context, CancellationToken cancellationToken = default(CancellationToken));
    }
}

