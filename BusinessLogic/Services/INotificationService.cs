using DataAccess.EntityEnums;
using DataAccess.Models.Requests;
using DataAccess.Models.Responses;

namespace BusinessLogic.Services
{
    public interface INotificationService
    {
        Task<CommonResponse> GetNotification(
            Guid userId,
            NotificationStatus? status,
            int? page,
            int? pageSize
        );
        Task<CommonResponse> UpdateNotification(NotificationUpdatingRequest request, Guid userId);
    }
}
