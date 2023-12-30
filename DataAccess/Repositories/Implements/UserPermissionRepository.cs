using DataAccess.DbContextData;
using DataAccess.Entities;
using DataAccess.EntityEnums;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DataAccess.Repositories.Implements
{
    public class UserPermissionRepository : IUserPermissionRepository
    {
        private readonly FoodDonationDeliveryDbContext _context;

        public UserPermissionRepository(FoodDonationDeliveryDbContext context)
        {
            _context = context;
        }

        public async Task<UserPermission?> UpdateUserPermissionAsync(
            Guid userId,
            Guid permissionId,
            UserPermissionStatus status
        )
        {
            UserPermission? userPermission = await _context.UserPermissions.FirstOrDefaultAsync(
                u => u.UserId == userId && u.PermissionId == permissionId
            );
            if (userPermission != null)
            {
                userPermission.Status = status;
                _context.Entry(userPermission).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                    return userPermission;
                }
                catch (DbUpdateConcurrencyException)
                {
                    return null;
                }
            }
            return null;
        }

        public async Task<List<UserPermission>?> GetPermissionsByUserIdAsync(Guid userId)
        {
            List<UserPermission>? permissions = await _context.UserPermissions
                .Include(p => p.Permission)
                .Where(rp => rp.UserId == userId)
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

        public async Task<IReadOnlyCollection<Claim>> GetClaimsByUserIdAsync(Guid userId)
        {
            var permissions = await _context.UserPermissions
                .Where(up => up.UserId == userId)
                .Select(up => up.Permission)
                .ToListAsync();
            var claims = new List<Claim>();
            foreach (var permission in permissions)
            {
                if (permission != null && permission.Name != null)
                {
                    var claim = new Claim("permissions", permission.Name);
                    claims.Add(claim);
                }
            }
            // Trả về danh sách claims dưới dạng IReadOnlyCollection<Claim>
            return claims.AsReadOnly();
        }

        public async Task<UserPermission?> AddUserPermissionAsync(
            Guid userId,
            Guid permissionId,
            UserPermissionStatus status
        )
        {
            var existingUserPermission = await _context.UserPermissions.FirstOrDefaultAsync(
                u => u.UserId == userId && u.PermissionId == permissionId
            );

            if (existingUserPermission != null)
            {
                return null;
            }
            var newUserPermission = new UserPermission
            {
                UserId = userId,
                PermissionId = permissionId,
                Status = status
            };
            _context.UserPermissions.Add(newUserPermission);
            try
            {
                await _context.SaveChangesAsync();
                return newUserPermission;
            }
            catch (DbUpdateConcurrencyException)
            {
                return null;
            }
        }

        public async Task<int> UpdateUserPermissionsByRoleIdAsync(
            Guid roleId,
            Guid permissionId,
            UserPermissionStatus status
        )
        {
            _context.UserPermissions
                .Include(r => r.User)
                .Where(r => r.User.RoleId == roleId && r.PermissionId == permissionId)
                .ToList()
                .ForEach(r => r.Status = status);

            return await _context.SaveChangesAsync();
        }

        public async Task<UserPermission?> DeleteUserPermissionAsync(Guid userId, Guid permissionId)
        {
            UserPermission? userPermission = await _context.UserPermissions.FirstOrDefaultAsync(
                u => u.UserId == userId && u.PermissionId == permissionId
            );
            if (userPermission != null)
            {
                _context.UserPermissions.Remove(userPermission);

                try
                {
                    await _context.SaveChangesAsync();
                    return userPermission;
                }
                catch (DbUpdateConcurrencyException)
                {
                    return null;
                }
            }
            return null;
        }

        public async Task<UserPermission?> UpdateOrCreateUserPermissionAsync(
            Guid userId,
            Guid permissionId,
            UserPermissionStatus status
        )
        {
            try
            {
                UserPermission? userPermission = await _context.UserPermissions.FirstOrDefaultAsync(
                    u => u.UserId == userId && u.PermissionId == permissionId
                );
                if (userPermission != null)
                {
                    userPermission.Status = status;
                    _context.Entry(userPermission).State = EntityState.Modified;
                }
                else
                {
                    UserPermission tmp = new UserPermission
                    {
                        PermissionId = permissionId,
                        UserId = userId,
                        Status = status
                    };

                    _context.Add(tmp);
                    userPermission = tmp;
                }

                await _context.SaveChangesAsync();
                return userPermission;
            }
            catch (DbUpdateConcurrencyException)
            {
                return null;
            }
        }
    }
}
