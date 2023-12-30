using System.ComponentModel.DataAnnotations;

namespace DataAccess.Models.Requests
{
    public class GoogleLoginRequest
    {
        [Required]
        public string? GoogleToken { get; set; }
    }
}
