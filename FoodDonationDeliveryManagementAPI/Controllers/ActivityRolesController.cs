using BusinessLogic.Services;
using BusinessLogic.Utils.SecurityServices;
using DataAccess.Models.Requests;
using DataAccess.Models.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodDonationDeliveryManagementAPI.Controllers
{
    [Route("activity-roles")]
    [ApiController]
    public class ActivityRolesController : ControllerBase
    {
        private readonly IActivityRoleService _activityRoleService;
        private readonly ILogger<ActivitiesController> _logger;
        private readonly IConfiguration _config;
        private readonly IJwtService _jwtService;

        public ActivityRolesController(
            IActivityRoleService activityRoleService,
            ILogger<ActivitiesController> logger,
            IConfiguration config,
            IJwtService jwtService
        )
        {
            _activityRoleService = activityRoleService;
            _logger = logger;
            _config = config;
            _jwtService = jwtService;
        }

        /// <summary>
        /// Use for create role  in activity Role: Branch_Admin
        /// Tài khoản test branchadminquan1@gmail.com,datnhtse151251@fpt.edu.vn cho user ,mật khẩu : 1234567890a
        /// </summary>
        /// <remarks>
        /// - **Name**: String(8-200 character, Not null).
        /// - **Description**: String(8-200 character, Not null).
        /// - **ActivityId**: Guid ( Not null).
        /// - **IsDefault**: boolean (Not null).
        /// </remarks>
        /// <response code="200">If success.</response>
        /// <response code="400">If validation error.</response>
        /// <response code="500">Internal server error.</response>
        [Authorize(Roles = "BRANCH_ADMIN,SYSTEM_ADMIN")]
        [HttpPost]
        public async Task<IActionResult> CreateActivityRole(
            [FromBody] ActivityRoleCreatingRequest request
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
                commonResponse = await _activityRoleService.CreateActivityRoleByActivtyId(
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
        /// Use for create role  in activity Role: Branch_Admin
        /// Tài khoản test branchadminquan1@gmail.com ,mật khẩu : 1234567890a
        /// </summary>
        /// <remarks>
        /// - **Name**: String(8-200 character, Not null).
        /// - **Description**: String(8-200 character, Not null).
        /// </remarks>
        /// <response code="200">If success.</response>
        /// <response code="400">If validation error.</response>
        /// <response code="500">Internal server error.</response>
        [Authorize(Roles = "BRANCH_ADMIN,SYSTEM_ADMIN")]
        [HttpPut()]
        public async Task<IActionResult> UpdateActivityRole(
            [FromBody] List<ActivityRoleUpdatingRequest> request,
            Guid activityId
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
                commonResponse = await _activityRoleService.UpdateActivityRoleById(
                    request,
                    Guid.Parse(userSub!),
                    activityId
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
        [HttpGet("{activityId}")]
        public async Task<IActionResult> GetListActivityRole(Guid activityId)
        {
            CommonResponse commonResponse = new CommonResponse();
            string internalServerErrorMsg = _config[
                "ResponseMessages:UserMsg:InternalServerErrorMsg"
            ];
            try
            {
                commonResponse = await _activityRoleService.GetListOfActivityRole(activityId);
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
