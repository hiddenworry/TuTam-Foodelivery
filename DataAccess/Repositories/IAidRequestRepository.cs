using DataAccess.Entities;
using DataAccess.EntityEnums;

namespace DataAccess.Repositories
{
    public interface IAidRequestRepository
    {
        Task<int> CountAidRequestBySelfShippingFlag(
            bool? isSelfShipping,
            DateTime? startDate,
            DateTime? endDate
        );
        Task<int> CreateAidRequestAsync(AidRequest aidRequest);
        Task<AidRequest?> FindAcceptedOrProcessingAidRequestByIdAndBranchIdAsync(
            Guid id,
            Guid branchId
        );
        Task<AidRequest?> FindAidRequestByIdAsync(Guid id);
        Task<AidRequest?> FindAidRequestOfCharityUnitByIdForDetailAsync(Guid id);
        Task<AidRequest?> FindPendingAidRequestByIdAsync(Guid id);
        Task<List<AidRequest>> GetAidRequestsOfCharityUnitAsync(
            AidRequestStatus? status,
            DateTime? startDate,
            DateTime? endDate,
            Guid? branchId,
            Guid? charityUnitId
        );
        Task<List<AidRequest>> GetAidRequestsOfCharityUnitWhichHasAppliableItemsToActivityAsync(
            DateTime? startDate,
            DateTime? endDate,
            Guid branchId,
            Guid? charityUnitId
        );
        Task<int> UpdateAidRequestAsync(AidRequest aidRequest);
        Task<AidRequest?> FindAcceptedAndProcessingAidRequestByIdAndBranchIdToFinishAsync(
            Guid aidRequestId,
            Guid id
        );
        Task<List<AidRequest>> FindPendingAndAcceptedAndProcessingAidRequestsAsync();
        Task<int> CountAidRequestByStatus(
            AidRequestStatus? status,
            DateTime? startDate,
            DateTime? endDate,
            Guid? branchId,
            Guid? charityUnitId
        );
    }
}
