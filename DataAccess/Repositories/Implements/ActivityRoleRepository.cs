using DataAccess.DbContextData;
using DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Repositories.Implements
{
    public class ActivityRoleRepository : IActivityRoleRepository
    {
        private readonly FoodDonationDeliveryDbContext _context;

        public ActivityRoleRepository(FoodDonationDeliveryDbContext context)
        {
            _context = context;
        }

        public async Task<int> CreateActivityRole(ActivityRole activityRole)
        {
            _context.ActivityRoles.Add(activityRole);
            return await _context.SaveChangesAsync();
        }

        public async Task<int> UpdateActivityRole(ActivityRole activityRole)
        {
            _context.ActivityRoles.Update(activityRole);
            return await _context.SaveChangesAsync();
        }

        public async Task<List<ActivityRole>?> GetListActivityRole(Guid activityId)
        {
            return await _context.ActivityRoles
                .Where(a => a.ActivityId == activityId)
                .ToListAsync();
        }

        public async Task<ActivityRole?> GetActivityRoleById(Guid activityRoleId)
        {
            return await _context.ActivityRoles
                .Where(a => a.Id == activityRoleId)
                .FirstOrDefaultAsync();
        }
    }
}
