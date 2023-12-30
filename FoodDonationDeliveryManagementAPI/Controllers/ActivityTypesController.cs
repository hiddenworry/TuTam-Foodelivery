using BusinessLogic.Services;
using DataAccess.Models.Responses;
using Microsoft.AspNetCore.Mvc;

namespace FoodDonationDeliveryManagementAPI.Controllers
{
    [Route("activity-types")]
    [ApiController]
    public class ActivityTypesController : ControllerBase
    {
        private readonly IActivityTypeService _activityTypeService;
        private readonly ILogger<ActivitiesController> _logger;
        private readonly IConfiguration _config;

        public ActivityTypesController(
            IActivityTypeService activityTypeService,
            ILogger<ActivitiesController> logger,
            IConfiguration config
        )
        {
            _activityTypeService = activityTypeService;
            _logger = logger;
            _config = config;
        }

        /// <summary>
        /// Get all activity types.
        /// </summary>
        /// <response code="200">
        /// Get all activity types success, return a list:
        /// ```
        /// {
        ///  "status": 200,
        ///  "data": [
        ///    {
        ///      "id": "f312b640-2751-ee11-9f1b-c809a8bfd17d",
        ///      "name": "Quyên góp"
        ///    },
        ///    {
        ///      "id": "69db3c47-2751-ee11-9f1b-c809a8bfd17d",
        ///      "name": "Lao động tình nguyện"
        ///    }
        ///  ],
        ///  "pagination": null,
        ///  "message": "Lấy danh sách cách loại hoạt động thành công."
        ///}
        /// ```
        /// </response>
        /// <response code="500">
        /// Internal server error, return message:
        /// ```
        /// {
        ///     "status": 500,
        ///     "data": null,
        ///     "pagination": null,
        ///     "message": "Lỗi hệ thống."
        /// }
        /// ```
        /// </response>
        [HttpGet]
        public async Task<IActionResult> GetAllActivityTypes()
        {
            string internalServerErrorMsg = _config[
                "ResponseMessages:CommonMsg:InternalServerErrorMsg"
            ];
            try
            {
                CommonResponse commonResponse =
                    await _activityTypeService.GetAllActivityTypesAsync();
                return Ok(commonResponse);
            }
            catch
            {
                return StatusCode(
                    500,
                    new CommonResponse { Status = 500, Message = internalServerErrorMsg }
                );
            }
        }
    }
}
