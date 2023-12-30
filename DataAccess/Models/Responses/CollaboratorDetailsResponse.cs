namespace DataAccess.Models.Responses
{
    public class CollaboratorDetailsResponse
    {
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? FullName { get; set; }

        public string? Avatar { get; set; }
        public DateTime? DateOfBirth { get; set; }

        public string? Gender { get; set; }

        public string? FrontOfIdCard { get; set; }

        public string? BackOfIdCard { get; set; }

        public string? Note { get; set; }

        public string? status { get; set; }
    }
}
