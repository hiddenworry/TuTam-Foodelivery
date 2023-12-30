using DataAccess.Entities;
using DataAccess.EntityEnums;
using DataAccess.Models.Requests;
using DataAccess.Models.Responses;
using DataAccess.ModelsEnum;

namespace BusinessLogic.Services
{
    public interface IScheduledRouteService
    {
        Task<CommonResponse> AcceptScheduledRouteAsync(
            Guid userId,
            ScheduledRouteAcceptingRequest scheduledRouteAcceptingRequest
        );

        Task AutoUpdateAvailableAndLateScheduledRoute();

        Task UpdateScheduledRoutes(DeliveryType? deliveryType, Guid? branchId);

        Task<CommonResponse> StartScheduledRouteAsync(
            Guid userId,
            ScheduledRouteStartingRequest scheduledRouteStartingRequest
        );

        Task SendNotificationBaseOnDeliveryRequestStatusOfDeliveryRequest(
            DeliveryRequest deliveryRequest
        );

        Task<CommonResponse> ReceiveItemsToFinishScheduledRouteTypeItemsToBranchAsync(
            Guid userId,
            ReceivedItemsToFinishScheduledRoute receivedItemsToFinishScheduledRoute
        );

        Task<CommonResponse> GetSampleGivingItemsToStartScheduledRouteAsync(
            Guid userId,
            Guid scheduledRouteId
        );

        Task<CommonResponse> GiveItemsToStartScheduledRouteAsync(
            Guid userId,
            ExportStocksForDeliveryRequestConfirmingRequest exportStocksForDeliveryRequestConfirmingRequest
        );

        Task<CommonResponse> GetScheduledRoutesForAdminAsync(
            Guid? branchId,
            StockUpdatedHistoryType? stockUpdatedHistoryType,
            ScheduledRouteStatus? status,
            string? startDate,
            string? endDate,
            Guid? userId,
            Guid callerId,
            string userRoleName,
            int? pageSize,
            int? page,
            SortType? sortType
        );

        Task<CommonResponse> GetScheduledRoutesForUserAsync(
            double? latitude,
            double? longitude,
            Guid? branchId,
            StockUpdatedHistoryType? stockUpdatedHistoryType,
            ScheduledRouteStatus? status,
            string? startDate,
            string? endDate,
            Guid userId,
            string userRoleName,
            SortType? sortType
        );

        Task<CommonResponse> GetScheduledRouteForUserAsync(Guid scheduledRouteId, Guid userId);

        Task<CommonResponse> GetScheduledRouteForAdminAsync(
            Guid scheduledRouteId,
            Guid userId,
            string userRoleName
        );

        Task<CommonResponse> UpdateNextStatusOfDeliveryRequestsOfScheduledRouteAsync(
            Guid userId,
            Guid scheduledRouteId
        );
        Task<CommonResponse> UpdateScheduledRoutesByDeliveryTypeAndBranchAdminIdAsync(
            DeliveryType deliveryType,
            Guid userId
        );
        Task<CommonResponse> CancelScheduledRouteAsync(Guid userId, Guid scheduledRouteId);

        Task AutoCheckLateScheduleRoute();
    }
}
