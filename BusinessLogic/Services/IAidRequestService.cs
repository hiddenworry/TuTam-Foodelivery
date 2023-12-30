using DataAccess.EntityEnums;
using DataAccess.Models.Requests;
using DataAccess.Models.Requests.Enum;
using DataAccess.Models.Responses;
using DataAccess.ModelsEnum;

namespace BusinessLogic.Services
{
    public interface IAidRequestService
    {
        Task<CommonResponse> CancelAidRequest(Guid donatedRequestId, Guid userId);
        Task<CommonResponse> CountAidRequestByAllStatus(
            DateTime? startDate,
            DateTime? endDate,
            Guid? branchId,
            Guid? charityUnitId,
            string? roleEnum,
            Guid? branchAdminId
        );
        Task<CommonResponse> CountAidRequestByStatus(
            DateTime startDate,
            DateTime endDate,
            AidRequestStatus? status,
            TimeFrame timeFrame,
            Guid? branchId,
            Guid? charityUnitId,
            string? roleEnum,
            Guid? branchAdminId
        );
        Task<CommonResponse> CreateAidRequestAsync(
            AidRequestCreatingRequest aidRequestCreatingRequest,
            Guid userId,
            string userRoleName
        );
        Task<CommonResponse> CreateAidRequestByCharityUnitAsync(
            AidRequestCreatingRequest aidRequestCreatingRequest,
            Guid userId
        );
        Task<CommonResponse> FinishAidRequestAsync(Guid aidRequestId, Guid userId);
        Task<CommonResponse> GetAidRequestAsync(Guid id, Guid? userId, string? userRoleName);
        Task<CommonResponse> GetAidRequestsAsync(
            AidRequestStatus? status,
            DateTime? startDate,
            DateTime? endDate,
            Guid? callerId,
            Guid? branchId,
            Guid? charityUnitId,
            int? pageSize,
            int? page,
            string? orderBy,
            SortType? sortType,
            string? userRoleName
        );
        Task UpdateOutDateAidRequestsAsync();
    }
}
