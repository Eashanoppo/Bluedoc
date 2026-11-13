using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BlueDoc.Core.Models;

namespace BlueDoc.Core.Interfaces
{
    public interface IDiagnosticSessionRepository
    {
        Task SaveSessionAsync(DiagnosticSession session);
        Task<List<DiagnosticSession>> GetRecentSessionsAsync(int limit = 20);
        Task<DiagnosticSession> GetSessionByIdAsync(Guid sessionId);
        Task ClearAllSessionsAsync();
    }
}
