using BusinessLogic.Services;
using DataAccess.Models.Requests;
using DataAccess.Models.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodDonationDeliveryManagementAPI.Controllers
{
    [Route("phases")]
    [ApiController]
    public class PhasesController : ControllerBase
    {
        private readonly IPhaseService _phaseService;
        private readonly ILogger<ActivitiesController> _logger;
        private readonly IConfiguration _config;

        public PhasesController(
            IPhaseService phaseService,
            ILogger<ActivitiesController> logger,
            IConfiguration config
        )
        {
            _phaseService = phaseService;
            _logger = logger;
            _config = config;
        }

        /// <summary>
        /// Use for create phase in activity Role: Branch_Admin
        /// </summary>
        /// <remarks>
        /// Register
        /// Parameters:
        /// - **Name**: String(8-200 character).
        /// - **EstimatedStartDate**: Date (not null, Not in the past).
        /// - **EstimatedEndDate**: Date (not null, Not in the past, after start date).
        /// - **activityId**: Guid (not null).
        /// </remarks>
        /// <response code="200">If success.</response>
        /// <response code="400">If validation error.</response>
        /// <response code="500">Internal server error.</response>
        [Authorize(Roles = "BRANCH_ADMIN,SYSTEM_ADMIN")]
        [HttpPost]
        public async Task<IActionResult> CreatePhase([FromBody] PharseCreatingRequest request)
        {
            CommonResponse commonResponse = new CommonResponse();
            string internalServerErrorMsg = _config[
                "ResponseMessages:UserMsg:InternalServerErrorMsg"
            ];
            try
            {
                commonResponse = await _phaseService.CreatePharse(request);
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
        /// Use for create phase in activity Role: Branch_Admin
        /// Tài khoản test branchadminquan1@gmail.com ,mật khẩu : 1234567890a
        /// </summary>
        /// <remarks>
        /// Parameters:  Để null để ko update
        /// - **Name**: String(8-200 character).
        /// - **EstimatedStartDate**: Date ( Not in the past).
        /// - **EstimatedEndDate**: Date ( Not in the past, after start date).
        /// - **status**: 0 (Bắt đầu phase, khi phase đang ở trạng thái NOT_STARTED) 1(Kết thúc phase, khi phase đang ở trạng thái đã bắt đầu(STARTED)).
        /// </remarks>
        /// <response code="200">If success.</response>
        /// <response code="400">If validation error.</response>
        /// <response code="500">Internal server error.</response>
        [Authorize(Roles = "BRANCH_ADMIN,SYSTEM_ADMIN")]
        [HttpPut("")]
        public async Task<IActionResult> UpdatePhase([FromBody] List<PhaseUpdatingRequest> request)
        {
            CommonResponse commonResponse = new CommonResponse();
            string internalServerErrorMsg = _config[
                "ResponseMessages:UserMsg:InternalServerErrorMsg"
            ];
            try
            {
                commonResponse = await _phaseService.UpdatePharse(request);
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
        /// Get phase by activty Id
        /// </summary>
        /// <remarks>
        /// Parameters:  Để null để ko update
        /// - **activityId**: Guid(not null).
        /// </remarks>
        /// <response code="200">
        /// Get branches success, return list:
        /// ```
        /// {
        ///     "status": 200,
        ///     "data": [
        ///   {
        ///    "id": "82e67444-e178-ee11-9937-00224857a4f7",
        ///    "name": "Giai đoạn 1",
        ///    "startDate": "0001-01-01T00:00:00",
        ///   "endDate": "0001-01-01T00:00:00",
        ///   "estimatedStartDate": "2023-11-02T18:04:00.523",
        ///   "estimatedEndDate": "2023-11-04T18:08:00.523",
        ///   "status": "NOT_STARTED",
        ///   "activityId": "630b5064-a274-ee11-9937-000d3ac814a5"
        ///  }
        ///     ],
        ///     "pagination": {
        ///         "currentPage": null,
        ///         "pageSize": null,
        ///         "total": null
        ///     },
        ///     "message": "Lấy danh sách các chi nhánh thành công."
        /// }
        /// ```
        /// </response>
        /// <response code="400">If validation error.</response>
        /// <response code="500">Internal server error.</response>
        [Authorize]
        [HttpGet("activity/{activityId}")]
        public async Task<IActionResult> GetPhaseByActivityId(Guid activityId)
        {
            CommonResponse commonResponse = new CommonResponse();
            string internalServerErrorMsg = _config[
                "ResponseMessages:UserMsg:InternalServerErrorMsg"
            ];
            try
            {
                commonResponse = await _phaseService.GetPhaseByActivityId(activityId);
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
        /// Use for delete phase in activity Role: Branch_Admin
        /// Tài khoản test branchadminquan1@gmail.com ,mật khẩu : 1234567890a
        /// </summary>
        /// <remarks>
        /// Chỉ được xóa phase ở trạng thái not started
        /// - **phaseId**: Guid(Not null).
        /// </remarks>
        /// <response code="200">If success.</response>
        /// <response code="400">If validation error.</response>
        /// <response code="500">Internal server error.</response>
        [Authorize(Roles = "BRANCH_ADMIN,SYSTEM_ADMIN")]
        [HttpDelete("{phaseId}")]
        public async Task<IActionResult> DeletePhase([FromRoute] Guid phaseId)
        {
            CommonResponse commonResponse = new CommonResponse();
            string internalServerErrorMsg = _config[
                "ResponseMessages:UserMsg:InternalServerErrorMsg"
            ];
            try
            {
                commonResponse = await _phaseService.DeletePharse(phaseId);
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
