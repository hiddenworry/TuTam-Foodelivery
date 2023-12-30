using DataAccess.Models.Requests;
using DataAccess.Models.Responses;
using DataAccess.ModelsEnum;

namespace BusinessLogic.Services
{
    public interface IUserPermissionService
    {
        Task<CommonResponse> GetPermissionsByUserAsync(
            Guid userId,
            int? page,
            int? pageSize,
            SortType? sortType
        );
        Task<CommonResponse> UpdateUserPermissionAsync(UserPermissionRequest request);
    }
}
