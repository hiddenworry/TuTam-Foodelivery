using DataAccess.DbContextData;
using DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Repositories.Implements
{
    public class ActivityTypeComponentRepository : IActivityTypeComponentRepository
    {
        private readonly FoodDonationDeliveryDbContext _context;

        public ActivityTypeComponentRepository(FoodDonationDeliveryDbContext context)
        {
            _context = context;
        }

        public async Task<int> CreateActivityTypeComponentAsync(
            ActivityTypeComponent activityTypeComponent
        )
        {
            await _context.ActivityTypeComponents.AddAsync(activityTypeComponent);
            return await _context.SaveChangesAsync();
        }

        public async Task<int> CreateActivityTypeComponentsAsync(
            List<ActivityTypeComponent> activityTypeComponents
        )
        {
            int rs = 0;
            foreach (ActivityTypeComponent item in activityTypeComponents)
            {
                rs += await CreateActivityTypeComponentAsync(item) > 0 ? 1 : 0;
            }
            return rs;
        }

        public async Task<int> DeleteActivityTypeComponentAsync(
            ActivityTypeComponent activityTypeComponent
        )
        {
            _context.ActivityTypeComponents.Remove(activityTypeComponent);
            return await _context.SaveChangesAsync();
        }

        public async Task<int> DeleteActivityTypeComponentsAsync(
            List<ActivityTypeComponent> activityTypeComponents
        )
        {
            int rs = 0;
            foreach (ActivityTypeComponent item in activityTypeComponents)
            {
                rs += await DeleteActivityTypeComponentAsync(item) > 0 ? 1 : 0;
            }
            return rs;
        }

        public async Task<ActivityTypeComponent?> FindActivityComponentByActivityIdAndActivityTypeIdAsync(
            Guid activityId,
            Guid activityTypeId
        )
        {
            return await _context.ActivityTypeComponents.FirstOrDefaultAsync(
                atc => atc.ActivityId == activityId && atc.ActivityTypeId == activityTypeId
            );
        }
    }
}
