using DataAccess.Models.Requests;

namespace DataAccess.Models.Responses
{
    public class AidRequestDetailResponse
    {
        public Guid Id { get; set; }

        public string Address { get; set; }

        public List<double>? Location { get; set; }

        public bool IsConfirmable { get; set; }

        public DateTime CreatedDate { get; set; }

        public DateTime? AcceptedDate { get; set; }

        public List<ScheduledTime>? ScheduledTimes { get; set; }

        public string Status { get; set; }

        public bool IsSelfShipping { get; set; }

        public string? Note { get; set; }

        public List<BranchResponse>? AcceptedBranches { get; set; }

        public List<AidItemResponse> AidItemResponses { get; set; }

        public BranchResponse? StartingBranch { get; set; }

        public CharityUnitResponse CharityUnitResponse { get; set; }

        public List<RejectingBranchResponse>? RejectingBranchResponses { get; set; }
    }
}
