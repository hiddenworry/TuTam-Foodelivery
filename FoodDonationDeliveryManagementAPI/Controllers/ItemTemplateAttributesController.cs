using BusinessLogic.Services;
using Microsoft.AspNetCore.Mvc;

namespace FoodDonationDeliveryManagementAPI.Controllers
{
    [Route("item-template-attributes")]
    [ApiController]
    public class ItemTemplateAttributesController : Controller
    {
        private readonly IItemTemplateAttributeService _itemTemplateAttributeService;
        private readonly ILogger<ActivitiesController> _logger;
        private readonly IConfiguration _config;

        public ItemTemplateAttributesController(
            IItemTemplateAttributeService _itemTemplateAttributeService,
            ILogger<ActivitiesController> logger,
            IConfiguration config
        )
        {
            this._itemTemplateAttributeService = _itemTemplateAttributeService;
            _logger = logger;
            _config = config;
        }
    }
}
