using BusinessLogic.Utils.ExcelService;
using DataAccess.Entities;
using DataAccess.EntityEnums;
using DataAccess.Models.Responses;
using DataAccess.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Drawing.Printing;

namespace BusinessLogic.Services.Implements
{
    public class StockUpdatedHistoryDetailService : IStockUpdatedHistoryDetailService
    {
        private readonly IStockUpdatedHistoryDetailRepository _stockUpdatedHistoryDetailRepository;
        private readonly ILogger<StockUpdatedHistoryDetailService> _logger;
        private readonly IConfiguration _config;
        private readonly IItemRepository _itemRepository;
        private readonly IExcelService _excelService;
        private readonly IStockRepository _stockRepository;
        private readonly IStockUpdatedHistoryRepository _stockUpdatedHistoryRepository;
        private readonly IUserRepository _userRepository;
        private readonly IActivityRepository _activityRepository;
        private readonly IBranchRepository _branchRepository;

        public StockUpdatedHistoryDetailService(
            IStockUpdatedHistoryDetailRepository stockUpdatedHistoryDetailRepository,
            IConfiguration configuration,
            ILogger<StockUpdatedHistoryDetailService> logger,
            IItemRepository itemRepository,
            IExcelService excelService,
            IStockRepository stockRepository,
            IStockUpdatedHistoryRepository stockUpdatedHistoryRepository,
            IUserRepository userRepository,
            IActivityRepository activityRepository,
            IBranchRepository branchRepository
        )
        {
            _stockUpdatedHistoryDetailRepository = stockUpdatedHistoryDetailRepository;
            _logger = logger;
            _config = configuration;
            _itemRepository = itemRepository;
            _excelService = excelService;
            _stockRepository = stockRepository;
            _userRepository = userRepository;
            _activityRepository = activityRepository;
            _stockUpdatedHistoryRepository = stockUpdatedHistoryRepository;
            _branchRepository = branchRepository;
        }

        public async Task<CommonResponse> GetStockUpdateHistoryDetailsOfBranch(
            int? page,
            int? pageSize,
            Guid branchId,
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
                List<StockUpdatedHistoryDetail>? stockUpdatedHistoryDetails =
                    await _stockUpdatedHistoryDetailRepository.GetStockUpdateHistoryDetailsByBranchId(
                        branchId,
                        startDate,
                        endDate
                    );

                //if (stockUpdatedHistories != null && stockUpdatedHistories.Count > 0)
                //{
                //    Pagination pagination = new Pagination();
                //    pagination.PageSize = pageSize == null ? 10 : pageSize.Value;
                //    pagination.CurrentPage = page == null ? 1 : page.Value;
                //    pagination.Total = stockUpdatedHistories.Count;
                //    stockUpdatedHistories = stockUpdatedHistories
                //        .Skip((pagination.CurrentPage - 1) * pagination.PageSize)
                //        .Take(pagination.PageSize)
                //        .ToList();
                //    List<StockUpdateHistoryDetailResponse> stockUpdateHistories =
                //        new List<StockUpdateHistoryDetailResponse>();
                //    foreach (var a in stockUpdatedHistories)
                //    {
                //        StockUpdateHistoryDetailResponse response =
                //            new StockUpdateHistoryDetailResponse();
                //        response.CreatedDate = a.StockUpdatedHistory.CreatedDate;
                //        response.Quantity = a.Quantity;
                //        response.Type = a.StockUpdatedHistory.Type.ToString();

                //        response.Note = a.Note ?? string.Empty;
                //        response.Id = a.Id;
                //        if (a.StockId != null && a.Stock!.ActivityId != null)
                //        {
                //            response.ActivityName = a.Stock!.Activity!.Name;
                //        }

                //        if (a.DeliveryItem != null)
                //        {
                //            if (a.DeliveryItem.DonatedItem != null)
                //            {
                //                Item? item = await _itemRepository.FindItemByIdAsync(
                //                    a.DeliveryItem.DonatedItem.ItemId
                //                );
                //                if (item != null)
                //                {
                //                    response.Unit =
                //                        item.ItemTemplate.Unit.Name.ToString() ?? string.Empty;
                //                    response.Name = item.ItemTemplate.Name;
                //                    response.AttributeValues = item.ItemAttributeValues
                //                        .Select(itav => itav.AttributeValue.Value)
                //                        .ToList();
                //                }
                //                response.PickUpPoint =
                //                    a.DeliveryItem.DeliveryRequest.DonatedRequest?.User.Name
                //                    ?? "No Name";
                //                response.DeliveryPoint =
                //                    a.DeliveryItem.DeliveryRequest.Branch.Name ?? string.Empty;
                //            }

                //            if (a.DeliveryItem.AidItem != null)
                //            {
                //                Item? item = await _itemRepository.FindItemByIdAsync(
                //                    a.DeliveryItem.AidItem.ItemId
                //                );
                //                if (item != null)
                //                {
                //                    response.Unit =
                //                        item.ItemTemplate.Unit.Name.ToString() ?? string.Empty;
                //                    response.Name = item.ItemTemplate.Name;
                //                    response.AttributeValues = item.ItemAttributeValues
                //                        .Select(itav => itav.AttributeValue.Value)
                //                        .ToList();
                //                }

                //                response.DeliveryPoint =
                //                    a.DeliveryItem.DeliveryRequest.AidRequest?.CharityUnit?.Name
                //                    ?? "No Name";
                //                response.PickUpPoint =
                //                    a.DeliveryItem.DeliveryRequest.Branch.Name ?? string.Empty;
                //            }
                //        }
                //        else if (a.Stock != null && a.DeliveryItem == null)
                //        {
                //            Item? item = await _itemRepository.FindItemByIdAsync(a.Stock.ItemId);
                //            if (item != null)
                //            {
                //                response.Unit =
                //                    item.ItemTemplate.Unit.Name.ToString() ?? string.Empty;
                //                response.Name = item.ItemTemplate.Name;
                //                response.AttributeValues = item.ItemAttributeValues
                //                    .Select(itav => itav.AttributeValue.Value)
                //                    .ToList();
                //            }
                //            if (a.Stock.UserId != null)
                //            {
                //                User? user = await _userRepository.FindUserByIdAsync(
                //                    a.Stock.UserId.Value
                //                );

                //                if (
                //                    a.Stock.UserId != null
                //                    && a.AidRequestId != null
                //                    && user != null
                //                    && a.StockUpdatedHistory.Type == StockUpdatedHistoryType.EXPORT
                //                )
                //                {
                //                    response.PickUpPoint = a.StockUpdatedHistory.Branch.Name;
                //                    response.DeliveryPoint = user.Name;
                //                }
                //                else if (
                //                    a.Stock.UserId != null
                //                    && user != null
                //                    && a.StockUpdatedHistory.Type == StockUpdatedHistoryType.IMPORT
                //                    && a.Stock.ActivityId != null
                //                )
                //                {
                //                    response.PickUpPoint = a.StockUpdatedHistory.Branch.Name;
                //                    response.DeliveryPoint = user.Name;
                //                }
                //                else
                //                {
                //                    response.PickUpPoint = "";
                //                    response.DeliveryPoint = "";
                //                }
                //            }
                //        }
                //        stockUpdateHistories.Add(response);
                //    }
                //    commonResponse.Data = stockUpdateHistories;
                //    commonResponse.Pagination = pagination;
                //}
                Pagination pagination = new Pagination();
                pagination.PageSize = pageSize == null ? 10 : pageSize.Value;
                pagination.CurrentPage = page == null ? 1 : page.Value;
                pagination.Total = stockUpdatedHistoryDetails.Count;
                stockUpdatedHistoryDetails = stockUpdatedHistoryDetails
                    .Skip((pagination.CurrentPage - 1) * pagination.PageSize)
                    .Take(pagination.PageSize)
                    .ToList();

                return new CommonResponse
                {
                    Status = 200,
                    Pagination = pagination,
                    Data = stockUpdatedHistoryDetails.Select(
                        suhd =>
                            new StockUpdateHistoryDetailResponse
                            {
                                Id = suhd.Id,
                                Quantity = suhd.Quantity,
                                Name = suhd.Stock!.Item.ItemTemplate.Name,
                                AttributeValues = suhd.Stock!.Item.ItemAttributeValues
                                    .Select(iav => iav.AttributeValue.Value)
                                    .ToList(),
                                Unit = suhd.Stock!.Item.ItemTemplate.Unit.Name,
                                PickUpPoint = suhd.StockUpdatedHistory.Branch.Name,
                                DeliveryPoint =
                                    suhd.AidRequestId != null
                                        ? (
                                            suhd.AidRequest!.CharityUnitId != null
                                                ? suhd.AidRequest!.CharityUnit!.Name
                                                : null
                                        )
                                        : null,
                                CreatedDate = suhd.StockUpdatedHistory.CreatedDate,
                                Type = suhd.StockUpdatedHistory.Type.ToString(),
                                Note = suhd.StockUpdatedHistory.Note,
                                DonorName =
                                    suhd.Stock!.UserId != null ? suhd.Stock!.User!.Name : null,
                                ActivityName =
                                    suhd.Stock!.ActivityId != null
                                        ? suhd.Stock!.Activity!.Name
                                        : null
                            }
                    )
                };
            }
            catch (Exception ex)
            {
                string className = nameof(StockUpdatedHistoryDetailService);
                string methodName = nameof(GetStockUpdateHistoryDetailsOfBranch);
                _logger.LogError(
                    ex,
                    "An error occurred in {ClassName}.{MethodName}: {ErrorMessage}",
                    className,
                    methodName,
                    ex.Message
                );
                commonResponse.Message = internalServerErrorMsg;
                commonResponse.Status = 500;
            }
            return commonResponse;
        }

