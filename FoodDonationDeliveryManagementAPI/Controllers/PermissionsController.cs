using BusinessLogic.Services;
using Microsoft.AspNetCore.Mvc;

namespace FoodDonationDeliveryManagementAPI.Controllers
{
    [Route("permissions")]
    [ApiController]
    public class PermissionsController : ControllerBase
    {
        private readonly IPermissionService _permissionService;
        private readonly ILogger<ActivitiesController> _logger;
        private readonly IConfiguration _config;

        public PermissionsController(
            IPermissionService permissionService,
            ILogger<ActivitiesController> logger,
            IConfiguration config
        )
        {
            _permissionService = permissionService;
            _logger = logger;
            _config = config;
        }
    }
}
