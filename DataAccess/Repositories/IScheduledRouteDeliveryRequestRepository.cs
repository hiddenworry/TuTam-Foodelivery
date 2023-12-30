using DataAccess.Entities;

namespace DataAccess.Repositories
{
    public interface IScheduledRouteDeliveryRequestRepository
    {
        Task<int> AddScheduledRouteDeliveryRequestsAsync(
            List<ScheduledRouteDeliveryRequest> scheduledRouteDeliveryRequests
        );
        Task<int> UpdateScheduledRouteDeliveryRequestAsync(
            ScheduledRouteDeliveryRequest scheduledRouteDeliveryRequest
        );
        Task<int> UpdateScheduledRouteDeliveryRequestsAsync(
            List<ScheduledRouteDeliveryRequest> scheduledRouteDeliveryRequests
        );
    }
}
