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
    [Route("charity-units")]
    [ApiController]
    public class CharityUnitsController : ControllerBase
    {
        private readonly ICharityUnitService _charityUnitService;
        private readonly ILogger<ActivitiesController> _logger;
        private readonly IConfiguration _config;
        private readonly IJwtService _jwtService;

        public CharityUnitsController(
            ICharityUnitService charityUnitService,
            ILogger<ActivitiesController> logger,
            IConfiguration config,
            IJwtService jwtService
        )
        {
            _charityUnitService = charityUnitService;
            _logger = logger;
            _config = config;
            _jwtService = jwtService;
        }

        /// <summary>
        /// Get charity unit details
        /// </summary>
        /// <br/>
        /// <remarks>
        /// - **charityUnitId**: id
        /// </remarks>
        /// <br />
        /// <response code="200">
        /// ```
        ///     {
        /// "status": 200,
        /// "data": {
        ///  "id": "c5235294-bb57-ee11-9937-6045bd1b698d",
        ///   "name": "Hội Chữ thập đỏ Việt Nam chi nhánh 1",
        ///   "status": "ACTIVE",
        ///  "email": "charityunit1@gmail.com",
        ///    "charityId": "a1f55c3d-bb57-ee11-9937-6045bd1b698d",
        ///   "phone": "0223456789",
        ///    "description": "Đơn vị chính trực thuộc Hội Chữ Thập Đỏ",
        ///   "image": "fsdfsdfs",
        ///    "legalDocuments": "qwertyuiopasdfghjklzxcvbnm",
        ///    "location": "10.841158360492932,106.80987937802871",
        ///   "address": "Khu Công nghệ cao, Quận 9",
        ///   "numberOfPost": 0
        ///  },
        ///  "pagination": null,
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
        //  [Authorize]
        [HttpGet("{charityUnitId}")]
        public async Task<IActionResult> GetCharityUnitById([FromRoute] Guid charityUnitId)
        {
            CommonResponse commonResponse = new CommonResponse();
            string internalServerErrorMsg = _config[
                "ResponseMessages:UserMsg:InternalServerErrorMsg"
            ];
            try
            {
                commonResponse = await _charityUnitService.GetCharityUnitDetails(charityUnitId);

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

        [Authorize(Roles = "CHARITY")]
        [HttpPost("")]
        public async Task<IActionResult> SendCreateCharityUnitRequest(
            [FromForm] CharityUnitCreatingRequest request
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
                commonResponse = await _charityUnitService.CreateCharityUnit(
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

        [Authorize(Roles = "CHARITY")]
        [HttpPut("")]
        public async Task<IActionResult> SendUpdateCharityUnitRequest(
            [FromForm] CharityUnitUpdatingRequest request
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
                commonResponse = await _charityUnitService.UpdateCharityUnit(
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

        [PermissionAuthorize("UPDATE-CHARITY")]
        [HttpPut("admin/{charityUnitId}")]
        public async Task<IActionResult> ConfirmCharityUnit(
            [FromRoute] Guid charityUnitId,
            [FromBody] ConfirmCharityUnitRequest request,
            bool update = true
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
                if (update == true)
                {
                    commonResponse = await _charityUnitService.ConfirmUpdateCharityUnit(
                        charityUnitId,
                        request,
                        Guid.Parse(userSub!)
                    );
                }
                else
                {
                    commonResponse = await _charityUnitService.ConfirmCharityUnit(
                        charityUnitId,
                        request,
                        Guid.Parse(userSub!)
                    );
                }

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

        ///// <summary>
        ///// Get charity unit update history by userId of charity account
        ///// </summary>
        ///// <br/>
        ///// <remarks>
        ///// - **userId**: id
        ///// </remarks>
        ///// <br />
        ///// <response code="200">
        ///// ```
        /////     {
        ///// "status": 200,
        ///// "data": {
        /////  "id": "c5235294-bb57-ee11-9937-6045bd1b698d",
        /////   "name": "Hội Chữ thập đỏ Việt Nam chi nhánh 1",
        /////   "status": "ACTIVE",
        /////  "email": "charityunit1@gmail.com",
        /////    "charityId": "a1f55c3d-bb57-ee11-9937-6045bd1b698d",
        /////   "phone": "0223456789",
        /////    "description": "Đơn vị chính trực thuộc Hội Chữ Thập Đỏ",
        /////   "image": "fsdfsdfs",
        /////    "legalDocuments": "qwertyuiopasdfghjklzxcvbnm",
        /////    "location": "10.841158360492932,106.80987937802871",
        /////   "address": "Khu Công nghệ cao, Quận 9",
        /////   "numberOfPost": 0
        /////  },
        /////  "pagination": null,
        ///// "message": null
        ///// }
        ///// ```
        ///// </response>
        ///// <response code="400">
        ///// ```
        ///// {
        /////     "status": 400,
        /////     "data": null,
        /////     "pagination": null,
        /////     "message": "User không tồn tại"
        ///// }
        ///// ```
        ///// </response>
        ///// <response code="500">
        ///// Internal server error, return message:
        ///// ```
        ///// {
        /////     "status": 500,
        /////     "data": null,
        /////     "pagination": null,
        /////     "message": "Lỗi hệ thống."
        ///// }
        ///// ```
        ///// </response>
        //[PermissionAuthorize("UPDATE-CHARITY")]
        //[HttpGet("/{userId}")]
        //public async Task<IActionResult> GetLatestCharityUnitUpdateVersion(Guid userId)
        //{
        //    CommonResponse commonResponse = new CommonResponse();
        //    string internalServerErrorMsg = _config[
        //        "ResponseMessages:UserMsg:InternalServerErrorMsg"
        //    ];
        //    try
        //    {
        //        var token = HttpContext.Request.Headers["Authorization"]
        //            .FirstOrDefault()
        //            ?.Split(" ")
        //            .Last();
        //        string? userSub = "";
        //        if (token != null)
        //        {
        //            var decodedToken = _jwtService.GetClaimsPrincipal(token);

        //            if (decodedToken != null)
        //            {
        //                userSub = decodedToken.Claims.FirstOrDefault(c => c.Type == "Id")?.Value;
        //            }
        //        }
        //        commonResponse = await _charityUnitService.GetLatestCharityUnitUpdateVersion(
        //            userId
        //        );

        //        switch (commonResponse.Status)
        //        {
        //            case 200:
        //                return Ok(commonResponse);
        //            case 400:
        //                return BadRequest(commonResponse);
        //            default:
        //                return StatusCode(500, commonResponse);
        //        }
        //    }
        //    catch
        //    {
        //        commonResponse.Message = internalServerErrorMsg;
        //        commonResponse.Status = 500;
        //        return StatusCode(500, commonResponse);
        //    }
        //}

        /// <summary>
        /// Get charities unit
        /// </summary>
        /// <param name="page">Default 1</param>
        /// <param name="pageSize">Default 10</param>
        /// <param name="sortType"></param>
        /// <param name="keyWord"></param>
        /// <param name="charityId"></param>
        /// <param name="charityStatus">UNVERIFIED(0), ACTIVE(1) - leave nul to get all status</param>
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

        [HttpGet]
        public async Task<IActionResult> GetListCharites(
            int? page,
            int? pageSize,
            SortType? sortType,
            string? keyWord,
            Guid? charityId,
            int? charityStatus
        )
        {
            CommonResponse commonResponse = new CommonResponse();
            string internalServerErrorMsg = _config[
                "ResponseMessages:UserMsg:InternalServerErrorMsg"
            ];
            try
            {
                CharityUnitStatus? charityUnitStatus = null;
                switch (charityStatus)
                {
                    case 0:
                        charityUnitStatus = CharityUnitStatus.DELETED;
                        break;
                    case 1:
                        charityUnitStatus = CharityUnitStatus.ACTIVE;
                        break;

                    default:
                        charityUnitStatus = null;
                        break;
                }
                commonResponse = await _charityUnitService.GetCharityUnit(
                    keyWord,
                    charityUnitStatus,
                    charityId,
                    page,
                    pageSize,
                    sortType
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
        /// Delete charities unit
        /// </summary>
        [Authorize(Roles = "SYSTEM_ADMIN")]
        [HttpDelete("{charityUnitId}")]
        public async Task<IActionResult> DeleteCharityUnit([FromRoute] Guid charityUnitId)
        {
            CommonResponse commonResponse = new CommonResponse();
            string internalServerErrorMsg = _config[
                "ResponseMessages:UserMsg:InternalServerErrorMsg"
            ];
            try
            {
                commonResponse = await _charityUnitService.DeleteCharityUnit(charityUnitId);

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

        [Authorize]
        [HttpGet("users/{userId}")]
        public async Task<IActionResult> GetCharityUnitDetailsByUserIdAndStatusForAdmin(
            [FromRoute] Guid userId,
            CharityUnitStatus status
        )
        {
            CommonResponse commonResponse = new CommonResponse();
            string internalServerErrorMsg = _config[
                "ResponseMessages:UserMsg:InternalServerErrorMsg"
            ];
            try
            {
                commonResponse =
                    await _charityUnitService.GetCharityUnitDetailsByUserIdAndStatusForAdmin(
                        userId,
                        status
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
