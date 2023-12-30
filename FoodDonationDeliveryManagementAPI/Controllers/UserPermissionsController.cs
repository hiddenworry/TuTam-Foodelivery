using BusinessLogic.Services;
using BusinessLogic.Utils.SecurityServices;
using DataAccess.Models.Requests;
using DataAccess.Models.Responses;
using DataAccess.ModelsEnum;
using FoodDonationDeliveryManagementAPI.Security.Authourization.PolicyProvider;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodDonationDeliveryManagementAPI.Controllers
{
    [Route("user-permissions")]
    [ApiController]
    public class UserPermissionsController : ControllerBase
    {
        private readonly IUserPermissionService _userPermissionService;
        private readonly IConfiguration _config;
        private readonly IJwtService _jwtService;
        private readonly ITokenBlacklistService _tokenBlacklistService;
        private readonly IUserService _userService;

        public UserPermissionsController(
            IUserPermissionService userPermissionService,
            IConfiguration config,
            IJwtService jwtService,
            ITokenBlacklistService tokenBlacklistService,
            IUserService userService
        )
        {
            _userPermissionService = userPermissionService;
            _config = config;
            _jwtService = jwtService;
            _tokenBlacklistService = tokenBlacklistService;
            _userService = userService;
        }

        /// <summary>
        /// Update Permission.
        /// </summary>
        /// <remarks>
        /// This api help system admin to update all permission of specific user except themselve.
        /// Paramters:
        /// - **userId**: Id of user (not null).
        /// - **permissionId**: Id of permission (not null).
        /// - **status**: Status of UserPermission with 0(PERMITTED), 1(BANNED)
        /// Role : System Admin
        /// </remarks>
        /// <response code="200">Returns list of user's permission.</response>
        /// <response code="400">If the user try to update their permission or the target user is not found.</response>
        /// <response code="403">If the user do not allow to access this api.</response>
        /// <response code="500">Internal server error.</response>
        [Authorize]
        [PermissionAuthorize("UPDATE-PERMISSION")]
        [HttpPut]
        public async Task<IActionResult> UpdateUserPermissionAsync(
            [FromBody] UserPermissionRequest request
        )
        {
            CommonResponse commonResponse = new CommonResponse();
            string internalServerErrorMsg = _config[
                "ResponseMessages:UserPermissionMsg:InternalServerErrorMsg"
            ];
            try
            {
                commonResponse = await _userPermissionService.UpdateUserPermissionAsync(request);
                switch (commonResponse.Status)
                {
                    case 200:
                        return Ok(commonResponse);
                    case 400:
                        return BadRequest(commonResponse);
                    default:
                        return StatusCode(500, internalServerErrorMsg);
                }
            }
            catch
            {
                commonResponse.Status = 500;
                commonResponse.Message = internalServerErrorMsg;
                return StatusCode(500, commonResponse);
            }
        }

        /// <summary>
        /// Read Permission by user ID.
        /// </summary>
        /// <remarks>
        /// This api help system admin to read all permission of specific user.
        /// Paramters:
        /// - **userId**: Id of user (not null).
        /// Role : System Admin
        /// </remarks>
        /// <response code="200">Returns list of user's permission.</response>
        /// <response code="400">If the user do not have any permisson.</response>
        /// <response code="403">If the user do not allow to access this api.</response>
        /// <response code="500">Internal server error.</response>
        [Authorize]
        [PermissionAuthorize("READ-PERMISSION")]
        [HttpGet]
        public async Task<IActionResult> GetPermissionByUserId(
            Guid userId,
            int? page,
            int? pageSize,
            SortType sortType
        )
        {
            CommonResponse commonResponse = new CommonResponse();
            string internalServerErrorMsg = _config[
                "ResponseMessages:UserPermissionMsg:InternalServerErrorMsg"
            ];
            try
            {
                commonResponse = await _userPermissionService.GetPermissionsByUserAsync(
                    userId,
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
                        return StatusCode(500, internalServerErrorMsg);
                }
            }
            catch
            {
                commonResponse.Status = 500;
                commonResponse.Message = "";
                return StatusCode(500, commonResponse);
            }
        }
    }
}
