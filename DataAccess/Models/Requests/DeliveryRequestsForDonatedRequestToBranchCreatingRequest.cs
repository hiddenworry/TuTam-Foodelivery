namespace DataAccess.Models.Requests
{
    public class DeliveryRequestsForDonatedRequestToBranchCreatingRequest
    {
        public string? Note { get; set; }

        public List<ScheduledTime> ScheduledTimes { get; set; }

        public Guid DonatedRequestId { get; set; }

        public List<List<DeliveryItemRequest>> DeliveryItemsForDeliveries { get; set; }
    }
}
