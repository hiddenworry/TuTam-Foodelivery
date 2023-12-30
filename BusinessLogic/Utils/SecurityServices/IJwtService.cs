using DataAccess.Entities;
using DataAccess.Models.Responses;
using System.Security.Claims;

namespace BusinessLogic.Utils.SecurityServices
{
    public interface IJwtService
    {
        ClaimsPrincipal? GetClaimsPrincipal(string jwtToken);
        string GenerateJwtToken(User user);
        string GenerateNewRefreshToken();
        AuthenticationResponse GenerateAuthenResponse(User user);
        Guid GetUserIdByJwtToken(string jwtToken);
        string GetRoleNameByJwtToken(string jwtToken);
    }
}
