using BusinessLogic.Services;
using BusinessLogic.Utils.SecurityServices;
using DataAccess.Models.Requests;
using DataAccess.Models.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Transactions;

namespace FoodDonationDeliveryManagementAPI.Controllers
{
    [ApiController]
    public class AuthenticationController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ITokenBlacklistService _tokenBlacklistService;
        private readonly IJwtService _jwtService;
        private readonly IConfiguration _config;
        private readonly ILogger<AuthenticationController> _logger;

        public AuthenticationController(
            IUserService userService,
            ITokenBlacklistService tokenBlacklistService,
            IJwtService jwtService,
            IConfiguration config,
            ILogger<AuthenticationController> logger
        )
        {
            _userService = userService;
            _tokenBlacklistService = tokenBlacklistService;
            _jwtService = jwtService;
            _config = config;
            _logger = logger;
        }

        /// <summary>
        /// User Authentication.
        /// </summary>
        /// <remarks>
        /// To authenticate the user, provide the following parameters in the request body:
        /// Paramters:
        /// - **UserName**: The username of the user (not null).
        /// - **Password**: The password of the user (not null).
        /// - **LoginRole**: The role you want to login(CONTRIBUTOR = 0,CHARITY = 1,ADMIN = 2) (not null).
        /// </remarks>
        /// <response code="200">
        /// Returns:
        ///
        /// "CommonResponse":
        /// {
        ///
        /// "status": 200,
        ///
        ///  "data":
        ///  {
        ///
        ///    "accessToken": "......",
        ///
        ///    "role": "User",
        ///
        ///    "refreshToken": "jPWJHNZfdcPkcny7X6xe5VEG/ypWLK2FjG74xpkGXgo="
        ///
        ///  },
        ///
        ///  "pagination": null,
        ///
        ///  "message": "Đăng nhập thành công."
        ///
        ///}
        ///
        /// </response>
        /// <response code="400">If the user is unverify.</response>
        /// <response code="400">Validatetion errors.</response>
        /// <response code="401">Login failed.</response>
        /// <response code="403">If the user inactive.</response>
        /// <response code="500">Internal server error.</response>

        [HttpPost("/authenticate")]
        public async Task<IActionResult> Authenticate([FromBody] LoginRequest loginRequest)
        {
            CommonResponse commonResponse = new CommonResponse();

            string internalServerErrorMsg = _config[
                "ResponseMessages:AuthenticationMsg:InternalServerErrorMsg"
            ];
            try
            {
                commonResponse = await _userService.AuthenticateAsync(loginRequest);
                switch (commonResponse.Status)
                {
                    case 200:
                        return Ok(commonResponse);
                    case 400:
                        return BadRequest(commonResponse);
                    case 401:
                        return Unauthorized(commonResponse);
                    case 403:
                        return StatusCode(403, commonResponse);
                    default:
                        return StatusCode(500, commonResponse);
                }
            }
            catch
            {
                commonResponse.Status = 500;
                commonResponse.Message = internalServerErrorMsg;
            }
            return StatusCode(500, commonResponse);
        }

