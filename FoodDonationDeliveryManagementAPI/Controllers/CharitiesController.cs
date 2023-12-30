using BusinessLogic.Services;
using BusinessLogic.Utils.SecurityServices;
using DataAccess.EntityEnums;
using DataAccess.Models.Requests;
using DataAccess.Models.Responses;
using DataAccess.ModelsEnum;
using FoodDonationDeliveryManagementAPI.Security.Authourization.PolicyProvider;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodDonationDeliveryManagementAPI.Controllers
{
    [Route("charities")]
    [ApiController]
    public class CharitiesController : ControllerBase
    {
        private readonly ICharityService _charityService;
        private readonly ILogger<ActivitiesController> _logger;
        private readonly IConfiguration _config;
        private readonly IJwtService _jwtService;

        public CharitiesController(
            ICharityService charityService,
            ILogger<ActivitiesController> logger,
            IConfiguration config,
            IJwtService jwtService
        )
        {
            _charityService = charityService;
            _logger = logger;
            _config = config;
            _jwtService = jwtService;
        }

        /// <summary>
        /// Get charities
        /// </summary>
        /// <param name="page">Default 1</param>
        /// <param name="pageSize">Default 10</param>
        /// <param name="sortType"></param>
        /// <param name="charityStatus">UNVERIFIED(0), ACTIVE(1) - leave nul to get all status</param>
        /// <param name="name">Charity's name</param>
        /// <param name="isWaitingToUpdate"></param>
        /// <response code="200">
        /// ```
        /// {
        ///     "status": 200,
        ///     "data": null,
        ///     "pagination": null,
        ///     "message": "Cập nhật thành công."
        /// }
        /// ```
        ///</response>
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
        ///</response>

        // [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetListCharites(
            int? page,
            int? pageSize,
            SortType? sortType,
            CharityStatus? charityStatus,
            string? name,
            bool isWaitingToUpdate
        )
        {
            CommonResponse commonResponse = new CommonResponse();
            string internalServerErrorMsg = _config[
                "ResponseMessages:UserMsg:InternalServerErrorMsg"
            ];
            try
            {
                commonResponse = await _charityService.GetCharitiesAsync(
                    page,
                    pageSize,
                    charityStatus,
                    sortType,
                    name,
                    isWaitingToUpdate
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
        /// Register to become collaborator
        /// </summary>
        /// <br/>
        /// <remarks>
        /// Body:
        /// - **Name**: 8-60 charater(Require)
        /// - **Email**: Email address(Require)
        /// - **Description**:  (under 500 characters)
        /// - **Image**: file(png,jpg,jpeg)
        /// - **CharityUnits**: object(Require)
        /// - **Name**: 8-60 charater(Require)
        /// - **Email**: Email address(Require)
        /// - **Description**:  (under 500 characters)
        /// - **Location**: double array[] (8-120 characters)
        /// - **Adresss**: 8-200 character(Require)
        /// - **LegalDocument**: file pdf,docx(Require)
        /// </remarks>
        /// <br />
        /// <response code="200">
        /// ```
        /// {
        ///     "status": 200,
        ///     "data": null,
        ///     "pagination": null,
        ///     "message": "Cập nhật thành công."
        /// }
        /// ```
        ///</response>
        /// <response code="400">
        /// ```
        /// {
        ///     "status": 400,
        ///     "data": null,
        ///     "pagination": null,
        ///     "message": "User không tồn tại"
        /// }
        /// ```
        ///</response>
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
        ///</response>

        [HttpPost]
        public async Task<IActionResult> RegiterToBecomeCharties(
            [FromForm] CharityCreatingRequest charityCreatingRequest
        )
        {
            CommonResponse commonResponse = new CommonResponse();
            string internalServerErrorMsg = _config[
                "ResponseMessages:UserMsg:InternalServerErrorMsg"
            ];
            try
            {
                commonResponse = await _charityService.RegisterToBecomeCharity(
                    charityCreatingRequest
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
        /// Admin confirm charity(Role SystemAdmin)
        /// </summary>
        /// <br/>
        /// <remarks>
        /// Body:
        /// - **isAccept**: true(Require)
        /// - **charityId**: id
        /// - **reason**: when isAccepted = false, reason must be input to notify to user
        /// </remarks>
        /// <br />
        /// <response code="200">
        /// ```
        /// {
        ///     "status": 200,
        ///     "data": null,
        ///     "pagination": null,
        ///     "message": "Cập nhật thành công."
        /// }
        /// ```
        ///</response>
        /// <response code="400">
        /// ```
        /// {
        ///     "status": 400,
        ///     "data": null,
        ///     "pagination": null,
        ///     "message": "User không tồn tại"
        /// }
        /// ```
        ///</response>
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
        ///</response>

        [PermissionAuthorize("UPDATE-CHARITY")]
        [HttpPut("{charityId}")]
        public async Task<IActionResult> ConfirmCharity(
            [FromRoute] Guid charityId,
            [FromBody] ConfirmCharityRequest request
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
                commonResponse = await _charityService.ConfirmCharity(
                    charityId,
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
        /// Get charity details
        /// </summary>
        /// <br/>
        /// <remarks>
        /// - **charityId**: id
        /// </remarks>
        /// <br />
        /// <response code="200">
        /// ```
        ///{
        /// "status": 200,
        /// "data": {
        ///   "name": "Hội Chữ thập đỏ Việt Nam",
        ///  "status": "ACTIVE",
        ///  "email": "",
        ///   "id": "a1f55c3d-bb57-ee11-9937-6045bd1b698d",
        ///    "numberOfPost": 0,
        ///    "numberOfCharityUnit": 1,
        ///    "description": "Hội Chữ thập đỏ Việt Nam là tổ chức xã hội nhân đạo của quần chúng, do Chủ tịch Hồ Chí Minh sáng lập ngày 23/11/1946 và Người làm Chủ tịch danh dự đầu tiên của Hội. Hội tập hợp mọi người Việt Nam, không phân biệt thành phần dân tộc, tôn giáo, nam nữ để làm công tác nhân đạo.",
        ///   "logo": "stgfedrtert"
        ///},
        ///  "pagination": null,
        ///  "message": null
        /// }
        /// ```
        /// </response>
        /// <response code="400">
        /// ```
        /// {
        ///     "status": 400,
        ///     "data": null,
        ///     "pagination": null,
        ///     "message": "User không tồn tại"
        /// }
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
        [HttpGet("{charityId}")]
        public async Task<IActionResult> GetCharityById([FromRoute] Guid charityId)
        {
            CommonResponse commonResponse = new CommonResponse();
            string internalServerErrorMsg = _config[
                "ResponseMessages:UserMsg:InternalServerErrorMsg"
            ];
            try
            {
                commonResponse = await _charityService.GetCharityDetails(charityId);

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
        /// Get charity details
        /// </summary>
        /// <br/>
        /// <remarks>
        /// Body:
        /// - **charityId**: id
        /// </remarks>
        /// <br />
        /// <response code="200">
        /// ```
        ///      {
        ///  "status": 200,
        /// "data": [
        ///   {
        ///     "name": "Hội Chữ thập đỏ Việt Nam chi nhánh 1",
        ///     "status": "ACTIVE",
        ///    "email": "charityunit1@gmail.com",
        ///    "id": "a1f55c3d-bb57-ee11-9937-6045bd1b698d",
        ///   "phone": "0223456789",
        ///     "description": "Đơn vị chính trực thuộc Hội Chữ Thập Đỏ",
        ///   "image": "stgfedrtert"
        ///     "legalDocuments": "qwertyuiopasdfghjklzxcvbnm",
        ///      "location": "10.841158360492932,106.80987937802871",
        ///      "address": "Khu Công nghệ cao, Quận 9"
        ///   }
        ///  ],
        /// "pagination": null,
        /// "message": null
        /// }
        /// ```
        /// </response>
        /// <response code="400">
        /// ```
        /// {
        ///     "status": 400,
        ///     "data": null,
        ///     "pagination": null,
        ///     "message": "User không tồn tại"
        /// }
        /// ```
        ///</response>
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

        [Authorize]
        [HttpGet("admin/{charityId}/charity-units")]
        public async Task<IActionResult> GetCharityUnitOfCharity([FromRoute] Guid charityId)
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
                commonResponse = await _charityService.GetCharityUnitListByCharityId(
                    charityId,
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

        [HttpGet("{charityId}/charity-units")]
        public async Task<IActionResult> GetCharityUnitOfCharityForGuess([FromRoute] Guid charityId)
        {
            CommonResponse commonResponse = new CommonResponse();
            string internalServerErrorMsg = _config[
                "ResponseMessages:UserMsg:InternalServerErrorMsg"
            ];
            try
            {
                commonResponse = await _charityService.GetCharityUnitListByCharityIdForGuess(
                    charityId
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

        [Authorize(Roles = "SYSTEM_ADMIN")]
        [HttpDelete("{charityId}")]
        public async Task<IActionResult> DeleteCharity([FromRoute] Guid charityId)
        {
            CommonResponse commonResponse = new CommonResponse();
            string internalServerErrorMsg = _config[
                "ResponseMessages:UserMsg:InternalServerErrorMsg"
            ];
            try
            {
                commonResponse = await _charityService.DeleteCharity(charityId);

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
