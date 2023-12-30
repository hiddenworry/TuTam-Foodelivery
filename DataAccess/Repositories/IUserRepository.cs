using DataAccess.Entities;
using DataAccess.Models.Requests;

namespace DataAccess.Repositories
{
    public interface IUserRepository
    {
        Task<User?> CreateUserAsync(User user);
        Task<User?> DeleteAccessTokenAsync(Guid userId);
        Task<User?> DeleteRefreshTokenAsync(Guid userId);
        Task<User?> FindUserByEmailAsync(string email);
        Task<User?> FindUserByIdAsync(Guid userId);
        Task<User?> UpdateUserAsync(User user);
        Task<User?> UpdateAccessTokenAsync(Guid userId, string newAccessToken);
        Task<User?> UpdateRefreshTokenAsync(Guid userId, string newRefreshToken, DateTime date);
        Task<User?> FindUserByEmailOrPhoneAsync(string str);
        Task<User?> FindUserByRefreshTokenAsync(string refreshToken);
        Task<User?> FindUserByVerifyCodeAsync(string verifyCode);
        Task<User?> FindUserByOtpcodeAndPhoneAsync(string otp, string phone);
        Task<List<User>?> FindUserAsyncExceptRole(string searchStr, List<Role> exceptRoles);
        Task<User?> FindUserByPhoneAsync(string str);
        Task<List<User>?> FindUserAsync(UserFilterRequest userFilterRequest);
        Task<User?> FindUserProfileByIdAsync(Guid userId);
        Task<User?> FindUserByIdInclueBranchAsync(Guid userId);
        Task<List<User>?> FindBranchAdminForAssignToBranchAsync();
        Task<User?> DeleteUserAsync(User user);
        Task<User?> FindUserByRoleAsync(string role);
    }
}
