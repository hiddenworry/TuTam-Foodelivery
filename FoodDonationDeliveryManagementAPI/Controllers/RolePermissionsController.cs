using BusinessLogic.Services;
using DataAccess.Models.Requests;
using DataAccess.Models.Responses;
using DataAccess.ModelsEnum;
using FoodDonationDeliveryManagementAPI.Security.Authourization.PolicyProvider;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodDonationDeliveryManagementAPI.Controllers
{
    [Route("role-permissions")]
    [ApiController]
    public class RolePermissionsController : ControllerBase
    {
        private readonly IRolePermissionService _rolePermissionService;
        private readonly ILogger<ActivitiesController> _logger;
        private readonly IConfiguration _config;

        public RolePermissionsController(
            IRolePermissionService rolePermissionService,
            ILogger<ActivitiesController> logger,
            IConfiguration config
        )
        {
            _rolePermissionService = rolePermissionService;
            _logger = logger;
            _config = config;
        }

        /// <summary>
        /// Read Permission by role ID.
        /// </summary>
        /// <remarks>
        /// This api help system admin to read all permission of specific role.
        /// Paramters:
        /// - **roleId**: Id of role (not null).
        /// Role : System Admin
        /// </remarks>
        /// <response code="200">Returns list of role's permission.</response>
        /// <response code="400">If the role do not have any permisson.</response>
        /// <response code="403">If the user do not allow to access this api.</response>
        /// <response code="500">Internal server error.</response>
        [Authorize]
        [PermissionAuthorize("READ-PERMISSION")]
        [HttpGet]
        public async Task<IActionResult> GetPermissionByRoleId(
            Guid roleId,
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
                commonResponse = await _rolePermissionService.GetPermissionsByRoleAsync(
                    roleId,
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
                commonResponse.Message = internalServerErrorMsg;
                return StatusCode(500, commonResponse);
            }
        }

        /// <summary>
        /// Update Permission status by role ID.
        /// </summary>
        /// <remarks>
        /// This api help system admin to update permission status all permission of specific role.
        /// Paramters:
        /// - **roleId**: Id of role (not null).
        /// Role : System Admin
        /// </remarks>
        /// <response code="200">Returns list of role's permission.</response>
        /// <response code="400">If the role do not have any permisson.</response>
        /// <response code="403">If the user do not allow to access this api.</response>
        /// <response code="500">Internal server error.</response>
        [Authorize]
        [PermissionAuthorize("UPDATE-PERMISSION")]
        [HttpPut]
        public async Task<IActionResult> UpdatePermissionByRoleId(
            RolePermissionUpdatingRequest request
        )
        {
            CommonResponse commonResponse = new CommonResponse();
            string internalServerErrorMsg = _config[
                "ResponseMessages:UserPermissionMsg:InternalServerErrorMsg"
            ];
            try
            {
                commonResponse = await _rolePermissionService.UpdatePermissionsByRoleAsync(request);
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
    }
}
