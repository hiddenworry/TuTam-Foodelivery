using BusinessLogic.Services;
using BusinessLogic.Utils.SecurityServices;
using DataAccess.EntityEnums;
using DataAccess.Models.Requests;
using DataAccess.Models.Requests.Enum;
using DataAccess.Models.Responses;
using DataAccess.ModelsEnum;
using FoodDonationDeliveryManagementAPI.Security.Authourization.PolicyProvider;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodDonationDeliveryManagementAPI.Controllers
{
    [Route("donated-requests")]
    [ApiController]
    public class DonatedRequestsController : ControllerBase
    {
        private readonly IDonatedRequestService _donatedRequestService;
        private readonly IDeliveryRequestService _deliveryRequestService;
        private readonly IJwtService _jwtService;
        private readonly ILogger<ActivitiesController> _logger;
        private readonly IConfiguration _config;

        public DonatedRequestsController(
            IDonatedRequestService donatedRequestService,
            IDeliveryRequestService deliveryRequestService,
            IJwtService jwtService,
            ILogger<ActivitiesController> logger,
            IConfiguration config
        )
        {
            _donatedRequestService = donatedRequestService;
            _deliveryRequestService = deliveryRequestService;
            _jwtService = jwtService;
            _logger = logger;
            _config = config;
        }

        /// <summary>
        /// Tạo yêu cầu quyên góp
        /// </summary>
        /// <remarks>
        /// Role: CONTRIBUTOR
        /// <br/>
        /// Permission: CREATE-DONATED-REQUEST
        /// <br/>
        /// Body:
        /// - **images**: các ảnh chụp của vật phẩm được quyên góp (từ 1 đến 5 ảnh).
        /// - **address**: địa chỉ text của nơi quyên góp (từ 10 đến 150 kí tự).
        /// - **location**: mảng tạo độ của nơi quyên góp (2 tọa độ).
        /// - **scheduledTimes**: danh sách các ngày và khung giờ có thể cho trong ngày đó (không null, không trống).
        ///     - **day**: ngày có thể cho, ex: "2023-09-30" (từ 2 ngày sau đến 3 tháng sau, không null, không trống).
        ///     - **startTime**: giờ bắt đầu của khung giờ cho, ex: "08:00" (không null, không trống).
        ///     - **endTime**: giờ kết thúc của khung giờ cho, ex: "08:00" (không null, không trống, sau **startTime** ít nhất 1h).
        /// - **note**: ghi chú (không null, phải có tối đa 2000 kí tự).
        /// - **activityId**: id của hoạt động nếu là quyên góp cụ thể cho hoạt động đó.
        /// - **donatedItemRequests**: danh sách các vật phẩm quyên góp (không null, không trống).
        ///     - **itemTemplateId**: id của mẫu vật phẩm (không được trùng).
        ///     - **quantity**: số lượng quyên góp (ít nhất là 1).
        ///     - **initialExpirationDate**: hạn sử dụng ước tính
        /// </remarks>
        /// <response code="200">
        /// Tạo yêu cầu quyên góp thành công, trả về Id của yêu cầu quyên góp:
        /// ```
        /// {
        ///     "status": 200,
        ///     "data": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
        ///     "pagination": null,
        ///     "message": "Yêu cầu cho đồ đã được gửi thành công đến các chi nhánh gần đó."
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
        ///     "message": "Không tìm thấy vật phẩm trong danh sách."
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
        [HttpPost]
        [PermissionAuthorize("CREATE-DONATED-REQUEST")]
        public async Task<IActionResult> CreateDonatedRequest(
            [FromForm] DonatedRequestCreatingRequest donatedRequestCreatingRequest
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
                    await _donatedRequestService.CreateDonatedRequestAsync(
                        donatedRequestCreatingRequest,
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
                    "An exception occurred in controller DonatedRequestsController, method CreateDonatedRequest."
                );
                return StatusCode(
                    500,
                    new CommonResponse { Status = 500, Message = internalServerErrorMsg }
                );
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetDonatedRequests(
            DonatedRequestStatus? status,
            DateTime? startDate,
            DateTime? endDate,
            Guid? branchId,
            Guid? userId,
            Guid? activityId,
            int? pageSize,
            int? page,
            string? orderBy,
            SortType? sortType
        )
        {
            string internalServerErrorMsg = _config[
                "ResponseMessages:CommonMsg:InternalServerErrorMsg"
            ];
            try
            {
                CommonResponse commonResponse = new CommonResponse();
                string? jwtToken = Request.Headers["Authorization"]
                    .FirstOrDefault()
                    ?.Split(" ")
                    .Last();
                Guid? callerId = null;
                string? userRoleName = null;
                if (jwtToken != null)
                {
                    callerId = _jwtService.GetUserIdByJwtToken(jwtToken);
                    userRoleName = _jwtService.GetRoleNameByJwtToken(jwtToken);
                }
                commonResponse = await _donatedRequestService.GetDonatedRequestsAsync(
                    status,
                    startDate,
                    endDate,
                    callerId,
                    branchId,
                    userId,
                    activityId,
                    pageSize,
                    page,
                    orderBy,
                    sortType,
                    userRoleName
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
                    "An exception occurred in controller AidRequestsController, methodex GetAidRequests."
                );
                return StatusCode(
                    500,
                    new CommonResponse { Status = 500, Message = internalServerErrorMsg }
                );
            }
        }

        [HttpGet]
        [Route("{id}")]
        public async Task<IActionResult> GetDonatedRequest(Guid id)
        {
            string internalServerErrorMsg = _config[
                "ResponseMessages:CommonMsg:InternalServerErrorMsg"
            ];
            try
            {
                CommonResponse commonResponse = new CommonResponse();
                string? jwtToken = Request.Headers["Authorization"]
                    .FirstOrDefault()
                    ?.Split(" ")
                    .Last();
                Guid? userId = null;
                string? userRoleName = null;
                if (jwtToken != null)
                {
                    userId = _jwtService.GetUserIdByJwtToken(jwtToken);
                    userRoleName = _jwtService.GetRoleNameByJwtToken(jwtToken);
                }
                commonResponse = await _donatedRequestService.GetDonatedRequestAsync(
                    id,
                    userId,
                    userRoleName
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
                    "An exception occurred in controller AidRequestsController, methodex GetAidRequests."
                );
                return StatusCode(
                    500,
                    new CommonResponse { Status = 500, Message = internalServerErrorMsg }
                );
            }
        }

        /// <summary>
        /// System get statistics of activty
        /// </summary>
        /// <remarks>
        /// Role: SYSTEM ADMIN
        /// <br />
        /// </remarks>
        [HttpGet]
        [Authorize(Roles = "SYSTEM_ADMIN,BRANCH_ADMIN")]
        [Route("statistics/all-status")]
        public async Task<IActionResult> CountDonatedRequest(
            DateTime? startDate,
            DateTime? endDate,
            Guid? branchId,
            Guid? userId,
            Guid? activityId
        )
        {
            try
            {
                string? jwtToken = Request.Headers["Authorization"]
                    .FirstOrDefault()
                    ?.Split(" ")
                    .Last();
                Guid? callerId = null;
                string? userRoleName = null;
                if (jwtToken != null)
                {
                    callerId = _jwtService.GetUserIdByJwtToken(jwtToken);
                    userRoleName = _jwtService.GetRoleNameByJwtToken(jwtToken);
                }
                CommonResponse commonResponse = new CommonResponse();
                if (userRoleName == RoleEnum.BRANCH_ADMIN.ToString())
                {
                    commonResponse = await _donatedRequestService.CountDonatedRequestByAllStatus(
                        startDate,
                        endDate,
                        branchId,
                        userId,
                        activityId,
                        userRoleName,
                        callerId
                    );
                }
                else
                {
                    commonResponse = await _donatedRequestService.CountDonatedRequestByAllStatus(
                        startDate,
                        endDate,
                        branchId,
                        userId,
                        activityId,
                        null,
                        null
                    );
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
                    $"An exception occurred in {nameof(DonatedRequestsController)}."
                );
                return StatusCode(
                    500,
                    new CommonResponse
                    {
                        Status = 500,
                        Message = _config["ResponseMessages:CommonMsg:InternalServerErrorMsg"]
                    }
                );
            }
        }

        /// <summary>
        /// System get statistics of activity
        /// </summary>
        /// <remarks>
        /// Role: SYSTEM ADMIN
        /// <br />
        /// </remarks>
        [HttpGet]
        [Authorize(Roles = "SYSTEM_ADMIN,BRANCH_ADMIN")]
        [Route("statistics")]
        public async Task<IActionResult> CountDonatedRequestByStatus(
            DateTime startDate,
            DateTime endDate,
            DonatedRequestStatus? status,
            TimeFrame timeFrame,
            Guid? branchId,
            Guid? userId,
            Guid? activityId
        )
        {
            try
            {
                string? jwtToken = Request.Headers["Authorization"]
                    .FirstOrDefault()
                    ?.Split(" ")
                    .Last();
                Guid? callerId = null;
                string? userRoleName = null;
                if (jwtToken != null)
                {
                    callerId = _jwtService.GetUserIdByJwtToken(jwtToken);
                    userRoleName = _jwtService.GetRoleNameByJwtToken(jwtToken);
                }
                CommonResponse commonResponse = new CommonResponse();
                if (userRoleName == RoleEnum.BRANCH_ADMIN.ToString())
                {
                    commonResponse = await _donatedRequestService.CountDonatedRequestByStatus(
                        startDate,
                        endDate,
                        status,
                        timeFrame,
                        branchId,
                        userId,
                        activityId,
                        userRoleName,
                        callerId
                    );
                }
                else
                {
                    commonResponse = await _donatedRequestService.CountDonatedRequestByStatus(
                        startDate,
                        endDate,
                        status,
                        timeFrame,
                        branchId,
                        userId,
                        activityId,
                        null,
                        null
                    );
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
                    $"An exception occurred in {nameof(DonatedRequestsController)}."
                );
                return StatusCode(
                    500,
                    new CommonResponse
                    {
                        Status = 500,
                        Message = _config["ResponseMessages:CommonMsg:InternalServerErrorMsg"]
                    }
                );
            }
        }

        /// <summary>
        /// Cancel pending donated request for user
        /// </summary>
        /// <remarks>
        /// Role: User
        /// </remarks>
        [Authorize]
        [HttpPut]
        [Route("{id}")]
        public async Task<IActionResult> CancelDonatedRequest(Guid id)
        {
            string internalServerErrorMsg = _config[
                "ResponseMessages:CommonMsg:InternalServerErrorMsg"
            ];
            try
            {
                CommonResponse commonResponse = new CommonResponse();
                string? jwtToken = Request.Headers["Authorization"]
                    .FirstOrDefault()
                    ?.Split(" ")
                    .Last();
                Guid? userId = null;

                if (jwtToken != null)
                {
                    userId = _jwtService.GetUserIdByJwtToken(jwtToken);
                }
                commonResponse = await _donatedRequestService.CancelDonatedRequest(
                    id,
                    userId!.Value
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
                    "An exception occurred in controller AidRequestsController, methodex GetAidRequests."
                );
                return StatusCode(
                    500,
                    new CommonResponse { Status = 500, Message = internalServerErrorMsg }
                );
            }
        }

        /// <summary>
        /// CONTRIBUTOR get finished and imported delivery requests of their donated request
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "CONTRIBUTOR")]
        [Route("{donatedRequestId}/finished-delivery-requests")]
        public async Task<IActionResult> GetFinishedDeliveryRequestsByDonatedRequestIdForUser(
            Guid donatedRequestId,
            int? pageSize,
            int? page
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
                    await _deliveryRequestService.GetFinishedDeliveryRequestsByDonatedRequestIdForUserAsync(
                        donatedRequestId,
                        userId,
                        pageSize,
                        page
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
                    $"An exception occurred in {nameof(DonatedRequestsController)}."
                );
                return StatusCode(
                    500,
                    new CommonResponse { Status = 500, Message = internalServerErrorMsg }
                );
            }
        }
    }
}
