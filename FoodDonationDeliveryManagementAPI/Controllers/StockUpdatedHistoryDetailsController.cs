using BusinessLogic.Services;
using BusinessLogic.Utils.SecurityServices;
using DataAccess.EntityEnums;
using DataAccess.Models.Responses;
using DataAccess.ModelsEnum;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodDonationDeliveryManagementAPI.Controllers
{
    [Route("stock-updated-history-details")]
    [ApiController]
    public class StockUpdatedHistoryDetailsController : ControllerBase
    {
        private readonly IStockUpdatedHistoryDetailService _stockUpdatedHistoryDetailService;
        private readonly ILogger<ActivitiesController> _logger;
        private readonly IConfiguration _config;
        private readonly IJwtService _jwtService;

        public StockUpdatedHistoryDetailsController(
            IStockUpdatedHistoryDetailService stockUpdatedHistoryDetailService,
            ILogger<ActivitiesController> logger,
            IConfiguration config,
            IJwtService jwtService
        )
        {
            _stockUpdatedHistoryDetailService = stockUpdatedHistoryDetailService;
            _logger = logger;
            _config = config;
            _jwtService = jwtService;
        }

        /// <summary>
        /// Lấy sao kê của branch
        /// </summary>
        /// <br/>
        /// <remarks>
        ///</remarks>
        [HttpGet("branch/{branchId}")]
        public async Task<IActionResult> GetStockUpdateHistoryByBranchId(
            int? page,
            int? pageSize,
            Guid branchId,
            DateTime? startDate,
            DateTime? endDate
        )
        {
            string internalServerErrorMsg = _config[
                "ResponseMessages:CommonMsg:InternalServerErrorMsg"
            ];
            try
            {
                CommonResponse commonResponse =
                    await _stockUpdatedHistoryDetailService.GetStockUpdateHistoryDetailsOfBranch(
                        page,
                        pageSize,
                        branchId,
                        startDate,
                        endDate
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
                    $"An exception occurred in controller {nameof(StockUpdatedHistoryDetailsController)}, method {nameof(GetStockUpdateHistoryByBranchId)}."
                );
                return StatusCode(
                    500,
                    new CommonResponse { Status = 500, Message = internalServerErrorMsg }
                );
            }
        }

        /// <summary>
        /// Lấy sao kê của branch bằng excel
        /// </summary>
        /// <br/>
        /// <remarks>
        ///</remarks>

        [HttpGet("branch/{branchId}/file")]
        public async Task<IActionResult> ExportStockUpdateHistoryByBracnhId(
            Guid branchId,
            DateTime startDate,
            DateTime endDate
        )
        {
            string internalServerErrorMsg = _config[
                "ResponseMessages:CommonMsg:InternalServerErrorMsg"
            ];
            try
            {
                CommonResponse commonResponse =
                    await _stockUpdatedHistoryDetailService.ExportStockUpdateHistoryDetailsOfBranch(
                        branchId,
                        startDate,
                        endDate
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
                    $"An exception occurred in controller {nameof(StockUpdatedHistoryDetailsController)}, method {nameof(ExportStockUpdateHistoryByBracnhId)}."
                );
                return StatusCode(
                    500,
                    new CommonResponse { Status = 500, Message = internalServerErrorMsg }
                );
            }
        }

        /// <summary>
        /// Lấy sao kê của activity
        /// </summary>
        /// <br/>
        /// <remarks>
        ///</remarks>

        [HttpGet("activity/{activityId}")]
        public async Task<IActionResult> GetStockUpdateHistoryByActivityId(
            int? page,
            int? pageSize,
            Guid activityId,
            DateTime? startDate,
            DateTime? endDate
        )
        {
            string internalServerErrorMsg = _config[
                "ResponseMessages:CommonMsg:InternalServerErrorMsg"
            ];
            try
            {
                CommonResponse commonResponse =
                    await _stockUpdatedHistoryDetailService.GetStockUpdateHistoryDetailsOfActivity(
                        page,
                        pageSize,
                        activityId,
                        startDate,
                        endDate
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
                    $"An exception occurred in controller {nameof(StockUpdatedHistoryDetailsController)}, method {nameof(GetStockUpdateHistoryByActivityId)}."
                );
                return StatusCode(
                    500,
                    new CommonResponse { Status = 500, Message = internalServerErrorMsg }
                );
            }
        }

        /// <summary>
        /// Lấy sao kê bằng excel của activity
        /// </summary>
        /// <br/>
        /// <remarks>
        ///</remarks>

        [HttpGet("activity/{activityId}/file")]
        public async Task<IActionResult> ExportStockUpdateHistoryByActivityId(
            Guid activityId,
            DateTime startDate,
            DateTime endDate
        )
        {
            string internalServerErrorMsg = _config[
                "ResponseMessages:CommonMsg:InternalServerErrorMsg"
            ];
            try
            {
                CommonResponse commonResponse =
                    await _stockUpdatedHistoryDetailService.ExportStockUpdateHistoryDetailsOfActivity(
                        activityId,
                        startDate,
                        endDate
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
                    $"An exception occurred in controller {nameof(StockUpdatedHistoryDetailsController)}, method {nameof(ExportStockUpdateHistoryByBracnhId)}."
                );
                return StatusCode(
                    500,
                    new CommonResponse { Status = 500, Message = internalServerErrorMsg }
                );
            }
        }

        /// <summary>
        /// Lấy sao kê của charity unit
        /// </summary>
        /// <br/>
        /// <remarks>
        ///</remarks>
        [HttpGet("charity-unit/{charityUnitId}")]
        public async Task<IActionResult> GetStockUpdateHistoryByCharityUnitId(
            int? page,
            int? pageSize,
            Guid charityUnitId,
            DateTime? startDate,
            DateTime? endDate
        )
        {
            string internalServerErrorMsg = _config[
                "ResponseMessages:CommonMsg:InternalServerErrorMsg"
            ];
            try
            {
                CommonResponse commonResponse =
                    await _stockUpdatedHistoryDetailService.GetStockUpdateHistoryByCharityUnit(
                        page,
                        pageSize,
                        charityUnitId,
                        startDate,
                        endDate
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
                    $"An exception occurred in controller {nameof(StockUpdatedHistoryDetailsController)}, method {nameof(GetStockUpdateHistoryByBranchId)}."
                );
                return StatusCode(
                    500,
                    new CommonResponse { Status = 500, Message = internalServerErrorMsg }
                );
            }
        }

        /// <summary>
        /// Lấy sao kê của bản thân người quyên góp
        /// </summary>
        /// <br/>
        /// <remarks>
        ///</remarks>
        [HttpGet("user")]
        [Authorize(Roles = "CONTRIBUTOR")]
        public async Task<IActionResult> GetStockUpdateHistoryOfContributor(
            int? page,
            int? pageSize,
            DateTime? startDate,
            DateTime? endDate
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
                    await _stockUpdatedHistoryDetailService.GetStockUpdateHistoryOfContributor(
                        page,
                        pageSize,
                        userId,
                        startDate,
                        endDate
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
                    $"An exception occurred in controller {nameof(StockUpdatedHistoryDetailsController)}, method {nameof(GetStockUpdateHistoryByBranchId)}."
                );
                return StatusCode(
                    500,
                    new CommonResponse { Status = 500, Message = internalServerErrorMsg }
                );
            }
        }

        [Authorize(Roles = "BRANCH_ADMIN,SYSTEM_ADMIN")]
        [HttpGet("admin")]
        public async Task<IActionResult> GetStockUpdateHistoryByCharityUnitId(
            int? page,
            int? pageSize,
            Guid? branchId,
            Guid? charityUnitId,
            StockUpdatedHistoryType? type,
            DateTime? startDate,
            DateTime? endDate
        )
        {
            CommonResponse commonResponse = new CommonResponse();
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
                string role = _jwtService.GetRoleNameByJwtToken(jwtToken);
                if (role == RoleEnum.SYSTEM_ADMIN.ToString())
                {
                    commonResponse =
                        await _stockUpdatedHistoryDetailService.GetStockUpdateHistoryDetailsForAdmin(
                            page,
                            pageSize,
                            branchId,
                            null,
                            charityUnitId,
                            type,
                            startDate,
                            endDate
                        );
                }
                else
                {
                    commonResponse =
                        await _stockUpdatedHistoryDetailService.GetStockUpdateHistoryDetailsForAdmin(
                            page,
                            pageSize,
                            null,
                            userId,
                            charityUnitId,
                            type,
                            startDate,
                            endDate
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
                    $"An exception occurred in controller {nameof(StocksController)}, method {nameof(GetStockUpdateHistoryByCharityUnitId)}."
                );
                return StatusCode(
                    500,
                    new CommonResponse { Status = 500, Message = internalServerErrorMsg }
                );
            }
        }

        [Authorize(Roles = "BRANCH_ADMIN,SYSTEM_ADMIN")]
        [HttpGet("admin/export")]
        public async Task<IActionResult> ExportStockUpdateHistory(
            Guid? branchId,
            Guid? charityUnitId,
            StockUpdatedHistoryType? type,
            DateTime? startDate,
            DateTime? endDate
        )
        {
            CommonResponse commonResponse = new CommonResponse();
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
                string role = _jwtService.GetRoleNameByJwtToken(jwtToken);
                if (role == RoleEnum.SYSTEM_ADMIN.ToString())
                {
                    commonResponse =
                        await _stockUpdatedHistoryDetailService.ExportStockUpdateHistoryDetailsForAdmin(
                            branchId,
                            null,
                            charityUnitId,
                            type,
                            startDate,
                            endDate
                        );
                }
                else
                {
                    commonResponse =
                        await _stockUpdatedHistoryDetailService.ExportStockUpdateHistoryDetailsForAdmin(
                            null,
                            userId,
                            charityUnitId,
                            type,
                            startDate,
                            endDate
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
                    $"An exception occurred in controller {nameof(StocksController)}, method {nameof(GetStockUpdateHistoryByCharityUnitId)}."
                );
                return StatusCode(
                    500,
                    new CommonResponse { Status = 500, Message = internalServerErrorMsg }
                );
            }
        }
    }
}
