using BusinessLogic.Services;
using BusinessLogic.Utils.SecurityServices;
using DataAccess.Models.Requests.Enum;
using DataAccess.Models.Responses;
using DataAccess.ModelsEnum;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodDonationDeliveryManagementAPI.Controllers
{
    [Route("aid-items")]
    [ApiController]
    public class AidItemsController : ControllerBase
    {
        private readonly IAidItemService _aidItemService;
        private readonly ILogger<ActivitiesController> _logger;
        private readonly IConfiguration _config;
        private readonly IJwtService _jwtService;

        public AidItemsController(
            IAidItemService aidItemService,
            ILogger<ActivitiesController> logger,
            IConfiguration config,
            IJwtService jwtService
        )
        {
            _aidItemService = aidItemService;
            _logger = logger;
            _config = config;
            _jwtService = jwtService;
        }

        /// <summary>
        /// Get paging list of item template and search key word. Role: User
        /// </summary>
        /// <remarks>
        /// - searchKeyWord: string.
        /// - Take: int(Number of value that you want to take order by highest point to low)(Do có ít dữ liệu nên mảng này ko có nhiều giá trị nên lấy khoản 1-2 giá trị có số điểm cao nhất là hợp lý nhất).
        /// </remarks>
        /// <response code="200">
        /// Get success, return list:
        /// ```
        ///{
        ///  "status": 200,
        ///     "data": [
        /// {
        ///   "itemTemplateId": "777bb661-da5d-ee11-9937-6045bd1b698d",
        ///   "name": "Cá Hồi",
        ///   "image": "Link",
        ///   "attributes": []
        /// },
        /// {
        ///   "itemTemplateId": "787bb661-da5d-ee11-9937-6045bd1b698d",
        ///   "name": "Cá Hồi",
        ///  "image": "Link",
        ///   "attributes": [
        ///     {
        ///       "attributeValue": "Phi Lê"
        ///    }
        ///  ],
        ///  "pagination": null
        ///  "message": null
        ///}
        /// ```
        /// </response>
        /// <response code="500">
        /// Internal server error, return message:
        /// ```
        /// {
        ///     "status": 500,
        ///     "data": null,
        ///     "pagination": null,
        ///     "message": "Lỗi hệ thống."
        /// }
        /// ```
        /// </response>
        [Authorize(Roles = "BRANCH_ADMIN")]
        [HttpGet]
        public async Task<IActionResult> GetAidItemsForBranchAdmin(
            string? keyWord,
            UrgencyLevel? urgencyLevel,
            DateTime? startDate,
            DateTime? endDate,
            Guid? charityUnitId,
            int? pageSize,
            int? page,
            string? orderBy,
            SortType? sortType
        )
        {
            string internalServerErrorMsg = _config[
                "ResponseMessages:UserMsg:InternalServerErrorMsg"
            ];
            CommonResponse commonResponse = new CommonResponse();
            try
            {
                string jwtToken = Request.Headers["Authorization"]
                    .FirstOrDefault()
                    ?.Split(" ")
                    .Last()!;
                Guid userId = _jwtService.GetUserIdByJwtToken(jwtToken);

                commonResponse = await _aidItemService.GetAidItemsForBranchAdminAsync(
                    keyWord,
                    urgencyLevel,
                    startDate,
                    endDate,
                    charityUnitId,
                    pageSize,
                    page,
                    orderBy,
                    sortType,
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
            catch
            {
                commonResponse.Status = 500;
                commonResponse.Message = internalServerErrorMsg;
                return StatusCode(500, commonResponse);
            }
        }
    }
}
