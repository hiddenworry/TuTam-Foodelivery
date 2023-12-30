using DataAccess.Entities;

namespace DataAccess.Repositories
{
    public interface ITargetProcessRepository
    {
        Task<int> CreateTargetProcessAsync(TargetProcess targetProcess);
        Task<int> CreateTargetProcessesAsync(List<TargetProcess> targetProcesses);
        Task<List<TargetProcess>> FindStillTakingPlaceActivityTargetProcessesByBranchIdAsync(
            Guid branchId
        );
        Task<TargetProcess?> FindTargetProcessByActivityIdAndItemIdAsync(
            Guid activityId,
            Guid itemId
        );
        Task<List<TargetProcess>> FindTargetProcessesByActivityIdAsync(Guid activityId);
        Task<int> UpdateTargetProcessAsync(TargetProcess targetProcess);
    }
}
