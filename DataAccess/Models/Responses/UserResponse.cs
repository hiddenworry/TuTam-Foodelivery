namespace DataAccess.Models.Responses
{
    public class UserResponse
    {
        public Guid Id { get; set; }

        public string? Email { get; set; }

        public string? Phone { get; set; }

        public string? FullName { get; set; }

        public string? Address { get; set; }

        public string? Status { get; set; }
        public string? Avatar { get; set; }

        public string? Description { get; set; }

        public string? FrontOfIdCard { get; set; }

        public string? BackOfIdCard { get; set; }

        public string? OtherContacts { get; set; }

        public string? Name { get; set; }
    }
}
