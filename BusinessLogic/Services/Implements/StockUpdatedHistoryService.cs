using BusinessLogic.Utils.ExcelService;
using BusinessLogic.Utils.Notification.Implements;
using BusinessLogic.Utils.SecurityServices;
using DataAccess.Entities;
using DataAccess.EntityEnums;
using DataAccess.Models.Requests;
using DataAccess.Models.Requests.ModelBinders;
using DataAccess.Models.Responses;
using DataAccess.ModelsEnum;
using DataAccess.Repositories;
using Hangfire;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Security.Cryptography;
using System.Transactions;

namespace BusinessLogic.Services.Implements
{
    public class StockUpdatedHistoryService : IStockUpdatedHistoryService
    {
        private readonly IStockUpdatedHistoryRepository _stockUpdatedHistoryRepository;
        private readonly ILogger<StockUpdatedHistoryDetailService> _logger;
        private readonly IConfiguration _config;
        private readonly IItemRepository _itemRepository;
        private readonly IExcelService _excelService;
        private readonly IStockRepository _stockRepository;
        private readonly IUserRepository _userRepository;
        private readonly IActivityRepository _activityRepository;
        private readonly IBranchRepository _branchRepository;
        private readonly IStockUpdatedHistoryDetailRepository _stockUpdatedHistoryDetailRepository;
        private readonly IRoleRepository _roleRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IActivityBranchRepository _activityBranchRepository;
        private readonly IHubContext<NotificationSignalSender> _hubContext;
        private readonly INotificationRepository _notificationRepository;
        private readonly ITargetProcessRepository _targetProcessRepository;
        private readonly IDeliveryItemRepository _deliveryItemRepository;
        private readonly IAidRequestRepository _aidRequestRepository;

        public StockUpdatedHistoryService(
            IStockUpdatedHistoryRepository stockUpdatedHistoryRepository,
            ILogger<StockUpdatedHistoryDetailService> logger,
            IConfiguration config,
            IItemRepository itemRepository,
            IExcelService excelService,
            IStockRepository stockRepository,
            IUserRepository userRepository,
            IActivityRepository activityRepository,
            IBranchRepository branchRepository,
            IStockUpdatedHistoryDetailRepository stockUpdatedHistoryDetailRepository,
            IRoleRepository roleRepository,
            IPasswordHasher passwordHasher,
            IActivityBranchRepository activityBranchRepository,
            IHubContext<NotificationSignalSender> hubContext,
            INotificationRepository notificationRepository,
            ITargetProcessRepository targetProcessRepository,
            IDeliveryItemRepository deliveryItemRepository,
            IAidRequestRepository aidRequestRepository
        )
        {
            _stockUpdatedHistoryRepository = stockUpdatedHistoryRepository;
            _logger = logger;
            _config = config;
            _itemRepository = itemRepository;
            _excelService = excelService;
            _stockRepository = stockRepository;
            _userRepository = userRepository;
            _activityRepository = activityRepository;
            _branchRepository = branchRepository;
            _stockUpdatedHistoryDetailRepository = stockUpdatedHistoryDetailRepository;
            _roleRepository = roleRepository;
            _passwordHasher = passwordHasher;
            _activityBranchRepository = activityBranchRepository;
            _hubContext = hubContext;
            _notificationRepository = notificationRepository;
            _targetProcessRepository = targetProcessRepository;
            _deliveryItemRepository = deliveryItemRepository;
            _aidRequestRepository = aidRequestRepository;
        }

