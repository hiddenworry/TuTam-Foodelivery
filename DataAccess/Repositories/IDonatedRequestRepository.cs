using DataAccess.Entities;
using DataAccess.EntityEnums;

namespace DataAccess.Repositories
{
    public interface IDonatedRequestRepository
    {
        Task<int> CreateDonatedRequestAsync(DonatedRequest donatedRequest);
        Task<List<DonatedRequest>> FindPendingAndAcceptedAndProcessingDonatedRequestsAsync();
        Task<DonatedRequest?> FindAcceptedDonatedRequestByIdAndBranchIdAsync(
            Guid id,
            Guid branchId
        );
        Task<DonatedRequest?> FindDonatedRequestByIdAsync(Guid id);
        Task<DonatedRequest?> FindDonatedRequestByIdForDetailAsync(Guid id);
        Task<DonatedRequest?> FindPendingDonatedRequestByIdAsync(Guid id);
        Task<List<DonatedRequest>> GetDonatedRequestsAsync(
            DonatedRequestStatus? status,
            DateTime? startDate,
            DateTime? endDate,
            Guid? branchId,
            Guid? userId,
            Guid? activityId
        );
        Task<int> UpdateDonatedRequestAsync(DonatedRequest donatedRequest);

        Task<int> CountDonatedRequestByStatus(
            DonatedRequestStatus? status,
            DateTime? startDate,
            DateTime? endDate,
            Guid? branchId,
            Guid? userId,
            Guid? activityId
        );
    }
}
