using BusinessLogic.Services;
using BusinessLogic.Utils.SecurityServices;
using DataAccess.EntityEnums;
using DataAccess.Models.Responses;
using DataAccess.ModelsEnum;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodDonationDeliveryManagementAPI.Controllers
{
    [Route("reports")]
    [ApiController]
    public class ReportsController : ControllerBase
    {
        private readonly IReportService _reportService;
        private readonly ILogger<ActivitiesController> _logger;
        private readonly IConfiguration _config;
        private readonly IJwtService _jwtService;

        public ReportsController(
            IReportService reportService,
            ILogger<ActivitiesController> logger,
            IConfiguration config,
            IJwtService jwtService
        )
        {
            _reportService = reportService;
            _logger = logger;
            _config = config;
            _jwtService = jwtService;
        }

        [Authorize(Roles = "SYSTEM_ADMIN,BRANCH_ADMIN")]
        [HttpGet("admin")]
        public async Task<IActionResult> GetReportsForAdminAsync(
            int? page,
            int? pageSize,
            ReportType? reportType,
            string? keyWord
        )
        {
            string internalServerErrorMsg = _config[
                "ResponseMessages:CommonMsg:InternalServerErrorMsg"
            ];
            try
            {
                CommonResponse commonResponse = new CommonResponse();
                var token = HttpContext.Request.Headers["Authorization"]
                    .FirstOrDefault()
                    ?.Split(" ")
                    .Last();

                Guid userId = Guid.Empty;
                string role = "";

                if (token != null)
                {
                    userId = _jwtService.GetUserIdByJwtToken(token);
                    role = _jwtService.GetRoleNameByJwtToken(token);
                }
                if (role == RoleEnum.SYSTEM_ADMIN.ToString())
                {
                    commonResponse = await _reportService.GetReportAsync(
                        page,
                        pageSize,
                        null,
                        keyWord,
                        reportType
                    );
                }
                else if (role == RoleEnum.BRANCH_ADMIN.ToString())
                {
                    commonResponse = await _reportService.GetReportAsync(
                        page,
                        pageSize,
                        userId,
                        keyWord,
                        reportType
                    );
                }
                else
                {
                    return StatusCode(403, commonResponse);
                }
                switch (commonResponse.Status)
                {
                    case 200:
                        return Ok(commonResponse);
                    case 400:
                        return BadRequest(commonResponse);
                    case 403:
                        return StatusCode(403, commonResponse);
                    default:
                        return StatusCode(500, commonResponse);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    $"An exception occurred in {nameof(DeliveryRequestsController)}."
                );
                return StatusCode(
                    500,
                    new CommonResponse { Status = 500, Message = internalServerErrorMsg }
                );
            }
        }

        [Authorize(Roles = "SYSTEM_ADMIN,BRANCH_ADMIN")]
        [HttpGet("admin/delivery-request/{deliveryRequestId}")]
        public async Task<IActionResult> GetReportsForAdminByDeliveryRequestIdAsync(
            int? page,
            int? pageSize,
            Guid deliveryRequestId,
            ReportType? reportType
        )
        {
            string internalServerErrorMsg = _config[
                "ResponseMessages:CommonMsg:InternalServerErrorMsg"
            ];
            try
            {
                CommonResponse commonResponse = new CommonResponse();
                var token = HttpContext.Request.Headers["Authorization"]
                    .FirstOrDefault()
                    ?.Split(" ")
                    .Last();

                commonResponse = await _reportService.GetReportByDeliveryRequestIdAsync(
                    page,
                    pageSize,
                    deliveryRequestId,
                    reportType
                );

                switch (commonResponse.Status)
                {
                    case 200:
                        return Ok(commonResponse);
                    case 400:
                        return BadRequest(commonResponse);
                    case 403:
                        return StatusCode(403, commonResponse);
                    default:
                        return StatusCode(500, commonResponse);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    $"An exception occurred in {nameof(DeliveryRequestsController)}."
                );
                return StatusCode(
                    500,
                    new CommonResponse { Status = 500, Message = internalServerErrorMsg }
                );
            }
        }

        [Authorize()]
        [HttpGet("")]
        public async Task<IActionResult> GetReportsForUserAsync(
            int? page,
            int? pageSize,
            ReportType? reportType
        )
        {
            string internalServerErrorMsg = _config[
                "ResponseMessages:CommonMsg:InternalServerErrorMsg"
            ];
            try
            {
                CommonResponse commonResponse = new CommonResponse();
                var token = HttpContext.Request.Headers["Authorization"]
                    .FirstOrDefault()
                    ?.Split(" ")
                    .Last();

                Guid userId = Guid.Empty;

                if (token != null)
                {
                    userId = _jwtService.GetUserIdByJwtToken(token);
                }

                commonResponse = await _reportService.GetReportAsync(
                    page,
                    pageSize,
                    userId,
                    null,
                    reportType
                );

                switch (commonResponse.Status)
                {
                    case 200:
                        return Ok(commonResponse);
                    case 400:
                        return BadRequest(commonResponse);
                    case 403:
                        return StatusCode(403, commonResponse);
                    default:
                        return StatusCode(500, commonResponse);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    $"An exception occurred in {nameof(DeliveryRequestsController)}."
                );
                return StatusCode(
                    500,
                    new CommonResponse { Status = 500, Message = internalServerErrorMsg }
                );
            }
        }
    }
}
