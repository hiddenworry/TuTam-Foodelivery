using DataAccess.DbContextData;
using DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Repositories.Implements
{
    public class NotificationRepository : INotificationRepository
    {
        private readonly FoodDonationDeliveryDbContext _context;

        public NotificationRepository(FoodDonationDeliveryDbContext context)
        {
            _context = context;
        }

        public async Task<int> CreateNotificationAsync(Notification notification)
        {
            var rs = _context.Notifications.Add(notification);
            return await _context.SaveChangesAsync();
        }

        public async Task<int> UpdateNotificationAsync(Notification notification)
        {
            var rs = _context.Notifications.Update(notification);
            return await _context.SaveChangesAsync();
        }

        public async Task<List<Notification>?> FindNotificationByUserIdAsync(Guid userId)
        {
            return await _context.Notifications
                .Where(n => n.ReceiverId == userId.ToString())
                .ToListAsync();
        }

        public async Task<Notification?> FindNotificationByIdAsync(Guid notificationId)
        {
            return await _context.Notifications
                .Where(n => n.Id == notificationId)
                .FirstOrDefaultAsync();
        }

        public async Task<Notification?> FindNotificationByIdAndUserIdAsync(
            Guid notificationId,
            Guid userId
        )
        {
            return await _context.Notifications
                .Where(n => n.Id == notificationId && n.ReceiverId == userId.ToString())
                .FirstOrDefaultAsync();
        }
    }
}
