using DataAccess.Entities;
using DataAccess.EntityEnums;

namespace DataAccess.Repositories
{
    public interface IRolePermissionRepository
    {
        Task<List<Permission>?> GetPermissionsByRoleIdAsync(Guid roleId);
        Task<List<RolePermission>?> GetRolePermissionsByRoleIdAsync(Guid roleId);
        Task<int> UpdateRolePermissionsByRoleIdAsync(
            Guid roleId,
            Guid permissionId,
            RolePermissionStatus status
        );
    }
}
