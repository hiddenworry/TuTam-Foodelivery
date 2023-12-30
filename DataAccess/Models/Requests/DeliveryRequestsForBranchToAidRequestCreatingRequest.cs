namespace DataAccess.Models.Requests
{
    public class DeliveryRequestsForBranchToAidRequestCreatingRequest
    {
        public string? Note { get; set; }

        public List<ScheduledTime> ScheduledTimes { get; set; }

        public Guid AidRequestId { get; set; }

        public List<List<DeliveryItemRequest>> DeliveryItemsForDeliveries { get; set; }
    }
}
