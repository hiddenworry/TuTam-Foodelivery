using DataAccess.DbContextData;
using DataAccess.Entities;
using DataAccess.EntityEnums;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Repositories.Implements
{
    public class ActivityMemberRepository : IActivityMemberRepository
    {
        private readonly FoodDonationDeliveryDbContext _context;

        public ActivityMemberRepository(FoodDonationDeliveryDbContext context)
        {
            _context = context;
        }

        public async Task<ActivityMember?> FindActiveActivityMemberByActivityIdAndUserIdAsync(
            Guid activityId,
            Guid userId
        )
        {
            return await _context.ActivityMembers.FirstOrDefaultAsync(
                am =>
                    am.ActivityId == activityId
                    && am.UserId == userId
                    && am.Status == ActivityMemberStatus.ACTIVE
            );
        }

        public async Task<int> CreateActivityMemberAsync(ActivityMember activityMember)
        {
            await _context.ActivityMembers.AddAsync(activityMember);
            return await _context.SaveChangesAsync();
        }

        public async Task<ActivityMember> CreateActivityMemberReturnObjectAsync(
            ActivityMember activityMember
        )
        {
            var rs = await _context.ActivityMembers.AddAsync(activityMember);
            await _context.SaveChangesAsync();
            return rs.Entity;
        }

        public async Task<int> UpdateActivityMemberAsync(ActivityMember activityMember)
        {
            _context.ActivityMembers.Update(activityMember);
            return await _context.SaveChangesAsync();
        }

        public async Task<int> DeleteActivityMemberAsync(ActivityMember activityMember)
        {
            _context.ActivityMembers.Remove(activityMember);
            return await _context.SaveChangesAsync();
        }

        public async Task<ActivityMember?> FindActivityMemberByIdAsync(Guid activityMemberId)
        {
            return await _context.ActivityMembers
                .Include(a => a.Activity)
                .FirstOrDefaultAsync(am => am.Id == activityMemberId);
        }

        public async Task<ActivityMember?> FindActivityMemberByActivityIdAndUserIdAsync(
            Guid activityId,
            Guid userId
        )
        {
            return await _context.ActivityMembers.FirstOrDefaultAsync(
                am => am.ActivityId == activityId && am.UserId == userId
            );
        }

        public async Task<List<ActivityMember>?> FindMemberByUserIdAndActivityIdAsync(
            Guid userId,
            Guid activityId,
            ActivityMemberStatus status
        )
        {
            return await _context.ActivityMembers
                .Where(a => a.UserId == userId && a.ActivityId == activityId && a.Status == status)
                .ToListAsync();
        }

        public async Task<List<ActivityMember>?> FindMemberApplicationAsync(
            Guid? ownerId,
            Guid? activityId,
            ActivityMemberStatus? status
        )
        {
            var query = _context.ActivityMembers
                .Include(u => u.User)
                .Include(u => u.Activity)
                .ThenInclude(u => u.ActivityBranches)
                .ThenInclude(u => u.Branch)
                .AsQueryable();
            if (ownerId != null)
            {
                query = query.Where(
                    a => a.Activity.ActivityBranches.Any(u => u.Branch.BranchAdminId == ownerId)
                );
            }

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

        public async Task<List<ActivityMember>?> FindMemberByActivityIdAsync(
            Guid activityId,
            ActivityMemberStatus status
        )
        {
            return await _context.ActivityMembers
                .Include(a => a.User)
                .Where(a => a.ActivityId == activityId && a.Status == status)
                .ToListAsync();
        }

        public async Task<List<ActivityMember>?> FindMemberByActivityIdAndUserIdAsync(
            Guid activityId,
            Guid userId
        )
        {
            return await _context.ActivityMembers
                .Where(a => a.ActivityId == activityId && a.UserId == userId)
                .ToListAsync();
        }
    }
}
