using BusinessLogic.Utils.FirebaseService;
using BusinessLogic.Utils.Notification.Implements;
using DataAccess.Entities;
using DataAccess.EntityEnums;
using DataAccess.Models.Requests;
using DataAccess.Models.Requests.Enum;
using DataAccess.Models.Requests.ModelBinders;
using DataAccess.Models.Responses;
using DataAccess.ModelsEnum;
using DataAccess.Repositories;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Transactions;

namespace BusinessLogic.Services.Implements
{
    public class DeliveryRequestService : IDeliveryRequestService
    {
        private readonly IDeliveryRequestRepository _deliveryRequestRepository;
        private readonly IConfiguration _config;
        private readonly IBranchRepository _branchRepository;
        private readonly ILogger<DeliveryRequestService> _logger;
        private readonly IDonatedRequestRepository _donatedRequestRepository;
        private readonly IAidRequestRepository _aidRequestRepository;
        private readonly IDeliveryItemRepository _deliveryItemRepository;
        private readonly IStockRepository _stockRepository;
        private readonly IItemRepository _itemRepository;
        private readonly ICollaboratorRepository _collaboratorRepository;
        private readonly IScheduledRouteRepository _scheduledRouteRepository;
        private readonly IUserRepository _userRepository;
        private readonly IAidItemRepository _aidItemRepository;
        private readonly IStockUpdatedHistoryRepository _stockUpdatedHistoryRepository;
        private readonly IStockUpdatedHistoryDetailRepository _stockUpdatedHistoryDetailRepository;
        private readonly ICharityUnitRepository _charityUnitRepository;
        private readonly IReportRepository _reportRepository;
        private readonly IScheduledRouteDeliveryRequestRepository _scheduledRouteDeliveryRequestRepository;
        private readonly IActivityBranchRepository _activityBranchRepository;
        private readonly INotificationRepository _notificationRepository;
        private readonly IHubContext<NotificationSignalSender> _hubContext;
        private readonly IFirebaseNotificationService _firebaseNotificationService;
        private readonly IServiceProvider _serviceProvider;

        public DeliveryRequestService(
            IDeliveryRequestRepository deliveryRequestRepository,
            IConfiguration config,
            IBranchRepository branchRepository,
            ILogger<DeliveryRequestService> logger,
            IDonatedRequestRepository donatedRequestRepository,
            IAidRequestRepository aidRequestRepository,
            IDeliveryItemRepository deliveryItemRepository,
            IStockRepository stockRepository,
            IItemRepository itemRepository,
            ICollaboratorRepository collaboratorRepository,
            IScheduledRouteRepository scheduledRouteRepository,
            IUserRepository userRepository,
            IAidItemRepository aidItemRepository,
            IStockUpdatedHistoryRepository stockUpdatedHistoryRepository,
            IStockUpdatedHistoryDetailRepository stockUpdatedHistoryDetailRepository,
            ICharityUnitRepository charityUnitRepository,
            IReportRepository reportRepository,
            IScheduledRouteDeliveryRequestRepository scheduledRouteDeliveryRequestRepository,
            IActivityBranchRepository activityBranchRepository,
            INotificationRepository notificationRepository,
            IHubContext<NotificationSignalSender> hubContext,
            IFirebaseNotificationService firebaseNotificationService,
            IServiceProvider serviceProvider
        )
        {
            _deliveryRequestRepository = deliveryRequestRepository;
            _config = config;
            _branchRepository = branchRepository;
            _logger = logger;
            _donatedRequestRepository = donatedRequestRepository;
            _aidRequestRepository = aidRequestRepository;
            _deliveryItemRepository = deliveryItemRepository;
            _stockRepository = stockRepository;
            _itemRepository = itemRepository;
            _collaboratorRepository = collaboratorRepository;
            _scheduledRouteRepository = scheduledRouteRepository;
            _userRepository = userRepository;
            _aidItemRepository = aidItemRepository;
            _stockUpdatedHistoryRepository = stockUpdatedHistoryRepository;
            _stockUpdatedHistoryDetailRepository = stockUpdatedHistoryDetailRepository;
            _charityUnitRepository = charityUnitRepository;
            _reportRepository = reportRepository;
            _scheduledRouteDeliveryRequestRepository = scheduledRouteDeliveryRequestRepository;
            _activityBranchRepository = activityBranchRepository;
            _notificationRepository = notificationRepository;
            _hubContext = hubContext;
            _firebaseNotificationService = firebaseNotificationService;
            _serviceProvider = serviceProvider;
        }

        public async Task<CommonResponse> CreateDeliveryRequestsForDonatedRequestToBranchAsync(
            Guid userId,
            DeliveryRequestsForDonatedRequestToBranchCreatingRequest deliveryRequestCreatingRequest
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

                DonatedRequest? donatedRequest =
                    await _donatedRequestRepository.FindAcceptedDonatedRequestByIdAndBranchIdAsync(
                        deliveryRequestCreatingRequest.DonatedRequestId!,
                        branch.Id
                    );
                if (donatedRequest == null)
                    return new CommonResponse
                    {
                        Status = 400,
                        Message = _config[
                            "ResponseMessages:DonatedRequestMsg:WaitingForDeliveringDonatedRequestNotFoundMsg"
                        ]
                    };

                List<ScheduledTime> scheduledTimesOfDonatedRequest = JsonConvert.DeserializeObject<
                    List<ScheduledTime>
                >(donatedRequest.ScheduledTimes)!;

                if (
                    !deliveryRequestCreatingRequest.ScheduledTimes.All(
                        st => scheduledTimesOfDonatedRequest.Any(stdr => stdr.Equals(st))
                    )
                )
                    return new CommonResponse
                    {
                        Status = 400,
                        Message = _config[
                            "ResponseMessages:DonatedRequestMsg:ScheduledTimeNotFoundMsg"
                        ]
                    };

                List<DeliveryItemRequest> deliveryItemRequestRemains = donatedRequest.DonatedItems
                    .Select(
                        di => new DeliveryItemRequest { ItemId = di.ItemId, Quantity = di.Quantity }
                    )
                    .ToList();

                foreach (
                    List<DeliveryItemRequest> deliveryItemRequests in deliveryRequestCreatingRequest.DeliveryItemsForDeliveries
                )
                {
                    foreach (DeliveryItemRequest deliveryItemRequest in deliveryItemRequests)
                    {
                        if (
                            !donatedRequest.DonatedItems
                                .Select(di => di.ItemId)
                                .Contains(deliveryItemRequest.ItemId)
                        )
                            return new CommonResponse
                            {
                                Status = 400,
                                Message = _config[
                                    "ResponseMessages:DonatedRequestMsg:AcceptedDonatedItemNotFoundInListMsg"
                                ]
                            };
                        else
                        {
                            foreach (
                                DeliveryItemRequest deliveryItemRequestRemain in deliveryItemRequestRemains
                            )
                            {
                                if (deliveryItemRequestRemain.ItemId == deliveryItemRequest.ItemId)
                                    deliveryItemRequestRemain.Quantity -=
                                        deliveryItemRequest.Quantity;
                            }
                        }
                    }

                    if (
                        !IsTotalTransportVolumeValidForDonatedRequestToBranch(
                            deliveryItemRequests,
                            donatedRequest
                        )
                    )
                        return new CommonResponse
                        {
                            Status = 400,
                            Message = _config[
                                "ResponseMessages:DeliveryRequestMsg:TotalTransportVolumnNotInLimit"
                            ]
                        };
                }

                if (!deliveryItemRequestRemains.All(dirr => dirr.Quantity == 0))
                    return new CommonResponse
                    {
                        Status = 400,
                        Message = _config[
                            "ResponseMessages:DeliveryRequestMsg:TotalQuantityNotMatchMsg"
                        ]
                    };

                donatedRequest.Status = DonatedRequestStatus.PROCESSING;

                using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
                {
                    foreach (
                        List<DeliveryItemRequest> deliveryItemRequests in deliveryRequestCreatingRequest.DeliveryItemsForDeliveries
                    )
                    {
                        DeliveryRequest deliveryRequest = new DeliveryRequest
                        {
                            Note = deliveryRequestCreatingRequest.Note,
                            CreatedDate = SettedUpDateTime.GetCurrentVietNamTime(),
                            ScheduledTimes = JsonConvert.SerializeObject(
                                deliveryRequestCreatingRequest.ScheduledTimes
                            ),
                            BranchId = branch.Id,
                            DonatedRequestId = deliveryRequestCreatingRequest.DonatedRequestId,
                            Status = DeliveryRequestStatus.PENDING
                        };

                        if (
                            await _deliveryRequestRepository.CreateDeliveryRequestAsync(
                                deliveryRequest
                            ) != 1
                        )
                            return new CommonResponse
                            {
                                Status = 500,
                                Message = _config[
                                    "ResponseMessages:CommonMsg:InternalServerErrorMsg"
                                ]
                            };

                        List<DeliveryItem> deliveryItems = deliveryItemRequests
                            .Select(
                                dir =>
                                    new DeliveryItem
                                    {
                                        Quantity = dir.Quantity,
                                        DeliveryRequestId = deliveryRequest.Id,
                                        DonatedItemId = donatedRequest.DonatedItems
                                            .FirstOrDefault(di => di.ItemId == dir.ItemId)!
                                            .Id
                                    }
                            )
                            .ToList();
                        if (
                            await _deliveryItemRepository.AddDeliveryItemsAsync(deliveryItems)
                            != deliveryItems.Count
                        )
                            return new CommonResponse
                            {
                                Status = 500,
                                Message = _config[
                                    "ResponseMessages:CommonMsg:InternalServerErrorMsg"
                                ]
                            };
                    }

                    if (
                        await _donatedRequestRepository.UpdateDonatedRequestAsync(donatedRequest)
                        != 1
                    )
                    {
                        return new CommonResponse
                        {
                            Status = 500,
                            Message = _config["ResponseMessages:CommonMsg:InternalServerErrorMsg"]
                        };
                    }

                    scope.Complete();
                    return new CommonResponse
                    {
                        Status = 200,
                        Message = _config[
                            "ResponseMessages:DeliveryRequestMsg:CreateDeliveryRequestsSuccessMsg"
                        ]
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    $"An exception occurred in service {nameof(DeliveryRequestService)}, method {nameof(CreateDeliveryRequestsForDonatedRequestToBranchAsync)}."
                );
            }
            return new CommonResponse
            {
                Status = 500,
                Message = _config["ResponseMessages:CommonMsg:InternalServerErrorMsg"]
            };
        }

