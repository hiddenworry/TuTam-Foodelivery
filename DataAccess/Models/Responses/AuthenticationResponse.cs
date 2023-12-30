namespace DataAccess.Models.Responses
{
    public class AuthenticationResponse
    {
        public string? AccessToken { get; set; }

        public string? Role { get; set; }

        public string? RefreshToken { get; set; }
    }
}
