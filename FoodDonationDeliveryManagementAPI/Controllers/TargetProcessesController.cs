using BusinessLogic.Services;
using Microsoft.AspNetCore.Mvc;

namespace FoodDonationDeliveryManagementAPI.Controllers
{
    [Route("target-processes")]
    [ApiController]
    public class TargetProcessesController : ControllerBase
    {
        private readonly ITargetProcessService _targetProcessService;
        private readonly ILogger<ActivitiesController> _logger;
        private readonly IConfiguration _config;

        public TargetProcessesController(
            ITargetProcessService targetProcessService,
            ILogger<ActivitiesController> logger,
            IConfiguration config
        )
        {
            _targetProcessService = targetProcessService;
            _logger = logger;
            _config = config;
        }
    }
}
