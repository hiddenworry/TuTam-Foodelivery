using DataAccess.Entities;
using DataAccess.EntityEnums;
using DataAccess.Models.Requests;
using DataAccess.ModelsEnum;

namespace DataAccess.Repositories
{
    public interface IDeliveryRequestRepository
    {
        Task<bool> CheckContributorAvailabileToCancelCollaborator(Guid userId);
        Task<int> CreateDeliveryRequestAsync(DeliveryRequest deliveryRequest);
        Task<DeliveryRequest?> FindProcessingDeliveryRequestByIdAsync(Guid deliveryRequestId);
        Task<
            List<List<DeliveryRequest>>
        > FindPendingDeliveryRequestsByDeliveryTypeAndMainBranchIdAsync(
            DeliveryType? deliveryType,
            Guid? branchId
        );
        Task<List<DeliveryRequest>?> GetDeliveryRequestsAsync(DeliveryFilterRequest request);
        Task<int> UpdateDeliveryRequestAsync(DeliveryRequest deliveryRequest);
        Task<DeliveryRequest?> GetDeliveryRequestsDetailAsync(
            Guid deliveryRequestId,
            Guid? branchAdminId
        );
        Task<int> UpdateDeliveryRequestsAsync(List<DeliveryRequest> deliveryRequests);
        Task<List<DeliveryRequest>?> GetDeliveryRequestsDetailForContributorAsync(
            Guid contributorId,
            Guid deliveryId
        );
        Task<DeliveryRequest?> FindDeliveryRequestByIdAsync(Guid deliveryRequestId);

        Task<List<DeliveryRequest>> FindDeliveryRequestsByDonatedRequestIdAsync(
            Guid donatedRequestId
        );
        Task<DeliveryRequest?> FindFinishedDeliveryRequestForDetailByIdAndDonorIdAsync(
            Guid deliveryRequestId,
            Guid userId
        );
        Task<DeliveryRequest?> FindFinishedDeliveryRequestForDetailByIdAndUserIOfCharityUnitAsync(
            Guid deliveryRequestId,
            Guid? userId
        );
        Task<int> CountDeliveryRequest(
            DeliveryRequestStatus? status,
            DateTime? startDate,
            DateTime? endDate,
            Guid? branchId
        );
    }
}
