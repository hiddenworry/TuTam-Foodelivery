using BusinessLogic.Services;
using BusinessLogic.Utils.SecurityServices;
using DataAccess.EntityEnums;
using DataAccess.Models.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodDonationDeliveryManagementAPI.Controllers
{
    [Route("donated-items")]
    [ApiController]
    public class DonatedItemsController : ControllerBase
    {
        private readonly IDonatedItemService _donatedItemService;
        private readonly ILogger<ActivitiesController> _logger;
        private readonly IConfiguration _config;
        private readonly IJwtService _jwtService;

        public DonatedItemsController(
            IDonatedItemService donatedItemService,
            ILogger<ActivitiesController> logger,
            IConfiguration config,
            IJwtService jwtService
        )
        {
            _donatedItemService = donatedItemService;
            _logger = logger;
            _config = config;
            _jwtService = jwtService;
        }

        [Authorize]
        [HttpGet("")]
        public async Task<IActionResult> SendCreateCharityUnitRequest(
            int? page,
            int? pageSize,
            string? keyWord,
            DonatedItemStatus? status,
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
                commonResponse = await _donatedItemService.GetDonatedItemAsync(
                    page,
                    pageSize,
                    Guid.Parse(userSub!),
                    keyWord,
                    status,
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
    }
}
