using DataAccess.Models.Requests;

namespace DataAccess.Models.Responses
{
    public class SimpleDeliveryRequestForUserResponse
    {
        public Guid Id { get; set; }

        public ScheduledTime? CurrentScheduledTime { get; set; }

        public DateTime? ImportedDate { get; set; }

        public string? ImportNote { get; set; }

        public string? ProofImage { get; set; }

        public string? Avatar { get; set; }

        public string? Name { get; set; }

        public string? Phone { get; set; }
    }
}
