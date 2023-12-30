using DataAccess.Entities;

namespace DataAccess.Repositories
{
    public interface IActivityRoleRepository
    {
        Task<int> CreateActivityRole(ActivityRole activityRole);
        Task<ActivityRole?> GetActivityRoleById(Guid activityRoleId);
        Task<List<ActivityRole>?> GetListActivityRole(Guid activityId);
        Task<int> UpdateActivityRole(ActivityRole activityRole);
    }
}
