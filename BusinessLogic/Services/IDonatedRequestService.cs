using DataAccess.EntityEnums;
using DataAccess.Models.Requests;
using DataAccess.Models.Requests.Enum;
using DataAccess.Models.Responses;
using DataAccess.ModelsEnum;

namespace BusinessLogic.Services
{
    public interface IDonatedRequestService
    {
        Task<CommonResponse> CancelDonatedRequest(Guid donatedRequestId, Guid userId);
        Task UpdateOutDateDonatedRequestsAsync();

        Task<CommonResponse> CountDonatedRequestByStatus(
            DateTime startDate,
            DateTime endDate,
            DonatedRequestStatus? status,
            TimeFrame timeFrame,
            Guid? branchId,
            Guid? userId,
            Guid? activityId,
            string? roleEnum,
            Guid? branchAdminId
        );
        Task<CommonResponse> CreateDonatedRequestAsync(
            DonatedRequestCreatingRequest donatedRequestCreatingRequest,
            Guid userId
        );
        Task<CommonResponse> GetDonatedRequestAsync(Guid id, Guid? userId, string? userRoleName);
        Task<CommonResponse> GetDonatedRequestsAsync(
            DonatedRequestStatus? status,
            DateTime? startDate,
            DateTime? endDate,
            Guid? callerId,
            Guid? branchId,
            Guid? userId,
            Guid? activityId,
            int? pageSize,
            int? page,
            string? orderBy,
            SortType? sortType,
            string? userRoleName
        );
        Task<CommonResponse> CountDonatedRequestByAllStatus(
            DateTime? startDate,
            DateTime? endDate,
            Guid? branchId,
            Guid? userId,
            Guid? activityId,
            string? roleEnum,
            Guid? branchAdminId
        );
    }
}
