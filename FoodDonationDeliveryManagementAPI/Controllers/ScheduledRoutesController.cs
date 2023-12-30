using BusinessLogic.Services;
using BusinessLogic.Utils.SecurityServices;
using DataAccess.EntityEnums;
using DataAccess.Models.Requests;
using DataAccess.Models.Responses;
using DataAccess.ModelsEnum;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodDonationDeliveryManagementAPI.Controllers
{
    [Route("scheduled-routes")]
    [ApiController]
    public class ScheduledRoutesController : ControllerBase
    {
        private readonly IScheduledRouteService _scheduledRouteService;
        private readonly ILogger<ScheduledRoutesController> _logger;
        private readonly IConfiguration _config;
        private readonly IJwtService _jwtService;

        public ScheduledRoutesController(
            IScheduledRouteService scheduledRouteService,
            ILogger<ScheduledRoutesController> logger,
            IConfiguration config,
            IJwtService jwtService
        )
        {
            _scheduledRouteService = scheduledRouteService;
            _logger = logger;
            _config = config;
            _jwtService = jwtService;
        }

        //[HttpPost]
        //public async Task<IActionResult> UpdateScheduledRoutes()
        //{
        //    try
        //    {
        //        await _scheduledRouteService.UpdateScheduledRoutes(null, null);
        //        return Ok();
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, ex);
        //    }
        //}


        /// <summary>
        /// Branch admin run feature checking to create new scheduled route of system
        /// </summary>
        /// <param name="deliveryType">type of delivery requests need to be scheduled - 0 (DONATED_REQUEST_TO_BRANCH), 1 (BRANCH_TO_AID_REQUEST), 2 (BRANCH_TO_BRANCH)</param>
        [HttpPost]
        [Authorize(Roles = "BRANCH_ADMIN")]
        public async Task<IActionResult> UpdateScheduledRoutesByDeliveryTypeAndBranchAdminId(
            DeliveryType deliveryType
        )
        {
            try
            {
                string jwtToken = Request.Headers["Authorization"]
                    .FirstOrDefault()
                    ?.Split(" ")
                    .Last()!;
                Guid userId = _jwtService.GetUserIdByJwtToken(jwtToken);
                CommonResponse commonResponse =
                    await _scheduledRouteService.UpdateScheduledRoutesByDeliveryTypeAndBranchAdminIdAsync(
                        deliveryType,
                        userId
                    );
                switch (commonResponse.Status)
                {
                    case 200:
                        return Ok(commonResponse);
                    case 403:
                        return StatusCode(403, commonResponse);
                    default:
                        return StatusCode(500, commonResponse);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    $"An exception occurred in controller {nameof(ScheduledRoutesController)}, method {nameof(UpdateScheduledRoutesByDeliveryTypeAndBranchAdminId)}."
                );
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
        /// CONTRIBUTOR accept scheduled route, if latitude and longitude are left null, system will use CONTRIBUTOR's location in profile
        /// </summary>
        /// <remarks>
        /// Role: CONTRIBUTOR
        /// </remarks>
        [HttpPut]
        //[PermissionAuthorize("ACCEPT-DELIVERY-REQUEST")]
        [Authorize(Roles = "CONTRIBUTOR")]
        public async Task<IActionResult> AcceptScheduledRoute(
            ScheduledRouteAcceptingRequest scheduledRouteAcceptingRequest
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
                CommonResponse commonResponse =
                    await _scheduledRouteService.AcceptScheduledRouteAsync(
                        userId,
                        scheduledRouteAcceptingRequest
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
                _logger.LogError(
                    ex,
                    $"An exception occurred in controller {nameof(ScheduledRoutesController)}, method {nameof(AcceptScheduledRoute)}."
                );
                return StatusCode(
                    500,
                    new CommonResponse { Status = 500, Message = internalServerErrorMsg }
                );
            }
        }

        /// <summary>
        /// CONTRIBUTOR start scheduled route that are accepted by them.
        /// </summary>
        /// <remarks>
        /// Role: CONTRIBUTOR
        /// </remarks>
        [HttpPut]
        [Authorize(Roles = "CONTRIBUTOR")]
        [Route("started-scheduled-route")]
        public async Task<IActionResult> StartScheduledRoute(
            ScheduledRouteStartingRequest scheduledRouteStartingRequest
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
                CommonResponse commonResponse =
                    await _scheduledRouteService.StartScheduledRouteAsync(
                        userId,
                        scheduledRouteStartingRequest
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
                _logger.LogError(
                    ex,
                    $"An exception occurred in controller {nameof(ScheduledRoutesController)}, method {nameof(StartScheduledRoute)}."
                );
                return StatusCode(
                    500,
                    new CommonResponse { Status = 500, Message = internalServerErrorMsg }
                );
            }
        }

        /// <summary>
        /// Branch admin collect brought items by user to import to system's stock and finish the scheduled route. Only pass delivery request's information that brought by CONTRIBUTOR. All items in those delivery request must be pass too even if their quantity is 0.
        /// </summary>
        /// <remarks>
        /// Role: BRANCH_ADMIN
        /// </remarks>
        [HttpPut]
        [Authorize(Roles = "BRANCH_ADMIN")]
        [Route("scheduled-route-type-donated-requests-to-branch")]
        public async Task<IActionResult> ReceiveItemsToFinishScheduledRouteTypeItemsToBranch(
            ReceivedItemsToFinishScheduledRoute receivedItemsToFinishScheduledRoute
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
                CommonResponse commonResponse =
                    await _scheduledRouteService.ReceiveItemsToFinishScheduledRouteTypeItemsToBranchAsync(
                        userId,
                        receivedItemsToFinishScheduledRoute
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
                _logger.LogError(
                    ex,
                    $"An exception occurred in controller {nameof(ScheduledRoutesController)}, method {nameof(ReceiveItemsToFinishScheduledRouteTypeItemsToBranch)}."
                );
                return StatusCode(
                    500,
                    new CommonResponse { Status = 500, Message = internalServerErrorMsg }
                );
            }
        }

        /// <summary>
        /// Branch admin get sample stocks detail before give items to user to deliver them to charity unit.
        /// </summary>
        /// <remarks>
        /// Role: BRANCH_ADMIN
        /// </remarks>
        [HttpGet]
        [Authorize(Roles = "BRANCH_ADMIN")]
        [Route("scheduled-route-type-branch-to-charity-unit/{scheduledRouteId}")]
        public async Task<IActionResult> GetSampleGiveItemsToStartScheduledRoute(
            Guid scheduledRouteId
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
                CommonResponse commonResponse =
                    await _scheduledRouteService.GetSampleGivingItemsToStartScheduledRouteAsync(
                        userId,
                        scheduledRouteId
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
                _logger.LogError(
                    ex,
                    $"An exception occurred in controller {nameof(ScheduledRoutesController)}, method {nameof(GetSampleGiveItemsToStartScheduledRoute)}."
                );
                return StatusCode(
                    500,
                    new CommonResponse { Status = 500, Message = internalServerErrorMsg }
                );
            }
        }

        /// <summary>
        /// Branch admin confirm giving items to user to deliver them to charity unit.
        /// </summary>
        /// <remarks>
        /// Role: BRANCH_ADMIN
        /// </remarks>
        [HttpPut]
        [Authorize(Roles = "BRANCH_ADMIN")]
        [Route("scheduled-route-type-branch-to-charity-unit")]
        public async Task<IActionResult> GiveItemsToStartScheduledRoute(
            ExportStocksForDeliveryRequestConfirmingRequest exportStocksForDeliveryRequestConfirmingRequest
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
                CommonResponse commonResponse =
                    await _scheduledRouteService.GiveItemsToStartScheduledRouteAsync(
                        userId,
                        exportStocksForDeliveryRequestConfirmingRequest
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
                _logger.LogError(
                    ex,
                    $"An exception occurred in controller {nameof(ScheduledRoutesController)}, method {nameof(GiveItemsToStartScheduledRoute)}."
                );
                return StatusCode(
                    500,
                    new CommonResponse { Status = 500, Message = internalServerErrorMsg }
                );
            }
        }

        /// <summary>
        /// Get list of scheduled routes for CONTRIBUTOR
        /// </summary>
        /// <remarks>
        /// Role: CONTRIBUTOR
        /// </remarks>
        /// <param name="latitude">Must has if filtering status = 0 (PENDING)</param>
        /// <param name="longitude">Must has if filtering status = 0 (PENDING)</param>
        /// <param name="branchId">Branch that the scheduled routes is responsible - leave null to get all</param>
        /// <param name="stockUpdatedHistoryType">0 (IMPORT) or 1 (EXPORT) - leave null to get all</param>
        /// <param name="status">0 (PENDING), 1 (ACCEPTED), 2 (PROCESSING), 3 (FINISHED) or 4 (CANCEL) - leave null to get all except 0 (PENDING)</param>
        /// <param name="startDate">startDate forward - leave null to igget all</param>
        /// <param name="endDate">endDate backward - leave null to get all</param>
        /// <param name="sortType">0 (ASC) or 1 (DES) - leave null to sort default as 0 (ASC)</param>
        /// <response code="200">
        /// ```
        /// {
        ///     "status": 200,
        ///     "data": [
        ///         {
        ///             "id": "3eb8c3b1-4580-ee11-9f24-005056c00008",
        ///             "numberOfDeliveryRequests": 1,
        ///             "scheduledTime": {
        ///                 "day": "2023-12-05",
        ///                 "startTime": "03:00",
        ///                 "endTime": "08:00"
        ///             },
        ///             "orderedAddresses": [
        ///                 "Thủ Đức, Thành phố Hồ Chí Minh",
        ///                 "Khu Công nghệ cao, Quận 9"
        ///             ],
        ///             "totalDistanceAsMeters": 5599.799999999999,
        ///             "totalTimeAsSeconds": 1000.763358778626,
        ///             "bulkyLevel": "VERY_BULKY",
        ///             "type": "IMPORT",
        ///             "status": "PENDING"
        ///         },
        ///         {
        ///             "id": "41b8c3b1-4580-ee11-9f24-005056c00008",
        ///             "numberOfDeliveryRequests": 1,
        ///             "scheduledTime": {
        ///             "day": "2023-12-05",
        ///                 "startTime": "03:00",
        ///                 "endTime": "08:00"
        ///             },
        ///             "orderedAddresses": [
        ///                 "Thủ Đức, Thành phố Hồ Chí Minh",
        ///                 "Khu Công nghệ cao, Quận 9"
        ///             ],
        ///             "totalDistanceAsMeters": 5599.799999999999,
        ///             "totalTimeAsSeconds": 1000.763358778626,
        ///             "bulkyLevel": "VERY_BULKY",
        ///             "type": "IMPORT",
        ///             "status": "PENDING"
        ///         }
        ///     ],
        ///     "pagination": null,
        ///     "message": "Lấy danh sách lịch trình vận chuyển thành công."
        /// }
        /// ```
        /// </response>
        [HttpGet]
        [Authorize(Roles = "CONTRIBUTOR")]
        [Route("contributor")]
        public async Task<IActionResult> GetScheduledRoutesForUser(
            double? latitude,
            double? longitude,
            Guid? branchId,
            StockUpdatedHistoryType? stockUpdatedHistoryType,
            ScheduledRouteStatus? status,
            string? startDate,
            string? endDate,
            SortType? sortType
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
                CommonResponse commonResponse =
                    await _scheduledRouteService.GetScheduledRoutesForUserAsync(
                        latitude,
                        longitude,
                        branchId,
                        stockUpdatedHistoryType,
                        status,
                        startDate,
                        endDate,
                        userId,
                        userRoleName,
                        sortType
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
                _logger.LogError(
                    ex,
                    $"An exception occurred in controller {nameof(ScheduledRoutesController)}, method {nameof(GetScheduledRoutesForUser)}."
                );
                return StatusCode(
                    500,
                    new CommonResponse { Status = 500, Message = internalServerErrorMsg }
                );
            }
        }

        /// <summary>
        /// Get list of scheduled routes for branch admin and system
        /// </summary>
        /// <remarks>
        /// Role: BRANCH_ADMIN, SYSTEM_ADMIN
        /// </remarks>
        /// <param name="branchId">Branch that the scheduled routes is responsible - leave null to get default (branch admin can only get their branch's scheduled routes, system admin can get all)</param>
        /// <param name="stockUpdatedHistoryType">0 (IMPORT) or 1 (EXPORT) - leave null to get all</param>
        /// <param name="status">0 (PENDING), 1 (ACCEPTED), 2 (PROCESSING), 3 (FINISHED) or 4 (CANCEL) - leave null to get all except 0 (PENDING)</param>
        /// <param name="userId">Id of user - leave null to get all</param>
        /// <param name="startDate">startDate forward - leave null to get all</param>
        /// <param name="endDate">endDate backward - leave null to get all</param>
        /// <param name="sortType">0 (ASC) or 1 (DES) - leave null to sort default as 0 (ASC)</param>
        /// <param name="pageSize">page size - default 10</param>
        /// <param name="page">page - default 1</param>
        /// <response code="200">
        /// ```
        /// {
        ///     "status": 200,
        ///     "data": [
        ///         {
        ///             "id": "41b8c3b1-4580-ee11-9f24-005056c00008",
        ///             "numberOfDeliveryRequests": 1,
        ///             "scheduledTime": {
        ///             "day": "2023-12-05",
        ///                 "startTime": "03:00",
        ///                 "endTime": "08:00"
        ///             },
        ///             "orderedAddresses": [
        ///                 "Thủ Đức, Thành phố Hồ Chí Minh",
        ///                 "Khu Công nghệ cao, Quận 9"
        ///             ],
        ///             "totalDistanceAsMeters": 5599.799999999999,
        ///             "totalTimeAsSeconds": 1000.763358778626,
        ///             "bulkyLevel": "VERY_BULKY",
        ///             "type": "IMPORT",
        ///             "branch": {
        ///                 "id": "db324806-1a51-ee11-9f1b-c809a8bfd17d",
        ///                 "name": "Từ Tâm số 2",
        ///                 "image": "link"
        ///             },
        ///             "status": "ACCEPTED",
        ///             "acceptedUser": {
        ///                 "id": "a1fe47d6-922e-ee11-adee-105bad532efe",
        ///                 "fullName": "Volunter",
        ///                 "avatar": "link",
        ///                 "role": null,
        ///                 "phone": null,
        ///                 "email": null,
        ///                 "status": null
        ///             }
        ///         }
        ///     ],
        ///     "pagination": null,
        ///     "message": "Lấy danh sách lịch trình vận chuyển thành công."
        /// }
        /// ```
        /// </response>
        [HttpGet]
        [Authorize(Roles = "BRANCH_ADMIN,SYSTEM_ADMIN")]
        [Route("admin")]
        public async Task<IActionResult> GetScheduledRoutesForAdmin(
            Guid? branchId,
            StockUpdatedHistoryType? stockUpdatedHistoryType,
            ScheduledRouteStatus? status,
            Guid? userId,
            string? startDate,
            string? endDate,
            SortType? sortType,
            int? pageSize,
            int? page
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
                Guid callerId = _jwtService.GetUserIdByJwtToken(jwtToken);
                string userRoleName = _jwtService.GetRoleNameByJwtToken(jwtToken);
                CommonResponse commonResponse =
                    await _scheduledRouteService.GetScheduledRoutesForAdminAsync(
                        branchId,
                        stockUpdatedHistoryType,
                        status,
                        startDate,
                        endDate,
                        userId,
                        callerId,
                        userRoleName,
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
                    case 403:
                        return StatusCode(403, commonResponse);
                    default:
                        return StatusCode(500, commonResponse);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    $"An exception occurred in controller {nameof(ScheduledRoutesController)}, method {nameof(GetScheduledRoutesForUser)}."
                );
                return StatusCode(
                    500,
                    new CommonResponse { Status = 500, Message = internalServerErrorMsg }
                );
            }
        }

        /// <summary>
        /// Get scheduled route detail for CONTRIBUTOR
        /// </summary>
        /// <remarks>
        /// Role: CONTRIBUTOR
        /// </remarks>
        /// <param name="scheduledRouteId">Scheduled route Id</param>
        /// <response code="200">
        /// ```
        /// {
        ///     "status": 200,
        ///     "data": {
        ///         "id": "115c8924-4b81-ee11-9f24-005056c00008",
        ///         "numberOfDeliveryRequests": 4,
        ///         "scheduledTime": {
        ///             "day": "11/12/2023",
        ///             "startTime": "17:00",
        ///             "endTime": "23:00"
        ///         },
        ///         "orderedDeliveryRequests": [
        ///             {
        ///                 "id": "e56042c1-6376-ee11-9f24-005056c00008",
        ///                 "address": "Thủ Đức, Thành phố Hồ Chí Minh",
        ///                 "location": [
        ///                     10.962153173514656,
        ///                     106.78998668280421
        ///                 ],
        ///                 "currentScheduledTime": {
        ///                     "day": "2023-11-12",
        ///                     "startTime": "17:00",
        ///                     "endTime": "23:00"
        ///                 },
        ///                 "avatar": "link",
        ///                 "name": "Volunter",
        ///                 "phone": "0345678912",
        ///                 "deliveryItems": [
        ///                     {
        ///                         "name": "Nước tương 1l, Mặn",
        ///                         "image": "link",
        ///                         "unit": "Chai",
        ///                         "quantity": 9,
        ///                         "receivedQuantity": 8
        ///                     },
        ///                     {
        ///                         "name": "Cá Hồi",
        ///                         "image": "link",
        ///                         "unit": "Kí",
        ///                         "quantity": 8,
        ///                         "receivedQuantity": 7
        ///                     }
        ///                 ]
        ///             },
        ///             {
        ///                 "id": "4f1c34b7-6276-ee11-9f24-005056c00008",
        ///                 "address": "Thủ Đức, Thành phố Hồ Chí Minh",
        ///                 "location": [
        ///                     10.997834611395536,
        ///                     106.8133758828707
        ///                 ],
        ///                 "currentScheduledTime": {
        ///                     "day": "2023-11-12",
        ///                     "startTime": "17:00",
        ///                     "endTime": "23:00"
        ///                 },
        ///                 "avatar": "link",
        ///                 "name": "Volunter",
        ///                 "phone": "0345678912",
        ///                 "deliveryItems": [
        ///                     {
        ///                         "name": "Nước tương 1l, Mặn",
        ///                         "image": "link",
        ///                         "unit": "Chai",
        ///                         "quantity": 8,
        ///                         "receivedQuantity": null
        ///                     },
        ///                     {
        ///                         "name": "Cá Hồi",
        ///                         "image": "link",
        ///                         "unit": "Kí",
        ///                         "quantity": 9,
        ///                         "receivedQuantity": null
        ///                     }
        ///                 ]
        ///             },
        ///             {
        ///                 "id": null,
        ///                 "address": "Hòa An, thành phố Biên Hòa",
        ///                 "location": [
        ///                     10.934712080642688,
        ///                     106.81640737419067
        ///                 ],
        ///                 "currentScheduledTime": null,
        ///                 "name": "ABC",
        ///                 "phone": "0323456789",
        ///                 "deliveryItems": null
        ///             }
        ///         ],
        ///         "totalDistanceAsMeters": 27419.5,
        ///         "totalTimeAsSeconds": 4631.297709923664,
        ///         "bulkyLevel": "VERY_BULKY",
        ///         "type": "IMPORT",
        ///         "status": "PROCESSING"
        ///     },
        ///     "pagination": null,
        ///     "message": "Lấy lịch trình vận chuyển thành công."
        /// }
        /// ```
        /// </response>
        [HttpGet]
        [Route("{scheduledRouteId}/contributor")]
        public async Task<IActionResult> GetScheduledRouteForUser(Guid scheduledRouteId)
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
                CommonResponse commonResponse =
                    await _scheduledRouteService.GetScheduledRouteForUserAsync(
                        scheduledRouteId,
                        userId
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
                _logger.LogError(
                    ex,
                    $"An exception occurred in controller {nameof(ScheduledRoutesController)}, method {nameof(GetScheduledRoutesForUser)}."
                );
                return StatusCode(
                    500,
                    new CommonResponse { Status = 500, Message = internalServerErrorMsg }
                );
            }
        }

        /// <summary>
        /// Get scheduled route detail for branch admin and system admin - branch admin can only get details of their branch's scheduled routes
        /// </summary>
        /// <remarks>
        /// Role: BRANCH_ADMIN,SYSTEM_ADMIN
        /// </remarks>
        /// <param name="scheduledRouteId">Scheduled route Id</param>
        /// <response code="200">
        /// ```
        /// {
        ///     "status": 200,
        ///     "data": {
        ///         "id": "4e59be1e-9f82-ee11-b75e-6045bd1c910c",
        ///         "numberOfDeliveryRequests": 4,
        ///         "scheduledTime": {
        ///             "day": "2023-11-14",
        ///             "startTime": "08:00",
        ///             "endTime": "23:30"
        ///         },
        ///         "orderedDeliveryRequests": [
        ///             {
        ///                 "id": "439c9aee-6376-ee11-9f24-005056c00008",
        ///                 "status": "ACCEPTED",
        ///                 "address": "Thủ Đức, Thành phố Hồ Chí Minh",
        ///                 "location": [
        ///                     10.935754877073498,
        ///                     106.80433235872876
        ///                 ],
        ///                 "currentScheduledTime": {
        ///                     "day": "2023-11-14",
        ///                     "startTime": "08:00",
        ///                     "endTime": "23:30"
        ///                 },
        ///                 "proofImage": null,
        ///                 "avatar": "link",
        ///                 "name": "Volunter",
        ///                 "phone": "0345678912",
        ///                 "activityId": null,
        ///                 "activityName": null,
        ///                 "deliveryItems": [
        ///                     {
        ///                         "name": "Nước tương 1l, Mặn",
        ///                         "image": "link",
        ///                         "unit": "Chai",
        ///                         "quantity": 7,
        ///                         "receivedQuantity": null
        ///                     },
        ///                     {
        ///                         "name": "Cá Hồi",
        ///                         "image": "link",
        ///                         "unit": "Kí",
        ///                         "quantity": 6,
        ///                         "receivedQuantity": null
        ///                     }
        ///                 ]
        ///             },
        ///             {
        ///                 "id": null,
        ///                 "status": null,
        ///                 "address": "Hòa An, thành phố Biên Hòa",
        ///                 "location": [
        ///                     10.934712080642688,
        ///                     106.81640737419067
        ///                 ],
        ///                 "currentScheduledTime": null,
        ///                 "proofImage": null,
        ///                 "avatar": "link",
        ///                 "name": "Từ Tâm số 2",
        ///                 "phone": "0323456789",
        ///                 "activityId": null,
        ///                 "activityName": null,
        ///                 "deliveryItems": null
        ///             }
        ///         ],
        ///         "totalDistanceAsMeters": 30152.299999999996,
        ///         "totalTimeAsSeconds": 4973.282442748092,
        ///         "bulkyLevel": "VERY_BULKY",
        ///         "type": "IMPORT",
        ///         "status": "ACCEPTED",
        ///         "acceptedUser": {
        ///             "id": "a1fe47d6-922e-ee11-adee-105bad532efe",
        ///             "fullName": "Volunter",
        ///             "avatar": "link",
        ///             "role": null,
        ///             "phone": "0345678912",
        ///             "email": "Volunteer@gmail.com",
        ///             "status": null
        ///         }
        ///     },
        ///     "pagination": null,
        ///     "message": "Lấy lịch trình vận chuyển thành công."
        /// }
        /// ```
        /// </response>
        [HttpGet]
        [Authorize(Roles = "BRANCH_ADMIN,SYSTEM_ADMIN")]
        [Route("{scheduledRouteId}/admin")]
        public async Task<IActionResult> GetScheduledRouteForAdmin(Guid scheduledRouteId)
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
                CommonResponse commonResponse =
                    await _scheduledRouteService.GetScheduledRouteForAdminAsync(
                        scheduledRouteId,
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
                _logger.LogError(
                    ex,
                    $"An exception occurred in controller {nameof(ScheduledRoutesController)}, method {nameof(GetScheduledRoutesForUser)}."
                );
                return StatusCode(
                    500,
                    new CommonResponse { Status = 500, Message = internalServerErrorMsg }
                );
            }
        }

        /// <summary>
        /// CONTRIBUTOR update all delivery requests' status to next status of scheduled route. Use this api for:
        /// </summary>
        /// <remarks>
        /// Role: CONTRIBUTOR
        /// <br />
        /// - **IMPORT scheduled route**:
        ///     - when CONTRIBUTOR arrived to branch to give collected donated items
        ///     - when CONTRIBUTOR give donated items to branch admin
        /// - **EXPORT scheduled route**:
        ///     - when CONTRIBUTOR arrived to branch to received aid items
        ///     - when CONTRIBUTOR received collected aid items to branch admin
        /// </remarks>
        [HttpPut]
        [Authorize(Roles = "CONTRIBUTOR")]
        [Route("{scheduledRouteId}/delivery-requests")]
        public async Task<IActionResult> UpdateNextStatusOfDeliveryRequestsOfScheduledRoute(
            Guid scheduledRouteId
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
                CommonResponse commonResponse =
                    await _scheduledRouteService.UpdateNextStatusOfDeliveryRequestsOfScheduledRouteAsync(
                        userId,
                        scheduledRouteId
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
                _logger.LogError(
                    ex,
                    $"An exception occurred in {nameof(ScheduledRoutesController)}."
                );
                return StatusCode(
                    500,
                    new CommonResponse { Status = 500, Message = internalServerErrorMsg }
                );
            }
        }

        /// <summary>
        /// CONTRIBUTOR cancel scheduled route
        /// </summary>
        /// <remarks>
        /// Role: CONTRIBUTOR
        /// </remarks>
        [HttpPut]
        [Authorize(Roles = "CONTRIBUTOR")]
        [Route("{scheduledRouteId}")]
        public async Task<IActionResult> CancelScheduledRoute(Guid scheduledRouteId)
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
                CommonResponse commonResponse =
                    await _scheduledRouteService.CancelScheduledRouteAsync(
                        userId,
                        scheduledRouteId
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
                _logger.LogError(
                    ex,
                    $"An exception occurred in {nameof(ScheduledRoutesController)}."
                );
                return StatusCode(
                    500,
                    new CommonResponse { Status = 500, Message = internalServerErrorMsg }
                );
            }
        }
    }
}
