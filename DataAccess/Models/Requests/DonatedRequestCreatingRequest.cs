using Microsoft.AspNetCore.Http;

namespace DataAccess.Models.Requests
{
    public class DonatedRequestCreatingRequest
    {
        public List<IFormFile> Images { get; set; }

        public string Address { get; set; }

        public List<double> Location { get; set; }

        public List<ScheduledTime> ScheduledTimes { get; set; }

        public string? Note { get; set; }

        public Guid? ActivityId { get; set; }

        public List<DonatedItemRequest> DonatedItemRequests { get; set; }
    }
}
