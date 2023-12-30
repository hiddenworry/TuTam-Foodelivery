using BusinessLogic.Services;
using Microsoft.AspNetCore.Mvc;

namespace FoodDonationDeliveryManagementAPI.Controllers
{
    [Route("activity-branches")]
    [ApiController]
    public class ActivityBranchesController : ControllerBase
    {
        private readonly IActivityBranchService _activityBranchService;
        private readonly ILogger<ActivitiesController> _logger;
        private readonly IConfiguration _config;

        public ActivityBranchesController(
            IActivityBranchService activityBranchService,
            ILogger<ActivitiesController> logger,
            IConfiguration config
        )
        {
            _activityBranchService = activityBranchService;
            _logger = logger;
            _config = config;
        }
    }
}
