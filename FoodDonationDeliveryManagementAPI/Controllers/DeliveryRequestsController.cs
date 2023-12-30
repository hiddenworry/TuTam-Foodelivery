using BusinessLogic.Services;
using BusinessLogic.Utils.SecurityServices;
using DataAccess.EntityEnums;
using DataAccess.Models.Requests;
using DataAccess.Models.Requests.Enum;
using DataAccess.Models.Responses;
using DataAccess.ModelsEnum;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodDonationDeliveryManagementAPI.Controllers
{
    [Route("delivery-requests")]
    [ApiController]
    public class DeliveryRequestsController : ControllerBase
    {
        private readonly IDeliveryRequestService _deliveryRequestService;
        private readonly IScheduledRouteService _scheduledRouteService;
        private readonly ILogger<ActivitiesController> _logger;
        private readonly IConfiguration _config;
        private readonly IJwtService _jwtService;

        public DeliveryRequestsController(
            IDeliveryRequestService deliveryRequestService,
            IScheduledRouteService scheduledRouteService,
            ILogger<ActivitiesController> logger,
            IConfiguration config,
            IJwtService jwtService
        )
        {
            _deliveryRequestService = deliveryRequestService;
            _scheduledRouteService = scheduledRouteService;
            _logger = logger;
            _config = config;
            _jwtService = jwtService;
        }

        [HttpPost]
        [Authorize(Roles = "BRANCH_ADMIN")]
        [Route("donated-request-to-branch")]
        public async Task<IActionResult> CreateDeliveryRequestsForDonatedRequestToBranch(
            DeliveryRequestsForDonatedRequestToBranchCreatingRequest deliveryRequestCreatingRequest
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
                    await _deliveryRequestService.CreateDeliveryRequestsForDonatedRequestToBranchAsync(
                        userId,
                        deliveryRequestCreatingRequest
                    );
                switch (commonResponse.Status)
                {
                    case 200:
                    {
                        //BackgroundJob.Enqueue(
                        //    () =>
                        //        _scheduledRouteService.UpdateScheduledRoutes(
                        //            BusinessObject
                        //                .EntityEnums
                        //                .DeliveryType
                        //                .DONATED_REQUEST_TO_BRANCH
                        //        )
                        //);
                        return Ok(commonResponse);
                    }
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
                    $"An exception occurred in {nameof(DeliveryRequestsController)}."
                );
                return StatusCode(
                    500,
                    new CommonResponse { Status = 500, Message = internalServerErrorMsg }
                );
            }
        }

        [HttpPost]
        [Authorize(Roles = "BRANCH_ADMIN")]
        [Route("branch-to-aid-request")]
        public async Task<IActionResult> CreateDeliveryRequestsForBranchToAidRequest(
            DeliveryRequestsForBranchToAidRequestCreatingRequest deliveryRequestCreatingRequest
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
                    await _deliveryRequestService.CreateDeliveryRequestsForBranchToAidRequestAsync(
                        userId,
                        deliveryRequestCreatingRequest
                    );
                switch (commonResponse.Status)
                {
                    case 200:
                    {
                        //BackgroundJob.Enqueue(
                        //    () =>
                        //        _scheduledRouteService.UpdateScheduledRoutes(
                        //            BusinessObject.EntityEnums.DeliveryType.BRANCH_TO_AID_REQUEST
                        //        )
                        //);
                        return Ok(commonResponse);
                    }
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
                    $"An exception occurred in {nameof(DeliveryRequestsController)}."
                );
                return StatusCode(
                    500,
                    new CommonResponse { Status = 500, Message = internalServerErrorMsg }
                );
            }
        }

        //[HttpPost]
        //[Authorize(Roles = "BRANCH_ADMIN")]
        //[Route("branch-to-branch")]
        //public async Task<IActionResult> CreateDeliveryRequestsForBranchToBranch(
        //    DeliveryRequestsForBranchToBranchCreatingRequest deliveryRequestCreatingRequest
        //)
        //{
        //    string internalServerErrorMsg = _config[
        //        "ResponseMessages:CommonMsg:InternalServerErrorMsg"
        //    ];
        //    try
        //    {
        //        string jwtToken = Request.Headers["Authorization"]
        //            .FirstOrDefault()
        //            ?.Split(" ")
        //            .Last()!;
        //        Guid userId = _jwtService.GetUserIdByJwtToken(jwtToken);
        //        CommonResponse commonResponse =
        //            await _deliveryRequestService.CreateDeliveryRequestsForBranchToBranchAsync(
        //                userId,
        //                deliveryRequestCreatingRequest
        //            );
        //        switch (commonResponse.Status)
        //        {
        //            case 200:
        //            {
        //                //BackgroundJob.Enqueue(
        //                //    () =>
        //                //        _scheduledRouteService.UpdateScheduledRoutes(
        //                //            BusinessObject
        //                //                .EntityEnums
        //                //                .DeliveryType
        //                //                .DONATED_REQUEST_TO_BRANCH
        //                //        )
        //                //);
        //                return Ok(commonResponse);
        //            }
        //            case 400:
        //                return BadRequest(commonResponse);
        //            case 403:
        //                return StatusCode(403, commonResponse);
        //            default:
        //                return StatusCode(500, commonResponse);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(
        //            ex,
        //            $"An exception occurred in {nameof(DeliveryRequestsController)}."
        //        );
        //        return StatusCode(
        //            500,
        //            new CommonResponse { Status = 500, Message = internalServerErrorMsg }
        //        );
        //    }
        //}

        [Authorize(Roles = "SYSTEM_ADMIN,BRANCH_ADMIN")]
        [HttpGet]
        public async Task<IActionResult> GetDeliveryRequestAsync(
            int? pageSize,
            int? page,
            DateTime? startDate,
            DateTime? endDate,
            string? keyWord,
            string? address,
            Guid? itemId,
            DeliveryType? deliveryType,
            DeliveryRequestStatus? status,
            Guid? branchId
        )
        {
            string internalServerErrorMsg = _config[
                "ResponseMessages:CommonMsg:InternalServerErrorMsg"
            ];
            try
            {
                CommonResponse commonResponse = new CommonResponse();
                var token = HttpContext.Request.Headers["Authorization"]
                    .FirstOrDefault()
                    ?.Split(" ")
                    .Last();

                string userRoleName = "";
                Guid userId = Guid.Empty;

                if (token != null)
                {
                    userRoleName = _jwtService.GetRoleNameByJwtToken(token);
                    userId = _jwtService.GetUserIdByJwtToken(token);
                }

                if (userId != Guid.Empty && userRoleName == RoleEnum.SYSTEM_ADMIN.ToString())
                {
                    DeliveryFilterRequest deliveryFilterRequest = new DeliveryFilterRequest
                    {
                        Address = address,
                        StartDate = startDate,
                        EndDate = endDate,
                        DeliveryType = deliveryType,
                        ItemId = itemId,
                        KeyWord = keyWord,
                        Status = status,
                        BranchAdminId = null,
                        BranchId = null
                    };
                    commonResponse = await _deliveryRequestService.GetDeliveryRequestAsync(
                        page,
                        pageSize,
                        deliveryFilterRequest
                    );
                }
                else if (userId != Guid.Empty && userRoleName == RoleEnum.BRANCH_ADMIN.ToString())
                {
                    DeliveryFilterRequest deliveryFilterRequest = new DeliveryFilterRequest
                    {
                        Address = address,
                        StartDate = startDate,
                        EndDate = endDate,
                        DeliveryType = deliveryType,
                        ItemId = itemId,
                        KeyWord = keyWord,
                        Status = status,
                        BranchAdminId = userId,
                        BranchId = branchId
                    };
                    commonResponse = await _deliveryRequestService.GetDeliveryRequestAsync(
                        page,
                        pageSize,
                        deliveryFilterRequest
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
                _logger.LogError(
                    ex,
                    $"An exception occurred in {nameof(DeliveryRequestsController)}."
                );
                return StatusCode(
                    500,
                    new CommonResponse { Status = 500, Message = internalServerErrorMsg }
                );
            }
        }

        /// <summary>
        /// User update delivery request's status to next status of scheduled route. Use this api for:
        /// </summary>
        /// <remarks>
        /// Role: CONTRIBUTOR
        /// <br />
        /// - **IMPORT scheduled route**:
        ///     - when user arrived to pickup place
        /// - **EXPORT scheduled route**:
        ///     - when user arrived to delivery place
        /// </remarks>
        [HttpPut]
        [Authorize(Roles = "CONTRIBUTOR")]
        [Route("{deliveryRequestId}")]
        public async Task<IActionResult> UpdateNextStatusOfDeliveryRequest(Guid deliveryRequestId)
        {
            try
            {
                string jwtToken = Request.Headers["Authorization"]
                    .FirstOrDefault()
                    ?.Split(" ")
                    .Last()!;
                Guid userId = _jwtService.GetUserIdByJwtToken(jwtToken);
                CommonResponse commonResponse =
                    await _deliveryRequestService.UpdateNextStatusOfDeliveryRequestAsync(
                        userId,
                        deliveryRequestId
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
                    $"An exception occurred in {nameof(DeliveryRequestsController)}."
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
        /// User update received quantity of delivery items when they collect donated items in the delivery request
        /// </summary>
        [HttpPut]
        [Route("{deliveryRequestId}/delivery-items")]
        public async Task<IActionResult> UpdateDeliveryItemsOfDeliveryRequest(
            Guid deliveryRequestId,
            DeliveryItemsOfDeliveryRequestUpdatingRequest deliveryItemsOfDeliveryRequestUpdatingRequest
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
                    await _deliveryRequestService.UpdateDeliveryItemsOfDeliveryRequest(
                        userId,
                        deliveryRequestId,
                        deliveryItemsOfDeliveryRequestUpdatingRequest
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
                    $"An exception occurred in {nameof(DeliveryRequestsController)}."
                );
                return StatusCode(
                    500,
                    new CommonResponse { Status = 500, Message = internalServerErrorMsg }
                );
            }
        }

        /// <summary>
        /// CONTRIBUTOR update proof image of delivery request when give delivery items to charity unit
        /// </summary>
        [HttpPut]
        [Route("{deliveryRequestId}/proof-image")]
        [Authorize(Roles = "CONTRIBUTOR")]
        public async Task<IActionResult> UpdateProofImageOfDeliveryRequest(
            Guid deliveryRequestId,
            [FromBody] ProofImage proofImage
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
                    await _deliveryRequestService.UpdateProofImageOfDeliveryRequest(
                        userId,
                        deliveryRequestId,
                        proofImage.Link
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
                    $"An exception occurred in {nameof(DeliveryRequestsController)}."
                );
                return StatusCode(
                    500,
                    new CommonResponse { Status = 500, Message = internalServerErrorMsg }
                );
            }
        }

        ///// <summary>
        ///// Charity unit update finished delivery request
        ///// </summary>
        //[HttpPut]
        //[Route("{deliveryRequestId}/finished-delivery-request")]
        //[Authorize(Roles = "CHARITY_UNIT")]
        //public async Task<IActionResult> UpdateFinishedDeliveryRequestTypeBranchToCharityUnit(
        //    Guid deliveryRequestId,
        //    string proofImage
        //)
        //{
        //    string internalServerErrorMsg = _config[
        //        "ResponseMessages:CommonMsg:InternalServerErrorMsg"
        //    ];
        //    try
        //    {
        //        string jwtToken = Request.Headers["Authorization"]
        //            .FirstOrDefault()
        //            ?.Split(" ")
        //            .Last()!;
        //        Guid userId = _jwtService.GetUserIdByJwtToken(jwtToken);
        //        CommonResponse commonResponse =
        //            await _deliveryRequestService.UpdateFinishedDeliveryRequestTypeBranchToCharityUnitAsync(
        //                userId,
        //                deliveryRequestId
        //            );
        //        switch (commonResponse.Status)
        //        {
        //            case 200:
        //                return Ok(commonResponse);
        //            case 400:
        //                return BadRequest(commonResponse);
        //            case 403:
        //                return StatusCode(403, commonResponse);
        //            default:
        //                return StatusCode(500, commonResponse);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(
        //            ex,
        //            $"An exception occurred in {nameof(DeliveryRequestsController)}."
        //        );
        //        return StatusCode(
        //            500,
        //            new CommonResponse { Status = 500, Message = internalServerErrorMsg }
        //        );
        //    }
        //}

        /// <summary>
        /// SYSTEM_ADMIN get detail of any delivery request, BRANCH_ADMIN can only get their own branch's delivery request details, CONTRIBUTOR can only get finished delivery request of their own donated request, CHARITY can only get finished delivery request of their own aid request
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "SYSTEM_ADMIN,BRANCH_ADMIN,CONTRIBUTOR,CHARITY")]
        [Route("{deliveryRequestId}")]
        public async Task<IActionResult> GetDeliveryRequestDetails(Guid deliveryRequestId)
        {
            try
            {
                CommonResponse commonResponse = new CommonResponse();
                var token = HttpContext.Request.Headers["Authorization"]
                    .FirstOrDefault()
                    ?.Split(" ")
                    .Last();

                string userRoleName = "";
                Guid userId = Guid.Empty;

                if (token != null)
                {
                    userRoleName = _jwtService.GetRoleNameByJwtToken(token);
                    userId = _jwtService.GetUserIdByJwtToken(token);
                }

                if (userId != Guid.Empty && userRoleName == RoleEnum.SYSTEM_ADMIN.ToString())
                {
                    commonResponse = await _deliveryRequestService.GetDeliveryRequestDetailsAsync(
                        null,
                        deliveryRequestId
                    );
                }
                else if (userId != Guid.Empty && userRoleName == RoleEnum.BRANCH_ADMIN.ToString())
                {
                    commonResponse = await _deliveryRequestService.GetDeliveryRequestDetailsAsync(
                        userId,
                        deliveryRequestId
                    );
                }
                else if (userId != Guid.Empty && userRoleName == RoleEnum.CONTRIBUTOR.ToString())
                {
                    commonResponse =
                        await _deliveryRequestService.GetFinishedDeliveryRequestByIdOfDonatedRequestForUserAsync(
                            deliveryRequestId,
                            userId
                        );
                }
                else if (userId != Guid.Empty && userRoleName == RoleEnum.CHARITY.ToString())
                {
                    commonResponse =
                        await _deliveryRequestService.GetFinishedDeliveryRequestByIdOfAidRequestForCharityUnitAsync(
                            deliveryRequestId,
                            userId
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
                _logger.LogError(
                    ex,
                    $"An exception occurred in {nameof(DeliveryRequestsController)}."
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
        /// SYSTEM_ADMIN, BRANCH_ADMIN get detail of any finished delivery request
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "SYSTEM_ADMIN,BRANCH_ADMIN")]
        [Route("finished-delivery-request/{deliveryRequestId}")]
        public async Task<IActionResult> GetFinishedDeliveryRequestDetails(Guid deliveryRequestId)
        {
            try
            {
                CommonResponse commonResponse = new CommonResponse();

                commonResponse =
                    await _deliveryRequestService.GetFinishedDeliveryRequestByIdOfAidRequestForCharityUnitAsync(
                        deliveryRequestId,
                        null
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
                    $"An exception occurred in {nameof(DeliveryRequestsController)}."
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
        /// User get their delivery request
        /// </summary>
        /// <remarks>
        /// Role: CONTRIBUTOR
        /// <br />
        /// </remarks>
        [HttpGet]
        [Authorize(Roles = "CONTRIBUTOR")]
        [Route("contributor")]
        public async Task<IActionResult> GetDeliveryRequestForContributor(Guid deliveryId)
        {
            try
            {
                string jwtToken = Request.Headers["Authorization"]
                    .FirstOrDefault()
                    ?.Split(" ")
                    .Last()!;
                Guid userId = _jwtService.GetUserIdByJwtToken(jwtToken);
                CommonResponse commonResponse =
                    await _deliveryRequestService.GetDeliveryRequestDetailsByContributorIdsync(
                        userId,
                        deliveryId
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
                    $"An exception occurred in {nameof(DeliveryRequestsController)}."
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
        /// CONTRIBUTOR send report for delivery request when it is FINISHED, CHARITY send report for delivery request when it is FINISHED
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "CONTRIBUTOR,CHARITY")]
        [Route("{deliveryRequestId}/reports")]
        public async Task<IActionResult> SendReportByUserOrCharityUnit(
            Guid deliveryRequestId,
            ReportForUserOrCharityUnitRequest reportForUserOrCharityUnitRequest
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
                    await _deliveryRequestService.SendReportByUserOrCharityUnitAsync(
                        userId,
                        userRoleName,
                        deliveryRequestId,
                        reportForUserOrCharityUnitRequest
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
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    $"An exception occurred in controller {nameof(DeliveryRequestsController)}, method {nameof(SendReportByUserOrCharityUnit)}."
                );
                return StatusCode(
                    500,
                    new CommonResponse { Status = 500, Message = internalServerErrorMsg }
                );
            }
        }

        /// <summary>
        /// CONTRIBUTOR send report for delivery request type IMPORT with status SHIPPING or ARRIVED_PICKUP
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "CONTRIBUTOR")]
        [Route("{deliveryRequestId}/reports/contributor")]
        public async Task<IActionResult> SendReportByContributor(
            Guid deliveryRequestId,
            ReportForContributorRequest reportForContributorRequest
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
                    await _deliveryRequestService.SendReportByContributorAsync(
                        userId,
                        deliveryRequestId,
                        reportForContributorRequest
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
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    $"An exception occurred in controller {nameof(DeliveryRequestsController)}, method {nameof(SendReportByUserOrCharityUnit)}."
                );
                return StatusCode(
                    500,
                    new CommonResponse { Status = 500, Message = internalServerErrorMsg }
                );
            }
        }

        /// <summary>
        /// System get statistics of delivery request
        /// </summary>
        /// <remarks>
        /// Role: SYSTEM ADMIN
        /// <br />
        /// </remarks>
        [HttpGet]
        [Authorize(Roles = "SYSTEM_ADMIN,BRANCH_ADMIN")]
        [Route("statistics/all-status")]
        public async Task<IActionResult> CountDeliveryRequest(
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
                    commonResponse = await _deliveryRequestService.CountDeliveryRequestByAllStatus(
                        startDate,
                        endDate,
                        branchId,
                        userRoleName,
                        callerId
                    );
                }
                else
                {
                    commonResponse = await _deliveryRequestService.CountDeliveryRequestByAllStatus(
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
                _logger.LogError(
                    ex,
                    $"An exception occurred in {nameof(DeliveryRequestsController)}."
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
        /// System get statistics of delivery request
        /// </summary>
        /// <remarks>
        /// Role: SYSTEM ADMIN
        /// <br />
        /// </remarks>
        [HttpGet]
        [Authorize(Roles = "SYSTEM_ADMIN,BRANCH_ADMIN")]
        [Route("statistics")]
        public async Task<IActionResult> CountDeliveryRequestByStatus(
            DateTime startDate,
            DateTime endDate,
            DeliveryRequestStatus? status,
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
                    commonResponse = await _deliveryRequestService.CountDeliveryRequestByStatus(
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
                    commonResponse = await _deliveryRequestService.CountDeliveryRequestByStatus(
                        startDate,
                        endDate,
                        status,
                        timeFrame,
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
                _logger.LogError(
                    ex,
                    $"An exception occurred in {nameof(DeliveryRequestsController)}."
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
        /// Branch admin handle reported delivery request, can only handle delivery request has status REPORTED and update its to 0 (PENDING) or 9 (EXPIRED)
        /// </summary>
        [HttpPut]
        [Authorize(Roles = "BRANCH_ADMIN")]
        [Route("{deliveryRequestId}/reports")]
        public async Task<IActionResult> HandleReportedDeliveryRequest(
            Guid deliveryRequestId,
            DeliveryRequestStatus deliveryRequestStatus
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
                    await _deliveryRequestService.HandleReportedDeliveryRequestAsync(
                        userId,
                        deliveryRequestId,
                        deliveryRequestStatus
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
                    $"An exception occurred in {nameof(DeliveryRequestsController)}."
                );
                return StatusCode(
                    500,
                    new CommonResponse { Status = 500, Message = internalServerErrorMsg }
                );
            }
        }

        /// <summary>
        /// Branch admin cancel delivery request that is created by themself
        /// </summary>
        /// <remarks>
        /// Role: BRANCH_ADMIN
        /// <br />
        /// - **Type DONATED_REQUEST_TO_BRANCH**:
        ///     - can only cancel PENDING or ACCEPTED delivery request
        /// - **Type BRANCH_TO_AID_REQUEST**:
        ///     - can only cancel PENDING, ACCEPTED, SHIPPING, ARRIVED_PICKUP, COLLECTED or ARRIVED_DELIVERY delivery request
        /// - **Type BRANCH_TO_BRANCH**:
        ///     - can only cancel PENDING, ACCEPTED, SHIPPING, ARRIVED_PICKUP, COLLECTED or ARRIVED_DELIVERY delivery request
        /// </remarks>
        [HttpDelete]
        [Authorize(Roles = "BRANCH_ADMIN")]
        [Route("{deliveryRequestId}")]
        public async Task<IActionResult> CancelDeliveryRequest(
            Guid deliveryRequestId,
            string canceledReason
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
                    await _deliveryRequestService.CancelDeliveryRequestAsync(
                        deliveryRequestId,
                        canceledReason,
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
                    $"An exception occurred in {nameof(DeliveryRequestsController)}."
                );
                return StatusCode(
                    500,
                    new CommonResponse { Status = 500, Message = internalServerErrorMsg }
                );
            }
        }
    }
}
