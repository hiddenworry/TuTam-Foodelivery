using DataAccess.Entities;
using DataAccess.EntityEnums;
using System.Security.Claims;

namespace DataAccess.Repositories
{
    public interface IUserPermissionRepository
    {
        Task<UserPermission?> AddUserPermissionAsync(
            Guid userId,
            Guid permissionId,
            UserPermissionStatus status
        );
        Task<UserPermission?> DeleteUserPermissionAsync(Guid userId, Guid permissionId);
        Task<IReadOnlyCollection<Claim>> GetClaimsByUserIdAsync(Guid userId);
        Task<List<UserPermission>?> GetPermissionsByUserIdAsync(Guid userId);
        Task<UserPermission?> UpdateOrCreateUserPermissionAsync(
            Guid userId,
            Guid permissionId,
            UserPermissionStatus status
        );
        Task<UserPermission?> UpdateUserPermissionAsync(
            Guid userId,
            Guid permissionId,
            UserPermissionStatus status
        );

        Task<int> UpdateUserPermissionsByRoleIdAsync(
            Guid roleId,
            Guid permissionId,
            UserPermissionStatus status
        );
    }
}
