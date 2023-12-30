using DataAccess.DbContextData;
using DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Repositories.Implements
{
    public class ActivityTypeRepository : IActivityTypeRepository
    {
        private readonly FoodDonationDeliveryDbContext _context;

        public ActivityTypeRepository(FoodDonationDeliveryDbContext context)
        {
            _context = context;
        }

        public async Task<ActivityType?> FindActivityTypeByIdAsync(Guid id)
        {
            return await _context.ActivityTypes.FirstOrDefaultAsync(a => a.Id == id);
        }

        public async Task<List<ActivityType>> GetAllActivityTypesAsync()
        {
            return await _context.ActivityTypes.ToListAsync();
        }
    }
}