        public async Task<CommonResponse> ExportStockUpdateHistoryDetailsOfBranch(
            Guid branchId,
            DateTime startDate,
            DateTime endDate
        )
        {
            CommonResponse commonResponse = new CommonResponse();
            string internalServerErrorMsg = _config[
                "ResponseMessages:CommonMsg:InternalServerErrorMsg"
            ];
            try
            {
                List<StockUpdatedHistoryDetail>? stockUpdatedHistoryDetails =
                    await _stockUpdatedHistoryDetailRepository.GetStockUpdateHistoryDetailsByBranchId(
                        branchId,
                        startDate,
                        endDate
                    );

                List<StockUpdateHistoryDetailResponse> stockUpdateHistoryDetailResponses =
                    stockUpdatedHistoryDetails
                        .Select(
                            suhd =>
                                new StockUpdateHistoryDetailResponse
                                {
                                    Id = suhd.Id,
                                    Quantity = suhd.Quantity,
                                    Name = suhd.Stock!.Item.ItemTemplate.Name,
                                    AttributeValues = suhd.Stock!.Item.ItemAttributeValues
                                        .Select(iav => iav.AttributeValue.Value)
                                        .ToList(),
                                    Unit = suhd.Stock!.Item.ItemTemplate.Unit.Name,
                                    PickUpPoint = suhd.StockUpdatedHistory.Branch.Name,
                                    DeliveryPoint =
                                        suhd.AidRequestId != null
                                            ? (
                                                suhd.AidRequest!.CharityUnitId != null
                                                    ? suhd.AidRequest!.CharityUnit!.Name
                                                    : null
                                            )
                                            : null,
                                    CreatedDate = suhd.StockUpdatedHistory.CreatedDate,
                                    Type = suhd.StockUpdatedHistory.Type.ToString(),
                                    Note = suhd.StockUpdatedHistory.Note,
                                    DonorName =
                                        suhd.Stock!.UserId != null ? suhd.Stock!.User!.Name : null,
                                    ActivityName =
                                        suhd.Stock!.ActivityId != null
                                            ? suhd.Stock!.Activity!.Name
                                            : null
                                }
                        )
                        .ToList();

                var rs = await _excelService.CreateExcelFile(stockUpdateHistoryDetailResponses);
                commonResponse.Data = rs;
                commonResponse.Status = 200;
            }
            catch (Exception ex)
            {
                string className = nameof(StockUpdatedHistoryDetailService);
                string methodName = nameof(ExportStockUpdateHistoryDetailsOfBranch);
                _logger.LogError(
                    ex,
                    "An error occurred in {ClassName}.{MethodName}: {ErrorMessage}",
                    className,
                    methodName,
                    ex.Message
                );
                commonResponse.Message = internalServerErrorMsg;
                commonResponse.Status = 500;
            }
            return commonResponse;
        }

