using BusinessLogic.Services;
using Microsoft.AspNetCore.Mvc;

namespace FoodDonationDeliveryManagementAPI.Controllers
{
    [Route("activity-type-components")]
    [ApiController]
    public class ActivityTypeComponentsController : Controller
    {
        private readonly IActivityTypeComponentService _activityTypeComponentService;
        private readonly ILogger<ActivitiesController> _logger;
        private readonly IConfiguration _config;

        public ActivityTypeComponentsController(
            IActivityTypeComponentService activityTypeComponentService,
            ILogger<ActivitiesController> logger,
            IConfiguration config
        )
        {
            _activityTypeComponentService = activityTypeComponentService;
            _logger = logger;
            _config = config;
        }
    }
}
