using DataAccess.Entities;

namespace DataAccess.Repositories
{
    public interface IAcceptableDonatedRequestRepository
    {
        Task<int> CreateAcceptableDonatedRequestAsync(
            AcceptableDonatedRequest acceptableDonatedRequests
        );
        Task<int> CreateAcceptableDonatedRequestsAsync(
            List<AcceptableDonatedRequest> acceptableDonatedRequests
        );
        Task<AcceptableDonatedRequest?> FindPendingAcceptableDonatedRequestByDonatedRequestIdAndBranchIdAsync(
            Guid donatedRequestId,
            Guid branchId
        );
        Task<
            List<AcceptableDonatedRequest>
        > FindPendingAcceptableDonatedRequestsByDonatedRequestIdAsync(Guid donatedRequestId);

        Task<int> UpdateAcceptableDonatedRequestAsync(
            AcceptableDonatedRequest pendingAcceptableDonatedRequest
        );
    }
}
