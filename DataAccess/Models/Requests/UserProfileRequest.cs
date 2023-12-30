using Microsoft.AspNetCore.Http;

namespace DataAccess.Models.Requests
{
    public class UserProfileRequest
    {
        public string? Name { get; set; }

        public string? Address { get; set; }

        public double[]? Location { get; set; }

        public IFormFile? Avatar { get; set; }

        public string? Phone { get; set; }
    }
}
