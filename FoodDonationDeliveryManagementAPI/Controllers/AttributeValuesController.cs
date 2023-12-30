using BusinessLogic.Services;
using Microsoft.AspNetCore.Mvc;

namespace FoodDonationDeliveryManagementAPI.Controllers
{
    [Route("attribute-values")]
    [ApiController]
    public class AttributeValuesController : ControllerBase
    {
        private readonly IAttributeValueService _attributeValueService;
        private readonly ILogger<ActivitiesController> _logger;
        private readonly IConfiguration _config;

        public AttributeValuesController(
            IAttributeValueService attributeValueService,
            ILogger<ActivitiesController> logger,
            IConfiguration config
        )
        {
            _attributeValueService = attributeValueService;
            _logger = logger;
            _config = config;
        }
    }
}
