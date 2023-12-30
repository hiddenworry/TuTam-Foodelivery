using DataAccess.Models.Requests;

namespace DataAccess.Models.Responses
{
    public class AidRequestForAdminResponse
    {
        public Guid Id { get; set; }

        public string Address { get; set; }

        public List<double>? Location { get; set; }

        public DateTime CreatedDate { get; set; }

        public DateTime? AcceptedDate { get; set; }

        public List<ScheduledTime>? ScheduledTimes { get; set; }

        public string Status { get; set; }

        public bool IsSelfShipping { get; set; }

        public List<SimpleBranchResponse> SimpleBranchResponses { get; set; }

        public SimpleCharityUnitResponse SimpleCharityUnitResponse { get; set; }
    }
}
