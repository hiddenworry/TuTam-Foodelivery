using DataAccess.Entities;

namespace DataAccess.Repositories
{
    public interface IActivityBranchRepository
    {
        Task<int> CreateActivityBranchesAsync(List<ActivityBranch> activityBranches);
        Task<int> DeleteActivityBranchesAsync(List<ActivityBranch> activityBranches);
        Task<ActivityBranch?> FindActivityBranchByActivityIdAndBranchIdAsync(
            Guid activityId,
            Guid branchId
        );
    }
}
