using DataAccess.Models.Requests;

namespace DataAccess.Models.Responses
{
    public class SimpleDeliveryRequestResponse
    {
        public Guid? Id { get; set; }

        public string? Status { get; set; }

        public string Address { get; set; }

        public List<double> Location { get; set; }

        public ScheduledTime? CurrentScheduledTime { get; set; }

        public List<string>? Images { get; set; }

        public string? ProofImage { get; set; }

        public string? Avatar { get; set; }

        public string Name { get; set; }

        public string Phone { get; set; }

        public Guid? ActivityId { get; set; }

        public string? ActivityName { get; set; }

        public List<DeliveryItemResponse>? DeliveryItems { get; set; }
    }
}
