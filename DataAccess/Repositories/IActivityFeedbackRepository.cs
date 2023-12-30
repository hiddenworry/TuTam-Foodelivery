using DataAccess.Entities;
using DataAccess.EntityEnums;

namespace DataAccess.Repositories
{
    public interface IActivityFeedbackRepository
    {
        Task<int> CreateFeedbackAsync(ActivityFeedback feedback);
        Task<ActivityFeedback?> FindActivityFeedbackAsync(
            Guid userId,
            Guid activityId,
            ActivityFeedbackStatus status
        );
        Task<ActivityFeedback?> FindActivityFeedbackByIdAsync(Guid activityFeedbackId);
        Task<List<ActivityFeedback>?> GetListActivityFeedbackAsync(
            Guid? activityId,
            ActivityFeedbackStatus? status
        );
        Task<int> UpdateFeedbackAsync(ActivityFeedback feedback);
    }
}