        public async Task<CommonResponse> CreateDeliveryRequestsForBranchToAidRequestAsync(
            Guid userId,
            DeliveryRequestsForBranchToAidRequestCreatingRequest deliveryRequestCreatingRequest
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

                AidRequest? aidRequest =
                    await _aidRequestRepository.FindAcceptedOrProcessingAidRequestByIdAndBranchIdAsync(
                        deliveryRequestCreatingRequest.AidRequestId,
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

                if (aidRequest.IsSelfShipping)
                    return new CommonResponse
                    {
                        Status = 400,
                        Message = _config[
                            "ResponseMessages:AidRequest:CanNotMakeDeliveryForSelfShippingAidRequest"
                        ]
                    };

                //if (deliveryRequestCreatingRequest.ActivityId != null)
                //{
                //    ActivityBranch? activityBranch =
                //        await _activityBranchRepository.FindActivityBranchByActivityIdAndBranchIdAsync(
                //            (Guid)deliveryRequestCreatingRequest.ActivityId,
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

                List<ScheduledTime> scheduledTimesOfAidRequest = JsonConvert.DeserializeObject<
                    List<ScheduledTime>
                >(aidRequest.ScheduledTimes)!;

                if (
                    !deliveryRequestCreatingRequest.ScheduledTimes.All(
                        st => scheduledTimesOfAidRequest.Any(stdr => stdr.Equals(st))
                    )
                )
                    return new CommonResponse
                    {
                        Status = 400,
                        Message = _config["ResponseMessages:AidRequest:ScheduledTimeNotFoundMsg"]
                    };

                List<Guid> neededToExportItemIds = new List<Guid>();
                deliveryRequestCreatingRequest.DeliveryItemsForDeliveries.ForEach(
                    dis => neededToExportItemIds.AddRange(dis.Select(di => di.ItemId))
                );
                neededToExportItemIds = neededToExportItemIds.DistinctBy(id => id).ToList();

                List<DeliveryItemRequest> currentStocks = new List<DeliveryItemRequest>();

                DateTime tmp = GetEndDateTimeFromScheduledTime(
                    GetLastAvailabeScheduledTime(deliveryRequestCreatingRequest.ScheduledTimes)!
                );

                DateTime endOfLastScheduledTime = new DateTime(tmp.Year, tmp.Month, tmp.Day);
                List<DeliveryItemRequest> pendingDeliveryItems = new List<DeliveryItemRequest>();

                foreach (Guid itemId in neededToExportItemIds)
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
                                        ) >= endOfLastScheduledTime.AddDays(1)
                                )
                                .Sum(s => s.Quantity)
                        }
                    );

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

                using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
                {
                    List<AidItem> aidItems = aidRequest.AidItems;

                    //check đồ vận chuyển có đúng với đồ mà aid request cần và check stock khả dụng
                    foreach (
                        List<DeliveryItemRequest> deliveryItemRequests in deliveryRequestCreatingRequest.DeliveryItemsForDeliveries
                    )
                    {
                        foreach (DeliveryItemRequest deliveryItemRequest in deliveryItemRequests)
                        {
                            if (
                                !aidItems
                                    .Select(ai => ai.ItemId)
                                    .Contains(deliveryItemRequest.ItemId)
                            )
                            {
                                //Item? item = await _itemRepository.FindItemByIdAsync(
                                //    deliveryItemRequest.ItemId
                                //);
                                //if (
                                //    item == null
                                //    || item.Status == ItemStatus.INACTIVE
                                //    || item.ItemTemplate == null
                                //    || item.ItemTemplate.Status == ItemTemplateStatus.INACTIVE
                                //)
                                //{
                                //    return new CommonResponse
                                //    {
                                //        Status = 400,
                                //        Message = _config[
                                //            "ResponseMessages:ItemTemplateMsg:ItemTemplateNotFoundInListMsg"
                                //        ]
                                //    };
                                //}

                                //AidItem aidItem = new AidItem
                                //{
                                //    Quantity = 0,
                                //    Status = AidItemStatus.REPLACED,
                                //    AidRequestId = aidRequest.Id,
                                //    ItemId = item.Id
                                //};

                                //if (await _aidItemRepository.CreateAidItemAsync(aidItem) != 1)
                                //    return new CommonResponse
                                //    {
                                //        Status = 500,
                                //        Message = _config[
                                //            "ResponseMessages:CommonMsg:InternalServerErrorMsg"
                                //        ]
                                //    };

                                //aidItem.Item = item;
                                //aidItems.Add(aidItem);

                                return new CommonResponse
                                {
                                    Status = 400,
                                    Message = _config[
                                        "ResponseMessages:AidRequest:AcceptedAidItemNotFoundInListMsg"
                                    ]
                                };
                            }
                            foreach (DeliveryItemRequest currentStock in currentStocks)
                            {
                                if (currentStock.ItemId == deliveryItemRequest.ItemId)
                                {
                                    currentStock.Quantity -= deliveryItemRequest.Quantity;
                                }
                            }
                        }

                        if (
                            !IsTotalTransportVolumeValidForBranchToAidRequest(
                                deliveryItemRequests,
                                aidItems
                            )
                        )
                            return new CommonResponse
                            {
                                Status = 400,
                                Message = _config[
                                    "ResponseMessages:DeliveryRequestMsg:TotalTransportVolumnNotInLimit"
                                ]
                            };
                    }

                    if (currentStocks.Any(s => s.Quantity < 0))
                        return new CommonResponse
                        {
                            Status = 400,
                            Message = _config[
                                "ResponseMessages:StockMsg:CurrentStockNotEnoughToDeliverToAidRequestMsg"
                            ]
                        };

                    aidRequest.Status = AidRequestStatus.PROCESSING;

                    foreach (
                        List<DeliveryItemRequest> deliveryItemRequests in deliveryRequestCreatingRequest.DeliveryItemsForDeliveries
                    )
                    {
                        DeliveryRequest deliveryRequest = new DeliveryRequest
                        {
                            Note = deliveryRequestCreatingRequest.Note,
                            CreatedDate = SettedUpDateTime.GetCurrentVietNamTime(),
                            ScheduledTimes = JsonConvert.SerializeObject(
                                deliveryRequestCreatingRequest.ScheduledTimes
                            ),
                            BranchId = branch.Id,
                            AidRequestId = deliveryRequestCreatingRequest.AidRequestId,
                            Status = DeliveryRequestStatus.PENDING
                        };

                        if (
                            await _deliveryRequestRepository.CreateDeliveryRequestAsync(
                                deliveryRequest
                            ) != 1
                        )
                            return new CommonResponse
                            {
                                Status = 500,
                                Message = _config[
                                    "ResponseMessages:CommonMsg:InternalServerErrorMsg"
                                ]
                            };

                        List<DeliveryItem> deliveryItems = deliveryItemRequests
                            .Select(
                                dir =>
                                    new DeliveryItem
                                    {
                                        Quantity = dir.Quantity,
                                        DeliveryRequestId = deliveryRequest.Id,
                                        AidItemId = aidRequest.AidItems
                                            .FirstOrDefault(ai => ai.ItemId == dir.ItemId)!
                                            .Id
                                    }
                            )
                            .ToList();
                        if (
                            await _deliveryItemRepository.AddDeliveryItemsAsync(deliveryItems)
                            != deliveryItems.Count
                        )
                            return new CommonResponse
                            {
                                Status = 500,
                                Message = _config[
                                    "ResponseMessages:CommonMsg:InternalServerErrorMsg"
                                ]
                            };

                        StockUpdatedHistory stockUpdatedHistory = new StockUpdatedHistory
                        {
                            Type = StockUpdatedHistoryType.EXPORT,
                            BranchId = branch.Id,
                            CreatedBy = userId
                        };

                        if (
                            await _stockUpdatedHistoryRepository.AddStockUpdatedHistoryAsync(
                                stockUpdatedHistory
                            ) != 1
                        )
                            return new CommonResponse
                            {
                                Status = 500,
                                Message = _config[
                                    "ResponseMessages:CommonMsg:InternalServerErrorMsg"
                                ]
                            };

                        List<StockUpdatedHistoryDetail> stockUpdatedHistoryDetails = deliveryItems
                            .Select(
                                di =>
                                    new StockUpdatedHistoryDetail
                                    {
                                        Quantity = di.Quantity,
                                        StockUpdatedHistoryId = stockUpdatedHistory.Id,
                                        DeliveryItemId = di.Id,
                                        AidRequestId = aidRequest.Id
                                    }
                            )
                            .ToList();

                        if (
                            await _stockUpdatedHistoryDetailRepository.AddStockUpdatedHistoryDetailsAsync(
                                stockUpdatedHistoryDetails
                            ) != stockUpdatedHistoryDetails.Count
                        )
                            return new CommonResponse
                            {
                                Status = 500,
                                Message = _config[
                                    "ResponseMessages:CommonMsg:InternalServerErrorMsg"
                                ]
                            };
                    }

                    if (await _aidRequestRepository.UpdateAidRequestAsync(aidRequest) != 1)
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
                            "ResponseMessages:DeliveryRequestMsg:CreateDeliveryRequestsSuccessMsg"
                        ]
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    $"An exception occurred in service {nameof(DeliveryRequestService)}, method {nameof(CreateDeliveryRequestsForBranchToAidRequestAsync)}."
                );
            }
            return new CommonResponse
            {
                Status = 500,
                Message = _config["ResponseMessages:CommonMsg:InternalServerErrorMsg"]
            };
        }

        public async Task<CommonResponse> CreateDeliveryRequestsForBranchToBranchAsync(
            Guid userId,
            DeliveryRequestsForBranchToBranchCreatingRequest deliveryRequestCreatingRequest
        )
        {
            try
            {
                Branch? pickupBranch = await _branchRepository.FindBranchByBranchAdminIdAsync(
                    userId
                );

                if (pickupBranch == null || pickupBranch.Status == BranchStatus.INACTIVE)
                {
                    return new CommonResponse
                    {
                        Status = 403,
                        Message = _config["ResponseMessages:BranchMsg:BranchNotFoundOrInactiveMsg"]
                    };
                }

                AidRequest? aidRequest =
                    await _aidRequestRepository.FindAcceptedOrProcessingAidRequestByIdAndBranchIdAsync(
                        deliveryRequestCreatingRequest.AidRequestId,
                        pickupBranch.Id
                    );

                if (aidRequest == null || aidRequest.BranchId == null)
                    return new CommonResponse
                    {
                        Status = 400,
                        Message = _config[
                            "ResponseMessages:AidRequest:AcceptedAidRequestFromBranchNotFoundMsg"
                        ]
                    };

                //Branch? deliveredBranch = await _branchRepository.FindBranchByIdAsync(
                //    (Guid)aidRequest.BranchId
                //);

                //if (deliveredBranch == null)
                //{
                //    return new CommonResponse
                //    {
                //        Status = 403,
                //        Message = _config["ResponseMessages:BranchMsg:BranchNotFoundMsg"]
                //    };
                //}

                if (aidRequest.IsSelfShipping)
                    return new CommonResponse
                    {
                        Status = 400,
                        Message = _config[
                            "ResponseMessages:AidRequest:CanNotMakeDeliveryForSelfShippingAidRequest"
                        ]
                    };

                List<ScheduledTime> scheduledTimesOfAidRequest = JsonConvert.DeserializeObject<
                    List<ScheduledTime>
                >(aidRequest.ScheduledTimes)!;

                if (
                    !deliveryRequestCreatingRequest.ScheduledTimes.All(
                        st => scheduledTimesOfAidRequest.Any(stdr => stdr.Equals(st))
                    )
                )
                    return new CommonResponse
                    {
                        Status = 400,
                        Message = _config["ResponseMessages:AidRequest:ScheduledTimeNotFoundMsg"]
                    };

                List<Guid> neededToExportItemIds = new List<Guid>();
                deliveryRequestCreatingRequest.DeliveryItemsForDeliveries.ForEach(
                    dis => neededToExportItemIds.AddRange(dis.Select(di => di.ItemId))
                );
                neededToExportItemIds = neededToExportItemIds.DistinctBy(id => id).ToList();

                List<DeliveryItemRequest> currentStocks = new List<DeliveryItemRequest>();
                DateTime tmp = GetEndDateTimeFromScheduledTime(
                    GetLastAvailabeScheduledTime(deliveryRequestCreatingRequest.ScheduledTimes)!
                );

                DateTime endOfLastScheduledTime = new DateTime(tmp.Year, tmp.Month, tmp.Day);
                foreach (Guid itemId in neededToExportItemIds)
                {
                    List<Stock> stocks =
                        await _stockRepository.GetCurrentValidStocksByItemIdAndBranchId(
                            itemId,
                            pickupBranch.Id
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
                                        ) >= endOfLastScheduledTime.AddDays(1)
                                )
                                .Sum(s => s.Quantity)
                        }
                    );
                }

                //tính stock không khả dụng
                List<DeliveryItemRequest> pendingDeliveryItems = new List<DeliveryItemRequest>();
                foreach (Guid itemId in neededToExportItemIds)
                {
                    List<DeliveryItem> deliveryItems =
                        await _deliveryItemRepository.GetPendingDeliveryItemsByItemIdAndBranchIdOfAidDeliveryRequest(
                            itemId,
                            pickupBranch.Id
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

                using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
                {
                    List<AidItem> aidItems = aidRequest.AidItems;

                    //check đồ vận chuyển có đúng với đồ mà aid request cần và check stock khả dụng
                    foreach (
                        List<DeliveryItemRequest> deliveryItemRequests in deliveryRequestCreatingRequest.DeliveryItemsForDeliveries
                    )
                    {
                        foreach (DeliveryItemRequest deliveryItemRequest in deliveryItemRequests)
                        {
                            if (
                                !aidItems
                                    .Select(ai => ai.ItemId)
                                    .Contains(deliveryItemRequest.ItemId)
                            )
                            {
                                //Item? item = await _itemRepository.FindItemByIdAsync(
                                //    deliveryItemRequest.ItemId
                                //);
                                //if (
                                //    item == null
                                //    || item.Status == ItemStatus.INACTIVE
                                //    || item.ItemTemplate == null
                                //    || item.ItemTemplate.Status == ItemTemplateStatus.INACTIVE
                                //)
                                //{
                                //    return new CommonResponse
                                //    {
                                //        Status = 400,
                                //        Message = _config[
                                //            "ResponseMessages:ItemTemplateMsg:ItemTemplateNotFoundInListMsg"
                                //        ]
                                //    };
                                //}

                                //AidItem aidItem = new AidItem
                                //{
                                //    Quantity = 0,
                                //    Status = AidItemStatus.REPLACED,
                                //    AidRequestId = aidRequest.Id,
                                //    ItemId = item.Id
                                //};

                                //if (await _aidItemRepository.CreateAidItemAsync(aidItem) != 1)
                                //    return new CommonResponse
                                //    {
                                //        Status = 500,
                                //        Message = _config[
                                //            "ResponseMessages:CommonMsg:InternalServerErrorMsg"
                                //        ]
                                //    };

                                //aidItem.Item = item;
                                //aidItems.Add(aidItem);

                                return new CommonResponse
                                {
                                    Status = 400,
                                    Message = _config[
                                        "ResponseMessages:AidRequest:AcceptedAidItemNotFoundInListMsg"
                                    ]
                                };
                            }

                            foreach (DeliveryItemRequest currentStock in currentStocks)
                            {
                                if (currentStock.ItemId == deliveryItemRequest.ItemId)
                                {
                                    currentStock.Quantity -= deliveryItemRequest.Quantity;
                                }
                            }
                        }

                        if (
                            !IsTotalTransportVolumeValidForBranchToBranch(
                                deliveryItemRequests,
                                aidItems
                            )
                        )
                            return new CommonResponse
                            {
                                Status = 400,
                                Message = _config[
                                    "ResponseMessages:DeliveryRequestMsg:TotalTransportVolumnNotInLimit"
                                ]
                            };
                    }

                    if (currentStocks.Any(s => s.Quantity < 0))
                        return new CommonResponse
                        {
                            Status = 400,
                            Message = _config[
                                "ResponseMessages:StockMsg:CurrentStockNotEnoughToDeliverToAidRequestMsg"
                            ]
                        };

                    aidRequest.Status = AidRequestStatus.PROCESSING;

                    foreach (
                        List<DeliveryItemRequest> deliveryItemRequests in deliveryRequestCreatingRequest.DeliveryItemsForDeliveries
                    )
                    {
                        DeliveryRequest deliveryRequest = new DeliveryRequest
                        {
                            Note = deliveryRequestCreatingRequest.Note,
                            CreatedDate = SettedUpDateTime.GetCurrentVietNamTime(),
                            ScheduledTimes = JsonConvert.SerializeObject(
                                deliveryRequestCreatingRequest.ScheduledTimes
                            ),
                            BranchId = pickupBranch.Id,
                            AidRequestId = deliveryRequestCreatingRequest.AidRequestId,
                            Status = DeliveryRequestStatus.PENDING
                        };

                        if (
                            await _deliveryRequestRepository.CreateDeliveryRequestAsync(
                                deliveryRequest
                            ) != 1
                        )
                            return new CommonResponse
                            {
                                Status = 500,
                                Message = _config[
                                    "ResponseMessages:CommonMsg:InternalServerErrorMsg"
                                ]
                            };

                        List<DeliveryItem> deliveryItems = deliveryItemRequests
                            .Select(
                                dir =>
                                    new DeliveryItem
                                    {
                                        Quantity = dir.Quantity,
                                        DeliveryRequestId = deliveryRequest.Id,
                                        AidItemId = aidRequest.AidItems
                                            .FirstOrDefault(ai => ai.ItemId == dir.ItemId)!
                                            .Id
                                    }
                            )
                            .ToList();
                        if (
                            await _deliveryItemRepository.AddDeliveryItemsAsync(deliveryItems)
                            != deliveryItems.Count
                        )
                            return new CommonResponse
                            {
                                Status = 500,
                                Message = _config[
                                    "ResponseMessages:CommonMsg:InternalServerErrorMsg"
                                ]
                            };

                        StockUpdatedHistory stockUpdatedHistory = new StockUpdatedHistory
                        {
                            Type = StockUpdatedHistoryType.EXPORT,
                            BranchId = pickupBranch.Id,
                            CreatedBy = userId
                        };

                        if (
                            await _stockUpdatedHistoryRepository.AddStockUpdatedHistoryAsync(
                                stockUpdatedHistory
                            ) != 1
                        )
                            return new CommonResponse
                            {
                                Status = 500,
                                Message = _config[
                                    "ResponseMessages:CommonMsg:InternalServerErrorMsg"
                                ]
                            };

                        List<StockUpdatedHistoryDetail> stockUpdatedHistoryDetails = deliveryItems
                            .Select(
                                di =>
                                    new StockUpdatedHistoryDetail
                                    {
                                        Quantity = di.Quantity,
                                        StockUpdatedHistoryId = stockUpdatedHistory.Id,
                                        DeliveryItemId = di.Id,
                                        AidRequestId = aidRequest.Id
                                    }
                            )
                            .ToList();

                        if (
                            await _stockUpdatedHistoryDetailRepository.AddStockUpdatedHistoryDetailsAsync(
                                stockUpdatedHistoryDetails
                            ) != stockUpdatedHistoryDetails.Count
                        )
                            return new CommonResponse
                            {
                                Status = 500,
                                Message = _config[
                                    "ResponseMessages:CommonMsg:InternalServerErrorMsg"
                                ]
                            };
                    }

                    if (await _aidRequestRepository.UpdateAidRequestAsync(aidRequest) != 1)
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
                            "ResponseMessages:DeliveryRequestMsg:CreateDeliveryRequestsSuccessMsg"
                        ]
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    $"An exception occurred in service {nameof(DeliveryRequestService)}, method {nameof(CreateDeliveryRequestsForBranchToBranchAsync)}."
                );
            }
            return new CommonResponse
            {
                Status = 500,
                Message = _config["ResponseMessages:CommonMsg:InternalServerErrorMsg"]
            };
        }

        private bool IsTotalTransportVolumeValidForDonatedRequestToBranch(
            List<DeliveryItemRequest> deliveryItemRequests,
            DonatedRequest donatedRequest
        )
        {
            double totalTransportVolumn = 0;
            foreach (DeliveryItemRequest deliveryItemRequest in deliveryItemRequests)
            {
                Item item = donatedRequest.DonatedItems
                    .Select(di => di.Item)
                    .FirstOrDefault(i => i.Id == deliveryItemRequest.ItemId)!;
                totalTransportVolumn += deliveryItemRequest.Quantity / item.MaximumTransportVolume;
            }

            return Math.Ceiling(totalTransportVolumn * 100)
                <= int.Parse(_config["ScheduledRoute:MaxVolumnPercentToSchedule"]);
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

        private bool IsTotalTransportVolumeValidForBranchToAidRequest(
            List<DeliveryItemRequest> deliveryItemRequests,
            List<AidItem> aidItems
        )
        {
            double totalTransportVolumn = 0;
            foreach (DeliveryItemRequest deliveryItemRequest in deliveryItemRequests)
            {
                Item item = aidItems
                    .Select(ai => ai.Item)
                    .FirstOrDefault(i => i.Id == deliveryItemRequest.ItemId)!;
                totalTransportVolumn += deliveryItemRequest.Quantity / item.MaximumTransportVolume;
            }

            return Math.Ceiling(totalTransportVolumn * 100)
                <= int.Parse(_config["ScheduledRoute:MaxVolumnPercentToSchedule"]);
        }

        private bool IsTotalTransportVolumeValidForBranchToBranch(
            List<DeliveryItemRequest> deliveryItemRequests,
            List<AidItem> aidItems
        )
        {
            double totalTransportVolumn = 0;
            foreach (DeliveryItemRequest deliveryItemRequest in deliveryItemRequests)
            {
                Item item = aidItems
                    .Select(ai => ai.Item)
                    .FirstOrDefault(i => i.Id == deliveryItemRequest.ItemId)!;
                totalTransportVolumn += deliveryItemRequest.Quantity / item.MaximumTransportVolume;
            }

            return Math.Ceiling(totalTransportVolumn * 100)
                <= int.Parse(_config["ScheduledRoute:MaxVolumnPercentToSchedule"]);
        }

        public async Task<CommonResponse> GetDeliveryRequestAsync(
            int? page,
            int? pageSize,
            DeliveryFilterRequest request
        )
        {
            CommonResponse commonResponse = new CommonResponse();
            try
            {
                List<DeliveryRequest>? deliveryRequests =
                    await _deliveryRequestRepository.GetDeliveryRequestsAsync(request);

                if (deliveryRequests != null && deliveryRequests.Count > 0)
                {
                    Pagination pagination = new Pagination();
                    pagination.PageSize = pageSize == null ? 10 : pageSize.Value;
                    pagination.CurrentPage = page == null ? 1 : page.Value;
                    pagination.Total = deliveryRequests.Count;
                    deliveryRequests = deliveryRequests
                        .Skip((pagination.CurrentPage - 1) * pagination.PageSize)
                        .Take(pagination.PageSize)
                        .ToList();
                    commonResponse.Pagination = pagination;
                    List<DeliveryRequestResponse> deliveryRequestResponses =
                        new List<DeliveryRequestResponse>();
                    foreach (var d in deliveryRequests)
                    {
                        DeliveryRequestResponse deliveryRequestResponse =
                            new DeliveryRequestResponse();
                        PickUpPointResponse? pickUpPointResponse = null;
                        DeliveryPointResponse? deliveryPointResponse = null;
                        // từ donated request về branch
                        if (d.DonatedRequestId != null)
                        {
                            deliveryRequestResponse.DeliveryType =
                                DeliveryType.DONATED_REQUEST_TO_BRANCH.ToString();
                            pickUpPointResponse = new PickUpPointResponse
                            {
                                UserId = d.DonatedRequest?.UserId ?? Guid.Empty,
                                Avatar = d.DonatedRequest?.User?.Avatar ?? string.Empty,
                                Name = d.DonatedRequest?.User?.Name ?? string.Empty,
                                Email = d.DonatedRequest?.User?.Email ?? string.Empty,
                                Phone = d.DonatedRequest?.User?.Phone ?? string.Empty,
                                Address = d.DonatedRequest?.Address ?? string.Empty,
                                Location = d.DonatedRequest?.Location ?? string.Empty
                            };
                            deliveryPointResponse = new DeliveryPointResponse
                            {
                                BranchId = d.Branch?.Id ?? Guid.Empty,
                                Avatar = d.Branch?.Image ?? string.Empty,
                                Name = d.Branch?.Name ?? string.Empty,
                                Email = d.Branch?.BranchAdmin?.Email ?? string.Empty,
                                Phone = d.Branch?.BranchAdmin?.Phone ?? string.Empty,
                                Address = d.Branch?.Address ?? string.Empty,
                                Location = d.Branch?.Location ?? string.Empty
                            };
                        }
                        else if (d.AidRequestId != null && d.AidRequest!.CharityUnitId != null)
                        {
                            deliveryRequestResponse.DeliveryType =
                                DeliveryType.BRANCH_TO_AID_REQUEST.ToString();
                            pickUpPointResponse = new PickUpPointResponse
                            {
                                BranchId = d.Branch?.Id ?? Guid.Empty,
                                Avatar = d.Branch?.Image ?? string.Empty,
                                Name = d.Branch?.Name ?? string.Empty,
                                Email = d.Branch?.BranchAdmin?.Email ?? string.Empty,
                                Phone = d.Branch?.BranchAdmin?.Phone ?? string.Empty,
                                Address = d.Branch?.Address ?? string.Empty,
                                Location = d.Branch?.Location ?? string.Empty
                            };
                            deliveryPointResponse = new DeliveryPointResponse
                            {
                                CharityUnitId = d.AidRequest.CharityUnitId,
                                Avatar = d.AidRequest?.CharityUnit?.Image ?? string.Empty,
                                Name = d.AidRequest?.CharityUnit?.Name ?? string.Empty,
                                Email = d.AidRequest?.CharityUnit?.User.Email ?? string.Empty,
                                Phone = d.AidRequest?.CharityUnit?.User.Phone ?? string.Empty,
                                Address = d.AidRequest?.Address ?? string.Empty,
                                Location = d.AidRequest?.Location ?? string.Empty
                            };
                        }
                        else if (d.AidRequestId != null && d.AidRequest!.BranchId != null)
                        {
                            deliveryRequestResponse.DeliveryType =
                                DeliveryType.BRANCH_TO_BRANCH.ToString();
                            pickUpPointResponse = new PickUpPointResponse
                            {
                                BranchId = d.Branch?.Id ?? Guid.Empty,
                                Avatar = d.Branch?.Image ?? string.Empty,
                                Name = d.Branch?.Name ?? string.Empty,
                                Email = d.Branch?.BranchAdmin?.Email ?? string.Empty,
                                Phone = d.Branch?.BranchAdmin?.Phone ?? string.Empty,
                                Address = d.Branch?.Address ?? string.Empty,
                                Location = d.Branch?.Location ?? string.Empty
                            };
                            deliveryPointResponse = new DeliveryPointResponse
                            {
                                BranchId = d.AidRequest.BranchId,
                                Avatar = d.AidRequest?.Branch?.Image ?? string.Empty,
                                Name = d.AidRequest?.Branch?.Name ?? string.Empty,
                                Email = d.AidRequest?.Branch?.BranchAdmin!.Email ?? string.Empty,
                                Phone = d.AidRequest?.Branch?.BranchAdmin!.Phone ?? string.Empty,
                                Address = d.AidRequest?.Address ?? string.Empty,
                                Location = d.AidRequest?.Location ?? string.Empty,
                            };
                        }

                        deliveryRequestResponse.CreatedDate = d.CreatedDate;
                        deliveryRequestResponse.Id = d.Id;
                        deliveryRequestResponse.Status = d.Status.ToString();
                        deliveryRequestResponse.PickUpPoint = pickUpPointResponse;
                        deliveryRequestResponse.DeliveryPoint = deliveryPointResponse;
                        List<ItemResponse> itemResponses = new List<ItemResponse>();
                        foreach (var i in d.DeliveryItems)
                        {
                            ItemResponse? itemResponse = null;
                            if (i.DonatedItem != null)
                            {
                                Item? Item = await _itemRepository.FindItemByIdAsync(
                                    i.DonatedItem.ItemId
                                );
                                itemResponse = new ItemResponse
                                {
                                    Id = Item!.Id,
                                    Image = Item.Image,
                                    Note = Item.Note ?? string.Empty,
                                    Unit = Item.ItemTemplate.Unit.Name,
                                    EstimatedExpirationDays = Item.EstimatedExpirationDays,
                                    MaximumTransportVolume = Item.MaximumTransportVolume,
                                    Name = Item.ItemTemplate.Name,
                                    AttributeValues = Item.ItemAttributeValues
                                        .Select(a => a.AttributeValue.Value)
                                        .ToList(),
                                };
                            }

                            if (i.AidItem != null)
                            {
                                Item? Item = await _itemRepository.FindItemByIdAsync(
                                    i.AidItem.ItemId
                                );
                                itemResponse = new ItemResponse
                                {
                                    Id = Item!.Id,
                                    Image = Item.Image,
                                    Note = Item.Note ?? string.Empty,
                                    Unit = Item.ItemTemplate.Unit.Name,
                                    EstimatedExpirationDays = Item.EstimatedExpirationDays,
                                    MaximumTransportVolume = Item.MaximumTransportVolume,
                                    Name = Item.ItemTemplate.Name,
                                    AttributeValues = Item.ItemAttributeValues
                                        .Select(a => a.AttributeValue.Value)
                                        .ToList(),
                                };
                            }
                            itemResponses.Add(itemResponse!);
                        }

                        deliveryRequestResponse.ItemResponses = itemResponses;
                        deliveryRequestResponses.Add(deliveryRequestResponse);
                    }

                    commonResponse.Data = deliveryRequestResponses;
                }
                commonResponse.Status = 200;
                return commonResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    $"An exception occurred in service {nameof(DeliveryRequestService)}, method {nameof(GetDeliveryRequestAsync)}."
                );
            }
            return new CommonResponse
            {
                Status = 500,
                Message = _config["ResponseMessages:CommonMsg:InternalServerErrorMsg"]
            };
        }

        public async Task<CommonResponse> UpdateNextStatusOfDeliveryRequestAsync(
            Guid userId,
            Guid deliveryRequestId
        )
        {
            try
            {
                User? user = await _userRepository.FindUserByIdAsync(userId);

                if (user == null || !user.IsCollaborator)
                    return new CommonResponse
                    {
                        Status = 403,
                        Message = _config[
                            "ResponseMessages:CollaboratorMsg:CollaboratorNotFoundMsg"
                        ]
                    };

                DeliveryRequest? deliveryRequest =
                    await _deliveryRequestRepository.FindProcessingDeliveryRequestByIdAsync(
                        deliveryRequestId
                    );

                if (deliveryRequest == null)
                    return new CommonResponse
                    {
                        Status = 400,
                        Message = _config[
                            "ResponseMessages:DeliveryRequestMsg:ProcessingDeliveryRequestNotFoundMsg"
                        ]
                    };

                ScheduledRouteDeliveryRequest? tmp =
                    deliveryRequest.ScheduledRouteDeliveryRequests.FirstOrDefault(
                        srdr => srdr.Status == ScheduledRouteDeliveryRequestStatus.SCHEDULED
                    );

                ScheduledRoute? scheduledRoute =
                    tmp != null
                        ? tmp.ScheduledRoute.UserId == userId
                            ? tmp.ScheduledRoute
                            : null
                        : null;

                if (scheduledRoute == null)
                    return new CommonResponse
                    {
                        Status = 400,
                        Message = _config[
                            "ResponseMessages:DeliveryRequestMsg:ProcessingDeliveryRequestNotFoundMsg"
                        ]
                    };

                DeliveryType deliveryType = GetDeliveryTypeOfDeliveryRequest(deliveryRequest);

                deliveryRequest.Status = CheckAndGetNextDeliveryRequestStatus(
                    deliveryRequest.Status,
                    deliveryType
                );

                using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
                {
                    if (
                        await _deliveryRequestRepository.UpdateDeliveryRequestAsync(deliveryRequest)
                        != 1
                    )
                        return new CommonResponse
                        {
                            Status = 500,
                            Message = _config["ResponseMessages:CommonMsg:InternalServerErrorMsg"]
                        };

                    using (var serviceScope = _serviceProvider.CreateScope())
                    {
                        var _scheduledRouteService =
                            serviceScope.ServiceProvider.GetRequiredService<IScheduledRouteService>();

                        await _scheduledRouteService.SendNotificationBaseOnDeliveryRequestStatusOfDeliveryRequest(
                            deliveryRequest
                        );
                    }

                    scope.Complete();
                    return new CommonResponse
                    {
                        Status = 200,
                        Message = _config[
                            "ResponseMessages:DeliveryRequestMsg:UpdateDeliverySuccessMsg"
                        ]
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    $"An exception occurred in service {nameof(DeliveryRequestService)}, method {nameof(UpdateNextStatusOfDeliveryRequestAsync)}."
                );
            }
            return new CommonResponse
            {
                Status = 500,
                Message = _config["ResponseMessages:CommonMsg:InternalServerErrorMsg"]
            };
        }

        public async Task<CommonResponse> GetDeliveryRequestDetailsAsync(
            Guid? userId,
            Guid deliveryId
        )
        {
            CommonResponse commonResponse = new CommonResponse();
            try
            {
                DeliveryRequest? d =
                    await _deliveryRequestRepository.GetDeliveryRequestsDetailAsync(
                        deliveryId,
                        userId
                    );

                if (d != null)
                {
                    DeliveryRequestResponseDetails deliveryRequestResponse =
                        new DeliveryRequestResponseDetails();
                    PickUpPointResponse? pickUpPointResponse = null;
                    DeliveryPointResponse? deliveryPointResponse = null;
                    // từ donated request về branch
                    if (d.DonatedRequestId != null)
                    {
                        deliveryRequestResponse.DeliveryType =
                            DeliveryType.DONATED_REQUEST_TO_BRANCH.ToString();
                        pickUpPointResponse = new PickUpPointResponse
                        {
                            UserId = d.DonatedRequest?.UserId ?? Guid.Empty,
                            Avatar = d.DonatedRequest?.User?.Avatar ?? string.Empty,
                            Name = d.DonatedRequest?.User?.Name ?? string.Empty,
                            Email = d.DonatedRequest?.User?.Email ?? string.Empty,
                            Phone = d.DonatedRequest?.User?.Phone ?? string.Empty,
                            Address = d.DonatedRequest?.Address ?? string.Empty,
                            Location = d.DonatedRequest?.Location ?? string.Empty
                        };
                        deliveryPointResponse = new DeliveryPointResponse
                        {
                            BranchId = d.Branch?.Id ?? Guid.Empty,
                            Avatar = d.Branch?.Image ?? string.Empty,
                            Name = d.Branch?.Name ?? string.Empty,
                            Email = d.Branch?.BranchAdmin?.Email ?? string.Empty,
                            Phone = d.Branch?.BranchAdmin?.Phone ?? string.Empty,
                            Address = d.Branch?.Address ?? string.Empty,
                            Location = d.Branch?.Location ?? string.Empty
                        };
                    }
                    else if (d.AidRequestId != null && d.AidRequest!.CharityUnitId != null)
                    {
                        deliveryRequestResponse.DeliveryType =
                            DeliveryType.BRANCH_TO_AID_REQUEST.ToString();
                        pickUpPointResponse = new PickUpPointResponse
                        {
                            BranchId = d.Branch?.Id ?? Guid.Empty,
                            Avatar = d.Branch?.Image ?? string.Empty,
                            Name = d.Branch?.Name ?? string.Empty,
                            Email = d.Branch?.BranchAdmin?.Email ?? string.Empty,
                            Phone = d.Branch?.BranchAdmin?.Phone ?? string.Empty,
                            Address = d.Branch?.Address ?? string.Empty,
                            Location = d.Branch?.Location ?? string.Empty
                        };
                        deliveryPointResponse = new DeliveryPointResponse
                        {
                            CharityUnitId = d.AidRequest.CharityUnitId,
                            Avatar = d.AidRequest?.CharityUnit?.Image ?? string.Empty,
                            Name = d.AidRequest?.CharityUnit?.Name ?? string.Empty,
                            Email = d.AidRequest?.CharityUnit?.User.Email ?? string.Empty,
                            Phone = d.AidRequest?.CharityUnit?.User.Phone ?? string.Empty,
                            Address = d.AidRequest?.Address ?? string.Empty,
                            Location = d.AidRequest?.Location ?? string.Empty
                        };
                    }
                    else if (d.AidRequestId != null && d.AidRequest!.BranchId != null)
                    {
                        deliveryRequestResponse.DeliveryType =
                            DeliveryType.BRANCH_TO_BRANCH.ToString();
                        pickUpPointResponse = new PickUpPointResponse
                        {
                            BranchId = d.Branch?.Id ?? Guid.Empty,
                            Avatar = d.Branch?.Image ?? string.Empty,
                            Name = d.Branch?.Name ?? string.Empty,
                            Email = d.Branch?.BranchAdmin?.Email ?? string.Empty,
                            Phone = d.Branch?.BranchAdmin?.Phone ?? string.Empty,
                            Address = d.Branch?.Address ?? string.Empty,
                            Location = d.Branch?.Location ?? string.Empty
                        };
                        deliveryPointResponse = new DeliveryPointResponse
                        {
                            BranchId = d.AidRequest.BranchId,
                            Avatar = d.AidRequest?.Branch?.Image ?? string.Empty,
                            Name = d.AidRequest?.Branch?.Name ?? string.Empty,
                            Email = d.AidRequest?.Branch?.BranchAdmin!.Email ?? string.Empty,
                            Phone = d.AidRequest?.Branch?.BranchAdmin!.Phone ?? string.Empty,
                            Address = d.AidRequest?.Address ?? string.Empty,
                            Location = d.AidRequest?.Location ?? string.Empty,
                        };
                    }

                    deliveryRequestResponse.CreatedDate = d.CreatedDate;
                    deliveryRequestResponse.Id = d.Id;
                    deliveryRequestResponse.Status = d.Status.ToString();
                    deliveryRequestResponse.CanceledReason = d.CanceledReason;
                    if (d.CurrentScheduledTime != null)
                    {
                        // Assuming CurrentScheduledTime is a single object in JSON
                        deliveryRequestResponse.CurrentScheduledTime =
                            JsonConvert.DeserializeObject<ScheduledTime?>(d.CurrentScheduledTime);
                    }

                    if (d.ScheduledTimes != null && d.ScheduledTimes.Any())
                    {
                        // ScheduledTimes is expected to be a JSON array
                        deliveryRequestResponse.ScheduledTimes =
                            JsonConvert.DeserializeObject<List<ScheduledTime>?>(d.ScheduledTimes);
                    }

                    deliveryRequestResponse.ProofImage = d.ProofImage;
                    deliveryRequestResponse.PickUpPoint = pickUpPointResponse;
                    deliveryRequestResponse.DeliveryPointResponse = deliveryPointResponse;

                    ScheduledRoute? scheduleRotes =
                        await _scheduledRouteRepository.FindScheduledRouteByDeliveryRequestId(d.Id);
                    if (scheduleRotes != null && scheduleRotes.UserId.HasValue)
                    {
                        User? collaborator = await _userRepository.FindUserByIdAsync(
                            scheduleRotes!.UserId.Value
                        );
                        if (collaborator != null)
                        {
                            deliveryRequestResponse.Collaborator = new SimpleUserResponse
                            {
                                Avatar = collaborator.Avatar,
                                FullName = collaborator.Name != null ? collaborator.Name : "",
                                Status = collaborator.Status.ToString(),
                                Email = collaborator.Email,
                                Phone = collaborator.Phone,
                                Id = collaborator.Id,
                                Role = collaborator.Role.Name
                            };
                        }
                    }

                    List<DeliveryItemDetailsResponse> itemResponses =
                        new List<DeliveryItemDetailsResponse>();
                    foreach (var i in d.DeliveryItems)
                    {
                        DeliveryItemDetailsResponse? itemResponse = null;
                        if (i.DonatedItem != null)
                        {
                            Item? Item = await _itemRepository.FindItemByIdAsync(
                                i.DonatedItem.ItemId
                            );
                            itemResponse = new DeliveryItemDetailsResponse
                            {
                                Id = Item!.Id,
                                Image = Item.Image,
                                Note = Item.Note ?? string.Empty,
                                Unit = Item.ItemTemplate.Unit.Name,
                                EstimatedExpirationDays = Item.EstimatedExpirationDays,
                                MaximumTransportVolume = Item.MaximumTransportVolume,
                                Name = Item.ItemTemplate.Name,
                                AttributeValues = Item.ItemAttributeValues
                                    .Select(a => a.AttributeValue.Value)
                                    .ToList(),
                                Quantity = i.Quantity,
                                InitialExpirationDate = i.DonatedItem.InitialExpirationDate,
                                ReceivedQuantity = i.ReceivedQuantity,
                                Status = i.DonatedItem.Status.ToString()
                            };
                        }

                        if (i.AidItem != null)
                        {
                            Item? Item = await _itemRepository.FindItemByIdAsync(i.AidItem.ItemId);
                            itemResponse = new DeliveryItemDetailsResponse
                            {
                                Id = Item!.Id,
                                Image = Item.Image,
                                Note = Item.Note ?? string.Empty,
                                Unit = Item.ItemTemplate.Unit.Name,
                                EstimatedExpirationDays = Item.EstimatedExpirationDays,
                                MaximumTransportVolume = Item.MaximumTransportVolume,
                                Name = Item.ItemTemplate.Name,
                                AttributeValues = Item.ItemAttributeValues
                                    .Select(a => a.AttributeValue.Value)
                                    .ToList(),
                                Quantity = i.Quantity,
                                ReceivedQuantity = i.ReceivedQuantity,
                                Status = i.AidItem.Status.ToString()
                            };
                        }
                        if (itemResponse != null)
                        {
                            itemResponses.Add(itemResponse);
                        }
                    }

                    deliveryRequestResponse.ItemResponses = itemResponses;

                    commonResponse.Data = deliveryRequestResponse;
                }
                commonResponse.Status = 200;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    $"An exception occurred in service {nameof(DeliveryRequestService)}, method {nameof(GetDeliveryRequestDetailsAsync)}."
                );
                return new CommonResponse
                {
                    Status = 500,
                    Message = _config["ResponseMessages:CommonMsg:InternalServerErrorMsg"]
                };
            }
            return commonResponse;
        }

        private DeliveryRequestStatus CheckAndGetNextDeliveryRequestStatus(
            DeliveryRequestStatus deliveryRequestStatus,
            DeliveryType deliveryType
        )
        {
            if (
                deliveryType == DeliveryType.DONATED_REQUEST_TO_BRANCH
                || deliveryType == DeliveryType.BRANCH_TO_BRANCH
            )
            {
                switch (deliveryRequestStatus)
                {
                    case DeliveryRequestStatus.SHIPPING:
                    {
                        return DeliveryRequestStatus.ARRIVED_PICKUP;
                    }
                    default:
                    {
                        throw new Exception();
                    }
                }
            }
            else
            {
                switch (deliveryRequestStatus)
                {
                    case DeliveryRequestStatus.COLLECTED:
                    {
                        return DeliveryRequestStatus.ARRIVED_DELIVERY;
                    }
                    default:
                    {
                        throw new Exception();
                    }
                }
            }
        }

        public async Task<CommonResponse> UpdateDeliveryItemsOfDeliveryRequest(
            Guid userId,
            Guid deliveryRequestId,
            DeliveryItemsOfDeliveryRequestUpdatingRequest deliveryItemsOfDeliveryRequestUpdatingRequest
        )
        {
            try
            {
                User? user = await _userRepository.FindUserByIdAsync(userId);

                if (user == null || !user.IsCollaborator)
                    return new CommonResponse
                    {
                        Status = 403,
                        Message = _config[
                            "ResponseMessages:CollaboratorMsg:CollaboratorNotFoundMsg"
                        ]
                    };

                DeliveryRequest? deliveryRequest =
                    await _deliveryRequestRepository.FindProcessingDeliveryRequestByIdAsync(
                        deliveryRequestId
                    );

                if (deliveryRequest == null)
                    return new CommonResponse
                    {
                        Status = 400,
                        Message = _config[
                            "ResponseMessages:DeliveryRequestMsg:ProcessingDeliveryRequestNotFoundMsg"
                        ]
                    };

                ScheduledRouteDeliveryRequest? tmp =
                    deliveryRequest.ScheduledRouteDeliveryRequests.FirstOrDefault(
                        srdr => srdr.Status == ScheduledRouteDeliveryRequestStatus.SCHEDULED
                    );

                ScheduledRoute? scheduledRoute =
                    tmp != null
                        ? tmp.ScheduledRoute.UserId == userId
                            ? tmp.ScheduledRoute
                            : null
                        : null;

                if (scheduledRoute == null)
                    return new CommonResponse
                    {
                        Status = 400,
                        Message = _config[
                            "ResponseMessages:DeliveryRequestMsg:ProcessingDeliveryRequestNotFoundMsg"
                        ]
                    };

                DeliveryType deliveryType = GetDeliveryTypeOfDeliveryRequest(deliveryRequest);

                if (
                    !(
                        (
                            deliveryType == DeliveryType.DONATED_REQUEST_TO_BRANCH
                            || deliveryType == DeliveryType.BRANCH_TO_BRANCH
                        )
                        && deliveryRequest.Status == DeliveryRequestStatus.ARRIVED_PICKUP
                    )
                )
                    return new CommonResponse
                    {
                        Status = 400,
                        Message = _config[
                            "ResponseMessages:DeliveryRequestMsg:OnlyArrivedPickupDeliveryRequestTypeItemToBranchCanBeUpdatedDeliveryItemsMsg"
                        ]
                    };

                if (
                    !(
                        deliveryItemsOfDeliveryRequestUpdatingRequest.DeliveryItemForUpdatings
                            .Select(dr => dr.DeliveryItemId)
                            .Except(deliveryRequest.DeliveryItems.Select(di => di.Id))
                            .Count() == 0
                        && deliveryItemsOfDeliveryRequestUpdatingRequest
                            .DeliveryItemForUpdatings
                            .Count == deliveryRequest.DeliveryItems.Count
                    )
                )
                    return new CommonResponse
                    {
                        Status = 400,
                        Message = _config[
                            "ResponseMessages:ScheduledRouteMsg:DeliveredDeliveryItemNotMatchInListMsg"
                        ]
                    };

                foreach (DeliveryItem deliveryItem in deliveryRequest.DeliveryItems)
                {
                    deliveryItem.ReceivedQuantity =
                        deliveryItemsOfDeliveryRequestUpdatingRequest.DeliveryItemForUpdatings
                            .FirstOrDefault(di => di.DeliveryItemId == deliveryItem.Id)!
                            .Quantity;
                }

                deliveryRequest.Status = DeliveryRequestStatus.COLLECTED;
                deliveryRequest.ProofImage =
                    deliveryItemsOfDeliveryRequestUpdatingRequest.ProofImage;

                using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
                {
                    if (
                        await _deliveryItemRepository.UpdateDeliveryItemsAsync(
                            deliveryRequest.DeliveryItems
                        ) != deliveryRequest.DeliveryItems.Count
                    )
                        return new CommonResponse
                        {
                            Status = 500,
                            Message = _config["ResponseMessages:CommonMsg:InternalServerErrorMsg"]
                        };

                    if (
                        await _deliveryRequestRepository.UpdateDeliveryRequestAsync(deliveryRequest)
                        != 1
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
                            "ResponseMessages:DeliveryRequestMsg:UpdateDeliverySuccessMsg"
                        ]
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    $"An exception occurred in service {nameof(ScheduledRouteService)}, method {nameof(UpdateDeliveryItemsOfDeliveryRequest)}."
                );
                return new CommonResponse
                {
                    Status = 500,
                    Message = _config["ResponseMessages:CommonMsg:InternalServerErrorMsg"]
                };
            }
        }

        public async Task<CommonResponse> GetDeliveryRequestDetailsByContributorIdsync(
            Guid contributorId,
            Guid deliveryId
        )
        {
            CommonResponse commonResponse = new CommonResponse();
            try
            {
                List<DeliveryRequest>? deliveryRequests =
                    await _deliveryRequestRepository.GetDeliveryRequestsDetailForContributorAsync(
                        contributorId,
                        deliveryId
                    );
                List<DeliveryRequestResponseDetails> deliveryRequestResponseDetails =
                    new List<DeliveryRequestResponseDetails>();
                if (deliveryRequests != null && deliveryRequests.Count > 0)
                {
                    foreach (var d in deliveryRequests)
                    {
                        DeliveryRequestResponseDetails deliveryRequestResponse =
                            new DeliveryRequestResponseDetails();
                        PickUpPointResponse? pickUpPointResponse = null;
                        DeliveryPointResponse? deliveryPointResponse = null;
                        // từ donated request về branch
                        if (d.DonatedRequestId != null)
                        {
                            deliveryRequestResponse.DeliveryType =
                                DeliveryType.DONATED_REQUEST_TO_BRANCH.ToString();
                            pickUpPointResponse = new PickUpPointResponse
                            {
                                UserId = d.DonatedRequest?.UserId ?? Guid.Empty,
                                Avatar = d.DonatedRequest?.User?.Avatar ?? string.Empty,
                                Name = d.DonatedRequest?.User?.Name ?? string.Empty,
                                Email = d.DonatedRequest?.User?.Email ?? string.Empty,
                                Phone = d.DonatedRequest?.User?.Phone ?? string.Empty,
                                Address = d.DonatedRequest?.Address ?? string.Empty,
                                Location = d.DonatedRequest?.Location ?? string.Empty
                            };
                            deliveryPointResponse = new DeliveryPointResponse
                            {
                                BranchId = d.Branch?.Id ?? Guid.Empty,
                                Avatar = d.Branch?.Image ?? string.Empty,
                                Name = d.Branch?.Name ?? string.Empty,
                                Email = d.Branch?.BranchAdmin?.Email ?? string.Empty,
                                Phone = d.Branch?.BranchAdmin?.Phone ?? string.Empty,
                                Address = d.Branch?.Address ?? string.Empty,
                                Location = d.Branch?.Location ?? string.Empty
                            };
                        }
                        else if (d.AidRequestId != null && d.AidRequest!.CharityUnitId != null)
                        {
                            deliveryRequestResponse.DeliveryType =
                                DeliveryType.BRANCH_TO_AID_REQUEST.ToString();
                            pickUpPointResponse = new PickUpPointResponse
                            {
                                BranchId = d.Branch?.Id ?? Guid.Empty,
                                Avatar = d.Branch?.Image ?? string.Empty,
                                Name = d.Branch?.Name ?? string.Empty,
                                Email = d.Branch?.BranchAdmin?.Email ?? string.Empty,
                                Phone = d.Branch?.BranchAdmin?.Phone ?? string.Empty,
                                Address = d.Branch?.Address ?? string.Empty,
                                Location = d.Branch?.Location ?? string.Empty
                            };
                            deliveryPointResponse = new DeliveryPointResponse
                            {
                                CharityUnitId = d.AidRequest.CharityUnitId,
                                Avatar = d.AidRequest?.CharityUnit?.Image ?? string.Empty,
                                Name = d.AidRequest?.CharityUnit?.Name ?? string.Empty,
                                Email = d.AidRequest?.CharityUnit?.User.Email ?? string.Empty,
                                Phone = d.AidRequest?.CharityUnit?.User.Phone ?? string.Empty,
                                Address = d.AidRequest?.Address ?? string.Empty,
                                Location = d.AidRequest?.Location ?? string.Empty
                            };
                        }
                        else if (d.AidRequestId != null && d.AidRequest!.BranchId != null)
                        {
                            deliveryRequestResponse.DeliveryType =
                                DeliveryType.BRANCH_TO_BRANCH.ToString();
                            pickUpPointResponse = new PickUpPointResponse
                            {
                                BranchId = d.Branch?.Id ?? Guid.Empty,
                                Avatar = d.Branch?.Image ?? string.Empty,
                                Name = d.Branch?.Name ?? string.Empty,
                                Email = d.Branch?.BranchAdmin?.Email ?? string.Empty,
                                Phone = d.Branch?.BranchAdmin?.Phone ?? string.Empty,
                                Address = d.Branch?.Address ?? string.Empty,
                                Location = d.Branch?.Location ?? string.Empty
                            };
                            deliveryPointResponse = new DeliveryPointResponse
                            {
                                BranchId = d.AidRequest.BranchId,
                                Avatar = d.AidRequest?.Branch?.Image ?? string.Empty,
                                Name = d.AidRequest?.Branch?.Name ?? string.Empty,
                                Email = d.AidRequest?.Branch?.BranchAdmin!.Email ?? string.Empty,
                                Phone = d.AidRequest?.Branch?.BranchAdmin!.Phone ?? string.Empty,
                                Address = d.AidRequest?.Address ?? string.Empty,
                                Location = d.AidRequest?.Location ?? string.Empty,
                            };
                        }

                        deliveryRequestResponse.CreatedDate = d.CreatedDate;
                        deliveryRequestResponse.Id = d.Id;
                        deliveryRequestResponse.Status = d.Status.ToString();
                        if (d.CurrentScheduledTime != null)
                        {
                            // Assuming CurrentScheduledTime is a single object in JSON
                            deliveryRequestResponse.CurrentScheduledTime =
                                JsonConvert.DeserializeObject<ScheduledTime?>(
                                    d.CurrentScheduledTime
                                );
                        }

                        if (d.ScheduledTimes != null && d.ScheduledTimes.Any())
                        {
                            // ScheduledTimes is expected to be a JSON array
                            deliveryRequestResponse.ScheduledTimes =
                                JsonConvert.DeserializeObject<List<ScheduledTime>?>(
                                    d.ScheduledTimes
                                );
                        }

                        deliveryRequestResponse.ProofImage = d.ProofImage;
                        deliveryRequestResponse.PickUpPoint = pickUpPointResponse;
                        deliveryRequestResponse.DeliveryPointResponse = deliveryPointResponse;

                        ScheduledRoute? scheduleRotes =
                            await _scheduledRouteRepository.FindScheduledRouteByDeliveryRequestId(
                                d.Id
                            );
                        if (scheduleRotes != null && scheduleRotes.UserId.HasValue)
                        {
                            User? collaborator = await _userRepository.FindUserByIdAsync(
                                scheduleRotes!.UserId.Value
                            );
                            if (collaborator != null)
                            {
                                deliveryRequestResponse.Collaborator = new SimpleUserResponse
                                {
                                    Avatar = collaborator.Avatar,
                                    Status = collaborator.Status.ToString(),
                                    Email = collaborator.Email,
                                    Phone = collaborator.Phone,
                                    Id = collaborator.Id,
                                    Role = collaborator.Role.Name
                                };
                            }
                        }

                        List<DeliveryItemDetailsResponse> itemResponses =
                            new List<DeliveryItemDetailsResponse>();
                        foreach (var i in d.DeliveryItems)
                        {
                            DeliveryItemDetailsResponse? itemResponse = null;
                            if (i.DonatedItem != null)
                            {
                                Item? Item = await _itemRepository.FindItemByIdAsync(
                                    i.DonatedItem.ItemId
                                );
                                itemResponse = new DeliveryItemDetailsResponse
                                {
                                    Id = Item!.Id,
                                    Image = Item.Image,
                                    Note = Item.Note ?? string.Empty,
                                    Unit = Item.ItemTemplate.Unit.Name,
                                    EstimatedExpirationDays = Item.EstimatedExpirationDays,
                                    MaximumTransportVolume = Item.MaximumTransportVolume,
                                    Name = Item.ItemTemplate.Name,
                                    AttributeValues = Item.ItemAttributeValues
                                        .Select(a => a.AttributeValue.Value)
                                        .ToList(),
                                    Quantity = i.DonatedItem.Quantity,
                                    InitialExpirationDate = i.DonatedItem.InitialExpirationDate,
                                    ReceivedQuantity = i.ReceivedQuantity,
                                    Status = i.DonatedItem.Status.ToString()
                                };
                            }

                            if (i.AidItem != null)
                            {
                                Item? Item = await _itemRepository.FindItemByIdAsync(
                                    i.AidItem.ItemId
                                );
                                itemResponse = new DeliveryItemDetailsResponse
                                {
                                    Id = Item!.Id,
                                    Image = Item.Image,
                                    Note = Item.Note ?? string.Empty,
                                    Unit = Item.ItemTemplate.Unit.Name,
                                    EstimatedExpirationDays = Item.EstimatedExpirationDays,
                                    MaximumTransportVolume = Item.MaximumTransportVolume,
                                    Name = Item.ItemTemplate.Name,
                                    AttributeValues = Item.ItemAttributeValues
                                        .Select(a => a.AttributeValue.Value)
                                        .ToList(),
                                    Quantity = i.AidItem.Quantity,
                                    ReceivedQuantity = i.ReceivedQuantity,
                                    Status = i.AidItem.Status.ToString()
                                };
                            }
                            if (itemResponse != null)
                            {
                                itemResponses.Add(itemResponse);
                            }
                        }

                        deliveryRequestResponse.ItemResponses = itemResponses;
                        deliveryRequestResponseDetails.Add(deliveryRequestResponse);
                    }
                }
                commonResponse.Data = deliveryRequestResponseDetails;
                commonResponse.Status = 200;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    $"An exception occurred in service {nameof(DeliveryRequestService)}, method {nameof(GetDeliveryRequestDetailsAsync)}."
                );
                return new CommonResponse
                {
                    Status = 500,
                    Message = _config["ResponseMessages:CommonMsg:InternalServerErrorMsg"]
                };
            }
            return commonResponse;
        }

        public async Task<CommonResponse> UpdateProofImageOfDeliveryRequest(
            Guid userId,
            Guid deliveryRequestId,
            string proofImage
        )
        {
            try
            {
                User? user = await _userRepository.FindUserByIdAsync(userId);

                if (user == null || !user.IsCollaborator)
                    return new CommonResponse
                    {
                        Status = 403,
                        Message = _config[
                            "ResponseMessages:CollaboratorMsg:CollaboratorNotFoundMsg"
                        ]
                    };

                DeliveryRequest? deliveryRequest =
                    await _deliveryRequestRepository.FindProcessingDeliveryRequestByIdAsync(
                        deliveryRequestId
                    );

                if (deliveryRequest == null)
                    return new CommonResponse
                    {
                        Status = 400,
                        Message = _config[
                            "ResponseMessages:DeliveryRequestMsg:ProcessingDeliveryRequestNotFoundMsg"
                        ]
                    };

                ScheduledRouteDeliveryRequest? tmp =
                    deliveryRequest.ScheduledRouteDeliveryRequests.FirstOrDefault(
                        srdr => srdr.Status == ScheduledRouteDeliveryRequestStatus.SCHEDULED
                    );

                ScheduledRoute? scheduledRoute =
                    tmp != null
                        ? tmp.ScheduledRoute.UserId == userId
                            ? tmp.ScheduledRoute
                            : null
                        : null;

                if (scheduledRoute == null)
                    return new CommonResponse
                    {
                        Status = 400,
                        Message = _config[
                            "ResponseMessages:DeliveryRequestMsg:ProcessingDeliveryRequestNotFoundMsg"
                        ]
                    };

                DeliveryType deliveryType = GetDeliveryTypeOfDeliveryRequest(deliveryRequest);

                if (
                    !(
                        deliveryType == DeliveryType.BRANCH_TO_AID_REQUEST
                        && deliveryRequest.Status == DeliveryRequestStatus.ARRIVED_DELIVERY
                    )
                )
                    return new CommonResponse
                    {
                        Status = 400,
                        Message = _config[
                            "ResponseMessages:DeliveryRequestMsg:OnlyArrivedDeliveryDeliveryRequestTypeBranchToCharityUnitCanBeUpdatedProofImageMsg"
                        ]
                    };

                deliveryRequest.ProofImage = proofImage;
                deliveryRequest.Status = DeliveryRequestStatus.FINISHED;
                scheduledRoute.Status = ScheduledRouteStatus.FINISHED;
                scheduledRoute.FinishedDate = SettedUpDateTime.GetCurrentVietNamTime();

                using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
                {
                    if (
                        await _deliveryRequestRepository.UpdateDeliveryRequestAsync(deliveryRequest)
                        != 1
                    )
                        return new CommonResponse
                        {
                            Status = 500,
                            Message = _config["ResponseMessages:CommonMsg:InternalServerErrorMsg"]
                        };

                    if (
                        await _scheduledRouteRepository.UpdateScheduledRouteAsync(scheduledRoute)
                        != 1
                    )
                        return new CommonResponse
                        {
                            Status = 500,
                            Message = _config["ResponseMessages:CommonMsg:InternalServerErrorMsg"]
                        };

                    using (var serviceScope = _serviceProvider.CreateScope())
                    {
                        var _scheduledRouteService =
                            serviceScope.ServiceProvider.GetRequiredService<IScheduledRouteService>();

                        await _scheduledRouteService.SendNotificationBaseOnDeliveryRequestStatusOfDeliveryRequest(
                            deliveryRequest
                        );
                    }

                    scope.Complete();
                    return new CommonResponse
                    {
                        Status = 200,
                        Message = _config[
                            "ResponseMessages:DeliveryRequestMsg:UpdateDeliverySuccessMsg"
                        ]
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    $"An exception occurred in service {nameof(DeliveryRequestService)}, method {nameof(UpdateProofImageOfDeliveryRequest)}."
                );
                return new CommonResponse
                {
                    Status = 500,
                    Message = _config["ResponseMessages:CommonMsg:InternalServerErrorMsg"]
                };
            }
        }

        public async Task<CommonResponse> UpdateFinishedDeliveryRequestTypeBranchToCharityUnitAsync(
            Guid userId,
            Guid deliveryRequestId
        )
        {
            try
            {
                CharityUnit? charityUnit =
                    await _charityUnitRepository.FindCharityUnitByUserIdOnlyAsync(userId);

                if (charityUnit == null)
                    return new CommonResponse
                    {
                        Status = 403,
                        Message = _config["ResponseMessages:CharityUnitMsg:CharityUnitNotFoundMsg"]
                    };

                DeliveryRequest? deliveryRequest =
                    await _deliveryRequestRepository.FindProcessingDeliveryRequestByIdAsync(
                        deliveryRequestId
                    );

                if (deliveryRequest == null)
                    return new CommonResponse
                    {
                        Status = 400,
                        Message = _config[
                            "ResponseMessages:DeliveryRequestMsg:ProcessingDeliveryRequestNotFoundMsg"
                        ]
                    };

                if (
                    deliveryRequest.AidRequest == null
                    || deliveryRequest.AidRequest.CharityUnitId != charityUnit.Id
                )
                    return new CommonResponse
                    {
                        Status = 403,
                        Message = _config[
                            "ResponseMessages:DeliveryRequestMsg:DeliveryRequestNotDeliverToThisCharityUnitMsg"
                        ]
                    };

                ScheduledRouteDeliveryRequest? tmp =
                    deliveryRequest.ScheduledRouteDeliveryRequests.FirstOrDefault(
                        srdr => srdr.Status == ScheduledRouteDeliveryRequestStatus.SCHEDULED
                    );

                ScheduledRoute? scheduledRoute =
                    tmp != null
                        ? tmp.ScheduledRoute.UserId == userId
                            ? tmp.ScheduledRoute
                            : null
                        : null;

                if (scheduledRoute == null)
                    return new CommonResponse
                    {
                        Status = 400,
                        Message = _config[
                            "ResponseMessages:DeliveryRequestMsg:ProcessingDeliveryRequestNotFoundMsg"
                        ]
                    };

                DeliveryType deliveryType = GetDeliveryTypeOfDeliveryRequest(deliveryRequest);

                if (
                    !(
                        deliveryType == DeliveryType.BRANCH_TO_AID_REQUEST
                        && deliveryRequest.Status == DeliveryRequestStatus.DELIVERED
                    )
                )
                    return new CommonResponse
                    {
                        Status = 400,
                        Message = _config[
                            "ResponseMessages:DeliveryRequestMsg:OnlyDeliveredDeliveryRequestTypeBranchToCharityUnitCanBeUpdatedToFinishedMsg"
                        ]
                    };

                deliveryRequest.Status = DeliveryRequestStatus.FINISHED;
                scheduledRoute.Status = ScheduledRouteStatus.FINISHED;

                using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
                {
                    if (
                        await _deliveryRequestRepository.UpdateDeliveryRequestAsync(deliveryRequest)
                        != 1
                    )
                        return new CommonResponse
                        {
                            Status = 500,
                            Message = _config["ResponseMessages:CommonMsg:InternalServerErrorMsg"]
                        };

                    if (
                        await _scheduledRouteRepository.UpdateScheduledRouteAsync(scheduledRoute)
                        != 1
                    )
                        return new CommonResponse
                        {
                            Status = 500,
                            Message = _config["ResponseMessages:CommonMsg:InternalServerErrorMsg"]
                        };

                    using (var serviceScope = _serviceProvider.CreateScope())
                    {
                        var _scheduledRouteService =
                            serviceScope.ServiceProvider.GetRequiredService<IScheduledRouteService>();

                        await _scheduledRouteService.SendNotificationBaseOnDeliveryRequestStatusOfDeliveryRequest(
                            deliveryRequest
                        );
                    }

                    scope.Complete();
                    return new CommonResponse
                    {
                        Status = 200,
                        Message = _config[
                            "ResponseMessages:DeliveryRequestMsg:UpdateDeliverySuccessMsg"
                        ]
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    $"An exception occurred in service {nameof(DeliveryRequestService)}, method {nameof(UpdateFinishedDeliveryRequestTypeBranchToCharityUnitAsync)}."
                );
                return new CommonResponse
                {
                    Status = 500,
                    Message = _config["ResponseMessages:CommonMsg:InternalServerErrorMsg"]
                };
            }
        }

        public async Task<CommonResponse> SendReportByUserOrCharityUnitAsync(
            Guid userId,
            string userRoleName,
            Guid deliveryRequestId,
            ReportForUserOrCharityUnitRequest reportForUserOrCharityUnitRequest
        )
        {
            try
            {
                DeliveryRequest? deliveryRequest =
                    await _deliveryRequestRepository.FindDeliveryRequestByIdAsync(
                        deliveryRequestId
                    );

                if (
                    deliveryRequest == null
                    || deliveryRequest.ScheduledRouteDeliveryRequests.Count == 0
                )
                    return new CommonResponse
                    {
                        Status = 400,
                        Message = _config[
                            "ResponseMessages:DeliveryRequestMsg:DeliveryRequestThatCanBeReportedNotFoundMsg"
                        ]
                    };

                Report report = new Report
                {
                    Title = reportForUserOrCharityUnitRequest.Title,
                    Content = reportForUserOrCharityUnitRequest.Content,
                    UserId = userId,
                    CreatedDate = SettedUpDateTime.GetCurrentVietNamTime()
                };

                if (deliveryRequest.ScheduledRouteDeliveryRequests[0].ReportId != null)
                    return new CommonResponse
                    {
                        Status = 400,
                        Message = _config[
                            "ResponseMessages:DeliveryRequestMsg:DeliveryRequestHasBeenAlreadyReportedMsg"
                        ]
                    };

                if (userRoleName == RoleEnum.CONTRIBUTOR.ToString())
                {
                    if (
                        deliveryRequest.DonatedRequestId == null
                        || deliveryRequest.DonatedRequest!.UserId != userId
                    )
                        return new CommonResponse
                        {
                            Status = 400,
                            Message = _config[
                                "ResponseMessages:DeliveryRequestMsg:DeliveryRequestThatCanBeReportedNotFoundMsg"
                            ]
                        };

                    if (deliveryRequest.Status != DeliveryRequestStatus.FINISHED)
                        return new CommonResponse
                        {
                            Status = 400,
                            Message = _config[
                                "ResponseMessages:DeliveryRequestMsg:DeliveryRequestTypeItemToBranchMustBeFinishedToBeReportedByUserMsg"
                            ]
                        };

                    report.Type = ReportType.MISSING_ITEMS_FROM_CONTRIBUTOR;
                }
                else
                {
                    if (
                        deliveryRequest.AidRequestId == null
                        || deliveryRequest.AidRequest!.CharityUnit!.UserId != userId
                    )
                        return new CommonResponse
                        {
                            Status = 400,
                            Message = _config[
                                "ResponseMessages:DeliveryRequestMsg:DeliveryRequestThatCanBeReportedNotFoundMsg"
                            ]
                        };

                    if (deliveryRequest.Status != DeliveryRequestStatus.FINISHED)
                        return new CommonResponse
                        {
                            Status = 400,
                            Message = _config[
                                "ResponseMessages:DeliveryRequestMsg:DeliveryRequestTypeBranchToCharityUnitMustBeFinishedToBeReportedByCharityUnitMsg"
                            ]
                        };

                    report.Type = ReportType.MISSING_ITEMS_TO_CHARITY;
                }

                using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
                {
                    if (await _reportRepository.CreateReportAsync(report) != 1)
                        return new CommonResponse
                        {
                            Status = 500,
                            Message = _config["ResponseMessages:CommonMsg:InternalServerErrorMsg"]
                        };

                    deliveryRequest.ScheduledRouteDeliveryRequests[0].ReportId = report.Id;

                    if (
                        await _scheduledRouteDeliveryRequestRepository.UpdateScheduledRouteDeliveryRequestAsync(
                            deliveryRequest.ScheduledRouteDeliveryRequests[0]
                        ) != 1
                    )
                        return new CommonResponse
                        {
                            Status = 500,
                            Message = _config["ResponseMessages:CommonMsg:InternalServerErrorMsg"]
                        };

                    Notification notificationForAdmin = new Notification
                    {
                        Name = "Chi nhánh có yêu cầu vận chuyển bị báo cáo.",
                        CreatedDate = SettedUpDateTime.GetCurrentVietNamTime(),
                        Image = _config["Notification:Image"],
                        Content =
                            userRoleName == RoleEnum.CONTRIBUTOR.ToString()
                                ? "Chi nhánh có yêu cầu vận chuyển bị báo cáo bởi nhà hảo tâm."
                                : "Chi nhánh có yêu cầu vận chuyển bị báo cáo bởi tổ chức từ thiện.",
                        ReceiverId = deliveryRequest.Branch.BranchAdminId.ToString(),
                        Status = NotificationStatus.NEW,
                        Type = NotificationType.NOTIFYING,
                        DataType = DataNotificationType.DELIVERY_REQUEST,
                        DataId = deliveryRequest.Id
                    };
                    await _notificationRepository.CreateNotificationAsync(notificationForAdmin);
                    await _hubContext.Clients.All.SendAsync(
                        deliveryRequest.Branch.BranchAdminId.ToString(),
                        notificationForAdmin
                    );

                    scope.Complete();
                    return new CommonResponse
                    {
                        Status = 200,
                        Message = _config["ResponseMessages:ReportMsg:SendReportSuccessMsg"]
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    $"An exception occurred in service {nameof(DeliveryRequestService)}, method {nameof(SendReportByUserOrCharityUnitAsync)}."
                );
                return new CommonResponse
                {
                    Status = 500,
                    Message = _config["ResponseMessages:CommonMsg:InternalServerErrorMsg"]
                };
            }
        }

        public async Task<CommonResponse> SendReportByContributorAsync(
            Guid userId,
            Guid deliveryRequestId,
            ReportForContributorRequest reportForCollaboratorRequest
        )
        {
            try
            {
                DeliveryRequest? deliveryRequest =
                    await _deliveryRequestRepository.FindDeliveryRequestByIdAsync(
                        deliveryRequestId
                    );

                if (
                    deliveryRequest == null
                    || deliveryRequest.ScheduledRouteDeliveryRequests.Count == 0
                    || deliveryRequest.ScheduledRouteDeliveryRequests[0].ScheduledRoute.UserId
                        != userId
                )
                    return new CommonResponse
                    {
                        Status = 400,
                        Message = _config[
                            "ResponseMessages:DeliveryRequestMsg:DeliveryRequestThatCanBeReportedNotFoundMsg"
                        ]
                    };

                StockUpdatedHistoryType stockUpdatedHistoryType =
                    GetStockUpdatedHistoryTypeOfDeliveryRequest(deliveryRequest);

                if (stockUpdatedHistoryType != StockUpdatedHistoryType.IMPORT)
                    return new CommonResponse
                    {
                        Status = 400,
                        Message = _config[
                            "ResponseMessages:DeliveryRequestMsg:DeliveryRequestMustBeImportTypeToReportByCollaboratorMsg"
                        ]
                    };

                if (
                    !(
                        deliveryRequest.Status == DeliveryRequestStatus.SHIPPING
                        || deliveryRequest.Status == DeliveryRequestStatus.ARRIVED_PICKUP
                    )
                )
                    return new CommonResponse
                    {
                        Status = 400,
                        Message = _config[
                            "ResponseMessages:DeliveryRequestMsg:DeliveryRequestTypeItemToBranchMustBeShippingOrArrivedPickupToBeReportedByCollaboratorMsg"
                        ]
                    };

                if (deliveryRequest.ScheduledRouteDeliveryRequests[0].ReportId != null)
                    return new CommonResponse
                    {
                        Status = 400,
                        Message = _config[
                            "ResponseMessages:DeliveryRequestMsg:DeliveryRequestHasBeenAlreadyReportedMsg"
                        ]
                    };

                Report report = new Report
                {
                    Title = reportForCollaboratorRequest.Title,
                    Content = reportForCollaboratorRequest.Content,
                    UserId = userId,
                    CreatedDate = SettedUpDateTime.GetCurrentVietNamTime(),
                    Type = reportForCollaboratorRequest.Type
                };

                using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
                {
                    if (await _reportRepository.CreateReportAsync(report) != 1)
                        return new CommonResponse
                        {
                            Status = 500,
                            Message = _config["ResponseMessages:CommonMsg:InternalServerErrorMsg"]
                        };

                    deliveryRequest.ScheduledRouteDeliveryRequests[0].ReportId = report.Id;

                    if (
                        await _scheduledRouteDeliveryRequestRepository.UpdateScheduledRouteDeliveryRequestAsync(
                            deliveryRequest.ScheduledRouteDeliveryRequests[0]
                        ) != 1
                    )
                        return new CommonResponse
                        {
                            Status = 500,
                            Message = _config["ResponseMessages:CommonMsg:InternalServerErrorMsg"]
                        };

                    deliveryRequest.Status = DeliveryRequestStatus.REPORTED;

                    if (
                        await _deliveryRequestRepository.UpdateDeliveryRequestAsync(deliveryRequest)
                        != 1
                    )
                        return new CommonResponse
                        {
                            Status = 500,
                            Message = _config["ResponseMessages:CommonMsg:InternalServerErrorMsg"]
                        };

                    Notification notificationForAdmin = new Notification
                    {
                        Name = "Chi nhánh có yêu cầu vận chuyển bị báo cáo.",
                        CreatedDate = SettedUpDateTime.GetCurrentVietNamTime(),
                        Image = _config["Notification:Image"],
                        Content =
                            "Chi nhánh có yêu cầu vận chuyển bị báo cáo bởi tình nguyện viên.",
                        ReceiverId = deliveryRequest.Branch.BranchAdminId.ToString(),
                        Status = NotificationStatus.NEW,
                        Type = NotificationType.NOTIFYING,
                        DataType = DataNotificationType.DELIVERY_REQUEST,
                        DataId = deliveryRequest.Id
                    };
                    await _notificationRepository.CreateNotificationAsync(notificationForAdmin);
                    await _hubContext.Clients.All.SendAsync(
                        deliveryRequest.Branch.BranchAdminId.ToString(),
                        notificationForAdmin
                    );

                    scope.Complete();
                    return new CommonResponse
                    {
                        Status = 200,
                        Message = _config["ResponseMessages:ReportMsg:SendReportSuccessMsg"]
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    $"An exception occurred in service {nameof(DeliveryRequestService)}, method {nameof(SendReportByContributorAsync)}."
                );
                return new CommonResponse
                {
                    Status = 500,
                    Message = _config["ResponseMessages:CommonMsg:InternalServerErrorMsg"]
                };
            }
        }

        public async Task<CommonResponse> CountDeliveryRequestByAllStatus(
            DateTime? startDate,
            DateTime? endDate,
            Guid? branchId,
            string? roleEnum,
            Guid? branchAdminId
        )
        {
            if (roleEnum == RoleEnum.BRANCH_ADMIN.ToString() && branchAdminId != null)
            {
                Branch? branch = await _branchRepository.FindBranchByBranchAdminIdAsync(
                    branchAdminId.Value
                );
                if (branch != null)
                {
                    branchId = branch.Id;
                }
            }
            CommonResponse commonResponse = new CommonResponse();
            try
            {
                int total = await _deliveryRequestRepository.CountDeliveryRequest(
                    null,
                    startDate,
                    endDate,
                    branchId
                );
                int numberOfPending = await _deliveryRequestRepository.CountDeliveryRequest(
                    DeliveryRequestStatus.PENDING,
                    startDate,
                    endDate,
                    branchId
                );
                int numberOfReported = await _deliveryRequestRepository.CountDeliveryRequest(
                    DeliveryRequestStatus.REPORTED,
                    startDate,
                    endDate,
                    branchId
                );
                int numberOfAccepted = await _deliveryRequestRepository.CountDeliveryRequest(
                    DeliveryRequestStatus.ACCEPTED,
                    startDate,
                    endDate,
                    branchId
                );
                int numberOfArrivedDelivery = await _deliveryRequestRepository.CountDeliveryRequest(
                    DeliveryRequestStatus.ARRIVED_DELIVERY,
                    startDate,
                    endDate,
                    branchId
                );
                int numberOfArrivedPickUp = await _deliveryRequestRepository.CountDeliveryRequest(
                    DeliveryRequestStatus.ARRIVED_PICKUP,
                    startDate,
                    endDate,
                    branchId
                );
                int numberOfDelivered = await _deliveryRequestRepository.CountDeliveryRequest(
                    DeliveryRequestStatus.DELIVERED,
                    startDate,
                    endDate,
                    branchId
                );
                int numberOfCollected = await _deliveryRequestRepository.CountDeliveryRequest(
                    DeliveryRequestStatus.COLLECTED,
                    startDate,
                    endDate,
                    branchId
                );
                int numberOfExpried = await _deliveryRequestRepository.CountDeliveryRequest(
                    DeliveryRequestStatus.EXPIRED,
                    startDate,
                    endDate,
                    branchId
                );

                int numberOfFinished = await _deliveryRequestRepository.CountDeliveryRequest(
                    DeliveryRequestStatus.FINISHED,
                    startDate,
                    endDate,
                    branchId
                );
                var rs = new
                {
                    NumberOfPending = numberOfPending,
                    NumberOfReported = numberOfReported,
                    NumberOfAccepted = numberOfAccepted,
                    NumberOfArrivedDelivery = numberOfArrivedDelivery,
                    NumberOfArrivedPickUp = numberOfArrivedPickUp,
                    NumberOfDelivered = numberOfDelivered,
                    NumberOfCollected = numberOfCollected,
                    NumberOfExpried = numberOfExpried,
                    NumberOfFinished = numberOfFinished,
                    Total = total
                };
                commonResponse.Data = rs;
                commonResponse.Status = 200;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    $"An exception occurred in service {nameof(ScheduledRouteService)}, method {nameof(CountDeliveryRequestByStatus)}."
                );
                return new CommonResponse
                {
                    Status = 500,
                    Message = _config["ResponseMessages:CommonMsg:InternalServerErrorMsg"]
                };
            }
            return commonResponse;
        }

        public async Task<CommonResponse> CountDeliveryRequestByStatus(
            DateTime startDate,
            DateTime endDate,
            DeliveryRequestStatus? status,
            TimeFrame timeFrame,
            Guid? branchId,
            string? roleEnum,
            Guid? branchAdminId
        )
        {
            if (roleEnum == RoleEnum.BRANCH_ADMIN.ToString() && branchAdminId != null)
            {
                Branch? branch = await _branchRepository.FindBranchByBranchAdminIdAsync(
                    branchAdminId.Value
                );
                if (branch != null)
                {
                    branchId = branch.Id;
                }
            }
            CommonResponse commonResponse = new CommonResponse();
            try
            {
                int total = await _deliveryRequestRepository.CountDeliveryRequest(
                    status,
                    startDate,
                    endDate,
                    branchId
                );
                List<StatisticObjectByTimeRangeResponse> responses =
                    new List<StatisticObjectByTimeRangeResponse>();
                switch (timeFrame)
                {
                    case TimeFrame.DAY:
                        for (
                            DateTime currentDate = startDate;
                            currentDate <= endDate;
                            currentDate = currentDate.AddDays(1)
                        )
                        {
                            StatisticObjectByTimeRangeResponse tmp =
                                new StatisticObjectByTimeRangeResponse();
                            tmp.From = currentDate;
                            tmp.To = currentDate.AddDays(1);
                            tmp.Quantity = await _deliveryRequestRepository.CountDeliveryRequest(
                                status,
                                tmp.From,
                                tmp.To,
                                branchId
                            );
                            responses.Add(tmp);
                        }

                        break;
                    case TimeFrame.MONTH:
                        for (
                            DateTime currentDate = startDate;
                            currentDate <= endDate;
                            currentDate = currentDate.AddMonths(1)
                        )
                        {
                            StatisticObjectByTimeRangeResponse tmp =
                                new StatisticObjectByTimeRangeResponse();
                            tmp.From = currentDate;
                            tmp.To = currentDate.AddMonths(1);
                            tmp.Quantity = await _deliveryRequestRepository.CountDeliveryRequest(
                                status,
                                tmp.From,
                                tmp.To,
                                branchId
                            );
                            responses.Add(tmp);
                        }

                        break;
                    case TimeFrame.YEAR:
                        for (
                            DateTime currentDate = startDate;
                            currentDate <= endDate;
                            currentDate = currentDate.AddYears(1)
                        )
                        {
                            StatisticObjectByTimeRangeResponse tmp =
                                new StatisticObjectByTimeRangeResponse();
                            tmp.From = currentDate;
                            tmp.To = currentDate.AddYears(1);
                            tmp.Quantity = await _deliveryRequestRepository.CountDeliveryRequest(
                                status,
                                tmp.From,
                                tmp.To,
                                branchId
                            );
                            responses.Add(tmp);
                        }
                        break;
                    case TimeFrame.WEEK:
                        for (
                            DateTime currentDate = startDate;
                            currentDate <= endDate;
                            currentDate = currentDate.AddDays(7)
                        )
                        {
                            StatisticObjectByTimeRangeResponse tmp =
                                new StatisticObjectByTimeRangeResponse();
                            tmp.From = currentDate;
                            tmp.To = currentDate.AddDays(7);
                            tmp.Quantity = await _deliveryRequestRepository.CountDeliveryRequest(
                                status,
                                tmp.From,
                                tmp.To,
                                branchId
                            );
                            responses.Add(tmp);
                        }
                        break;
                    default:
                        break;
                }
                var rs = new { Total = total, DeliveryRequestByTimeRangeResponse = responses };
                commonResponse.Data = rs;
                commonResponse.Status = 200;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    $"An exception occurred in service {nameof(ScheduledRouteService)}, method {nameof(CountDeliveryRequestByStatus)}."
                );
                return new CommonResponse
                {
                    Status = 500,
                    Message = _config["ResponseMessages:CommonMsg:InternalServerErrorMsg"]
                };
            }
            return commonResponse;
        }

        public async Task<CommonResponse> HandleReportedDeliveryRequestAsync(
            Guid userId,
            Guid deliveryRequestId,
            DeliveryRequestStatus deliveryRequestStatus
        )
        {
            try
            {
                DeliveryRequest? deliveryRequest =
                    await _deliveryRequestRepository.FindDeliveryRequestByIdAsync(
                        deliveryRequestId
                    );

                if (
                    deliveryRequest == null
                    || deliveryRequest.ScheduledRouteDeliveryRequests.Count == 0
                    || GetMainBranch(deliveryRequest).BranchAdminId != userId
                )
                    return new CommonResponse
                    {
                        Status = 400,
                        Message = _config[
                            "ResponseMessages:DeliveryRequestMsg:DeliveryRequestThatCanBeReportedNotFoundMsg"
                        ]
                    };

                StockUpdatedHistoryType stockUpdatedHistoryType =
                    GetStockUpdatedHistoryTypeOfDeliveryRequest(deliveryRequest);

                if (stockUpdatedHistoryType != StockUpdatedHistoryType.IMPORT)
                    return new CommonResponse
                    {
                        Status = 400,
                        Message = _config[
                            "ResponseMessages:DeliveryRequestMsg:DeliveryRequestMustBeImportTypeToBeProcessedByBranchAdminMsg"
                        ]
                    };

                if (
                    !(
                        deliveryRequestStatus == DeliveryRequestStatus.PENDING
                        || deliveryRequestStatus != DeliveryRequestStatus.EXPIRED
                    )
                    || deliveryRequest.Status != DeliveryRequestStatus.REPORTED
                )
                    return new CommonResponse
                    {
                        Status = 400,
                        Message = _config[
                            "ResponseMessages:DeliveryRequestMsg:ProcessedReportedDeliveryRequestMustBePendingOrExpiredMsg"
                        ]
                    };

                using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
                {
                    //if (deliveryRequestStatus == DeliveryRequestStatus.PENDING)
                    //{
                    //    deliveryRequest.ScheduledRouteDeliveryRequests[0].Status =
                    //        ScheduledRouteDeliveryRequestStatus.CANCELED;

                    //    if (
                    //        await _scheduledRouteDeliveryRequestRepository.UpdateScheduledRouteDeliveryRequestAsync(
                    //            deliveryRequest.ScheduledRouteDeliveryRequests[0]
                    //        ) != 1
                    //    )
                    //        return new CommonResponse
                    //        {
                    //            Status = 500,
                    //            Message = _config[
                    //                "ResponseMessages:CommonMsg:InternalServerErrorMsg"
                    //            ]
                    //        };
                    //}

                    deliveryRequest.ScheduledRouteDeliveryRequests[0].Status =
                        ScheduledRouteDeliveryRequestStatus.CANCELED;

                    if (
                        await _scheduledRouteDeliveryRequestRepository.UpdateScheduledRouteDeliveryRequestAsync(
                            deliveryRequest.ScheduledRouteDeliveryRequests[0]
                        ) != 1
                    )
                        return new CommonResponse
                        {
                            Status = 500,
                            Message = _config["ResponseMessages:CommonMsg:InternalServerErrorMsg"]
                        };

                    deliveryRequest.Status = deliveryRequestStatus;

                    if (
                        await _deliveryRequestRepository.UpdateDeliveryRequestAsync(deliveryRequest)
                        != 1
                    )
                        return new CommonResponse
                        {
                            Status = 500,
                            Message = _config["ResponseMessages:CommonMsg:InternalServerErrorMsg"]
                        };

                    if (deliveryRequest.DonatedRequestId != null)
                    {
                        List<DeliveryRequest> deliveryRequests =
                            await _deliveryRequestRepository.FindDeliveryRequestsByDonatedRequestIdAsync(
                                (Guid)deliveryRequest.DonatedRequestId
                            );

                        if (
                            deliveryRequests.Any(dr => dr.Status == DeliveryRequestStatus.FINISHED)
                            && deliveryRequests
                                .Where(
                                    dr =>
                                        dr.Status == DeliveryRequestStatus.FINISHED
                                        || dr.Status == DeliveryRequestStatus.EXPIRED
                                        || dr.Status == DeliveryRequestStatus.CANCELED
                                )
                                .Count() == deliveryRequests.Count
                        )
                        {
                            deliveryRequest.DonatedRequest!.Status = DonatedRequestStatus.FINISHED;
                            if (
                                await _donatedRequestRepository.UpdateDonatedRequestAsync(
                                    deliveryRequest.DonatedRequest
                                ) != 1
                            )
                                return new CommonResponse
                                {
                                    Status = 500,
                                    Message = _config[
                                        "ResponseMessages:CommonMsg:InternalServerErrorMsg"
                                    ]
                                };
                        }
                        else if (
                            !deliveryRequests.Any(dr => dr.Status == DeliveryRequestStatus.FINISHED)
                            && deliveryRequests
                                .Where(
                                    dr =>
                                        dr.Status == DeliveryRequestStatus.EXPIRED
                                        || dr.Status == DeliveryRequestStatus.CANCELED
                                )
                                .Count() == deliveryRequests.Count
                        )
                        {
                            deliveryRequest.DonatedRequest!.Status = DonatedRequestStatus.CANCELED;
                            if (
                                await _donatedRequestRepository.UpdateDonatedRequestAsync(
                                    deliveryRequest.DonatedRequest
                                ) != 1
                            )
                                return new CommonResponse
                                {
                                    Status = 500,
                                    Message = _config[
                                        "ResponseMessages:CommonMsg:InternalServerErrorMsg"
                                    ]
                                };
                        }
                    }

                    scope.Complete();
                    return new CommonResponse
                    {
                        Status = 200,
                        Message = _config["ResponseMessages:ReportMsg:CreateReportSuccessMsg"]
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    $"An exception occurred in service {nameof(DeliveryRequestService)}, method {nameof(HandleReportedDeliveryRequestAsync)}."
                );
                return new CommonResponse
                {
                    Status = 500,
                    Message = _config["ResponseMessages:CommonMsg:InternalServerErrorMsg"]
                };
            }
        }

        private Branch GetMainBranch(DeliveryRequest deliveryRequest)
        {
            if (deliveryRequest.DonatedRequestId != null)
            {
                return deliveryRequest.Branch;
            }
            else
            {
                if (deliveryRequest.AidRequest!.CharityUnitId != null)
                {
                    return deliveryRequest.Branch;
                }
                else
                {
                    return deliveryRequest.AidRequest.Branch!;
                }
            }
        }

        public async Task<CommonResponse> CancelDeliveryRequestAsync(
            Guid deliveryRequestId,
            string canceledReason,
            Guid userId
        )
        {
            try
            {
                DeliveryRequest? deliveryRequest =
                    await _deliveryRequestRepository.FindDeliveryRequestByIdAsync(
                        deliveryRequestId
                    );

                if (deliveryRequest == null || deliveryRequest.Branch!.BranchAdminId != userId)
                    return new CommonResponse
                    {
                        Status = 400,
                        Message = _config[
                            "ResponseMessages:DeliveryRequestMsg:DeliveryRequestOfBranchNotFoundMsg"
                        ]
                    };

                ScheduledRouteDeliveryRequest? scheduledRouteDeliveryRequest =
                    deliveryRequest.ScheduledRouteDeliveryRequests.Count > 0
                        ? deliveryRequest.ScheduledRouteDeliveryRequests[0]
                        : null;

                DeliveryType deliveryType = GetDeliveryTypeOfDeliveryRequest(deliveryRequest);

                CommonResponse commonResponse;

                if (deliveryType == DeliveryType.DONATED_REQUEST_TO_BRANCH)
                    commonResponse = await CancelDeliveryRequestTypeImportAsync(
                        deliveryRequest,
                        canceledReason,
                        userId
                    );
                else
                    commonResponse = await CancelDeliveryRequestTypeExportAsync(
                        deliveryRequest,
                        canceledReason,
                        userId
                    );

                if (
                    commonResponse.Status == 200
                    && scheduledRouteDeliveryRequest != null
                    && scheduledRouteDeliveryRequest.ScheduledRoute.UserId != null
                )
                {
                    Notification notificationForUser = new Notification
                    {
                        Name = "Bạn có yêu cầu vận chuyển bị hủy.",
                        CreatedDate = SettedUpDateTime.GetCurrentVietNamTime(),
                        Image = _config["Notification:Image"],
                        Content = "Bạn có yêu cầu vận chuyển bị hủy bới quản trị viên chi nhánh.",
                        ReceiverId =
                            scheduledRouteDeliveryRequest.ScheduledRoute.UserId.ToString()!,
                        Status = NotificationStatus.NEW,
                        Type = NotificationType.NOTIFYING,
                        DataType = DataNotificationType.DELIVERY_REQUEST,
                        DataId = deliveryRequest.Id
                    };
                    await _notificationRepository.CreateNotificationAsync(notificationForUser);
                    await _hubContext.Clients.All.SendAsync(
                        scheduledRouteDeliveryRequest.ScheduledRoute.UserId.ToString()!,
                        notificationForUser
                    );
                    if (scheduledRouteDeliveryRequest.ScheduledRoute.User!.DeviceToken != null)
                    {
                        PushNotificationRequest pushNotificationRequest =
                            new PushNotificationRequest
                            {
                                DeviceToken = scheduledRouteDeliveryRequest
                                    .ScheduledRoute
                                    .User!
                                    .DeviceToken,
                                Message =
                                    "Bạn có yêu cầu vận chuyển bị hủy bới quản trị viên chi nhánh.",
                                Title = "Bạn có yêu cầu vận chuyển bị hủy."
                            };
                        await _firebaseNotificationService.PushNotification(
                            pushNotificationRequest
                        );
                    }
                }

                return commonResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    $"An exception occurred in service {nameof(DeliveryRequestService)}, method {nameof(CancelDeliveryRequestAsync)}."
                );
                return new CommonResponse
                {
                    Status = 500,
                    Message = _config["ResponseMessages:CommonMsg:InternalServerErrorMsg"]
                };
            }
        }

        public async Task<CommonResponse> CancelDeliveryRequestTypeImportAsync(
            DeliveryRequest deliveryRequest,
            string canceledReason,
            Guid userId
        )
        {
            try
            {
                if (
                    deliveryRequest.Status != DeliveryRequestStatus.PENDING
                    && deliveryRequest.Status != DeliveryRequestStatus.ACCEPTED
                )
                    return new CommonResponse
                    {
                        Status = 400,
                        Message = _config[
                            "ResponseMessages:DeliveryRequestMsg:DeliveryRequestTypeImportMustBePendingOrAcceptedToBeCanceledMsg"
                        ]
                    };

                deliveryRequest.Status = DeliveryRequestStatus.CANCELED;
                deliveryRequest.CanceledReason = canceledReason;

                ScheduledRouteDeliveryRequest? scheduledRouteDeliveryRequest =
                    deliveryRequest.ScheduledRouteDeliveryRequests[0];

                if (scheduledRouteDeliveryRequest != null)
                {
                    scheduledRouteDeliveryRequest.Status =
                        ScheduledRouteDeliveryRequestStatus.CANCELED;
                }

                using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
                {
                    if (
                        await _deliveryRequestRepository.UpdateDeliveryRequestAsync(deliveryRequest)
                        != 1
                    )
                        return new CommonResponse
                        {
                            Status = 500,
                            Message = _config["ResponseMessages:CommonMsg:InternalServerErrorMsg"]
                        };

                    if (scheduledRouteDeliveryRequest != null)
                    {
                        if (
                            await _scheduledRouteDeliveryRequestRepository.UpdateScheduledRouteDeliveryRequestAsync(
                                scheduledRouteDeliveryRequest
                            ) != 1
                        )
                            return new CommonResponse
                            {
                                Status = 500,
                                Message = _config[
                                    "ResponseMessages:CommonMsg:InternalServerErrorMsg"
                                ]
                            };
                    }

                    if (deliveryRequest.DonatedRequestId != null)
                    {
                        List<DeliveryRequest> deliveryRequests =
                            await _deliveryRequestRepository.FindDeliveryRequestsByDonatedRequestIdAsync(
                                (Guid)deliveryRequest.DonatedRequestId
                            );

                        if (
                            deliveryRequests.Any(dr => dr.Status == DeliveryRequestStatus.FINISHED)
                            && deliveryRequests
                                .Where(
                                    dr =>
                                        dr.Status == DeliveryRequestStatus.FINISHED
                                        || dr.Status == DeliveryRequestStatus.EXPIRED
                                        || dr.Status == DeliveryRequestStatus.CANCELED
                                )
                                .Count() == deliveryRequests.Count
                        )
                        {
                            deliveryRequest.DonatedRequest!.Status = DonatedRequestStatus.FINISHED;
                            if (
                                await _donatedRequestRepository.UpdateDonatedRequestAsync(
                                    deliveryRequest.DonatedRequest
                                ) != 1
                            )
                                return new CommonResponse
                                {
                                    Status = 500,
                                    Message = _config[
                                        "ResponseMessages:CommonMsg:InternalServerErrorMsg"
                                    ]
                                };
                        }
                        else if (
                            !deliveryRequests.Any(dr => dr.Status == DeliveryRequestStatus.FINISHED)
                            && deliveryRequests
                                .Where(
                                    dr =>
                                        dr.Status == DeliveryRequestStatus.EXPIRED
                                        || dr.Status == DeliveryRequestStatus.CANCELED
                                )
                                .Count() == deliveryRequests.Count
                        )
                        {
                            deliveryRequest.DonatedRequest!.Status = DonatedRequestStatus.CANCELED;
                            if (
                                await _donatedRequestRepository.UpdateDonatedRequestAsync(
                                    deliveryRequest.DonatedRequest
                                ) != 1
                            )
                                return new CommonResponse
                                {
                                    Status = 500,
                                    Message = _config[
                                        "ResponseMessages:CommonMsg:InternalServerErrorMsg"
                                    ]
                                };
                        }
                    }

                    scope.Complete();
                    return new CommonResponse
                    {
                        Status = 200,
                        Message = _config[
                            "ResponseMessages:DeliveryRequestMsg:CancelDeliveryRequestSuccessMsg"
                        ]
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    $"An exception occurred in service {nameof(DeliveryRequestService)}, method {nameof(CancelDeliveryRequestTypeImportAsync)}."
                );
                return new CommonResponse
                {
                    Status = 500,
                    Message = _config["ResponseMessages:CommonMsg:InternalServerErrorMsg"]
                };
            }
        }

        public async Task<CommonResponse> CancelDeliveryRequestTypeExportAsync(
            DeliveryRequest deliveryRequest,
            string canceledReason,
            Guid userId
        )
        {
            try
            {
                if (
                    deliveryRequest.Status != DeliveryRequestStatus.PENDING
                    && deliveryRequest.Status != DeliveryRequestStatus.ACCEPTED
                    && deliveryRequest.Status != DeliveryRequestStatus.SHIPPING
                    && deliveryRequest.Status != DeliveryRequestStatus.ARRIVED_PICKUP
                    && deliveryRequest.Status != DeliveryRequestStatus.COLLECTED
                    && deliveryRequest.Status != DeliveryRequestStatus.ARRIVED_DELIVERY
                )
                    return new CommonResponse
                    {
                        Status = 400,
                        Message = _config[
                            "ResponseMessages:DeliveryRequestMsg:DeliveryRequestTypeExportMustBePendingOrAcceptedOrDelveringToBeCanceledMsg"
                        ]
                    };

                deliveryRequest.Status = DeliveryRequestStatus.CANCELED;
                deliveryRequest.CanceledReason = canceledReason;

                ScheduledRouteDeliveryRequest? scheduledRouteDeliveryRequest = null;
                if (deliveryRequest.ScheduledRouteDeliveryRequests.Count > 0)
                    scheduledRouteDeliveryRequest = deliveryRequest.ScheduledRouteDeliveryRequests[
                        0
                    ];

                if (scheduledRouteDeliveryRequest != null)
                {
                    scheduledRouteDeliveryRequest.Status =
                        ScheduledRouteDeliveryRequestStatus.CANCELED;

                    scheduledRouteDeliveryRequest.ScheduledRoute.Status =
                        ScheduledRouteStatus.PENDING;

                    scheduledRouteDeliveryRequest.ScheduledRoute.UserId = null;
                }

                List<StockUpdatedHistoryDetail> stockUpdatedHistoryDetails =
                    new List<StockUpdatedHistoryDetail>();

                foreach (DeliveryItem deliveryItem in deliveryRequest.DeliveryItems)
                {
                    stockUpdatedHistoryDetails.AddRange(
                        deliveryItem.StockUpdatedHistoryDetails.Where(suhd => suhd.StockId != null)
                    );
                }

                using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
                {
                    if (stockUpdatedHistoryDetails.Count > 0)
                    {
                        StockUpdatedHistory stockUpdatedHistory = stockUpdatedHistoryDetails[
                            0
                        ].StockUpdatedHistory;

                        stockUpdatedHistory.IsPrivate = true;

                        StockUpdatedHistory newStockUpdatedHistory = new StockUpdatedHistory
                        {
                            CreatedDate = SettedUpDateTime.GetCurrentVietNamTime(),
                            Type = StockUpdatedHistoryType.IMPORT,
                            BranchId = stockUpdatedHistory.BranchId,
                            CreatedBy = userId,
                            Note = canceledReason,
                            IsPrivate = true
                        };

                        if (
                            await _stockUpdatedHistoryRepository.UpdateStockUpdatedHistoryAsync(
                                stockUpdatedHistory
                            ) != 1
                        )
                            return new CommonResponse
                            {
                                Status = 500,
                                Message = _config[
                                    "ResponseMessages:CommonMsg:InternalServerErrorMsg"
                                ]
                            };

                        if (
                            await _stockUpdatedHistoryRepository.AddStockUpdatedHistoryAsync(
                                newStockUpdatedHistory
                            ) != 1
                        )
                            return new CommonResponse
                            {
                                Status = 500,
                                Message = _config[
                                    "ResponseMessages:CommonMsg:InternalServerErrorMsg"
                                ]
                            };

                        List<StockUpdatedHistoryDetail> newStockUpdatedHistoryDetails =
                            new List<StockUpdatedHistoryDetail>();

                        List<Stock> returnedStocks = new List<Stock>();

                        foreach (StockUpdatedHistoryDetail suhd in stockUpdatedHistoryDetails)
                        {
                            newStockUpdatedHistoryDetails.Add(
                                new StockUpdatedHistoryDetail
                                {
                                    Quantity = suhd.Quantity,
                                    Note = canceledReason,
                                    StockUpdatedHistoryId = newStockUpdatedHistory.Id,
                                    DeliveryItemId = suhd.DeliveryItemId,
                                    StockId = suhd.StockId,
                                    //AidRequestId = suhd.AidRequestId
                                }
                            );

                            suhd.Stock!.Quantity += suhd.Quantity;
                            returnedStocks.Add(suhd.Stock);
                        }

                        if (
                            await _stockUpdatedHistoryDetailRepository.AddStockUpdatedHistoryDetailsAsync(
                                newStockUpdatedHistoryDetails
                            ) != newStockUpdatedHistoryDetails.Count
                        )
                            return new CommonResponse
                            {
                                Status = 500,
                                Message = _config[
                                    "ResponseMessages:CommonMsg:InternalServerErrorMsg"
                                ]
                            };

                        if (
                            await _stockRepository.UpdateStocksAsync(returnedStocks)
                            != returnedStocks.Count
                        )
                            return new CommonResponse
                            {
                                Status = 500,
                                Message = _config[
                                    "ResponseMessages:CommonMsg:InternalServerErrorMsg"
                                ]
                            };
                    }

                    if (
                        await _deliveryRequestRepository.UpdateDeliveryRequestAsync(deliveryRequest)
                        != 1
                    )
                        return new CommonResponse
                        {
                            Status = 500,
                            Message = _config["ResponseMessages:CommonMsg:InternalServerErrorMsg"]
                        };

                    if (scheduledRouteDeliveryRequest != null)
                    {
                        if (
                            await _scheduledRouteDeliveryRequestRepository.UpdateScheduledRouteDeliveryRequestAsync(
                                scheduledRouteDeliveryRequest
                            ) != 1
                        )
                            return new CommonResponse
                            {
                                Status = 500,
                                Message = _config[
                                    "ResponseMessages:CommonMsg:InternalServerErrorMsg"
                                ]
                            };

                        if (
                            await _scheduledRouteRepository.UpdateScheduledRouteAsync(
                                scheduledRouteDeliveryRequest.ScheduledRoute
                            ) != 1
                        )
                            return new CommonResponse
                            {
                                Status = 500,
                                Message = _config[
                                    "ResponseMessages:CommonMsg:InternalServerErrorMsg"
                                ]
                            };
                    }

                    scope.Complete();
                    return new CommonResponse
                    {
                        Status = 200,
                        Message = _config[
                            "ResponseMessages:DeliveryRequestMsg:CancelDeliveryRequestSuccessMsg"
                        ]
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    $"An exception occurred in service {nameof(DeliveryRequestService)}, method {nameof(CancelDeliveryRequestTypeImportAsync)}."
                );
                return new CommonResponse
                {
                    Status = 500,
                    Message = _config["ResponseMessages:CommonMsg:InternalServerErrorMsg"]
                };
            }
        }

        private DeliveryType GetDeliveryTypeOfDeliveryRequest(DeliveryRequest deliveryRequest)
        {
            if (deliveryRequest.DonatedRequestId != null)
                return DeliveryType.DONATED_REQUEST_TO_BRANCH;
            else if (deliveryRequest.AidRequest!.CharityUnitId != null)
                return DeliveryType.BRANCH_TO_AID_REQUEST;
            else
                return DeliveryType.BRANCH_TO_BRANCH;
        }

        private StockUpdatedHistoryType GetStockUpdatedHistoryTypeOfDeliveryRequest(
            DeliveryRequest deliveryRequest
        )
        {
            if (deliveryRequest.DonatedRequestId != null)
                return StockUpdatedHistoryType.IMPORT;
            else if (deliveryRequest.AidRequest!.CharityUnitId != null)
                return StockUpdatedHistoryType.EXPORT;
            else
                return StockUpdatedHistoryType.IMPORT;
        }

        public async Task<CommonResponse> GetFinishedDeliveryRequestsByDonatedRequestIdForUserAsync(
            Guid donatedRequestId,
            Guid userId,
            int? pageSize,
            int? page
        )
        {
            try
            {
                DonatedRequest? donatedRequest =
                    await _donatedRequestRepository.FindDonatedRequestByIdAsync(donatedRequestId);

                if (donatedRequest == null || donatedRequest.UserId != userId)
                    return new CommonResponse
                    {
                        Status = 400,
                        Message = _config[
                            "ResponseMessages:DonatedRequestMsg:DonatedRequestNotFound"
                        ]
                    };

                List<StockUpdatedHistory> stockUpdatedHistories =
                    await _stockUpdatedHistoryRepository.FindStockUpdatedHistoriesByDonatedRequestIdAsync(
                        donatedRequestId
                    );

                stockUpdatedHistories = stockUpdatedHistories
                    .OrderByDescending(suh => suh.CreatedDate)
                    .ToList();

                Pagination pagination = new Pagination();
                pagination.PageSize = pageSize == null ? 10 : pageSize.Value;
                pagination.CurrentPage = page == null ? 1 : page.Value;
                pagination.Total = stockUpdatedHistories.Count;
                stockUpdatedHistories = stockUpdatedHistories
                    .Skip((pagination.CurrentPage - 1) * pagination.PageSize)
                    .Take(pagination.PageSize)
                    .ToList();

                return new CommonResponse
                {
                    Status = 200,
                    Data = stockUpdatedHistories.Select(
                        suh =>
                            new SimpleDeliveryRequestForUserResponse
                            {
                                Id = suh.StockUpdatedHistoryDetails[0]
                                    .DeliveryItem!
                                    .DeliveryRequest
                                    .Id,
                                CurrentScheduledTime = JsonConvert.DeserializeObject<ScheduledTime>(
                                    suh.StockUpdatedHistoryDetails[0]
                                        .DeliveryItem!
                                        .DeliveryRequest
                                        .CurrentScheduledTime!
                                ),
                                ImportedDate = suh.CreatedDate,
                                ImportNote = suh.Note,
                                ProofImage = suh.StockUpdatedHistoryDetails[0]
                                    .DeliveryItem!
                                    .DeliveryRequest
                                    .ProofImage,
                                Avatar = suh.StockUpdatedHistoryDetails[0]
                                    .DeliveryItem!
                                    .DeliveryRequest
                                    .ScheduledRouteDeliveryRequests[0]
                                    .ScheduledRoute
                                    .User!
                                    .Avatar!,
                                Name = suh.StockUpdatedHistoryDetails[0]
                                    .DeliveryItem!
                                    .DeliveryRequest
                                    .ScheduledRouteDeliveryRequests[0]
                                    .ScheduledRoute
                                    .User!
                                    .Name!,
                                Phone = suh.StockUpdatedHistoryDetails[0]
                                    .DeliveryItem!
                                    .DeliveryRequest
                                    .ScheduledRouteDeliveryRequests[0]
                                    .ScheduledRoute
                                    .User!
                                    .Phone
                            }
                    ),
                    Pagination = pagination,
                    Message = _config[
                        "ResponseMessages:DeliveryRequestMsg:GetDeliveryRequestsSuccessMsg"
                    ]
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    $"An exception occurred in service {nameof(DeliveryRequestService)}, method {nameof(GetFinishedDeliveryRequestsByDonatedRequestIdForUserAsync)}."
                );
                return new CommonResponse
                {
                    Status = 500,
                    Message = _config["ResponseMessages:CommonMsg:InternalServerErrorMsg"]
                };
            }
        }

        public async Task<CommonResponse> GetFinishedDeliveryRequestByIdOfDonatedRequestForUserAsync(
            Guid deliveryRequestId,
            Guid userId
        )
        {
            try
            {
                DeliveryRequest? deliveryRequest =
                    await _deliveryRequestRepository.FindFinishedDeliveryRequestForDetailByIdAndDonorIdAsync(
                        deliveryRequestId,
                        userId
                    );

                if (deliveryRequest == null)
                    return new CommonResponse
                    {
                        Status = 500,
                        Message = _config[
                            "ResponseMessages:DeliveryRequestMsg:FinishedDeliveryRequestForDonatedRequestNotFoundMsg"
                        ]
                    };

                Report? report =
                    await _reportRepository.FindReportByUserIdAndDeliveryRequestIdAsync(
                        userId,
                        deliveryRequestId,
                        null
                    );

                return new CommonResponse
                {
                    Status = 200,
                    Data = new SimpleDeliveryRequestDetailForUserOrCharityUnitResponse
                    {
                        Id = deliveryRequest.Id,
                        CurrentScheduledTime = JsonConvert.DeserializeObject<ScheduledTime>(
                            deliveryRequest.CurrentScheduledTime!
                        ),
                        ImportedDate = deliveryRequest.DeliveryItems[0].StockUpdatedHistoryDetails[
                            0
                        ]
                            .StockUpdatedHistory
                            .CreatedDate,
                        ImportNote = deliveryRequest.DeliveryItems[0].StockUpdatedHistoryDetails[0]
                            .StockUpdatedHistory
                            .Note,
                        ProofImage = deliveryRequest.ProofImage,
                        Avatar = deliveryRequest.ScheduledRouteDeliveryRequests[0]
                            .ScheduledRoute
                            .User!
                            .Avatar!,
                        Name = deliveryRequest.ScheduledRouteDeliveryRequests[0]
                            .ScheduledRoute
                            .User!
                            .Name!,
                        Phone = deliveryRequest.ScheduledRouteDeliveryRequests[0]
                            .ScheduledRoute
                            .User!
                            .Phone,
                        IsReported = report != null,
                        Report =
                            report != null
                                ? new ReportResponse
                                {
                                    Id = report.Id,
                                    Title = report.Title,
                                    Content = report.Content,
                                    CreatedDate = report.CreatedDate,
                                    Type = report.Type.ToString()
                                }
                                : null,
                        ActivityId =
                            deliveryRequest.DonatedRequestId == null
                                ? null
                                : deliveryRequest.DonatedRequest!.ActivityId == null
                                    ? null
                                    : deliveryRequest.DonatedRequest!.Activity!.Id,
                        ActivityName =
                            deliveryRequest.DonatedRequestId == null
                                ? null
                                : deliveryRequest.DonatedRequest!.ActivityId == null
                                    ? null
                                    : deliveryRequest.DonatedRequest!.Activity!.Name,
                        BranchId = deliveryRequest.Branch.Id,
                        BranchAddress = deliveryRequest.Branch.Address,
                        BranchName = deliveryRequest.Branch.Name,
                        BranchImage = deliveryRequest.Branch.Image,
                        DeliveryItems = deliveryRequest.DeliveryItems
                            .Select(
                                di =>
                                    new FinishedDeliveryItemTypeImportResponse
                                    {
                                        DeliveryItemId = di.Id,
                                        Name = GetFullNameOfItem(di.DonatedItem!.Item),
                                        Image = di.DonatedItem!.Item.Image,
                                        Unit = di.DonatedItem!.Item.ItemTemplate.Unit.Name,
                                        ConfirmedExpirationDate = di.StockUpdatedHistoryDetails[0]
                                            .Stock!
                                            .ExpirationDate,
                                        AssignedQuantity = di.Quantity,
                                        ReceivedQuantity = (double)di.ReceivedQuantity!,
                                        ImportedQuantity = di.StockUpdatedHistoryDetails[0].Quantity
                                    }
                            )
                            .ToList()
                    },
                    Message = _config[
                        "ResponseMessages:DeliveryRequestMsg:GetDeliveryRequestsSuccessMsg"
                    ]
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    $"An exception occurred in service {nameof(DeliveryRequestService)}, method {nameof(GetFinishedDeliveryRequestByIdOfDonatedRequestForUserAsync)}."
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

        public async Task<CommonResponse> GetFinishedDeliveryRequestsByAidRequestIdForCharityUnitAsync(
            Guid aidRequestId,
            Guid userId,
            string userRoleName,
            int? pageSize,
            int? page
        )
        {
            try
            {
                AidRequest? aidRequest = await _aidRequestRepository.FindAidRequestByIdAsync(
                    aidRequestId
                );

                if (
                    aidRequest == null
                    || (
                        userRoleName == RoleEnum.CHARITY.ToString()
                        && aidRequest.CharityUnit!.UserId != userId
                    )
                )
                    return new CommonResponse
                    {
                        Status = 400,
                        Message = _config["ResponseMessages:AidRequest:AidRequestNotFound"]
                    };

                List<StockUpdatedHistory> stockUpdatedHistories =
                    await _stockUpdatedHistoryRepository.FindStockUpdatedHistoriesByAidRequestIdAsync(
                        aidRequestId
                    );

                stockUpdatedHistories = stockUpdatedHistories
                    .OrderByDescending(suh => suh.CreatedDate)
                    .ToList();

                Pagination pagination = new Pagination();
                pagination.PageSize = pageSize == null ? 10 : pageSize.Value;
                pagination.CurrentPage = page == null ? 1 : page.Value;
                pagination.Total = stockUpdatedHistories.Count;
                stockUpdatedHistories = stockUpdatedHistories
                    .Skip((pagination.CurrentPage - 1) * pagination.PageSize)
                    .Take(pagination.PageSize)
                    .ToList();

                List<SimpleDeliveryRequestForCharityUnitResponse> stockUpdatedHistoryResponses =
                    new();

                foreach (StockUpdatedHistory suh in stockUpdatedHistories)
                {
                    if (suh.StockUpdatedHistoryDetails[0].DeliveryItemId != null)
                    {
                        stockUpdatedHistoryResponses.Add(
                            new SimpleDeliveryRequestForCharityUnitResponse
                            {
                                Id = suh.StockUpdatedHistoryDetails[0]
                                    .DeliveryItem!
                                    .DeliveryRequest
                                    .Id,
                                CurrentScheduledTime = JsonConvert.DeserializeObject<ScheduledTime>(
                                    suh.StockUpdatedHistoryDetails[0]
                                        .DeliveryItem!
                                        .DeliveryRequest
                                        .CurrentScheduledTime!
                                ),
                                ExportedDate = suh.CreatedDate,
                                FinishedDate = suh.StockUpdatedHistoryDetails[0]
                                    .DeliveryItem!
                                    .DeliveryRequest
                                    .ScheduledRouteDeliveryRequests[0]
                                    .ScheduledRoute
                                    .FinishedDate,
                                ExportNote = suh.Note,
                                ProofImage = suh.StockUpdatedHistoryDetails[0]
                                    .DeliveryItem!
                                    .DeliveryRequest
                                    .ProofImage,
                                Avatar = suh.StockUpdatedHistoryDetails[0]
                                    .DeliveryItem!
                                    .DeliveryRequest
                                    .ScheduledRouteDeliveryRequests[0]
                                    .ScheduledRoute
                                    .User!
                                    .Avatar!,
                                Name = suh.StockUpdatedHistoryDetails[0]
                                    .DeliveryItem!
                                    .DeliveryRequest
                                    .ScheduledRouteDeliveryRequests[0]
                                    .ScheduledRoute
                                    .User!
                                    .Name!,
                                Phone = suh.StockUpdatedHistoryDetails[0]
                                    .DeliveryItem!
                                    .DeliveryRequest
                                    .ScheduledRouteDeliveryRequests[0]
                                    .ScheduledRoute
                                    .User!
                                    .Phone
                            }
                        );
                    }
                    else
                    {
                        stockUpdatedHistoryResponses.Add(
                            new SimpleDeliveryRequestForCharityUnitResponse
                            {
                                Id = suh.Id,
                                ExportedDate = suh.CreatedDate,
                                ExportNote = suh.Note,
                            }
                        );
                    }
                }

                return new CommonResponse
                {
                    Status = 200,
                    Data = stockUpdatedHistoryResponses,
                    Pagination = pagination,
                    Message = _config[
                        "ResponseMessages:DeliveryRequestMsg:GetDeliveryRequestsSuccessMsg"
                    ]
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    $"An exception occurred in service {nameof(DeliveryRequestService)}, method {nameof(GetFinishedDeliveryRequestsByAidRequestIdForCharityUnitAsync)}."
                );
                return new CommonResponse
                {
                    Status = 500,
                    Message = _config["ResponseMessages:CommonMsg:InternalServerErrorMsg"]
                };
            }
        }

        public async Task<CommonResponse> GetFinishedDeliveryRequestByIdOfAidRequestForCharityUnitAsync(
            Guid deliveryRequestId,
            Guid? userId
        )
        {
            try
            {
                DeliveryRequest? deliveryRequest =
                    await _deliveryRequestRepository.FindFinishedDeliveryRequestForDetailByIdAndUserIOfCharityUnitAsync(
                        deliveryRequestId,
                        userId
                    );

                if (
                    deliveryRequest == null
                    || !deliveryRequest.DeliveryItems.All(
                        di => di.StockUpdatedHistoryDetails.All(suhd => suhd.StockId != null)
                    )
                )
                    return new CommonResponse
                    {
                        Status = 400,
                        Message = _config[
                            "ResponseMessages:DeliveryRequestMsg:FinishedDeliveryRequestForAidRequestNotFoundMsg"
                        ]
                    };

                Report? report =
                    userId != null
                        ? await _reportRepository.FindReportByUserIdAndDeliveryRequestIdAsync(
                            (Guid)userId,
                            deliveryRequestId,
                            null
                        )
                        : null;

                return new CommonResponse
                {
                    Status = 200,
                    Data = new SimpleDeliveryRequestDetailForCharityUnitResponse
                    {
                        Id = deliveryRequest.Id,
                        CurrentScheduledTime = JsonConvert.DeserializeObject<ScheduledTime>(
                            deliveryRequest.CurrentScheduledTime!
                        ),
                        ExportedDate = deliveryRequest.DeliveryItems[0].StockUpdatedHistoryDetails[
                            0
                        ]
                            .StockUpdatedHistory
                            .CreatedDate,
                        ExportNote = deliveryRequest.DeliveryItems[0].StockUpdatedHistoryDetails[0]
                            .StockUpdatedHistory
                            .Note,
                        ProofImage = deliveryRequest.ProofImage,
                        Avatar = deliveryRequest.ScheduledRouteDeliveryRequests[0]
                            .ScheduledRoute
                            .User!
                            .Avatar!,
                        Name = deliveryRequest.ScheduledRouteDeliveryRequests[0]
                            .ScheduledRoute
                            .User!
                            .Name!,
                        Phone = deliveryRequest.ScheduledRouteDeliveryRequests[0]
                            .ScheduledRoute
                            .User!
                            .Phone,
                        IsReported = report != null,
                        Report =
                            report != null
                                ? new ReportResponse
                                {
                                    Id = report.Id,
                                    Title = report.Title,
                                    Content = report.Content,
                                    CreatedDate = report.CreatedDate,
                                    Type = report.Type.ToString()
                                }
                                : null,
                        BranchId = deliveryRequest.Branch.Id,
                        BranchAddress = deliveryRequest.Branch.Address,
                        BranchName = deliveryRequest.Branch.Name,
                        BranchImage = deliveryRequest.Branch.Image,
                        DeliveryItems = deliveryRequest.DeliveryItems
                            .Select(
                                di =>
                                    new FinishedDeliveryItemTypeExportResponse
                                    {
                                        DeliveryItemId = di.Id,
                                        Name = GetFullNameOfItem(di.AidItem!.Item),
                                        Image = di.AidItem!.Item.Image,
                                        Unit = di.AidItem!.Item.ItemTemplate.Unit.Name,
                                        AssignedQuantity = di.Quantity,
                                        ExportedQuantity = di.StockUpdatedHistoryDetails
                                            .Select(suhd => suhd.Quantity)
                                            .Sum(),
                                        Stocks = di.StockUpdatedHistoryDetails
                                            .Select(
                                                suhd =>
                                                    new SimpleStockUpdatedHistoryDetailResponse
                                                    {
                                                        StockId = suhd.Stock!.Id,
                                                        StockCode = suhd.Stock.StockCode,
                                                        Quantity = suhd.Quantity,
                                                        ExpirationDate = suhd.Stock!.ExpirationDate
                                                    }
                                            )
                                            .ToList()
                                    }
                            )
                            .ToList()
                    },
                    Message = _config[
                        "ResponseMessages:DeliveryRequestMsg:GetDeliveryRequestsSuccessMsg"
                    ]
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    $"An exception occurred in service {nameof(DeliveryRequestService)}, method {nameof(GetFinishedDeliveryRequestByIdOfAidRequestForCharityUnitAsync)}."
                );
                return new CommonResponse
                {
                    Status = 500,
                    Message = _config["ResponseMessages:CommonMsg:InternalServerErrorMsg"]
                };
            }
        }
    }
}
