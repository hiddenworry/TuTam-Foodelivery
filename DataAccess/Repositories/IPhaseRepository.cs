using DataAccess.Entities;

namespace DataAccess.Repositories
{
    public interface IPhaseRepository
    {
        Task<int> CreatePhaseAsync(Phase phase);
        Task<int> DeletePhaseAsync(Phase phase);
        Task<List<Phase>?> GetPhaseByActivityIdAsync(Guid activityId);
        Phase? GetPhaseById(Guid phaseId);
        Task<Phase?> GetPhaseByIdAsync(Guid phaseId);
        int UpdatePhase(Phase phase);
        Task<int> UpdatePhaseAsync(Phase phase);
    }
}
