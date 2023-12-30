using BusinessLogic.Services;
using DataAccess.Models.Responses;
using Microsoft.AspNetCore.Mvc;

namespace FoodDonationDeliveryManagementAPI.Controllers
{
    [Route("item-units")]
    [ApiController]
    public class ItemUnitsController : ControllerBase
    {
        private readonly IItemUnitService _itemUnitService;
        private readonly ILogger<ActivitiesController> _logger;
        private readonly IConfiguration _config;

        public ItemUnitsController(
            IItemUnitService itemUnitService,
            ILogger<ActivitiesController> logger,
            IConfiguration config
        )
        {
            _itemUnitService = itemUnitService;
            _logger = logger;
            _config = config;
        }

        /// <summary>
        /// Used to load the dropdown list of item unit.
        /// </summary>
        ///
        /// <remarks>
        /// </remarks>
        /// <response code="200">If successful.</response>
        /// <response code="400">If there's a validation error.</response>
        /// <response code="500">If there's an internal server error.</response>
        [HttpGet]
        public async Task<IActionResult> GetItemCategory()
        {
            CommonResponse commonResponse = new CommonResponse();
            string internalServerErrorMsg = _config[
                "ResponseMessages:UserMsg:InternalServerErrorMsg"
            ];
            try
            {
                commonResponse = await _itemUnitService.GetItemUnitListAsync();
                switch (commonResponse.Status)
                {
                    case 200:
                        return Ok(commonResponse);
                    default:
                        return StatusCode(500, commonResponse);
                }
            }
            catch (Exception ex)
            {
                commonResponse.Status = 500;
                commonResponse.Message = internalServerErrorMsg;
                _logger.LogError(ex, "An error occurred: {ErrorMessage}", ex.Message);
                return StatusCode(500, commonResponse);
            }
        }
    }
}