        public async Task<CommonResponse> CreateStockUpdatedHistoryTypeExportByItemsAsync(
            Guid userId,
            StockUpdatedHistoryTypeExportByItemsCreatingRequest updatedHistoryTypeExportByItemsCreatingRequest
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

                if (updatedHistoryTypeExportByItemsCreatingRequest.AidRequestId != null)
                {
                    AidRequest? aidRequest =
                        await _aidRequestRepository.FindAcceptedOrProcessingAidRequestByIdAndBranchIdAsync(
                            (Guid)updatedHistoryTypeExportByItemsCreatingRequest.AidRequestId,
                            branch.Id
                        );

                    if (aidRequest == null)
                        return new CommonResponse
                        {
                            Status = 400,
                            Message = _config[
                                "ResponseMessages:AidRequest:WaitingForDeliveringAidRequestNotFoundMsg"
                            ]
                        };

                    //if (!aidRequest.IsSelfShipping)
                    //    return new CommonResponse
                    //    {
                    //        Status = 400,
                    //        Message = _config[
                    //            "ResponseMessages:AidRequest:CanNotExportStockForNotSelfShippingAidRequestMsg"
                    //        ]
                    //    };
                }

                //if (updatedHistoryTypeExportByItemsCreatingRequest.ActivityId != null)
                //{
                //    ActivityBranch? activityBranch =
                //        await _activityBranchRepository.FindActivityBranchByActivityIdAndBranchIdAsync(
                //            (Guid)updatedHistoryTypeExportByItemsCreatingRequest.ActivityId,
                //            branch.Id
                //        );

                //    if (activityBranch == null)
                //    {
                //        return new CommonResponse
                //        {
                //            Status = 403,
                //            Message = _config[
                //                "ResponseMessages:ActivityBranchMsg:BranchNotJoinInTheActivityMsg"
                //            ]
                //        };
                //    }
                //}

                List<DeliveryItemRequest> currentStocks = new List<DeliveryItemRequest>();
                Dictionary<Guid, List<Stock>> currentStocksToUpdate =
                    new Dictionary<Guid, List<Stock>>();

                DateTime tmp = GetEndDateTimeFromScheduledTime(
                    GetLastAvailabeScheduledTime(
                        updatedHistoryTypeExportByItemsCreatingRequest.ScheduledTimes
                    )!
                );

                DateTime endOfLastScheduledTime = new DateTime(tmp.Year, tmp.Month, tmp.Day);

                //tính stock hiện tại
                foreach (
                    Guid itemId in updatedHistoryTypeExportByItemsCreatingRequest.ExportedItems.Select(
                        i => i.ItemId
                    )
                )
                {
                    List<Stock> stocks =
                        await _stockRepository.GetCurrentValidStocksByItemIdAndBranchId(
                            itemId,
                            branch.Id
                        );

                    currentStocks.Add(
                        new DeliveryItemRequest
                        {
                            ItemId = itemId,
                            Quantity = stocks
                                .Where(
                                    s =>
                                        new DateTime(
                                            s.ExpirationDate.Year,
                                            s.ExpirationDate.Month,
                                            s.ExpirationDate.Day
                                        )
                                        >= endOfLastScheduledTime.AddDays(
                                            updatedHistoryTypeExportByItemsCreatingRequest.AidRequestId
                                            != null
                                                ? 1
                                                : 0
                                        )
                                )
                                .Sum(s => s.Quantity)
                        }
                    );

                    currentStocksToUpdate[itemId] = stocks
                        .Where(
                            s =>
                                new DateTime(
                                    s.ExpirationDate.Year,
                                    s.ExpirationDate.Month,
                                    s.ExpirationDate.Day
                                )
                                >= endOfLastScheduledTime.AddDays(
                                    updatedHistoryTypeExportByItemsCreatingRequest.AidRequestId
                                    != null
                                        ? 1
                                        : 0
                                )
                        )
                        .OrderBy(s => s.ExpirationDate)
                        .ToList();
                }

                //tính stock không khả dụng
                List<DeliveryItemRequest> pendingDeliveryItems = new List<DeliveryItemRequest>();
                foreach (
                    Guid itemId in updatedHistoryTypeExportByItemsCreatingRequest.ExportedItems.Select(
                        i => i.ItemId
                    )
                )
                {
                    List<DeliveryItem> deliveryItems =
                        await _deliveryItemRepository.GetPendingDeliveryItemsByItemIdAndBranchIdOfAidDeliveryRequest(
                            itemId,
                            branch.Id
                        );

                    pendingDeliveryItems.Add(
                        new DeliveryItemRequest
                        {
                            ItemId = itemId,
                            Quantity = deliveryItems
                            //.Where(
                            //    (di) =>
                            //    {
                            //        DateTime endOfLastScheduledTimeOfPendingDeliveryRequest =
                            //            GetEndDateTimeFromScheduledTime(
                            //                GetLastAvailabeScheduledTime(
                            //                    JsonConvert.DeserializeObject<
                            //                        List<ScheduledTime>
                            //                    >(di.DeliveryRequest.ScheduledTimes)!
                            //                )!
                            //            );
                            //        return new DateTime(
                            //                endOfLastScheduledTimeOfPendingDeliveryRequest.Year,
                            //                endOfLastScheduledTimeOfPendingDeliveryRequest.Month,
                            //                endOfLastScheduledTimeOfPendingDeliveryRequest.Day
                            //            ) >= endOfLastScheduledTime;
                            //    }
                            //)
                            .Sum(s => s.Quantity)
                        }
                    );
                }

                //tính stock khả dụng
                foreach (DeliveryItemRequest currentStock in currentStocks)
                {
                    foreach (DeliveryItemRequest pendingDeliveryItem in pendingDeliveryItems)
                    {
                        if (currentStock.ItemId == pendingDeliveryItem.ItemId)
                            currentStock.Quantity -= pendingDeliveryItem.Quantity;
                    }
                }

                //trừ stock nếu xuất kho thành công
                foreach (DeliveryItemRequest currentStock in currentStocks)
                {
                    foreach (
                        DeliveryItemRequest exportedItem in updatedHistoryTypeExportByItemsCreatingRequest.ExportedItems
                    )
                    {
                        if (currentStock.ItemId == exportedItem.ItemId)
                            currentStock.Quantity -= exportedItem.Quantity;
                    }
                }

                if (currentStocks.Any(s => s.Quantity < 0))
                    return new CommonResponse
                    {
                        Status = 400,
                        Message = _config["ResponseMessages:StockMsg:StockNotEnoughToExportMsg"]
                    };

                using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
                {
                    StockUpdatedHistory stockUpdatedHistory = new StockUpdatedHistory
                    {
                        CreatedDate = SettedUpDateTime.GetCurrentVietNamTime(),
                        Type = StockUpdatedHistoryType.EXPORT,
                        BranchId = branch.Id,
                        //ActivityId = updatedHistoryTypeExportByItemsCreatingRequest.ActivityId,
                        CreatedBy = userId,
                        Note = updatedHistoryTypeExportByItemsCreatingRequest.Note
                    };

                    if (
                        await _stockUpdatedHistoryRepository.AddStockUpdatedHistoryAsync(
                            stockUpdatedHistory
                        ) != 1
                    )
                        return new CommonResponse
                        {
                            Status = 500,
                            Message = _config["ResponseMessages:CommonMsg:InternalServerErrorMsg"]
                        };

                    List<Stock> newStocks = new List<Stock>();
                    List<StockUpdatedHistoryDetail> newStockUpdatedHistoryDetails =
                        new List<StockUpdatedHistoryDetail>();

                    foreach (
                        DeliveryItemRequest exportedItem in updatedHistoryTypeExportByItemsCreatingRequest.ExportedItems
                    )
                    {
                        List<Stock> stocks = currentStocksToUpdate[exportedItem.ItemId];

                        double deliveryItemQuantityLeft = exportedItem.Quantity;

                        foreach (Stock stock in stocks)
                        {
                            double consumedStock = 0;

                            if (stock.Quantity < deliveryItemQuantityLeft)
                            {
                                consumedStock = stock.Quantity;
                                deliveryItemQuantityLeft -= consumedStock;
                                stock.Quantity = 0;
                            }
                            else
                            {
                                consumedStock = deliveryItemQuantityLeft;
                                stock.Quantity -= consumedStock;
                                deliveryItemQuantityLeft = 0;
                            }

                            newStocks.Add(stock);

                            newStockUpdatedHistoryDetails.Add(
                                new StockUpdatedHistoryDetail
                                {
                                    Quantity = consumedStock,
                                    StockUpdatedHistoryId = stockUpdatedHistory.Id,
                                    StockId = stock.Id,
                                    Note = exportedItem.Note,
                                    AidRequestId =
                                        updatedHistoryTypeExportByItemsCreatingRequest.AidRequestId,
                                }
                            );

                            if (deliveryItemQuantityLeft == 0)
                                break;
                        }
                    }

                    if (await _stockRepository.UpdateStocksAsync(newStocks) != newStocks.Count)
                        return new CommonResponse
                        {
                            Status = 500,
                            Message = _config["ResponseMessages:CommonMsg:InternalServerErrorMsg"]
                        };

                    if (
                        await _stockUpdatedHistoryDetailRepository.AddStockUpdatedHistoryDetailsAsync(
                            newStockUpdatedHistoryDetails
                        ) != newStockUpdatedHistoryDetails.Count
                    )
                        return new CommonResponse
                        {
                            Status = 500,
                            Message = _config["ResponseMessages:CommonMsg:InternalServerErrorMsg"]
                        };

                    scope.Complete();
                    return new CommonResponse
                    {
                        Status = 200,
                        Message = _config[
                            "ResponseMessages:StockUpdatedHistoryMsg:ExportStocksSuccessMsg"
                        ]
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    $"An exception occurred in service {nameof(StockUpdatedHistoryService)}, method {nameof(CreateStockUpdatedHistoryTypeExportByItemsAsync)}."
                );
            }
            return new CommonResponse
            {
                Status = 500,
                Message = _config["ResponseMessages:CommonMsg:InternalServerErrorMsg"]
            };
        }

