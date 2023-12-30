using DataAccess.Entities;

namespace DataAccess.Repositories
{
    public interface INotificationRepository
    {
        Task<int> CreateNotificationAsync(Notification notification);
        Task<Notification?> FindNotificationByIdAndUserIdAsync(Guid notificationId, Guid userId);
        Task<Notification?> FindNotificationByIdAsync(Guid notificationId);
        Task<List<Notification>?> FindNotificationByUserIdAsync(Guid userId);
        Task<int> UpdateNotificationAsync(Notification notification);
    }
}
