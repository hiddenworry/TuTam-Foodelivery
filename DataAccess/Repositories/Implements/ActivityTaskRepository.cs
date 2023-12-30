using DataAccess.DbContextData;
using DataAccess.Entities;
using DataAccess.EntityEnums;
using DataAccess.ModelsEnum;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Repositories.Implements
{
    public class ActivityTaskRepository : IActivityTaskRepository
    {
        private readonly FoodDonationDeliveryDbContext _context;

        public ActivityTaskRepository(FoodDonationDeliveryDbContext context)
        {
            _context = context;
        }

        public async Task<int> CreateTaskAsync(ActivityTask task)
        {
            _context.ActivityTasks.Add(task);
            return await _context.SaveChangesAsync();
        }

        public async Task<ActivityTask?> CreateTaskReturnObjectAsync(ActivityTask task)
        {
            var rs = _context.ActivityTasks.Add(task);
            await _context.SaveChangesAsync();
            return rs.Entity;
        }

        public async Task<int> UpdateTaskAsync(ActivityTask task)
        {
            _context.ActivityTasks.Update(task);
            return await _context.SaveChangesAsync();
        }

        public async Task<List<ActivityTask>?> GetTaskByPhaseIdAsync(
            Guid phaseId,
            ActivityTaskStatus status
        )
        {
            return await _context.ActivityTasks
                .Where(p => p.PhaseId == phaseId && p.Status == status)
                .ToListAsync();
        }

        public async Task<ActivityTask?> GetTaskByIdAsync(Guid taskId)
        {
            return await _context.ActivityTasks
                .Include(a => a.Phase)
                .Include(a => a.RoleTasks)
                .Where(p => p.Id == taskId)
                .FirstOrDefaultAsync();
        }

        public async Task<List<ActivityTask>?> GetTaskAsync(
            Guid ownerId,
            Guid? activityId,
            Guid? phaseId,
            string? Name,
            DateTime? StartDate,
            DateTime? EndDate
        )
        {
            var query = _context.ActivityTasks
                .Include(t => t.Phase)
                .ThenInclude(t => t.Activity)
                .ThenInclude(t => t.ActivityMembers)
                .Include(t => t.RoleTasks)
                .ThenInclude(a => a.ActivityRole)
                .Include(t => t.Phase)
                .ThenInclude(t => t.Activity)
                .ThenInclude(t => t.ActivityBranches)
                .ThenInclude(t => t.Branch)
                .AsQueryable();

            var userRole = await _context.Users
                .Where(u => u.Id == ownerId)
                .Select(u => u.Role.Name)
                .FirstOrDefaultAsync();

            if (userRole == RoleEnum.BRANCH_ADMIN.ToString())
            {
                query = query.Where(
                    a =>
                        a.Phase.Activity.ActivityBranches.Any(
                            u => u.Branch.BranchAdminId == ownerId
                        )
                );
            }
            else if (userRole == RoleEnum.SYSTEM_ADMIN.ToString())
            {
                var roleMemberQuery = _context.RoleMembers
                    .Include(a => a.ActivityMember)
                    .Include(a => a.ActivityRole)
                    .Select(r => r.ActivityRole.Id);
                var x = roleMemberQuery.Count();
                query = query.Where(
                    a =>
                        a.Phase.Activity.ActivityRoles.Any(
                            ar =>
                                ar.RoleTasks.Any(rt => roleMemberQuery.Contains(rt.ActivityRoleId))
                        )
                );
            }
            else
            {
                var roleMemberQuery = _context.RoleMembers
                    .Include(a => a.ActivityMember)
                    .Include(a => a.ActivityRole)
                    .Where(r => r.ActivityMember.UserId == ownerId)
                    .Select(r => r.ActivityRole.Id);
                var x = roleMemberQuery.Count();
                query = query.Where(
                    a =>
                        a.Phase.Activity.ActivityRoles.Any(
                            ar =>
                                ar.RoleTasks.Any(rt => roleMemberQuery.Contains(rt.ActivityRoleId))
                        )
                );
            }

            if (activityId != null)
            {
                query = query.Where(t => t.Phase.ActivityId == activityId);
            }
            if (phaseId != null)
            {
                query = query.Where(t => t.PhaseId == phaseId);
            }
            if (!string.IsNullOrEmpty(Name))
            {
                query = query.Where(t => t.Name.ToLower().Contains(Name.ToLower()));
            }
            if (StartDate != null)
            {
                query = query.Where(t => t.StartDate >= StartDate);
            }
            if (EndDate != null)
            {
                query = query.Where(t => t.EndDate <= EndDate);
            }

            return await query.ToListAsync();
        }

        public async Task<int> DeleteTaskAsync(ActivityTask task)
        {
            List<RoleTask>? roleTasks = await _context.RoleTasks
                .Where(a => a.ActivityTaskId == task.Id)
                .ToListAsync();
            if (roleTasks != null && roleTasks.Count > 0)
            {
                foreach (RoleTask roleTask in task.RoleTasks)
                {
                    _context.RoleTasks.Remove(roleTask);
                }
            }

            _context.ActivityTasks.Remove(task);
            return await _context.SaveChangesAsync();
        }

        public async Task<ActivityTask?> GetTaskDetailAsync(Guid ownerId, Guid? taskId)
        {
            var query = _context.ActivityTasks
                .Include(t => t.Phase)
                .ThenInclude(t => t.Activity)
                .ThenInclude(t => t.ActivityMembers)
                .Include(t => t.RoleTasks)
                .ThenInclude(a => a.ActivityRole)
                .Include(t => t.Phase)
                .ThenInclude(t => t.Activity)
                .ThenInclude(t => t.ActivityBranches)
                .ThenInclude(t => t.Branch)
                .AsQueryable();

            var userRole = await _context.Users
                .Where(u => u.Id == ownerId)
                .Select(u => u.Role.Name)
                .FirstOrDefaultAsync();

            if (userRole == RoleEnum.BRANCH_ADMIN.ToString())
            {
                query = query.Where(
                    a =>
                        a.Phase.Activity.ActivityBranches.Any(
                            u => u.Branch.BranchAdminId == ownerId
                        )
                );
            }
            else
            {
                var roleMemberQuery = _context.RoleMembers
                    .Include(a => a.ActivityMember)
                    .Include(a => a.ActivityRole)
                    .Where(r => r.ActivityMember.UserId == ownerId)
                    .Select(r => r.ActivityRole.Id);
                var x = roleMemberQuery.Count();
                query = query.Where(
                    a =>
                        a.Phase.Activity.ActivityRoles.Any(
                            ar =>
                                ar.RoleTasks.Any(rt => roleMemberQuery.Contains(rt.ActivityRoleId))
                        )
                );
            }

            if (taskId != null)
            {
                query = query.Where(t => t.Id == taskId);
            }

            return await query.FirstOrDefaultAsync();
        }
    }
}
