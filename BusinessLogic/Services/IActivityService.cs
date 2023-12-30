using DataAccess.EntityEnums;
using DataAccess.Models.Requests;
using DataAccess.Models.Requests.Enum;
using DataAccess.Models.Responses;
using DataAccess.ModelsEnum;

namespace BusinessLogic.Services
{
    public interface IActivityService
    {
        Task<CommonResponse> CountActivityByAllStatus(
            DateTime? startDate,
            DateTime? endDate,
            Guid? branchId,
            string? roleEnum,
            Guid? branchAdminId
        );

        Task<CommonResponse> CountActivityByStatus(
            DateTime startDate,
            DateTime endDate,
            ActivityStatus? status,
            TimeFrame timeFrame,
            Guid? branchId,
            string? roleEnum,
            Guid? branchAdminId
        );
        Task<CommonResponse> CreateActivityAsync(
            ActivityCreatingRequest activityCreatingRequest,
            Guid userId,
            string userRoleName
        );
        Task<CommonResponse> DeactivateActivityAsync(Guid id, Guid userId, string userRoleName);
        Task<CommonResponse> GetActivitiesAsync(
            string? name,
            ActivityStatus? status,
            ActivityScope? scope,
            List<Guid>? activityTypeIds,
            DateTime? startDate,
            DateTime? endDate,
            bool? isJoined,
            Guid? userId,
            Guid? callerId,
            Guid? branchId,
            string? address,
            int? pageSize,
            int? page,
            string? orderBy,
            SortType? sortType,
            string? userRoleName
        );
        Task<CommonResponse> GetActivityAsync(Guid id, Guid? userId, string? userRoleName);

        Task<CommonResponse> SearchActivityByItemId(Guid itemId, Guid userId);
        Task<CommonResponse> UpdateActivityAsync(
            ActivityUpdatingRequest activityUpdatingRequest,
            Guid userId,
            string userRoleName
        );
    }
}
