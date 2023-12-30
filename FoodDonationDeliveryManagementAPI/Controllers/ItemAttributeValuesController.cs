using BusinessLogic.Services;
using Microsoft.AspNetCore.Mvc;

namespace FoodDonationDeliveryManagementAPI.Controllers
{
    [Route("item-attribute-values")]
    [ApiController]
    public class ItemAttributeValuesController : ControllerBase
    {
        private readonly IItemAttributeValueService _itemAttributeValueService;
        private readonly ILogger<ActivitiesController> _logger;
        private readonly IConfiguration _config;

        public ItemAttributeValuesController(
            IItemAttributeValueService itemAttributeValueService,
            ILogger<ActivitiesController> logger,
            IConfiguration config
        )
        {
            _itemAttributeValueService = itemAttributeValueService;
            _logger = logger;
            _config = config;
        }
    }
}
