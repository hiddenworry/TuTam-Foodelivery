using DataAccess.Models.Requests;
using DataAccess.Models.Responses;
using DataAccess.ModelsEnum;

namespace BusinessLogic.Services
{
    public interface IRolePermissionService
    {
        Task<CommonResponse> GetPermissionsByRoleAsync(
            Guid roleId,
            int? page,
            int? pageSize,
            SortType sortType
        );
        Task<CommonResponse> UpdatePermissionsByRoleAsync(RolePermissionUpdatingRequest request);
    }
}
