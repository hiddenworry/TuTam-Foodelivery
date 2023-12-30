using BusinessLogic.Services;
using BusinessLogic.Utils.SecurityServices;
using DataAccess.Models.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodDonationDeliveryManagementAPI.Controllers
{
    [Route("delivery-items")]
    [ApiController]
    public class DeliveryItemsController : ControllerBase
    {
        private readonly IDeliveryItemService _deliveryItemService;
        private readonly ILogger<ActivitiesController> _logger;
        private readonly IConfiguration _config;
        private readonly IJwtService _jwtService;

        public DeliveryItemsController(
            IDeliveryItemService deliveryItemService,
            ILogger<ActivitiesController> logger,
            IConfiguration config,
            IJwtService jwtService
        )
        {
            _deliveryItemService = deliveryItemService;
            _logger = logger;
            _config = config;
            _jwtService = jwtService;
        }

        /// <summary>
        /// CHARITY get their delivery request
        /// </summary>
        /// <remarks>
        /// Role: CHARITY
        /// <br />
        /// </remarks>
        [HttpGet]
        [Authorize(Roles = "CHARITY")]
        [Route("charity-unit")]
        public async Task<IActionResult> GetDeliveryRequestForCharityUnit(int? page, int? pageSize)
        {
            try
            {
                string jwtToken = Request.Headers["Authorization"]
                    .FirstOrDefault()
                    ?.Split(" ")
                    .Last()!;
                Guid userId = _jwtService.GetUserIdByJwtToken(jwtToken);
                CommonResponse commonResponse =
                    await _deliveryItemService.GetDeliveredItemByCharityUnit(
                        userId,
                        page,
                        pageSize
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
                    new CommonResponse
                    {
                        Status = 500,
                        Message = _config["ResponseMessages:CommonMsg:InternalServerErrorMsg"]
                    }
                );
            }
        }
    }
}
