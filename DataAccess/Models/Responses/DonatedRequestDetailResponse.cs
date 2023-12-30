using DataAccess.Models.Requests;

namespace DataAccess.Models.Responses
{
    public class DonatedRequestDetailResponse
    {
        public Guid Id { get; set; }

        public string Address { get; set; }

        public List<double>? Location { get; set; }

        public List<string> Images { get; set; }

        public bool IsConfirmable { get; set; }

        public DateTime CreatedDate { get; set; }

        public DateTime? AcceptedDate { get; set; }

        public List<ScheduledTime>? ScheduledTimes { get; set; }

        public string Status { get; set; }

        public string? Note { get; set; }

        public BranchResponse? AcceptedBranch { get; set; }

        public List<DonatedItemResponse> DonatedItemResponses { get; set; }

        public SimpleUserResponse SimpleUserResponse { get; set; }

        public List<RejectingBranchResponse>? RejectingBranchResponses { get; set; }

        public SimpleActivityResponse? SimpleActivityResponse { get; set; }
    }
}
