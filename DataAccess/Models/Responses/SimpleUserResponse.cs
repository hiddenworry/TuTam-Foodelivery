namespace DataAccess.Models.Responses
{
    public class SimpleUserResponse
    {
        public Guid Id { get; set; }

        public string FullName { get; set; }

        public string? Avatar { get; set; }

        public string Role { get; set; }

        public string? Phone { get; set; }

        public string? Email { get; set; }

        public string? Status { get; set; }
    }
}
