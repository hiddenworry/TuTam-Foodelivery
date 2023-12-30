using BusinessLogic.Services;
using DataAccess.Models.Requests;
using DataAccess.Models.Responses;
using FoodDonationDeliveryManagementAPI.Security.Authourization.PolicyProvider;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodDonationDeliveryManagementAPI.Controllers
{
    [Route("item-categories")]
    [ApiController]
    public class ItemCategoriesController : ControllerBase
    {
        private readonly IItemCategoryService _itemCategoryService;
        private readonly ILogger<ActivitiesController> _logger;
        private readonly IConfiguration _config;

        public ItemCategoriesController(
            IItemCategoryService itemCategoryService,
            ILogger<ActivitiesController> logger,
            IConfiguration config
        )
        {
            _itemCategoryService = itemCategoryService;
            _logger = logger;
            _config = config;
        }

        /// <summary>
        /// Use to create item category. Create categories like meat, fish, canned goods. Permission: CREATE-ITEMCATEGORY - BRANCH_ADMIN
        /// </summary>
        ///
        /// <remarks>
        /// - **Name**: Category name (Cannot be empty). (String)
        /// - **Type**: FOOD = 0 , NON-FOOD = 1 (Cannot be empty). (File)
        /// </remarks>
        /// <response code="200">If successful.</response>
        /// <response code="400">If there's a validation error.</response>
        /// <response code="500">If there's an internal server error.</response>

        [Authorize]
        [PermissionAuthorize("CREATE-ITEMCATEGORY")]
        [HttpPost]
        public async Task<IActionResult> CreateItemCategory([FromBody] ItemsCategoryRequest request)
        {
            CommonResponse commonResponse = new CommonResponse();
            string internalServerErrorMsg = _config[
                "ResponseMessages:UserMsg:InternalServerErrorMsg"
            ];
            try
            {
                commonResponse = await _itemCategoryService.CreateItemsCategoryAsync(request);
                switch (commonResponse.Status)
                {
                    case 200:
                        return Ok(commonResponse);
                    default:
                        return StatusCode(500, commonResponse);
                }
            }
            catch (Exception ex)
            {
                commonResponse.Status = 500;
                commonResponse.Message = internalServerErrorMsg;
                _logger.LogError(ex, "An error occurred: {ErrorMessage}", ex.Message);
                return StatusCode(500, commonResponse);
            }
        }

        /// <summary>
        /// Used to load the dropdown list of item categories.
        /// </summary>
        ///
        /// <remarks>
        /// </remarks>
        /// <response code="200">If successful.</response>
        /// <response code="400">If there's a validation error.</response>
        /// <response code="500">If there's an internal server error.</response>
        [HttpGet]
        public async Task<IActionResult> GetItemCategory()
        {
            CommonResponse commonResponse = new CommonResponse();
            string internalServerErrorMsg = _config[
                "ResponseMessages:UserMsg:InternalServerErrorMsg"
            ];
            try
            {
                commonResponse = await _itemCategoryService.GetItemCategoriesListAsync();
                switch (commonResponse.Status)
                {
                    case 200:
                        return Ok(commonResponse);
                    default:
                        return StatusCode(500, commonResponse);
                }
            }
            catch (Exception ex)
            {
                commonResponse.Status = 500;
                commonResponse.Message = internalServerErrorMsg;
                _logger.LogError(ex, "An error occurred: {ErrorMessage}", ex.Message);
                return StatusCode(500, commonResponse);
            }
        }
    }
}
