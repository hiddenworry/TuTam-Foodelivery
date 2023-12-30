namespace DataAccess.Models.Requests
{
    public class ReceivedItemsToFinishScheduledRoute
    {
        public Guid ScheduledRouteId { get; set; }

        public List<DeliveryRequestRequest> DeliveryRequests { get; set; }
    }
}
