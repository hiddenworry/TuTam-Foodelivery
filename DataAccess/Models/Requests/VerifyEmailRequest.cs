using System.ComponentModel.DataAnnotations;

namespace DataAccess.Models.Requests
{
    public class VerifyEmailRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }
}
