using BusinessLogic.Services;
using BusinessLogic.Utils.SecurityServices;
using DataAccess.Models.Requests;
using DataAccess.Models.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodDonationDeliveryManagementAPI.Controllers
{
    [Route("stock-updated-histories")]
    [ApiController]
    public class StockUpdatedHistoriesController : ControllerBase
    {
        private readonly IStockUpdatedHistoryService _stockUpdatedHistoryService;
        private readonly ILogger<StockUpdatedHistoriesController> _logger;
        private readonly IConfiguration _config;
        private readonly IJwtService _jwtService;

        public StockUpdatedHistoriesController(
            IStockUpdatedHistoryService stockUpdatedHistoryService,
            ILogger<StockUpdatedHistoriesController> logger,
            IConfiguration config,
            IJwtService jwtService
        )
        {
            _stockUpdatedHistoryService = stockUpdatedHistoryService;
            _logger = logger;
            _config = config;
            _jwtService = jwtService;
        }

        [Authorize(Roles = "BRANCH_ADMIN")]
        [HttpPost("direct-donate")]
        public async Task<IActionResult> CreateStockUpdateHistoryWhenUserDirectlyDonate(
            StockUpdateForUserDirectDonateRequest request
        )
        {
            string internalServerErrorMsg = _config[
                "ResponseMessages:CommonMsg:InternalServerErrorMsg"
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
                CommonResponse commonResponse =
                    await _stockUpdatedHistoryService.CreateStockUpdateHistoryWhenUserDirectlyDonate(
                        request,
                        Guid.Parse(userSub!)
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
                    $"An exception occurred in controller {nameof(StocksController)}, method {nameof(CreateStockUpdateHistoryWhenUserDirectlyDonate)}."
                );
                return StatusCode(
                    500,
                    new CommonResponse { Status = 500, Message = internalServerErrorMsg }
                );
            }
        }

        /// <summary>
        /// branch admin direact import stock
        /// </summary>
        [Authorize(Roles = "BRANCH_ADMIN")]
        [HttpPost("import")]
        public async Task<IActionResult> CreateStockUpdateHistoryWhenAdminDirectlyImport(
            StockUpdateForImportingByBranchAdmin request
        )
        {
            string internalServerErrorMsg = _config[
                "ResponseMessages:CommonMsg:InternalServerErrorMsg"
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
                CommonResponse commonResponse =
                    await _stockUpdatedHistoryService.CreateStockUpdateHistoryWhenBranchAdminImport(
                        request,
                        Guid.Parse(userSub!)
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
                    $"An exception occurred in controller {nameof(StocksController)}, method {nameof(CreateStockUpdateHistoryWhenUserDirectlyDonate)}."
                );
                return StatusCode(
                    500,
                    new CommonResponse { Status = 500, Message = internalServerErrorMsg }
                );
            }
        }

        /// <summary>
        /// branch admin export current available stocks by item ids and their quantities for selfshipping aid request
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "BRANCH_ADMIN")]
        [Route("stock-updated-history-type-export-by-items")]
        public async Task<IActionResult> CreateStockUpdatedHistoryTypeExportByItems(
            StockUpdatedHistoryTypeExportByItemsCreatingRequest updatedHistoryTypeExportByItemsCreatingRequest
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
                    await _stockUpdatedHistoryService.CreateStockUpdatedHistoryTypeExportByItemsAsync(
                        userId,
                        updatedHistoryTypeExportByItemsCreatingRequest
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
                    $"An exception occurred in controller {nameof(StockUpdatedHistoriesController)}, method {CreateStockUpdatedHistoryTypeExportByItems}."
                );
                return StatusCode(
                    500,
                    new CommonResponse { Status = 500, Message = internalServerErrorMsg }
                );
            }
        }

        /// <summary>
        /// branch admin export expired stocks by stock ids and their quantities
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "BRANCH_ADMIN")]
        [Route("stock-updated-history-type-export-by-stocks")]
        public async Task<IActionResult> CreateStockUpdatedHistoryTypeExportByStocks(
            StockUpdatedHistoryTypeExportByStocksCreatingRequest updatedHistoryTypeExportByStocksCreatingRequest
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
                    await _stockUpdatedHistoryService.CreateStockUpdatedHistoryTypeExportByStocksAsync(
                        userId,
                        updatedHistoryTypeExportByStocksCreatingRequest
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
                    $"An exception occurred in controller {nameof(StockUpdatedHistoriesController)}, method {CreateStockUpdatedHistoryTypeExportByStocks}."
                );
                return StatusCode(
                    500,
                    new CommonResponse { Status = 500, Message = internalServerErrorMsg }
                );
            }
        }

        /// <summary>
        /// Charity unit get stock updated hostory detail for their self shipping aid request; BRANCH_ADMIN, SYSTEM_ADMIN get stock updated hostory detail for any self shipping aid request
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "CHARITY,BRANCH_ADMIN,SYSTEM_ADMIN")]
        [Route("{stockUpdatedHistoryId}")]
        public async Task<IActionResult> GetStockUpdatedHistoryOfSelfShippingAidRequestForCharityUnit(
            Guid stockUpdatedHistoryId
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
                    await _stockUpdatedHistoryService.GetStockUpdatedHistoryOfSelfShippingAidRequestForCharityUnit(
                        stockUpdatedHistoryId,
                        userId,
                        userRoleName
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
                    $"An exception occurred in controller {nameof(StockUpdatedHistoriesController)}, method {nameof(GetStockUpdatedHistoryOfSelfShippingAidRequestForCharityUnit)}."
                );
                return StatusCode(
                    500,
                    new CommonResponse { Status = 500, Message = internalServerErrorMsg }
                );
            }
        }
    }
}
