namespace DataAccess.Models.Responses
{
    public class ActivityForAdminResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string? Address { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime EstimatedStartDate { get; set; }
        public DateTime EstimatedEndDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool IsJoined { get; set; }
        public string Status { get; set; }
        public string Scope { get; set; }
        public List<string> ActivityTypeComponents { get; set; }
        public List<BranchResponse> BranchResponses { get; set; }
    }
}
