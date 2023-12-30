using BusinessLogic.Services;
using Microsoft.AspNetCore.Mvc;

namespace FoodDonationDeliveryManagementAPI.Controllers
{
    [Route("role-members")]
    [ApiController]
    public class RoleMembersController : ControllerBase
    {
        private readonly IRoleMemberService _roleMemberService;
        private readonly ILogger<ActivitiesController> _logger;
        private readonly IConfiguration _config;

        public RoleMembersController(
            IRoleMemberService roleMemberService,
            ILogger<ActivitiesController> logger,
            IConfiguration config
        )
        {
            _roleMemberService = roleMemberService;
            _logger = logger;
            _config = config;
        }
    }
}
