using BusinessLogic.Services;
using BusinessLogic.Utils.SecurityServices;
using DataAccess.EntityEnums;
using DataAccess.Models.Requests;
using DataAccess.Models.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodDonationDeliveryManagementAPI.Controllers
{
    [Route("notifications")]
    [ApiController]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationService _notificationService;
        private readonly ILogger<ActivitiesController> _logger;
        private readonly IConfiguration _config;
        private readonly IJwtService _jwtService;

        public NotificationsController(
            INotificationService notificationService,
            ILogger<ActivitiesController> logger,
            IConfiguration config,
            IJwtService jwtService
        )
        {
            _notificationService = notificationService;
            _logger = logger;
            _config = config;
            _jwtService = jwtService;
        }

        /// <summary>
        /// Update notification status.
        /// </summary>
        /// <remarks>
        /// Paramters:
        /// - **notificationIds**: List of notification id (not null)
        /// - **Status**: Status that you want to update (not null)
        /// </remarks>
        /// <response code="200">Returns success message</response>
        /// <response code="500">Internal server error.</response>
        [Authorize]
        [HttpPut]
        public async Task<IActionResult> UpdateNotification(
            NotificationUpdatingRequest notificationRequest
        )
        {
            CommonResponse commonResponse = new CommonResponse();
            string internalServerErrorMsg = _config[
                "ResponseMessages:UserPermissionMsg:InternalServerErrorMsg"
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
                commonResponse = await _notificationService.UpdateNotification(
                    notificationRequest,
                    Guid.Parse(userSub!)
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
        /// Get notification of user.
        /// </summary>
        /// <remarks>
        /// Paramters:
        /// - **notificationStatus**: Status 0(NEW), 1(SEEN) (not null)
        /// - **page**: page (not null)
        /// - **pageSize**: pageSize (not null)
        /// </remarks>
        /// <response code="200">Returns success message</response>
        /// <response code="500">Internal server error.</response>
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetNotification(
            NotificationStatus? notificationStatus,
            int? page,
            int? pageSize
        )
        {
            CommonResponse commonResponse = new CommonResponse();
            string internalServerErrorMsg = _config[
                "ResponseMessages:UserPermissionMsg:InternalServerErrorMsg"
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
                commonResponse = await _notificationService.GetNotification(
                    Guid.Parse(userSub!),
                    notificationStatus,
                    page,
                    pageSize
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
    }
}
