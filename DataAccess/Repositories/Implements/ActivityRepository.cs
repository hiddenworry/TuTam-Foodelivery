using DataAccess.DbContextData;
using DataAccess.Entities;
using DataAccess.EntityEnums;
using DataAccess.ModelsEnum;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Repositories.Implements
{
    public class ActivityRepository : IActivityRepository
    {
        private readonly FoodDonationDeliveryDbContext _context;

        public ActivityRepository(FoodDonationDeliveryDbContext context)
        {
            _context = context;
        }

        public async Task<int> CreateActivityAsync(Activity activity)
        {
            await _context.Activities.AddAsync(activity);
            return await _context.SaveChangesAsync();
        }

        public async Task<Activity?> FindActivityByIdAsync(Guid id)
        {
            return await _context.Activities
                .Include(a => a.ActivityTypeComponents)
                .Include(a => a.ActivityBranches)
                .ThenInclude(ab => ab.Branch)
                .Include(a => a.TargetProcesses)
                .FirstOrDefaultAsync(a => a.Id == id);
        }

        public async Task<Activity?> FindActivityByIdForDetailAsync(Guid id)
        {
            return await _context.Activities
                .Include(a => a.ActivityTypeComponents)
                .ThenInclude(a => a.ActivityType)
                .Include(a => a.ActivityMembers)
                .Include(a => a.ActivityBranches)
                .ThenInclude(ab => ab.Branch)
                .FirstOrDefaultAsync(a => a.Id == id);
        }

        public async Task<Activity?> FindActivityByNameIgnoreCaseAsync(string name)
        {
            return await _context.Activities.FirstOrDefaultAsync(
                a => a.Name.ToUpper().Equals(name.ToUpper())
            );
        }

        public async Task<List<Activity>> GetActivitiesAsync(
            string? name,
            ActivityStatus? status,
            ActivityScope? scope,
            List<Guid>? activityTypeIds,
            DateTime? startDate,
            DateTime? endDate,
            Guid? userId,
            Guid? branchId,
            string? address,
            string? userRoleName
        )
        {
            List<Activity> activities = await _context.Activities
                .Include(a => a.ActivityTypeComponents)
                .ThenInclude(a => a.ActivityType)
                .Include(a => a.ActivityMembers)
                .Include(a => a.ActivityBranches)
                .ThenInclude(ab => ab.Branch)
                .Where(
                    a =>
                        (name != null ? a.Name.ToUpper().Contains(name.ToUpper()) : true)
                        && (
                            status != null
                                ? a.Status == status
                                : (
                                    (
                                        userRoleName == RoleEnum.BRANCH_ADMIN.ToString()
                                        || userRoleName == RoleEnum.SYSTEM_ADMIN.ToString()
                                    )
                                        ? true
                                        : a.Status != ActivityStatus.INACTIVE
                                )
                        )
                        && (
                            scope != null
                                ? a.Scope == scope
                                : (
                                    (
                                        userRoleName == RoleEnum.BRANCH_ADMIN.ToString()
                                        || userRoleName == RoleEnum.SYSTEM_ADMIN.ToString()
                                    )
                                        ? true
                                        : a.Scope != ActivityScope.INTERNAL
                                )
                        )
                        && (
                            endDate != null
                                ? (
                                    a.StartDate != null
                                        ? a.StartDate <= endDate
                                        : a.EstimatedStartDate <= endDate
                                )
                                : true
                        )
                        && (
                            startDate != null
                                ? (
                                    a.EndDate != null
                                        ? a.EndDate >= startDate
                                        : a.EstimatedEndDate >= startDate
                                )
                                : true
                        )
                        && (
                            userId != null
                                ? a.ActivityMembers.Any(
                                    am =>
                                        am.UserId == userId
                                        && am.Status == ActivityMemberStatus.ACTIVE
                                )
                                : true
                        )
                        && (
                            branchId != null
                                ? (a.ActivityBranches.Any(ab => ab.BranchId == branchId))
                                : true
                        )
                        && (
                            address != null
                                ? (
                                    a.Address != null
                                        ? a.Address.ToUpper().Contains(address.ToUpper())
                                        : (
                                            a.ActivityBranches.Any(
                                                ab =>
                                                    ab.Branch.Address
                                                        .ToUpper()
                                                        .Contains(address.ToUpper())
                                            )
                                        )
                                )
                                : true
                        )
                )
                .ToListAsync();

            return activities
                .Where(
                    a =>
                        (activityTypeIds != null && activityTypeIds.Count > 0)
                            ? activityTypeIds.All(
                                id =>
                                    a.ActivityTypeComponents
                                        .Select(atc => atc.ActivityTypeId)
                                        .Contains(id)
                            )
                            : true
                )
                .ToList();
        }

        public async Task<int> UpdateActivityAsync(Activity activity)
        {
            _context.Activities.Update(activity);
            return await _context.SaveChangesAsync();
        }

        public async Task<int> CountActivityByStatus(
            ActivityStatus? status,
            DateTime? startDate,
            DateTime? endDate,
            Guid? branchId
        )
        {
            var query = _context.Activities.Include(a => a.ActivityBranches).AsQueryable();

            if (status != null)
            {
                // Filter by status
                query = query.Where(a => a.Status == status);
            }

            if (startDate != null && endDate == null)
            {
                query = query.Where(a => a.CreatedDate >= startDate);
            }

            if (endDate != null && startDate == null)
            {
                query = query.Where(a => a.CreatedDate <= endDate);
            }
            if (endDate != null && startDate != null)
            {
                query = query.Where(a => a.CreatedDate <= endDate && a.CreatedDate >= startDate);
            }
            if (branchId != null)
            {
                query = query.Where(a => a.ActivityBranches.Any(a => a.BranchId == branchId));
            }

            int count = await query.CountAsync();

            return count;
        }

        public async Task<List<Activity>> FindActivityByItemId(Guid itemId, Guid userId)
        {
            var query = _context.Activities
                .Include(a => a.TargetProcesses)
                .Include(a => a.ActivityBranches)
                .ThenInclude(a => a.Branch)
                .AsQueryable();

            query = query.Where(
                a =>
                    a.ActivityBranches.Any(a => a.Branch.BranchAdminId == userId)
                    && a.Status == ActivityStatus.STARTED
            );
            var rs = await query
                .Where(a => a.TargetProcesses.Any(a => a.ItemId == itemId))
                .ToListAsync();
            return rs;
        }
    }
}
