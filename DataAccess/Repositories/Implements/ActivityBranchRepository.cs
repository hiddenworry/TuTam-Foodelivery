using DataAccess.DbContextData;
using DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Repositories.Implements
{
    public class ActivityBranchRepository : IActivityBranchRepository
    {
        private readonly FoodDonationDeliveryDbContext _context;

        public ActivityBranchRepository(FoodDonationDeliveryDbContext context)
        {
            _context = context;
        }

        public async Task<int> CreateActivityBranchesAsync(List<ActivityBranch> activityBranches)
        {
            int rs = 0;
            foreach (ActivityBranch item in activityBranches)
            {
                rs += await CreateActivityBranchAsync(item) > 0 ? 1 : 0;
            }
            return rs;
        }

        public async Task<int> CreateActivityBranchAsync(ActivityBranch activityBranch)
        {
            await _context.ActivityBranches.AddAsync(activityBranch);
            return await _context.SaveChangesAsync();
        }

        public async Task<int> DeleteActivityBranchesAsync(List<ActivityBranch> activityBranches)
        {
            int rs = 0;
            foreach (ActivityBranch item in activityBranches)
            {
                rs += await DeleteActivityBranchAsync(item) > 0 ? 1 : 0;
            }
            return rs;
        }

        public async Task<int> DeleteActivityBranchAsync(ActivityBranch activityBranch)
        {
            _context.ActivityBranches.Remove(activityBranch);
            return await _context.SaveChangesAsync();
        }

        public async Task<ActivityBranch?> FindActivityBranchByActivityIdAndBranchIdAsync(
            Guid activityId,
            Guid branchId
        )
        {
            return await _context.ActivityBranches.FirstOrDefaultAsync(
                a => a.ActivityId == activityId && a.BranchId == branchId
            );
        }
    }
}