        public async Task<CommonResponse> CreateStockUpdatedHistoryTypeExportByStocksAsync(
            Guid userId,
            StockUpdatedHistoryTypeExportByStocksCreatingRequest updatedHistoryTypeExportByStocksCreatingRequest
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

                //if (updatedHistoryTypeExportByStocksCreatingRequest.ActivityId != null)
                //{
                //    ActivityBranch? activityBranch =
                //        await _activityBranchRepository.FindActivityBranchByActivityIdAndBranchIdAsync(
                //            (Guid)updatedHistoryTypeExportByStocksCreatingRequest.ActivityId,
                //            branch.Id
                //        );

                //    if (activityBranch == null)
                //    {
                //        return new CommonResponse
                //        {
                //            Status = 403,
                //            Message = _config[
                //                "ResponseMessages:ActivityBranchMsg:BranchNotJoinInTheActivityMsg"
                //            ]
                //        };
                //    }
                //}

                using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
                {
                    StockUpdatedHistory stockUpdatedHistory = new StockUpdatedHistory
                    {
                        CreatedDate = SettedUpDateTime.GetCurrentVietNamTime(),
                        Type = StockUpdatedHistoryType.EXPORT,
                        BranchId = branch.Id,
                        //ActivityId = updatedHistoryTypeExportByStocksCreatingRequest.ActivityId,
                        CreatedBy = userId,
                        Note = updatedHistoryTypeExportByStocksCreatingRequest.Note
                    };

                    if (
                        await _stockUpdatedHistoryRepository.AddStockUpdatedHistoryAsync(
                            stockUpdatedHistory
                        ) != 1
                    )
                        return new CommonResponse
                        {
                            Status = 500,
                            Message = _config["ResponseMessages:CommonMsg:InternalServerErrorMsg"]
                        };

                    List<Stock> stocks = new List<Stock>();
                    List<StockUpdatedHistoryDetail> stockUpdatedHistoryDetails =
                        new List<StockUpdatedHistoryDetail>();

                    //foreach (
                    //    StockRequest stockRequest in updatedHistoryTypeExportByStocksCreatingRequest.ExportedStocks
                    //)
                    //{
                    //    Stock? stock = await _stockRepository.GetStocksByIdAndBranchId(
                    //        stockRequest.StockId,
                    //        branch.Id
                    //    );

                    //    if (stock == null)
                    //        return new CommonResponse
                    //        {
                    //            Status = 400,
                    //            Message = _config["ResponseMessages:StockMsg:StockNotFoundMsg"]
                    //        };

                    //    if (stock.Quantity - stockRequest.Quantity < 0)
                    //        return new CommonResponse
                    //        {
                    //            Status = 400,
                    //            Message = _config[
                    //                "ResponseMessages:StockMsg:StockNotEnoughToExportMsg"
                    //            ]
                    //        };

                    //    stock.Quantity -= stockRequest.Quantity;
                    //    stocks.Add(stock);
                    //    stockUpdatedHistoryDetails.Add(
                    //        new StockUpdatedHistoryDetail
                    //        {
                    //            Quantity = stockRequest.Quantity,
                    //            StockUpdatedHistoryId = stockUpdatedHistory.Id,
                    //            StockId = stock.Id,
                    //            Note = stockRequest.Note
                    //        }
                    //    );
                    //}

                    foreach (
                        StockRequest stockRequest in updatedHistoryTypeExportByStocksCreatingRequest.ExportedStocks
                    )
                    {
                        //tìm stock và tổng stock của item
                        //tìm stock không khả
                        Stock? stock = await _stockRepository.GetExpiredStocksByIdAndBranchId(
                            stockRequest.StockId,
                            branch.Id
                        );

                        if (stock == null)
                            return new CommonResponse
                            {
                                Status = 400,
                                Message = _config["ResponseMessages:StockMsg:StockNotFoundMsg"]
                            };

                        if (stock.Quantity - stockRequest.Quantity < 0)
                            return new CommonResponse
                            {
                                Status = 400,
                                Message = _config[
                                    "ResponseMessages:StockMsg:StockNotEnoughToExportMsg"
                                ]
                            };

                        stock.Quantity -= stockRequest.Quantity;
                        stocks.Add(stock);
                        stockUpdatedHistoryDetails.Add(
                            new StockUpdatedHistoryDetail
                            {
                                Quantity = stockRequest.Quantity,
                                StockUpdatedHistoryId = stockUpdatedHistory.Id,
                                StockId = stock.Id,
                                Note = stockRequest.Note
                            }
                        );
                    }

                    if (await _stockRepository.UpdateStocksAsync(stocks) != stocks.Count)
                        return new CommonResponse
                        {
                            Status = 500,
                            Message = _config["ResponseMessages:CommonMsg:InternalServerErrorMsg"]
                        };

                    if (
                        await _stockUpdatedHistoryDetailRepository.AddStockUpdatedHistoryDetailsAsync(
                            stockUpdatedHistoryDetails
                        ) != stockUpdatedHistoryDetails.Count
                    )
                        return new CommonResponse
                        {
                            Status = 500,
                            Message = _config["ResponseMessages:CommonMsg:InternalServerErrorMsg"]
                        };

                    scope.Complete();
                    return new CommonResponse
                    {
                        Status = 200,
                        Message = _config[
                            "ResponseMessages:StockUpdatedHistoryMsg:ExportStocksSuccessMsg"
                        ]
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    $"An exception occurred in service {nameof(StockUpdatedHistoryService)}, method {nameof(CreateStockUpdatedHistoryTypeExportByStocksAsync)}."
                );
            }
            return new CommonResponse
            {
                Status = 500,
                Message = _config["ResponseMessages:CommonMsg:InternalServerErrorMsg"]
            };
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

        private DateTime GetEndDateTimeFromScheduledTime(ScheduledTime scheduledTime)
        {
            DateOnly day = DateOnly.Parse(scheduledTime.Day);
            TimeOnly endTime = TimeOnly.Parse(scheduledTime.EndTime);
            return day.ToDateTime(endTime);
        }

        public async Task<CommonResponse> CreateStockUpdateHistoryWhenUserDirectlyDonate(
            StockUpdateForUserDirectDonateRequest request,
            Guid branchAdminId
        )
        {
            string internalServerErrorMsg = _config[
                "ResponseMessages:CommonMsg:InternalServerErrorMsg"
            ];

            CommonResponse commonResponse = new CommonResponse();
            try
            {
                using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
                {
                    Branch? branch = await _branchRepository.FindBranchByBranchAdminIdAsync(
                        branchAdminId
                    );
                    if (branch == null)
                    {
                        commonResponse.Status = 400;
                        commonResponse.Message = "Không tìm thấy chi nhánh tương ứng";
                        return commonResponse;
                    }

                    User? userCheck = null;
                    if (request.UserId != null)
                    {
                        userCheck = await _userRepository.FindUserByIdAsync(request.UserId.Value);
                    }
                    else if (request.Phone != null && request.FullName != null)
                    {
                        userCheck = await CreateUserByPhoneForBranchAdmin(
                            request.Phone,
                            request.FullName
                        );
                    }

                    if (userCheck == null)
                    {
                        commonResponse.Status = 400;
                        commonResponse.Message = "Không tìm thấy người dùng tương ứng";
                        return commonResponse;
                    }
                    else
                    {
                        StockUpdatedHistory stockUpdatedHistory = new StockUpdatedHistory();
                        stockUpdatedHistory.BranchId = branch.Id;

                        List<TargetProcess> targetProcesses =
                            await _targetProcessRepository.FindStillTakingPlaceActivityTargetProcessesByBranchIdAsync(
                                branch.Id
                            );
                        List<StockUpdatedHistoryDetail> stockUpdatedHistoryDetails =
                            new List<StockUpdatedHistoryDetail>();

                        foreach (var a in request.DirectDonationRequests)
                        {
                            StockUpdatedHistoryDetail stockUpdatedHistoryDetail =
                                new StockUpdatedHistoryDetail();

                            Item? itemCheck = await _itemRepository.FindItemByIdAsync(a.ItemId);
                            if (itemCheck == null
                            //|| itemCheck.Status != ItemStatus.ACTIVE
                            )
                            {
                                commonResponse.Status = 400;
                                commonResponse.Message = "Không tìm thấy vật phẩm yêu cầu";
                                return commonResponse;
                            }
                            else
                            {
                                TargetProcess? targetProcess = targetProcesses
                                    .Where(tp => tp.ItemId == a.ItemId)
                                    .MinBy(tp => tp.Process / tp.Target);

                                Stock? checkStock =
                                    await _stockRepository.FindStockByItemIdAndExpirationDateAndBranchIdAndUserIdAndActivityId(
                                        a.ItemId,
                                        a.ExpirationDate,
                                        branch.Id,
                                        userCheck.Id,
                                        targetProcess == null ? null : targetProcess.ActivityId
                                    );

                                if (checkStock != null)
                                {
                                    checkStock.Quantity = checkStock.Quantity += a.Quantity;
                                    //checkStock.Status = StockStatus.VALID;
                                    //checkStock.BranchId = branch.Id;
                                    //checkStock.ExpirationDate = a.ExpirationDate;
                                    //checkStock.ItemId = a.ItemId;
                                    //checkStock.CreatedDate =
                                    //    SettedUpDateTime.GetCurrentVietNamTime();
                                    await _stockRepository.UpdateStockAsync(checkStock);
                                }
                                else
                                {
                                    checkStock = new Stock();

                                    checkStock.Status = StockStatus.VALID;
                                    checkStock.Quantity = a.Quantity;
                                    checkStock.BranchId = branch.Id;
                                    checkStock.ExpirationDate = a.ExpirationDate.Date;
                                    checkStock.ItemId = a.ItemId;
                                    checkStock.ActivityId =
                                        targetProcess == null ? null : targetProcess.ActivityId;
                                    checkStock.UserId = userCheck.Id;
                                    checkStock.CreatedDate =
                                        SettedUpDateTime.GetCurrentVietNamTime();
                                    await _stockRepository.AddStockAsync(checkStock);
                                }

                                stockUpdatedHistoryDetail.Quantity = a.Quantity;
                                stockUpdatedHistoryDetail.StockId = checkStock.Id;
                                stockUpdatedHistoryDetail.Note = a.Note;
                                stockUpdatedHistoryDetails.Add(stockUpdatedHistoryDetail);

                                if (targetProcess != null)
                                {
                                    targetProcess.Process += a.Quantity;

                                    if (
                                        await _targetProcessRepository.UpdateTargetProcessAsync(
                                            targetProcess
                                        ) != 1
                                    )
                                        return new CommonResponse
                                        {
                                            Status = 500,
                                            Message = _config[
                                                "ResponseMessages:CommonMsg:InternalServerErrorMsg"
                                            ]
                                        };

                                    int index = targetProcesses.FindIndex(
                                        tp =>
                                            tp.ActivityId == targetProcess.ActivityId
                                            && tp.ItemId == targetProcess.ItemId
                                    );
                                    targetProcesses[index] = targetProcess;
                                }
                            }
                        }
                        stockUpdatedHistory.StockUpdatedHistoryDetails = stockUpdatedHistoryDetails;
                        stockUpdatedHistory.Note = request.Note;
                        stockUpdatedHistory.BranchId = branch.Id;
                        stockUpdatedHistory.Type = StockUpdatedHistoryType.IMPORT;
                        stockUpdatedHistory.CreatedBy = branchAdminId;
                        stockUpdatedHistory.CreatedDate = SettedUpDateTime.GetCurrentVietNamTime();
                        int rs = await _stockUpdatedHistoryRepository.AddStockUpdatedHistoryAsync(
                            stockUpdatedHistory
                        );
                        if (rs < 0)
                            throw new Exception();
                        commonResponse.Status = 200;
                        commonResponse.Message = "Cập nhật thành công";
                        scope.Complete();
                    }
                }
            }
            catch (Exception ex)
            {
                string className = nameof(StockUpdatedHistoryDetailService);
                string methodName = nameof(CreateStockUpdateHistoryWhenUserDirectlyDonate);
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

        private async Task<User?> CreateUserByPhoneForBranchAdmin(string phone, string fullname)
        {
            string defaultAvatar = _config["DefaultProfile:DefaultAvatar"];
            string registerByPhoneSuccessMsg = _config[
                "ResponseMessages:UserMsg:RegisterByPhoneSuccessMsg"
            ];

            User? user = await _userRepository.FindUserByEmailOrPhoneAsync(phone);
            if (user != null)
            {
                return user;
            }

            Role? userRole = await _roleRepository.GetRoleByName(RoleEnum.CONTRIBUTOR.ToString());
            if (phone != null && userRole != null)
            {
                user = new User
                {
                    Email = "",
                    Phone = phone,
                    Password = _passwordHasher.Hash(GenerateNewPasword()),
                    RoleId = userRole.Id,
                    Name = fullname,
                    Status = UserStatus.UNVERIFIED,
                    Avatar = defaultAvatar
                };
                return await _userRepository.CreateUserAsync(user);
            }
            else
                return null;
        }

        private string GenerateNewPasword()
        {
            using (var rng = RandomNumberGenerator.Create())
            {
                var tokenBytes = new byte[32]; // 32 bytes for a secure 256-bit token
                rng.GetBytes(tokenBytes);
                return Convert.ToBase64String(tokenBytes);
            }
        }

        public void SendNotification(DateTime stockExpriation, Guid userId, string itemName)
        {
            TimeSpan startDelayFirst = stockExpriation.AddDays(-2) - DateTime.Now;
            startDelayFirst = new TimeSpan(startDelayFirst.Days, 0, 0, 0);
            if (startDelayFirst.TotalMilliseconds > 0)
            {
                var jobId = BackgroundJob.Schedule(
                    () =>
                        SetNotificationForStockBeforeExpiration(stockExpriation, userId, itemName),
                    startDelayFirst
                );
            }

            TimeSpan startDelaySecond = stockExpriation.AddDays(-1) - DateTime.Now;
            startDelayFirst = new TimeSpan(startDelayFirst.Days, 0, 0, 0);
            if (startDelayFirst.TotalMilliseconds > 0)
            {
                var jobId = BackgroundJob.Schedule(
                    () =>
                        SetNotificationForStockBeforeExpiration(stockExpriation, userId, itemName),
                    startDelaySecond
                );
            }

            TimeSpan startDelayThird = stockExpriation - DateTime.Now;
            startDelayFirst = new TimeSpan(startDelayFirst.Days, 0, 0, 0);
            if (startDelayFirst.TotalMilliseconds > 0)
            {
                var jobId = BackgroundJob.Schedule(
                    () => SetNotificationForStockWhenExpiration(stockExpriation, userId, itemName),
                    startDelayThird
                );
            }
        }

        public async Task SetNotificationForStockBeforeExpiration(
            DateTime stockExpriation,
            Guid userId,
            string itemName
        )
        {
            string notificationImage = _config["Notification:Image"];
            string notificationTitleForItemOutOfDateMsg = _config[
                "Notification:NotificationTitleForItemOutOfDateMsg"
            ];

            Notification notification = new Notification
            {
                Name = notificationTitleForItemOutOfDateMsg,
                CreatedDate = SettedUpDateTime.GetCurrentVietNamTime(),
                Image = notificationImage,
                Content =
                    $"Thông báo về vật phẩm {itemName} sẽ hết hạn vào ngày {stockExpriation.ToString()}",
                ReceiverId = userId.ToString(),
                Status = NotificationStatus.NEW
            };
            await _notificationRepository.CreateNotificationAsync(notification);
            await _hubContext.Clients.All.SendAsync(userId.ToString(), notification);
        }

        public async Task SetNotificationForStockWhenExpiration(
            DateTime stockExpriation,
            Guid userId,
            string itemName
        )
        {
            string notificationImage = _config["Notification:Image"];
            string notificationTitleForItemOutOfDateMsg = _config[
                "Notification:NotificationTitleForItemOutOfDateMsg"
            ];

            Notification notification = new Notification
            {
                Name = notificationTitleForItemOutOfDateMsg,
                CreatedDate = SettedUpDateTime.GetCurrentVietNamTime(),
                Image = notificationImage,
                Content =
                    $"Thông báo về vật phẩm {itemName} đã hết hạn vào ngày {stockExpriation.ToString()}, vui lòng kiểm tra lại kho.",
                ReceiverId = userId.ToString(),
                Status = NotificationStatus.NEW
            };
            await _notificationRepository.CreateNotificationAsync(notification);
            await _hubContext.Clients.All.SendAsync(userId.ToString(), notification);
        }

        public async Task<CommonResponse> CreateStockUpdateHistoryWhenBranchAdminImport(
            StockUpdateForImportingByBranchAdmin request,
            Guid branchAdminId
        )
        {
            string internalServerErrorMsg = _config[
                "ResponseMessages:CommonMsg:InternalServerErrorMsg"
            ];

            CommonResponse commonResponse = new CommonResponse();
            try
            {
                using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
                {
                    Branch? branch = await _branchRepository.FindBranchByBranchAdminIdAsync(
                        branchAdminId
                    );
                    if (branch == null)
                    {
                        commonResponse.Status = 400;
                        commonResponse.Message = "Không tìm thấy chi nhánh tương ứng";
                        return commonResponse;
                    }
                    StockUpdatedHistory stockUpdatedHistory = new StockUpdatedHistory();
                    stockUpdatedHistory.BranchId = branch.Id;

                    List<StockUpdatedHistoryDetail> stockUpdatedHistoryDetails =
                        new List<StockUpdatedHistoryDetail>();
                    foreach (var a in request.DirectDonationRequests)
                    {
                        StockUpdatedHistoryDetail stockUpdatedHistoryDetail =
                            new StockUpdatedHistoryDetail();
                        Item? itemCheck = await _itemRepository.FindItemByIdAsync(a.ItemId);
                        if (itemCheck == null
                        //|| itemCheck.Status != ItemStatus.ACTIVE
                        )
                        {
                            commonResponse.Status = 400;
                            commonResponse.Message = "Không tìm thấy vật phẩm yêu cầu";
                            return commonResponse;
                        }
                        else
                        {
                            Stock? checkStock =
                                await _stockRepository.GetStocksByItemIdAndBranchIdAndExpirationDate(
                                    a.ItemId,
                                    branch.Id,
                                    a.ExpirationDate
                                );
                            if (checkStock != null)
                            {
                                checkStock.Status = StockStatus.VALID;
                                checkStock.Quantity = a.Quantity;
                                checkStock.BranchId = branch.Id;
                                checkStock.ExpirationDate = a.ExpirationDate.Date;
                                checkStock.CreatedDate = SettedUpDateTime.GetCurrentVietNamTime();
                                await _stockRepository.UpdateStockAsync(checkStock);
                            }
                            else
                            {
                                checkStock = new Stock();
                                checkStock.Status = StockStatus.VALID;
                                checkStock.Quantity = a.Quantity;
                                checkStock.BranchId = branch.Id;
                                checkStock.ExpirationDate = a.ExpirationDate.Date;
                                checkStock.ItemId = a.ItemId;
                                checkStock.CreatedDate = SettedUpDateTime.GetCurrentVietNamTime();
                                await _stockRepository.AddStockAsync(checkStock);
                            }

                            stockUpdatedHistoryDetail.Quantity = a.Quantity;
                            stockUpdatedHistoryDetail.StockId = checkStock.Id;
                            stockUpdatedHistoryDetail.Note = a.Note;
                            stockUpdatedHistoryDetails.Add(stockUpdatedHistoryDetail);
                        }
                    }
                    stockUpdatedHistory.StockUpdatedHistoryDetails = stockUpdatedHistoryDetails;
                    stockUpdatedHistory.BranchId = branch.Id;
                    stockUpdatedHistory.Type = StockUpdatedHistoryType.IMPORT;
                    stockUpdatedHistory.CreatedBy = branchAdminId;
                    stockUpdatedHistory.CreatedDate = SettedUpDateTime.GetCurrentVietNamTime();
                    stockUpdatedHistory.Note = request.Note;
                    int rs = await _stockUpdatedHistoryRepository.AddStockUpdatedHistoryAsync(
                        stockUpdatedHistory
                    );
                    if (rs < 0)
                        throw new Exception();
                    commonResponse.Status = 200;
                    commonResponse.Message = "Cập nhật thành công";
                    scope.Complete();
                }
            }
            catch (Exception ex)
            {
                string className = nameof(StockUpdatedHistoryDetailService);
                string methodName = nameof(CreateStockUpdateHistoryWhenUserDirectlyDonate);
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

        public async Task<CommonResponse> GetStockUpdatedHistoryOfSelfShippingAidRequestForCharityUnit(
            Guid stockUpdatedHistoryId,
            Guid userId,
            string userRoleName
        )
        {
            try
            {
                StockUpdatedHistory? stockUpdatedHistory =
                    userRoleName == RoleEnum.CHARITY.ToString()
                        ? await _stockUpdatedHistoryRepository.FindStockUpdatedHistoryByIdAndCharityUnitUserIdAsync(
                            stockUpdatedHistoryId,
                            userId
                        )
                        : await _stockUpdatedHistoryRepository.FindStockUpdatedHistoryByIdAndCharityUnitUserIdAsync(
                            stockUpdatedHistoryId,
                            null
                        );

                if (stockUpdatedHistory == null)
                    return new CommonResponse
                    {
                        Status = 400,
                        Message = _config[
                            "ResponseMessages:StockUpdatedHistoryMsg:StockUpdatedHistoryTypeExportNotFoundMsg"
                        ]
                    };

                return new CommonResponse
                {
                    Status = 200,
                    Data = new SimpleStockUpdatedHistoryDetailForCharityUnitResponse
                    {
                        Id = stockUpdatedHistory.Id,
                        ExportedDate = stockUpdatedHistory.CreatedDate,
                        ExportedNote = stockUpdatedHistory.Note,
                        BranchId = stockUpdatedHistory.Branch.Id,
                        BranchName = stockUpdatedHistory.Branch.Name,
                        BranchAddress = stockUpdatedHistory.Branch.Address,
                        BranchImage = stockUpdatedHistory.Branch.Image,
                        ExportedItems = stockUpdatedHistory.StockUpdatedHistoryDetails
                            .Select(
                                suhd =>
                                    new StockUpdatedHistoryDetailForSelfShippingResponse
                                    {
                                        StockUpdatedHistoryDetailId = suhd.Id,
                                        Name = GetFullNameOfItem(suhd.Stock!.Item),
                                        Image = suhd.Stock.Item.Image,
                                        Unit = suhd.Stock.Item.ItemTemplate.Unit.Name,
                                        ConfirmedExpirationDate = suhd.Stock.ExpirationDate,
                                        ExportedQuantity = suhd.Quantity
                                    }
                            )
                            .ToList()
                    },
                    Message = _config[
                        "ResponseMessages:StockUpdatedHistoryMsg:StockUpdatedHistoryTypeExportSuccessMsg"
                    ]
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    $"An exception occurred in service {nameof(StockUpdatedHistoryService)}, method {nameof(GetStockUpdatedHistoryOfSelfShippingAidRequestForCharityUnit)}."
                );
                return new CommonResponse
                {
                    Status = 500,
                    Message = _config["ResponseMessages:CommonMsg:InternalServerErrorMsg"]
                };
            }
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
    }
}
