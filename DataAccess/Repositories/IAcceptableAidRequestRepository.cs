using DataAccess.Entities;

namespace DataAccess.Repositories
{
    public interface IAcceptableAidRequestRepository
    {
        Task<int> CreateAcceptableAidRequestsAsync(
            List<AcceptableAidRequest> acceptableAidRequests
        );
        Task<AcceptableAidRequest?> FindPendingAcceptableAidRequestByAidRequestIdAndBranchIdAsync(
            Guid aidRequestId,
            Guid branchId
        );
        Task<List<AcceptableAidRequest>> FindPendingAcceptableAidRequestsByAidRequestIdAsync(
            Guid aidRequestId
        );
        Task<int> UpdateAcceptableAidRequestAsync(AcceptableAidRequest acceptableAidRequest);
    }
}
