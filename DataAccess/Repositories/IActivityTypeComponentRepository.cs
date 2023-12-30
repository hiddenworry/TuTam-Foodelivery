using DataAccess.Entities;

namespace DataAccess.Repositories
{
    public interface IActivityTypeComponentRepository
    {
        Task<int> CreateActivityTypeComponentsAsync(
            List<ActivityTypeComponent> activityTypeComponents
        );
        Task<int> DeleteActivityTypeComponentsAsync(
            List<ActivityTypeComponent> activityTypeComponents
        );
        Task<int> CreateActivityTypeComponentAsync(ActivityTypeComponent activityTypeComponent);
        Task<int> DeleteActivityTypeComponentAsync(ActivityTypeComponent activityTypeComponent);
        Task<ActivityTypeComponent?> FindActivityComponentByActivityIdAndActivityTypeIdAsync(
            Guid activityId,
            Guid activityTypeId
        );
    }
}
