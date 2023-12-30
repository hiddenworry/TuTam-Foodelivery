using BusinessLogic.Services;
using DataAccess.EntityEnums;
using DataAccess.Models.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodDonationDeliveryManagementAPI.Controllers
{
    [Route("item")]
    [ApiController]
    public class ItemController : ControllerBase
    {
        private readonly IItemService _itemService;
        private readonly ILogger<ItemController> _logger;
        private readonly IConfiguration _config;

        public ItemController(
            IItemService itemService,
            ILogger<ItemController> logger,
            IConfiguration config
        )
        {
            _itemService = itemService;
            _logger = logger;
            _config = config;
        }

        /// <summary>
        /// Get paging list of item template and search key word. Role: User
        /// </summary>
        /// <remarks>
        /// - searchKeyWord: string.
        /// - Take: int(Number of value that you want to take order by highest point to low)(Do có ít dữ liệu nên mảng này ko có nhiều giá trị nên lấy khoản 1-2 giá trị có số điểm cao nhất là hợp lý nhất).
        /// - itemCategoryType 0(Food), 1(NonFood)
        /// - itemCategoryId (Id của loại, thịt cá ....)
        /// </remarks>
        /// <response code="200">
        /// Get success, return list:
        /// ```
        ///{
        ///  "status": 200,
        ///     "data": [
        /// {
        ///   "itemTemplateId": "777bb661-da5d-ee11-9937-6045bd1b698d",
        ///   "name": "Cá Hồi",
        ///   "image": "Link",
        ///   "attributes": []
        /// },
        /// {
        ///   "itemTemplateId": "787bb661-da5d-ee11-9937-6045bd1b698d",
        ///   "name": "Cá Hồi",
        ///  "image": "Link",
        ///   "attributes": [
        ///     {
        ///       "attributeValue": "Phi Lê"
        ///    }
        ///  ],
        ///  "pagination": null
        ///  "message": null
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
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetItem(
            string? searchKeyWord,
            ItemCategoryType? itemCategoryType,
            Guid? itemCategoryId,
            int? page,
            int? pageSize
        )
        {
            string internalServerErrorMsg = _config[
                "ResponseMessages:UserMsg:InternalServerErrorMsg"
            ];
            CommonResponse commonResponse = new CommonResponse();
            try
            {
                commonResponse = await _itemService.SearchItemForUser(
                    searchKeyWord,
                    itemCategoryType,
                    itemCategoryId,
                    page,
                    pageSize
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
        /// Get item template by itemtemplateId. Role: User
        /// </summary>
        /// <remarks>
        /// - itemTemplateId: ItemTemplate ID (required - passed in the route).
        /// </remarks>
        /// <response code="200">
        /// Get branches success, return list:
        /// ```
        ///{
        ///  "status": 200,
        ///  "data": {
        ///    "id": "ABCD-4325-0000-0000-000000000000",
        ///    "name": null,
        ///    "attributeValues": [
        ///      "Việt Nam",
        ///      "Tam Thái Tử",
        ///      "500 ml"
        ///    ],
        ///    "note": null,
        ///    "estimatedExpirationDays": 0,
        ///    "maximumTransportVolume": 0
        ///  },
        ///  "pagination": null,
        ///  "message": null
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
        // [Authorize]
        [HttpGet("{itemId}")]
        public async Task<IActionResult> GetItemById(Guid itemId)
        {
            string internalServerErrorMsg = _config[
                "ResponseMessages:UserMsg:InternalServerErrorMsg"
            ];
            CommonResponse commonResponse = new CommonResponse();
            try
            {
                commonResponse = await _itemService.GetItemById(itemId);

                switch (commonResponse.Status)
                {
                    case 200:
                        return Ok(commonResponse);
                    case 400:
                        return BadRequest(commonResponse);
                    case 403:
                        return StatusCode(403, commonResponse);
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
