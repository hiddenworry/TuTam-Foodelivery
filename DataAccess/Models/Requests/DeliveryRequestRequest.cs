namespace DataAccess.Models.Requests
{
    public class DeliveryRequestRequest
    {
        public Guid DeliveryRequestId { get; set; }

        public List<ReceivedDeliveryItemRequest> ReceivedDeliveryItemRequests { get; set; }
    }
}
