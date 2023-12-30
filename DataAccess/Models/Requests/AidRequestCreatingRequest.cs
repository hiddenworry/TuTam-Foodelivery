namespace DataAccess.Models.Requests
{
    public class AidRequestCreatingRequest
    {
        public List<ScheduledTime> ScheduledTimes { get; set; }

        public string? Note { get; set; }

        public List<AidItemRequest> AidItemRequests { get; set; }

        public bool IsSelfShipping { get; set; }
    }
}