        /// <summary>
        /// User Log out.
        /// </summary>
        /// <remarks>
        /// To logout jwt token and put it into black-list cache
        /// Paramters: No
        /// </remarks>
        /// <response code="200">
        /// Returns:
        ///
        /// "CommonResponse":
        /// {
        ///
        /// "status": 200,
        ///
        ///  "data":
        ///  {
        ///
        ///    "accessToken": "......",
        ///
        ///    "role": "User",
        ///
        ///    "refreshToken": "jPWJHNZfdcPkcny7X6xe5VEG/ypWLK2FjG74xpkGXgo="
        ///
        ///  },
        ///
        ///  "pagination": null,
        ///
        ///  "message": "Đăng nhập thành công."
        ///
        ///}
        ///
        /// </response>
        /// <response code="400">User not found.</response>
        /// <response code="500">Internal server error.</response>
        [HttpGet("/logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            string internalServerErrorMsg = _config[
                "ResponseMessages:AuthenticationMsg:InternalServerErrorMsg"
            ];
            string unauthenticationMsg = _config[
                "ResponseMessages:AuthenticationMsg:UnauthenticationMsg"
            ];
            string alreadyLogoutMsg = _config[
                "ResponseMessages:AuthenticationMsg:AlreadyLogoutMsg"
            ];
            string userNotFoundMsg = _config["ResponseMessages:AuthenticationMsg:UserNotFoundMsg"];
            string LogoutSuccessMsg = _config[
                "ResponseMessages:AuthenticationMsg:LogoutSuccessMsg"
            ];

            CommonResponse commonResponse = new CommonResponse();
            var userId = "";
            try
            {
                var token = HttpContext.Request.Headers["Authorization"]
                    .FirstOrDefault()
                    ?.Split(" ")
                    .Last();
                if (token != null)
                {
                    var decodedToken = _jwtService.GetClaimsPrincipal(token);
                    if (decodedToken != null)
                    {
                        var userIdClaim = decodedToken.Claims.FirstOrDefault(c => c.Type == "Id");
                        if (userIdClaim != null)
                        {
                            userId = userIdClaim.Value;
                        }
                        else
                            throw new Exception();
                    }
                    // Kiểm tra xem token đã logout chưa
                    if (_tokenBlacklistService.IsTokenBlacklisted(token))
                    {
                        commonResponse.Status = 400;
                        commonResponse.Message = alreadyLogoutMsg;
                        return BadRequest(commonResponse);
                    }
                    using (
                        var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled)
                    )
                    {
                        var jwt = _jwtService.GetClaimsPrincipal(token);
                        double? exp = null;
                        if (jwt != null)
                        {
                            var expClaim = jwt.Claims.FirstOrDefault(c => c.Type == "exp");
                            if (expClaim != null)
                            {
                                exp = double.TryParse(expClaim.Value, out var parsedExp)
                                    ? parsedExp
                                    : (double?)null;
                            }
                        }
                        double expiryTimeUnix = exp ?? 0;
                        var expiryTimeUtc = DateTimeOffset.FromUnixTimeSeconds(
                            (long)expiryTimeUnix
                        );

                        // Thêm token vào blacklist với thời gian sống thời gian còn lại của token
                        _tokenBlacklistService.AddTokenToBlacklist(
                            token,
                            expiryTimeUtc.UtcDateTime
                        );
                        var checkDeleteRefreshToken = await _userService.DeleteRefreshTokenAsync(
                            Guid.Parse(userId)
                        );
                        var checkDeleteAccessToken = await _userService.DeleteAccessTokenAsync(
                            Guid.Parse(userId)
                        );

                        if (checkDeleteRefreshToken == null || checkDeleteAccessToken == null)
                        {
                            commonResponse.Message = userNotFoundMsg;
                            commonResponse.Status = 400;
                            return StatusCode(400, commonResponse);
                        }
                        else
                        {
                            commonResponse.Message = LogoutSuccessMsg;
                            commonResponse.Status = 200;
                            scope.Complete();
                        }
                    }
                }
            }
            catch
            {
                commonResponse.Message = internalServerErrorMsg;
                commonResponse.Status = 500;
                return StatusCode(500, commonResponse);
            }
            return Ok(commonResponse);
        }

        /// <summary>
        /// Refresh accesstoken.
        /// </summary>
        /// <remarks>
        /// To refresh access token of user, provide the following parameters in the request body:
        /// Paramters:
        /// - **refreshToken**: the refresh that frontend had collected (not null).
        /// </remarks>
        /// <response code="200">
        /// Returns:
        ///
        /// "CommonResponse":
        /// {
        ///
        /// "status": 200,
        ///
        ///  "data":
        ///  {
        ///
        ///    "accessToken": "......",
        ///
        ///    "role": "User",
        ///
        ///    "refreshToken": "jPWJHNZfdcPkcny7X6xe5VEG/ypWLK2FjG74xpkGXgo="
        ///
        ///  },
        ///
        ///  "pagination": null,
        ///
        ///  "message": "Đăng nhập thành công."
        ///
        ///}
        ///
        /// </response>
        /// <response code="400">If the user is unverify.</response>
        /// <response code="403">If the user inactive.</response>
        /// <response code="500">Internal server error.</response>

        [HttpPost("/refresh-access-token")]
        public async Task<IActionResult> RefreshAccessToken(
            [FromBody] RefreshAccessTokenRequest request
        )
        {
            CommonResponse commonResponse = new CommonResponse();
            string internalServerErrorMsg = _config[
                "ResponseMessages:AuthenticationMsg:InternalServerErrorMsg"
            ];
            try
            {
                commonResponse = await _userService.RefreshAccessTokenAsync(request);
                switch (commonResponse.Status)
                {
                    case 200:
                        return Ok(commonResponse);
                    case 400:
                        return BadRequest(commonResponse);
                    case 401:
                        return Unauthorized(commonResponse);
                    case 403:
                        return StatusCode(403, commonResponse);
                    default:
                        return StatusCode(500, commonResponse);
                }
            }
            catch
            {
                commonResponse.Status = 500;
                commonResponse.Message = internalServerErrorMsg;
            }
            return StatusCode(500, commonResponse);
        }

        /// <summary>
        /// Login google.
        /// </summary>
        /// <remarks>
        /// Login google
        /// Paramters:
        /// - **GoogleToken**: Use for retrive user information (not null).
        /// </remarks>
        /// <response code="200">
        /// Returns:
        ///
        /// "CommonResponse":
        /// {
        ///
        /// "status": 200,
        ///
        ///  "data":
        ///  {
        ///
        ///    "accessToken": "......",
        ///
        ///    "role": "User",
        ///
        ///    "refreshToken": "jPWJHNZfdcPkcny7X6xe5VEG/ypWLK2FjG74xpkGXgo="
        ///
        ///  },
        ///
        ///  "pagination": null,
        ///
        ///  "message": "Đăng nhập thành công."
        ///
        ///}
        ///
        /// </response>
        /// <response code="400">If the user is unverify.</response>
        /// <response code="403">If the user inactive.</response>
        /// <response code="500">Internal server error.</response>
        [HttpPost]
        [Route("google-sign-in")]
        public async Task<IActionResult> LoginGoogle([FromBody] GoogleLoginRequest request)
        {
            CommonResponse commonResponse = new CommonResponse();
            string internalServerErrorMsg = _config[
                "ResponseMessages:AuthenticationMsg:InternalServerErrorMsg"
            ];
            try
            {
                var clientId = _config["Google:ClientId"];
                var clientSecret = _config["Google:ClientSecret"];
                var authenLink = _config["Google:authenLink"];
                var userInfoLink = _config["Google:userInfo"];
                using (var httpClient = new HttpClient())
                {
                    // Set the authorization header with the access token
                    var client = new HttpClient();
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                        "Bearer",
                        request.GoogleToken
                    );
                    var response = await httpClient.GetAsync(userInfoLink);
                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        var userInfo = JsonConvert.DeserializeObject<GoogleUserInfoResponse>(
                            content
                        );
                        if (userInfo != null && userInfo.Email != null && userInfo.Name != null)
                        {
                            commonResponse = await _userService.AuthenticateByGoogleAsync(userInfo);
                            switch (commonResponse.Status)
                            {
                                case 200:
                                    return Ok(commonResponse);
                                case 400:
                                    return BadRequest(commonResponse);
                                case 401:
                                    return Unauthorized(commonResponse);
                                case 403:
                                    return StatusCode(403, commonResponse);
                                default:
                                    return StatusCode(500, commonResponse);
                            }
                        }
                        else
                            throw new Exception("Error when parser information");
                    }
                    else
                    {
                        _logger.LogError(
                            "An error occurred: {ErrorMessage}",
                            "Given token google to retrive information failed"
                        );
                        commonResponse.Status = 400;
                        commonResponse.Message = "Given token google to retrive information failed";
                        return StatusCode(400, commonResponse);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred: {ErrorMessage}", ex.Message);
                commonResponse.Status = 500;
                commonResponse.Message = internalServerErrorMsg;
            }
            return StatusCode(500, commonResponse);
        }
    }
}
