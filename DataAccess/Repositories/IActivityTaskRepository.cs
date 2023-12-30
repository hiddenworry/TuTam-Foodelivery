using DataAccess.Entities;
using DataAccess.EntityEnums;

namespace DataAccess.Repositories
{
    public interface IActivityTaskRepository
    {
        Task<int> CreateTaskAsync(ActivityTask task);
        Task<ActivityTask?> CreateTaskReturnObjectAsync(ActivityTask task);
        Task<int> DeleteTaskAsync(ActivityTask task);

        Task<List<ActivityTask>?> GetTaskAsync(
            Guid ownerId,
            Guid? activityId,
            Guid? phaseId,
            string? Name,
            DateTime? StartDate,
            DateTime? EndDate
        );
        Task<ActivityTask?> GetTaskByIdAsync(Guid taskId);
        Task<List<ActivityTask>?> GetTaskByPhaseIdAsync(Guid phaseId, ActivityTaskStatus status);

        Task<ActivityTask?> GetTaskDetailAsync(Guid ownerId, Guid? taskId);
        Task<int> UpdateTaskAsync(ActivityTask task);
    }
}
