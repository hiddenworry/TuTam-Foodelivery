using BusinessLogic.Utils.Notification.Implements;
using DataAccess.Entities;
using DataAccess.EntityEnums;
using DataAccess.Models.Requests;
using DataAccess.Models.Requests.ModelBinders;
using DataAccess.Models.Responses;
using DataAccess.Repositories;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BusinessLogic.Services.Implements
{
    public class StockService : IStockService
    {
        private readonly IStockRepository _stockRepository;
        private readonly IConfiguration _config;
        private readonly IBranchRepository _branchRepository;
        private readonly IDeliveryItemRepository _deliveryItemRepository;
        private readonly IItemRepository _itemRepository;
        private readonly ILogger<StockService> _logger;
        private readonly IHubContext<NotificationSignalSender> _hubContext;
        private readonly INotificationRepository _notificationRepository;

        public StockService(
            IStockRepository stockRepository,
            IConfiguration config,
            IBranchRepository branchRepository,
            IDeliveryItemRepository deliveryItemRepository,
            IItemRepository itemRepository,
            ILogger<StockService> logger,
            IHubContext<NotificationSignalSender> hubContext,
            INotificationRepository notificationRepository
        )
        {
            _stockRepository = stockRepository;
            _config = config;
            _branchRepository = branchRepository;
            _deliveryItemRepository = deliveryItemRepository;
            _itemRepository = itemRepository;
            _logger = logger;
            _hubContext = hubContext;
            _notificationRepository = notificationRepository;
        }

        public async Task<CommonResponse> GetStockByItemIdAndScheduledTimesAsync(
            Guid userId,
            StockGettingRequest stockGettingRequest
        )
        {
            try
            {
                Branch? branch = await _branchRepository.FindBranchByBranchAdminIdAsync(userId);
                if (branch == null)
                {
                    return new CommonResponse
                    {
                        Status = 403,
                        Message = _config["ResponseMessages:BranchMsg:BranchNotFoundMsg"]
                    };
                }
                else if (branch.Status == BranchStatus.INACTIVE)
                {
                    return new CommonResponse
                    {
                        Status = 403,
                        Message = _config["ResponseMessages:BranchMsg:InactiveBranchMsg"]
                    };
                }

                Item? item = await _itemRepository.FindItemByIdAsync(stockGettingRequest.ItemId);

                if (item == null)
                    return new CommonResponse
                    {
                        Status = 400,
                        Message = _config["ResponseMessages:ItemMsg:ItemNotFoundMsg"]
                    };

                DateTime tmp = GetEndDateTimeFromScheduledTime(
                    GetLastAvailabeScheduledTime(stockGettingRequest.ScheduledTimes)!
                );

                DateTime endDateTimeOfLastScheduledtime = new DateTime(
                    tmp.Year,
                    tmp.Month,
                    tmp.Day
                );

                //stock hiện có
                List<Stock> stocks =
                    await _stockRepository.GetCurrentValidStocksByItemIdAndBranchId(
                        item.Id,
                        branch.Id
                    );
                double currentStock = stocks
                    .Where(
                        s =>
                            new DateTime(
                                s.ExpirationDate.Year,
                                s.ExpirationDate.Month,
                                s.ExpirationDate.Day
                            ) >= endDateTimeOfLastScheduledtime.AddDays(1)
                    )
                    .Sum(s => s.Quantity);

                //stock không khả dụng
                List<DeliveryItem> deliveryItems =
                    await _deliveryItemRepository.GetPendingDeliveryItemsByItemIdAndBranchIdOfAidDeliveryRequest(
                        item.Id,
                        branch.Id
                    );
                double pendingStock = deliveryItems
                //.Where(
                //    (di) =>
                //    {
                //        DateTime endOfLastScheduledTimeOfPendingDeliveryRequest =
                //            GetEndDateTimeFromScheduledTime(
                //                GetLastAvailabeScheduledTime(
                //                    JsonConvert.DeserializeObject<List<ScheduledTime>>(
                //                        di.DeliveryRequest.ScheduledTimes
                //                    )!
                //                )!
                //            );
                //        return new DateTime(
                //                endOfLastScheduledTimeOfPendingDeliveryRequest.Year,
                //                endOfLastScheduledTimeOfPendingDeliveryRequest.Month,
                //                endOfLastScheduledTimeOfPendingDeliveryRequest.Day
                //            ) >= endDateTimeOfLastScheduledtime;
                //    }
                //)
                .Sum(s => s.Quantity);

                //stock khả dụng
                currentStock = currentStock - pendingStock;

                return new CommonResponse
                {
                    Status = 200,
                    Message = _config[
                        "ResponseMessages:DeliveryRequestMsg:CreateDeliveryRequestsSuccessMsg"
                    ],
                    Data = new TotalStockOfAnItemResponse
                    {
                        Id = item.Id,
                        Name = GetFullNameOfItem(item),
                        Image = item.Image,
                        Note = item.Note,
                        EstimatedExpirationDays = item.EstimatedExpirationDays,
                        MaximumTransportVolume = item.MaximumTransportVolume,
                        Unit = item.ItemTemplate.Unit.Name,
                        TotalStock = currentStock > 0 ? currentStock : 0
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    $"An exception occurred in service {nameof(StockService)}, method {nameof(GetStockByItemIdAndScheduledTimesAsync)}."
                );
            }
            return new CommonResponse
            {
                Status = 500,
                Message = _config["ResponseMessages:CommonMsg:InternalServerErrorMsg"]
            };
        }

        private string GetFullNameOfItem(Item item)
        {
            return $"{item.ItemTemplate.Name}"
                + (
                    item.ItemAttributeValues.Count > 0
                        ? $" {string.Join(", ", item.ItemAttributeValues.Select(iav => iav.AttributeValue.Value))}"
                        : ""
                );
        }

        private DateTime GetEndDateTimeFromScheduledTime(ScheduledTime scheduledTime)
        {
            DateOnly day = DateOnly.Parse(scheduledTime.Day);
            TimeOnly endTime = TimeOnly.Parse(scheduledTime.EndTime);
            return day.ToDateTime(endTime);
        }

        private ScheduledTime? GetLastAvailabeScheduledTime(List<ScheduledTime> scheduledTimes)
        {
            return scheduledTimes
                .Where(
                    st =>
                        GetEndDateTimeFromScheduledTime(st)
                        > SettedUpDateTime.GetCurrentVietNamTime()
                )
                .MaxBy(st => GetEndDateTimeFromScheduledTime(st));
        }

        public async Task<CommonResponse> GetStockByItemIdAndBranchId(
            Guid itemId,
            Guid branchId,
            int? page,
            int? pageSize,
            Guid? branchAdminId
        )
        {
            CommonResponse commonResponse = new CommonResponse();
            try
            {
                Item? item = await _itemRepository.FindItemByIdAsync(itemId);

                if (branchAdminId != null)
                {
                    Branch? branch = await _branchRepository.FindBranchByIdAsync(branchId);
                    if (branch == null || branch.BranchAdminId != branchAdminId)
                    {
                        commonResponse.Status = 403;
                        commonResponse.Message = "Bạn không có quyền thực hiện hành động này.";
                        return commonResponse;
                    }
                }

                List<StockDetailsResponse> currentStocks = new List<StockDetailsResponse>();

                if (item != null)
                {
                    List<Stock>? stocks = await _stockRepository.GetStocksByItemIdAndBranchId(
                        itemId,
                        branchId
                    );
                    if (stocks != null && stocks.Count > 0)
                    {
                        Pagination pagination = new Pagination();
                        pagination.PageSize = pageSize == null ? 10 : pageSize.Value;
                        pagination.CurrentPage = page == null ? 1 : page.Value;
                        pagination.Total = stocks.Count;
                        double total = stocks.Sum(a => a.Quantity);

                        List<DeliveryItem> deliveryItems =
                            await _deliveryItemRepository.GetPendingDeliveryItemsByItemIdAndBranchIdOfAidDeliveryRequest(
                                itemId,
                                branchId
                            );

                        double totalItemAvailable = total;

                        foreach (DeliveryItem pendingDeliveryItem in deliveryItems)
                        {
                            totalItemAvailable -= pendingDeliveryItem.Quantity;
                        }
                        // trừ số lượng expried
                        totalItemAvailable -= stocks
                            .Where(a => a.Status == StockStatus.EXPIRED)
                            .Sum(a => a.Quantity);

                        stocks = stocks
                            .Skip((pagination.CurrentPage - 1) * pagination.PageSize)
                            .Take(pagination.PageSize)
                            .ToList();

                        currentStocks = stocks
                            .Select(
                                a =>
                                    new StockDetailsResponse
                                    {
                                        ItemId = a.ItemId,
                                        StockId = a.Id,
                                        CreatedDate = a.CreatedDate,
                                        ExprirationDate = a.ExpirationDate,
                                        Quantity = a.Quantity,
                                        Status = a.Status.ToString(),
                                        Unit = item.ItemTemplate.Unit.Name
                                    }
                            )
                            .ToList();

                        var rs = new
                        {
                            Total = total > 0 ? total : 0,
                            TotalItemAvailable = totalItemAvailable > 0 ? totalItemAvailable : 0,
                            TotalItemNotAvailable = (total - totalItemAvailable) > 0
                                ? (total - totalItemAvailable)
                                : 0,
                            CurrentStocksDetails = currentStocks.OrderBy(a => a.ExprirationDate)
                        };
                        commonResponse.Pagination = pagination;
                        commonResponse.Data = rs;
                    }
                    commonResponse.Status = 200;
                }
                else
                {
                    commonResponse.Status = 400;
                    commonResponse.Message = "Không tìm thấy vật phẩm tương ứng";
                }
                return commonResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    $"An exception occurred in service {nameof(StockService)}, method {nameof(GetStockByItemIdAndScheduledTimesAsync)}."
                );
            }
            return new CommonResponse
            {
                Status = 500,
                Message = _config["ResponseMessages:CommonMsg:InternalServerErrorMsg"]
            };
        }

        public async Task UpdateStockWhenOutDate()
        {
            string notificationImage = _config["Notification:Image"];
            string notificationTitleForItemOutOfDateMsg = _config[
                "Notification:NotificationTitleForItemOutOfDateMsg"
            ];

            try
            {
                List<Branch>? branches = await _branchRepository.GetBranchesAsync(
                    null,
                    BranchStatus.ACTIVE,
                    null
                );

                foreach (var b in branches)
                {
                    DateTime checkDate = DateTime.Now.Date;
                    int numberOfOutDate = 0;
                    List<Stock>? stocksOutDateNextDate = await _stockRepository.GetStocksAsync(
                        null,
                        b.Id,
                        checkDate.AddDays(1)
                    );
                    List<Stock>? stocksOutDateToday = await _stockRepository.GetStocksAsync(
                        null,
                        b.Id,
                        checkDate
                    );
                    if (stocksOutDateNextDate != null && stocksOutDateNextDate.Count > 0)
                    {
                        foreach (var s in stocksOutDateNextDate)
                        {
                            List<DeliveryItem>? deliveryItems =
                                await _deliveryItemRepository.GetPendingDeliveryItemsByItemIdAndBranchIdOfAidDeliveryRequest(
                                    s.ItemId,
                                    b.Id
                                );
                            if (deliveryItems != null && deliveryItems.Count > 0)
                            {
                                continue;
                            }
                            else
                            {
                                numberOfOutDate++;
                            }
                        }
                        if (numberOfOutDate > 0)
                        {
                            Notification notification = new Notification
                            {
                                Name = "Thông báo về vật phẩm sắp hết hạn",
                                CreatedDate = SettedUpDateTime.GetCurrentVietNamTime(),
                                Image = notificationImage,
                                Content =
                                    $"Thông báo vào ngày {checkDate.ToShortDateString()}, sẽ có {numberOfOutDate} vật phẩm hết hạn.",
                                ReceiverId = b.BranchAdminId.ToString(),
                                Status = NotificationStatus.NEW,
                                DataType = DataNotificationType.STOCK
                            };
                            await _notificationRepository.CreateNotificationAsync(notification);
                            await _hubContext.Clients.All.SendAsync(
                                b.BranchAdminId.ToString(),
                                notification
                            );
                        }
                    }
                    if (stocksOutDateToday != null && stocksOutDateToday.Count > 0)
                    {
                        foreach (var s in stocksOutDateToday)
                        {
                            //List<DeliveryItem>? deliveryItems =
                            //    await _deliveryItemRepository.GetPendindDeliveryItemsByItemIdAndBranchIdOfAidDeliveryRequest(
                            //        s.ItemId,
                            //        b.Id
                            //    );


                            numberOfOutDate++;
                            s.Status = StockStatus.EXPIRED;
                            await _stockRepository.UpdateStockAsync(s);
                        }
                        if (numberOfOutDate > 0)
                        {
                            Notification notification = new Notification
                            {
                                Name = "Thông báo về vật phẩm đã hết hạn",
                                CreatedDate = SettedUpDateTime.GetCurrentVietNamTime(),
                                Image = notificationImage,
                                Content =
                                    $"Thông báo đã có {numberOfOutDate} vật phẩm hết hạn vào ngày hôm nay.",
                                ReceiverId = b.BranchAdminId.ToString(),
                                Status = NotificationStatus.NEW,
                                DataType = DataNotificationType.STOCK
                            };
                            await _notificationRepository.CreateNotificationAsync(notification);
                            await _hubContext.Clients.All.SendAsync(
                                b.BranchAdminId.ToString(),
                                notification
                            );
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    $"An exception occurred in service {nameof(StockService)}, method {nameof(GetStockByItemIdAndScheduledTimesAsync)}."
                );
            }
        }

        public async Task<CommonResponse> GetListAvalableByItemsId(
            NumberOfAvalibleItemRequest request,
            Guid? branchAdminId
        )
        {
            CommonResponse commonResponse = new CommonResponse();
            try
            {
                Branch? branch = null;
                if (request.BranchId != null)
                {
                    branch = await _branchRepository.FindBranchByIdAsync(request.BranchId.Value);
                }
                else if (branchAdminId != null)
                {
                    branch = await _branchRepository.FindBranchByBranchAdminIdAsync(
                        branchAdminId.Value
                    );
                }
                if (branch != null)
                {
                    List<NumberOfAvalibleItemResponse> itemQuantities =
                        new List<NumberOfAvalibleItemResponse>();

                    foreach (var itemId in request.ItemIds)
                    {
                        NumberOfAvalibleItemResponse? tmp = await GetAvalableStockFromDate(
                            itemId,
                            request.ScheduledTimes,
                            branch
                        );
                        if (tmp != null)
                        {
                            itemQuantities.Add(tmp);
                        }
                    }
                    commonResponse.Data = itemQuantities;
                }
                commonResponse.Status = 200;
                return commonResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    $"An exception occurred in service {nameof(StockService)}, method {nameof(GetListAvalableByItemsId)}."
                );
            }
            return new CommonResponse
            {
                Status = 500,
                Message = _config["ResponseMessages:CommonMsg:InternalServerErrorMsg"]
            };
        }

        private async Task<NumberOfAvalibleItemResponse?> GetAvalableStockFromDate(
            Guid ItemId,
            List<ScheduledTime> scheduledTimes,
            Branch branch
        )
        {
            Item? item = await _itemRepository.FindItemByIdAsync(ItemId);
            if (item != null)
            {
                DateTime tmp = GetEndDateTimeFromScheduledTime(
                    GetLastAvailabeScheduledTime(scheduledTimes)!
                );

                DateTime endDateTimeOfLastScheduledtime = new DateTime(
                    tmp.Year,
                    tmp.Month,
                    tmp.Day
                );

                //stock hiện có
                List<Stock> stocks =
                    await _stockRepository.GetCurrentValidStocksByItemIdAndBranchId(
                        item.Id,
                        branch.Id
                    );
                double currentStock = stocks
                    .Where(
                        s =>
                            new DateTime(
                                s.ExpirationDate.Year,
                                s.ExpirationDate.Month,
                                s.ExpirationDate.Day
                            ) >= endDateTimeOfLastScheduledtime.AddDays(1)
                    )
                    .Sum(s => s.Quantity);

                //stock không khả dụng
                List<DeliveryItem> deliveryItems =
                    await _deliveryItemRepository.GetPendingDeliveryItemsByItemIdAndBranchIdOfAidDeliveryRequest(
                        item.Id,
                        branch.Id
                    );
                double pendingStock = deliveryItems
                //.Where(
                //    (di) =>
                //    {
                //        DateTime endOfLastScheduledTimeOfPendingDeliveryRequest =
                //            GetEndDateTimeFromScheduledTime(
                //                GetLastAvailabeScheduledTime(
                //                    JsonConvert.DeserializeObject<List<ScheduledTime>>(
                //                        di.DeliveryRequest.ScheduledTimes
                //                    )!
                //                )!
                //            );
                //        return new DateTime(
                //                endOfLastScheduledTimeOfPendingDeliveryRequest.Year,
                //                endOfLastScheduledTimeOfPendingDeliveryRequest.Month,
                //                endOfLastScheduledTimeOfPendingDeliveryRequest.Day
                //            ) >= endDateTimeOfLastScheduledtime;
                //    }
                //)
                .Sum(s => s.Quantity);

                //stock khả dụng
                currentStock = currentStock - pendingStock;

                return new NumberOfAvalibleItemResponse
                {
                    Item = new ItemResponse
                    {
                        Id = item.Id,
                        Name = item.ItemTemplate.Name,
                        Image = item.Image,
                        Note = item.Note ?? string.Empty,
                        EstimatedExpirationDays = item.EstimatedExpirationDays,
                        MaximumTransportVolume = item.MaximumTransportVolume,
                        Unit = item.ItemTemplate.Unit.Name,
                        AttributeValues = item.ItemAttributeValues
                            .Select(av => av.AttributeValue.Value)
                            .ToList()
                    },
                    Quantity = currentStock > 0 ? currentStock : 0
                };
            }
            else
            {
                return null;
            }
        }
    }
}
