using DataAccess.Entities;
using DataAccess.EntityEnums;

namespace DataAccess.Repositories
{
    public interface IActivityRepository
    {
        Task<int> CountActivityByStatus(
            ActivityStatus? status,
            DateTime? startDate,
            DateTime? endDate,
            Guid? branchId
        );
        Task<int> CreateActivityAsync(Activity activity);
        Task<Activity?> FindActivityByIdAsync(Guid id);
        Task<Activity?> FindActivityByIdForDetailAsync(Guid id);
        Task<List<Activity>> FindActivityByItemId(Guid itemId, Guid userId);
        Task<Activity?> FindActivityByNameIgnoreCaseAsync(string name);
        Task<List<Activity>> GetActivitiesAsync(
            string? name,
            ActivityStatus? status,
            ActivityScope? scope,
            List<Guid>? activityTypeIds,
            DateTime? startDate,
            DateTime? endDate,
            Guid? userId,
            Guid? branchId,
            string? address,
            string? userRoleName
        );
        Task<int> UpdateActivityAsync(Activity activity);
    }
}
