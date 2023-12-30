using BusinessLogic.Services;
using BusinessLogic.Utils.SecurityServices;
using DataAccess.Models.Requests;
using DataAccess.Models.Responses;
using FoodDonationDeliveryManagementAPI.Security.Authourization.PolicyProvider;
using Microsoft.AspNetCore.Mvc;

namespace FoodDonationDeliveryManagementAPI.Controllers
{
    [Route("acceptable-donated-requests")]
    [ApiController]
    public class AcceptableDonatedRequestsController : ControllerBase
    {
        private readonly IAcceptableDonatedRequestService _acceptableDonatedRequestService;
        private readonly ILogger<ActivitiesController> _logger;
        private readonly IConfiguration _config;
        private readonly IJwtService _jwtService;

        public AcceptableDonatedRequestsController(
            IAcceptableDonatedRequestService acceptableDonatedRequestService,
            ILogger<ActivitiesController> logger,
            IConfiguration config,
            IJwtService jwtService
        )
        {
            _acceptableDonatedRequestService = acceptableDonatedRequestService;
            _logger = logger;
            _config = config;
            _jwtService = jwtService;
        }

        /// <summary>
        /// Xét duyệt yêu quyên góp.
        /// </summary>
        /// <remarks>
        /// Role: BRANCH_ADMIN
        /// <br/>
        /// Permission: CONFIRM-DONATED-REQUEST
        /// <br/>
        /// Body:
        /// - **id**: Id của yêu cầu cần hỗ trợ (không null).
        /// - **rejectingReason**: lý do từ chối (từ 30 đến 300 kí tự, chỉ khi không nhận ít nhất 1 vật phẩm thì mới truyền lý do từ chối).
        /// - **donatedItemIds**: các vật phẩm được quyên góp được chấp nhận
        /// </remarks>
        /// <response code="200">
        /// Xét duyệt thành công, trả về Id của yêu cầu cần hỗ trợ:
        /// ```
        /// {
        ///     "status": 200,
        ///     "data": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
        ///     "pagination": null,
        ///     "message": "Chấp nhận yêu cầu quyên góp thành công."
        /// }
        /// ```
        /// </response>
        /// <response code="400">
        /// Request không hợp lệ, trả về thông báo:
        /// ```
        /// {
        ///     "status": 400,
        ///     "data": null,
        ///     "pagination": null,
        ///     "message": "Không tìm thấy yêu cầu quyên góp đang chờ xét duyệt."
        /// }
        /// ```
        /// </response>
        /// <response code="403">
        /// Người dùng không có quyền thực hiện, trả về thông báo:
        /// ```
        /// {
        ///     "status": 403,
        ///     "data": null,
        ///     "pagination": null,
        ///     "message": "Bạn hiện không là thành viên của một chi nhánh nào."
        /// }
        /// ```
        /// </response>
        /// <response code="500">
        /// Lỗi hệ thống, trả về thông báo:
        /// ```
        /// {
        ///     "status": 500,
        ///     "data": null,
        ///     "pagination": null,
        ///     "message": "Lỗi hệ thống."
        /// }
        /// ```
        /// </response>
        [HttpPut]
        [PermissionAuthorize("CONFIRM-DONATED-REQUEST")]
        public async Task<IActionResult> ConfirmDonatedRequest(
            DonatedRequestConfirmingRequest donatedRequestConfirmingRequest
        )
        {
            string internalServerErrorMsg = _config[
                "ResponseMessages:CommonMsg:InternalServerErrorMsg"
            ];
            try
            {
                string jwtToken = Request.Headers["Authorization"]
                    .FirstOrDefault()
                    ?.Split(" ")
                    .Last()!;
                Guid userId = _jwtService.GetUserIdByJwtToken(jwtToken);
                CommonResponse commonResponse =
                    await _acceptableDonatedRequestService.ConfirmDonatedRequestAsync(
                        donatedRequestConfirmingRequest,
                        userId
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
                    "An exception occurred in controller AcceptableDonatedRequestsController, method ConfirmDonatedRequest."
                );
                return StatusCode(
                    500,
                    new CommonResponse { Status = 500, Message = internalServerErrorMsg }
                );
            }
        }
    }
}
