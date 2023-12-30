using DataAccess.EntityEnums;
using DataAccess.Models.Requests;
using DataAccess.Models.Requests.Enum;
using DataAccess.Models.Responses;

namespace BusinessLogic.Services
{
    public interface IDeliveryRequestService
    {
        Task<CommonResponse> CreateDeliveryRequestsForDonatedRequestToBranchAsync(
            Guid userId,
            DeliveryRequestsForDonatedRequestToBranchCreatingRequest deliveryRequestCreatingRequest
        );

        Task<CommonResponse> CreateDeliveryRequestsForBranchToAidRequestAsync(
            Guid userId,
            DeliveryRequestsForBranchToAidRequestCreatingRequest deliveryRequestCreatingRequest
        );

        Task<CommonResponse> CreateDeliveryRequestsForBranchToBranchAsync(
            Guid userId,
            DeliveryRequestsForBranchToBranchCreatingRequest deliveryRequestCreatingRequest
        );

        Task<CommonResponse> GetDeliveryRequestAsync(
            int? page,
            int? pageSize,
            DeliveryFilterRequest request
        );

        Task<CommonResponse> UpdateNextStatusOfDeliveryRequestAsync(
            Guid userId,
            Guid deliveryRequestId
        );
        Task<CommonResponse> UpdateDeliveryItemsOfDeliveryRequest(
            Guid userId,
            Guid deliveryRequestId,
            DeliveryItemsOfDeliveryRequestUpdatingRequest deliveryItemsOfDeliveryRequestUpdatingRequest
        );

        Task<CommonResponse> GetDeliveryRequestDetailsAsync(Guid? userId, Guid deliveryId);
        Task<CommonResponse> GetDeliveryRequestDetailsByContributorIdsync(
            Guid contributorId,
            Guid deliveryId
        );
        Task<CommonResponse> UpdateProofImageOfDeliveryRequest(
            Guid userId,
            Guid deliveryRequestId,
            string proofImage
        );

        Task<CommonResponse> UpdateFinishedDeliveryRequestTypeBranchToCharityUnitAsync(
            Guid userId,
            Guid deliveryRequestId
        );

        Task<CommonResponse> SendReportByUserOrCharityUnitAsync(
            Guid userId,
            string userRoleName,
            Guid deliveryRequestId,
            ReportForUserOrCharityUnitRequest reportForUserOrCharityUnitRequest
        );

        Task<CommonResponse> SendReportByContributorAsync(
            Guid userId,
            Guid deliveryRequestId,
            ReportForContributorRequest reportForCollaboratorRequest
        );

        Task<CommonResponse> HandleReportedDeliveryRequestAsync(
            Guid userId,
            Guid deliveryRequestId,
            DeliveryRequestStatus deliveryRequestStatus
        );

        Task<CommonResponse> CancelDeliveryRequestAsync(
            Guid deliveryRequestId,
            string canceledReason,
            Guid userId
        );

        Task<CommonResponse> GetFinishedDeliveryRequestsByDonatedRequestIdForUserAsync(
            Guid donatedRequestId,
            Guid userId,
            int? pageSize,
            int? page
        );

        Task<CommonResponse> GetFinishedDeliveryRequestByIdOfDonatedRequestForUserAsync(
            Guid deliveryRequestId,
            Guid userId
        );

        Task<CommonResponse> GetFinishedDeliveryRequestsByAidRequestIdForCharityUnitAsync(
            Guid aidRequestId,
            Guid userId,
            string userRoleName,
            int? pageSize,
            int? page
        );

        Task<CommonResponse> GetFinishedDeliveryRequestByIdOfAidRequestForCharityUnitAsync(
            Guid deliveryRequestId,
            Guid? userId
        );
        Task<CommonResponse> CountDeliveryRequestByAllStatus(
            DateTime? startDate,
            DateTime? endDate,
            Guid? branchId,
            string? roleEnum,
            Guid? branchAdminId
        );
        Task<CommonResponse> CountDeliveryRequestByStatus(
            DateTime startDate,
            DateTime endDate,
            DeliveryRequestStatus? status,
            TimeFrame timeFrame,
            Guid? branchId,
            string? roleEnum,
            Guid? branchAdminId
        );
    }
}
