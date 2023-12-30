using BusinessLogic.Services;
using BusinessLogic.Utils.SecurityServices;
using DataAccess.Models.Requests;
using DataAccess.Models.Responses;
using DataAccess.ModelsEnum;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodDonationDeliveryManagementAPI.Controllers
{
    [Route("stocks")]
    [ApiController]
    public class StocksController : ControllerBase
    {
        private readonly IStockService _stockService;
        private readonly ILogger<ActivitiesController> _logger;
        private readonly IConfiguration _config;
        private readonly IJwtService _jwtService;

        public StocksController(
            IStockService stockService,
            ILogger<ActivitiesController> logger,
            IConfiguration config,
            IJwtService jwtService
        )
        {
            _stockService = stockService;
            _logger = logger;
            _config = config;
            _jwtService = jwtService;
        }

        [Authorize(Roles = "BRANCH_ADMIN")]
        [HttpPost]
        public async Task<IActionResult> GetStockByItemIdAndScheduledTimes(
            StockGettingRequest stockGettingRequest
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
                    await _stockService.GetStockByItemIdAndScheduledTimesAsync(
                        userId,
                        stockGettingRequest
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
                    $"An exception occurred in controller {nameof(StocksController)}, method {nameof(GetStockByItemIdAndScheduledTimes)}."
                );
                return StatusCode(
                    500,
                    new CommonResponse { Status = 500, Message = internalServerErrorMsg }
                );
            }
        }

        [Authorize(Roles = "BRANCH_ADMIN,SYSTEM_ADMIN")]
        [HttpGet("/branch/{branchId}/items/{itemId}")]
        public async Task<IActionResult> GetStockByItemIdAndBranchId(
            Guid branchId,
            Guid itemId,
            int? pageSize,
            int? page
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
                    commonResponse = await _stockService.GetStockByItemIdAndBranchId(
                        itemId,
                        branchId,
                        page,
                        pageSize,
                        null
                    );
                }
                else
                {
                    commonResponse = await _stockService.GetStockByItemIdAndBranchId(
                        itemId,
                        branchId,
                        page,
                        pageSize,
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
                    $"An exception occurred in controller {nameof(StocksController)}, method {nameof(GetStockByItemIdAndScheduledTimes)}."
                );
                return StatusCode(
                    500,
                    new CommonResponse { Status = 500, Message = internalServerErrorMsg }
                );
            }
        }

        [Authorize(Roles = "BRANCH_ADMIN,SYSTEM_ADMIN")]
        [HttpPost("available")]
        public async Task<IActionResult> GetAvailableStockByItemIdAndBranchId(
            NumberOfAvalibleItemRequest request
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
                    commonResponse = await _stockService.GetListAvalableByItemsId(request, null);
                }
                else
                {
                    commonResponse = await _stockService.GetListAvalableByItemsId(request, userId);
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
                    $"An exception occurred in controller {nameof(StocksController)}, method {nameof(GetStockByItemIdAndScheduledTimes)}."
                );
                return StatusCode(
                    500,
                    new CommonResponse { Status = 500, Message = internalServerErrorMsg }
                );
            }
        }
    }
}
