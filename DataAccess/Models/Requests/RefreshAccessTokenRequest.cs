using System.ComponentModel.DataAnnotations;

namespace DataAccess.Models.Requests
{
    public class RefreshAccessTokenRequest
    {
        [Required(ErrorMessage = "Refresh Token is required")]
        public string refreshToken { get; set; }
    }
}
