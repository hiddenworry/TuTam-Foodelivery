namespace DataAccess.Models.Responses
{
    public class ActivityDetailResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string? Address { get; set; }
        public string? Location { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime EstimatedStartDate { get; set; }
        public DateTime EstimatedEndDate { get; set; }
        public DateTime? DeliveringDate { get; set; }
        public string Status { get; set; }
        public string Description { get; set; }
        public List<string> Images { get; set; }
        public string Scope { get; set; }
        public bool IsNearby { get; set; }
        public int NumberOfParticipants { get; set; }
        public List<string> ActivityTypeComponents { get; set; }
        public double TotalTargetProcessPercentage { get; set; }
        public List<TargetProcessResponse> TargetProcessResponses { get; set; }
        public bool? IsJoined { get; set; }
        public List<BranchResponse> BranchResponses { get; set; }
        public SimpleUserResponse Creater { get; set; }
    }
}
