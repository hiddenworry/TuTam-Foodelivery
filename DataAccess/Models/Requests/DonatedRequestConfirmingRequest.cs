namespace DataAccess.Models.Requests
{
    public class DonatedRequestConfirmingRequest
    {
        public Guid Id { get; set; }

        public string? RejectingReason { get; set; }

        public List<Guid>? DonatedItemIds { get; set; }
    }
}
