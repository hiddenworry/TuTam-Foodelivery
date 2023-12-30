using DataAccess.Entities;
using DataAccess.Models.Requests;
using DataAccess.Models.Responses;
using DataAccess.ModelsEnum;

namespace BusinessLogic.Services
{
    public interface IUserService
    {
        Task<CommonResponse> AuthenticateAsync(LoginRequest LoginRequest);
        Task<CommonResponse> AuthenticateByGoogleAsync(GoogleUserInfoResponse res);
        Task<CommonResponse> CreateAccountForBranchAdmin(BranchAdminCreatingRequest request);
        Task<User?> DeleteAccessTokenAsync(Guid userId);
        Task<User?> DeleteRefreshTokenAsync(Guid userId);
        Task<User?> FindUserByEmailAsync(string email);
        Task<CommonResponse> GetListUserForSystemAdminForAssignBranchMember(string? searchStr);
        Task<CommonResponse> GetProfile(Guid userId);
        Task<CommonResponse> GetUserAsync(
            UserFilterRequest request,
            int? pageSize,
            int? page,
            SortType? sortType = SortType.DES
        );
        Task<CommonResponse> GetUserById(Guid Id);
        Task<CommonResponse> LinkToEmail(string verifyCode, Guid userId, string email);
        Task<CommonResponse> RefreshAccessTokenAsync(RefreshAccessTokenRequest request);
        Task<CommonResponse> RegisterUserByPhone(UserRegisterByPhoneRequest request);
        Task<CommonResponse> ResetPasswordAsync(ResetPasswordRequest request);
        Task<CommonResponse> SendOtp(string phone);
        Task<CommonResponse> SendVerificationEmail(VerifyEmailRequest request);
        Task<CommonResponse> SendVerifyCodeToEmail(VerifyEmailRequest request, Guid UserId);
        Task<CommonResponse> UpdatePasswordAsync(UpdatePasswordRequest request, Guid userId);
        Task<CommonResponse> UpdateProfile(Guid userId, UserProfileRequest request);
        Task<CommonResponse> UpdateUserAsync(UserUpdaingForAdminRequest request, Guid userId);
        Task<CommonResponse> VerifyUserEmail(string verifyCode);
        Task<CommonResponse> VerifyUserPhone(VerifyOtpRequest request);
        Task<CommonResponse> UpdateDeviceToken(Guid userId, string deviceToken);
        Task<CommonResponse> RegisterUserByPhoneForBranchAdmin(UserRegisterByPhoneRequest request);
        Task<CommonResponse> GetUserAsyncByPhone(
            UserFilterRequest request,
            int? pageSize,
            int? page,
            SortType? sortType = SortType.DES
        );
    }
}
