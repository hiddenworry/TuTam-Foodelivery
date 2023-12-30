using BusinessLogic.Services;
using BusinessLogic.Utils.SecurityServices;
using DataAccess.EntityEnums;
using DataAccess.Models.Requests;
using DataAccess.Models.Responses;
using DataAccess.ModelsEnum;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodDonationDeliveryManagementAPI.Controllers
{
    [Route("users")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IPasswordHasher passwordHasher;
        private readonly ILogger<UsersController> _logger;
        private readonly IConfiguration _config;
        private readonly IJwtService _jwtService;
        private readonly IUserPermissionService _userPermissionService;

        public UsersController(
            IUserService userService,
            IPasswordHasher passwordHasher,
            ILogger<UsersController> logger,
            IConfiguration config,
            IJwtService jwtService,
            IUserPermissionService userPermissionService
        )
        {
            _userService = userService;
            this.passwordHasher = passwordHasher;
            _logger = logger;
            _config = config;
            _jwtService = jwtService;
            _userPermissionService = userPermissionService;
        }

        ///// <summary>
        ///// Send email verify.
        ///// </summary>
        ///// <remarks>
        ///// Dùng để gửi email xác thực
        ///// Paramters:
        ///// - **Email**:Địa chỉ email(Not Null).
        ///// </remarks>
        ///// <response code="200">If success.</response>
        ///// <response code="400">If the emails not found.</response>
        ///// <response code="500">Internal server error.</response>
        //[Authorize]
        //[HttpPost("email/verification")]
        //public async Task<IActionResult> SendEmailVerify([FromBody]VerifyEmailRequest request)
        //{
        //    CommonResponse commonResponse = new CommonResponse();
        //    string internalServerErrorMsg = _config["ResponseMessages:UserMsg:InternalServerErrorMsg"];
        //    try
        //    {
        //        commonResponse = await _userService.SendVerificationEmail(request);
        //        switch (commonResponse.Status)
        //        {
        //            case 200:
        //                return Ok(commonResponse);
        //            case 400:
        //                return BadRequest(commonResponse);
        //            default:
        //                return StatusCode(500, commonResponse);
        //        }

        //    }
        //    catch
        //    {
        //        commonResponse.Message = internalServerErrorMsg;
        //        commonResponse.Status = 500;
        //        return StatusCode(500, commonResponse);

        //    }

        //}

        ///// <summary>
        ///// Verify user by code.
        ///// </summary>
        ///// <remarks>
        ///// Dùng để xác thực danh tính của người dùng qua mã code
        ///// Paramters:
        ///// - **code**:Mã code được gửi về mail hoặc sdt (Not Null).
        ///// </remarks>
        ///// <response code="200">If success.</response>
        ///// <response code="400">If user is inactive or code is incorrect.</response>
        ///// <response code="500">Internal server error.</response>
        //[HttpGet("/verification")]
        //public async Task<IActionResult> VerifyEmail(string code)
        //{
        //    CommonResponse commonResponse = new CommonResponse();
        //    string internalServerErrorMsg = _config["ResponseMessages:UserMsg:InternalServerErrorMsg"];
        //    try
        //    {
        //        commonResponse = await _userService.VerifyUserEmail(code);
        //        switch (commonResponse.Status)
        //        {
        //            case 200:
        //                return Ok(commonResponse);
        //            case 400:
        //                return BadRequest(commonResponse);
        //            default:
        //                return StatusCode(500, commonResponse);
        //        }

        //    }
        //    catch
        //    {
        //        commonResponse.Message = internalServerErrorMsg;
        //        commonResponse.Status = 500;
        //        return StatusCode(500, commonResponse);

        //    }

        //}




        /// <summary>
        /// Use for Register. abc
        /// </summary>
        /// <remarks>
        /// Register
        /// Parameters:
        /// - **Phone**: Phone number (Not null and must be in phone number format).
        /// - **FullName**: Name (not null, from 8 to 60 characters).
        /// </remarks>
        /// <response code="200">If success.</response>
        /// <response code="400">If validation error.</response>
        /// <response code="500">Internal server error.</response>
        [HttpPost]
        public async Task<IActionResult> RegisterForUserByPhone(
            [FromBody] UserRegisterByPhoneRequest request
        )
        {
            CommonResponse commonResponse = new CommonResponse();
            string internalServerErrorMsg = _config[
                "ResponseMessages:UserMsg:InternalServerErrorMsg"
            ];
            try
            {
                commonResponse = await _userService.RegisterUserByPhone(request);
                switch (commonResponse.Status)
                {
                    case 200:
                        return Ok(commonResponse);
                    case 400:
                        return BadRequest(commonResponse);
                    default:
                        return StatusCode(500, commonResponse);
                }
            }
            catch (Exception ex)
            {
                commonResponse.Status = 500;
                commonResponse.Message = internalServerErrorMsg;
                _logger.LogError(ex, "An error occurred: {ErrorMessage}", ex.Message);
                return StatusCode(500, commonResponse);
            }
        }

        /// <summary>
        ///Use for Register.
        /// </summary>
        /// <remarks>
        /// Register
        /// Parameters:
        /// - **Phone**: Phone number (Not null and must be in phone number format).
        /// - **Otp**: One-time password sent (not null).
        /// </remarks>
        /// <response code="200">If success.
        /// "CommonResponse":
        /// {
        ///
        /// "status": 200,
        ///
        ///  "data": "abcxyz" (Verifycode used for password update in the "user/password" API)
        ///
        ///  "pagination": null,
        ///
        ///  "message": "Xác thực thành công."
        ///
        ///}
        /// </response>
        /// <response code="400">If validation error.</response>
        /// <response code="500">Internal server error.</response>
        [HttpPost("phone/verification")]
        public async Task<IActionResult> VerifyOtp(VerifyOtpRequest request)
        {
            CommonResponse commonResponse = new CommonResponse();
            string internalServerErrorMsg = _config[
                "ResponseMessages:UserMsg:InternalServerErrorMsg"
            ];
            try
            {
                commonResponse = await _userService.VerifyUserPhone(request);
                switch (commonResponse.Status)
                {
                    case 200:
                        return Ok(commonResponse);
                    case 400:
                        return BadRequest(commonResponse);
                    default:
                        return StatusCode(500, commonResponse);
                }
            }
            catch (Exception ex)
            {
                commonResponse.Status = 500;
                commonResponse.Message = internalServerErrorMsg;
                _logger.LogError(ex, "An error occurred: {ErrorMessage}", ex.Message);
                return StatusCode(500, commonResponse);
            }
        }

        /// <summary>
        ///Use for Register.
        /// </summary>
        /// <remarks>
        /// Register
        /// Paramters:
        /// - **Password**: Password (Not null and must contain at least 1 digit and 1 character).
        /// - **VerifyCode**: Used for authentication when changing the password (not null).
        /// </remarks>
        /// <response code="200">If success.</response>
        /// <response code="400">If validation error.</response>
        /// <response code="500">Internal server error.</response>
        [HttpPut("password")]
        public async Task<IActionResult> UpdatePasswordWhenRegister(ResetPasswordRequest request)
        {
            CommonResponse commonResponse = new CommonResponse();
            string internalServerErrorMsg = _config[
                "ResponseMessages:UserMsg:InternalServerErrorMsg"
            ];
            try
            {
                commonResponse = await _userService.ResetPasswordAsync(request);
                switch (commonResponse.Status)
                {
                    case 200:
                        return Ok(commonResponse);
                    case 400:
                        return BadRequest(commonResponse);
                    default:
                        return StatusCode(500, commonResponse);
                }
            }
            catch (Exception ex)
            {
                commonResponse.Status = 500;
                commonResponse.Message = internalServerErrorMsg;
                _logger.LogError(ex, "An error occurred: {ErrorMessage}", ex.Message);
                return StatusCode(500, commonResponse);
            }
        }

        /// <summary>
        ///Use for Register.
        /// </summary>
        /// <remarks>
        /// Register
        /// Paramters:
        /// - **phone**:Phone(Not null).
        /// </remarks>
        /// <response code="200">If success.</response>
        /// <response code="400">If validation error.</response>
        /// <response code="500">Internal server error.</response>
        [HttpPut("otp")]
        public async Task<IActionResult> ResendOtpToPhone(ResendOtpRequest request)
        {
            CommonResponse commonResponse = new CommonResponse();
            string internalServerErrorMsg = _config[
                "ResponseMessages:UserMsg:InternalServerErrorMsg"
            ];
            try
            {
                commonResponse = await _userService.SendOtp(request.Phone);
                switch (commonResponse.Status)
                {
                    case 200:
                        return Ok(commonResponse);
                    case 400:
                        return BadRequest(commonResponse);
                    default:
                        return StatusCode(500, commonResponse);
                }
            }
            catch (Exception ex)
            {
                commonResponse.Status = 500;
                commonResponse.Message = internalServerErrorMsg;
                _logger.LogError(ex, "An error occurred: {ErrorMessage}", ex.Message);
                return StatusCode(500, commonResponse);
            }
        }

        /// <summary>
        ///Use for link account.
        /// </summary>
        /// <remarks>
        /// Link email to user account
        ///
        /// Paramters:
        /// - **Email**: email(Not null).
        /// - **VerifyCode**: Verify code user receive from email (Not null).
        /// </remarks>
        /// <response code="200">If success.</response>
        /// <response code="400">If validation error.</response>
        /// <response code="500">Internal server error.</response>
        [Authorize]
        [HttpPut("email/link")]
        public async Task<IActionResult> LinkToEmail(LinkToEmailRequest request)
        {
            CommonResponse commonResponse = new CommonResponse();
            string internalServerErrorMsg = _config[
                "ResponseMessages:UserMsg:InternalServerErrorMsg"
            ];

            try
            {
                var token = HttpContext.Request.Headers["Authorization"]
                    .FirstOrDefault()
                    ?.Split(" ")
                    .Last();
                string? userSub = "";
                if (token != null)
                {
                    var decodedToken = _jwtService.GetClaimsPrincipal(token);

                    if (decodedToken != null)
                    {
                        userSub = decodedToken.Claims.FirstOrDefault(c => c.Type == "Id")?.Value;
                    }
                }
                commonResponse = await _userService.LinkToEmail(
                    request.VerifyCode,
                    Guid.Parse(userSub!),
                    request.Email!
                );
                switch (commonResponse.Status)
                {
                    case 200:
                        return Ok(commonResponse);
                    case 400:
                        return BadRequest(commonResponse);
                    default:
                        return StatusCode(500, commonResponse);
                }
            }
            catch (Exception ex)
            {
                commonResponse.Status = 500;
                commonResponse.Message = internalServerErrorMsg;
                _logger.LogError(ex, "An error occurred: {ErrorMessage}", ex.Message);
                return StatusCode(500, commonResponse);
            }
        }

        /// <summary>
        ///Use for link account.
        /// </summary>
        /// <remarks>
        /// Send verify code to user for link account
        ///
        /// Paramters:
        /// - **Email**: email(Not null).
        /// </remarks>
        /// <response code="200">If success.</response>
        /// <response code="400">If validation error.</response>
        /// <response code="500">Internal server error.</response>
        [Authorize]
        [HttpPost("verifycode")]
        public async Task<IActionResult> SendVerifyCodeToEmail(
            [FromBody] VerifyEmailRequest request
        )
        {
            CommonResponse commonResponse = new CommonResponse();
            string internalServerErrorMsg = _config[
                "ResponseMessages:UserMsg:InternalServerErrorMsg"
            ];
            try
            {
                var token = HttpContext.Request.Headers["Authorization"]
                    .FirstOrDefault()
                    ?.Split(" ")
                    .Last();
                string? userSub = "";
                if (token != null)
                {
                    var decodedToken = _jwtService.GetClaimsPrincipal(token);

                    if (decodedToken != null)
                    {
                        userSub = decodedToken.Claims.FirstOrDefault(c => c.Type == "Id")?.Value;
                    }
                }
                commonResponse = await _userService.SendVerifyCodeToEmail(
                    request,
                    Guid.Parse(userSub!)
                );
                switch (commonResponse.Status)
                {
                    case 200:
                        return Ok(commonResponse);
                    case 400:
                        return BadRequest(commonResponse);
                    default:
                        return StatusCode(500, commonResponse);
                }
            }
            catch
            {
                commonResponse.Message = internalServerErrorMsg;
                commonResponse.Status = 500;
                return StatusCode(500, commonResponse);
            }
        }

        /// <summary>
        ///Use for search branch admin to assign branch member.
        /// </summary>
        /// <remarks>
        /// Use for search branch admin.
        /// Paramters:
        /// - **searchStr**: input string that you want to search(Not null) by email, phone, name.
        /// </remarks>
        /// <response code="200">
        /// ```
        /// {
        ///     "status": 200,
        ///     "data": [
        ///         {
        ///             "id": "326abb80-972e-ee11-adee-105bad532eaf",
        ///             "email": "branchadminquan2@gmail.com",
        ///             "phone": "0323456789",
        ///             "fullName": "ABC",
        ///             "avatar": "Image"
        ///         }
        ///     ]
        ///     "pagination": null,
        ///     "message": null
        /// }
        /// ```
        /// </response>
        /// <response code="400">If validation error.</response>
        /// <response code="500">Internal server error.</response>
        [Authorize(Roles = "SYSTEM_ADMIN")]
        [HttpGet("branch-admin")]
        public async Task<IActionResult> GetListUserForSystemAdminForAssignBranchMember(
            string? searchStr
        )
        {
            CommonResponse commonResponse = new CommonResponse();
            string internalServerErrorMsg = _config[
                "ResponseMessages:UserMsg:InternalServerErrorMsg"
            ];
            try
            {
                commonResponse = await _userService.GetListUserForSystemAdminForAssignBranchMember(
                    searchStr
                );
                switch (commonResponse.Status)
                {
                    case 200:
                        return Ok(commonResponse);
                    default:
                        return StatusCode(500, commonResponse);
                }
            }
            catch
            {
                commonResponse.Message = internalServerErrorMsg;
                commonResponse.Status = 500;
                return StatusCode(500, commonResponse);
            }
        }

        /// <summary>
        /// Update profile (Fields not provided will not be updated)
        /// </summary>
        /// <remarks>
        /// Body:
        /// - **Name**: Name (8-60 characters)
        /// - **address**: (8-200 characters)
        /// - **location**: Location (double array [x, y])
        /// - **avatar**: Image link (not empty).
        /// </remarks>
        /// <br />
        /// <response code="200">
        /// ```
        /// {
        ///     "status": 200,
        ///     "data": null,
        ///     "pagination": null,
        ///     "message": "Cập nhật thành công."
        /// }
        /// ```
        ///</response>
        /// <response code="400">
        /// ```
        /// {
        ///     "status": 400,
        ///     "data": null,
        ///     "pagination": null,
        ///     "message": "User không tồn tại"
        /// }
        /// ```
        ///</response>
        /// <response code="500">
        /// Internal server error, return message:
        /// ```
        /// {
        ///     "status": 500,
        ///     "data": null,
        ///     "pagination": null,
        ///     "message": "Lỗi hệ thống."
        /// }
        ///</response>
        /// ```
        [Authorize]
        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromForm] UserProfileRequest request)
        {
            CommonResponse commonResponse = new CommonResponse();
            string internalServerErrorMsg = _config[
                "ResponseMessages:UserMsg:InternalServerErrorMsg"
            ];
            try
            {
                var token = HttpContext.Request.Headers["Authorization"]
                    .FirstOrDefault()
                    ?.Split(" ")
                    .Last();
                string? userSub = "";
                if (token != null)
                {
                    var decodedToken = _jwtService.GetClaimsPrincipal(token);

                    if (decodedToken != null)
                    {
                        userSub = decodedToken.Claims.FirstOrDefault(c => c.Type == "Id")?.Value;
                    }
                }

                commonResponse = await _userService.UpdateProfile(Guid.Parse(userSub!), request);
                switch (commonResponse.Status)
                {
                    case 200:
                        return Ok(commonResponse);
                    case 400:
                        return BadRequest(commonResponse);
                    default:
                        return StatusCode(500, commonResponse);
                }
            }
            catch
            {
                commonResponse.Message = internalServerErrorMsg;
                commonResponse.Status = 500;
                return StatusCode(500, commonResponse);
            }
        }

        /// <summary>
        /// Get profile,
        /// </summary>
        /// <response code="200">
        /// ```
        ///{
        ///"status": 200,
        ///"data": {
        ///    "id": "70bd5186-895b-ee11-9f0d-f46add733d9c",
        ///     "Name": "Bảo",
        ///      "description": "stringst",
        ///      "address": "Tân định,bến cát, bình dương",
        ///      "location": "[123,123]",
        ///       "avatar": "string",
        ///   },
        ///   "pagination": null,
        ///   "message": null
        ///}
        /// ```
        ///</response>
        /// <response code="500">
        /// Internal server error, return message:
        /// ```
        /// {
        ///     "status": 500,
        ///     "data": null,
        ///     "pagination": null,
        ///     "message": "Lỗi hệ thống."
        /// }
        ///</response>
        /// ```
        [Authorize]
        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            CommonResponse commonResponse = new CommonResponse();
            string internalServerErrorMsg = _config[
                "ResponseMessages:UserMsg:InternalServerErrorMsg"
            ];
            try
            {
                var token = HttpContext.Request.Headers["Authorization"]
                    .FirstOrDefault()
                    ?.Split(" ")
                    .Last();
                string? userSub = "";
                if (token != null)
                {
                    var decodedToken = _jwtService.GetClaimsPrincipal(token);

                    if (decodedToken != null)
                    {
                        userSub = decodedToken.Claims.FirstOrDefault(c => c.Type == "Id")?.Value;
                    }
                }
                commonResponse = await _userService.GetProfile(Guid.Parse(userSub!));
                switch (commonResponse.Status)
                {
                    case 200:
                        return Ok(commonResponse);
                    case 400:
                        return BadRequest(commonResponse);
                    default:
                        return StatusCode(500, commonResponse);
                }
            }
            catch
            {
                commonResponse.Message = internalServerErrorMsg;
                commonResponse.Status = 500;
                return StatusCode(500, commonResponse);
            }
        }

        /// <summary>
        ///Create branch admin account
        /// </summary>
        /// <br/>
        /// <remarks>
        /// Body:
        /// - **email**: email
        /// - **phone**: phone
        /// - **fullName**: FullName (8-60 characters)
        /// - **address**: (8-200 characters)
        /// - **location**: Location (double array [x, y])
        /// - **avatar**: Image link (not empty).
        /// - **description**: (8-200 characters)
        /// - **frontOfIdCard**: Image Link
        /// - **backOfIdCard**: Image Link
        /// - **otherContacts**: "(8-200 character)"
        /// </remarks>
        /// <br />
        /// <response code="200">
        /// ```
        /// {
        ///     "status": 200,
        ///     "data": null,
        ///     "pagination": null,
        ///     "message": "Cập nhật thành công."
        /// }
        /// ```
        ///</response>
        /// <response code="400">
        /// ```
        /// {
        ///     "status": 400,
        ///     "data": null,
        ///     "pagination": null,
        ///     "message": "User không tồn tại"
        /// }
        /// ```
        ///</response>
        /// <response code="500">
        /// Internal server error, return message:
        /// ```
        /// {
        ///     "status": 500,
        ///     "data": null,
        ///     "pagination": null,
        ///     "message": "Lỗi hệ thống."
        /// }
        ///</response>
        /// ```
        [Authorize(Roles = "SYSTEM_ADMIN")]
        [HttpPost("branch-admin")]
        public async Task<IActionResult> CreateBranchAdminAccount(
            [FromForm] BranchAdminCreatingRequest request
        )
        {
            CommonResponse commonResponse = new CommonResponse();
            string internalServerErrorMsg = _config[
                "ResponseMessages:UserMsg:InternalServerErrorMsg"
            ];
            try
            {
                commonResponse = await _userService.CreateAccountForBranchAdmin(request);
                switch (commonResponse.Status)
                {
                    case 200:
                        return Ok(commonResponse);
                    case 400:
                        return BadRequest(commonResponse);
                    default:
                        return StatusCode(500, commonResponse);
                }
            }
            catch
            {
                commonResponse.Message = internalServerErrorMsg;
                commonResponse.Status = 500;
                return StatusCode(500, commonResponse);
            }
        }

        /// <summary>
        /// Use filter users.  system admin
        /// </summary>
        /// <remarks>
        /// - **roleId**: role of user, you can get roleId from api GET:/roles
        /// - **status**: status of user
        /// - **keyWords**: Search by that name or email or phone match the keyword
        /// - **pageSize**: Page size.
        /// - **page**: Page number.
        /// - **sortType**: Sort By Name(0 ASC, 1 DES)
        /// </remarks>
        /// <response code="200">If successful.</response>
        /// <response code="400">If there's a validation error.</response>
        /// <response code="500">If there's an internal server error.</response>
        [Authorize(Roles = "SYSTEM_ADMIN")]
        [HttpGet]
        public async Task<IActionResult> GetUserAsync(
            string? keyWords,
            [FromQuery] List<Guid>? roleIds,
            UserStatus? status,
            SortType sortType,
            int? page,
            int? pageSize
        )
        {
            CommonResponse commonResponse = new CommonResponse();
            string internalServerErrorMsg = _config[
                "ResponseMessages:UserMsg:InternalServerErrorMsg"
            ];
            try
            {
                UserFilterRequest userFilterRequest = new UserFilterRequest
                {
                    KeyWord = keyWords,
                    RoleIds = roleIds,
                    UserStatus = status
                };
                commonResponse = await _userService.GetUserAsync(
                    userFilterRequest,
                    pageSize,
                    page,
                    sortType
                );
                switch (commonResponse.Status)
                {
                    case 200:
                        return Ok(commonResponse);
                    case 400:
                        return BadRequest(commonResponse);
                    default:
                        return StatusCode(500, commonResponse);
                }
            }
            catch
            {
                commonResponse.Message = internalServerErrorMsg;
                commonResponse.Status = 500;
                return StatusCode(500, commonResponse);
            }
        }

        /// <summary>
        /// Use get user by id. system admin
        /// </summary>
        /// <remarks>
        /// - **userId**:Id of user
        /// </remarks>
        /// <response code="200">
        /// ```
        /// {
        ///     "status": 200,
        ///     "data": {
        ///         "id": "226abb80-972e-ee11-adee-105bad532eff",
        ///         "email": "branchadminquan1@gmail.com",
        ///         "phone": "0223456789",
        ///         "fullName": "ABC",
        ///         "address": "Binh Duong",
        ///         "status": "ACTIVE",
        ///         "avatar": "Image",
        ///         "description": "",
        ///         "frontOfIdCard": "Card",
        ///         "backOfIdCard": "Carrd",
        ///         "otherContacts": ""
        ///     },
        ///     "pagination": null,
        ///     "message": null
        /// }
        /// ```
        /// </response>
        /// <response code="400">If there's a validation error.</response>
        /// <response code="500">If there's an internal server error.</response>
        [Authorize(Roles = "SYSTEM_ADMIN")]
        [HttpGet("{userId}")]
        public async Task<IActionResult> GetUserByIdAsync(Guid userId)
        {
            CommonResponse commonResponse = new CommonResponse();
            string internalServerErrorMsg = _config[
                "ResponseMessages:UserMsg:InternalServerErrorMsg"
            ];
            try
            {
                commonResponse = await _userService.GetUserById(userId);

                switch (commonResponse.Status)
                {
                    case 200:
                        return Ok(commonResponse);
                    case 400:
                        return BadRequest(commonResponse);
                    default:
                        return StatusCode(500, commonResponse);
                }
            }
            catch
            {
                commonResponse.Message = internalServerErrorMsg;
                commonResponse.Status = 500;
                return StatusCode(500, commonResponse);
            }
        }

        /// <summary>
        /// Update User (Fields not provided will not be updated) use for system admin
        /// </summary>
        /// <remarks>
        /// Body:
        /// - **status**: User status (must be 0 or 1, corresponding to ACTIVE or BANNED)
        /// </remarks>
        /// <br />
        /// <response code="200">
        /// ```
        /// {
        ///     "status": 200,
        ///     "data": null,
        ///     "pagination": null,
        ///     "message": "Cập nhật thành công."
        /// }
        /// ```
        ///</response>
        /// <response code="400">
        /// ```
        /// {
        ///     "status": 400,
        ///     "data": null,
        ///     "pagination": null,
        ///     "message": "User không tồn tại"
        /// }
        /// ```
        ///</response>
        /// <response code="500">
        /// Internal server error, return message:
        /// ```
        /// {
        ///     "status": 500,
        ///     "data": null,
        ///     "pagination": null,
        ///     "message": "Lỗi hệ thống."
        /// }
        ///</response>
        /// ```
        [Authorize(Roles = "SYSTEM_ADMIN")]
        [HttpPut("{userId}")]
        public async Task<IActionResult> UpdateUserAsync(
            [FromForm] UserUpdaingForAdminRequest request,
            [FromRoute] Guid userId
        )
        {
            CommonResponse commonResponse = new CommonResponse();
            string internalServerErrorMsg = _config[
                "ResponseMessages:UserMsg:InternalServerErrorMsg"
            ];
            try
            {
                commonResponse = await _userService.UpdateUserAsync(request, userId);
                switch (commonResponse.Status)
                {
                    case 200:
                        return Ok(commonResponse);
                    case 400:
                        return BadRequest(commonResponse);
                    default:
                        return StatusCode(500, commonResponse);
                }
            }
            catch
            {
                commonResponse.Message = internalServerErrorMsg;
                commonResponse.Status = 500;
                return StatusCode(500, commonResponse);
            }
        }

        /// <summary>
        /// Update User's password
        /// </summary>
        /// <remarks>
        /// Body:
        /// - **oldPassword**: old password (8-60 characters)
        /// - **newPassword**: not null,  must have 8-40 character and at least one character and number
        /// </remarks>
        /// <br />
        /// <response code="200">
        /// ```
        /// {
        ///     "status": 200,
        ///     "data": null,
        ///     "pagination": null,
        ///     "message": "Cập nhật thành công."
        /// }
        /// ```
        ///</response>
        /// <response code="400">
        /// ```
        /// {
        ///     "status": 400,
        ///     "data": null,
        ///     "pagination": null,
        ///     "message": "User không tồn tại"
        /// }
        /// ```
        ///</response>
        /// <response code="500">
        /// Internal server error, return message:
        /// ```
        /// {
        ///     "status": 500,
        ///     "data": null,
        ///     "pagination": null,
        ///     "message": "Lỗi hệ thống."
        /// }
        ///</response>
        /// ```

        [Authorize]
        [HttpPut("profile/password")]
        public async Task<IActionResult> UpdateUserPasswordAsync(
            [FromBody] UpdatePasswordRequest request
        )
        {
            CommonResponse commonResponse = new CommonResponse();
            string internalServerErrorMsg = _config[
                "ResponseMessages:UserMsg:InternalServerErrorMsg"
            ];
            try
            {
                var token = HttpContext.Request.Headers["Authorization"]
                    .FirstOrDefault()
                    ?.Split(" ")
                    .Last();
                string? userSub = "";
                if (token != null)
                {
                    var decodedToken = _jwtService.GetClaimsPrincipal(token);

                    if (decodedToken != null)
                    {
                        userSub = decodedToken.Claims.FirstOrDefault(c => c.Type == "Id")?.Value;
                    }
                }
                commonResponse = await _userService.UpdatePasswordAsync(
                    request,
                    Guid.Parse(userSub!)
                );
                switch (commonResponse.Status)
                {
                    case 200:
                        return Ok(commonResponse);
                    case 400:
                        return BadRequest(commonResponse);
                    default:
                        return StatusCode(500, commonResponse);
                }
            }
            catch
            {
                commonResponse.Message = internalServerErrorMsg;
                commonResponse.Status = 500;
                return StatusCode(500, commonResponse);
            }
        }

        [Authorize]
        [HttpGet("permissions")]
        public async Task<IActionResult> GetPermissionByUserIdAsync(
            int? page,
            int? pageSize,
            SortType? sortType
        )
        {
            CommonResponse commonResponse = new CommonResponse();
            string internalServerErrorMsg = _config[
                "ResponseMessages:UserMsg:InternalServerErrorMsg"
            ];
            try
            {
                var token = HttpContext.Request.Headers["Authorization"]
                    .FirstOrDefault()
                    ?.Split(" ")
                    .Last();
                string? userSub = "";
                if (token != null)
                {
                    var decodedToken = _jwtService.GetClaimsPrincipal(token);

                    if (decodedToken != null)
                    {
                        userSub = decodedToken.Claims.FirstOrDefault(c => c.Type == "Id")?.Value;
                    }
                }
                commonResponse = await _userPermissionService.GetPermissionsByUserAsync(
                    Guid.Parse(userSub!),
                    page,
                    pageSize,
                    sortType
                );

                switch (commonResponse.Status)
                {
                    case 200:
                        return Ok(commonResponse);
                    case 400:
                        return BadRequest(commonResponse);
                    default:
                        return StatusCode(500, commonResponse);
                }
            }
            catch
            {
                commonResponse.Message = internalServerErrorMsg;
                commonResponse.Status = 500;
                return StatusCode(500, commonResponse);
            }
        }

        [Authorize]
        [HttpPut("device-token")]
        public async Task<IActionResult> UpdateDeviceTokenAsync(
            [FromBody] DeviceTokenRequest deviceToken
        )
        {
            CommonResponse commonResponse = new CommonResponse();
            string internalServerErrorMsg = _config[
                "ResponseMessages:UserMsg:InternalServerErrorMsg"
            ];
            try
            {
                string userId = User.FindFirst("id")!.Value;
                commonResponse = await _userService.UpdateDeviceToken(
                    Guid.Parse(userId),
                    deviceToken.deviceToken
                );
                switch (commonResponse.Status)
                {
                    case 200:
                        return Ok(commonResponse);
                    case 400:
                        return BadRequest(commonResponse);
                    default:
                        return StatusCode(500, commonResponse);
                }
            }
            catch
            {
                commonResponse.Message = internalServerErrorMsg;
                commonResponse.Status = 500;
                return StatusCode(500, commonResponse);
            }
        }

        /// <summary>
        /// Use for Register. abc
        /// </summary>
        /// <remarks>
        /// Register
        /// Parameters:
        /// - **Phone**: Phone number (Not null and must be in phone number format).
        /// - **FullName**: Name (not null, from 8 to 60 characters).
        /// </remarks>
        /// <response code="200">If success.</response>
        /// <response code="400">If validation error.</response>
        /// <response code="500">Internal server error.</response>
        [Authorize(Roles = "BRANCH_ADMIN")]
        [HttpPost("branch-admin/registered-user")]
        public async Task<IActionResult> RegisterForUserByPhoneForBranchAdmin(
            [FromBody] UserRegisterByPhoneRequest request
        )
        {
            CommonResponse commonResponse = new CommonResponse();
            string internalServerErrorMsg = _config[
                "ResponseMessages:UserMsg:InternalServerErrorMsg"
            ];
            try
            {
                commonResponse = await _userService.RegisterUserByPhoneForBranchAdmin(request);
                switch (commonResponse.Status)
                {
                    case 200:
                        return Ok(commonResponse);
                    case 400:
                        return BadRequest(commonResponse);
                    default:
                        return StatusCode(500, commonResponse);
                }
            }
            catch (Exception ex)
            {
                commonResponse.Status = 500;
                commonResponse.Message = internalServerErrorMsg;
                _logger.LogError(ex, "An error occurred: {ErrorMessage}", ex.Message);
                return StatusCode(500, commonResponse);
            }
        }

        /// <summary>
        /// Use filter users by phone.  branch admin
        /// </summary>
        /// <remarks>
        /// - **roleId**: role of user, you can get roleId from api GET:/roles
        /// - **status**: status of user
        /// - **keyWords**: Search by that name or email or phone match the keyword
        /// - **pageSize**: Page size.
        /// - **page**: Page number.
        /// - **sortType**: Sort By Name(0 ASC, 1 DES)
        /// </remarks>
        /// <response code="200">If successful.</response>
        /// <response code="400">If there's a validation error.</response>
        /// <response code="500">If there's an internal server error.</response>
        [Authorize(Roles = "BRANCH_ADMIN")]
        [HttpGet("simple-users")]
        public async Task<IActionResult> GetUserAsync(string? phone, int? page, int? pageSize)
        {
            CommonResponse commonResponse = new CommonResponse();
            string internalServerErrorMsg = _config[
                "ResponseMessages:UserMsg:InternalServerErrorMsg"
            ];
            try
            {
                UserFilterRequest userFilterRequest = new UserFilterRequest
                {
                    KeyWord = phone,
                    UserStatus = UserStatus.ACTIVE,
                    RoleIds = null,
                };
                commonResponse = await _userService.GetUserAsyncByPhone(
                    userFilterRequest,
                    pageSize,
                    page,
                    SortType.ASC
                );
                switch (commonResponse.Status)
                {
                    case 200:
                        return Ok(commonResponse);
                    case 400:
                        return BadRequest(commonResponse);
                    default:
                        return StatusCode(500, commonResponse);
                }
            }
            catch
            {
                commonResponse.Message = internalServerErrorMsg;
                commonResponse.Status = 500;
                return StatusCode(500, commonResponse);
            }
        }
    }
}
