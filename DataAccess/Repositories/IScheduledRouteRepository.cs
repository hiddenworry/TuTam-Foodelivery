using DataAccess.Entities;
using DataAccess.EntityEnums;

namespace DataAccess.Repositories
{
    public interface IScheduledRouteRepository
    {
        Task<int> AddScheduledRouteAsync(ScheduledRoute scheduledRoute);
        Task<List<ScheduledRoute>> FindAcceptedAndProcessingScheduledRoutesByContributorIdAsync(
            Guid contributorId
        );
        Task<ScheduledRoute?> FindAcceptedScheduledRouteByIdAndUserIdAsync(
            Guid scheduledRouteId,
            Guid userId
        );
        Task<ScheduledRoute?> FindPendingScheduledRouteByIdAsync(Guid id);
        Task<ScheduledRoute?> FindProcessingScheduledRouteByIdAndUserIdAsync(Guid id, Guid userId);
        Task<ScheduledRoute?> FindProcessingScheduledRouteByIdAsync(Guid id);
        Task<List<ScheduledRoute>> GetScheduledRoutesForUserAsync(
            ScheduledRouteStatus? status,
            Guid userId
        );
        Task<List<ScheduledRoute>> GetScheduledRoutesForAdminAsync(
            ScheduledRouteStatus? status,
            Guid? userId
        );
        Task<int> UpdateScheduledRouteAsync(ScheduledRoute scheduledRoute);
        Task<ScheduledRoute?> FindScheduledRouteByIdForDetailAsync(Guid id);
        Task<ScheduledRoute?> FindScheduledRouteByDeliveryRequestId(Guid deliveryRequestId);
        Task<ScheduledRoute?> FindScheduledRouteByIdForDetailForAdminAsync(
            Guid scheduledRouteId,
            bool isProcessing
        );
        Task<ScheduledRoute?> FindAcceptedAndProcessingScheduledRouteByUserIdAsync(
            Guid scheduledRouteId,
            Guid userId
        );
        Task<List<ScheduledRoute>?> FindAcceptedAndProcessingScheduledRoutedAsync();
    }
}
