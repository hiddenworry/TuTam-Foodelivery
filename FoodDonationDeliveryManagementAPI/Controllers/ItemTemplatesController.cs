using BusinessLogic.Services;
using BusinessLogic.Utils.FirebaseService;
using BusinessLogic.Utils.SecurityServices;
using DataAccess.EntityEnums;
using DataAccess.Models.Requests;
using DataAccess.Models.Responses;
using DataAccess.ModelsEnum;
using FoodDonationDeliveryManagementAPI.Security.Authourization.PolicyProvider;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodDonationDeliveryManagementAPI.Controllers
{
    [Route("item-templates")]
    [ApiController]
    public class ItemTemplatesController : ControllerBase
    {
        private readonly IItemTemplateService _itemTemplateService;
        private readonly ILogger<ActivitiesController> _logger;
        private readonly IConfiguration _config;
        private readonly IJwtService _jwtService;

        public ItemTemplatesController(
            IItemTemplateService itemTemplateService,
            ILogger<ActivitiesController> logger,
            IConfiguration config,
            IJwtService jwtService,
            IFirebaseStorageService firebaseStorageService
        )
        {
            _itemTemplateService = itemTemplateService;
            _logger = logger;
            _config = config;
            _jwtService = jwtService;
        }

        /// <summary>
        ///Use create item.
        ///   Permission: CREATE-ITEM - BRANCH_ADMIN
        /// </summary>
        /// <remarks>
        /// - **Name**: Item name (Cannot be empty). Must be between 8 and 60 characters.
        /// - **Status**: Item status (Cannot be empty). Value must be 0 or 1, corresponding to INACTIVE or ACTIVE.
        /// - **EstimatedExpirationDays**: Estimated expiration days (Cannot be empty). Value must be greater than or equal to 1.
        /// - **ItemUnitId**: Unit (Cannot be empty). Id of itemUnit
        /// - **ImageUrl**: Image URL (Cannot be empty).
        /// - **ItemcategoryId**: Id of the item category (Cannot be empty).
        /// **Attributes**: List of item attributes (Optional).
        /// **ItemTemplates**: List of item templates (Optional).
        /// </remarks>

        /// <response code="200">If success.</response>
        /// <response code="400">If validation error.</response>
        /// <response code="500">Internal server error.</response>
        [Authorize]
        [PermissionAuthorize("CREATE-ITEM")]
        [HttpPost]
        public async Task<IActionResult> CreateItem(ItemTemplateRequest request)
        {
            CommonResponse commonResponse = new CommonResponse();
            string internalServerErrorMsg = _config[
                "ResponseMessages:UserMsg:InternalServerErrorMsg"
            ];
            try
            {
                var token = HttpContext.Request.Headers["Authorization"]
                    .FirstOrDefault()
                    ?.Split(" ")
                    .Last();
                string? userSub = "";
                if (token != null)
                {
                    var decodedToken = _jwtService.GetClaimsPrincipal(token);

                    if (decodedToken != null)
                    {
                        userSub = decodedToken.Claims.FirstOrDefault(c => c.Type == "Id")?.Value;
                    }
                }
                commonResponse = await _itemTemplateService.CreateItemTemplate(
                    request,
                    Guid.Parse(userSub!)
                );
                switch (commonResponse.Status)
                {
                    case 200:
                        return Ok(commonResponse);
                    case 400:
                        return BadRequest(commonResponse);
                    default:
                        return StatusCode(500, commonResponse);
                }
            }
            catch
            {
                commonResponse.Status = 500;
                commonResponse.Message = internalServerErrorMsg;
                return StatusCode(500, commonResponse);
            }
        }

        /// <summary>
        ///Use create item.
        ///   Permission: UPDATE-ITEM - BRANCH_ADMIN
        /// </summary>
        /// <remarks>
        /// - itemId: Item ID (required - passed in the route).
        /// - **Name**: Item name (Cannot be empty). Must be between 8 and 60 characters.
        /// - **Status**: Item status (Cannot be empty). Value must be 0 or 1, corresponding to INACTIVE or ACTIVE.
        /// - **EstimatedExpirationDays**: Estimated expiration days (Cannot be empty). Value must be greater than or equal to 1.
        /// - **ItemUnit**: Unit (Cannot be empty). Value must be 0 or 1 or 2 or 3, corresponding to KG or L or PIECE or BOTTLE.
        /// - **ImageUrl**: Image URL (Cannot be empty).
        /// - **ItemcategoryId**: Item category ID (Cannot be empty).
        /// **Attributes**: List of item attributes (Optional).
        /// **ItemTemplates**: List of item templates (Optional).
        /// </remarks>
        /// <response code="200">If success.</response>
        /// <response code="400">If validation error.</response>
        /// <response code="500">Internal server error.</response>
        [Authorize]
        [PermissionAuthorize("UPDATE-ITEM")]
        [HttpPut("{itemId}")]
        public async Task<IActionResult> UpdateItem(
            ItemTemplateRequest request,
            [FromRoute] Guid itemId
        )
        {
            CommonResponse commonResponse = new CommonResponse();
            string internalServerErrorMsg = _config[
                "ResponseMessages:UserMsg:InternalServerErrorMsg"
            ];
            try
            {
                var token = HttpContext.Request.Headers["Authorization"]
                    .FirstOrDefault()
                    ?.Split(" ")
                    .Last();
                string? userSub = "";
                if (token != null)
                {
                    var decodedToken = _jwtService.GetClaimsPrincipal(token);

                    if (decodedToken != null)
                    {
                        userSub = decodedToken.Claims.FirstOrDefault(c => c.Type == "Id")?.Value;
                    }
                }
                commonResponse = await _itemTemplateService.UpdateItemTemplate(
                    request,
                    itemId,
                    Guid.Parse(userSub!)
                );
                switch (commonResponse.Status)
                {
                    case 200:
                        return Ok(commonResponse);
                    case 400:
                        return BadRequest(commonResponse);
                    default:
                        return StatusCode(500, commonResponse);
                }
            }
            catch
            {
                commonResponse.Status = 500;
                commonResponse.Message = internalServerErrorMsg;
                return StatusCode(500, commonResponse);
            }
        }

        /// <summary>
        /// Use filter item. Role branch admin, system admin, user
        /// </summary>
        /// <remarks>
        /// - **categoryType**: FOOD = 0 or NONFOOD = 1. (Null = search all)
        /// - **itemCategoryId**: Specific category, for example, meat, fish. Pass the Id from the dropdown list. (Null = search all)
        /// - **name**: Search by food name. (Null = search all)
        /// - **pageSize**: Page size.
        /// - **page**: Page number.
        /// - **sortType**: Sort By Name(0 ASC, 1 DES)
        /// </remarks>
        /// <response code="200">If successful.</response>
        /// <response code="400">If there's a validation error.</response>
        /// <response code="500">If there's an internal server error.</response>

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Filter(
            ItemCategoryType? categoryType,
            Guid? itemCategoryId,
            string? name,
            int? pageSize,
            int? page,
            SortType sortType
        )
        {
            string internalServerErrorMsg = _config[
                "ResponseMessages:UserMsg:InternalServerErrorMsg"
            ];
            CommonResponse commonResponse = new CommonResponse();
            try
            {
                var token = HttpContext.Request.Headers["Authorization"]
                    .FirstOrDefault()
                    ?.Split(" ")
                    .Last();
                string? userSub = "";
                if (token != null)
                {
                    var decodedToken = _jwtService.GetClaimsPrincipal(token);

                    if (decodedToken != null)
                    {
                        userSub = decodedToken.Claims.FirstOrDefault(c => c.Type == "Id")?.Value;
                    }
                }
                ItemFilterRequest itemFilterRequest = new ItemFilterRequest
                {
                    categoryType = categoryType,
                    itemCategoryId = itemCategoryId,
                    name = name
                };
                commonResponse = await _itemTemplateService.GetItemTemplatesAsync(
                    Guid.Parse(userSub!),
                    itemFilterRequest,
                    pageSize,
                    page,
                    sortType
                );

                switch (commonResponse.Status)
                {
                    case 200:
                        return Ok(commonResponse);
                    case 400:
                        return BadRequest(commonResponse);
                    default:
                        return StatusCode(500, commonResponse);
                }
            }
            catch
            {
                commonResponse.Status = 500;
                commonResponse.Message = internalServerErrorMsg;
                return StatusCode(500, commonResponse);
            }
        }

        /// <summary>
        /// Get item by id.
        /// </summary>
        /// <remarks>
        /// - **categoryType**: FOOD = 0 or NONFOOD = 1. (Null = search all)
        /// - **itemCategoryId**: Specific category, for example, meat, fish. Pass the Id from the dropdown list. (Null = search all)
        /// - **name**: Search by food name. (Null = search all)
        /// - **pageSize**: Page size.
        /// - **page**: Page number.
        /// </remarks>
        /// <response code="200">If successful.</response>
        /// <response code="400">If there's a validation error.</response>
        /// <response code="500">If there's an internal server error.</response>

        [HttpGet("{itemId}")]
        public async Task<IActionResult> GetItemById([FromRoute] Guid itemId)
        {
            string internalServerErrorMsg = _config[
                "ResponseMessages:UserMsg:InternalServerErrorMsg"
            ];
            CommonResponse commonResponse = new CommonResponse();
            try
            {
                var token = HttpContext.Request.Headers["Authorization"]
                    .FirstOrDefault()
                    ?.Split(" ")
                    .Last();
                string? userSub = "";
                if (token != null)
                {
                    var decodedToken = _jwtService.GetClaimsPrincipal(token);

                    if (decodedToken != null)
                    {
                        userSub = decodedToken.Claims.FirstOrDefault(c => c.Type == "Id")?.Value;
                    }
                }
                commonResponse = await _itemTemplateService.GetItemTemplateById(
                    itemId,
                    Guid.Parse(userSub!)
                );

                switch (commonResponse.Status)
                {
                    case 200:
                        return Ok(commonResponse);
                    case 400:
                        return BadRequest(commonResponse);
                    default:
                        return StatusCode(500, commonResponse);
                }
            }
            catch
            {
                commonResponse.Status = 500;
                commonResponse.Message = internalServerErrorMsg;
                return StatusCode(500, commonResponse);
            }
        }
    }
}
