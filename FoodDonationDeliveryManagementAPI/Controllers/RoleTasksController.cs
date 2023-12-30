using BusinessLogic.Services;
using Microsoft.AspNetCore.Mvc;

namespace FoodDonationDeliveryManagementAPI.Controllers
{
    [Route("role-tasks")]
    [ApiController]
    public class RoleTasksController : ControllerBase
    {
        private readonly IRoleTaskService _roleTaskService;
        private readonly ILogger<ActivitiesController> _logger;
        private readonly IConfiguration _config;

        public RoleTasksController(
            IRoleTaskService roleTaskService,
            ILogger<ActivitiesController> logger,
            IConfiguration config
        )
        {
            _roleTaskService = roleTaskService;
            _logger = logger;
            _config = config;
        }
    }
}
