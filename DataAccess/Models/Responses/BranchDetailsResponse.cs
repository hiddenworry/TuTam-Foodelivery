namespace DataAccess.Models.Responses
{
    public class BranchDetailsResponse
    {
        public Guid Id { get; set; }

        public string Name { get; set; }

        public string Address { get; set; }

        public string Location { get; set; }

        public string Image { get; set; }

        public DateTime CreatedDate { get; set; }

        public string Status { get; set; }

        public BranchAdminResponse BranchAdminResponses { get; set; }

        public string? Description { get; set; }
    }
}
