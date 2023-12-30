using BusinessLogic.Services;
using BusinessLogic.Utils.SecurityServices;
using DataAccess.Models.Requests;
using DataAccess.Models.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodDonationDeliveryManagementAPI.Controllers
{
    [Route("activity-tasks")]
    [ApiController]
    public class ActivityTasksController : Controller
    {
        private readonly IActivityTaskService _activityTaskService;
        private readonly ILogger<ActivitiesController> _logger;
        private readonly IConfiguration _config;
        private readonly IJwtService _jwtService;

        public ActivityTasksController(
            IActivityTaskService activityTaskService,
            ILogger<ActivitiesController> logger,
            IConfiguration config,
            IJwtService jwtService
        )
        {
            _activityTaskService = activityTaskService;
            _logger = logger;
            _config = config;
            _jwtService = jwtService;
        }

        /// <summary>
        /// Use for create role  in activity Role: Branch_Admin
        /// Tài khoản test branchadminquan1@gmail.com,datnhtse151251@fpt.edu.vn cho user ,mật khẩu : 1234567890a
        /// </summary>
        [Authorize(Roles = "BRANCH_ADMIN,SYSTEM_ADMIN")]
        [HttpPost]
        public async Task<IActionResult> CreateTask([FromBody] TaskCreaingRequest request)
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

                commonResponse = await _activityTaskService.CreateTask(
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
        /// Tài khoản test branchadminquan1@gmail.com,datnhtse151251@fpt.edu.vn cho user ,mật khẩu : 1234567890a
        /// </summary>
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetTask(
            int? page,
            int? pageSize,
            Guid? activityId,
            Guid? phaseId,
            string? name,
            DateTime? startDate,
            DateTime? endDate
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
                commonResponse = await _activityTaskService.GetTask(
                    page,
                    pageSize,
                    Guid.Parse(userSub!),
                    activityId,
                    phaseId,
                    name,
                    startDate,
                    endDate
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
        /// Tài khoản test branchadminquan1@gmail.com,datnhtse151251@fpt.edu.vn cho user ,mật khẩu : 1234567890a
        /// </summary>
        [Authorize(Roles = "BRANCH_ADMIN,SYSTEM_ADMIN")]
        [HttpPut("")]
        public async Task<IActionResult> UpdateTask([FromBody] List<TaskUpdatingRequest> request)
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

                commonResponse = await _activityTaskService.UpdateTask(
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
        /// Use for create role  in activity Role: Branch_Admin
        /// Tài khoản test branchadminquan1@gmail.com,datnhtse151251@fpt.edu.vn cho user ,mật khẩu : 1234567890a
        /// </summary>
        [Authorize(Roles = "BRANCH_ADMIN")]
        [HttpDelete("{taskId}")]
        public async Task<IActionResult> DeleteTask([FromRoute] Guid taskId)
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

                commonResponse = await _activityTaskService.DeleteTask(
                    taskId,
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
        /// Tài khoản test branchadminquan1@gmail.com,datnhtse151251@fpt.edu.vn cho user ,mật khẩu : 1234567890a
        /// </summary>
        [Authorize]
        [HttpGet("{taskId}")]
        public async Task<IActionResult> GetTaskDetails(
            int? page,
            int? pageSize,
            [FromRoute] Guid? taskId
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
                commonResponse = await _activityTaskService.GetTaskDetail(
                    taskId,
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
    }
}
