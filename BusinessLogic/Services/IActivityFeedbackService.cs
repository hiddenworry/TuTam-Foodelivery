using DataAccess.EntityEnums;
using DataAccess.Models.Requests;
using DataAccess.Models.Responses;

namespace BusinessLogic.Services
{
    public interface IActivityFeedbackService
    {
        Task<CommonResponse> CheckActivityIsFeedbacked(Guid activityId);
        Task<CommonResponse> CheckUserIsFeedbacked(Guid userId, Guid activityId);

        Task<CommonResponse> CreatFeedback(Guid activityId, Guid userId, string role);

        Task<CommonResponse> GetFeedback(
            int? page,
            int? pageSize,
            Guid activityId,
            ActivityFeedbackStatus? status,
            Guid userId,
            string role
        );
        Task<CommonResponse> SendFeedback(Guid userId, ActivityFeedbackCreatingRequest request);
    }
}
