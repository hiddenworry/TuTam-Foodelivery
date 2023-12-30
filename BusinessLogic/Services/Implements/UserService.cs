using BusinessLogic.Utils.EmailService;
using BusinessLogic.Utils.FirebaseService;
using BusinessLogic.Utils.SecurityServices;
using BusinessLogic.Utils.SmsService;
using DataAccess.Entities;
using DataAccess.EntityEnums;
using DataAccess.Models.Requests;
using DataAccess.Models.Requests.ModelBinders;
using DataAccess.Models.Responses;
using DataAccess.ModelsEnum;
using DataAccess.Repositories;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Transactions;

namespace BusinessLogic.Services.Implements
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IJwtService _jwtService;
        private readonly IPermissionRepository _permissionRepository;
        private readonly IConfiguration _config;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IRoleRepository _roleRepository;
        private readonly IRolePermissionRepository _rolePermissionRepository;
        private readonly IUserPermissionRepository _userPermissionRepository;
        private readonly IEmailService _emailService;
        private readonly ISMSService _smsService;
        private readonly ILogger<UserService> _logger;
        private readonly IFirebaseStorageService _firebaseStorageService;
        private readonly ICharityUnitRepository _charityUnitRepository;
        private readonly ICollaboratorRepository _collaboratorRepository;

        public UserService(
            IUserRepository userRepository,
            IJwtService jwtService,
            IPermissionRepository permissionRepository,
            IConfiguration configuration,
            IPasswordHasher passwordHasher,
            IRoleRepository roleRepository,
            IRolePermissionRepository rolePermissionRepository,
            IUserPermissionRepository userPermissionRepository,
            IEmailService emailService,
            ISMSService SMSService,
            ILogger<UserService> logger,
            IFirebaseStorageService firebaseStorageService,
            ICharityUnitRepository charityUnitRepository,
            ICollaboratorRepository collaboratorRepository
        )
        {
            _userRepository = userRepository;
            _jwtService = jwtService;
            _permissionRepository = permissionRepository;
            _config = configuration;
            _passwordHasher = passwordHasher;
            _roleRepository = roleRepository;
            _rolePermissionRepository = rolePermissionRepository;
            _userPermissionRepository = userPermissionRepository;
            _emailService = emailService;
            _smsService = SMSService;
            _logger = logger;
            _firebaseStorageService = firebaseStorageService;
            _charityUnitRepository = charityUnitRepository;
            _collaboratorRepository = collaboratorRepository;
        }

        public async Task<CommonResponse> AuthenticateAsync(LoginRequest LoginRequest)
        {
            CommonResponse commonResponse = new CommonResponse();
            string loginSuccessMsg = _config["ResponseMessages:AuthenticationMsg:LoginSuccessMsg"];
            string loginFailedMsg = _config["ResponseMessages:AuthenticationMsg:LoginFailedMsg"];
            string userNotFoundMsg = _config["ResponseMessages:AuthenticationMsg:UserNotFoundMsgs"];
            string internalServerErrorMsg = _config[
                "ResponseMessages:AuthenticationMsg:InternalServerErrorMsg"
            ];
            string unVerifyUserMsg = _config["ResponseMessages:AuthenticationMsg:UnVerifyUserMsg"];
            string inactiveUserMsg = _config["ResponseMessages:AuthenticationMsg:InactiveUserMsg"];
            try
            {
                string userName = LoginRequest.UserName ?? string.Empty;
                var user = await _userRepository.FindUserByEmailOrPhoneAsync(userName);
                if (user != null)
                {
                    if (
                        LoginRequest.Password == null
                        || user.Password == null
                        || _passwordHasher.Verify(LoginRequest.Password, user.Password) == false
                    )
                    {
                        commonResponse.Status = 401;
                        commonResponse.Message = loginFailedMsg;
                        return commonResponse;
                    }
                    if (user.Status == UserStatus.UNVERIFIED)
                    {
                        commonResponse.Status = 400;
                        commonResponse.Message = unVerifyUserMsg;
                        return commonResponse;
                    }
                    if (user.Status == UserStatus.INACTIVE)
                    {
                        commonResponse.Status = 403;
                        commonResponse.Message = inactiveUserMsg;
                        return commonResponse;
                    }
                    if (
                        user.Role.Name == RoleEnum.CONTRIBUTOR.ToString()
                        && LoginRequest.LoginRole != UserRole.CONTRIBUTOR
                    )
                    {
                        commonResponse.Status = 401;
                        commonResponse.Message = loginFailedMsg;
                        return commonResponse;
                    }
                    if (
                        (
                            user.Role.Name == RoleEnum.SYSTEM_ADMIN.ToString()
                            || user.Role.Name == RoleEnum.BRANCH_ADMIN.ToString()
                        )
                        && LoginRequest.LoginRole != UserRole.ADMIN
                    )
                    {
                        commonResponse.Status = 401;
                        commonResponse.Message = loginFailedMsg;
                        return commonResponse;
                    }
                    if (
                        user.Role.Name == RoleEnum.CHARITY.ToString()
                        && LoginRequest.LoginRole != UserRole.CHARITY
                    )
                    {
                        commonResponse.Status = 401;
                        commonResponse.Message = loginFailedMsg;
                        return commonResponse;
                    }
                    using (
                        var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled)
                    )
                    {
                        // Generate refresh token and add it to user
                        var refreshToken = _jwtService.GenerateNewRefreshToken();
                        DateTime exprireDate = SettedUpDateTime.GetCurrentVietNamTime();
                        string expiredTimeDaysStr = _config["RefreshToken:ExpiredTimeDays"];
                        decimal expiredTimeDays = decimal.Parse(expiredTimeDaysStr);

                        exprireDate = exprireDate.AddDays((double)expiredTimeDays);
                        var updatedRefreshToken = await _userRepository.UpdateRefreshTokenAsync(
                            user.Id,
                            refreshToken,
                            exprireDate
                        );
                        if (updatedRefreshToken == null)
                            throw new Exception(userNotFoundMsg);
                        var response = _jwtService.GenerateAuthenResponse(user);
                        User? updatedAccessToken = null;
                        if (response != null && response.AccessToken != null)
                        {
                            updatedAccessToken = await _userRepository.UpdateAccessTokenAsync(
                                user.Id,
                                response.AccessToken
                            );
                        }
                        if (updatedAccessToken == null)
                            throw new Exception(userNotFoundMsg);
                        scope.Complete(); // commit transaction
                        commonResponse.Status = 200;
                        commonResponse.Data = response;
                        commonResponse.Message = loginSuccessMsg;
                    }
                }
                else
                {
                    commonResponse.Status = 401;
                    commonResponse.Message = loginFailedMsg;
                }
            }
            catch (Exception ex)
            {
                string className = nameof(AuthenticationService);
                string methodName = nameof(AuthenticateAsync);
                _logger.LogError(
                    ex,
                    "An error occurred in {ClassName}.{MethodName}: {ErrorMessage}",
                    className,
                    methodName,
                    ex.Message
                );
                commonResponse.Status = 500;
                commonResponse.Message = internalServerErrorMsg;
            }
            return commonResponse;
        }

        public async Task<CommonResponse> RefreshAccessTokenAsync(RefreshAccessTokenRequest request)
        {
            CommonResponse commonResponse = new CommonResponse();
            string loginSuccessMsg = _config["ResponseMessages:AuthenticationMsg:LoginSuccessMsg"];
            string loginFailedMsg = _config["ResponseMessages:AuthenticationMsg:LoginFailedMsg"];
            string userNotFoundMsg = _config["ResponseMessages:AuthenticationMsg:UserNotFoundMsgs"];
            string internalServerErrorMsg = _config[
                "ResponseMessages:AuthenticationMsg:InternalServerErrorMsg"
            ];
            string unVerifyUserMsg = _config["ResponseMessages:AuthenticationMsg:UnVerifyUserMsg"];
            string inactiveUserMsg = _config["ResponseMessages:AuthenticationMsg:InactiveUserMsg"];
            try
            {
                var user = await _userRepository.FindUserByRefreshTokenAsync(request.refreshToken);
                if (
                    user != null
                    && user.RefreshTokenExpiration > SettedUpDateTime.GetCurrentVietNamTime()
                )
                {
                    if (user.Status == UserStatus.UNVERIFIED)
                    {
                        commonResponse.Status = 400;
                        commonResponse.Message = unVerifyUserMsg;
                        return commonResponse;
                    }
                    if (user.Status == UserStatus.INACTIVE)
                    {
                        commonResponse.Status = 403;
                        commonResponse.Message = inactiveUserMsg;
                        return commonResponse;
                    }

                    using (
                        var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled)
                    )
                    {
                        User? updatedAccessToken = null;
                        var response = _jwtService.GenerateAuthenResponse(user);
                        string? resfreshToken = null;
                        if (user.RefreshTokenExpiration <= SettedUpDateTime.GetCurrentVietNamTime())
                        {
                            resfreshToken = _jwtService.GenerateNewRefreshToken();
                        }
                        else
                        {
                            resfreshToken = response.RefreshToken;
                        }

                        if (
                            response != null
                            && response.AccessToken != null
                            && resfreshToken != null
                        )
                        {
                            var newAccessToken = _jwtService.GenerateJwtToken(user);
                            updatedAccessToken = await _userRepository.UpdateAccessTokenAsync(
                                user.Id,
                                newAccessToken
                            );
                        }
                        if (updatedAccessToken == null)
                            throw new Exception(userNotFoundMsg);
                        scope.Complete(); // commit transaction
                        commonResponse.Status = 200;
                        commonResponse.Data = response;
                        commonResponse.Message = loginSuccessMsg;
                    }
                }
                else
                {
                    commonResponse.Status = 401;
                    commonResponse.Message = loginFailedMsg;
                }
            }
            catch (Exception ex)
            {
                string className = nameof(AuthenticationService);
                string methodName = nameof(RefreshAccessTokenAsync);
                _logger.LogError(
                    ex,
                    "An error occurred in {ClassName}.{MethodName}: {ErrorMessage}",
                    className,
                    methodName,
                    ex.Message
                );
                commonResponse.Status = 500;
                commonResponse.Message = internalServerErrorMsg;
            }
            return commonResponse;
        }

        public async Task<User?> DeleteRefreshTokenAsync(Guid userId)
        {
            return await _userRepository.DeleteRefreshTokenAsync(userId);
        }

        public async Task<User?> DeleteAccessTokenAsync(Guid userId)
        {
            return await _userRepository.DeleteAccessTokenAsync(userId);
        }

        public async Task<User?> FindUserByEmailAsync(string email)
        {
            return await _userRepository.FindUserByEmailAsync(email);
        }

        public async Task<CommonResponse> AuthenticateByGoogleAsync(GoogleUserInfoResponse res)
        {
            CommonResponse commonResponse = new CommonResponse();
            string loginSuccessMsg = _config["ResponseMessages:AuthenticationMsg:LoginSuccessMsg"];
            string internalServerErrorMsg = _config[
                "ResponseMessages:AuthenticationMsg:InternalServerErrorMsg"
            ];
            string unVerifyUserMsg = _config["ResponseMessages:AuthenticationMsg:UnVerifyUserMsg"];
            string inactiveUserMsg = _config["ResponseMessages:AuthenticationMsg:InactiveUserMsg"];
            string notAllowMsg = _config["ResponseMessages:AuthenticationMsg:notAllowMsg"];
            try
            {
                var user = await _userRepository.FindUserByEmailAsync(res.Email);
                if (user != null)
                {
                    if (user.Status == UserStatus.UNVERIFIED)
                    {
                        commonResponse.Status = 400;
                        commonResponse.Message = unVerifyUserMsg;
                        return commonResponse;
                    }
                    if (user.Status == UserStatus.INACTIVE)
                    {
                        commonResponse.Status = 403;
                        commonResponse.Message = inactiveUserMsg;
                        return commonResponse;
                    }
                    if (
                        user.Role.Name == "SYSTEM_ADMIN"
                        || user.Role.Name == "BRANCH_ADMIN"
                        || user.Role.Name == "CHARITY"
                    )
                    {
                        commonResponse.Status = 403;
                        commonResponse.Message = notAllowMsg;
                        return commonResponse;
                    }
                    using (
                        var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled)
                    )
                    {
                        // Generate refresh token and add it to user
                        var refreshToken = _jwtService.GenerateNewRefreshToken();
                        DateTime exprireDate = SettedUpDateTime.GetCurrentVietNamTime();
                        decimal expiredTimeDays = _config.GetValue<decimal>(
                            "RefreshToken:ExpiredTimeDays"
                        );
                        exprireDate = exprireDate.AddDays((double)expiredTimeDays);
                        var updatedRefreshToken = await _userRepository.UpdateRefreshTokenAsync(
                            user.Id,
                            refreshToken,
                            exprireDate
                        );
                        if (updatedRefreshToken == null)
                            throw new Exception(internalServerErrorMsg);
                        var response = _jwtService.GenerateAuthenResponse(user);
                        User? updatedAccessToken = null;
                        if (response != null && response.AccessToken != null)
                        {
                            updatedAccessToken = await _userRepository.UpdateAccessTokenAsync(
                                user.Id,
                                response.AccessToken
                            );
                        }
                        if (updatedAccessToken == null)
                            throw new Exception(internalServerErrorMsg);
                        scope.Complete(); // commit transaction
                        commonResponse.Status = 200;
                        commonResponse.Data = response;
                        commonResponse.Message = loginSuccessMsg;
                    }
                }
                else
                {
                    using (
                        var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled)
                    )
                    {
                        AuthenticationResponse authenticationResponse =
                            await CreateUserByFirstTimeLoginByGoogle(res);
                        if (authenticationResponse != null)
                        {
                            commonResponse.Status = 200;
                            commonResponse.Message = loginSuccessMsg;
                            commonResponse.Data = authenticationResponse;
                            scope.Complete();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                string className = nameof(AuthenticationService);
                string methodName = nameof(AuthenticateByGoogleAsync);
                _logger.LogError(
                    ex,
                    "An error occurred in {ClassName}.{MethodName}: {ErrorMessage}",
                    className,
                    methodName,
                    ex.Message
                );
                commonResponse.Status = 500;
                commonResponse.Message = internalServerErrorMsg;
            }
            return commonResponse;
        }

        private async Task<AuthenticationResponse> CreateUserByFirstTimeLoginByGoogle(
            GoogleUserInfoResponse infoResponse
        )
        {
            Role? userRole = await _roleRepository.GetRoleByName(RoleEnum.CONTRIBUTOR.ToString());
            if (userRole != null)
            {
                var refreshToken = _jwtService.GenerateNewRefreshToken();
                DateTime exprireDate = SettedUpDateTime.GetCurrentVietNamTime();
                decimal expiredTimeDays = _config.GetValue<decimal>("RefreshToken:ExpiredTimeDays");
                exprireDate = exprireDate.AddDays((double)expiredTimeDays);

                User user = new User
                {
                    Email = infoResponse.Email,
                    Password = _passwordHasher.Hash(_passwordHasher.GenerateNewPassword()),
                    RefreshToken = refreshToken,
                    RefreshTokenExpiration = exprireDate,
                    Phone = "",
                    RoleId = userRole.Id,
                    Name = infoResponse.Name,
                    Status = UserStatus.ACTIVE,
                };
                var rs = await _userRepository.CreateUserAsync(user);
                string response = _jwtService.GenerateJwtToken(user);
                if (response != null)
                {
                    user.AccessToken = response;
                }

                if (rs != null)
                {
                    var rolePermissions =
                        await _rolePermissionRepository.GetPermissionsByRoleIdAsync(userRole.Id);
                    if (rolePermissions != null && rolePermissions.Count > 0)
                    {
                        foreach (var u in rolePermissions)
                        {
                            await _userPermissionRepository.AddUserPermissionAsync(
                                rs.Id,
                                u!.Id,
                                UserPermissionStatus.PERMITTED
                            );
                        }
                    }
                    var issuer = _config["JwtConfig:Issuer"];
                    int expiredTimeMinutes = _config.GetValue<int>("JwtConfig:ExpiredTimeMinutes");
                    var ExpireTime = SettedUpDateTime
                        .GetCurrentVietNamTime()
                        .AddMinutes(expiredTimeMinutes);
                    AuthenticationResponse authenticationResponse = new AuthenticationResponse
                    {
                        AccessToken = user.AccessToken,
                        Role = userRole.Name,
                        RefreshToken = user.RefreshToken
                    };
                    return authenticationResponse;
                }
                else
                    throw new Exception();
            }
            else
                throw new Exception();
        }

        public async Task<CommonResponse> SendVerificationEmail(VerifyEmailRequest request)
        {
            string internalServerErrorMsg = _config[
                "ResponseMessages:UserMsg:InternalServerErrorMsg"
            ];
            string verifyLink = _config["VerifyLink"];
            string sendVerificationEmailSuccessMsg = _config[
                "ResponseMessages:UserMsg:SendVerificationEmailSuccessMsg"
            ];
            string emailNotFoundMsg = _config["ResponseMessages:UserMsg:EmailNotFoundMsg"];
            CommonResponse commonResponse = new CommonResponse();
            try
            {
                var user = await _userRepository.FindUserByEmailOrPhoneAsync(request.Email);
                if (user == null)
                {
                    commonResponse.Message = emailNotFoundMsg;
                    commonResponse.Status = 400;
                    return commonResponse;
                }
                if (!string.IsNullOrEmpty(verifyLink))
                {
                    using (
                        var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled)
                    )
                    {
                        var code = GenerateNewVerificationCode();
                        user.VerifyCode = code;
                        user.VerifyCodeExpiration = SettedUpDateTime
                            .GetCurrentVietNamTime()
                            .AddMinutes(30);
                        var updatedUser = _userRepository.UpdateUserAsync(user);
                        if (updatedUser != null)
                        {
                            verifyLink += code;
                            await _emailService.SendVerificationEmail(request.Email, verifyLink);
                            commonResponse.Message = sendVerificationEmailSuccessMsg;
                            commonResponse.Status = 200;
                            scope.Complete();
                        }
                        else
                            throw new Exception();
                    }
                }
            }
            catch
            {
                commonResponse.Message = internalServerErrorMsg;
                commonResponse.Status = 500;
            }
            return commonResponse;
        }

        public async Task<CommonResponse> VerifyUserEmail(string verifyCode)
        {
            string internalServerErrorMsg = _config[
                "ResponseMessages:UserMsg:InternalServerErrorMsg"
            ];
            string verifySuccessMsg = _config["ResponseMessages:UserMsg:VerifySuccessMsg"];
            string verifyErrorMsg = _config["ResponseMessages:UserMsg:VerifyErrorMsg"];
            CommonResponse commonResponse = new CommonResponse();
            try
            {
                var user = await _userRepository.FindUserByVerifyCodeAsync(verifyCode);
                if (user != null && user.Status != UserStatus.INACTIVE)
                {
                    using (
                        var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled)
                    )
                    {
                        user.Status = UserStatus.ACTIVE;
                        var rs = await _userRepository.UpdateUserAsync(user);
                        scope.Complete();
                        if (rs != null)
                        {
                            commonResponse.Status = 200;
                            commonResponse.Message = verifySuccessMsg;
                            return commonResponse;
                        }
                        else
                            throw new Exception();
                    }
                }
                else
                {
                    commonResponse.Status = 400;
                    commonResponse.Message = verifyErrorMsg;
                    return commonResponse;
                }
            }
            catch
            {
                commonResponse.Status = 500;
                commonResponse.Message = internalServerErrorMsg;
                return commonResponse;
            }
        }

        private string GenerateNewVerificationCode()
        {
            Guid guid = Guid.NewGuid();
            string verificationCode = guid.ToString();
            return verificationCode;
        }

        private string GenerateNewOtpCode()
        {
            Random random = new Random();
            string otp = random.Next(10000, 99999).ToString();
            return otp;
        }

        private string GenerateNewPasword()
        {
            using (var rng = RandomNumberGenerator.Create())
            {
                var tokenBytes = new byte[32]; // 32 bytes for a secure 256-bit token
                rng.GetBytes(tokenBytes);
                return Convert.ToBase64String(tokenBytes);
            }
        }

        private string GenerateNewVerificationShortCode()
        {
            using (var rng = RandomNumberGenerator.Create())
            {
                var tokenBytes = new byte[6];
                rng.GetBytes(tokenBytes);
                return Convert.ToBase64String(tokenBytes);
            }
        }

        public async Task<CommonResponse> SendOtp(string phone)
        {
            string internalServerErrorMsg = _config[
                "ResponseMessages:UserMsg:InternalServerErrorMsg"
            ];
            string registerByPhoneSuccessMsg = _config[
                "ResponseMessages:UserMsg:RegisterByPhoneSuccessMsg"
            ];
            string phoneNotFoundMsg = _config["ResponseMessages:UserMsg:PhoneNotFoundMsg"];
            CommonResponse commonResponse = new CommonResponse();
            try
            {
                var user = await _userRepository.FindUserByEmailOrPhoneAsync(phone);
                if (user == null)
                {
                    commonResponse.Message = phoneNotFoundMsg;
                    commonResponse.Status = 400;
                    return commonResponse;
                }

                using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
                {
                    var code = GenerateNewOtpCode();
                    user.OtpCode = code;
                    DateTime exprireTime = SettedUpDateTime.GetCurrentVietNamTime();
                    decimal expiredTimeMinutes = _config.GetValue<decimal>("OTP:ExpiredMinutes");
                    exprireTime = exprireTime.AddMinutes((double)expiredTimeMinutes);
                    user.OtpCodeExpiration = exprireTime;
                    var updatedUser = _userRepository.UpdateUserAsync(user);
                    if (updatedUser != null)
                    {
                        _smsService.sendSMS(phone, code);
                        commonResponse.Message = registerByPhoneSuccessMsg;
                        commonResponse.Status = 200;
                        scope.Complete();
                    }
                    else
                        throw new Exception();
                }
            }
            catch
            {
                commonResponse.Message = internalServerErrorMsg;
                commonResponse.Status = 500;
            }
            return commonResponse;
        }

        public async Task<CommonResponse> RegisterUserByPhone(UserRegisterByPhoneRequest request)
        {
            string internalServerErrorMsg = _config[
                "ResponseMessages:UserMsg:InternalServerErrorMsg"
            ];
            string registerByPhoneSuccessMsg = _config[
                "ResponseMessages:UserMsg:RegisterByPhoneSuccessMsg"
            ];
            string duplicatedUserMsg = _config["ResponseMessages:UserMsg:DuplicatedUserMsg"];
            User? user = null;
            CommonResponse commonResponse = new CommonResponse();
            try
            {
                User? check = await _userRepository.FindUserByEmailOrPhoneAsync(request.Phone);
                if (check != null)
                {
                    if (check.Status == UserStatus.UNVERIFIED)
                    {
                        var code = GenerateNewOtpCode();
                        check.OtpCode = code;
                        DateTime exprireTime = SettedUpDateTime.GetCurrentVietNamTime();
                        //decimal expiredTimeMinutes = _config.GetValue<decimal>(
                        //    "OTP:ExpiredMinutes"
                        //);
                        string expiredTimeDaysStr = _config["OTP:ExpiredMinutes"];
                        decimal expiredTimeMinutes = decimal.Parse(expiredTimeDaysStr);

                        exprireTime = exprireTime.AddMinutes((double)expiredTimeMinutes);
                        check.OtpCodeExpiration = exprireTime;
                        using (
                            var scope = new TransactionScope(
                                TransactionScopeAsyncFlowOption.Enabled
                            )
                        )
                        {
                            var updatedUser = _userRepository.UpdateUserAsync(check);
                            scope.Complete();

                            if (updatedUser != null)
                            {
                                _smsService.sendSMS(check.Phone, code);
                                commonResponse.Message =
                                    "Chúng tôi đã gửi mã xác thực đến số điện thoại này";
                                commonResponse.Status = 200;
                                return commonResponse;
                            }
                            else
                                throw new Exception();
                        }
                    }
                    else if (
                        check.Status == UserStatus.ACTIVE || check.Status == UserStatus.INACTIVE
                    )
                    {
                        commonResponse.Status = 400;
                        commonResponse.Message = duplicatedUserMsg;
                        return commonResponse;
                    }
                }
                Role? userRole = await _roleRepository.GetRoleByName(
                    RoleEnum.CONTRIBUTOR.ToString()
                );
                if (request.Phone != null && userRole != null)
                {
                    user = new User
                    {
                        Email = "",
                        Phone = request.Phone,
                        Password = _passwordHasher.Hash(GenerateNewPasword()),
                        RoleId = userRole.Id,
                        Name = request.FullName,
                        Status = UserStatus.UNVERIFIED,
                        Avatar = ""
                    };
                }
                else
                {
                    commonResponse.Message = internalServerErrorMsg;
                    commonResponse.Status = 500;
                    return commonResponse;
                }

                using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
                {
                    if (user != null)
                    {
                        var rs = await _userRepository.CreateUserAsync(user);

                        if (rs != null)
                        {
                            var code = GenerateNewOtpCode();
                            user.OtpCode = code;
                            DateTime exprireTime = SettedUpDateTime.GetCurrentVietNamTime();
                            //decimal expiredTimeMinutes = _config.GetValue<decimal>(
                            //    "OTP:ExpiredMinutes"
                            //);
                            string expiredTimeDaysStr = _config["OTP:ExpiredMinutes"];
                            decimal expiredTimeMinutes = decimal.Parse(expiredTimeDaysStr);
                            exprireTime = exprireTime.AddMinutes((double)expiredTimeMinutes);
                            user.OtpCodeExpiration = exprireTime;
                            var updatedUser = _userRepository.UpdateUserAsync(user);
                            scope.Complete();
                            if (updatedUser != null)
                            {
                                _smsService.sendSMS(user.Phone, code);
                                commonResponse.Message = registerByPhoneSuccessMsg;
                                commonResponse.Status = 200;
                                return commonResponse;
                            }
                            else
                                throw new Exception();
                        }
                        else
                            throw new Exception();
                    }
                    throw new Exception("Can not create user.");
                }
            }
            catch (Exception ex)
            {
                string className = nameof(AuthenticationService);
                string methodName = nameof(RegisterUserByPhone);
                _logger.LogError(
                    ex,
                    "An error occurred in {ClassName}.{MethodName}: {ErrorMessage}",
                    className,
                    methodName,
                    ex.Message
                );
                commonResponse.Message = internalServerErrorMsg;
                commonResponse.Status = 500;
                return commonResponse;
            }
        }

        public async Task<CommonResponse> VerifyUserPhone(VerifyOtpRequest request)
        {
            string internalServerErrorMsg = _config[
                "ResponseMessages:UserMsg:InternalServerErrorMsg"
            ];
            string verifyPhoneSuccessMsg = _config[
                "ResponseMessages:UserMsg:VerifyPhoneSuccessMsg"
            ];
            string verifyPhoneErrorMsg = _config["ResponseMessages:UserMsg:VerifyPhoneErrorMsg"];
            CommonResponse commonResponse = new CommonResponse();
            try
            {
                Role? userRole = await _roleRepository.GetRoleByName(
                    RoleEnum.CONTRIBUTOR.ToString()
                );
                var user = await _userRepository.FindUserByOtpcodeAndPhoneAsync(
                    request.Otp,
                    request.Phone
                );
                if (user != null && user.Status != UserStatus.INACTIVE)
                {
                    using (
                        var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled)
                    )
                    {
                        // active user và tạo một mã mới confirm pasword
                        var code = GenerateNewVerificationCode();
                        user.VerifyCode = code;
                        DateTime exprireTime = SettedUpDateTime
                            .GetCurrentVietNamTime()
                            .AddMinutes(5);
                        user.VerifyCodeExpiration = exprireTime;
                        user.Status = UserStatus.ACTIVE;

                        var rs = await _userRepository.UpdateUserAsync(user);
                        if (rs != null)
                        {
                            if (userRole != null)
                            {
                                var rolePermissions =
                                    await _rolePermissionRepository.GetPermissionsByRoleIdAsync(
                                        userRole.Id
                                    );
                                if (rolePermissions != null && rolePermissions.Count > 0)
                                {
                                    foreach (var u in rolePermissions)
                                    {
                                        await _userPermissionRepository.AddUserPermissionAsync(
                                            rs.Id,
                                            u!.Id,
                                            UserPermissionStatus.PERMITTED
                                        );
                                    }
                                }
                            }
                            scope.Complete();
                            commonResponse.Data = code;
                            commonResponse.Status = 200;
                            commonResponse.Message = verifyPhoneSuccessMsg;
                            return commonResponse;
                        }
                        else
                            throw new Exception();
                    }
                }
                else
                {
                    commonResponse.Status = 400;
                    commonResponse.Message = verifyPhoneErrorMsg;
                    return commonResponse;
                }
            }
            catch (Exception ex)
            {
                string className = nameof(AuthenticationService);
                string methodName = nameof(VerifyUserPhone);
                _logger.LogError(
                    ex,
                    "An error occurred in {ClassName}.{MethodName}: {ErrorMessage}",
                    className,
                    methodName,
                    ex.Message
                );
                commonResponse.Status = 500;
                commonResponse.Message = internalServerErrorMsg;
                return commonResponse;
            }
        }

        public async Task<CommonResponse> ResetPasswordAsync(ResetPasswordRequest request)
        {
            string internalServerErrorMsg = _config[
                "ResponseMessages:UserMsg:InternalServerErrorMsg"
            ];
            string registerSuccessMsg = _config["ResponseMessages:UserMsg:RegisterSuccessMsg"];
            string userNotFoundMsg = _config["ResponseMessages:UserMsg:UserNotFoundMsg"];
            CommonResponse commonResponse = new CommonResponse();
            try
            {
                var user = await _userRepository.FindUserByVerifyCodeAsync(request.VerifyCode);
                if (user == null)
                {
                    commonResponse.Message = userNotFoundMsg;
                    commonResponse.Status = 400;
                }
                else
                {
                    user.Password = _passwordHasher.Hash(request.Password);
                    using (
                        var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled)
                    )
                    {
                        var updateUser = await _userRepository.UpdateUserAsync(user);
                        if (updateUser != null)
                        {
                            scope.Complete();
                            commonResponse.Message = registerSuccessMsg;
                            commonResponse.Status = 200;
                        }
                        else
                            throw new Exception();
                    }
                }
            }
            catch (Exception ex)
            {
                string className = nameof(AuthenticationService);
                string methodName = nameof(ResetPasswordAsync);
                _logger.LogError(
                    ex,
                    "An error occurred in {ClassName}.{MethodName}: {ErrorMessage}",
                    className,
                    methodName,
                    ex.Message
                );
                commonResponse.Message = internalServerErrorMsg;
                commonResponse.Status = 500;
            }
            return commonResponse;
        }

        public async Task<CommonResponse> LinkToEmail(string verifyCode, Guid userId, string email)
        {
            string internalServerErrorMsg = _config[
                "ResponseMessages:UserMsg:InternalServerErrorMsg"
            ];
            string linkToEmalSuccessMsg = _config["ResponseMessages:UserMsg:LinkToEmalSuccessMsg"];
            string linkToEmailFailedMsg = _config["ResponseMessages:UserMsg:LinkToEmailFailedMsg"];
            CommonResponse commonResponse = new CommonResponse();
            try
            {
                User? currentUser = await _userRepository.FindUserByVerifyCodeAsync(verifyCode);
                User? otherUser = await _userRepository.FindUserByEmailOrPhoneAsync(email);

                if (
                    currentUser != null
                    && currentUser.Id == userId
                    && (currentUser == otherUser || otherUser == null)
                )
                {
                    currentUser.Email = email;
                    using (
                        var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled)
                    )
                    {
                        var rs = await _userRepository.UpdateUserAsync(currentUser);

                        if (rs != null)
                        {
                            commonResponse.Status = 200;
                            commonResponse.Message = linkToEmalSuccessMsg;
                            scope.Complete();
                            return commonResponse;
                        }
                    }
                    return commonResponse;
                }
                else
                {
                    commonResponse.Status = 400;
                    commonResponse.Message = linkToEmailFailedMsg;
                    return commonResponse;
                }
            }
            catch (Exception ex)
            {
                string className = nameof(AuthenticationService);
                string methodName = nameof(LinkToEmail);
                _logger.LogError(
                    ex,
                    "An error occurred in {ClassName}.{MethodName}: {ErrorMessage}",
                    className,
                    methodName,
                    ex.Message
                );
                commonResponse.Status = 500;
                commonResponse.Message = internalServerErrorMsg;
                return commonResponse;
            }
        }

        public async Task<CommonResponse> SendVerifyCodeToEmail(
            VerifyEmailRequest request,
            Guid UserId
        )
        {
            string internalServerErrorMsg = _config[
                "ResponseMessages:UserMsg:InternalServerErrorMsg"
            ];
            string sendVerificationEmailSuccessMsg = _config[
                "ResponseMessages:UserMsg:SendVerificationEmailSuccessMsg"
            ];
            string userNotFoundMsg = _config["ResponseMessages:UserMsg:UserNotFoundMsg"];
            string duplicatedEmailMsg = _config["ResponseMessages:UserMsg:DuplicatedEmailMsg"];
            CommonResponse commonResponse = new CommonResponse();
            try
            {
                User? user = await _userRepository.FindUserByIdAsync(UserId);
                User? otherUser = await _userRepository.FindUserByEmailOrPhoneAsync(request.Email);
                if (user == null)
                {
                    commonResponse.Message = userNotFoundMsg;
                    commonResponse.Status = 400;
                    return commonResponse;
                }
                if (user != null && otherUser != user && otherUser != null)
                {
                    commonResponse.Message = duplicatedEmailMsg;
                    commonResponse.Status = 400;
                    return commonResponse;
                }

                using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
                {
                    var code = GenerateNewVerificationShortCode();
                    user!.VerifyCode = code;
                    user.VerifyCodeExpiration = SettedUpDateTime
                        .GetCurrentVietNamTime()
                        .AddMinutes(3);
                    var updatedUser = _userRepository.UpdateUserAsync(user);
                    if (updatedUser != null)
                    {
                        await _emailService.SendVerifyCodeToEmail(request.Email, code);
                        commonResponse.Message = sendVerificationEmailSuccessMsg;
                        commonResponse.Status = 200;
                        scope.Complete();
                    }
                    else
                        throw new Exception();
                }
            }
            catch (Exception ex)
            {
                string className = nameof(AuthenticationService);
                string methodName = nameof(SendVerifyCodeToEmail);
                _logger.LogError(
                    ex,
                    "An error occurred in {ClassName}.{MethodName}: {ErrorMessage}",
                    className,
                    methodName,
                    ex.Message
                );
                commonResponse.Message = internalServerErrorMsg;
                commonResponse.Status = 500;
            }
            return commonResponse;
        }

        public async Task<CommonResponse> GetListUserForSystemAdminForAssignBranchMember(
            string? searchStr
        )
        {
            string internalServerErrorMsg = _config[
                "ResponseMessages:CommonMsg:InternalServerErrorMsg"
            ];

            CommonResponse commonResponse = new CommonResponse();
            try
            {
                var rs = await _userRepository.FindBranchAdminForAssignToBranchAsync();
                if (rs != null && rs.Count > 0)
                {
                    var selectedInfo = rs.Select(
                            u =>
                                new
                                {
                                    u.Id,
                                    u.Email,
                                    u.Phone,
                                    FullName = u.Name,
                                    u.Avatar,
                                }
                        )
                        .ToList();
                    commonResponse.Data = selectedInfo;
                }
                commonResponse.Status = 200;
            }
            catch (Exception ex)
            {
                string className = nameof(AuthenticationService);
                string methodName = nameof(GetListUserForSystemAdminForAssignBranchMember);
                _logger.LogError(
                    ex,
                    "An error occurred in {ClassName}.{MethodName}: {ErrorMessage}",
                    className,
                    methodName,
                    ex.Message
                );
                commonResponse.Message = internalServerErrorMsg;
                commonResponse.Status = 500;
            }
            return commonResponse;
        }

        public async Task<CommonResponse> UpdateProfile(Guid userId, UserProfileRequest request)
        {
            CommonResponse commonResponse = new CommonResponse();
            string internalServerErrorMsg = _config[
                "ResponseMessages:CommonMsg:InternalServerErrorMsg"
            ];
            string userNotFoundMsg = _config["ResponseMessages:UserMsg:UserNotFoundMsg"];
            string updateUserProfileSucessMsg = _config[
                "ResponseMessages:UserMsg:UpdateUserProfileSucessMsg"
            ];
            try
            {
                User? user = await _userRepository.FindUserByIdAsync(userId);

                if (user != null)
                {
                    user.Name = !string.IsNullOrEmpty(request.Name) ? request.Name : user.Name;

                    if (request.Location != null && request.Location.Length >= 2)
                    {
                        user.Location =
                            request.Location[0].ToString() + ", " + request.Location[1].ToString();
                    }

                    user.Address = !string.IsNullOrEmpty(request.Address)
                        ? request.Address
                        : user.Address;
                    if (request.Phone != null)
                    {
                        if (
                            await _userRepository.FindUserByEmailOrPhoneAsync(request.Phone) == null
                        )
                        {
                            user.Phone = !string.IsNullOrEmpty(request.Phone)
                                ? request.Phone
                                : user.Phone;
                        }
                        else
                        {
                            commonResponse.Message = "Số điện thoại đã tồn tại.";
                            commonResponse.Status = 400;
                        }
                    }

                    if (request.Avatar != null)
                    {
                        using (var stream = request.Avatar.OpenReadStream())
                        {
                            string imageName =
                                Guid.NewGuid().ToString()
                                + Path.GetExtension(request.Avatar.FileName);
                            string imageUrl = await _firebaseStorageService.UploadImageToFirebase(
                                stream,
                                imageName
                            );
                            user.Avatar = imageUrl;
                        }
                    }

                    using (
                        var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled)
                    )
                    {
                        var rs = await _userRepository.UpdateUserAsync(user);
                        if (rs == null)
                            throw new Exception();
                        commonResponse.Message = updateUserProfileSucessMsg;
                        scope.Complete();
                    }
                    commonResponse.Status = 200;
                }
                else
                {
                    commonResponse.Message = userNotFoundMsg;
                    commonResponse.Status = 400;
                }
            }
            catch (Exception ex)
            {
                string className = nameof(AuthenticationService);
                string methodName = nameof(UpdateProfile);
                _logger.LogError(
                    ex,
                    "An error occurred in {ClassName}.{MethodName}: {ErrorMessage}",
                    className,
                    methodName,
                    ex.Message
                );
                commonResponse.Message = internalServerErrorMsg;
                commonResponse.Status = 500;
            }
            return commonResponse;
        }

        public async Task<CommonResponse> GetProfile(Guid userId)
        {
            CommonResponse commonResponse = new CommonResponse();
            string internalServerErrorMsg = _config[
                "ResponseMessages:CommonMsg:InternalServerErrorMsg"
            ];
            string userNotFoundMsg = _config["ResponseMessages:UserMsg:UserNotFoundMsg"];
            string updateUserProfileSucessMsg = _config[
                "ResponseMessages:UserMsg:UpdateUserProfileSucessMsg"
            ];
            try
            {
                User? user = await _userRepository.FindUserProfileByIdAsync(userId);

                if (user != null)
                {
                    if (user.Role.Name == RoleEnum.CHARITY.ToString())
                    {
                        bool IsHeadquarter = false;
                        CharityUnit? charityUnit =
                            await _charityUnitRepository.FindCharityUnitByUserIdAsync(userId);
                        string? LegalDocuments = null;
                        if (charityUnit != null)
                        {
                            IsHeadquarter =
                                charityUnit!.IsHeadquarter != null
                                    ? (bool)charityUnit!.IsHeadquarter
                                    : false;
                            LegalDocuments = charityUnit.LegalDocuments;
                        }
                        var userResponse = new
                        {
                            Id = userId,
                            user.Address,
                            user.Location,
                            user.Avatar,
                            user.Name,
                            user.Phone,
                            user.Email,
                            IsHeadquarter,
                            LegalDocuments,
                            charityUnit!.Description,
                            CharityUnitId = charityUnit.Id,
                            Charity = new
                            {
                                charityUnit.Charity.Name,
                                charityUnit.Charity.Id,
                                charityUnit.Charity.Email,
                                Status = charityUnit.Status.ToString(),
                                charityUnit.Charity.Description,
                                charityUnit.Charity.Logo
                            },
                            CollaboratorStatus = user.IsCollaborator
                        };
                        commonResponse.Data = userResponse;
                    }
                    else if (user.IsCollaborator)
                    {
                        var userResponse = new
                        {
                            Id = userId,
                            user.Address,
                            user.Location,
                            user.Avatar,
                            user.Name,
                            user.Phone,
                            user.Email,
                            CollaboratorStatus = user.IsCollaborator
                        };
                        commonResponse.Data = userResponse;
                    }
                    else
                    {
                        var userResponse = new
                        {
                            Id = userId,
                            user.Address,
                            user.Location,
                            user.Avatar,
                            user.Name,
                            user.Phone,
                            user.Email,
                            CollaboratorStatus = user.IsCollaborator
                        };
                        commonResponse.Data = userResponse;
                    }

                    commonResponse.Status = 200;
                }
                else
                {
                    commonResponse.Message = userNotFoundMsg;
                    commonResponse.Status = 400;
                }
            }
            catch (Exception ex)
            {
                string className = nameof(AuthenticationService);
                string methodName = nameof(GetProfile);
                _logger.LogError(
                    ex,
                    "An error occurred in {ClassName}.{MethodName}: {ErrorMessage}",
                    className,
                    methodName,
                    ex.Message
                );
                commonResponse.Message = internalServerErrorMsg;
                commonResponse.Status = 500;
            }
            return commonResponse;
        }

        public async Task<CommonResponse> CreateAccountForBranchAdmin(
            BranchAdminCreatingRequest request
        )
        {
            CommonResponse commonResponse = new CommonResponse();
            string internalServerErrorMsg = _config[
                "ResponseMessages:CommonMsg:InternalServerErrorMsg"
            ];
            string duplicatedUserMsg = _config["ResponseMessages:UserMsg:DuplicatedUserMsg"];
            Role? branchAdminRole = await _roleRepository.GetRoleByName(
                RoleEnum.BRANCH_ADMIN.ToString()
            );

            try
            {
                User? emailCheck = await _userRepository.FindUserByEmailAsync(request.Email);
                User? phoneCheck = await _userRepository.FindUserByPhoneAsync(request.Phone);
                if (phoneCheck != null || emailCheck != null)
                {
                    commonResponse.Status = 400;
                    commonResponse.Message = duplicatedUserMsg;
                    return commonResponse;
                }
                if (branchAdminRole != null)
                {
                    var password = _passwordHasher.GenerateNewPassword();

                    User user = new User();
                    user.Email = request.Email;
                    user.Name = request.FullName;
                    user.Address = request.Address;
                    user.Status = UserStatus.ACTIVE;
                    user.RoleId = branchAdminRole.Id;
                    if (request.Location != null && request.Location.Length >= 2)
                    {
                        user.Location =
                            request.Location[0].ToString() + ", " + request.Location[1].ToString();
                    }
                    user.Phone = request.Phone;
                    if (string.IsNullOrEmpty(user.Password))
                    {
                        user.Password = _passwordHasher.Hash(password);
                    }
                    using (var stream = request.Avatar!.OpenReadStream())
                    {
                        string imageName =
                            Guid.NewGuid().ToString() + Path.GetExtension(request.Avatar.FileName);
                        string imageUrl = await _firebaseStorageService.UploadImageToFirebase(
                            stream,
                            imageName
                        );
                        user.Avatar = imageUrl;
                    }
                    using (
                        var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled)
                    )
                    {
                        var rs = await _userRepository.UpdateUserAsync(user);
                        var rolePermissions =
                            await _rolePermissionRepository.GetPermissionsByRoleIdAsync(
                                branchAdminRole.Id
                            );
                        if (rolePermissions != null && rolePermissions.Count > 0)
                        {
                            foreach (var u in rolePermissions)
                            {
                                await _userPermissionRepository.AddUserPermissionAsync(
                                    rs!.Id,
                                    u!.Id,
                                    UserPermissionStatus.BANNED
                                );
                            }
                        }

                        await _emailService.SendNotificationForCreatingAccountForBranchAdminEmail(
                            request.Email,
                            request.Phone,
                            request.Email,
                            password
                        );
                        if (rs == null)
                            throw new Exception();
                        commonResponse.Status = 200;
                        commonResponse.Message = "Tạo thành công";
                        scope.Complete();
                    }
                }
                else
                    throw new Exception("Error at GetListUserForSystemAdmin");
            }
            catch (Exception ex)
            {
                string className = nameof(AuthenticationService);
                string methodName = nameof(CreateAccountForBranchAdmin);
                _logger.LogError(
                    ex,
                    "An error occurred in {ClassName}.{MethodName}: {ErrorMessage}",
                    className,
                    methodName,
                    ex.Message
                );
                commonResponse.Message = internalServerErrorMsg;
                commonResponse.Status = 500;
            }
            return commonResponse;
        }

        public async Task<CommonResponse> GetUserAsync(
            UserFilterRequest request,
            int? pageSize,
            int? page,
            SortType? sortType = SortType.DES
        )
        {
            string internalServerErrorMsg = _config[
                "ResponseMessages:CommonMsg:InternalServerErrorMsg"
            ];
            CommonResponse commonResponse = new CommonResponse();
            try
            {
                // var role = await _roleRepository.GetRoleByName(RoleEnum.VOLUNTEER.ToString());
                // request.RoleIds = new List<Guid> { role.Id };

                List<User>? rs = await _userRepository.FindUserAsync(request);

                if (rs != null && rs.Count > 0)
                {
                    Pagination pagination = new Pagination();
                    pagination.PageSize = pageSize == null ? 10 : pageSize.Value;
                    pagination.CurrentPage = page == null ? 1 : page.Value;
                    pagination.Total = rs.Count;
                    rs = rs.Skip((pagination.CurrentPage - 1) * pagination.PageSize)
                        .Take(pagination.PageSize)
                        .ToList();
                    List<SimpleUserResponse> simpleUserResponses = rs.Select(
                            u =>
                                new SimpleUserResponse
                                {
                                    FullName = u.Name!,
                                    Avatar = u.Avatar,
                                    Id = u.Id,
                                    Role = u.Role.DisplayName,
                                    Phone = u.Phone,
                                    Email = u.Email,
                                    Status = u.Status.ToString()
                                }
                        )
                        .ToList();

                    if (sortType != null)
                    {
                        if (sortType == SortType.ASC)
                        {
                            simpleUserResponses = simpleUserResponses
                                .OrderBy(u => u.FullName)
                                .ToList();
                        }
                        else
                        {
                            simpleUserResponses = simpleUserResponses
                                .OrderByDescending(u => u.FullName)
                                .ToList();
                        }
                    }
                    commonResponse.Data = simpleUserResponses;
                    commonResponse.Pagination = pagination;
                }
                commonResponse.Status = 200;
                return commonResponse;
            }
            catch (Exception ex)
            {
                string className = nameof(AuthenticationService);
                string methodName = nameof(GetUserAsync);
                _logger.LogError(
                    ex,
                    "An error occurred in {ClassName}.{MethodName}: {ErrorMessage}",
                    className,
                    methodName,
                    ex.Message
                );
                commonResponse.Message = internalServerErrorMsg;
                commonResponse.Status = 500;
            }
            return commonResponse;
        }

        public async Task<CommonResponse> GetUserById(Guid Id)
        {
            string internalServerErrorMsg = _config[
                "ResponseMessages:CommonMsg:InternalServerErrorMsg"
            ];
            string userNotFoundMsg = _config["ResponseMessages:UserMsg:UserNotFoundMsg"];
            CommonResponse commonResponse = new CommonResponse();
            try
            {
                User? rs = await _userRepository.FindUserByIdAsync(Id);

                if (rs != null)
                {
                    UserResponse userResponse = new UserResponse
                    {
                        Id = rs.Id,
                        FullName = rs.Name,
                        Name = rs.Name,
                        Phone = rs.Phone,
                        Address = rs.Address,
                        Avatar = rs.Avatar,
                        Email = rs.Email,
                        Status = rs.Status.ToString()
                    };
                    commonResponse.Data = userResponse;
                    commonResponse.Status = 200;
                }
                else
                {
                    commonResponse.Message = userNotFoundMsg;
                    commonResponse.Status = 400;
                }
            }
            catch (Exception ex)
            {
                string className = nameof(AuthenticationService);
                string methodName = nameof(CreateAccountForBranchAdmin);
                _logger.LogError(
                    ex,
                    "An error occurred in {ClassName}.{MethodName}: {ErrorMessage}",
                    className,
                    methodName,
                    ex.Message
                );
                commonResponse.Message = internalServerErrorMsg;
                commonResponse.Status = 500;
            }
            return commonResponse;
        }

        public async Task<CommonResponse> UpdateUserAsync(
            UserUpdaingForAdminRequest request,
            Guid userId
        )
        {
            CommonResponse commonResponse = new CommonResponse();
            string internalServerErrorMsg = _config[
                "ResponseMessages:CommonMsg:InternalServerErrorMsg"
            ];
            string userNotFoundMsg = _config["ResponseMessages:UserMsg:UserNotFoundMsg"];
            string updateUserProfileSucessMsg = _config[
                "ResponseMessages:UserMsg:UpdateUserProfileSucessMsg"
            ];
            string unauthenticationMsg = _config["ResponseMessages:UserMsg:UnauthenticationMsg"];
            try
            {
                User? user = await _userRepository.FindUserByIdAsync(userId);

                if (user!.Role.Name == RoleEnum.BRANCH_ADMIN.ToString())
                {
                    commonResponse.Message = unauthenticationMsg;
                    commonResponse.Status = 403;
                }
                if (user != null)
                {
                    using (
                        var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled)
                    )
                    {
                        if (request.Status != null && request.Status == UserStatus.INACTIVE)
                        {
                            user.Status = (UserStatus)request.Status;
                            await BanAccount(user);
                        }
                        else if (request.Status != null && request.Status == UserStatus.ACTIVE)
                        {
                            user.Status = (UserStatus)request.Status;
                            await UnBanAccount(user);
                        }

                        commonResponse.Message = updateUserProfileSucessMsg;
                        scope.Complete();
                    }
                    commonResponse.Status = 200;
                }
                else
                {
                    commonResponse.Message = userNotFoundMsg;
                    commonResponse.Status = 400;
                }
            }
            catch (Exception ex)
            {
                string className = nameof(AuthenticationService);
                string methodName = nameof(UpdateUserAsync);
                _logger.LogError(
                    ex,
                    "An error occurred in {ClassName}.{MethodName}: {ErrorMessage}",
                    className,
                    methodName,
                    ex.Message
                );
                commonResponse.Message = internalServerErrorMsg;
                commonResponse.Status = 500;
            }
            return commonResponse;
        }

        private async Task BanAccount(User user)
        {
            user.Status = UserStatus.INACTIVE;
            foreach (var up in user.UserPermissions)
            {
                up.Status = UserPermissionStatus.BANNED;
            }
            await _userRepository.UpdateUserAsync(user);
        }

        private async Task UnBanAccount(User user)
        {
            user.Status = UserStatus.ACTIVE;
            foreach (var up in user.UserPermissions)
            {
                up.Status = UserPermissionStatus.PERMITTED;
            }
            await _userRepository.UpdateUserAsync(user);
        }

        public async Task<CommonResponse> UpdatePasswordAsync(
            UpdatePasswordRequest request,
            Guid userId
        )
        {
            string internalServerErrorMsg = _config[
                "ResponseMessages:CommonMsg:InternalServerErrorMsg"
            ];
            string userNotFoundMsg = _config["ResponseMessages:UserMsg:UserNotFoundMsg"];
            string incorrectPasswordMsg = _config["ResponseMessages:UserMsg:IncorrectPasswordMsg"];
            string updateUserProfileSucessMsg = _config[
                "ResponseMessages:UserMsg:UpdateUserProfileSucessMsg"
            ];
            CommonResponse commonResponse = new CommonResponse();
            try
            {
                User? user = await _userRepository.FindUserByIdAsync(userId);

                if (user != null)
                {
                    if (_passwordHasher.Verify(request.oldPassword, user.Password))
                    {
                        user.Password = _passwordHasher.Hash(request.newPassword);
                        User? rs = await _userRepository.UpdateUserAsync(user);
                        if (rs != null)
                        {
                            commonResponse.Message = updateUserProfileSucessMsg;
                            commonResponse.Status = 200;
                        }
                    }
                    else
                    {
                        commonResponse.Message = incorrectPasswordMsg;
                        commonResponse.Status = 400;
                    }
                }
                else
                {
                    commonResponse.Message = userNotFoundMsg;
                    commonResponse.Status = 400;
                }
            }
            catch (Exception ex)
            {
                string className = nameof(UpdatePasswordAsync);
                string methodName = nameof(CreateAccountForBranchAdmin);
                _logger.LogError(
                    ex,
                    "An error occurred in {ClassName}.{MethodName}: {ErrorMessage}",
                    className,
                    methodName,
                    ex.Message
                );
                commonResponse.Message = internalServerErrorMsg;
                commonResponse.Status = 500;
            }
            return commonResponse;
        }

        public async Task<CommonResponse> UpdateDeviceToken(Guid userId, string deviceToken)
        {
            string internalServerErrorMsg = _config[
                "ResponseMessages:CommonMsg:InternalServerErrorMsg"
            ];
            string userNotFoundMsg = _config["ResponseMessages:UserMsg:UserNotFoundMsg"];
            string incorrectPasswordMsg = _config["ResponseMessages:UserMsg:IncorrectPasswordMsg"];
            string updateUserProfileSucessMsg = _config[
                "ResponseMessages:UserMsg:UpdateUserProfileSucessMsg"
            ];
            CommonResponse commonResponse = new CommonResponse();
            try
            {
                User? user = await _userRepository.FindUserByIdAsync(userId);

                if (user != null)
                {
                    user.DeviceToken = deviceToken;
                    User? rs = await _userRepository.UpdateUserAsync(user);
                    if (rs != null)
                    {
                        commonResponse.Message = updateUserProfileSucessMsg;
                        commonResponse.Status = 200;
                    }
                }
                else
                {
                    commonResponse.Message = userNotFoundMsg;
                    commonResponse.Status = 400;
                }
            }
            catch (Exception ex)
            {
                string className = nameof(UserService);
                string methodName = nameof(UpdateDeviceToken);
                _logger.LogError(
                    ex,
                    "An error occurred in {ClassName}.{MethodName}: {ErrorMessage}",
                    className,
                    methodName,
                    ex.Message
                );
                commonResponse.Message = internalServerErrorMsg;
                commonResponse.Status = 500;
            }
            return commonResponse;
        }

        public async Task<CommonResponse> RegisterUserByPhoneForBranchAdmin(
            UserRegisterByPhoneRequest request
        )
        {
            string internalServerErrorMsg = _config[
                "ResponseMessages:UserMsg:InternalServerErrorMsg"
            ];

            string defaultAvatar = _config["DefaultProfile:DefaultAvatar"];
            string registerByPhoneSuccessMsg = _config[
                "ResponseMessages:UserMsg:RegisterByPhoneSuccessMsg"
            ];
            string duplicatedUserMsg = _config["ResponseMessages:UserMsg:DuplicatedUserMsg"];
            User? user = null;
            CommonResponse commonResponse = new CommonResponse();
            try
            {
                if (await _userRepository.FindUserByEmailOrPhoneAsync(request.Phone) != null)
                {
                    commonResponse.Status = 400;
                    commonResponse.Message = duplicatedUserMsg;
                    return commonResponse;
                }
                Role? userRole = await _roleRepository.GetRoleByName(
                    RoleEnum.CONTRIBUTOR.ToString()
                );
                if (request.Phone != null && userRole != null)
                {
                    user = new User
                    {
                        Email = "",
                        Phone = request.Phone,
                        Password = _passwordHasher.Hash(GenerateNewPasword()),
                        RoleId = userRole.Id,
                        Name = request.FullName,
                        Status = UserStatus.UNVERIFIED,
                        Avatar = defaultAvatar
                    };
                    await _userRepository.CreateUserAsync(user);
                }
                else
                {
                    commonResponse.Message = internalServerErrorMsg;
                    commonResponse.Status = 500;
                    return commonResponse;
                }

                commonResponse.Message = registerByPhoneSuccessMsg;
                commonResponse.Message = registerByPhoneSuccessMsg;
                commonResponse.Status = 200;
                return commonResponse;
            }
            catch (Exception ex)
            {
                string className = nameof(AuthenticationService);
                string methodName = nameof(RegisterUserByPhone);
                _logger.LogError(
                    ex,
                    "An error occurred in {ClassName}.{MethodName}: {ErrorMessage}",
                    className,
                    methodName,
                    ex.Message
                );
                commonResponse.Message = internalServerErrorMsg;
                commonResponse.Status = 500;
                return commonResponse;
            }
        }

        public async Task<CommonResponse> GetUserAsyncByPhone(
            UserFilterRequest request,
            int? pageSize,
            int? page,
            SortType? sortType = SortType.DES
        )
        {
            string internalServerErrorMsg = _config[
                "ResponseMessages:CommonMsg:InternalServerErrorMsg"
            ];
            CommonResponse commonResponse = new CommonResponse();
            try
            {
                var role = await _roleRepository.GetRoleByName(RoleEnum.CONTRIBUTOR.ToString());
                request.RoleIds = new List<Guid> { role!.Id };

                List<User>? rs = await _userRepository.FindUserAsync(request);

                if (rs != null && rs.Count > 0)
                {
                    Pagination pagination = new Pagination();
                    pagination.PageSize = pageSize == null ? 10 : pageSize.Value;
                    pagination.CurrentPage = page == null ? 1 : page.Value;
                    pagination.Total = rs.Count;
                    rs = rs.Skip((pagination.CurrentPage - 1) * pagination.PageSize)
                        .Take(pagination.PageSize)
                        .ToList();
                    List<SimpleUserResponse> simpleUserResponses = rs.Select(
                            u =>
                                new SimpleUserResponse
                                {
                                    FullName = u.Name!,
                                    Avatar = u.Avatar,
                                    Id = u.Id,
                                    Role = u.Role.DisplayName,
                                    Phone = u.Phone,
                                    Email = u.Email,
                                    Status = u.Status.ToString()
                                }
                        )
                        .ToList();

                    if (sortType != null)
                    {
                        if (sortType == SortType.ASC)
                        {
                            simpleUserResponses = simpleUserResponses
                                .OrderBy(u => u.FullName)
                                .ToList();
                        }
                        else
                        {
                            simpleUserResponses = simpleUserResponses
                                .OrderByDescending(u => u.FullName)
                                .ToList();
                        }
                    }
                    commonResponse.Data = simpleUserResponses;
                    commonResponse.Pagination = pagination;
                }
                commonResponse.Status = 200;
                return commonResponse;
            }
            catch (Exception ex)
            {
                string className = nameof(AuthenticationService);
                string methodName = nameof(GetUserAsync);
                _logger.LogError(
                    ex,
                    "An error occurred in {ClassName}.{MethodName}: {ErrorMessage}",
                    className,
                    methodName,
                    ex.Message
                );
                commonResponse.Message = internalServerErrorMsg;
                commonResponse.Status = 500;
            }
            return commonResponse;
        }
    }
}