        public async Task<CommonResponse> GetStockUpdateHistoryDetailsOfActivity(
            int? page,
            int? pageSize,
            Guid activityId,
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
                List<StockUpdatedHistoryDetail> stockUpdatedHistoryDetails =
                    await _stockUpdatedHistoryDetailRepository.GetStockUpdateHistoryDetailsByActivityId(
                        activityId,
                        startDate,
                        endDate
                    );

                //if (stockUpdatedHistories != null && stockUpdatedHistories.Count > 0)
                //{
                //    Pagination pagination = new Pagination();
                //    pagination.PageSize = pageSize == null ? 10 : pageSize.Value;
                //    pagination.CurrentPage = page == null ? 1 : page.Value;
                //    pagination.Total = stockUpdatedHistories.Count;
                //    stockUpdatedHistories = stockUpdatedHistories
                //        .Skip((pagination.CurrentPage - 1) * pagination.PageSize)
                //        .Take(pagination.PageSize)
                //        .ToList();
                //    List<StockUpdateHistoryDetailResponse> stockUpdateHistories =
                //        new List<StockUpdateHistoryDetailResponse>();
                //    foreach (var a in stockUpdatedHistories)
                //    {
                //        StockUpdateHistoryDetailResponse response =
                //            new StockUpdateHistoryDetailResponse();
                //        response.CreatedDate = a.StockUpdatedHistory.CreatedDate;
                //        if (a.StockId != null && a.Stock!.ActivityId != null)
                //        {
                //            response.ActivityName = a.Stock!.Activity!.Name;
                //        }

                //        response.Quantity = a.Quantity;
                //        response.Type = a.StockUpdatedHistory.Type.ToString();

                //        response.Note = a.Note ?? string.Empty;
                //        if (a.DeliveryItem != null)
                //        {
                //            if (a.DeliveryItem.DonatedItem != null)
                //            {
                //                Item? item = await _itemRepository.FindItemByIdAsync(
                //                    a.DeliveryItem.DonatedItem.ItemId
                //                );
                //                if (item != null)
                //                {
                //                    response.Unit =
                //                        item.ItemTemplate.Unit.Name.ToString() ?? string.Empty;
                //                    response.Name = item.ItemTemplate.Name;
                //                    response.AttributeValues = item.ItemAttributeValues
                //                        .Select(itav => itav.AttributeValue.Value)
                //                        .ToList();
                //                }
                //                response.PickUpPoint =
                //                    a.DeliveryItem.DeliveryRequest.DonatedRequest?.User.Name
                //                    ?? "No Name";
                //                response.DeliveryPoint =
                //                    a.DeliveryItem.DeliveryRequest.Branch.Name ?? string.Empty;
                //            }

                //            if (a.DeliveryItem.AidItem != null)
                //            {
                //                Item? item = await _itemRepository.FindItemByIdAsync(
                //                    a.DeliveryItem.AidItem.ItemId
                //                );
                //                if (item != null)
                //                {
                //                    response.Unit =
                //                        item.ItemTemplate.Unit.Name.ToString() ?? string.Empty;
                //                    response.Name = item.ItemTemplate.Name;
                //                    response.AttributeValues = item.ItemAttributeValues
                //                        .Select(itav => itav.AttributeValue.Value)
                //                        .ToList();
                //                }

                //                response.DeliveryPoint =
                //                    a.DeliveryItem.DeliveryRequest.AidRequest?.CharityUnit?.Name
                //                    ?? "No Name";
                //                response.PickUpPoint =
                //                    a.DeliveryItem.DeliveryRequest.Branch.Name ?? string.Empty;
                //            }
                //        }
                //        else if (a.Stock != null && a.DeliveryItem == null)
                //        {
                //            Item? item = await _itemRepository.FindItemByIdAsync(a.Stock.ItemId);
                //            if (item != null)
                //            {
                //                response.Unit =
                //                    item.ItemTemplate.Unit.Name.ToString() ?? string.Empty;
                //                response.Name = item.ItemTemplate.Name;
                //                response.AttributeValues = item.ItemAttributeValues
                //                    .Select(itav => itav.AttributeValue.Value)
                //                    .ToList();
                //            }
                //            if (a.Stock.UserId != null)
                //            {
                //                User? user = await _userRepository.FindUserByIdAsync(
                //                    a.Stock.UserId.Value
                //                );

                //                if (
                //                    a.Stock.UserId != null
                //                    && a.AidRequestId != null
                //                    && user != null
                //                    && a.StockUpdatedHistory.Type == StockUpdatedHistoryType.EXPORT
                //                )
                //                {
                //                    response.PickUpPoint = a.StockUpdatedHistory.Branch.Name;
                //                    response.DeliveryPoint = user.Name;
                //                }
                //                else if (
                //                    a.Stock.UserId != null
                //                    && user != null
                //                    && a.StockUpdatedHistory.Type == StockUpdatedHistoryType.IMPORT
                //                    && a.Stock.ActivityId != null
                //                )
                //                {
                //                    response.PickUpPoint = a.StockUpdatedHistory.Branch.Name;
                //                    response.DeliveryPoint = user.Name;
                //                }
                //                else
                //                {
                //                    response.PickUpPoint = "";
                //                    response.DeliveryPoint = "";
                //                }
                //            }
                //        }
                ////        stockUpdateHistories.Add(response);
                //    }
                //    commonResponse.Data = stockUpdateHistories;
                //    commonResponse.Pagination = pagination;
                //}
                //commonResponse.Status = 200;

                Pagination pagination = new Pagination();
                pagination.PageSize = pageSize == null ? 10 : pageSize.Value;
                pagination.CurrentPage = page == null ? 1 : page.Value;
                pagination.Total = stockUpdatedHistoryDetails.Count;
                stockUpdatedHistoryDetails = stockUpdatedHistoryDetails
                    .Skip((pagination.CurrentPage - 1) * pagination.PageSize)
                    .Take(pagination.PageSize)
                    .ToList();

                return new CommonResponse
                {
                    Status = 200,
                    Pagination = pagination,
                    Data = stockUpdatedHistoryDetails.Select(
                        suhd =>
                            new StockUpdateHistoryDetailResponse
                            {
                                Id = suhd.Id,
                                Quantity = suhd.Quantity,
                                Name = suhd.Stock!.Item.ItemTemplate.Name,
                                AttributeValues = suhd.Stock!.Item.ItemAttributeValues
                                    .Select(iav => iav.AttributeValue.Value)
                                    .ToList(),
                                Unit = suhd.Stock!.Item.ItemTemplate.Unit.Name,
                                PickUpPoint = suhd.StockUpdatedHistory.Branch.Name,
                                DeliveryPoint =
                                    suhd.AidRequestId != null
                                        ? (
                                            suhd.AidRequest!.CharityUnitId != null
                                                ? suhd.AidRequest!.CharityUnit!.Name
                                                : null
                                        )
                                        : null,
                                CreatedDate = suhd.StockUpdatedHistory.CreatedDate,
                                Type = suhd.StockUpdatedHistory.Type.ToString(),
                                Note = suhd.StockUpdatedHistory.Note,
                                DonorName =
                                    suhd.Stock!.UserId != null ? suhd.Stock!.User!.Name : null,
                                ActivityName =
                                    suhd.Stock!.ActivityId != null
                                        ? suhd.Stock!.Activity!.Name
                                        : null
                            }
                    )
                };
            }
            catch (Exception ex)
            {
                string className = nameof(StockUpdatedHistoryDetailService);
                string methodName = nameof(GetStockUpdateHistoryDetailsOfActivity);
                _logger.LogError(
                    ex,
                    "An error occurred in {ClassName}.{MethodName}: {ErrorMessage}",
                    className,
                    methodName,
                    ex.Message
                );
                commonResponse.Message = internalServerErrorMsg;
                commonResponse.Status = 500;
            }
            return commonResponse;
        }

        public async Task<CommonResponse> ExportStockUpdateHistoryDetailsOfActivity(
            Guid branchId,
            DateTime startDate,
            DateTime endDate
        )
        {
            CommonResponse commonResponse = new CommonResponse();
            string internalServerErrorMsg = _config[
                "ResponseMessages:CommonMsg:InternalServerErrorMsg"
            ];
            try
            {
                List<StockUpdatedHistoryDetail> stockUpdatedHistoryDetails =
                    await _stockUpdatedHistoryDetailRepository.GetStockUpdateHistoryDetailsByActivityId(
                        branchId,
                        startDate,
                        endDate
                    );

                List<StockUpdateHistoryDetailResponse> stockUpdateHistoryDetailResponses =
                    stockUpdatedHistoryDetails
                        .Select(
                            suhd =>
                                new StockUpdateHistoryDetailResponse
                                {
                                    Id = suhd.Id,
                                    Quantity = suhd.Quantity,
                                    Name = suhd.Stock!.Item.ItemTemplate.Name,
                                    AttributeValues = suhd.Stock!.Item.ItemAttributeValues
                                        .Select(iav => iav.AttributeValue.Value)
                                        .ToList(),
                                    Unit = suhd.Stock!.Item.ItemTemplate.Unit.Name,
                                    PickUpPoint = suhd.StockUpdatedHistory.Branch.Name,
                                    DeliveryPoint =
                                        suhd.AidRequestId != null
                                            ? (
                                                suhd.AidRequest!.CharityUnitId != null
                                                    ? suhd.AidRequest!.CharityUnit!.Name
                                                    : null
                                            )
                                            : null,
                                    CreatedDate = suhd.StockUpdatedHistory.CreatedDate,
                                    Type = suhd.StockUpdatedHistory.Type.ToString(),
                                    Note = suhd.StockUpdatedHistory.Note,
                                    DonorName =
                                        suhd.Stock!.UserId != null ? suhd.Stock!.User!.Name : null,
                                    ActivityName =
                                        suhd.Stock!.ActivityId != null
                                            ? suhd.Stock!.Activity!.Name
                                            : null
                                }
                        )
                        .ToList();

                var rs = await _excelService.CreateExcelFile(stockUpdateHistoryDetailResponses);
                commonResponse.Data = rs;
                commonResponse.Status = 200;
            }
            catch (Exception ex)
            {
                string className = nameof(StockUpdatedHistoryDetailService);
                string methodName = nameof(ExportStockUpdateHistoryDetailsOfActivity);
                _logger.LogError(
                    ex,
                    "An error occurred in {ClassName}.{MethodName}: {ErrorMessage}",
                    className,
                    methodName,
                    ex.Message
                );
                commonResponse.Message = internalServerErrorMsg;
                commonResponse.Status = 500;
            }
            return commonResponse;
        }

        public async Task<CommonResponse> GetStockUpdateHistoryByCharityUnit(
            int? page,
            int? pageSize,
            Guid charityUnitId,
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
                List<StockUpdatedHistoryDetail> stockUpdatedHistoryDetails =
                    await _stockUpdatedHistoryDetailRepository.GetStockUpdateHistoryByCharityUnitId(
                        charityUnitId,
                        startDate,
                        endDate
                    );

                //if (stockUpdatedHistories != null && stockUpdatedHistories.Count > 0)
                //{
                //    Pagination pagination = new Pagination();
                //    pagination.PageSize = pageSize == null ? 10 : pageSize.Value;
                //    pagination.CurrentPage = page == null ? 1 : page.Value;
                //    pagination.Total = stockUpdatedHistories.Count;
                //    stockUpdatedHistories = stockUpdatedHistories
                //        .Skip((pagination.CurrentPage - 1) * pagination.PageSize)
                //        .Take(pagination.PageSize)
                //        .ToList();
                //    List<StockUpdateHistoryDetailResponse> stockUpdateHistories =
                //        new List<StockUpdateHistoryDetailResponse>();
                //    foreach (var a in stockUpdatedHistories)
                //    {
                //        StockUpdateHistoryDetailResponse response =
                //            new StockUpdateHistoryDetailResponse();
                //        response.CreatedDate = a.StockUpdatedHistory.CreatedDate;
                //        if (a.StockId != null && a.Stock!.ActivityId != null)
                //        {
                //            response.ActivityName = a.Stock!.Activity!.Name;
                //        }

                //        response.Quantity = a.Quantity;

                //        response.Type = a.StockUpdatedHistory.Type.ToString();

                //        response.Id = a.Id;
                //        response.Note = a.Note ?? string.Empty;
                //        if (a.DeliveryItem != null)
                //        {
                //            if (a.DeliveryItem.DonatedItem != null)
                //            {
                //                Item? item = await _itemRepository.FindItemByIdAsync(
                //                    a.DeliveryItem.DonatedItem.ItemId
                //                );
                //                if (item != null)
                //                {
                //                    response.Unit =
                //                        item.ItemTemplate.Unit.Name.ToString() ?? string.Empty;
                //                    response.Name = item.ItemTemplate.Name;
                //                    response.AttributeValues = item.ItemAttributeValues
                //                        .Select(itav => itav.AttributeValue.Value)
                //                        .ToList();
                //                }
                //                response.PickUpPoint =
                //                    a.DeliveryItem.DeliveryRequest.DonatedRequest?.User.Name
                //                    ?? "No Name";
                //                response.DeliveryPoint =
                //                    a.DeliveryItem.DeliveryRequest.Branch.Name ?? string.Empty;
                //            }

                //            if (a.DeliveryItem.AidItem != null)
                //            {
                //                Item? item = await _itemRepository.FindItemByIdAsync(
                //                    a.DeliveryItem.AidItem.ItemId
                //                );
                //                if (item != null)
                //                {
                //                    response.Unit =
                //                        item.ItemTemplate.Unit.Name.ToString() ?? string.Empty;
                //                    response.Name = item.ItemTemplate.Name;
                //                    response.AttributeValues = item.ItemAttributeValues
                //                        .Select(itav => itav.AttributeValue.Value)
                //                        .ToList();
                //                }

                //                response.DeliveryPoint =
                //                    a.DeliveryItem.DeliveryRequest.AidRequest?.CharityUnit?.Name
                //                    ?? "No Name";
                //                response.PickUpPoint =
                //                    a.DeliveryItem.DeliveryRequest.Branch.Name ?? string.Empty;
                //            }
                //        }
                //        else if (a.Stock != null && a.DeliveryItem == null)
                //        {
                //            Item? item = await _itemRepository.FindItemByIdAsync(a.Stock.ItemId);
                //            if (item != null)
                //            {
                //                response.Unit =
                //                    item.ItemTemplate.Unit.Name.ToString() ?? string.Empty;
                //                response.Name = item.ItemTemplate.Name;
                //                response.AttributeValues = item.ItemAttributeValues
                //                    .Select(itav => itav.AttributeValue.Value)
                //                    .ToList();
                //            }
                //            if (a.Stock.UserId != null)
                //            {
                //                User? user = await _userRepository.FindUserByIdAsync(
                //                    a.Stock.UserId.Value
                //                );

                //                if (
                //                    a.Stock.UserId != null
                //                    && a.AidRequestId != null
                //                    && user != null
                //                    && a.StockUpdatedHistory.Type == StockUpdatedHistoryType.EXPORT
                //                )
                //                {
                //                    response.PickUpPoint = a.StockUpdatedHistory.Branch.Name;
                //                    response.DeliveryPoint = user.Name;
                //                }
                //                else if (
                //                    a.Stock.UserId != null
                //                    && user != null
                //                    && a.StockUpdatedHistory.Type == StockUpdatedHistoryType.IMPORT
                //                    && a.Stock.ActivityId != null
                //                )
                //                {
                //                    response.PickUpPoint = a.StockUpdatedHistory.Branch.Name;
                //                    response.DeliveryPoint = user.Name;
                //                }
                //                else
                //                {
                //                    response.PickUpPoint = "";
                //                    response.DeliveryPoint = "";
                //                }
                //            }
                //        }
                //        stockUpdateHistories.Add(response);
                //    }
                //    commonResponse.Data = stockUpdateHistories;
                //    commonResponse.Pagination = pagination;
                //}
                //commonResponse.Status = 200;

                Pagination pagination = new Pagination();
                pagination.PageSize = pageSize == null ? 10 : pageSize.Value;
                pagination.CurrentPage = page == null ? 1 : page.Value;
                pagination.Total = stockUpdatedHistoryDetails.Count;
                stockUpdatedHistoryDetails = stockUpdatedHistoryDetails
                    .Skip((pagination.CurrentPage - 1) * pagination.PageSize)
                    .Take(pagination.PageSize)
                    .ToList();

                return new CommonResponse
                {
                    Status = 200,
                    Pagination = pagination,
                    Data = stockUpdatedHistoryDetails.Select(
                        suhd =>
                            new StockUpdateHistoryDetailResponse
                            {
                                Id = suhd.Id,
                                Quantity = suhd.Quantity,
                                Name = suhd.Stock!.Item.ItemTemplate.Name,
                                AttributeValues = suhd.Stock!.Item.ItemAttributeValues
                                    .Select(iav => iav.AttributeValue.Value)
                                    .ToList(),
                                Unit = suhd.Stock!.Item.ItemTemplate.Unit.Name,
                                PickUpPoint = suhd.StockUpdatedHistory.Branch.Name,
                                DeliveryPoint =
                                    suhd.AidRequestId != null
                                        ? (
                                            suhd.AidRequest!.CharityUnitId != null
                                                ? suhd.AidRequest!.CharityUnit!.Name
                                                : null
                                        )
                                        : null,
                                CreatedDate = suhd.StockUpdatedHistory.CreatedDate,
                                Type = suhd.StockUpdatedHistory.Type.ToString(),
                                Note = suhd.StockUpdatedHistory.Note,
                                DonorName =
                                    suhd.Stock!.UserId != null ? suhd.Stock!.User!.Name : null,
                                ActivityName =
                                    suhd.Stock!.ActivityId != null
                                        ? suhd.Stock!.Activity!.Name
                                        : null
                            }
                    )
                };
            }
            catch (Exception ex)
            {
                string className = nameof(StockUpdatedHistoryDetailService);
                string methodName = nameof(GetStockUpdateHistoryByCharityUnit);
                _logger.LogError(
                    ex,
                    "An error occurred in {ClassName}.{MethodName}: {ErrorMessage}",
                    className,
                    methodName,
                    ex.Message
                );
                commonResponse.Message = internalServerErrorMsg;
                commonResponse.Status = 500;
            }
            return commonResponse;
        }

        //public async Task<CommonResponse> CreateStockUpdateHistoryDetails(
        //    List<StockUpdateHistoryRequest> request,
        //    Guid UserId,
        //    Guid Id
        //)
        //{
        //    CommonResponse commonResponse = new CommonResponse();
        //    string internalServerErrorMsg = _config[
        //        "ResponseMessages:CommonMsg:InternalServerErrorMsg"
        //    ];
        //    try
        //    {
        //        StockUpdatedHistory stockUpdatedHistory = new StockUpdatedHistory();
        //        stockUpdatedHistory.CreatedDate = SettedUpDateTime.GetCurrentVietNamTime();
        //        List<StockUpdatedHistoryDetail> stockUpdatedHistoryDetails =
        //            new List<StockUpdatedHistoryDetail>();
        //        foreach (var a in request)
        //        {
        //            Stock? stock = await _stockRepository.GetCurrentValidStocksById(Id);
        //            if (stock != null)
        //            {
        //                if (stock.Quantity < a.Quantity)
        //                {
        //                    commonResponse.Status = 400;
        //                    commonResponse.Message =
        //                        "Không thể xuất kho vì số lượng"
        //                        + stock.Item.ItemTemplate.Name
        //                        + "còn lại không đủ.";
        //                    return commonResponse;
        //                }
        //                stock.Quantity = stock.Quantity - a.Quantity;

        //                int updateResult = await _stockRepository.UpdateStockAsync(stock);
        //                if (updateResult < 0)
        //                    throw new Exception();
        //                stockUpdatedHistory.BranchId = stock.BranchId;
        //            }
        //            StockUpdatedHistoryDetail stockUpdatedHistoryDetail =
        //                new StockUpdatedHistoryDetail();
        //            stockUpdatedHistoryDetail.Quantity = a.Quantity;
        //            stockUpdatedHistoryDetail.Note = a.Note;

        //            stockUpdatedHistoryDetails.Add(stockUpdatedHistoryDetail);
        //        }
        //        stockUpdatedHistory.StockUpdatedHistoryDetails = stockUpdatedHistoryDetails;
        //        int rs = await _stockUpdatedHistoryRepository.AddStockUpdatedHistoryAsync(
        //            stockUpdatedHistory
        //        );
        //    }
        //    catch (Exception ex)
        //    {
        //        string className = nameof(StockUpdatedHistoryDetailService);
        //        string methodName = nameof(GetStockUpdateHistoryDetailsOfBranch);
        //        _logger.LogError(
        //            ex,
        //            "An error occurred in {ClassName}.{MethodName}: {ErrorMessage}",
        //            className,
        //            methodName,
        //            ex.Message
        //        );
        //        commonResponse.Message = internalServerErrorMsg;
        //        commonResponse.Status = 500;
        //    }
        //    return commonResponse;
        //}

        public async Task<CommonResponse> GetStockUpdateHistoryDetailsForAdmin(
            int? page,
            int? pageSize,
            Guid? branchId,
            Guid? branchAdminId,
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
                Branch? branch = new Branch();
                if (branchAdminId != null && branchId == null)
                {
                    branch = await _branchRepository.FindBranchByBranchAdminIdAsync(
                        branchAdminId.Value
                    );
                }
                else if (branchAdminId == null && branchId != null)
                {
                    branch = await _branchRepository.FindBranchByIdAsync(branchId.Value);
                }
                List<StockUpdatedHistoryDetail>? stockUpdatedHistoryDetails =
                    await _stockUpdatedHistoryDetailRepository.GetStockUpdateHistoryDetailsForAdmin(
                        charityUnitId,
                        branch == null ? null : branch.Id,
                        type,
                        startDate,
                        endDate
                    );

                //if (stockUpdatedHistories != null && stockUpdatedHistories.Count > 0)
                //{
                //    Pagination pagination = new Pagination();
                //    pagination.PageSize = pageSize == null ? 10 : pageSize.Value;
                //    pagination.CurrentPage = page == null ? 1 : page.Value;
                //    pagination.Total = stockUpdatedHistories.Count;
                //    stockUpdatedHistories = stockUpdatedHistories
                //        .Skip((pagination.CurrentPage - 1) * pagination.PageSize)
                //        .Take(pagination.PageSize)
                //        .ToList();
                //    List<StockUpdateHistoryDetailResponse> stockUpdateHistories =
                //        new List<StockUpdateHistoryDetailResponse>();
                //    foreach (var a in stockUpdatedHistories)
                //    {
                //        StockUpdateHistoryDetailResponse response =
                //            new StockUpdateHistoryDetailResponse();
                //        response.CreatedDate = a.StockUpdatedHistory.CreatedDate;
                //        response.Quantity = a.Quantity;
                //        response.Type = a.StockUpdatedHistory.Type.ToString();

                //        response.Note = a.Note ?? string.Empty;
                //        response.Id = a.Id;
                //        if (a.StockId != null && a.Stock!.ActivityId != null)
                //        {
                //            response.ActivityName = a.Stock!.Activity!.Name;
                //        }

                //        if (a.DeliveryItem != null)
                //        {
                //            if (a.DeliveryItem.DonatedItem != null)
                //            {
                //                Item? item = await _itemRepository.FindItemByIdAsync(
                //                    a.DeliveryItem.DonatedItem.ItemId
                //                );
                //                if (item != null)
                //                {
                //                    response.Unit =
                //                        item.ItemTemplate.Unit.Name.ToString() ?? string.Empty;
                //                    response.Name = item.ItemTemplate.Name;
                //                    response.AttributeValues = item.ItemAttributeValues
                //                        .Select(itav => itav.AttributeValue.Value)
                //                        .ToList();
                //                }
                //                response.PickUpPoint =
                //                    a.DeliveryItem.DeliveryRequest.DonatedRequest?.User.Name
                //                    ?? "No Name";
                //                response.DeliveryPoint =
                //                    a.DeliveryItem.DeliveryRequest.Branch.Name ?? string.Empty;
                //            }

                //            if (a.DeliveryItem.AidItem != null)
                //            {
                //                Item? item = await _itemRepository.FindItemByIdAsync(
                //                    a.DeliveryItem.AidItem.ItemId
                //                );
                //                if (item != null)
                //                {
                //                    response.Unit =
                //                        item.ItemTemplate.Unit.Name.ToString() ?? string.Empty;
                //                    response.Name = item.ItemTemplate.Name;
                //                    response.AttributeValues = item.ItemAttributeValues
                //                        .Select(itav => itav.AttributeValue.Value)
                //                        .ToList();
                //                }

                //                response.DeliveryPoint =
                //                    a.DeliveryItem.DeliveryRequest.AidRequest?.CharityUnit?.Name
                //                    ?? "No Name";
                //                response.PickUpPoint =
                //                    a.DeliveryItem.DeliveryRequest.Branch.Name ?? string.Empty;
                //            }
                //        }
                //        else if (a.Stock != null && a.DeliveryItem == null)
                //        {
                //            Item? item = await _itemRepository.FindItemByIdAsync(a.Stock.ItemId);
                //            if (item != null)
                //            {
                //                response.Unit =
                //                    item.ItemTemplate.Unit.Name.ToString() ?? string.Empty;
                //                response.Name = item.ItemTemplate.Name;
                //                response.AttributeValues = item.ItemAttributeValues
                //                    .Select(itav => itav.AttributeValue.Value)
                //                    .ToList();
                //            }
                //            if (a.Stock.UserId != null)
                //            {
                //                User? user = await _userRepository.FindUserByIdAsync(
                //                    a.Stock.UserId.Value
                //                );

                //                if (
                //                    a.Stock.UserId != null
                //                    && a.AidRequestId != null
                //                    && user != null
                //                    && a.StockUpdatedHistory.Type == StockUpdatedHistoryType.EXPORT
                //                )
                //                {
                //                    response.PickUpPoint = a.StockUpdatedHistory.Branch.Name;
                //                    response.DeliveryPoint = user.Name;
                //                }
                //                else if (
                //                    a.Stock.UserId != null
                //                    && user != null
                //                    && a.StockUpdatedHistory.Type == StockUpdatedHistoryType.IMPORT
                //                    && a.Stock.ActivityId != null
                //                )
                //                {
                //                    response.PickUpPoint = a.StockUpdatedHistory.Branch.Name;
                //                    response.DeliveryPoint = user.Name;
                //                }
                //                else
                //                {
                //                    response.PickUpPoint = "";
                //                    response.DeliveryPoint = "";
                //                }
                //            }
                //        }
                //        stockUpdateHistories.Add(response);
                //    }
                //    commonResponse.Data = stockUpdateHistories;
                //    commonResponse.Pagination = pagination;
                //}
                Pagination pagination = new Pagination();
                pagination.PageSize = pageSize == null ? 10 : pageSize.Value;
                pagination.CurrentPage = page == null ? 1 : page.Value;
                pagination.Total = stockUpdatedHistoryDetails.Count;
                stockUpdatedHistoryDetails = stockUpdatedHistoryDetails
                    .Skip((pagination.CurrentPage - 1) * pagination.PageSize)
                    .Take(pagination.PageSize)
                    .ToList();

                return new CommonResponse
                {
                    Status = 200,
                    Pagination = pagination,
                    Data = stockUpdatedHistoryDetails.Select(
                        suhd =>
                            new StockUpdateHistoryDetailResponse
                            {
                                Id = suhd.Id,
                                Quantity = suhd.Quantity,
                                Name = suhd.Stock!.Item.ItemTemplate.Name,
                                AttributeValues = suhd.Stock!.Item.ItemAttributeValues
                                    .Select(iav => iav.AttributeValue.Value)
                                    .ToList(),
                                Unit = suhd.Stock!.Item.ItemTemplate.Unit.Name,
                                PickUpPoint = suhd.StockUpdatedHistory.Branch.Name,
                                DeliveryPoint =
                                    suhd.AidRequestId != null
                                        ? (
                                            suhd.AidRequest!.CharityUnitId != null
                                                ? suhd.AidRequest!.CharityUnit!.Name
                                                : null
                                        )
                                        : null,
                                CreatedDate = suhd.StockUpdatedHistory.CreatedDate,
                                Type = suhd.StockUpdatedHistory.Type.ToString(),
                                Note = suhd.StockUpdatedHistory.Note,
                                DonorName =
                                    suhd.Stock!.UserId != null ? suhd.Stock!.User!.Name : null,
                                ActivityName =
                                    suhd.Stock!.ActivityId != null
                                        ? suhd.Stock!.Activity!.Name
                                        : null
                            }
                    )
                };
            }
            catch (Exception ex)
            {
                string className = nameof(StockUpdatedHistoryDetailService);
                string methodName = nameof(GetStockUpdateHistoryDetailsOfBranch);
                _logger.LogError(
                    ex,
                    "An error occurred in {ClassName}.{MethodName}: {ErrorMessage}",
                    className,
                    methodName,
                    ex.Message
                );
                commonResponse.Message = internalServerErrorMsg;
                commonResponse.Status = 500;
            }
            return commonResponse;
        }

        public async Task<CommonResponse> ExportStockUpdateHistoryDetailsForAdmin(
            Guid? branchId,
            Guid? branchAdminId,
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
                Branch? branch = new Branch();
                if (branchAdminId != null && branchId == null)
                {
                    branch = await _branchRepository.FindBranchByBranchAdminIdAsync(
                        branchAdminId.Value
                    );
                }
                else if (branchAdminId == null && branchId != null)
                {
                    branch = await _branchRepository.FindBranchByIdAsync(branchId.Value);
                }

                List<StockUpdatedHistoryDetail>? stockUpdatedHistoryDetails =
                    await _stockUpdatedHistoryDetailRepository.GetStockUpdateHistoryDetailsForAdmin(
                        charityUnitId,
                        branch == null ? null : branch.Id,
                        type,
                        startDate,
                        endDate
                    );

                List<StockUpdateHistoryDetailResponse> stockUpdateHistoryDetailResponses =
                    stockUpdatedHistoryDetails
                        .Select(
                            suhd =>
                                new StockUpdateHistoryDetailResponse
                                {
                                    Id = suhd.Id,
                                    Quantity = suhd.Quantity,
                                    Name = suhd.Stock!.Item.ItemTemplate.Name,
                                    AttributeValues = suhd.Stock!.Item.ItemAttributeValues
                                        .Select(iav => iav.AttributeValue.Value)
                                        .ToList(),
                                    Unit = suhd.Stock!.Item.ItemTemplate.Unit.Name,
                                    PickUpPoint = suhd.StockUpdatedHistory.Branch.Name,
                                    DeliveryPoint =
                                        suhd.AidRequestId != null
                                            ? (
                                                suhd.AidRequest!.CharityUnitId != null
                                                    ? suhd.AidRequest!.CharityUnit!.Name
                                                    : null
                                            )
                                            : null,
                                    CreatedDate = suhd.StockUpdatedHistory.CreatedDate,
                                    Type = suhd.StockUpdatedHistory.Type.ToString(),
                                    Note = suhd.StockUpdatedHistory.Note,
                                    DonorName =
                                        suhd.Stock!.UserId != null ? suhd.Stock!.User!.Name : null,
                                    ActivityName =
                                        suhd.Stock!.ActivityId != null
                                            ? suhd.Stock!.Activity!.Name
                                            : null
                                }
                        )
                        .ToList();

                var rs = await _excelService.CreateExcelFile(stockUpdateHistoryDetailResponses);
                commonResponse.Data = rs;
                commonResponse.Status = 200;
            }
            catch (Exception ex)
            {
                string className = nameof(StockUpdatedHistoryDetailService);
                string methodName = nameof(GetStockUpdateHistoryDetailsOfBranch);
                _logger.LogError(
                    ex,
                    "An error occurred in {ClassName}.{MethodName}: {ErrorMessage}",
                    className,
                    methodName,
                    ex.Message
                );
                commonResponse.Message = internalServerErrorMsg;
                commonResponse.Status = 500;
            }
            return commonResponse;
        }

        public async Task<CommonResponse> GetStockUpdateHistoryOfContributor(
            int? page,
            int? pageSize,
            Guid userId,
            DateTime? startDate,
            DateTime? endDate
        )
        {
            List<StockUpdatedHistoryDetail> stockUpdatedHistoryDetails =
                await _stockUpdatedHistoryDetailRepository.GetStockUpdateHistoryOfContributor(
                    userId,
                    startDate,
                    endDate
                );

            Pagination pagination = new Pagination();
            pagination.PageSize = pageSize == null ? 10 : pageSize.Value;
            pagination.CurrentPage = page == null ? 1 : page.Value;
            pagination.Total = stockUpdatedHistoryDetails.Count;
            stockUpdatedHistoryDetails = stockUpdatedHistoryDetails
                .Skip((pagination.CurrentPage - 1) * pagination.PageSize)
                .Take(pagination.PageSize)
                .ToList();

            return new CommonResponse
            {
                Status = 200,
                Pagination = pagination,
                Data = stockUpdatedHistoryDetails.Select(
                    suhd =>
                        new StockUpdateHistoryDetailResponse
                        {
                            Id = suhd.Id,
                            Quantity = suhd.Quantity,
                            Name = suhd.Stock!.Item.ItemTemplate.Name,
                            AttributeValues = suhd.Stock!.Item.ItemAttributeValues
                                .Select(iav => iav.AttributeValue.Value)
                                .ToList(),
                            Unit = suhd.Stock!.Item.ItemTemplate.Unit.Name,
                            PickUpPoint = suhd.StockUpdatedHistory.Branch.Name,
                            DeliveryPoint =
                                suhd.AidRequestId != null
                                    ? (
                                        suhd.AidRequest!.CharityUnitId != null
                                            ? suhd.AidRequest!.CharityUnit!.Name
                                            : null
                                    )
                                    : null,
                            CreatedDate = suhd.StockUpdatedHistory.CreatedDate,
                            Type = suhd.StockUpdatedHistory.Type.ToString(),
                            Note = suhd.StockUpdatedHistory.Note,
                            DonorName = suhd.Stock!.UserId != null ? suhd.Stock!.User!.Name : null,
                            ActivityName =
                                suhd.Stock!.ActivityId != null ? suhd.Stock!.Activity!.Name : null
                        }
                )
            };
        }
    }
}
