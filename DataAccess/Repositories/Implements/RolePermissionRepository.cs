using DataAccess.DbContextData;
using DataAccess.Entities;
using DataAccess.EntityEnums;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Repositories.Implements
{
    public class RolePermissionRepository : IRolePermissionRepository
    {
        private readonly FoodDonationDeliveryDbContext _context;

        public RolePermissionRepository(FoodDonationDeliveryDbContext context)
        {
            _context = context;
        }

        public async Task<List<RolePermission>?> GetRolePermissionsByRoleIdAsync(Guid roleId)
        {
            List<RolePermission> rs = await _context.RolePermissions
                .Include(p => p.Permission)
                .Where(p => p.RoleId == roleId)
                .ToListAsync();

            if (rs != null)
            {
                return rs;
            }
            else
                return null;
        }

        public async Task<List<Permission>?> GetPermissionsByRoleIdAsync(Guid roleId)
        {
            List<Permission>? permissions = await _context.RolePermissions
                .Where(rp => rp.RoleId == roleId)
                .Select(rp => rp.Permission)
                .ToListAsync();

            if (permissions != null)
            {
                return permissions;
            }
            else
            {
                return null;
            }
        }

        public async Task<int> UpdateRolePermissionsByRoleIdAsync(
            Guid roleId,
            Guid permissionId,
            RolePermissionStatus status
        )
        {
            _context.RolePermissions
                .Where(r => r.RoleId == roleId && r.PermissionId == permissionId)
                .ToList()
                .ForEach(r => r.Status = status);

            return await _context.SaveChangesAsync();
        }
    }
}
