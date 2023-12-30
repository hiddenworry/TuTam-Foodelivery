using DataAccess.DbContextData;
using DataAccess.Entities;
using DataAccess.EntityEnums;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Repositories.Implements
{
    public class TargetProcessRepository : ITargetProcessRepository
    {
        private readonly FoodDonationDeliveryDbContext _context;

        public TargetProcessRepository(FoodDonationDeliveryDbContext context)
        {
            _context = context;
        }

        public async Task<int> CreateTargetProcessesAsync(List<TargetProcess> targetProcesses)
        {
            int rs = 0;
            foreach (TargetProcess item in targetProcesses)
            {
                rs += await CreateTargetProcessAsync(item);
            }
            return rs;
        }

        public async Task<int> CreateTargetProcessAsync(TargetProcess targetProcess)
        {
            await _context.TargetProcesses.AddAsync(targetProcess);
            return await _context.SaveChangesAsync() > 0 ? 1 : 0;
        }

        public async Task<List<TargetProcess>> FindTargetProcessesByActivityIdAsync(Guid activityId)
        {
            return await _context.TargetProcesses
                .Where(tp => tp.ActivityId == activityId)
                .ToListAsync();
        }

        public async Task<
            List<TargetProcess>
        > FindStillTakingPlaceActivityTargetProcessesByBranchIdAsync(Guid branchId)
        {
            return await _context.TargetProcesses
                .Include(tp => tp.Activity)
                .ThenInclude(a => a.ActivityBranches)
                .Where(
                    tp =>
                        tp.Activity.Status == ActivityStatus.STARTED
                        && tp.Activity.ActivityBranches.Any(ab => ab.BranchId == branchId)
                )
                .ToListAsync();
        }

        public async Task<TargetProcess?> FindTargetProcessByActivityIdAndItemIdAsync(
            Guid activityId,
            Guid itemId
        )
        {
            return await _context.TargetProcesses.FirstOrDefaultAsync(
                tp => tp.ActivityId == activityId && tp.ItemId == itemId
            );
        }

        public async Task<int> UpdateTargetProcessAsync(TargetProcess targetProcess)
        {
            _context.Update(targetProcess);
            return await _context.SaveChangesAsync() > 0 ? 1 : 0;
        }
    }
}
