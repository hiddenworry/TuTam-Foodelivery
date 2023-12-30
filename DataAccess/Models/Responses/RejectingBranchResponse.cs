namespace DataAccess.Models.Responses
{
    public class RejectingBranchResponse
    {
        public Guid Id { get; set; }

        public string Name { get; set; }

        public string Address { get; set; }

        public string Image { get; set; }

        public DateTime CreatedDate { get; set; }

        public string Status { get; set; }

        public string? RejectingReason { get; set; }
    }
}
