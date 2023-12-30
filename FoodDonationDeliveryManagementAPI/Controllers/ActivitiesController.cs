using BusinessLogic.Services;
using BusinessLogic.Utils.OpenRouteService;
using BusinessLogic.Utils.SecurityServices;
using DataAccess.EntityEnums;
using DataAccess.Models.Requests;
using DataAccess.Models.Requests.Enum;
using DataAccess.Models.Responses;
using DataAccess.ModelsEnum;
using FoodDonationDeliveryManagementAPI.Security.Authourization.PolicyProvider;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodDonationDeliveryManagementAPI.Controllers
{
    [Route("activities")]
    [ApiController]
    public class ActivitiesController : ControllerBase
    {
        private readonly IActivityService _activityService;
        private readonly ILogger<ActivitiesController> _logger;
        private readonly IConfiguration _config;
        private readonly IJwtService _jwtService;
        private readonly IOpenRouteService _openRouteService;

        public ActivitiesController(
            IActivityService activityService,
            ILogger<ActivitiesController> logger,
            IConfiguration config,
            IJwtService jwtService,
            IOpenRouteService openRouteService
        )
        {
            _activityService = activityService;
            _logger = logger;
            _config = config;
            _jwtService = jwtService;
            _openRouteService = openRouteService;
        }

        /// <summary>
        /// Create activity.
        /// </summary>
        /// <remarks>
        /// Role: BRANCH_ADMIN
        /// <br/>
        /// Permission: CREATE-ACTIVITY
        /// <br/>
        /// Body:
        /// - **name**: activity's name (not null, 10 &lt;= length &lt;= 150).
        /// - **address**: activity's address (10 &lt;= length &lt;= 50).
        /// - **location**: activity's location (length = 2).
        /// - **estimatedStartDate**: estimated date to start the activity (not null, now &lt;= value &lt;= now + 3 months).
        /// - **estimatedEndDate**: estimated date to end the activity (not null, estimatedStartDate &lt; value &lt;= estimatedStartDate + 3 months).
        /// - **deliveringDate**: date to start delivering it's donated items (estimatedStartDate &lt;= value &lt;= estimatedEndDate).
        /// - **description**: activity's description (not null, 20 &lt;= length &lt;= 2000).
        /// - **images**: activity's images (not null, not empty).
        /// - **scope**: 0 default (PUBLIC) or 1 (INTERNAL).
        /// - **activityTypeIds**: Ids of activity types (not null, not empty).
        /// - **branchIds**: Ids of joined branches (only SYSTEM_ADMIN can use).
        /// </remarks>
        /// <response code="200">
        /// Create activity success, return created activity's Id:
        /// ```
        /// {
        ///     "status": 200,
        ///     "data": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
        ///     "pagination": null,
        ///     "message": "Tạo hoạt động thành công."
        /// }
        /// ```
        /// </response>
        /// <response code="400">
        /// Bad request in many ways, return a message of what's wrong:
        /// ```
        /// {
        ///     "status": 400,
        ///     "data": null,
        ///     "pagination": null,
        ///     "message": "Không tìm thấy loại hoạt động."
        /// }
        /// ```
        /// </response>
        /// <response code="403">
        /// User is not allow to perform this function, return a message of what's wrong:
        /// ```
        /// {
        ///     "status": 403,
        ///     "data": null,
        ///     "pagination": null,
        ///     "message": "Chỉ quản trị hệ thống được phép tạo hoạt động và chỉ định các chi nhánh tham gia."
        /// }
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
        [HttpPost]
        [PermissionAuthorize("CREATE-ACTIVITY")]
        public async Task<IActionResult> CreateActivity(
            [FromForm] ActivityCreatingRequest activityCreatingRequest
        )
        {
            string internalServerErrorMsg = _config[
                "ResponseMessages:CommonMsg:InternalServerErrorMsg"
            ];
            try
            {
                string jwtToken = Request.Headers["Authorization"]
                    .FirstOrDefault()
                    ?.Split(" ")
                    .Last()!;
                Guid userId = _jwtService.GetUserIdByJwtToken(jwtToken);
                string userRoleName = _jwtService.GetRoleNameByJwtToken(jwtToken);
                CommonResponse commonResponse = await _activityService.CreateActivityAsync(
                    activityCreatingRequest,
                    userId,
                    userRoleName
                );
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "An exception occurred in API CreateActivity.");
                return StatusCode(
                    500,
                    new CommonResponse { Status = 500, Message = internalServerErrorMsg }
                );
            }
        }

        /// <summary>
        /// Get paging list of activities by search and filter.
        /// </summary>
        /// <param name="name">Searching contain name</param>
        /// <param name="status">NOT_STARTED(0), STARTED(1), ENDED(2), INACTIVE(3) - leave null to get all statuses - only SYSTEM_ADMIN and BRANCH_ADMIN can search status INACTIVE</param>
        /// <param name="scope">PUBLIC(0), INTERNAL(1) - only SYSTEM_ADMIN and BRANCH_ADMIN can search scope INTERNAL</param>
        /// <param name="activityTypeIds">Searching must contain all passed activity type Ids</param>
        /// <param name="startDate">Start before activity's end date - if activity's end date is null, apply for activity's estimated end date</param>
        /// <param name="endDate">Start after activity's start date - if activity's start date is null, apply for activity's estimated start date</param>
        /// <param name="isJoined">Search joined activity of the account by passing TRUE - only authenticated user can use, except SYSTEM_ADMIN and CHARITY - CONTRIBUTOR will get their own joined activities, BRANCH_ADMIN will get their branch's joined activities</param>
        /// <param name="userId">Search user's joined activities by their Id - only SYSTEM_ADMIN and BRANCH_ADMIN can use</param>
        /// <param name="branchId">Search branch's joined activities by their Id</param>
        /// <param name="address">Searching contain address - if activity's address is null, apply for joined branches' address</param>
        /// <param name="pageSize">Default 10</param>
        /// <param name="page">Default 1</param>
        /// <param name="orderBy">Order by a field, ex: Name - leave null to ignore order</param>
        /// <param name="sortType">ASC(0), DESC(1) - leave null to ignore sorting</param>
        /// <response code="200">
        /// Get activities success, return list:
        /// <br />
        /// User is unauthenticated, CONTRIBUTOR or CHARITY:
        /// ```
        /// {
        ///     "status": 200,
        ///     "data": [
        ///         {
        ///             "id": "4a0187a0-8253-ee11-9f1e-005056c00008",
        ///             "name": "Demo test 01",
        ///             "startDate": null,
        ///             "endDate": null,
        ///             "estimatedStartDate": "2023-09-15T17:00:00",
        ///             "estimatedEndDate": "2023-09-17T17:00:00",
        ///             "status": "NOT_STARTED",
        ///             "description": "stringstringstringstringstringstringstring",
        ///             "images": [
        ///                 "link"
        ///             ],
        ///             "scope": "PUBLIC",
        ///             "isNearby": false,
        ///             "activityTypeComponents": [
        ///                 "Quyên góp",
        ///                 "Lao động tình nguyện"
        ///             ],
        ///             "targetProcessResponses": []
        ///         },
        ///         {
        ///             "id": "3db45abe-2751-ee11-9f1b-c809a8bfd17d",
        ///             "name": "Hoạt động Thủ Đức 1",
        ///             "startDate": null,
        ///             "endDate": null,
        ///             "estimatedStartDate": "2023-09-13T04:39:26.176",
        ///             "estimatedEndDate": "2023-09-14T04:39:26.176",
        ///             "status": "NOT_STARTED",
        ///             "description": "stringstringstringstringstringstringstring",
        ///             "images": [
        ///                 "link"
        ///             ],
        ///             "scope": "PUBLIC",
        ///             "isNearby": false,
        ///             "activityTypeComponents": [
        ///                 "Lao động tình nguyện",
        ///                 "Hỗ trợ phát đồ"
        ///             ],
        ///             "targetProcessResponses": []
        ///         }
        ///     ],
        ///     "pagination": {
        ///         "currentPage": 1,
        ///         "pageSize": 10,
        ///         "total": 2
        ///     },
        ///     "message": "Lấy danh sách hoạt động thành công."
        /// }
        /// ```
        /// User is BRANCH_ADMIN or SYSTEM_ADMIN:
        /// ```
        /// {
        ///     "status": 200,
        ///     "data": [
        ///         {
        ///             "id": "4a0187a0-8253-ee11-9f1e-005056c00008",
        ///             "name": "Demo test 01",
        ///             "createdDate": "2023-09-15T04:45:00.8061431",
        ///             "startDate": null,
        ///             "endDate": null,
        ///             "status": "NOT_STARTED",
        ///             "scope": "PUBLIC",
        ///             "activityTypeComponents": [
        ///                 "Quyên góp",
        ///                 "Lao động tình nguyện"
        ///             ]
        ///         },
        ///         {
        ///             "id": "3db45abe-2751-ee11-9f1b-c809a8bfd17d",
        ///             "name": "Hoạt động Thủ Đức 1",
        ///             "createdDate": "2023-09-12T04:49:24.4290322",
        ///             "startDate": null,
        ///             "endDate": null,
        ///             "status": "NOT_STARTED",
        ///             "scope": "PUBLIC",
        ///             "activityTypeComponents": [
        ///                 "Lao động tình nguyện",
        ///                 "Hỗ trợ phát đồ"
        ///             ]
        ///         }
        ///     ],
        ///     "pagination": {
        ///         "currentPage": 1,
        ///         "pageSize": 10,
        ///         "total": 2
        ///     },
        ///     "message": "Lấy danh sách hoạt động thành công."
        /// }
        /// ```
        /// </response>
        /// <response code="400">
        /// Bad request in many ways, return a message of what's wrong:
        /// ```
        /// {
        ///     "status": 400,
        ///     "data": null,
        ///     "pagination": null,
        ///     "message": "Không tìm thấy trường dữ liệu cần sắp xếp."
        /// }
        /// ```
        /// </response>
        /// <response code="403">
        /// User is not allow to perform this function, return a message of what's wrong:
        /// ```
        /// {
        ///     "status": 403,
        ///     "data": null,
        ///     "pagination": null,
        ///     "message": "Bạn không có quyền thực hiện hành động này."
        /// }
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
        public async Task<IActionResult> GetActivities(
            string? name,
            ActivityStatus? status,
            ActivityScope? scope,
            [FromQuery] List<Guid>? activityTypeIds,
            DateTime? startDate,
            DateTime? endDate,
            bool? isJoined,
            Guid? userId,
            Guid? branchId,
            string? address,
            int? pageSize,
            int? page,
            string? orderBy,
            SortType? sortType
        )
        {
            string internalServerErrorMsg = _config[
                "ResponseMessages:CommonMsg:InternalServerErrorMsg"
            ];
            try
            {
                CommonResponse commonResponse = new CommonResponse();
                string? jwtToken = Request.Headers["Authorization"]
                    .FirstOrDefault()
                    ?.Split(" ")
                    .Last();
                Guid? callerId = null;
                string? userRoleName = null;
                if (jwtToken != null)
                {
                    callerId = _jwtService.GetUserIdByJwtToken(jwtToken);
                    userRoleName = _jwtService.GetRoleNameByJwtToken(jwtToken);
                }
                commonResponse = await _activityService.GetActivitiesAsync(
                    name,
                    status,
                    scope,
                    activityTypeIds,
                    startDate,
                    endDate,
                    isJoined,
                    userId,
                    callerId,
                    branchId,
                    address,
                    pageSize,
                    page,
                    orderBy,
                    sortType,
                    userRoleName
                );
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "An exception occurred in API GetActivities.");
                return StatusCode(
                    500,
                    new CommonResponse { Status = 500, Message = internalServerErrorMsg }
                );
            }
        }

        /// <summary>
        /// Get activity's detail.
        /// </summary>
        /// <response code="200">
        /// Get activity (only BRANCH_ADMIN and SYSTEM_ADMIN can get INTERNAL activity), return activty:
        /// ```
        /// {
        ///     "status": 200,
        ///     "data": {
        ///         "id": "4a0187a0-8253-ee11-9f1e-005056c00008",
        ///         "name": "Demo test 01",
        ///         "address": "thu duc",
        ///         "startDate": null,
        ///         "endDate": null,
        ///         "estimatedStartDate": "2023-09-15T17:00:00",
        ///         "estimatedEndDate": "2023-09-17T17:00:00",
        ///         "deliveringDate": "2023-09-16T17:00:00",
        ///         "status": "NOT_STARTED",
        ///         "description": "stringstringstringstringstringstringstring",
        ///         "images": [
        ///             "link"
        ///         ],
        ///         "scope": "PUBLIC",
        ///         "isNearby": false,
        ///         "numberOfParticipants": 9,
        ///         "activityTypeComponents": [
        ///             "Quyên góp",
        ///             "Lao động tình nguyện"
        ///         ],
        ///         "targetProcessResponses": [],
        ///         "isJoined": false,
        ///         "branchResponses": [
        ///             {
        ///                 "id": "0337cb91-1a51-ee11-9f1b-c809a8bfd17d",
        ///                 "name": "Từ Tâm Quận 1",
        ///                 "address": "Quận 1",
        ///                 "image": "image",
        ///                 "createdDate": "2023-09-11T00:00:00",
        ///                 "status": "ACTIVE"
        ///             }
        ///         ],
        ///         "creater": {
        ///             "id": "226abb80-972e-ee11-adee-105bad532eff",
        ///             "fullName": "ABC",
        ///             "avatar": "Image",
        ///             "role": "BusinessObject.Entities.Role"
        ///         },
        ///         "updater": {
        ///             "id": "226abb80-972e-ee11-adee-105bad532eff",
        ///             "fullName": "ABC",
        ///             "avatar": "Image",
        ///             "role": "BusinessObject.Entities.Role"
        ///         }
        ///     },
        ///     "pagination": null,
        ///     "message": "Lấy chi tiết hoạt động thành công."
        /// }
        /// ```
        /// </response>
        /// <response code="400">
        /// Activity not found, return a message of what's wrong:
        /// ```
        /// {
        ///     "status": 400,
        ///     "data": null,
        ///     "pagination": null,
        ///     "message": "Không tìm thấy hoạt động."
        /// }
        /// ```
        /// </response>
        /// <response code="403">
        /// User is not allow to get INTERNAL activity, return a message of what's wrong:
        /// ```
        /// {
        ///     "status": 403,
        ///     "data": null,
        ///     "pagination": null,
        ///     "message": "Bạn không có quyền thực hiện hành động này."
        /// }
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
        [Route("{id}")]
        public async Task<IActionResult> GetActivity(Guid id)
        {
            string internalServerErrorMsg = _config[
                "ResponseMessages:CommonMsg:InternalServerErrorMsg"
            ];
            try
            {
                string? jwtToken = Request.Headers["Authorization"]
                    .FirstOrDefault()
                    ?.Split(" ")
                    .Last();

                Guid? userId = null;
                string? userRoleName = null;
                if (jwtToken != null)
                {
                    userId = _jwtService.GetUserIdByJwtToken(jwtToken);
                    userRoleName = _jwtService.GetRoleNameByJwtToken(jwtToken);
                }
                CommonResponse commonResponse = await _activityService.GetActivityAsync(
                    id,
                    userId,
                    userRoleName
                );
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "An exception occurred in API GetActivity.");
                return StatusCode(
                    500,
                    new CommonResponse { Status = 500, Message = internalServerErrorMsg }
                );
            }
        }

        /// <summary>
        /// Update activity.
        /// </summary>
        /// <remarks>
        /// Permission: UPDATE-ACTIVITY
        /// <br/>
        /// Body:
        /// - **id**: activity's Id.
        /// - **name**: activity's name (not null, 10 &lt;= length &lt;= 150).
        /// - **address**: activity's address (10 &lt;= length &lt;= 50).
        /// - **location**: activity's location (length = 2).
        /// - **estimatedStartDate**: estimated date to start the activity (not null, created date &lt;= value &lt;= created date + 3 months).
        /// - **estimatedEndDate**: estimated date to end the activity (not null, estimatedStartDate &lt; value &lt;= estimatedStartDate + 3 months).
        /// - **deliveringDate**: date to start delivering it's donated items (estimatedStartDate &lt;= value &lt;= estimatedEndDate).
        /// - **status**: activity'status (NOT_STARTED(0), STARTED(1) or ENDED(2)).
        /// - **description**: activity's description (not null, 20 &lt;= length &lt;= 2000).
        /// - **images**: activity's images (pass images to update all activity's images, leave it null otherwise).
        /// - **activityTypeIds**: Ids of activity types (not null, not empty).
        /// - **branchIds**: Ids of joined branches (only SYSTEM_ADMIN can use).
        /// </remarks>
        /// <response code="200">
        /// Update activity success, return updated activity's Id:
        /// ```
        /// {
        ///     "status": 200,
        ///     "data": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
        ///     "pagination": null,
        ///     "message": "Cập nhật hoạt động thành công."
        /// }
        /// </response>
        /// <response code="400">
        /// Bad request in many ways, return a message of what's wrong:
        /// ```
        /// {
        ///     "status": 400,
        ///     "data": null,
        ///     "pagination": null,
        ///     "message": "Không tìm thấy hoạt động."
        /// }
        /// ```
        /// </response>
        /// <response code="403">
        /// User is not allow to perform the feature, return a message of what's wrong:
        /// ```
        /// {
        ///     "status": 403,
        ///     "data": null,
        ///     "pagination": null,
        ///     "message": "Chỉ quản trị viên hệ thống mới được cập nhật thông tin hoạt động do họ tạo."
        /// }
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
        [HttpPut]
        [PermissionAuthorize("UPDATE-ACTIVITY")]
        public async Task<IActionResult> UpdateActivity(
            [FromForm] ActivityUpdatingRequest activityUpdatingRequest
        )
        {
            string internalServerErrorMsg = _config[
                "ResponseMessages:CommonMsg:InternalServerErrorMsg"
            ];
            try
            {
                string jwtToken = Request.Headers["Authorization"]
                    .FirstOrDefault()
                    ?.Split(" ")
                    .Last()!;
                Guid userId = _jwtService.GetUserIdByJwtToken(jwtToken);
                string userRoleName = _jwtService.GetRoleNameByJwtToken(jwtToken);
                CommonResponse commonResponse = await _activityService.UpdateActivityAsync(
                    activityUpdatingRequest,
                    userId,
                    userRoleName
                );
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "An exception occurred in API UpdateActivity.");
                return StatusCode(
                    500,
                    new CommonResponse { Status = 500, Message = internalServerErrorMsg }
                );
            }
        }

        /// <summary>
        /// Deactivate activity.
        /// </summary>
        /// <remarks>
        /// Permission: DELETE-ACTIVITY
        /// </remarks>
        /// <response code="200">
        /// Deactivate activity success, return message:
        /// ```
        /// {
        ///     "status": 200,
        ///     "data": null,
        ///     "pagination": null,
        ///     "message": "Ngừng hoạt động thành công."
        /// }
        /// </response>
        /// <response code="400">
        /// Bad request in many ways, return a message of what's wrong:
        /// ```
        /// {
        ///     "status": 400,
        ///     "data": null,
        ///     "pagination": null,
        ///     "message": "Không tìm thấy hoạt động."
        /// }
        /// ```
        /// </response>
        /// <response code="403">
        /// User is not allow to perform the feature, return a message of what's wrong:
        /// ```
        /// {
        ///     "status": 403,
        ///     "data": null,
        ///     "pagination": null,
        ///     "message": "Chỉ quản trị viên hệ thống mới được cập nhật thông tin hoạt động do họ tạo."
        /// }
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
        [HttpDelete]
        [PermissionAuthorize("DELETE-ACTIVITY")]
        [Route("{id}")]
        public async Task<IActionResult> DeactivateActivity(Guid id)
        {
            string internalServerErrorMsg = _config[
                "ResponseMessages:CommonMsg:InternalServerErrorMsg"
            ];
            try
            {
                string jwtToken = Request.Headers["Authorization"]
                    .FirstOrDefault()
                    ?.Split(" ")
                    .Last()!;
                Guid userId = _jwtService.GetUserIdByJwtToken(jwtToken);
                string userRoleName = _jwtService.GetRoleNameByJwtToken(jwtToken);
                CommonResponse commonResponse = await _activityService.DeactivateActivityAsync(
                    id,
                    userId,
                    userRoleName
                );
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "An exception occurred in API DeactivateActivity.");
                return StatusCode(
                    500,
                    new CommonResponse { Status = 500, Message = internalServerErrorMsg }
                );
            }
        }

        //[HttpGet]
        //[Route("test")]
        //public async Task<IActionResult> Test()
        //{
        //    //List<Item> items = new List<Item>
        //    //{
        //    //    new Item { A = 1, B = 2 },
        //    //    new Item { A = null, B = 3 },
        //    //    new Item { A = 2, B = 1 },
        //    //    new Item { A = null, B = 4 },
        //    //    new Item { A = 3, B = 3 },
        //    //    new Item { A = null, B = null }
        //    //};

        //    //// Order the list by A (items with A as null will be ranked behind)
        //    //// Then, order by B
        //    //List<Item> orderedItems = items
        //    //    .OrderByDescending(item => item.A.HasValue) // Order by A not null (true comes before false)
        //    //    .ThenBy(item => item.A) // Order by A (null values will be grouped together)
        //    //    .ThenBy(item => item.B)
        //    //    .ToList(); // Then, order by B

        //    //List<string> rs = new List<string>();
        //    //foreach (var item in orderedItems)
        //    //{
        //    //    rs.Add($"A: {item.A}, B: {item.B}");
        //    //}
        //    //return Ok(rs);
        //    return Ok();
        //}


        /// <summary>
        /// System get statistics of activty
        /// </summary>
        /// <remarks>
        /// Role: SYSTEM ADMIN
        /// <br />
        /// </remarks>
        [HttpGet]
        [Authorize(Roles = "SYSTEM_ADMIN,BRANCH_ADMIN")]
        [Route("statistics/all-status")]
        public async Task<IActionResult> CountActivty(
            DateTime? startDate,
            DateTime? endDate,
            Guid? branchId
        )
        {
            try
            {
                string? jwtToken = Request.Headers["Authorization"]
                    .FirstOrDefault()
                    ?.Split(" ")
                    .Last();
                Guid? callerId = null;
                string? userRoleName = null;
                if (jwtToken != null)
                {
                    callerId = _jwtService.GetUserIdByJwtToken(jwtToken);
                    userRoleName = _jwtService.GetRoleNameByJwtToken(jwtToken);
                }
                CommonResponse commonResponse = new CommonResponse();
                if (userRoleName == RoleEnum.BRANCH_ADMIN.ToString())
                {
                    commonResponse = await _activityService.CountActivityByAllStatus(
                        startDate,
                        endDate,
                        branchId,
                        userRoleName,
                        callerId
                    );
                }
                else
                {
                    commonResponse = await _activityService.CountActivityByAllStatus(
                        startDate,
                        endDate,
                        branchId,
                        null,
                        null
                    );
                }
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
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An exception occurred in {nameof(ActivitiesController)}.");
                return StatusCode(
                    500,
                    new CommonResponse
                    {
                        Status = 500,
                        Message = _config["ResponseMessages:CommonMsg:InternalServerErrorMsg"]
                    }
                );
            }
        }

        /// <summary>
        /// System get statistics of activity
        /// </summary>
        /// <remarks>
        /// Role: SYSTEM ADMIN
        /// <br />
        /// </remarks>
        [HttpGet]
        [Authorize(Roles = "SYSTEM_ADMIN,BRANCH_ADMIN")]
        [Route("statistics")]
        public async Task<IActionResult> CountActivityByStatus(
            DateTime startDate,
            DateTime endDate,
            ActivityStatus? status,
            TimeFrame timeFrame,
            Guid? branchId
        )
        {
            try
            {
                string? jwtToken = Request.Headers["Authorization"]
                    .FirstOrDefault()
                    ?.Split(" ")
                    .Last();
                Guid? callerId = null;
                string? userRoleName = null;
                if (jwtToken != null)
                {
                    callerId = _jwtService.GetUserIdByJwtToken(jwtToken);
                    userRoleName = _jwtService.GetRoleNameByJwtToken(jwtToken);
                }
                CommonResponse commonResponse = new CommonResponse();
                if (userRoleName == RoleEnum.BRANCH_ADMIN.ToString())
                {
                    commonResponse = await _activityService.CountActivityByStatus(
                        startDate,
                        endDate,
                        status,
                        timeFrame,
                        branchId,
                        userRoleName,
                        callerId
                    );
                }
                else
                {
                    commonResponse = await _activityService.CountActivityByStatus(
                        startDate,
                        endDate,
                        status,
                        timeFrame,
                        branchId,
                        null,
                        callerId
                    );
                }
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
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An exception occurred in {nameof(ActivitiesController)}.");
                return StatusCode(
                    500,
                    new CommonResponse
                    {
                        Status = 500,
                        Message = _config["ResponseMessages:CommonMsg:InternalServerErrorMsg"]
                    }
                );
            }
        }

        [HttpGet]
        [Authorize(Roles = "BRANCH_ADMIN")]
        [Route("items/{itemId}")]
        public async Task<IActionResult> SearchActivityByItemId(Guid itemId)
        {
            try
            {
                string? jwtToken = Request.Headers["Authorization"]
                    .FirstOrDefault()
                    ?.Split(" ")
                    .Last();

                Guid? userId = null;
                string? userRoleName = null;
                if (jwtToken != null)
                {
                    userId = _jwtService.GetUserIdByJwtToken(jwtToken);
                    userRoleName = _jwtService.GetRoleNameByJwtToken(jwtToken);
                }
                CommonResponse commonResponse = await _activityService.SearchActivityByItemId(
                    itemId,
                    userId!.Value
                );
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
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An exception occurred in {nameof(ActivitiesController)}.");
                return StatusCode(
                    500,
                    new CommonResponse
                    {
                        Status = 500,
                        Message = _config["ResponseMessages:CommonMsg:InternalServerErrorMsg"]
                    }
                );
            }
        }
    }

    //class Item
    //{
    //    public int? A { get; set; }
    //    public int? B { get; set; }
    //}
}
