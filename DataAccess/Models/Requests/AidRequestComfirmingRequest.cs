namespace DataAccess.Models.Requests
{
    public class AidRequestComfirmingRequest
    {
        public Guid Id { get; set; }

        public string? RejectingReason { get; set; }

        public List<Guid>? AidItemIds { get; set; }
    }
}
