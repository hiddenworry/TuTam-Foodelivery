using BusinessLogic.Services;
using BusinessLogic.Utils.SecurityServices;
using DataAccess.EntityEnums;
using DataAccess.Models.Requests;
using DataAccess.Models.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodDonationDeliveryManagementAPI.Controllers
{
    [Route("activity-feedbacks")]
    [ApiController]
    public class ActivityFeedbacksController : ControllerBase
    {
        private readonly IActivityFeedbackService _activityFeedbackService;
        private readonly ILogger<ActivitiesController> _logger;
        private readonly IConfiguration _config;
        private readonly IJwtService _jwtService;

        public ActivityFeedbacksController(
            IActivityFeedbackService activityFeedbackService,
            ILogger<ActivitiesController> logger,
            IConfiguration config,
            IJwtService jwtService
        )
        {
            _activityFeedbackService = activityFeedbackService;
            _logger = logger;
            _config = config;
            _jwtService = jwtService;
        }

        /// <summary>
        /// Use for system amdin want to collect feedback about activity: System_Admin
        /// </summary>
        /// <remarks>
        /// - **activityId**: Guid ( Not null).
        /// </remarks>
        /// <response code="200">If success.</response>
        /// <response code="400">If validation error.</response>
        /// <response code="500">Internal server error.</response>
        [Authorize(Roles = "SYSTEM_ADMIN,BRANCH_ADMIN")]
        [HttpPost("activity")]
        public async Task<IActionResult> CreateFeedbackForAdmin(Guid activityId)
        {
            CommonResponse commonResponse = new CommonResponse();
            string internalServerErrorMsg = _config[
                "ResponseMessages:UserMsg:InternalServerErrorMsg"
            ];
            try
            {
                string jwtToken = Request.Headers["Authorization"]
                    .FirstOrDefault()
                    ?.Split(" ")
                    .Last()!;
                Guid userId = _jwtService.GetUserIdByJwtToken(jwtToken);
                string userRoleName = _jwtService.GetRoleNameByJwtToken(jwtToken);
                commonResponse = await _activityFeedbackService.CreatFeedback(
                    activityId,
                    userId,
                    userRoleName
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
        /// User who joined in activity, want to send feedback about activity
        /// </summary>
        /// <remarks>
        /// - **activityId**: Guid ( Not null).
        /// </remarks>
        /// <response code="200">If success.</response>
        /// <response code="400">If validation error.</response>
        /// <response code="500">Internal server error.</response>
        [Authorize]
        [HttpPut("")]
        public async Task<IActionResult> SendFeedbackForUser(
            ActivityFeedbackCreatingRequest request
        )
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
            CommonResponse commonResponse = new CommonResponse();
            string internalServerErrorMsg = _config[
                "ResponseMessages:UserMsg:InternalServerErrorMsg"
            ];
            try
            {
                commonResponse = await _activityFeedbackService.SendFeedback(
                    Guid.Parse(userSub!),
                    request
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
        /// System admin collect feedback about activity
        /// </summary>
        /// <remarks>
        /// - **activityId**: Guid ( Not null).
        /// </remarks>
        /// <response code="200">If success.</response>
        /// <response code="400">If validation error.</response>
        /// <response code="500">Internal server error.</response>
        [Authorize]
        [HttpGet("")]
        public async Task<IActionResult> GetFeedBack(
            int? page,
            int? pageSize,
            Guid activityId,
            ActivityFeedbackStatus? status
        )
        {
            string jwtToken = Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last()!;
            Guid userId = _jwtService.GetUserIdByJwtToken(jwtToken);
            string userRoleName = _jwtService.GetRoleNameByJwtToken(jwtToken);
            CommonResponse commonResponse = new CommonResponse();
            string internalServerErrorMsg = _config[
                "ResponseMessages:UserMsg:InternalServerErrorMsg"
            ];
            try
            {
                commonResponse = await _activityFeedbackService.GetFeedback(
                    page,
                    pageSize,
                    activityId,
                    status,
                    userId,
                    userRoleName
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
        /// User check if they have feeback about this activity
        /// </summary>
        /// <remarks>
        /// - **activityId**: Guid ( Not null).
        /// </remarks>
        /// <response code="200">If success.</response>
        /// <response code="400">If validation error.</response>
        /// <response code="500">Internal server error.</response>
        [Authorize]
        [HttpGet("activity")]
        public async Task<IActionResult> GetFeedBackByActivtyId(Guid activityId)
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
            CommonResponse commonResponse = new CommonResponse();
            string internalServerErrorMsg = _config[
                "ResponseMessages:UserMsg:InternalServerErrorMsg"
            ];
            try
            {
                commonResponse = await _activityFeedbackService.CheckUserIsFeedbacked(
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

        /// <summary>
        /// Checkj activity can get feedback for admin
        /// </summary>
        /// <remarks>
        /// - **activityId**: Guid ( Not null).
        /// </remarks>
        /// <response code="200">If success.</response>
        /// <response code="400">If validation error.</response>
        /// <response code="500">Internal server error.</response>

        [HttpGet("activity/is-feedback")]
        public async Task<IActionResult> CheckActivityCanGetFeddback(Guid activityId)
        {
            CommonResponse commonResponse = new CommonResponse();
            string internalServerErrorMsg = _config[
                "ResponseMessages:UserMsg:InternalServerErrorMsg"
            ];
            try
            {
                commonResponse = await _activityFeedbackService.CheckActivityIsFeedbacked(
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
    }
}
