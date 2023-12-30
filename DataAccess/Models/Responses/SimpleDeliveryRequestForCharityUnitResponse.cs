using DataAccess.Models.Requests;

namespace DataAccess.Models.Responses
{
    public class SimpleDeliveryRequestForCharityUnitResponse
    {
        public Guid Id { get; set; }

        public ScheduledTime? CurrentScheduledTime { get; set; }

        public DateTime? ExportedDate { get; set; }

        public DateTime? FinishedDate { get; set; }

        public string? ExportNote { get; set; }

        public string? ProofImage { get; set; }

        public string? Avatar { get; set; }

        public string? Name { get; set; }

        public string? Phone { get; set; }
    }
}
