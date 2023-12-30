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
    [Route("aid-requests")]
    [ApiController]
    public class AidRequestsController : ControllerBase
    {
        private readonly IAidRequestService _aidRequestService;
        private readonly IDeliveryRequestService _deliveryRequestService;
        private readonly ILogger<ActivitiesController> _logger;
        private readonly IConfiguration _config;
        private readonly IJwtService _jwtService;

        public AidRequestsController(
            IAidRequestService aidRequestService,
            IDeliveryRequestService deliveryRequestService,
            ILogger<ActivitiesController> logger,
            IConfiguration config,
            IJwtService jwtService
        )
        {
            _aidRequestService = aidRequestService;
            _deliveryRequestService = deliveryRequestService;
            _logger = logger;
            _config = config;
            _jwtService = jwtService;
        }

        /// <summary>
        /// Tạo yêu cầu hỗ trợ vật phẩm
        /// </summary>
        /// <remarks>
        /// Role: CHARITY
        /// <br/>
        /// Permission: CREATE-AID-REQUEST
        /// <br/>
        /// Body:
        /// - **scheduledTimes**: danh sách các ngày và khung giờ có thể nhận trong ngày đó (không null, không trống).
        ///     - **day**: ngày có thể nhận, ex: "2023-09-30" (từ 2 ngày sau đến 3 tháng sau, không null, không trống).
        ///     - **startTime**: giờ bắt đầu của khung giờ nhận, ex: "08:00" (không null, không trống).
        ///     - **endTime**: giờ kết thúc của khung giờ nhận, ex: "08:00" (không null, không trống, sau **startTime** ít nhất 1h).
        /// - **note**: ghi chú (phải có tối đa 2000 kí tự nếu có giá trị).
        /// - **aidItemRequests**: danh sách các vật phẩm cần được hỗ trợ (không null, không trống).
        ///     - **itemTemplateId**: id của mẫu vật phẩm (không được trùng).
        ///     - **quantity**: số lượng cần hỗ trợ (ít nhất là 1).
        /// </remarks>
        /// <response code="200">
        /// Tạo yêu cầu cần hỗ trợ thành công, trả về Id của yêu cầu cần hỗ trợ:
        /// ```
        /// {
        ///     "status": 200,
        ///     "data": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
        ///     "pagination": null,
        ///     "message": "Yêu cầu cần hổ trợ đã được gửi thành công đến các chi nhánh gần đó."
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
        /// <response code="403">
        /// Người dùng không có quyền thực hiện, trả về thông báo:
        /// ```
        /// {
        ///     "status": 403,
        ///     "data": null,
        ///     "pagination": null,
        ///     "message": "Tổ chức từ thiện hoặc đơn vị không tìm thấy hoặc không còn hoạt động."
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
        [PermissionAuthorize("CREATE-AID-REQUEST")]
        public async Task<IActionResult> CreateAidRequest(
            AidRequestCreatingRequest aidRequestCreatingRequest
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
                //string userRoleName = _jwtService.GetRoleNameByJwtToken(jwtToken);
                CommonResponse commonResponse =
                    await _aidRequestService.CreateAidRequestByCharityUnitAsync(
                        aidRequestCreatingRequest,
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
                    "An exception occurred in controller AidRequestsController, method CreateAidRequest."
                );
                return StatusCode(
                    500,
                    new CommonResponse { Status = 500, Message = internalServerErrorMsg }
                );
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAidRequests(
            AidRequestStatus? status,
            DateTime? startDate,
            DateTime? endDate,
            Guid? branchId,
            Guid? charityUnitId,
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
                commonResponse = await _aidRequestService.GetAidRequestsAsync(
                    status,
                    startDate,
                    endDate,
                    callerId,
                    branchId,
                    charityUnitId,
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
        public async Task<IActionResult> GetAidRequest(Guid id)
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
                commonResponse = await _aidRequestService.GetAidRequestAsync(
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
        /// System get statistics of aid request
        /// </summary>
        /// <remarks>
        /// Role: SYSTEM ADMIN
        /// <br />
        /// </remarks>
        [HttpGet]
        [Authorize(Roles = "SYSTEM_ADMIN,BRANCH_ADMIN")]
        [Route("statistics/all-status")]
        public async Task<IActionResult> CountAidRequest(
            DateTime? startDate,
            DateTime? endDate,
            Guid? branchId,
            Guid? charityUnitId
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
                    commonResponse = await _aidRequestService.CountAidRequestByAllStatus(
                        startDate,
                        endDate,
                        branchId,
                        charityUnitId,
                        userRoleName,
                        callerId
                    );
                }
                else
                {
                    commonResponse = await _aidRequestService.CountAidRequestByAllStatus(
                        startDate,
                        endDate,
                        branchId,
                        charityUnitId,
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
                    $"An exception occurred in {nameof(DeliveryRequestsController)}."
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
        /// System get statistics of aid request
        /// </summary>
        /// <remarks>
        /// Role: SYSTEM ADMIN
        /// <br />
        /// </remarks>
        [HttpGet]
        [Authorize(Roles = "SYSTEM_ADMIN,BRANCH_ADMIN")]
        [Route("statistics")]
        public async Task<IActionResult> CountActivityByStatus(
            DateTime startDate,
            DateTime endDate,
            AidRequestStatus? status,
            TimeFrame timeFrame,
            Guid? branchId,
            Guid? charityUnitId
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
                    commonResponse = await _aidRequestService.CountAidRequestByStatus(
                        startDate,
                        endDate,
                        status,
                        timeFrame,
                        branchId,
                        charityUnitId,
                        userRoleName,
                        callerId
                    );
                }
                else
                {
                    commonResponse = await _aidRequestService.CountAidRequestByStatus(
                        startDate,
                        endDate,
                        status,
                        timeFrame,
                        branchId,
                        charityUnitId,
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
                    $"An exception occurred in {nameof(DeliveryRequestsController)}."
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
        /// Cancel pending aid request for charity
        /// </summary>
        /// <remarks>
        /// Role: CHARITY
        /// </remarks>
        [Authorize(Roles = "CHARITY")]
        [HttpPut]
        [Route("{id}")]
        public async Task<IActionResult> CancelAidRequest(Guid id)
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
                commonResponse = await _aidRequestService.CancelAidRequest(id, userId!.Value);
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
        /// Branch admin finish accepted or processing aid request
        /// </summary>
        /// <remarks>
        /// Role: BRANCH_ADMIN
        /// </remarks>
        [Authorize(Roles = "BRANCH_ADMIN")]
        [HttpPut]
        [Route("{aidRequestId}/finished-aid-request")]
        public async Task<IActionResult> FinishAidRequest(Guid aidRequestId)
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
                CommonResponse commonResponse = await _aidRequestService.FinishAidRequestAsync(
                    aidRequestId,
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
                    $"An exception occurred in controller {nameof(AidRequestsController)}, method {nameof(CreateAidRequest)}."
                );
                return StatusCode(
                    500,
                    new CommonResponse { Status = 500, Message = internalServerErrorMsg }
                );
            }
        }

        /// <summary>
        /// CHARITY get finished delivery requests of their aid request; BRANCH_ADMIN, SYSTEM_ADMIN get finished delivery requests of any aid request
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "CHARITY,BRANCH_ADMIN,SYSTEM_ADMIN")]
        [Route("{aidRequestId}/finished-delivery-requests")]
        public async Task<IActionResult> GetFinishedDeliveryRequestsByAidRequestIdForCharityUnit(
            Guid aidRequestId,
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
                string userRoleName = _jwtService.GetRoleNameByJwtToken(jwtToken);
                CommonResponse commonResponse =
                    await _deliveryRequestService.GetFinishedDeliveryRequestsByAidRequestIdForCharityUnitAsync(
                        aidRequestId,
                        userId,
                        userRoleName,
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
                _logger.LogError(ex, $"An exception occurred in {nameof(AidRequestsController)}.");
                return StatusCode(
                    500,
                    new CommonResponse { Status = 500, Message = internalServerErrorMsg }
                );
            }
        }
    }
}
