using DataAccess.DbContextData;
using DataAccess.Entities;
using DataAccess.EntityEnums;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Repositories.Implements
{
    public class ActivityFeedbackRepository : IActivityFeedbackRepository
    {
        private readonly FoodDonationDeliveryDbContext _context;

        public ActivityFeedbackRepository(FoodDonationDeliveryDbContext context)
        {
            _context = context;
        }

        public async Task<int> CreateFeedbackAsync(ActivityFeedback feedback)
        {
            await _context.ActivityFeedbacks.AddAsync(feedback);
            return await _context.SaveChangesAsync();
        }

        public async Task<int> UpdateFeedbackAsync(ActivityFeedback feedback)
        {
            try
            {
                _context.ActivityFeedbacks.Update(feedback);
                return await _context.SaveChangesAsync();
            }
            catch
            {
                return 0;
            }
        }

        public async Task<ActivityFeedback?> FindActivityFeedbackByIdAsync(Guid activityFeedbackId)
        {
            return await _context.ActivityFeedbacks.FirstOrDefaultAsync(
                a => a.Id == activityFeedbackId
            );
        }

        public async Task<ActivityFeedback?> FindActivityFeedbackAsync(
            Guid userId,
            Guid activityId,
            ActivityFeedbackStatus status
        )
        {
            return await _context.ActivityFeedbacks.FirstOrDefaultAsync(
                a => a.UserId == userId && a.ActivityId == activityId && status == a.Status
            );
        }

        public async Task<List<ActivityFeedback>?> GetListActivityFeedbackAsync(
            Guid? activityId,
            ActivityFeedbackStatus? status
        )
        {
            var query = _context.ActivityFeedbacks.AsQueryable();
            if (activityId != null)
            {
                query = query.Where(a => a.ActivityId == activityId);
            }
            if (status != null)
            {
                query = query.Where(a => a.Status == status);
            }
            return await query.ToListAsync();
        }
    }
}
