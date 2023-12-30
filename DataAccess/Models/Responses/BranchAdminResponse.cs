namespace DataAccess.Models.Responses
{
    public class BranchAdminResponse
    {
        public Guid? Id { get; set; }
        public string? MemberName { get; set; }

        public string? Email { get; set; }

        public string? Phone { get; set; }
    }
}
