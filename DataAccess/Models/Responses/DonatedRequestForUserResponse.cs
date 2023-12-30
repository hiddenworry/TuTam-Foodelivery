using DataAccess.Models.Requests;

namespace DataAccess.Models.Responses
{
    public class DonatedRequestForUserResponse
    {
        public Guid Id { get; set; }

        public List<string> Images { get; set; }

        public string Address { get; set; }

        public List<double>? Location { get; set; }

        public DateTime CreatedDate { get; set; }

        public DateTime? AcceptedDate { get; set; }

        public List<ScheduledTime>? ScheduledTimes { get; set; }

        public string Status { get; set; }

        public SimpleBranchResponse? SimpleBranchResponse { get; set; }

        public SimpleActivityResponse? SimpleActivityResponse { get; set; }

        public List<DonatedItemResponse> DonatedItemResponses { get; set; }
    }
}
