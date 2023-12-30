using BusinessLogic.Services;
using BusinessLogic.Utils.SecurityServices;
using DataAccess.Models.Requests;
using DataAccess.Models.Responses;
using FoodDonationDeliveryManagementAPI.Security.Authourization.PolicyProvider;
using Microsoft.AspNetCore.Mvc;

namespace FoodDonationDeliveryManagementAPI.Controllers
{
    [Route("acceptable-aid-requests")]
    [ApiController]
    public class AcceptableAidRequestsController : ControllerBase
    {
        private readonly IAcceptableAidRequestService _acceptableAidRequestService;
        private readonly ILogger<ActivitiesController> _logger;
        private readonly IConfiguration _config;
        private readonly IJwtService _jwtService;

        public AcceptableAidRequestsController(
            IAcceptableAidRequestService acceptableAidRequestService,
            ILogger<ActivitiesController> logger,
            IConfiguration config,
            IJwtService jwtService
        )
        {
            _acceptableAidRequestService = acceptableAidRequestService;
            _logger = logger;
            _config = config;
            _jwtService = jwtService;
        }

        /// <summary>
        /// Xét duyệt yêu cần cần hỗ trợ.
        /// </summary>
        /// <remarks>
        /// Role: BRANCH_ADMIN
        /// <br/>
        /// Permission: CONFRIM-AID-REQUEST
        /// <br/>
        /// Body:
        /// - **id**: Id của yêu cầu cần hỗ trợ (không null).
        /// - **status**: trạng thái xét duyệt - ACCEPTED(1) hoặc REJECTED(2).
        /// - **rejectingReason**: lý do từ chối (từ 30 đến 300 kí tự, chỉ khi trạng thái xét duyệt là REJECTED(2) thì mới truyền lý do từ chối).
        /// </remarks>
        /// <response code="200">
        /// Xét duyệt thành công, trả về Id của yêu cầu cần hỗ trợ:
        /// ```
        /// {
        ///     "status": 200,
        ///     "data": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
        ///     "pagination": null,
        ///     "message": "Chấp nhận yêu cầu cần hỗ trợ thành công."
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
        ///     "message": "Không tìm thấy yêu cầu cần hỗ trợ đang chờ xét duyệt."
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
        [PermissionAuthorize("CONFIRM-AID-REQUEST")]
        public async Task<IActionResult> ConfirmAidRequest(
            AidRequestComfirmingRequest aidRequestComfirmingRequest
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
                string userRoleName = _jwtService.GetRoleNameByJwtToken(jwtToken);
                CommonResponse commonResponse =
                    await _acceptableAidRequestService.ConfirmAidRequestAsync(
                        aidRequestComfirmingRequest,
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
                    "An exception occurred in controller AcceptableAidRequestsController, method ConfirmAidRequest."
                );
                return StatusCode(
                    500,
                    new CommonResponse { Status = 500, Message = internalServerErrorMsg }
                );
            }
        }

        //[HttpGet]
        //[Route("time-stamp")]
        //public IActionResult GetTimeStamp(DateTime dateTime)
        //{
        //    long unixTimestampMilliseconds = (long)
        //        (dateTime - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;

        //    return Ok(unixTimestampMilliseconds);
        //}

        //[HttpGet]
        //[Route("date-time")]
        //public IActionResult GetTimeStamp(long timeStamp)
        //{
        //    long timestampInSeconds = timeStamp;

        //    DateTime dateTime = DateTimeOffset.FromUnixTimeSeconds(timestampInSeconds).UtcDateTime;

        //    return Ok(dateTime);
        //}
    }
}
