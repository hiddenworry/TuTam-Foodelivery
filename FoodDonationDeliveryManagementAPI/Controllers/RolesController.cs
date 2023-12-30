using BusinessLogic.Services;
using DataAccess.Models.Responses;
using Microsoft.AspNetCore.Mvc;

namespace FoodDonationDeliveryManagementAPI.Controllers
{
    [Route("roles")]
    [ApiController]
    public class RolesController : ControllerBase
    {
        private readonly IRoleService _roleService;
        private readonly ILogger<ActivitiesController> _logger;
        private readonly IConfiguration _config;

        public RolesController(
            IRoleService roleService,
            ILogger<ActivitiesController> logger,
            IConfiguration config
        )
        {
            _roleService = roleService;
            _logger = logger;
            _config = config;
        }

        /// <summary>
        /// Get All Role
        /// </summary>
        /// <remarks>
        /// This api help system admin to read all permission of specific role.
        /// Paramters: No
        /// Role : System Admin
        /// </remarks>
        /// <response code="200">Returns list of role.</response>
        /// <response code="500">Internal server error.</response>
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            string errorMsg = _config["ResponseMessages:CommonMsg:InternalServerErrorMsg"];
            CommonResponse commonResponse = new CommonResponse();
            try
            {
                commonResponse = await _roleService.GetAllRolesAsync();

                switch (commonResponse.Status)
                {
                    case 200:
                        return Ok(commonResponse);
                    default:
                        return StatusCode(500, commonResponse);
                }
            }
            catch
            {
                commonResponse.Message = errorMsg;
                return StatusCode(500, commonResponse);
            }
        }
    }
}
