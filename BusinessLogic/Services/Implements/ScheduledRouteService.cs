using BusinessLogic.Utils.FirebaseService;
using BusinessLogic.Utils.Notification.Implements;
using BusinessLogic.Utils.OpenRouteService;
using DataAccess.Entities;
using DataAccess.EntityEnums;
using DataAccess.Models.Requests;
using DataAccess.Models.Requests.ModelBinders;
using DataAccess.Models.Requests.OpenRouteService.Request;
using DataAccess.Models.Requests.OpenRouteService.Response;
using DataAccess.Models.Responses;
using DataAccess.ModelsEnum;
using DataAccess.Repositories;
using Hangfire;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Globalization;
using System.Transactions;

namespace BusinessLogic.Services.Implements
{
    public class ScheduledRouteService : IScheduledRouteService
    {
        private readonly IScheduledRouteRepository _scheduledRouteRepository;
        private readonly IDeliveryRequestRepository _deliveryRequestRepository;
        private readonly IDeliveryItemRepository _deliveryItemRepository;
        private readonly IDonatedItemRepository _donatedItemRepository;
        private readonly IBranchRepository _branchRepository;
        private readonly IConfiguration _config;
        private readonly IOpenRouteService _openRouteService;
        private readonly IAidRequestRepository _aidRequestRepository;
        private readonly IDonatedRequestRepository _donatedRequestRepository;
        private readonly ILogger<ScheduledRouteService> _logger;
        private readonly IScheduledRouteDeliveryRequestRepository _scheduledRouteDeliveryRequestRepository;
        private readonly IAidItemRepository _aidItemRepository;
        private readonly ICollaboratorRepository _collaboratorRepository;
        private readonly ICharityUnitRepository _charityUnitRepository;
        private readonly IStockRepository _stockRepository;
        private readonly IStockUpdatedHistoryDetailRepository _stockUpdatedHistoryDetailRepository;
        private readonly IStockUpdatedHistoryRepository _stockUpdatedHistoryRepository;
        private readonly IUserRepository _userRepository;
        private readonly IItemRepository _itemRepository;
        private readonly IActivityRepository _activityRepository;
        private readonly IHubContext<NotificationSignalSender> _hubContext;
        private readonly INotificationRepository _notificationRepository;
        private readonly IFirebaseNotificationService _firebaseNotificationService;
        private readonly IHubContext<ScheduleRouteHub> _scheduleHubContext;
        private readonly ITargetProcessRepository _targetProcessRepository;

        public ScheduledRouteService(
            IScheduledRouteRepository scheduledRouteRepository,
            IDeliveryRequestRepository deliveryRequestRepository,
            IDeliveryItemRepository deliveryItemRepository,
            IDonatedItemRepository donatedItemRepository,
            IBranchRepository branchRepository,
            IConfiguration config,
            IOpenRouteService openRouteService,
            IAidRequestRepository aidRequestRepository,
            IDonatedRequestRepository donatedRequestRepository,
            ILogger<ScheduledRouteService> logger,
            IScheduledRouteDeliveryRequestRepository scheduledRouteDeliveryRequestRepository,
            IAidItemRepository aidItemRepository,
            ICollaboratorRepository collaboratorRepository,
            ICharityUnitRepository charityUnitRepository,
            IStockRepository stockRepository,
            IStockUpdatedHistoryDetailRepository stockUpdatedHistoryDetailRepository,
            IStockUpdatedHistoryRepository stockUpdatedHistoryRepository,
            IUserRepository userRepository,
            IItemRepository itemRepository,
            IActivityRepository activityRepository,
            IHubContext<NotificationSignalSender> hubContext,
            INotificationRepository notificationRepository,
            IFirebaseNotificationService firebaseNotificationService,
            IHubContext<ScheduleRouteHub> scheduleHubContext,
            ITargetProcessRepository targetProcessRepository
        )
        {
            _scheduledRouteRepository = scheduledRouteRepository;
            _deliveryRequestRepository = deliveryRequestRepository;
            _deliveryItemRepository = deliveryItemRepository;
            _donatedItemRepository = donatedItemRepository;
            _branchRepository = branchRepository;
            _config = config;
            _openRouteService = openRouteService;
            _aidRequestRepository = aidRequestRepository;
            _donatedRequestRepository = donatedRequestRepository;
            _logger = logger;
            _scheduledRouteDeliveryRequestRepository = scheduledRouteDeliveryRequestRepository;
            _aidItemRepository = aidItemRepository;
            _collaboratorRepository = collaboratorRepository;
            _charityUnitRepository = charityUnitRepository;
            _stockRepository = stockRepository;
            _stockUpdatedHistoryDetailRepository = stockUpdatedHistoryDetailRepository;
            _stockUpdatedHistoryRepository = stockUpdatedHistoryRepository;
            _userRepository = userRepository;
            _itemRepository = itemRepository;
            _activityRepository = activityRepository;
            _hubContext = hubContext;
            _notificationRepository = notificationRepository;
            _firebaseNotificationService = firebaseNotificationService;
            _scheduleHubContext = scheduleHubContext;
            _targetProcessRepository = targetProcessRepository;
        }

        public async Task<CommonResponse> UpdateScheduledRoutesByDeliveryTypeAndBranchAdminIdAsync(
            DeliveryType deliveryType,
            Guid userId
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

                await UpdateScheduledRoutes(deliveryType, branch.Id);

                return new CommonResponse
                {
                    Status = 200,
                    Message = _config[
                        "ResponseMessages:ScheduledRouteMsg:CheckToCreateNewScheduledRoutesSuccessMsg"
                    ]
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    $"An exception occurred in service {nameof(ScheduledRouteService)}, method {nameof(UpdateScheduledRoutesByDeliveryTypeAndBranchAdminIdAsync)}."
                );
            }
            return new CommonResponse
            {
                Status = 500,
                Message = _config["ResponseMessages:CommonMsg:InternalServerErrorMsg"]
            };
        }

        public async Task AutoUpdateAvailableAndLateScheduledRoute()
        {
            await AutoCheckLateScheduleRoute();
            await UpdateScheduledRoutes(null, null);
        }

        public async Task UpdateScheduledRoutes(DeliveryType? deliveryType, Guid? branchId)
        {
            using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                List<List<DeliveryRequest>> finalDeliveryRequestsForScheduledRoutes =
                    await _deliveryRequestRepository.FindPendingDeliveryRequestsByDeliveryTypeAndMainBranchIdAsync(
                        deliveryType,
                        branchId
                    );

                List<GroupOfDeliveryRequestWithSamePartOfScheduledTime> groupsOfDeliveryRequestWithSamePartOfScheduledTimes =
                    new List<GroupOfDeliveryRequestWithSamePartOfScheduledTime>();

                groupsOfDeliveryRequestWithSamePartOfScheduledTimes =
                    await GroupListOfDeliveryRequestsByScheduleTime(
                        finalDeliveryRequestsForScheduledRoutes
                    );

                if (groupsOfDeliveryRequestWithSamePartOfScheduledTimes.Count != 0)
                {
                    groupsOfDeliveryRequestWithSamePartOfScheduledTimes = UpdateNumberOfVehicles(
                        groupsOfDeliveryRequestWithSamePartOfScheduledTimes
                    );

                    await AddOptimizedScheduledRoutes(
                        groupsOfDeliveryRequestWithSamePartOfScheduledTimes
                    );
                }

                scope.Complete();
            }
        }

        private async Task UpdateScheduledRoutesForUnassignedDeliveryRequest(
            List<DeliveryRequest> deliveryRequests
        )
        {
            List<List<DeliveryRequest>> finalDeliveryRequestsForScheduledRoutes = new List<
                List<DeliveryRequest>
            >
            {
                deliveryRequests
            };

            List<GroupOfDeliveryRequestWithSamePartOfScheduledTime> groupsOfDeliveryRequestWithSamePartOfScheduledTimes =
                await GroupListOfDeliveryRequestsByScheduleTime(
                    finalDeliveryRequestsForScheduledRoutes
                );

            groupsOfDeliveryRequestWithSamePartOfScheduledTimes = UpdateNumberOfVehicles(
                groupsOfDeliveryRequestWithSamePartOfScheduledTimes
            );

            if (groupsOfDeliveryRequestWithSamePartOfScheduledTimes.Count == 0)
                return;

            await AddOptimizedScheduledRoutes(groupsOfDeliveryRequestWithSamePartOfScheduledTimes);
        }

        public async Task AddOptimizedScheduledRoutes(
            List<GroupOfDeliveryRequestWithSamePartOfScheduledTime> groupsOfDeliveryRequestWithSamePartOfScheduledTimes
        )
        {
            Dictionary<int, DeliveryRequest> deliveryRequestIdPairs =
                new Dictionary<int, DeliveryRequest>();
            List<OptimizeRequest> optimizeRequests = new List<OptimizeRequest>();
            int deliveryRequestCount = 0;
            int vehicleCount = 0;
            int groupCount = 0;
            double speedFactor = double.Parse(_config["OpenRoute:SpeedFactor"]);
            foreach (
                GroupOfDeliveryRequestWithSamePartOfScheduledTime tmp in groupsOfDeliveryRequestWithSamePartOfScheduledTimes
            )
            {
                List<GroupOfDeliveryRequestWithSamePartOfScheduledTime> tmps =
                    new List<GroupOfDeliveryRequestWithSamePartOfScheduledTime> { tmp };
                List<Vehicle> vehicles = new List<Vehicle>();
                List<Shipment> shipments = new List<Shipment>();

                foreach (
                    GroupOfDeliveryRequestWithSamePartOfScheduledTime groupOfDeliveryRequestWithSamePartOfScheduledTime in tmps
                )
                {
                    List<double> swappedLocationOfBranch = GetMainBranchSwappedLocation(
                        groupOfDeliveryRequestWithSamePartOfScheduledTime.DeliveryRequests[0]
                    );
                    groupCount += 1;
                    for (
                        int i = 0;
                        i < groupOfDeliveryRequestWithSamePartOfScheduledTime.NumberOfVehicles;
                        i++
                    )
                    {
                        vehicleCount += 1;
                        vehicles.Add(
                            new Vehicle
                            {
                                Id = vehicleCount,
                                Capacity = new List<int>
                                {
                                    int.Parse(_config["ScheduledRoute:MaxVolumnPercentToSchedule"])
                                },
                                Skills = new List<int> { groupCount },
                                Start = swappedLocationOfBranch,
                                End = swappedLocationOfBranch,
                                time_window = GetTimeWindowByScheduledTime(
                                    groupOfDeliveryRequestWithSamePartOfScheduledTime.ScheduledTime
                                )
                            }
                        );
                    }
                    foreach (
                        DeliveryRequest deliveryRequest in groupOfDeliveryRequestWithSamePartOfScheduledTime.DeliveryRequests
                    )
                    {
                        deliveryRequestCount += 1;
                        deliveryRequestIdPairs[deliveryRequestCount] = deliveryRequest;
                        shipments.Add(
                            new Shipment
                            {
                                Amount = new List<int>
                                {
                                    (int)
                                        Math.Ceiling(
                                            deliveryRequest.DeliveryItems
                                                .Select(
                                                    di =>
                                                        di.Quantity
                                                        * 100
                                                        / (
                                                            deliveryRequest.DonatedRequest != null
                                                                ? di.DonatedItem!
                                                                    .Item
                                                                    .MaximumTransportVolume
                                                                : di.AidItem!
                                                                    .Item
                                                                    .MaximumTransportVolume
                                                        )
                                                )
                                                .Sum()
                                        )
                                },
                                Skills = new List<int> { groupCount },
                                Pickup = new Place
                                {
                                    Id = deliveryRequestCount,
                                    Service = (int)Math.Ceiling(300 * speedFactor),
                                    Location = GetSwappedPickupLocation(deliveryRequest)
                                    //Location =
                                    //    deliveryRequest.DeliveryType
                                    //    == DeliveryType.BRANCH_TO_AID_REQUEST
                                    //        ? swappedLocationOfBranch
                                    //        : donatedOrAidRequestSwappedLocation,
                                },
                                Delivery = new Place
                                {
                                    Id = deliveryRequestCount,
                                    Service = (int)Math.Ceiling(300 * speedFactor),
                                    Location = GetSwappedDeliveryLocation(deliveryRequest)
                                    //Location =
                                    //    deliveryRequest.DeliveryType
                                    //    == DeliveryType.BRANCH_TO_AID_REQUEST
                                    //        ? donatedOrAidRequestSwappedLocation
                                    //        : swappedLocationOfBranch,
                                }
                            }
                        );
                    }
                }

                optimizeRequests.Add(
                    new OptimizeRequest { Vehicles = vehicles, Shipments = shipments }
                );
            }
            foreach (OptimizeRequest optimizeRequest in optimizeRequests)
            {
                OptimizeResponse? optimizeResponse =
                    await _openRouteService.GetOptimizeResponseAsync(
                        new OptimizeRequest
                        {
                            Vehicles = optimizeRequest.Vehicles,
                            Shipments = optimizeRequest.Shipments
                        }
                    );
                if (optimizeResponse != null)
                {
                    await CheckToCreateScheduledRoute(optimizeResponse, deliveryRequestIdPairs);
                }
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

        private List<double> GetSwappedPickupLocation(DeliveryRequest deliveryRequest)
        {
            if (deliveryRequest.DonatedRequestId != null)
            {
                return _openRouteService.SwapCoordinates(
                    _openRouteService.GetCoordinatesByLocation(
                        deliveryRequest.DonatedRequest!.Location
                    )!
                );
            }
            else
            {
                return _openRouteService.SwapCoordinates(
                    _openRouteService.GetCoordinatesByLocation(deliveryRequest.Branch.Location)!
                );
            }
        }

        private string GetPickupAddress(DeliveryRequest deliveryRequest)
        {
            if (deliveryRequest.DonatedRequestId != null)
            {
                return deliveryRequest.DonatedRequest!.Address;
            }
            else
            {
                return deliveryRequest.Branch.Address;
            }
        }

        private async Task<SimpleDeliveryRequestResponse> GetPickupSimpleDeliveryRequestResponse(
            DeliveryRequest deliveryRequest,
            bool isForAdmin
        )
        {
            if (deliveryRequest.DonatedRequestId != null)
            {
                Activity? activity =
                    deliveryRequest.DonatedRequest!.ActivityId == null
                        ? null
                        : await _activityRepository.FindActivityByIdAsync(
                            (Guid)deliveryRequest.DonatedRequest.ActivityId
                        );

                return new SimpleDeliveryRequestResponse
                {
                    Id = deliveryRequest.Id,
                    Status = deliveryRequest.Status.ToString(),
                    Address = deliveryRequest.DonatedRequest!.Address,
                    Location = _openRouteService.GetCoordinatesByLocation(
                        deliveryRequest.DonatedRequest.Location
                    )!,
                    CurrentScheduledTime = JsonConvert.DeserializeObject<ScheduledTime>(
                        deliveryRequest.CurrentScheduledTime!
                    ),
                    Images = deliveryRequest.DonatedRequest!.Images.Split(",").ToList(),
                    ProofImage = deliveryRequest.ProofImage,
                    Avatar = deliveryRequest.DonatedRequest.User.Avatar,
                    Name = deliveryRequest.DonatedRequest.User.Name!,
                    Phone = deliveryRequest.DonatedRequest.User.Phone,
                    ActivityId = activity == null ? null : activity.Id,
                    ActivityName = activity == null ? null : activity.Name,
                    DeliveryItems = deliveryRequest.DeliveryItems
                        .Select(
                            di =>
                                new DeliveryItemResponse
                                {
                                    DeliveryItemId = di.Id,
                                    Name = GetFullNameOfItem(di.DonatedItem!.Item),
                                    Image = di.DonatedItem!.Item.Image,
                                    Unit = di.DonatedItem!.Item.ItemTemplate.Unit.Name,
                                    Quantity = di.Quantity,
                                    Stocks =
                                        isForAdmin
                                        && di.StockUpdatedHistoryDetails.Count > 0
                                        && di.StockUpdatedHistoryDetails.All(
                                            suhd => suhd.StockId != null
                                        )
                                            ? di.StockUpdatedHistoryDetails
                                                .Select(
                                                    suhd =>
                                                        new SimpleStockUpdatedHistoryDetailResponse
                                                        {
                                                            StockId = suhd.Stock!.Id,
                                                            StockCode = suhd.Stock!.StockCode,
                                                            ExpirationDate =
                                                                suhd.Stock.ExpirationDate,
                                                            Quantity = suhd.Quantity
                                                        }
                                                )
                                                .ToList()
                                            : null,
                                    InitialExpirationDate = di.DonatedItem.InitialExpirationDate,
                                    ReceivedQuantity = di.ReceivedQuantity
                                }
                        )
                        .ToList()
                };
            }
            else
            {
                return new SimpleDeliveryRequestResponse
                {
                    Id = deliveryRequest.Id,
                    Status = deliveryRequest.Status.ToString(),
                    Address = deliveryRequest.Branch.Address,
                    Location = _openRouteService.GetCoordinatesByLocation(
                        deliveryRequest.Branch.Location
                    )!,
                    CurrentScheduledTime = JsonConvert.DeserializeObject<ScheduledTime>(
                        deliveryRequest.CurrentScheduledTime!
                    ),
                    ProofImage = deliveryRequest.ProofImage,
                    Avatar = deliveryRequest.Branch.Image,
                    Name = deliveryRequest.Branch.Name,
                    Phone = deliveryRequest.Branch.BranchAdmin!.Phone,
                    DeliveryItems = deliveryRequest.DeliveryItems
                        .Select(
                            di =>
                                new DeliveryItemResponse
                                {
                                    DeliveryItemId = di.Id,
                                    Name = GetFullNameOfItem(di.AidItem!.Item),
                                    Image = di.AidItem!.Item.Image,
                                    Unit = di.AidItem!.Item.ItemTemplate.Unit.Name,
                                    Quantity = di.Quantity,
                                    Stocks =
                                        isForAdmin
                                        && di.StockUpdatedHistoryDetails.Count > 0
                                        && di.StockUpdatedHistoryDetails.All(
                                            suhd => suhd.StockId != null
                                        )
                                            ? di.StockUpdatedHistoryDetails
                                                .Select(
                                                    suhd =>
                                                        new SimpleStockUpdatedHistoryDetailResponse
                                                        {
                                                            StockId = suhd.Stock!.Id,
                                                            StockCode = suhd.Stock!.StockCode,
                                                            ExpirationDate =
                                                                suhd.Stock.ExpirationDate,
                                                            Quantity = suhd.Quantity
                                                        }
                                                )
                                                .ToList()
                                            : null,
                                    ReceivedQuantity = di.ReceivedQuantity
                                }
                        )
                        .ToList()
                };
            }
        }

        private List<double> GetSwappedDeliveryLocation(DeliveryRequest deliveryRequest)
        {
            if (deliveryRequest.DonatedRequestId != null)
            {
                return _openRouteService.SwapCoordinates(
                    _openRouteService.GetCoordinatesByLocation(deliveryRequest.Branch.Location)!
                );
            }
            else
            {
                return _openRouteService.SwapCoordinates(
                    _openRouteService.GetCoordinatesByLocation(
                        deliveryRequest.AidRequest!.Location
                    )!
                );
            }
        }

        private string GetDeliveryAddress(DeliveryRequest deliveryRequest)
        {
            if (deliveryRequest.DonatedRequestId != null)
            {
                return deliveryRequest.Branch.Address;
            }
            else
            {
                return deliveryRequest.AidRequest!.Address;
            }
        }

        private async Task<SimpleDeliveryRequestResponse> GetDeliverySimpleDeliveryRequestResponse(
            DeliveryRequest deliveryRequest,
            bool isForAdmin
        )
        {
            if (deliveryRequest.DonatedRequestId != null)
            {
                //return deliveryRequest.Branch.Address;
                return new SimpleDeliveryRequestResponse
                {
                    Address = deliveryRequest.Branch.Address,
                    Location = _openRouteService.GetCoordinatesByLocation(
                        deliveryRequest.Branch.Location
                    )!,
                    Avatar = deliveryRequest.Branch.Image,
                    Name = deliveryRequest.Branch.Name,
                    Phone = deliveryRequest.Branch.BranchAdmin!.Phone
                };
            }
            else
            {
                List<StockUpdatedHistoryDetail> stockUpdatedHistoryDetails =
                    await _stockUpdatedHistoryDetailRepository.GetStockUpdatedHistoryDetailsByDeliveryItemIdAsync(
                        deliveryRequest.DeliveryItems[0].Id
                    );

                StockUpdatedHistoryDetail? stockUpdatedHistoryDetail =
                    stockUpdatedHistoryDetails.Count > 0 ? stockUpdatedHistoryDetails[0] : null;

                Activity? activity =
                    stockUpdatedHistoryDetail == null
                    || stockUpdatedHistoryDetail.StockId == null
                    || stockUpdatedHistoryDetail.Stock!.ActivityId == null
                        ? null
                        : stockUpdatedHistoryDetail.Stock.Activity;

                return new SimpleDeliveryRequestResponse
                {
                    Id = deliveryRequest.Id,
                    Status = deliveryRequest.Status.ToString(),
                    Address = deliveryRequest.AidRequest!.Address,
                    Location = _openRouteService.GetCoordinatesByLocation(
                        deliveryRequest.AidRequest!.Location
                    )!,
                    CurrentScheduledTime = JsonConvert.DeserializeObject<ScheduledTime>(
                        deliveryRequest.CurrentScheduledTime!
                    ),
                    ProofImage = deliveryRequest.ProofImage,
                    Avatar = deliveryRequest.AidRequest!.CharityUnit!.Image,
                    Name = deliveryRequest.AidRequest!.CharityUnit!.Name,
                    Phone = deliveryRequest.AidRequest!.CharityUnit!.User.Phone,
                    ActivityId = activity == null ? null : activity.Id,
                    ActivityName = activity == null ? null : activity.Name,
                    DeliveryItems = deliveryRequest.DeliveryItems
                        .Select(
                            di =>
                                new DeliveryItemResponse
                                {
                                    DeliveryItemId = di.Id,
                                    Name = GetFullNameOfItem(di.AidItem!.Item),
                                    Image = di.AidItem!.Item.Image,
                                    Unit = di.AidItem!.Item.ItemTemplate.Unit.Name,
                                    Quantity = di.Quantity,
                                    Stocks =
                                        isForAdmin
                                        && di.StockUpdatedHistoryDetails.Count > 0
                                        && di.StockUpdatedHistoryDetails.All(
                                            suhd => suhd.StockId != null
                                        )
                                            ? di.StockUpdatedHistoryDetails
                                                .Select(
                                                    suhd =>
                                                        new SimpleStockUpdatedHistoryDetailResponse
                                                        {
                                                            StockId = suhd.Stock!.Id,
                                                            StockCode = suhd.Stock!.StockCode,
                                                            ExpirationDate =
                                                                suhd.Stock.ExpirationDate,
                                                            Quantity = suhd.Quantity
                                                        }
                                                )
                                                .ToList()
                                            : null,
                                    ReceivedQuantity = di.ReceivedQuantity
                                }
                        )
                        .ToList()
                };
            }
        }

        private Guid GetMainBranchId(DeliveryRequest deliveryRequest)
        {
            if (deliveryRequest.DonatedRequestId != null)
            {
                return deliveryRequest.BranchId;
            }
            else
            {
                if (deliveryRequest.AidRequest!.CharityUnitId != null)
                {
                    return deliveryRequest.BranchId;
                }
                else
                {
                    return (Guid)deliveryRequest.AidRequest.BranchId!;
                }
            }
        }

        private List<double> GetMainBranchSwappedLocation(DeliveryRequest deliveryRequest)
        {
            if (deliveryRequest.DonatedRequestId != null)
            {
                return _openRouteService.SwapCoordinates(
                    _openRouteService.GetCoordinatesByLocation(deliveryRequest.Branch.Location)!
                );
            }
            else
            {
                if (deliveryRequest.AidRequest!.CharityUnitId != null)
                {
                    return _openRouteService.SwapCoordinates(
                        _openRouteService.GetCoordinatesByLocation(deliveryRequest.Branch.Location)!
                    );
                }
                else
                {
                    return _openRouteService.SwapCoordinates(
                        _openRouteService.GetCoordinatesByLocation(
                            deliveryRequest.AidRequest.Location
                        )!
                    );
                }
            }
        }

        private string GetMainBranchAddress(DeliveryRequest deliveryRequest)
        {
            if (deliveryRequest.DonatedRequestId != null)
            {
                return deliveryRequest.Branch.Address;
            }
            else
            {
                if (deliveryRequest.AidRequest!.CharityUnitId != null)
                {
                    return deliveryRequest.Branch.Address;
                }
                else
                {
                    return deliveryRequest.AidRequest.Address;
                }
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

        private SimpleDeliveryRequestResponse GetMainBranchSimpleDeliveryRequestResponse(
            DeliveryRequest deliveryRequest
        )
        {
            if (deliveryRequest.DonatedRequestId != null)
            {
                return new SimpleDeliveryRequestResponse
                {
                    Address = deliveryRequest.Branch.Address,
                    Location = _openRouteService.GetCoordinatesByLocation(
                        deliveryRequest.Branch.Location
                    )!,
                    Avatar = deliveryRequest.Branch.Image,
                    Name = deliveryRequest.Branch.Name,
                    Phone = deliveryRequest.Branch.BranchAdmin!.Phone
                };
            }
            else
            {
                if (deliveryRequest.AidRequest!.CharityUnitId != null)
                {
                    return new SimpleDeliveryRequestResponse
                    {
                        Address = deliveryRequest.Branch.Address,
                        Location = _openRouteService.GetCoordinatesByLocation(
                            deliveryRequest.Branch.Location
                        )!,
                        Avatar = deliveryRequest.Branch.Image,
                        Name = deliveryRequest.Branch.Name,
                        Phone = deliveryRequest.Branch.BranchAdmin!.Phone
                    };
                }
                else
                {
                    return new SimpleDeliveryRequestResponse
                    {
                        Address = deliveryRequest.AidRequest.Branch!.Address,
                        Location = _openRouteService.GetCoordinatesByLocation(
                            deliveryRequest.AidRequest.Branch!.Location
                        )!,
                        Avatar = deliveryRequest.AidRequest.Branch!.Image,
                        Name = deliveryRequest.AidRequest.Branch!.Name,
                        Phone = deliveryRequest.AidRequest.Branch!.BranchAdmin!.Phone
                    };
                }
            }
        }

        private List<GroupOfDeliveryRequestWithSamePartOfScheduledTime> UpdateNumberOfVehicles(
            List<GroupOfDeliveryRequestWithSamePartOfScheduledTime> groupsOfDeliveryRequestWithSamePartOfScheduledTimes
        )
        {
            double maxSum =
                double.Parse(_config["ScheduledRoute:MaxVolumnPercentToSchedule"])
                * double.Parse(_config["OpenRoute:MaxVehicles"])
                / 100;
            List<GroupOfDeliveryRequestWithSamePartOfScheduledTime> finalResult =
                new List<GroupOfDeliveryRequestWithSamePartOfScheduledTime>();
            foreach (
                GroupOfDeliveryRequestWithSamePartOfScheduledTime groupOfDeliveryRequestWithSamePartOfScheduledTime in groupsOfDeliveryRequestWithSamePartOfScheduledTimes
            )
            {
                //foreach (
                //    DeliveryRequest deliveryRequest in groupOfDeliveryRequestWithSamePartOfScheduledTime.DeliveryRequests
                //)
                //{
                //    if (deliveryRequest.DonatedRequestId != null)
                //    {
                //        foreach (DeliveryItem deliveryItem in deliveryRequest.DeliveryItems)
                //        {
                //            deliveryItem.DonatedItem =
                //                await _donatedItemRepository.FindDonatedItemByIdAsync(
                //                    (Guid)deliveryItem.DonatedItemId!
                //                );
                //        }
                //    }
                //    else
                //    {
                //        foreach (DeliveryItem deliveryItem in deliveryRequest.DeliveryItems)
                //        {
                //            deliveryItem.AidItem = await _aidItemRepository.FindAidItemByIdAsync(
                //                (Guid)deliveryItem.AidItemId!
                //            );
                //        }
                //    }
                //}

                groupOfDeliveryRequestWithSamePartOfScheduledTime.DeliveryRequests =
                    groupOfDeliveryRequestWithSamePartOfScheduledTime.DeliveryRequests
                        .OrderByDescending(d => GetTotalTransportVolumnOfDeliveryRequest(d))
                        .ToList();

                List<List<DeliveryRequest>> groupsOfDeliveryRequests =
                    new List<List<DeliveryRequest>>();

                int batchSize = int.Parse(_config["OpenRoute:MaxShipments"]);

                int noOfBatch = (int)
                    Math.Ceiling(
                        groupOfDeliveryRequestWithSamePartOfScheduledTime.DeliveryRequests.Count
                            / (double)batchSize
                    );

                //for (int i = 0; i < noOfBatch; i++)
                //{
                //    groupsOfDeliveryRequests.Add(new List<DeliveryRequest>());
                //}

                //for (
                //    int i = 0;
                //    i < groupOfDeliveryRequestWithSamePartOfScheduledTime.DeliveryRequests.Count;
                //    i++
                //)
                //{
                //    groupsOfDeliveryRequests[i % noOfBatch].Add(
                //        groupOfDeliveryRequestWithSamePartOfScheduledTime.DeliveryRequests[i]
                //    );
                //}

                for (
                    int i = 0;
                    i < groupOfDeliveryRequestWithSamePartOfScheduledTime.DeliveryRequests.Count;
                    i += batchSize
                )
                {
                    List<DeliveryRequest> batch =
                        groupOfDeliveryRequestWithSamePartOfScheduledTime.DeliveryRequests
                            .Skip(i)
                            .Take(batchSize)
                            .ToList();
                    groupsOfDeliveryRequests.Add(batch);
                }

                List<List<DeliveryRequest>> result = new List<List<DeliveryRequest>>();
                foreach (List<DeliveryRequest> deliveryRequests in groupsOfDeliveryRequests)
                {
                    List<DeliveryRequest> currentList = new List<DeliveryRequest>();
                    double totalPercentOfVolumn = 0;

                    foreach (DeliveryRequest deliveryRequest in deliveryRequests)
                    {
                        try
                        {
                            double percentOfVolumn = GetTotalTransportVolumnOfDeliveryRequest(
                                deliveryRequest
                            );
                            //if (
                            //    deliveryRequest.DeliveryType
                            //    == DeliveryType.DONATED_REQUEST_TO_BRANCH
                            //)
                            //{
                            //    percentOfVolumn = deliveryRequest.DeliveryItems
                            //        .Select(
                            //            di =>
                            //                di.Quantity
                            //                / di.DonatedItem!.Item.MaximumTransportVolume
                            //        )
                            //        .Sum();
                            //}
                            //else if (
                            //    deliveryRequest.DeliveryType == DeliveryType.BRANCH_TO_AID_REQUEST
                            //)
                            //{
                            //    percentOfVolumn = deliveryRequest.DeliveryItems
                            //        .Select(
                            //            di => di.Quantity / di.AidItem!.Item.MaximumTransportVolume
                            //        )
                            //        .Sum();
                            //}
                            //else { }

                            if (totalPercentOfVolumn + percentOfVolumn <= maxSum)
                            {
                                currentList.Add(deliveryRequest);
                                totalPercentOfVolumn += percentOfVolumn;
                            }
                            else
                            {
                                result.Add(currentList);
                                currentList = new List<DeliveryRequest> { deliveryRequest };
                                totalPercentOfVolumn = percentOfVolumn;
                            }
                        }
                        catch { }
                    }

                    if (currentList.Count > 0)
                    {
                        result.Add(currentList);
                    }
                }

                foreach (List<DeliveryRequest> deliveryRequests in result)
                {
                    double totalPercentOfVolumn = 0;
                    foreach (DeliveryRequest deliveryRequest in deliveryRequests)
                    {
                        //double percentOfVolumn = 0;
                        //if (deliveryRequest.DeliveryType == DeliveryType.DONATED_REQUEST_TO_BRANCH)
                        //{
                        //    percentOfVolumn = deliveryRequest.DeliveryItems
                        //        .Select(
                        //            di => di.Quantity / di.DonatedItem!.Item.MaximumTransportVolume
                        //        )
                        //        .Sum();
                        //}
                        //else if (deliveryRequest.DeliveryType == DeliveryType.BRANCH_TO_AID_REQUEST)
                        //{
                        //    percentOfVolumn = deliveryRequest.DeliveryItems
                        //        .Select(di => di.Quantity / di.AidItem!.Item.MaximumTransportVolume)
                        //        .Sum();
                        //}
                        //else { }

                        totalPercentOfVolumn += GetTotalTransportVolumnOfDeliveryRequest(
                            deliveryRequest
                        );
                    }

                    finalResult.Add(
                        new GroupOfDeliveryRequestWithSamePartOfScheduledTime
                        {
                            DeliveryRequests = deliveryRequests,
                            ScheduledTime =
                                groupOfDeliveryRequestWithSamePartOfScheduledTime.ScheduledTime,
                            //NumberOfVehicles = (int)
                            //    Math.Ceiling(
                            //        totalPercentOfVolumn
                            //            * 100
                            //            / int.Parse(
                            //                _config["ScheduledRoute:MaxVolumnPercentToSchedule"]
                            //            )
                            //    )
                            NumberOfVehicles = 3
                        }
                    );
                }
            }

            return finalResult;
        }

        private double GetTotalTransportVolumnOfDeliveryRequest(DeliveryRequest deliveryRequest)
        {
            if (deliveryRequest.DonatedRequestId != null)
            {
                return deliveryRequest.DeliveryItems
                    .Select(di => di.Quantity / di.DonatedItem!.Item.MaximumTransportVolume)
                    .Sum();
            }
            else
            {
                return deliveryRequest.DeliveryItems
                    .Select(di => di.Quantity / di.AidItem!.Item.MaximumTransportVolume)
                    .Sum();
            }
        }

        private async Task<
            List<GroupOfDeliveryRequestWithSamePartOfScheduledTime>
        > GroupListOfDeliveryRequestsByScheduleTime(
            List<List<DeliveryRequest>> inputListsOfDeliveryRequests
        )
        {
            List<DeliveryRequest> outDateDeliveryRequests = new List<DeliveryRequest>();
            List<GroupOfDeliveryRequestWithSamePartOfScheduledTime> groupOfDeliveryRequestWithSamePartOfScheduledTimes =
                new List<GroupOfDeliveryRequestWithSamePartOfScheduledTime>();

            List<List<DeliveryRequest>> listsOfDeliveryRequests = new List<List<DeliveryRequest>>();
            foreach (List<DeliveryRequest> inputDeliveryRequests in inputListsOfDeliveryRequests)
            {
                listsOfDeliveryRequests.Add(
                    inputDeliveryRequests
                        .Where(dr => GetPeriodOfFirstAvailabeScheduledTime(dr) != null)
                        .OrderBy(dr => GetPeriodOfFirstAvailabeScheduledTime(dr))
                        .ToList()
                );

                outDateDeliveryRequests.AddRange(
                    inputDeliveryRequests
                        .Where(dr => GetPeriodOfFirstAvailabeScheduledTime(dr) == null)
                        .OrderBy(dr => GetPeriodOfFirstAvailabeScheduledTime(dr))
                        .ToList()
                );
            }

            //listsOfDeliveryRequests.ForEach(
            //    drs => drs = drs.OrderBy(dr => GetPeriodOfFirstAvailabeScheduledTime(dr)).ToList()
            //);

            foreach (List<DeliveryRequest> deliveryRequests in listsOfDeliveryRequests)
            {
                List<GroupOfDeliveryRequestWithSamePartOfScheduledTime> partialGroupOfDeliveryRequestWithSamePartOfScheduledTimes =
                    new List<GroupOfDeliveryRequestWithSamePartOfScheduledTime>();
                //List<List<DeliveryRequest>> resultChild = new List<List<DeliveryRequest>>();
                foreach (DeliveryRequest deliveryRequest in deliveryRequests)
                {
                    ScheduledTime? scheduledTime = GetFirstAvailabeScheduledTime(
                        JsonConvert.DeserializeObject<List<ScheduledTime>>(
                            deliveryRequest.ScheduledTimes
                        )!
                    );
                    if (scheduledTime == null)
                    {
                        //deliveryRequest.Status =
                        deliveryRequest.CurrentScheduledTime = null;
                        outDateDeliveryRequests.Add(deliveryRequest);
                    }
                    else
                    {
                        if (
                            deliveryRequest.CurrentScheduledTime != null
                            && scheduledTime.Equals(
                                JsonConvert.DeserializeObject<ScheduledTime>(
                                    deliveryRequest.CurrentScheduledTime
                                )!
                            )
                        )
                        {
                            continue;
                        }
                        ScheduledTime? newScheduledTimeIfAdded = null;
                        foreach (
                            GroupOfDeliveryRequestWithSamePartOfScheduledTime groupOfDeliveryRequestWithSamePartOfScheduledTime in partialGroupOfDeliveryRequestWithSamePartOfScheduledTimes
                        )
                        {
                            newScheduledTimeIfAdded = GetSameScheduledTimeIfAddedToGroup(
                                groupOfDeliveryRequestWithSamePartOfScheduledTime,
                                scheduledTime
                            );
                            if (newScheduledTimeIfAdded != null)
                            {
                                groupOfDeliveryRequestWithSamePartOfScheduledTime.ScheduledTime =
                                    newScheduledTimeIfAdded;
                                groupOfDeliveryRequestWithSamePartOfScheduledTime.DeliveryRequests.Add(
                                    deliveryRequest
                                );
                                break;
                            }
                        }
                        if (newScheduledTimeIfAdded == null)
                        {
                            GroupOfDeliveryRequestWithSamePartOfScheduledTime groupOfDeliveryRequestWithSamePartOfScheduledTime =
                                new GroupOfDeliveryRequestWithSamePartOfScheduledTime
                                {
                                    ScheduledTime = scheduledTime,
                                    DeliveryRequests = new List<DeliveryRequest> { deliveryRequest }
                                };
                            partialGroupOfDeliveryRequestWithSamePartOfScheduledTimes.Add(
                                groupOfDeliveryRequestWithSamePartOfScheduledTime
                            );
                        }
                    }
                }
                groupOfDeliveryRequestWithSamePartOfScheduledTimes.AddRange(
                    partialGroupOfDeliveryRequestWithSamePartOfScheduledTimes
                );
            }

            if (outDateDeliveryRequests.Count > 0)
                await UpdateOutDateDeliveryRequests(outDateDeliveryRequests);

            return groupOfDeliveryRequestWithSamePartOfScheduledTimes;
        }

        private double? GetPeriodOfFirstAvailabeScheduledTime(DeliveryRequest deliveryRequest)
        {
            ScheduledTime? firstAvailabeScheduledTime = GetFirstAvailabeScheduledTime(
                JsonConvert.DeserializeObject<List<ScheduledTime>>(deliveryRequest.ScheduledTimes)!
            );

            if (firstAvailabeScheduledTime == null)
                return null;

            double rs = (
                GetEndDateTimeFromScheduledTime(firstAvailabeScheduledTime)
                - GetStartDateTimeFromScheduledTime(firstAvailabeScheduledTime)
            ).TotalHours;

            return rs;
        }

        private double? GetPeriodOfCurrentScheduledTime(DeliveryRequest deliveryRequest)
        {
            double rs = (
                GetEndDateTimeFromScheduledTime(
                    JsonConvert.DeserializeObject<ScheduledTime>(
                        deliveryRequest.CurrentScheduledTime!
                    )!
                )
                - GetStartDateTimeFromScheduledTime(
                    JsonConvert.DeserializeObject<ScheduledTime>(
                        deliveryRequest.CurrentScheduledTime!
                    )!
                )
            ).TotalHours;

            return rs;
        }

        private ScheduledTime? GetFirstAvailabeScheduledTime(List<ScheduledTime> scheduledTimes)
        {
            return scheduledTimes
                .Where(
                    st =>
                        GetEndDateTimeFromScheduledTime(st)
                        > SettedUpDateTime.GetCurrentVietNamTime()
                )
                .MinBy(st => GetStartDateTimeFromScheduledTime(st));
        }

        private ScheduledTime? GetSameScheduledTimeIfAddedToGroup(
            GroupOfDeliveryRequestWithSamePartOfScheduledTime groupOfDeliveryRequestWithSamePartOfScheduledTime,
            ScheduledTime scheduledTime
        )
        {
            DateOnly addedDay = DateOnly.Parse(scheduledTime.Day);
            TimeOnly addedStartTime = TimeOnly.Parse(scheduledTime.StartTime);
            TimeOnly addedEndTime = TimeOnly.Parse(scheduledTime.EndTime);
            DateTime addedStartDate = addedDay.ToDateTime(addedStartTime);
            DateTime addedEndDate = addedDay.ToDateTime(addedEndTime);

            DateOnly groupDay = DateOnly.Parse(
                groupOfDeliveryRequestWithSamePartOfScheduledTime.ScheduledTime.Day
            );
            TimeOnly groupStartTime = TimeOnly.Parse(
                groupOfDeliveryRequestWithSamePartOfScheduledTime.ScheduledTime.StartTime
            );
            TimeOnly groupEndTime = TimeOnly.Parse(
                groupOfDeliveryRequestWithSamePartOfScheduledTime.ScheduledTime.EndTime
            );

            DateTime groupStartDate = groupDay.ToDateTime(groupStartTime);
            DateTime groupEndDate = groupDay.ToDateTime(groupEndTime);

            DateTime? overlapStart =
                addedStartDate > groupStartDate ? addedStartDate : groupStartDate;
            DateTime? overlapEnd = addedEndDate < groupEndDate ? addedEndDate : groupEndDate;

            if (overlapStart < overlapEnd)
            {
                if (
                    overlapEnd - overlapStart
                    >= TimeSpan.FromHours(
                        int.Parse(_config["ScheduledRoute:MinHoursToAllowAddedToScheduledRoute"])
                    )
                )
                    return new ScheduledTime
                    {
                        Day = DateOnly.FromDateTime((DateTime)overlapStart).ToString(),
                        StartTime = TimeOnly.FromDateTime((DateTime)overlapStart).ToString("HH:mm"),
                        EndTime = TimeOnly.FromDateTime((DateTime)overlapEnd).ToString("HH:mm")
                    };
                else
                    return null;
            }
            else
                return null;
        }

        private DateTime GetSameStartTimeFromDeliveryRequests(
            List<DeliveryRequest> deliveryRequests
        )
        {
            DateTime startTime = GetStartDateTimeFromScheduledTime(
                GetFirstAvailabeScheduledTime(
                    JsonConvert.DeserializeObject<List<ScheduledTime>>(
                        deliveryRequests[0].ScheduledTimes
                    )!
                )!
            );

            foreach (DeliveryRequest deliveryRequest in deliveryRequests)
            {
                ScheduledTime scheduledTime = GetFirstAvailabeScheduledTime(
                    JsonConvert.DeserializeObject<List<ScheduledTime>>(
                        deliveryRequest.ScheduledTimes
                    )!
                )!;

                DateOnly addedDay = DateOnly.Parse(scheduledTime.Day);
                TimeOnly addedStartTime = TimeOnly.Parse(scheduledTime.StartTime);
                DateTime addedStartDate = addedDay.ToDateTime(addedStartTime);

                startTime = addedStartDate > startTime ? addedStartDate : startTime;
            }

            return startTime;
        }

        private DateTime GetStartDateTimeFromScheduledTime(ScheduledTime scheduledTime)
        {
            DateOnly day = DateOnly.Parse(scheduledTime.Day);
            TimeOnly startTime = TimeOnly.Parse(scheduledTime.StartTime);
            return day.ToDateTime(startTime);
        }

        private DateTime GetEndDateTimeFromScheduledTime(ScheduledTime scheduledTime)
        {
            DateOnly day = DateOnly.Parse(scheduledTime.Day);
            TimeOnly endTime = TimeOnly.Parse(scheduledTime.EndTime);
            return day.ToDateTime(endTime);
        }

        public async Task<int> UpdateOutDateDeliveryRequests(List<DeliveryRequest> deliveryRequests)
        {
            foreach (DeliveryRequest deliveryRequest in deliveryRequests)
            {
                deliveryRequest.Status = DeliveryRequestStatus.EXPIRED;
                List<ScheduledRouteDeliveryRequest> scheduledRouteDeliveryRequests =
                    deliveryRequest.ScheduledRouteDeliveryRequests
                        .Where(srdr => srdr.Status == ScheduledRouteDeliveryRequestStatus.SCHEDULED)
                        .ToList();

                scheduledRouteDeliveryRequests.ForEach(
                    srdr => srdr.Status = ScheduledRouteDeliveryRequestStatus.CANCELED
                );

                await _scheduledRouteDeliveryRequestRepository.UpdateScheduledRouteDeliveryRequestsAsync(
                    scheduledRouteDeliveryRequests
                );

                Notification notification = new Notification
                {
                    Name = _config[
                        "ResponseMessages:NotificationMsg:NotificationTitleForExpiredDeliveryRequestsMsg"
                    ],
                    CreatedDate = SettedUpDateTime.GetCurrentVietNamTime(),
                    Image = _config["Notification:Image"],
                    Content = _config[
                        "ResponseMessages:NotificationMsg:NotificationContentForExpiredDeliveryRequestsMsg"
                    ],
                    ReceiverId = GetMainBranch(deliveryRequest).BranchAdminId!.ToString(),
                    Status = NotificationStatus.NEW,
                    Type = NotificationType.NOTIFYING,
                    DataType = DataNotificationType.DELIVERY_REQUEST,
                    DataId = deliveryRequest.Id
                };
                await _notificationRepository.CreateNotificationAsync(notification);
                await _hubContext.Clients.All.SendAsync(
                    GetMainBranch(deliveryRequest).BranchAdminId!.ToString(),
                    notification
                );
            }

            return await _deliveryRequestRepository.UpdateDeliveryRequestsAsync(deliveryRequests);
        }

        public async Task<int> UpdateCurrentScheduledTimeOfDeliveryRequests(
            List<DeliveryRequest> deliveryRequests
        )
        {
            foreach (DeliveryRequest deliveryRequest in deliveryRequests)
            {
                List<ScheduledRouteDeliveryRequest> scheduledRouteDeliveryRequests =
                    deliveryRequest.ScheduledRouteDeliveryRequests
                        .Where(srdr => srdr.Status == ScheduledRouteDeliveryRequestStatus.SCHEDULED)
                        .ToList();

                scheduledRouteDeliveryRequests.ForEach(
                    srdr => srdr.Status = ScheduledRouteDeliveryRequestStatus.CANCELED
                );
                await _scheduledRouteDeliveryRequestRepository.UpdateScheduledRouteDeliveryRequestsAsync(
                    scheduledRouteDeliveryRequests
                );
            }

            return await _deliveryRequestRepository.UpdateDeliveryRequestsAsync(deliveryRequests);
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

        private ScheduledTime? GetLastScheduledTime(List<ScheduledTime> scheduledTimes)
        {
            return scheduledTimes.MaxBy(st => GetEndDateTimeFromScheduledTime(st));
        }

        private List<long> GetTimeWindowByScheduledTime(ScheduledTime scheduledTime)
        {
            //DateOnly addedDay = DateOnly.Parse(scheduledTime.Day);
            //TimeOnly addedStartTime = TimeOnly.Parse(scheduledTime.StartTime);
            //TimeOnly addedEndTime = TimeOnly.Parse(scheduledTime.EndTime);
            //DateTime addedStartDate = addedDay.ToDateTime(addedStartTime);

            //double speedFactor = double.Parse(_config["OpenRoute:SpeedFactor"]);
            //DateTime sampleEndDate = addedDay.ToDateTime(addedEndTime);
            //DateTime addedEndDate = addedStartDate.AddHours(
            //    (sampleEndDate - addedStartDate).TotalHours * speedFactor
            //);

            //return new List<long>
            //{
            //    (long)
            //        (
            //            addedStartDate - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            //        ).TotalSeconds,
            //    (long)
            //        (
            //            addedEndDate - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            //        ).TotalSeconds
            //};

            DateOnly scheduledDay = DateOnly.Parse(scheduledTime.Day);
            TimeOnly scheduledStartTime = TimeOnly.Parse(scheduledTime.StartTime);
            TimeOnly scheduledEndTime = scheduledStartTime.AddHours(
                double.Parse(_config["OpenRoute:MaxHoursForARoute"])
            );
            DateTime scheduledStartDate = scheduledDay.ToDateTime(scheduledStartTime);

            double speedFactor = double.Parse(_config["OpenRoute:SpeedFactor"]);
            DateTime sampleEndDate = scheduledDay.ToDateTime(scheduledEndTime);
            DateTime addedEndDate = scheduledStartDate.AddHours(
                (sampleEndDate - scheduledStartDate).TotalHours * speedFactor * 2
            );

            return new List<long>
            {
                (long)
                    (
                        scheduledStartDate - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                    ).TotalSeconds,
                (long)
                    (
                        addedEndDate - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                    ).TotalSeconds
            };
        }

        private async Task CheckToCreateScheduledRoute(
            OptimizeResponse optimizeResponse,
            Dictionary<int, DeliveryRequest> deliveryRequestIdPairs
        )
        {
            List<GroupOfDeliveryRequestOfRouteWithTimeAndDistance> groupOfDeliveryRequestsWithTimeAndDistances =
                new List<GroupOfDeliveryRequestOfRouteWithTimeAndDistance>();
            double speedFactor = double.Parse(_config["OpenRoute:SpeedFactor"]);

            if (optimizeResponse.Unassigned.Count > 0)
            {
                List<int> ids = optimizeResponse.Unassigned
                    .Select(u => u.Id)
                    .DistinctBy(u => u)
                    .ToList();
                await UpdateScheduledRoutesForUnassignedDeliveryRequest(
                    ids.Select(key => deliveryRequestIdPairs[key]).ToList()
                );
            }

            foreach (Route route in optimizeResponse.Routes)
            {
                Step step = route.Steps.FirstOrDefault(s => s.Type.Equals("pickup"))!;
                DeliveryRequest deliveryRequestType = deliveryRequestIdPairs[step.Id];
                if (deliveryRequestType.AidRequestId != null)
                    groupOfDeliveryRequestsWithTimeAndDistances.AddRange(
                        GetAidRequestRoutes(route.Steps, deliveryRequestIdPairs)
                    );
                else
                {
                    groupOfDeliveryRequestsWithTimeAndDistances.AddRange(
                        GetDonatedRequestRoutes(route.Steps, deliveryRequestIdPairs)
                    );
                }
            }

            double maxLimitVolumn = double.Parse(
                _config["ScheduledRoute:MaxVolumnPercentToSchedule"]
            );
            double minLimitVolumn = double.Parse(
                _config["ScheduledRoute:MinVolumnPercentToSchedule"]
            );

            foreach (
                GroupOfDeliveryRequestOfRouteWithTimeAndDistance groupOfDeliveryRequestOfRouteWithTimeAndDistance in groupOfDeliveryRequestsWithTimeAndDistances
            )
            {
                try
                {
                    List<DeliveryRequest> updatedDeliveryRequests = new List<DeliveryRequest>();

                    double totalVolumn =
                        groupOfDeliveryRequestOfRouteWithTimeAndDistance.DeliveryRequests
                            .Select(dr => GetTotalTransportVolumnOfDeliveryRequest(dr))
                            .Sum() * 100;

                    List<ScheduledTime> scheduledTimes = new List<ScheduledTime>();
                    groupOfDeliveryRequestOfRouteWithTimeAndDistance.DeliveryRequests.ForEach(
                        dr =>
                            scheduledTimes.AddRange(
                                JsonConvert.DeserializeObject<List<ScheduledTime>>(
                                    dr.ScheduledTimes
                                )!
                            )
                    );

                    ScheduledTime lastScheduledTime = GetLastAvailabeScheduledTime(scheduledTimes)!;

                    if (
                        totalVolumn <= maxLimitVolumn && totalVolumn >= minLimitVolumn
                        || DateOnly.Parse(lastScheduledTime.Day)
                            <= DateOnly.FromDateTime(
                                SettedUpDateTime
                                    .GetCurrentVietNamTime()
                                    .AddDays(
                                        double.Parse(
                                            _config["ScheduledRoute:UrgencyDaysToSchedule"]
                                        )
                                    )
                            )
                            && DateOnly.Parse(lastScheduledTime.Day)
                                >= DateOnly.FromDateTime(SettedUpDateTime.GetCurrentVietNamTime())
                        || GetDeliveryTypeOfDeliveryRequest(
                            groupOfDeliveryRequestOfRouteWithTimeAndDistance.DeliveryRequests[0]
                        ) == DeliveryType.BRANCH_TO_AID_REQUEST
                    )
                    {
                        ScheduledRoute scheduledRoute = new ScheduledRoute
                        {
                            Status = ScheduledRouteStatus.PENDING,
                            CreatedDate = SettedUpDateTime.GetCurrentVietNamTime(),
                            StartDate = GetSameStartTimeFromDeliveryRequests(
                                groupOfDeliveryRequestOfRouteWithTimeAndDistance.DeliveryRequests
                            ),
                        };

                        await _scheduledRouteRepository.AddScheduledRouteAsync(scheduledRoute);

                        List<ScheduledRouteDeliveryRequest> scheduledRouteDeliveryRequests =
                            new List<ScheduledRouteDeliveryRequest>();

                        for (
                            int i = 0;
                            i
                                < groupOfDeliveryRequestOfRouteWithTimeAndDistance
                                    .DeliveryRequests
                                    .Count;
                            i++
                        )
                        {
                            ScheduledTime? scheduledTime = GetFirstAvailabeScheduledTime(
                                JsonConvert.DeserializeObject<List<ScheduledTime>>(
                                    groupOfDeliveryRequestOfRouteWithTimeAndDistance.DeliveryRequests[
                                        i
                                    ].ScheduledTimes
                                )!
                            );
                            groupOfDeliveryRequestOfRouteWithTimeAndDistance.DeliveryRequests[
                                i
                            ].CurrentScheduledTime = JsonConvert.SerializeObject(scheduledTime);
                            updatedDeliveryRequests.Add(
                                groupOfDeliveryRequestOfRouteWithTimeAndDistance.DeliveryRequests[i]
                            );

                            scheduledRouteDeliveryRequests.Add(
                                new ScheduledRouteDeliveryRequest
                                {
                                    ScheduledRouteId = scheduledRoute.Id,
                                    DeliveryRequestId =
                                        groupOfDeliveryRequestOfRouteWithTimeAndDistance.DeliveryRequests[
                                            i
                                        ].Id,
                                    Status = ScheduledRouteDeliveryRequestStatus.SCHEDULED,
                                    Order = i + 1,
                                    TimeToReachThisOrNextAsSeconds =
                                        groupOfDeliveryRequestOfRouteWithTimeAndDistance.TimesAsSeconds[
                                            i
                                        ] / speedFactor,
                                    DistanceToReachThisOrNextAsMeters =
                                        groupOfDeliveryRequestOfRouteWithTimeAndDistance.DistancesAsMeters[
                                            i
                                        ]
                                }
                            );
                        }

                        await UpdateCurrentScheduledTimeOfDeliveryRequests(updatedDeliveryRequests);

                        await _scheduledRouteDeliveryRequestRepository.AddScheduledRouteDeliveryRequestsAsync(
                            scheduledRouteDeliveryRequests
                        );
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        $"An exception occurred in service {nameof(ScheduledRouteService)}, method {nameof(CheckToCreateScheduledRoute)}."
                    );
                }
            }
        }

        private List<GroupOfDeliveryRequestOfRouteWithTimeAndDistance> GetDonatedRequestRoutes(
            List<Step> inputSteps,
            Dictionary<int, DeliveryRequest> deliveryRequestIdPairs
        )
        {
            List<GroupOfDeliveryRequestOfRouteWithTimeAndDistance> groupOfDeliveryRequestOfRouteWithTimeAndDistances =
                new List<GroupOfDeliveryRequestOfRouteWithTimeAndDistance>();
            List<List<Step>> groupOfBaseSteps = new List<List<Step>>();
            double speedAsMetersPerSecond = double.Parse(
                _config["OpenRoute:EstimatedSpeedAsMetersPerSencond"]
            );
            foreach (Step step in inputSteps)
            {
                if (step.Type.Equals("pickup"))
                {
                    if (groupOfBaseSteps.LastOrDefault() == null)
                    {
                        groupOfBaseSteps.Add(new List<Step>());
                    }
                    groupOfBaseSteps.LastOrDefault()!.Add(step);
                }
                else if (step.Type.Equals("delivery"))
                {
                    if (groupOfBaseSteps.LastOrDefault() == null)
                    {
                        groupOfBaseSteps.Add(new List<Step>());
                    }
                    if (groupOfBaseSteps.LastOrDefault()!.Count > 0)
                    {
                        groupOfBaseSteps.LastOrDefault()!.Add(step);
                        groupOfBaseSteps.Add(new List<Step>());
                    }
                }
            }

            groupOfBaseSteps = groupOfBaseSteps.Where(g => g.Count > 0).ToList();

            foreach (List<Step> steps in groupOfBaseSteps)
            {
                GroupOfDeliveryRequestOfRouteWithTimeAndDistance groupOfDeliveryRequestOfRouteWithTimeAndDistance =
                    new GroupOfDeliveryRequestOfRouteWithTimeAndDistance
                    {
                        DeliveryRequests = new List<DeliveryRequest>(),
                        DistancesAsMeters = new List<double>(),
                        TimesAsSeconds = new List<double>()
                    };
                for (int i = 0; i < steps.Count; i++)
                {
                    if (steps[i].Type.Equals("pickup"))
                    {
                        groupOfDeliveryRequestOfRouteWithTimeAndDistance.DeliveryRequests.Add(
                            deliveryRequestIdPairs[steps[i].Id]
                        );
                        groupOfDeliveryRequestOfRouteWithTimeAndDistance.TimesAsSeconds.Add(
                            steps[i + 1].Arrival - steps[i].Arrival
                        );
                        groupOfDeliveryRequestOfRouteWithTimeAndDistance.DistancesAsMeters.Add(
                            speedAsMetersPerSecond * (steps[i + 1].Duration - steps[i].Duration)
                        );
                    }
                }
                groupOfDeliveryRequestOfRouteWithTimeAndDistances.Add(
                    groupOfDeliveryRequestOfRouteWithTimeAndDistance
                );
            }

            return groupOfDeliveryRequestOfRouteWithTimeAndDistances;
        }

        private List<GroupOfDeliveryRequestOfRouteWithTimeAndDistance> GetAidRequestRoutes(
            List<Step> inputSteps,
            Dictionary<int, DeliveryRequest> deliveryRequestIdPairs
        )
        {
            List<GroupOfDeliveryRequestOfRouteWithTimeAndDistance> groupOfDeliveryRequestOfRouteWithTimeAndDistances =
                new List<GroupOfDeliveryRequestOfRouteWithTimeAndDistance>();
            List<List<Step>> groupOfBaseSteps = new List<List<Step>>();
            double speedAsMetersPerSecond = double.Parse(
                _config["OpenRoute:EstimatedSpeedAsMetersPerSencond"]
            );
            foreach (Step step in inputSteps)
            {
                if (step.Type.Equals("pickup"))
                {
                    if (groupOfBaseSteps.LastOrDefault() == null)
                    {
                        groupOfBaseSteps.Add(new List<Step>());
                    }
                    if (groupOfBaseSteps.LastOrDefault()!.Count == 0)
                    {
                        groupOfBaseSteps.LastOrDefault()!.Add(step);
                    }
                    else if (groupOfBaseSteps.LastOrDefault()!.Any(s => s.Type.Equals("delivery")))
                    {
                        groupOfBaseSteps.Add(new List<Step>());
                        groupOfBaseSteps.LastOrDefault()!.Add(step);
                    }
                }
                else if (step.Type.Equals("delivery"))
                {
                    if (groupOfBaseSteps.LastOrDefault() == null)
                    {
                        groupOfBaseSteps.Add(new List<Step>());
                    }
                    groupOfBaseSteps.LastOrDefault()!.Add(step);
                }
            }

            groupOfBaseSteps = groupOfBaseSteps.Where(g => g.Count > 0).ToList();

            foreach (List<Step> steps in groupOfBaseSteps)
            {
                GroupOfDeliveryRequestOfRouteWithTimeAndDistance groupOfDeliveryRequestOfRouteWithTimeAndDistance =
                    new GroupOfDeliveryRequestOfRouteWithTimeAndDistance
                    {
                        DeliveryRequests = new List<DeliveryRequest>(),
                        DistancesAsMeters = new List<double>(),
                        TimesAsSeconds = new List<double>()
                    };
                for (int i = 0; i < steps.Count; i++)
                {
                    if (steps[i].Type.Equals("delivery"))
                    {
                        groupOfDeliveryRequestOfRouteWithTimeAndDistance.DeliveryRequests.Add(
                            deliveryRequestIdPairs[steps[i].Id]
                        );
                        groupOfDeliveryRequestOfRouteWithTimeAndDistance.TimesAsSeconds.Add(
                            steps[i].Arrival - steps[i - 1].Arrival
                        );
                        groupOfDeliveryRequestOfRouteWithTimeAndDistance.DistancesAsMeters.Add(
                            speedAsMetersPerSecond * (steps[i].Duration - steps[i - 1].Duration)
                        );
                    }
                }
                groupOfDeliveryRequestOfRouteWithTimeAndDistances.Add(
                    groupOfDeliveryRequestOfRouteWithTimeAndDistance
                );
            }

            return groupOfDeliveryRequestOfRouteWithTimeAndDistances;
        }

        public async Task<CommonResponse> AcceptScheduledRouteAsync(
            Guid userId,
            ScheduledRouteAcceptingRequest scheduledRouteAcceptingRequest
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

                ScheduledRoute? scheduledRoute =
                    await _scheduledRouteRepository.FindPendingScheduledRouteByIdAsync(
                        scheduledRouteAcceptingRequest.ScheduledRouteId
                    );

                if (scheduledRoute == null)
                    return new CommonResponse
                    {
                        Status = 400,
                        Message = _config[
                            "ResponseMessages:ScheduledRouteMsg:PendingScheduledRouteNotFoundMsg"
                        ]
                    };

                ScheduledTime? pendingScheduledTime = GetAvailableScheduledTimeOfScheduledRoute(
                    scheduledRoute
                );

                if (pendingScheduledTime == null)
                    return new CommonResponse
                    {
                        Status = 400,
                        Message = _config[
                            "ResponseMessages:ScheduledRouteMsg:ScheduledRouteIsExpiredMsg"
                        ]
                    };

                List<ScheduledRoute> acceptedScheduledRoutes =
                    await _scheduledRouteRepository.FindAcceptedAndProcessingScheduledRoutesByContributorIdAsync(
                        userId
                    );

                List<ScheduledTime> scheduledTimes = new List<ScheduledTime>();

                foreach (ScheduledRoute acceptedScheduledRoute in acceptedScheduledRoutes)
                {
                    ScheduledTime? tmp = GetAvailableScheduledTimeOfScheduledRoute(
                        acceptedScheduledRoute
                    );
                    if (tmp != null)
                        scheduledTimes.Add(tmp);
                }

                foreach (ScheduledTime scheduledTime in scheduledTimes)
                {
                    if (IsTwoScheduledTimeOverlap(pendingScheduledTime, scheduledTime))
                        return new CommonResponse
                        {
                            Status = 400,
                            Message = _config[
                                "ResponseMessages:ScheduledRouteMsg:ExistOverlapScheduledRoute"
                            ]
                        };
                }

                Branch? branch = await _branchRepository.FindBranchByIdAsync(
                    GetMainBranchId(
                        scheduledRoute.ScheduledRouteDeliveryRequests[0].DeliveryRequest
                    )
                );

                string location =
                    scheduledRouteAcceptingRequest.Latitude != null
                    && scheduledRouteAcceptingRequest.Longitude != null
                        ? $"{scheduledRouteAcceptingRequest.Latitude},{scheduledRouteAcceptingRequest.Longitude}"
                        : user.Location!;

                double maxRoadLengthAsMetersForCollaborator = double.Parse(
                    _config["OpenRoute:MaxRoadLengthAsMetersForCollaborator"]
                );

                if (
                    (
                        await _openRouteService.GetDeliverableBranchesByUserLocation(
                            location,
                            new List<Branch> { branch! },
                            maxRoadLengthAsMetersForCollaborator
                        )
                    ).NearestBranch == null
                )
                    return new CommonResponse
                    {
                        Status = 400,
                        Message =
                            _config[
                                "ResponseMessages:ScheduledRouteMsg:BranchTooFarToAcceptScheduledRouteMsg"
                            ] + $"({maxRoadLengthAsMetersForCollaborator / 1000} km)"
                    };

                scheduledRoute.UserId = userId;
                scheduledRoute.Status = ScheduledRouteStatus.ACCEPTED;
                scheduledRoute.AcceptedDate = SettedUpDateTime.GetCurrentVietNamTime();

                scheduledRoute.ScheduledRouteDeliveryRequests.ForEach(
                    srdr => srdr.DeliveryRequest.Status = DeliveryRequestStatus.ACCEPTED
                );

                using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
                {
                    if (
                        await _scheduledRouteRepository.UpdateScheduledRouteAsync(scheduledRoute)
                        != 1
                    )
                        return new CommonResponse
                        {
                            Status = 500,
                            Message = _config["ResponseMessages:CommonMsg:InternalServerErrorMsg"]
                        };

                    if (
                        await _deliveryRequestRepository.UpdateDeliveryRequestsAsync(
                            scheduledRoute.ScheduledRouteDeliveryRequests
                                .Select(srdr => srdr.DeliveryRequest)
                                .ToList()
                        )
                        != scheduledRoute.ScheduledRouteDeliveryRequests
                            .Select(srdr => srdr.DeliveryRequest)
                            .Count()
                    )
                        return new CommonResponse
                        {
                            Status = 500,
                            Message = _config["ResponseMessages:CommonMsg:InternalServerErrorMsg"]
                        };

                    //List<CollaboratorApplication>? collaboratorsList =
                    //    await _collaboratorRepository.FindCollaboratorActiveAsync();
                    //if (collaboratorsList != null && collaboratorsList.Count > 0)
                    //{
                    //foreach (var c in collaboratorsList)
                    //{
                    //    var data = new
                    //    {
                    //        scheduledRouteAcceptingRequest.ScheduledRouteId
                    //    };

                    //    await _scheduleHubContext.Clients.All.SendAsync(
                    //        c.UserId.ToString(),
                    //        data
                    //    );
                    //  }
                    var data = new { scheduledRouteAcceptingRequest.ScheduledRouteId };
                    await _scheduleHubContext.Clients.All.SendAsync("schedule-route", data);
                    // }

                    scope.Complete();
                    return new CommonResponse
                    {
                        Status = 200,
                        Message = _config[
                            "ResponseMessages:ScheduledRouteMsg:AcceptScheduledRouteSuccess"
                        ]
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    $"An exception occurred in service {nameof(ScheduledRouteService)}, method {nameof(AcceptScheduledRouteAsync)}."
                );
                return new CommonResponse
                {
                    Status = 500,
                    Message = _config["ResponseMessages:CommonMsg:InternalServerErrorMsg"]
                };
            }
        }

        private bool IsTwoScheduledTimeOverlap(
            ScheduledTime scheduledTime1,
            ScheduledTime scheduledTime2
        )
        {
            DateOnly day1 = DateOnly.Parse(scheduledTime1.Day);
            TimeOnly startTime1 = TimeOnly.Parse(scheduledTime1.StartTime);
            TimeOnly endTime1 = TimeOnly.Parse(scheduledTime1.EndTime);
            DateTime startDate1 = day1.ToDateTime(startTime1);
            DateTime endDate1 = day1.ToDateTime(endTime1);

            DateOnly day2 = DateOnly.Parse(scheduledTime2.Day);
            TimeOnly startTime2 = TimeOnly.Parse(scheduledTime2.StartTime);
            TimeOnly endTime2 = TimeOnly.Parse(scheduledTime2.EndTime);
            DateTime startDate2 = day2.ToDateTime(startTime2);
            DateTime endDate2 = day2.ToDateTime(endTime2);

            DateTime? overlapStart = startDate1 > startDate2 ? startDate1 : startDate2;
            DateTime? overlapEnd = endDate1 < endDate2 ? endDate1 : endDate2;

            return overlapStart < overlapEnd;
        }

        private bool IsTwoDateTimePeriodOverlap(
            DateTime startDate1,
            DateTime endDate1,
            DateTime? startDate2,
            DateTime? endDate2
        )
        {
            DateTime? overlapStart = startDate1 > startDate2 ? startDate1 : startDate2;
            DateTime? overlapEnd = endDate1 < endDate2 ? endDate1 : endDate2;

            return overlapStart < overlapEnd;
        }

        private ScheduledTime? GetAvailableScheduledTimeOfScheduledRoute(
            ScheduledRoute scheduledRoute
        )
        {
            GroupOfDeliveryRequestWithSamePartOfScheduledTime tmp =
                new GroupOfDeliveryRequestWithSamePartOfScheduledTime();

            List<DeliveryRequest> deliveryRequests = scheduledRoute.ScheduledRouteDeliveryRequests
                .Where(
                    srdr =>
                        srdr.Status == ScheduledRouteDeliveryRequestStatus.SCHEDULED
                        && GetPeriodOfFirstAvailabeScheduledTime(srdr.DeliveryRequest) != null
                )
                .Select(srdr => srdr.DeliveryRequest)
                .OrderBy(dr => GetPeriodOfFirstAvailabeScheduledTime(dr))
                .ToList();

            foreach (DeliveryRequest deliveryRequest in deliveryRequests)
            {
                if (tmp.ScheduledTime == null)
                {
                    tmp.ScheduledTime = JsonConvert.DeserializeObject<ScheduledTime>(
                        deliveryRequest.CurrentScheduledTime!
                    )!;
                    continue;
                }
                tmp.ScheduledTime = GetSameScheduledTimeIfAddedToGroup(
                    tmp,
                    JsonConvert.DeserializeObject<ScheduledTime>(
                        deliveryRequest.CurrentScheduledTime!
                    )!
                )!;
            }

            if (tmp.ScheduledTime == null)
                return null;

            try
            {
                DateTime result = DateTime.ParseExact(
                    tmp.ScheduledTime.Day,
                    "M/d/yyyy",
                    CultureInfo.InvariantCulture
                );

                tmp.ScheduledTime.Day = result.ToString("yyyy-MM-dd");
                return tmp.ScheduledTime;
            }
            catch
            {
                return tmp.ScheduledTime;
            }
        }

        private ScheduledTime GetCurrentScheduledTimeOfScheduledRoute(ScheduledRoute scheduledRoute)
        {
            GroupOfDeliveryRequestWithSamePartOfScheduledTime tmp =
                new GroupOfDeliveryRequestWithSamePartOfScheduledTime();

            List<DeliveryRequest> deliveryRequests = scheduledRoute.ScheduledRouteDeliveryRequests
                .Where(
                    srdr =>
                        (
                            srdr.Status == ScheduledRouteDeliveryRequestStatus.SCHEDULED
                            && GetPeriodOfCurrentScheduledTime(srdr.DeliveryRequest) != null
                        )
                        || (
                            srdr.Status
                            == ScheduledRouteDeliveryRequestStatus.CANCELED_BY_CONTRIBUTOR
                        )
                )
                .Select(srdr => srdr.DeliveryRequest)
                .OrderBy(dr => GetPeriodOfCurrentScheduledTime(dr))
                .ToList();

            if (
                scheduledRoute.Status == ScheduledRouteStatus.CANCELED
                || scheduledRoute.Status == ScheduledRouteStatus.LATE
            )
            {
                return new ScheduledTime
                {
                    Day = scheduledRoute.StartDate.ToString("yyyy-MM-dd"),
                    StartTime = scheduledRoute.StartDate.ToString("HH:mm"),
                    EndTime = scheduledRoute.StartDate.ToString("HH:mm")
                };
            }
            else
            {
                foreach (DeliveryRequest deliveryRequest in deliveryRequests)
                {
                    if (tmp.ScheduledTime == null)
                    {
                        tmp.ScheduledTime = JsonConvert.DeserializeObject<ScheduledTime>(
                            deliveryRequest.CurrentScheduledTime!
                        )!;
                        continue;
                    }
                    tmp.ScheduledTime = GetSameScheduledTimeIfAddedToGroup(
                        tmp,
                        JsonConvert.DeserializeObject<ScheduledTime>(
                            deliveryRequest.CurrentScheduledTime!
                        )!
                    )!;
                }
            }

            try
            {
                DateTime result = DateTime.ParseExact(
                    tmp.ScheduledTime.Day,
                    "M/d/yyyy",
                    CultureInfo.InvariantCulture
                );

                tmp.ScheduledTime.Day = result.ToString("yyyy-MM-dd");
                return tmp.ScheduledTime;
            }
            catch
            {
                return tmp.ScheduledTime;
            }
        }

        public async Task<CommonResponse> ReceiveItemsToFinishScheduledRouteTypeItemsToBranchAsync(
            Guid userId,
            ReceivedItemsToFinishScheduledRoute receivedItemsToFinishScheduledRoute
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

                ScheduledRoute? scheduledRoute =
                    await _scheduledRouteRepository.FindProcessingScheduledRouteByIdAsync(
                        receivedItemsToFinishScheduledRoute.ScheduledRouteId
                    );

                if (scheduledRoute == null)
                    return new CommonResponse
                    {
                        Status = 400,
                        Message = _config[
                            "ResponseMessages:ScheduledRouteMsg:ProcessingScheduledRouteNotFound"
                        ]
                    };

                if (
                    GetMainBranchId(
                        scheduledRoute.ScheduledRouteDeliveryRequests[0].DeliveryRequest
                    ) != branch.Id
                )
                    return new CommonResponse
                    {
                        Status = 400,
                        Message = _config[
                            "ResponseMessages:ScheduledRouteMsg:ScheduledRouteNotBelongToYourBranch"
                        ]
                    };

                if (
                    GetStockUpdatedHistoryTypeOfScheduledRoute(scheduledRoute)
                    != StockUpdatedHistoryType.IMPORT
                )
                    return new CommonResponse
                    {
                        Status = 400,
                        Message = _config[
                            "ResponseMessages:ScheduledRouteMsg:ScheduledRouteIsNotDonatedRequestToBranchTypeMsg"
                        ]
                    };

                //xét các delivery request trong route, nếu status sủa delivery request là:
                //DELIVERED: cho phép data match trong request vào history, vào stock và cập nhật delivery request thành finished - có xuất hiện trong request
                //ACCEPTED, SHIPPING, ARRIVED_PICKUP, COLLECTED, ARRIVED_DELIVERY: về peding và ngắt qh với scheduled route - không xuất hiện trong request
                //REPORTED: không xuất hiện trong request
                //1. report ko cho/lấy đồ khi tới nơi: alerting
                //2. report ko cho/lấy đồ trước khi tới nơi: warning
                //3. report call nhưng ko bắt máy hoặc hên bữa sau: remind

                List<DeliveryRequest> deliveredDeliveryRequests =
                    scheduledRoute.ScheduledRouteDeliveryRequests
                        .Where(
                            srdr => srdr.DeliveryRequest.Status == DeliveryRequestStatus.DELIVERED
                        )
                        .Select(srdr => srdr.DeliveryRequest)
                        .ToList();

                List<ScheduledRouteDeliveryRequest> shippingScheduledRouteDeliveryRequests =
                    scheduledRoute.ScheduledRouteDeliveryRequests
                        .Where(
                            srdr =>
                                srdr.DeliveryRequest.Status == DeliveryRequestStatus.SHIPPING
                                || srdr.DeliveryRequest.Status
                                    == DeliveryRequestStatus.ARRIVED_PICKUP
                        )
                        .ToList();

                if (deliveredDeliveryRequests.Count == 0)
                    return new CommonResponse
                    {
                        Status = 400,
                        Message = _config[
                            "ResponseMessages:ScheduledRouteMsg:ScheduledRouteDontHaveAnyDeliveredDeliveryRequestMsg"
                        ]
                    };

                if (
                    !(
                        receivedItemsToFinishScheduledRoute.DeliveryRequests
                            .Select(dr => dr.DeliveryRequestId)
                            .Except(deliveredDeliveryRequests.Select(dr => dr.Id))
                            .Count() == 0
                        && receivedItemsToFinishScheduledRoute.DeliveryRequests.Count()
                            == deliveredDeliveryRequests.Count
                    )
                )
                    return new CommonResponse
                    {
                        Status = 400,
                        Message = _config[
                            "ResponseMessages:ScheduledRouteMsg:DeliveredDeliveryRequestNotMatchInListMsg"
                        ]
                    };

                List<TargetProcess> targetProcesses =
                    await _targetProcessRepository.FindStillTakingPlaceActivityTargetProcessesByBranchIdAsync(
                        branch.Id
                    );

                using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
                {
                    foreach (DeliveryRequest deliveryRequest in deliveredDeliveryRequests)
                    {
                        DeliveryRequestRequest tmpDeliveryRequest =
                            receivedItemsToFinishScheduledRoute.DeliveryRequests.FirstOrDefault(
                                dr => dr.DeliveryRequestId == deliveryRequest.Id
                            )!;

                        if (
                            !(
                                tmpDeliveryRequest.ReceivedDeliveryItemRequests
                                    .Select(dr => dr.DeliveryItemId)
                                    .Except(deliveryRequest.DeliveryItems.Select(di => di.Id))
                                    .Count() == 0
                                && tmpDeliveryRequest.ReceivedDeliveryItemRequests.Count
                                    == deliveryRequest.DeliveryItems.Count
                            )
                        )
                            return new CommonResponse
                            {
                                Status = 400,
                                Message = _config[
                                    "ResponseMessages:ScheduledRouteMsg:DeliveredDeliveryItemNotMatchInListMsg"
                                ]
                            };

                        //Guid? activityId =
                        //    deliveryRequest.DeliveryItems[0].DonatedItemId != null
                        //        ? (
                        //            await _donatedItemRepository.FindDonatedItemByIdAsync(
                        //                (Guid)deliveryRequest.DeliveryItems[0].DonatedItemId!
                        //            )
                        //        )!
                        //            .DonatedRequest
                        //            .ActivityId
                        //        : null;

                        List<Stock> newStocks = new List<Stock>();
                        List<Stock> oldStocks = new List<Stock>();

                        StockUpdatedHistory stockUpdatedHistory = new StockUpdatedHistory
                        {
                            CreatedDate = SettedUpDateTime.GetCurrentVietNamTime(),
                            Type = StockUpdatedHistoryType.IMPORT,
                            BranchId = branch.Id,
                            CreatedBy = userId,
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

                        foreach (DeliveryItem deliveryItem in deliveryRequest.DeliveryItems)
                        {
                            ReceivedDeliveryItemRequest receivedDeliveryItemRequest =
                                tmpDeliveryRequest.ReceivedDeliveryItemRequests.FirstOrDefault(
                                    rdi => rdi.DeliveryItemId == deliveryItem.Id
                                )!;

                            DonatedItem donatedItem = (
                                await _donatedItemRepository.FindDonatedItemByIdAsync(
                                    (Guid)deliveryItem.DonatedItemId!
                                )
                            )!;

                            TargetProcess? targetProcess = null;

                            if (donatedItem.DonatedRequest.ActivityId != null)
                            {
                                targetProcess = targetProcesses.FirstOrDefault(
                                    tp =>
                                        tp.ItemId == donatedItem.ItemId
                                        && tp.ActivityId == donatedItem.DonatedRequest.ActivityId
                                );

                                if (targetProcess == null)
                                {
                                    List<TargetProcess> tmp =
                                        await _targetProcessRepository.FindTargetProcessesByActivityIdAsync(
                                            (Guid)donatedItem.DonatedRequest.ActivityId
                                        );
                                    targetProcesses.AddRange(tmp);

                                    targetProcess = targetProcesses.FirstOrDefault(
                                        tp =>
                                            tp.ItemId == donatedItem.ItemId
                                            && tp.ActivityId
                                                == donatedItem.DonatedRequest.ActivityId
                                    );
                                }
                            }
                            else
                            {
                                targetProcess = targetProcesses
                                    .Where(tp => tp.ItemId == donatedItem.ItemId)
                                    .MinBy(tp => tp.Process / tp.Target);
                            }

                            Stock? stock =
                                await _stockRepository.FindStockByItemIdAndExpirationDateAndBranchIdAndUserIdAndActivityId(
                                    donatedItem.ItemId,
                                    receivedDeliveryItemRequest.ExpirationDate,
                                    branch.Id,
                                    donatedItem.DonatedRequest.UserId,
                                    donatedItem.DonatedRequest.ActivityId != null
                                        ? donatedItem.DonatedRequest.ActivityId
                                        : targetProcess == null
                                            ? null
                                            : targetProcess.ActivityId
                                );

                            int rs = 0;
                            if (stock != null)
                            {
                                stock.Quantity += receivedDeliveryItemRequest.Quantity;
                                rs = await _stockRepository.UpdateStockAsync(stock);
                                // gui thong bao
                                //User? systemAdmin = await _userRepository.FindUserByRoleAsync(
                                //    "SYSTEM_ADMIN"
                                //);
                            }
                            else
                            {
                                stock = new Stock
                                {
                                    CreatedDate = SettedUpDateTime.GetCurrentVietNamTime(),
                                    ExpirationDate = receivedDeliveryItemRequest
                                        .ExpirationDate
                                        .Date,
                                    Quantity = receivedDeliveryItemRequest.Quantity,
                                    ItemId = donatedItem.ItemId,
                                    Status = StockStatus.VALID,
                                    BranchId = branch.Id,
                                    UserId = donatedItem.DonatedRequest.UserId,
                                    ActivityId =
                                        donatedItem.DonatedRequest.ActivityId != null
                                            ? donatedItem.DonatedRequest.ActivityId
                                            : targetProcess == null
                                                ? null
                                                : targetProcess.ActivityId
                                };
                                rs = await _stockRepository.AddStockAsync(stock);
                                // gui thong bao
                                //User? systemAdmin = await _userRepository.FindUserByRoleAsync(
                                //    "SYSTEM_ADMIN"
                                //);
                            }

                            if (rs != 1)
                                return new CommonResponse
                                {
                                    Status = 500,
                                    Message = _config[
                                        "ResponseMessages:CommonMsg:InternalServerErrorMsg"
                                    ]
                                };

                            if (targetProcess != null)
                            {
                                targetProcess.Process += receivedDeliveryItemRequest.Quantity;

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

                            StockUpdatedHistoryDetail stockUpdatedHistoryDetail =
                                new StockUpdatedHistoryDetail
                                {
                                    Quantity = receivedDeliveryItemRequest.Quantity,
                                    Note = receivedDeliveryItemRequest.Note,
                                    StockUpdatedHistoryId = stockUpdatedHistory.Id,
                                    StockId = stock.Id,
                                    DeliveryItemId = deliveryItem.Id
                                };

                            if (
                                await _stockUpdatedHistoryDetailRepository.AddStockUpdatedHistoryDetailAsync(
                                    stockUpdatedHistoryDetail
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

                        deliveryRequest.Status = DeliveryRequestStatus.FINISHED;
                        if (
                            await _deliveryRequestRepository.UpdateDeliveryRequestAsync(
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

                        if (deliveryRequest.DonatedRequestId != null)
                        {
                            List<DeliveryRequest> deliveryRequests =
                                await _deliveryRequestRepository.FindDeliveryRequestsByDonatedRequestIdAsync(
                                    (Guid)deliveryRequest.DonatedRequestId
                                );

                            if (
                                deliveryRequests.Any(
                                    dr => dr.Status == DeliveryRequestStatus.FINISHED
                                )
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
                                deliveryRequest.DonatedRequest!.Status =
                                    DonatedRequestStatus.FINISHED;
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
                                !deliveryRequests.Any(
                                    dr => dr.Status == DeliveryRequestStatus.FINISHED
                                )
                                && deliveryRequests
                                    .Where(
                                        dr =>
                                            dr.Status == DeliveryRequestStatus.EXPIRED
                                            || dr.Status == DeliveryRequestStatus.CANCELED
                                    )
                                    .Count() == deliveryRequests.Count
                            )
                            {
                                deliveryRequest.DonatedRequest!.Status =
                                    DonatedRequestStatus.CANCELED;
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
                    }

                    foreach (
                        ScheduledRouteDeliveryRequest scheduledRouteDeliveryRequest in shippingScheduledRouteDeliveryRequests
                    )
                    {
                        scheduledRouteDeliveryRequest.DeliveryRequest.Status =
                            DeliveryRequestStatus.PENDING;
                        if (
                            await _deliveryRequestRepository.UpdateDeliveryRequestAsync(
                                scheduledRouteDeliveryRequest.DeliveryRequest
                            ) != 1
                        )
                            return new CommonResponse
                            {
                                Status = 500,
                                Message = _config[
                                    "ResponseMessages:CommonMsg:InternalServerErrorMsg"
                                ]
                            };

                        scheduledRouteDeliveryRequest.Status =
                            ScheduledRouteDeliveryRequestStatus.CANCELED;

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

                    scheduledRoute.Status = ScheduledRouteStatus.FINISHED;
                    scheduledRoute.FinishedDate = SettedUpDateTime.GetCurrentVietNamTime();
                    if (
                        await _scheduledRouteRepository.UpdateScheduledRouteAsync(scheduledRoute)
                        != 1
                    )
                        return new CommonResponse
                        {
                            Status = 500,
                            Message = _config["ResponseMessages:CommonMsg:InternalServerErrorMsg"]
                        };

                    if (shippingScheduledRouteDeliveryRequests.Count > 0)
                    {
                        BackgroundJob.Enqueue(
                            () =>
                                UpdateScheduledRoutes(
                                    DeliveryType.DONATED_REQUEST_TO_BRANCH,
                                    branch.Id
                                )
                        );
                    }

                    foreach (DeliveryRequest deliveryRequest in deliveredDeliveryRequests)
                    {
                        await SendNotificationBaseOnDeliveryRequestStatusOfDeliveryRequest(
                            deliveryRequest
                        );
                    }

                    scope.Complete();
                    return new CommonResponse
                    {
                        Status = 200,
                        Message = _config[
                            "ResponseMessages:ScheduledRouteMsg:FinishScheduledRouteSuccessMsg"
                        ]
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    $"An exception occurred in service {nameof(ScheduledRouteService)}, method {nameof(ReceiveItemsToFinishScheduledRouteTypeItemsToBranchAsync)}."
                );
            }
            return new CommonResponse
            {
                Status = 500,
                Message = _config["ResponseMessages:CommonMsg:InternalServerErrorMsg"]
            };
        }

        public async Task<CommonResponse> StartScheduledRouteAsync(
            Guid userId,
            ScheduledRouteStartingRequest scheduledRouteStartingRequest
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

                ScheduledRoute? scheduledRoute =
                    await _scheduledRouteRepository.FindAcceptedScheduledRouteByIdAndUserIdAsync(
                        scheduledRouteStartingRequest.ScheduledRouteId,
                        userId
                    );

                if (scheduledRoute == null)
                    return new CommonResponse
                    {
                        Status = 400,
                        Message = _config[
                            "ResponseMessages:ScheduledRouteMsg:AcceptedScheduledRouteNotFoundMsg"
                        ]
                    };

                ScheduledTime scheduledTime = GetCurrentScheduledTimeOfScheduledRoute(
                    scheduledRoute
                );

                DateTime startDate = GetStartDateTimeFromScheduledTime(scheduledTime);

                if (SettedUpDateTime.GetCurrentVietNamTime() < startDate)
                    return new CommonResponse
                    {
                        Status = 400,
                        Message = _config[
                            "ResponseMessages:ScheduledRouteMsg:ScheduledRouteHasNotStartedYetMsg"
                        ]
                    };

                List<ScheduledRouteDeliveryRequest> scheduledRouteDeliveryRequests =
                    scheduledRoute.ScheduledRouteDeliveryRequests
                        .Where(srdr => srdr.Status == ScheduledRouteDeliveryRequestStatus.SCHEDULED)
                        .ToList();

                List<DeliveryRequest> deliveryRequests = scheduledRouteDeliveryRequests
                    .Select(srdr => srdr.DeliveryRequest)
                    .ToList();

                deliveryRequests.ForEach(dr => dr.Status = DeliveryRequestStatus.SHIPPING);
                scheduledRoute.Status = ScheduledRouteStatus.PROCESSING;

                List<ScheduledRouteDeliveryRequest> orderedScheduledRouteDeliveryRequests =
                    await GetRealDeliveringOrderForScheduledRoute(
                        deliveryRequests,
                        scheduledTime,
                        _openRouteService.SwapCoordinates(
                            _openRouteService.GetCoordinatesByLocation(
                                $"{scheduledRouteStartingRequest.Latitude},{scheduledRouteStartingRequest.Longitude}"
                            )!
                        )
                    );

                foreach (
                    ScheduledRouteDeliveryRequest scheduledRouteDeliveryRequest in scheduledRouteDeliveryRequests
                )
                {
                    ScheduledRouteDeliveryRequest tmp =
                        orderedScheduledRouteDeliveryRequests.FirstOrDefault(
                            srdr =>
                                srdr.DeliveryRequestId
                                == scheduledRouteDeliveryRequest.DeliveryRequestId
                        )!;

                    scheduledRouteDeliveryRequest.Order = tmp.Order;
                    scheduledRouteDeliveryRequest.TimeToReachThisOrNextAsSeconds =
                        tmp.TimeToReachThisOrNextAsSeconds;
                    scheduledRouteDeliveryRequest.DistanceToReachThisOrNextAsMeters =
                        tmp.DistanceToReachThisOrNextAsMeters;
                }

                using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
                {
                    if (
                        await _scheduledRouteRepository.UpdateScheduledRouteAsync(scheduledRoute)
                        != 1
                    )
                        return new CommonResponse
                        {
                            Status = 500,
                            Message = _config["ResponseMessages:CommonMsg:InternalServerErrorMsg"]
                        };

                    if (
                        await _deliveryRequestRepository.UpdateDeliveryRequestsAsync(
                            deliveryRequests
                        ) != deliveryRequests.Count
                    )
                        return new CommonResponse
                        {
                            Status = 500,
                            Message = _config["ResponseMessages:CommonMsg:InternalServerErrorMsg"]
                        };

                    if (
                        await _scheduledRouteDeliveryRequestRepository.UpdateScheduledRouteDeliveryRequestsAsync(
                            scheduledRouteDeliveryRequests
                        ) != scheduledRouteDeliveryRequests.Count
                    )
                        return new CommonResponse
                        {
                            Status = 500,
                            Message = _config["ResponseMessages:CommonMsg:InternalServerErrorMsg"]
                        };

                    foreach (DeliveryRequest deliveryRequest in deliveryRequests)
                    {
                        await SendNotificationBaseOnDeliveryRequestStatusOfDeliveryRequest(
                            deliveryRequest
                        );
                    }

                    scope.Complete();
                    return new CommonResponse
                    {
                        Status = 200,
                        Message = _config[
                            "ResponseMessages:ScheduledRouteMsg:StartScheduledRouteSuccessMsg"
                        ]
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    $"An exception occurred in service {nameof(ScheduledRouteService)}, method {nameof(StartScheduledRouteAsync)}."
                );
                return new CommonResponse
                {
                    Status = 500,
                    Message = _config["ResponseMessages:CommonMsg:InternalServerErrorMsg"]
                };
            }
        }

        public async Task SendNotificationBaseOnDeliveryRequestStatusOfDeliveryRequest(
            DeliveryRequest deliveryRequest
        )
        {
            try
            {
                DeliveryType deliveryType = GetDeliveryTypeOfDeliveryRequest(deliveryRequest);

                if (deliveryType == DeliveryType.DONATED_REQUEST_TO_BRANCH)
                {
                    if (deliveryRequest.Status == DeliveryRequestStatus.SHIPPING)
                    {
                        Notification notificationForUser = new Notification
                        {
                            Name =
                                "Có yêu cầu vận chuyển đang được thực hiện cho yêu cầu quyên góp của bạn.",
                            CreatedDate = SettedUpDateTime.GetCurrentVietNamTime(),
                            Image = _config["Notification:Image"],
                            Content =
                                "Có yêu cầu vận chuyển đang được thực hiện cho yêu cầu quyên góp của bạn. Hãy chuẩn bị vật phẩm để giao cho tình nguyện viên vận chuyển về chi nhánh.",
                            ReceiverId = deliveryRequest.DonatedRequest!.UserId.ToString(),
                            Status = NotificationStatus.NEW,
                            Type = NotificationType.NOTIFYING,
                            DataType = DataNotificationType.DONATED_REQUEST,
                            DataId = deliveryRequest.DonatedRequestId
                        };
                        await _notificationRepository.CreateNotificationAsync(notificationForUser);
                        await _hubContext.Clients.All.SendAsync(
                            deliveryRequest.DonatedRequest!.UserId.ToString(),
                            notificationForUser
                        );
                        if (deliveryRequest.DonatedRequest!.User.DeviceToken != null)
                        {
                            PushNotificationRequest pushNotificationRequest =
                                new PushNotificationRequest
                                {
                                    DeviceToken = deliveryRequest.DonatedRequest!.User.DeviceToken,
                                    Message =
                                        "Có yêu cầu vận chuyển đang được thực hiện cho yêu cầu quyên góp của bạn. Hãy chuẩn bị vật phẩm để giao cho tình nguyện viên vận chuyển về chi nhánh.",
                                    Title =
                                        "Có yêu cầu vận chuyển đang được thực hiện cho yêu cầu quyên góp của bạn."
                                };
                            await _firebaseNotificationService.PushNotification(
                                pushNotificationRequest
                            );
                        }
                    }

                    if (deliveryRequest.Status == DeliveryRequestStatus.ARRIVED_PICKUP)
                    {
                        Notification notificationForUser = new Notification
                        {
                            Name = "Có yêu cầu vận chuyển đã tới nơi quyên góp của bạn.",
                            CreatedDate = SettedUpDateTime.GetCurrentVietNamTime(),
                            Image = _config["Notification:Image"],
                            Content =
                                "Có yêu cầu vận chuyển đã tới nơi quyên góp của bạn. Hãy giao vật phẩm quyên góp cho tình nguyện viên.",
                            ReceiverId = deliveryRequest.DonatedRequest!.UserId.ToString(),
                            Status = NotificationStatus.NEW,
                            Type = NotificationType.NOTIFYING,
                            DataType = DataNotificationType.DONATED_REQUEST,
                            DataId = deliveryRequest.DonatedRequestId
                        };
                        await _notificationRepository.CreateNotificationAsync(notificationForUser);
                        await _hubContext.Clients.All.SendAsync(
                            deliveryRequest.DonatedRequest!.UserId.ToString(),
                            notificationForUser
                        );
                        if (deliveryRequest.DonatedRequest!.User.DeviceToken != null)
                        {
                            PushNotificationRequest pushNotificationRequest =
                                new PushNotificationRequest
                                {
                                    DeviceToken = deliveryRequest.DonatedRequest!.User.DeviceToken,
                                    Message = "Có yêu cầu vận chuyển đã tới nơi quyên góp của bạn.",
                                    Title =
                                        "Có yêu cầu vận chuyển đã tới nơi quyên góp của bạn. Hãy giao vật phẩm quyên góp cho tình nguyện viên."
                                };
                            await _firebaseNotificationService.PushNotification(
                                pushNotificationRequest
                            );
                        }
                    }

                    if (deliveryRequest.Status == DeliveryRequestStatus.FINISHED)
                    {
                        Notification notificationForUser = new Notification
                        {
                            Name = "Vật phẩm quyên góp đã được nhập kho.",
                            CreatedDate = SettedUpDateTime.GetCurrentVietNamTime(),
                            Image = _config["Notification:Image"],
                            Content = "Vật phẩm quyên góp đã được nhập kho ở chi nhánh trực thuộc.",
                            ReceiverId = deliveryRequest.DonatedRequest!.UserId.ToString(),
                            Status = NotificationStatus.NEW,
                            Type = NotificationType.NOTIFYING,
                            DataType = DataNotificationType.REPORTABLE_DELIVERY_REQUEST,
                            DataId = deliveryRequest.Id
                        };
                        await _notificationRepository.CreateNotificationAsync(notificationForUser);
                        await _hubContext.Clients.All.SendAsync(
                            deliveryRequest.DonatedRequest!.UserId.ToString(),
                            notificationForUser
                        );
                        if (deliveryRequest.DonatedRequest!.User.DeviceToken != null)
                        {
                            PushNotificationRequest pushNotificationRequest =
                                new PushNotificationRequest
                                {
                                    DeviceToken = deliveryRequest.DonatedRequest!.User.DeviceToken,
                                    Message =
                                        "Vật phẩm quyên góp đã được nhập kho ở chi nhánh trực thuộc.",
                                    Title = "Vật phẩm quyên góp đã được nhập kho."
                                };
                            await _firebaseNotificationService.PushNotification(
                                pushNotificationRequest
                            );
                        }
                    }
                }
                else if (deliveryType == DeliveryType.BRANCH_TO_AID_REQUEST)
                {
                    if (deliveryRequest.Status == DeliveryRequestStatus.SHIPPING)
                    {
                        Notification notificationForAdmin = new Notification
                        {
                            Name = "Có yêu cầu vận chuyển vật phẩm hỗ trợ đang được thực hiện.",
                            CreatedDate = SettedUpDateTime.GetCurrentVietNamTime(),
                            Image = _config["Notification:Image"],
                            Content =
                                "Có yêu cầu vận chuyển vật phẩm hỗ trợ đang được thực hiện. Hãy chuẩn bị vật phẩm để giao cho tình nguyện viên vận chuyển tới nơi cần hỗ trợ.",
                            ReceiverId = deliveryRequest.Branch.BranchAdminId.ToString(),
                            Status = NotificationStatus.NEW,
                            Type = NotificationType.NOTIFYING,
                            DataType = DataNotificationType.SCHEDULED_ROUTE,
                            DataId = deliveryRequest.ScheduledRouteDeliveryRequests
                                .FirstOrDefault(
                                    srdr =>
                                        srdr.Status == ScheduledRouteDeliveryRequestStatus.SCHEDULED
                                )!
                                .ScheduledRouteId
                        };
                        await _notificationRepository.CreateNotificationAsync(notificationForAdmin);
                        await _hubContext.Clients.All.SendAsync(
                            deliveryRequest.Branch.BranchAdminId.ToString(),
                            notificationForAdmin
                        );

                        Notification notificationForCharityUnit = new Notification
                        {
                            Name = "Có yêu cầu vận chuyển vật phẩm hỗ trợ đang được thực hiện.",
                            CreatedDate = SettedUpDateTime.GetCurrentVietNamTime(),
                            Image = _config["Notification:Image"],
                            Content =
                                "Có yêu cầu vận chuyển vật phẩm hỗ trợ đang được thực hiện. Hãy chuẩn bị nhận vật phẩm từ chi nhánh do tình nguyện viên vận chuyển tới.",
                            ReceiverId = deliveryRequest.AidRequest!.CharityUnit!.UserId.ToString(),
                            Status = NotificationStatus.NEW,
                            Type = NotificationType.NOTIFYING,
                            DataType = DataNotificationType.AID_REQUEST,
                            DataId = deliveryRequest.AidRequestId
                        };
                        await _notificationRepository.CreateNotificationAsync(
                            notificationForCharityUnit
                        );
                        await _hubContext.Clients.All.SendAsync(
                            deliveryRequest.AidRequest!.CharityUnit!.UserId.ToString(),
                            notificationForCharityUnit
                        );
                    }

                    if (deliveryRequest.Status == DeliveryRequestStatus.ARRIVED_PICKUP)
                    {
                        Notification notificationForAdmin = new Notification
                        {
                            Name =
                                "Có yêu cầu vận chuyển vật phẩm hỗ trợ đã tới chi nhánh lấy hàng.",
                            CreatedDate = SettedUpDateTime.GetCurrentVietNamTime(),
                            Image = _config["Notification:Image"],
                            Content =
                                "Có yêu cầu vận chuyển vật phẩm hỗ trợ đã tới chi nhánh lấy hàng. Hãy giao vật phẩm cho tình nguyện viên vận chuyển tới nơi cần hỗ trợ.",
                            ReceiverId = deliveryRequest.Branch.BranchAdminId.ToString(),
                            Status = NotificationStatus.NEW,
                            Type = NotificationType.NOTIFYING,
                            DataType = DataNotificationType.SCHEDULED_ROUTE,
                            DataId = deliveryRequest.ScheduledRouteDeliveryRequests
                                .FirstOrDefault(
                                    srdr =>
                                        srdr.Status == ScheduledRouteDeliveryRequestStatus.SCHEDULED
                                )!
                                .ScheduledRouteId
                        };
                        await _notificationRepository.CreateNotificationAsync(notificationForAdmin);
                        await _hubContext.Clients.All.SendAsync(
                            deliveryRequest.Branch.BranchAdminId.ToString(),
                            notificationForAdmin
                        );
                    }

                    if (deliveryRequest.Status == DeliveryRequestStatus.ARRIVED_DELIVERY)
                    {
                        Notification notificationForCharityUnit = new Notification
                        {
                            Name = "Có yêu cầu vận chuyển vật phẩm hỗ trợ đã tới nơi giao.",
                            CreatedDate = SettedUpDateTime.GetCurrentVietNamTime(),
                            Image = _config["Notification:Image"],
                            Content =
                                "Có yêu cầu vận chuyển vật phẩm hỗ trợ đã tới nơi giao. Hãy nhận vật phẩm cho tình nguyện viên vận chuyển tới từ chi nhánh.",
                            ReceiverId = deliveryRequest.AidRequest!.CharityUnit!.UserId.ToString(),
                            Status = NotificationStatus.NEW,
                            Type = NotificationType.NOTIFYING,
                            DataType = DataNotificationType.AID_REQUEST,
                            DataId = deliveryRequest.AidRequestId
                        };
                        await _notificationRepository.CreateNotificationAsync(
                            notificationForCharityUnit
                        );
                        await _hubContext.Clients.All.SendAsync(
                            deliveryRequest.AidRequest!.CharityUnit!.UserId.ToString(),
                            notificationForCharityUnit
                        );
                    }

                    if (deliveryRequest.Status == DeliveryRequestStatus.FINISHED)
                    {
                        Notification notificationForAdmin = new Notification
                        {
                            Name = "Yêu cầu vận chuyển tới nơi cần hỗ trợ đã được hoàn thành.",
                            CreatedDate = SettedUpDateTime.GetCurrentVietNamTime(),
                            Image = _config["Notification:Image"],
                            Content = "Yêu cầu vận chuyển tới nơi cần hỗ trợ đã được hoàn thành.",
                            ReceiverId = deliveryRequest.Branch.BranchAdminId.ToString(),
                            Status = NotificationStatus.NEW,
                            Type = NotificationType.NOTIFYING,
                            DataType = DataNotificationType.SCHEDULED_ROUTE,
                            DataId = deliveryRequest.ScheduledRouteDeliveryRequests
                                .FirstOrDefault(
                                    srdr =>
                                        srdr.Status == ScheduledRouteDeliveryRequestStatus.SCHEDULED
                                )!
                                .ScheduledRouteId
                        };
                        await _notificationRepository.CreateNotificationAsync(notificationForAdmin);
                        await _hubContext.Clients.All.SendAsync(
                            deliveryRequest.Branch.BranchAdminId.ToString(),
                            notificationForAdmin
                        );
                    }
                }
                else
                {
                    //gửi các thông báo giữa branch nhận và branch giao
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    $"An exception occurred in service {nameof(ScheduledRouteService)}, method {nameof(SendNotificationBaseOnDeliveryRequestStatusOfDeliveryRequest)}."
                );
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

        private StockUpdatedHistoryType GetStockUpdatedHistoryTypeOfScheduledRoute(
            ScheduledRoute scheduledRoute
        )
        {
            DeliveryRequest firstDeliveryRequest = scheduledRoute.ScheduledRouteDeliveryRequests[
                0
            ].DeliveryRequest;

            if (firstDeliveryRequest.DonatedRequestId != null)
                return StockUpdatedHistoryType.IMPORT;
            else if (firstDeliveryRequest.AidRequest!.CharityUnitId != null)
                return StockUpdatedHistoryType.EXPORT;
            else
                return StockUpdatedHistoryType.IMPORT;
        }

        private async Task<
            List<ScheduledRouteDeliveryRequest>
        > GetRealDeliveringOrderForScheduledRoute(
            List<DeliveryRequest> deliveryRequests,
            ScheduledTime scheduledTime,
            List<double> swappedCollaboratorLocation
        )
        {
            Dictionary<int, DeliveryRequest> deliveryRequestIdPairs =
                new Dictionary<int, DeliveryRequest>();
            int deliveryRequestCount = 0;
            double speedFactor = double.Parse(_config["OpenRoute:SpeedFactor"]);
            GroupOfDeliveryRequestWithSamePartOfScheduledTime tmp =
                new GroupOfDeliveryRequestWithSamePartOfScheduledTime
                {
                    ScheduledTime = scheduledTime,
                    DeliveryRequests = deliveryRequests
                };
            List<Shipment> shipments = new List<Shipment>();

            Vehicle vehicle = new Vehicle
            {
                Id = 1,
                Capacity = new List<int>
                {
                    int.Parse(_config["ScheduledRoute:MaxVolumnPercentToSchedule"])
                },
                Start = swappedCollaboratorLocation,
                End = null,
                Skills = null
            };

            foreach (DeliveryRequest deliveryRequest in tmp.DeliveryRequests)
            {
                deliveryRequestCount += 1;
                deliveryRequestIdPairs[deliveryRequestCount] = deliveryRequest;
                shipments.Add(
                    new Shipment
                    {
                        Amount = new List<int> { 1 },
                        Skills = null,
                        Pickup = new Place
                        {
                            Id = deliveryRequestCount,
                            Service = (int)Math.Ceiling(300 * speedFactor),
                            Location = GetSwappedPickupLocation(deliveryRequest)
                        },
                        Delivery = new Place
                        {
                            Id = deliveryRequestCount,
                            Service = (int)Math.Ceiling(300 * speedFactor),
                            Location = GetSwappedDeliveryLocation(deliveryRequest)
                        }
                    }
                );
            }

            OptimizeRequest optimizeRequest = new OptimizeRequest
            {
                Vehicles = new List<Vehicle> { vehicle },
                Shipments = shipments
            };

            OptimizeResponse? optimizeResponse = await _openRouteService.GetOptimizeResponseAsync(
                new OptimizeRequest
                {
                    Vehicles = optimizeRequest.Vehicles,
                    Shipments = optimizeRequest.Shipments
                }
            );

            List<GroupOfDeliveryRequestOfRouteWithTimeAndDistance> groupOfDeliveryRequestsWithTimeAndDistances =
                new List<GroupOfDeliveryRequestOfRouteWithTimeAndDistance>();

            Route route = optimizeResponse!.Routes[0];
            Step step = route.Steps.FirstOrDefault(s => s.Type.Equals("pickup"))!;
            DeliveryRequest deliveryRequestType = deliveryRequestIdPairs[step.Id];
            if (deliveryRequestType.AidRequestId != null)
                groupOfDeliveryRequestsWithTimeAndDistances.AddRange(
                    GetAidRequestRoutes(route.Steps, deliveryRequestIdPairs)
                );
            else
            {
                groupOfDeliveryRequestsWithTimeAndDistances.AddRange(
                    GetDonatedRequestRoutes(route.Steps, deliveryRequestIdPairs)
                );
            }

            GroupOfDeliveryRequestOfRouteWithTimeAndDistance groupOfDeliveryRequestOfRouteWithTimeAndDistance =
                groupOfDeliveryRequestsWithTimeAndDistances[0];

            List<ScheduledRouteDeliveryRequest> scheduledRouteDeliveryRequests =
                new List<ScheduledRouteDeliveryRequest>();

            for (
                int i = 0;
                i < groupOfDeliveryRequestOfRouteWithTimeAndDistance.DeliveryRequests.Count;
                i++
            )
            {
                scheduledRouteDeliveryRequests.Add(
                    new ScheduledRouteDeliveryRequest
                    {
                        DeliveryRequestId =
                            groupOfDeliveryRequestOfRouteWithTimeAndDistance.DeliveryRequests[i].Id,
                        Order = i + 1,
                        TimeToReachThisOrNextAsSeconds =
                            groupOfDeliveryRequestOfRouteWithTimeAndDistance.TimesAsSeconds[i]
                            / speedFactor,
                        DistanceToReachThisOrNextAsMeters =
                            groupOfDeliveryRequestOfRouteWithTimeAndDistance.DistancesAsMeters[i]
                    }
                );
            }

            return scheduledRouteDeliveryRequests;
        }

        public async Task<CommonResponse> GetScheduledRoutesForAdminAsync(
            Guid? branchId,
            StockUpdatedHistoryType? stockUpdatedHistoryType,
            ScheduledRouteStatus? status,
            string? startDate,
            string? endDate,
            Guid? userId,
            Guid callerId,
            string userRoleName,
            int? pageSize,
            int? page,
            SortType? sortType
        )
        {
            if (userRoleName == RoleEnum.BRANCH_ADMIN.ToString())
            {
                Branch? branch = await _branchRepository.FindBranchByBranchAdminIdAsync(callerId);
                if (branch == null || branchId != null && branchId != branch.Id)
                {
                    return new CommonResponse
                    {
                        Status = 403,
                        Message = _config["ResponseMessages:CommonMsg:UnauthenticationMsg"]
                    };
                }
                branchId = branch.Id;
            }
            else if (userRoleName != RoleEnum.SYSTEM_ADMIN.ToString())
                return new CommonResponse
                {
                    Status = 403,
                    Message = _config["ResponseMessages:CommonMsg:UnauthenticationMsg"]
                };

            List<ScheduledRoute> scheduledRoutes =
                await _scheduledRouteRepository.GetScheduledRoutesForAdminAsync(status, userId);

            scheduledRoutes = scheduledRoutes
                .Where(
                    sr =>
                        sr.Status != ScheduledRouteStatus.PENDING
                            ? true
                            : sr.ScheduledRouteDeliveryRequests.Any(
                                srdr => srdr.Status == ScheduledRouteDeliveryRequestStatus.SCHEDULED
                            )
                )
                .ToList();

            scheduledRoutes = scheduledRoutes
                .Where(sr => sr.ScheduledRouteDeliveryRequests.Count > 0)
                .ToList();

            scheduledRoutes = scheduledRoutes
                .Where(
                    (s) =>
                    {
                        bool checkBranch =
                            branchId == null
                                ? true
                                : GetMainBranchId(
                                    s.ScheduledRouteDeliveryRequests[0].DeliveryRequest
                                ) == branchId;

                        bool checkType =
                            stockUpdatedHistoryType == null
                                ? true
                                : GetStockUpdatedHistoryTypeOfScheduledRoute(s)
                                    == stockUpdatedHistoryType;

                        bool checkDate = IsTrueForFilteredDateTime(s, startDate, endDate);
                        return checkBranch && checkType && checkDate;
                    }
                )
                .ToList();

            if (sortType == SortType.DES)
                scheduledRoutes = scheduledRoutes
                    .OrderByDescending(
                        s =>
                            GetEndDateTimeFromScheduledTime(
                                GetCurrentScheduledTimeOfScheduledRoute(s)
                            )
                    )
                    .ToList();
            else
                scheduledRoutes = scheduledRoutes
                    .OrderBy(
                        s =>
                            GetEndDateTimeFromScheduledTime(
                                GetCurrentScheduledTimeOfScheduledRoute(s)
                            )
                    )
                    .ToList();

            Pagination pagination = new Pagination();
            pagination.PageSize = pageSize == null ? 10 : pageSize.Value;
            pagination.CurrentPage = page == null ? 1 : page.Value;
            pagination.Total = scheduledRoutes.Count;
            scheduledRoutes = scheduledRoutes
                .Skip((pagination.CurrentPage - 1) * pagination.PageSize)
                .Take(pagination.PageSize)
                .ToList();

            List<ScheduledRouteResponseForAdmin> scheduledRouteResponsesForAdmin =
                new List<ScheduledRouteResponseForAdmin>();

            foreach (ScheduledRoute scheduledRoute in scheduledRoutes)
            {
                StockUpdatedHistoryType type = GetStockUpdatedHistoryTypeOfScheduledRoute(
                    scheduledRoute
                );

                scheduledRoute.ScheduledRouteDeliveryRequests =
                    scheduledRoute.ScheduledRouteDeliveryRequests
                        .Where(
                            srdr =>
                                srdr.Status == ScheduledRouteDeliveryRequestStatus.SCHEDULED
                                || srdr.Status
                                    == ScheduledRouteDeliveryRequestStatus.CANCELED_BY_CONTRIBUTOR
                        )
                        .OrderBy(srdr => srdr.Order)
                        .ToList();

                List<string> orderedAddresses = new List<string>();

                foreach (
                    ScheduledRouteDeliveryRequest scheduledRouteDeliveryRequest in scheduledRoute.ScheduledRouteDeliveryRequests
                )
                {
                    scheduledRouteDeliveryRequest.DeliveryRequest.DeliveryItems =
                        await _deliveryItemRepository.GetDeliveryItemsByDeliveryRequestIdAsync(
                            scheduledRouteDeliveryRequest.DeliveryRequestId
                        );

                    if (type == StockUpdatedHistoryType.IMPORT)
                    {
                        orderedAddresses.Add(
                            GetPickupAddress(scheduledRouteDeliveryRequest.DeliveryRequest)
                        );
                    }
                    else
                    {
                        orderedAddresses.Add(
                            GetDeliveryAddress(scheduledRouteDeliveryRequest.DeliveryRequest)
                        );
                    }
                }

                if (type == StockUpdatedHistoryType.IMPORT)
                {
                    orderedAddresses.Add(
                        GetMainBranchAddress(
                            scheduledRoute.ScheduledRouteDeliveryRequests[0].DeliveryRequest
                        )
                    );
                }
                else
                {
                    orderedAddresses.Insert(
                        0,
                        GetMainBranchAddress(
                            scheduledRoute.ScheduledRouteDeliveryRequests[0].DeliveryRequest
                        )
                    );
                }

                Branch mainBranch = GetMainBranch(
                    scheduledRoute.ScheduledRouteDeliveryRequests[0].DeliveryRequest
                );

                scheduledRouteResponsesForAdmin.Add(
                    new ScheduledRouteResponseForAdmin
                    {
                        Id = scheduledRoute.Id,
                        NumberOfDeliveryRequests = scheduledRoute
                            .ScheduledRouteDeliveryRequests
                            .Count,
                        ScheduledTime = GetCurrentScheduledTimeOfScheduledRoute(scheduledRoute),
                        OrderedAddresses = orderedAddresses,
                        TotalDistanceAsMeters = scheduledRoute.ScheduledRouteDeliveryRequests.Sum(
                            srdr => srdr.DistanceToReachThisOrNextAsMeters
                        ),
                        TotalTimeAsSeconds = scheduledRoute.ScheduledRouteDeliveryRequests.Sum(
                            srdr => srdr.TimeToReachThisOrNextAsSeconds
                        ),
                        BulkyLevel = GetBulkyLevel(
                                scheduledRoute.ScheduledRouteDeliveryRequests.Sum(
                                    srdr =>
                                        GetTotalTransportVolumnOfDeliveryRequest(
                                            srdr.DeliveryRequest
                                        )
                                ) * 100
                            )
                            .ToString(),
                        Type = type.ToString(),
                        Branch = new SimpleBranchResponse
                        {
                            Id = mainBranch.Id,
                            Name = mainBranch.Name,
                            Image = mainBranch.Image
                        },
                        Status = scheduledRoute.Status.ToString(),
                        AcceptedUser =
                            scheduledRoute.User == null
                                ? null
                                : new SimpleUserResponse
                                {
                                    Id = scheduledRoute.User.Id,
                                    FullName = scheduledRoute.User.Name!,
                                    Avatar = scheduledRoute.User.Avatar
                                }
                    }
                );
            }

            return new CommonResponse
            {
                Status = 200,
                Data = scheduledRouteResponsesForAdmin,
                Pagination = pagination,
                Message = _config["ResponseMessages:ScheduledRouteMsg:GetScheduledRoutesSuccessMsg"]
            };
        }

        public async Task<CommonResponse> GetScheduledRoutesForUserAsync(
            double? latitude,
            double? longitude,
            Guid? branchId,
            StockUpdatedHistoryType? stockUpdatedHistoryType,
            ScheduledRouteStatus? status,
            string? startDate,
            string? endDate,
            Guid userId,
            string userRoleName,
            SortType? sortType
        )
        {
            User? user = await _userRepository.FindUserByIdAsync(userId);

            if (user == null || !user.IsCollaborator)
                return new CommonResponse
                {
                    Status = 403,
                    Message = _config["ResponseMessages:CommonMsg:UnauthenticationMsg"]
                };

            List<ScheduledRoute> scheduledRoutes =
                await _scheduledRouteRepository.GetScheduledRoutesForUserAsync(status, userId);

            scheduledRoutes = scheduledRoutes
                .Where(
                    sr =>
                        sr.Status != ScheduledRouteStatus.PENDING
                            ? true
                            : GetAvailableScheduledTimeOfScheduledRoute(sr) != null
                                && sr.ScheduledRouteDeliveryRequests.Any(
                                    srdr =>
                                        srdr.Status == ScheduledRouteDeliveryRequestStatus.SCHEDULED
                                )
                )
                .ToList();

            scheduledRoutes = scheduledRoutes
                .Where(sr => sr.ScheduledRouteDeliveryRequests.Count > 0)
                .ToList();

            scheduledRoutes = scheduledRoutes
                .Where(
                    (s) =>
                    {
                        bool checkBranch =
                            branchId == null
                                ? true
                                : GetMainBranchId(
                                    s.ScheduledRouteDeliveryRequests[0].DeliveryRequest
                                ) == branchId;

                        bool checkType =
                            stockUpdatedHistoryType == null
                                ? true
                                : GetStockUpdatedHistoryTypeOfScheduledRoute(s)
                                    == stockUpdatedHistoryType;

                        bool checkDate = IsTrueForFilteredDateTime(s, startDate, endDate);
                        return checkBranch && checkType && checkDate;
                    }
                )
                .ToList();

            List<ScheduledRoute> finalResult = new List<ScheduledRoute>();

            if (
                userRoleName == RoleEnum.CONTRIBUTOR.ToString()
                && status == ScheduledRouteStatus.PENDING
            )
            {
                if (latitude == null || longitude == null)
                    return new CommonResponse
                    {
                        Status = 403,
                        Message = _config["ResponseMessages:CommonMsg:LocationNotValid"]
                    };

                double maxDistance = double.Parse(
                    _config["OpenRoute:MaxRoadLengthAsMetersForCollaborator"]
                );

                List<List<ScheduledRoute>> groupsOfScheduledRoutes = scheduledRoutes
                    .GroupBy(
                        s => GetMainBranchId(s.ScheduledRouteDeliveryRequests[0].DeliveryRequest)
                    )
                    .Select(g => g.ToList())
                    .ToList();

                foreach (List<ScheduledRoute> tmp in groupsOfScheduledRoutes)
                {
                    Branch branch = (
                        await _branchRepository.FindBranchByIdAsync(
                            GetMainBranchId(
                                tmp[0].ScheduledRouteDeliveryRequests[0].DeliveryRequest
                            )
                        )
                    )!;

                    if (
                        (
                            await _openRouteService.GetDeliverableBranchesByUserLocation(
                                $"{latitude},{longitude}",
                                new List<Branch> { branch },
                                maxDistance
                            )
                        ).NearestBranch != null
                    )
                    {
                        finalResult.AddRange(tmp);
                    }
                }
            }
            else
                finalResult = scheduledRoutes;

            if (sortType == SortType.DES)
                finalResult = finalResult
                    .OrderByDescending(
                        s =>
                            GetEndDateTimeFromScheduledTime(
                                GetCurrentScheduledTimeOfScheduledRoute(s)
                            )
                    )
                    .ToList();
            else
                finalResult = finalResult
                    .OrderBy(
                        s =>
                            GetEndDateTimeFromScheduledTime(
                                GetCurrentScheduledTimeOfScheduledRoute(s)
                            )
                    )
                    .ToList();

            List<ScheduledRouteResponseForUser> scheduledRouteResponsesForUser =
                new List<ScheduledRouteResponseForUser>();

            foreach (ScheduledRoute scheduledRoute in finalResult)
            {
                StockUpdatedHistoryType type = GetStockUpdatedHistoryTypeOfScheduledRoute(
                    scheduledRoute
                );

                scheduledRoute.ScheduledRouteDeliveryRequests =
                    scheduledRoute.ScheduledRouteDeliveryRequests
                        .Where(
                            srdr =>
                                srdr.Status == ScheduledRouteDeliveryRequestStatus.SCHEDULED
                                || srdr.Status
                                    == ScheduledRouteDeliveryRequestStatus.CANCELED_BY_CONTRIBUTOR
                        )
                        .OrderBy(srdr => srdr.Order)
                        .ToList();

                List<string> orderedAddresses = new List<string>();

                foreach (
                    ScheduledRouteDeliveryRequest scheduledRouteDeliveryRequest in scheduledRoute.ScheduledRouteDeliveryRequests
                )
                {
                    scheduledRouteDeliveryRequest.DeliveryRequest.DeliveryItems =
                        await _deliveryItemRepository.GetDeliveryItemsByDeliveryRequestIdAsync(
                            scheduledRouteDeliveryRequest.DeliveryRequestId
                        );

                    if (type == StockUpdatedHistoryType.IMPORT)
                    {
                        orderedAddresses.Add(
                            GetPickupAddress(scheduledRouteDeliveryRequest.DeliveryRequest)
                        );
                    }
                    else
                    {
                        orderedAddresses.Add(
                            GetDeliveryAddress(scheduledRouteDeliveryRequest.DeliveryRequest)
                        );
                    }
                }

                if (type == StockUpdatedHistoryType.IMPORT)
                {
                    orderedAddresses.Add(
                        GetMainBranchAddress(
                            scheduledRoute.ScheduledRouteDeliveryRequests[0].DeliveryRequest
                        )
                    );
                }
                else
                {
                    orderedAddresses.Insert(
                        0,
                        GetMainBranchAddress(
                            scheduledRoute.ScheduledRouteDeliveryRequests[0].DeliveryRequest
                        )
                    );
                }

                scheduledRouteResponsesForUser.Add(
                    new ScheduledRouteResponseForUser
                    {
                        Id = scheduledRoute.Id,
                        NumberOfDeliveryRequests = scheduledRoute
                            .ScheduledRouteDeliveryRequests
                            .Count,
                        ScheduledTime = GetCurrentScheduledTimeOfScheduledRoute(scheduledRoute),
                        OrderedAddresses = orderedAddresses,
                        TotalDistanceAsMeters = scheduledRoute.ScheduledRouteDeliveryRequests.Sum(
                            srdr => srdr.DistanceToReachThisOrNextAsMeters
                        ),
                        TotalTimeAsSeconds = scheduledRoute.ScheduledRouteDeliveryRequests.Sum(
                            srdr => srdr.TimeToReachThisOrNextAsSeconds
                        ),
                        BulkyLevel = GetBulkyLevel(
                                scheduledRoute.ScheduledRouteDeliveryRequests.Sum(
                                    srdr =>
                                        GetTotalTransportVolumnOfDeliveryRequest(
                                            srdr.DeliveryRequest
                                        )
                                ) * 100
                            )
                            .ToString(),
                        Type = type.ToString(),
                        Status = scheduledRoute.Status.ToString()
                    }
                );
            }

            return new CommonResponse
            {
                Status = 200,
                Data = scheduledRouteResponsesForUser,
                Message = _config["ResponseMessages:ScheduledRouteMsg:GetScheduledRoutesSuccessMsg"]
            };
        }

        private BulkyLevel GetBulkyLevel(double volumnPercent)
        {
            if (volumnPercent < 50)
                return BulkyLevel.NOT_BULKY;
            else if (volumnPercent >= 50 && volumnPercent < 80)
                return BulkyLevel.BULKY;
            else
                return BulkyLevel.VERY_BULKY;
        }

        private bool IsTrueForFilteredDateTime(
            ScheduledRoute scheduledRoute,
            string? inputedStartDateString,
            string? inputedEndDateString
        )
        {
            try
            {
                ScheduledTime scheduledTime = GetCurrentScheduledTimeOfScheduledRoute(
                    scheduledRoute
                );

                DateOnly day = DateOnly.Parse(scheduledTime.Day);
                //TimeOnly startTime = TimeOnly.Parse(scheduledTime.StartTime);
                //TimeOnly endTime = TimeOnly.Parse(scheduledTime.EndTime);

                DateOnly? inputedStartDate =
                    inputedStartDateString == null ? null : DateOnly.Parse(inputedStartDateString);
                DateOnly? inputedEndDate =
                    inputedEndDateString == null ? null : DateOnly.Parse(inputedEndDateString);

                return (inputedStartDate == null ? true : inputedStartDate <= day)
                    && (inputedEndDate == null ? true : inputedEndDate >= day);
            }
            catch
            {
                return false;
            }
        }

        public async Task<CommonResponse> GetScheduledRouteForUserAsync(
            Guid scheduledRouteId,
            Guid userId
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

                ScheduledRoute? scheduledRoute =
                    await _scheduledRouteRepository.FindScheduledRouteByIdForDetailAsync(
                        scheduledRouteId
                    );

                if (
                    scheduledRoute == null
                    || scheduledRoute.Status != ScheduledRouteStatus.PENDING
                        && scheduledRoute.UserId != userId
                )
                    return new CommonResponse
                    {
                        Status = 400,
                        Message = _config[
                            "ResponseMessages:ScheduledRouteMsg:ScheduledRouteNotFoundMsg"
                        ]
                    };

                scheduledRoute.ScheduledRouteDeliveryRequests =
                    scheduledRoute.ScheduledRouteDeliveryRequests
                        .Where(
                            srdr =>
                                srdr.Status == ScheduledRouteDeliveryRequestStatus.SCHEDULED
                                || srdr.Status
                                    == ScheduledRouteDeliveryRequestStatus.CANCELED_BY_CONTRIBUTOR
                        )
                        .OrderBy(srdr => srdr.Order)
                        .ToList();

                StockUpdatedHistoryType type = GetStockUpdatedHistoryTypeOfScheduledRoute(
                    scheduledRoute
                );

                foreach (
                    DeliveryRequest deliveryRequest in scheduledRoute.ScheduledRouteDeliveryRequests.Select(
                        srdr => srdr.DeliveryRequest
                    )
                )
                {
                    foreach (DeliveryItem deliveryItem in deliveryRequest.DeliveryItems)
                    {
                        if (deliveryItem.DonatedItem != null)

                            deliveryItem.DonatedItem.Item = (
                                await _itemRepository.FindItemByIdAsync(
                                    deliveryItem.DonatedItem.ItemId
                                )
                            )!;
                        else
                            deliveryItem.AidItem!.Item = (
                                await _itemRepository.FindItemByIdAsync(deliveryItem.AidItem.ItemId)
                            )!;
                    }
                }

                List<SimpleDeliveryRequestResponse> orderedDeliveryRequests =
                    new List<SimpleDeliveryRequestResponse>();

                foreach (
                    ScheduledRouteDeliveryRequest scheduledRouteDeliveryRequest in scheduledRoute.ScheduledRouteDeliveryRequests
                )
                {
                    if (type == StockUpdatedHistoryType.IMPORT)
                    {
                        orderedDeliveryRequests.Add(
                            await GetPickupSimpleDeliveryRequestResponse(
                                scheduledRouteDeliveryRequest.DeliveryRequest,
                                false
                            )
                        );
                    }
                    else
                    {
                        orderedDeliveryRequests.Add(
                            await GetDeliverySimpleDeliveryRequestResponse(
                                scheduledRouteDeliveryRequest.DeliveryRequest,
                                false
                            )
                        );
                    }
                }

                if (type == StockUpdatedHistoryType.IMPORT)
                {
                    orderedDeliveryRequests.Add(
                        GetMainBranchSimpleDeliveryRequestResponse(
                            scheduledRoute.ScheduledRouteDeliveryRequests[0].DeliveryRequest
                        )
                    );
                }
                else
                {
                    orderedDeliveryRequests.Insert(
                        0,
                        GetMainBranchSimpleDeliveryRequestResponse(
                            scheduledRoute.ScheduledRouteDeliveryRequests[0].DeliveryRequest
                        )
                    );
                }

                return new CommonResponse
                {
                    Status = 200,
                    Data = new ScheduledRouteDetailForUserResponse
                    {
                        Id = scheduledRoute.Id,
                        NumberOfDeliveryRequests = scheduledRoute
                            .ScheduledRouteDeliveryRequests
                            .Count,
                        ScheduledTime = GetCurrentScheduledTimeOfScheduledRoute(scheduledRoute),
                        OrderedDeliveryRequests = orderedDeliveryRequests,
                        TotalDistanceAsMeters = scheduledRoute.ScheduledRouteDeliveryRequests.Sum(
                            srdr => srdr.DistanceToReachThisOrNextAsMeters
                        ),
                        TotalTimeAsSeconds = scheduledRoute.ScheduledRouteDeliveryRequests.Sum(
                            srdr => srdr.TimeToReachThisOrNextAsSeconds
                        ),
                        BulkyLevel = GetBulkyLevel(
                                scheduledRoute.ScheduledRouteDeliveryRequests.Sum(
                                    srdr =>
                                        GetTotalTransportVolumnOfDeliveryRequest(
                                            srdr.DeliveryRequest
                                        )
                                ) * 100
                            )
                            .ToString(),
                        Type = type.ToString(),
                        Status = scheduledRoute.Status.ToString(),
                        CreatedDate = scheduledRoute.CreatedDate,
                        AcceptedDate = scheduledRoute.AcceptedDate,
                        FinishedDate = scheduledRoute.FinishedDate,
                        IsCancelable = IsCancelable(scheduledRoute)
                    },
                    Message = _config[
                        "ResponseMessages:ScheduledRouteMsg:GetScheduledRouteSuccessMsg"
                    ]
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    $"An exception occurred in service {nameof(ScheduledRouteService)}, method {nameof(GetScheduledRouteForUserAsync)}."
                );
                return new CommonResponse
                {
                    Status = 500,
                    Message = _config["ResponseMessages:CommonMsg:InternalServerErrorMsg"]
                };
            }
        }

        private bool IsCancelable(ScheduledRoute scheduledRoute)
        {
            return !scheduledRoute.ScheduledRouteDeliveryRequests.Any(
                d =>
                    d.DeliveryRequest.Status == DeliveryRequestStatus.COLLECTED
                    || d.DeliveryRequest.Status == DeliveryRequestStatus.ARRIVED_DELIVERY
                    || d.DeliveryRequest.Status == DeliveryRequestStatus.DELIVERED
            );
        }

        public async Task<CommonResponse> GetScheduledRouteForAdminAsync(
            Guid scheduledRouteId,
            Guid userId,
            string userRoleName
        )
        {
            try
            {
                Branch? branch = null;

                if (userRoleName == RoleEnum.BRANCH_ADMIN.ToString())
                {
                    branch = await _branchRepository.FindBranchByBranchAdminIdAsync(userId);
                    if (branch == null)

                        return new CommonResponse
                        {
                            Status = 403,
                            Message = _config["ResponseMessages:CommonMsg:UnauthenticationMsg"]
                        };
                }

                ScheduledRoute? scheduledRoute =
                    await _scheduledRouteRepository.FindScheduledRouteByIdForDetailForAdminAsync(
                        scheduledRouteId,
                        false
                    );

                if (scheduledRoute == null)
                    return new CommonResponse
                    {
                        Status = 400,
                        Message = _config[
                            "ResponseMessages:ScheduledRouteMsg:ScheduledRouteNotFoundMsg"
                        ]
                    };

                scheduledRoute.ScheduledRouteDeliveryRequests =
                    scheduledRoute.ScheduledRouteDeliveryRequests
                        .Where(
                            srdr =>
                                srdr.Status == ScheduledRouteDeliveryRequestStatus.SCHEDULED
                                || srdr.Status
                                    == ScheduledRouteDeliveryRequestStatus.CANCELED_BY_CONTRIBUTOR
                        )
                        .OrderBy(srdr => srdr.Order)
                        .ToList();

                if (
                    userRoleName == RoleEnum.BRANCH_ADMIN.ToString()
                    && branch != null
                    && GetMainBranchId(
                        scheduledRoute.ScheduledRouteDeliveryRequests[0].DeliveryRequest
                    ) != branch.Id
                )
                    return new CommonResponse
                    {
                        Status = 400,
                        Message = _config[
                            "ResponseMessages:ScheduledRouteMsg:ScheduledRouteNotFoundMsg"
                        ]
                    };

                //StockUpdatedHistoryType type = GetStockUpdatedHistoryTypeOfScheduledRoute(
                //    scheduledRoute
                //);

                //foreach (
                //    DeliveryRequest deliveryRequest in scheduledRoute.ScheduledRouteDeliveryRequests.Select(
                //        srdr => srdr.DeliveryRequest
                //    )
                //)
                //{
                //    foreach (DeliveryItem deliveryItem in deliveryRequest.DeliveryItems)
                //    {
                //        if (deliveryItem.DonatedItem != null)

                //            deliveryItem.DonatedItem.Item = (
                //                await _itemRepository.FindItemByIdAsync(
                //                    deliveryItem.DonatedItem.ItemId
                //                )
                //            )!;
                //        else
                //            deliveryItem.AidItem!.Item = (
                //                await _itemRepository.FindItemByIdAsync(deliveryItem.AidItem.ItemId)
                //            )!;
                //    }
                //}

                //scheduledRoute.ScheduledRouteDeliveryRequests =
                //    scheduledRoute.ScheduledRouteDeliveryRequests
                //        .Where(srdr => srdr.Status == ScheduledRouteDeliveryRequestStatus.SCHEDULED)
                //        .OrderBy(srdr => srdr.Order)
                //        .ToList();

                //List<SimpleDeliveryRequestResponse> orderedDeliveryRequests =
                //    new List<SimpleDeliveryRequestResponse>();

                //foreach (
                //    ScheduledRouteDeliveryRequest scheduledRouteDeliveryRequest in scheduledRoute.ScheduledRouteDeliveryRequests
                //)
                //{
                //    if (type == StockUpdatedHistoryType.IMPORT)
                //    {
                //        orderedDeliveryRequests.Add(
                //            await GetPickupSimpleDeliveryRequestResponse(
                //                scheduledRouteDeliveryRequest.DeliveryRequest,
                //                true
                //            )
                //        );
                //    }
                //    else
                //    {
                //        orderedDeliveryRequests.Add(
                //            await GetDeliverySimpleDeliveryRequestResponse(
                //                scheduledRouteDeliveryRequest.DeliveryRequest,
                //                true
                //            )
                //        );
                //    }
                //}

                //if (type == StockUpdatedHistoryType.IMPORT)
                //{
                //    orderedDeliveryRequests.Add(
                //        GetMainBranchSimpleDeliveryRequestResponse(
                //            scheduledRoute.ScheduledRouteDeliveryRequests[0].DeliveryRequest
                //        )
                //    );
                //}
                //else
                //{
                //    orderedDeliveryRequests.Insert(
                //        0,
                //        GetMainBranchSimpleDeliveryRequestResponse(
                //            scheduledRoute.ScheduledRouteDeliveryRequests[0].DeliveryRequest
                //        )
                //    );
                //}

                //scheduledRoute.User =
                //    scheduledRoute.UserId == null
                //        ? null
                //        : await _userRepository.FindUserByIdAsync((Guid)scheduledRoute.UserId);

                return new CommonResponse
                {
                    Status = 200,
                    Data = await GetScheduledRouteDetailForAdminResponse(scheduledRoute),
                    Message = _config[
                        "ResponseMessages:ScheduledRouteMsg:GetScheduledRouteSuccessMsg"
                    ]
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    $"An exception occurred in service {nameof(ScheduledRouteService)}, method {nameof(GetScheduledRouteForAdminAsync)}."
                );
                return new CommonResponse
                {
                    Status = 500,
                    Message = _config["ResponseMessages:CommonMsg:InternalServerErrorMsg"]
                };
            }
        }

        public async Task<CommonResponse> UpdateNextStatusOfDeliveryRequestsOfScheduledRouteAsync(
            Guid userId,
            Guid scheduledRouteId
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

                ScheduledRoute? scheduledRoute =
                    await _scheduledRouteRepository.FindProcessingScheduledRouteByIdAndUserIdAsync(
                        scheduledRouteId,
                        userId
                    );

                if (scheduledRoute == null)
                    return new CommonResponse
                    {
                        Status = 400,
                        Message = _config[
                            "ResponseMessages:ScheduledRouteMsg:AcceptedScheduledRouteNotFoundMsg"
                        ]
                    };

                List<DeliveryRequest> deliveryRequests =
                    scheduledRoute.ScheduledRouteDeliveryRequests
                        .Where(srdr => srdr.Status == ScheduledRouteDeliveryRequestStatus.SCHEDULED)
                        .Select(srdr => srdr.DeliveryRequest)
                        .ToList();

                DeliveryType? firstDeliveryTypeOfDeliveryRequest = null;

                if (deliveryRequests[0].DonatedRequestId != null)
                    firstDeliveryTypeOfDeliveryRequest = DeliveryType.DONATED_REQUEST_TO_BRANCH;
                else if (deliveryRequests[0].AidRequest!.CharityUnitId != null)
                    firstDeliveryTypeOfDeliveryRequest = DeliveryType.BRANCH_TO_AID_REQUEST;
                else
                    firstDeliveryTypeOfDeliveryRequest = DeliveryType.BRANCH_TO_BRANCH;

                if (
                    (
                        firstDeliveryTypeOfDeliveryRequest == DeliveryType.DONATED_REQUEST_TO_BRANCH
                        || firstDeliveryTypeOfDeliveryRequest == DeliveryType.BRANCH_TO_BRANCH
                    )
                    && !deliveryRequests.Any(
                        dr =>
                            dr.Status == DeliveryRequestStatus.COLLECTED
                            || dr.Status == DeliveryRequestStatus.ARRIVED_DELIVERY
                    )
                )
                    return new CommonResponse
                    {
                        Status = 400,
                        Message = _config[
                            "ResponseMessages:ScheduledRouteMsg:ScheduledRouteTypeImportMustHasAtLeastOneCollectedMsg"
                        ]
                    };

                foreach (DeliveryRequest deliveryRequest in deliveryRequests)
                {
                    if (
                        deliveryRequest.Status == DeliveryRequestStatus.ARRIVED_PICKUP
                        && firstDeliveryTypeOfDeliveryRequest == DeliveryType.BRANCH_TO_AID_REQUEST
                        && deliveryRequest.DeliveryItems
                            .Select(di => di.StockUpdatedHistoryDetails)
                            .All(suhds => suhds.All(suhd => suhd.StockId == null))
                    )
                        return new CommonResponse
                        {
                            Status = 400,
                            Message = _config[
                                "ResponseMessages:DeliveryRequestMsg:DeliveryRequestTypeExportMustBeExportedToUpdateStatusToCollectedMsg"
                            ]
                        };

                    deliveryRequest.Status =
                        CheckAndGetNextDeliveryRequestStatusForDeliveryRequestsOfScheduledRoute(
                            deliveryRequest.Status,
                            (DeliveryType)firstDeliveryTypeOfDeliveryRequest
                        );
                }

                using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
                {
                    if (
                        await _deliveryRequestRepository.UpdateDeliveryRequestsAsync(
                            deliveryRequests
                        ) != deliveryRequests.Count
                    )
                        return new CommonResponse
                        {
                            Status = 500,
                            Message = _config["ResponseMessages:CommonMsg:InternalServerErrorMsg"]
                        };

                    foreach (DeliveryRequest item in deliveryRequests)
                    {
                        await SendNotificationBaseOnDeliveryRequestStatusOfDeliveryRequest(item);
                    }

                    if (
                        (
                            firstDeliveryTypeOfDeliveryRequest
                                == DeliveryType.DONATED_REQUEST_TO_BRANCH
                            || firstDeliveryTypeOfDeliveryRequest == DeliveryType.BRANCH_TO_BRANCH
                        )
                        && deliveryRequests.Any(
                            dr => dr.Status == DeliveryRequestStatus.ARRIVED_DELIVERY
                        )
                    )
                    {
                        Notification notificationForAdmin = new Notification
                        {
                            Name =
                                "Có lịch trình vận chuyển vật phẩm từ người cho đã về tới chi nhánh.",
                            CreatedDate = SettedUpDateTime.GetCurrentVietNamTime(),
                            Image = _config["Notification:Image"],
                            Content =
                                "Có lịch trình vận chuyển vật phẩm từ người cho đã về tới chi nhánh. Hãy nhận vật phẩm được quyên góp từ tình nguyện viên vận chuyển và tiến hành nhập kho",
                            ReceiverId = deliveryRequests[0].Branch.BranchAdminId.ToString(),
                            Status = NotificationStatus.NEW,
                            Type = NotificationType.NOTIFYING,
                            DataType = DataNotificationType.SCHEDULED_ROUTE,
                            DataId = scheduledRoute.Id
                        };
                        await _notificationRepository.CreateNotificationAsync(notificationForAdmin);
                        await _hubContext.Clients.All.SendAsync(
                            deliveryRequests[0].Branch.BranchAdminId.ToString(),
                            notificationForAdmin
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
                    $"An exception occurred in service {nameof(ScheduledRouteService)}, method {nameof(UpdateNextStatusOfDeliveryRequestsOfScheduledRouteAsync)}."
                );
                return new CommonResponse
                {
                    Status = 500,
                    Message = _config["ResponseMessages:CommonMsg:InternalServerErrorMsg"]
                };
            }
        }

        private DeliveryRequestStatus CheckAndGetNextDeliveryRequestStatusForDeliveryRequestsOfScheduledRoute(
            DeliveryRequestStatus deliveryRequestStatus,
            DeliveryType firstDeliveryTypeOfDeliveryRequest
        )
        {
            if (
                firstDeliveryTypeOfDeliveryRequest == DeliveryType.DONATED_REQUEST_TO_BRANCH
                || firstDeliveryTypeOfDeliveryRequest == DeliveryType.BRANCH_TO_BRANCH
            )
            {
                if (
                    deliveryRequestStatus == DeliveryRequestStatus.COLLECTED
                    || deliveryRequestStatus == DeliveryRequestStatus.ARRIVED_DELIVERY
                )
                {
                    if (deliveryRequestStatus == DeliveryRequestStatus.COLLECTED)
                        return DeliveryRequestStatus.ARRIVED_DELIVERY;
                    else
                        return DeliveryRequestStatus.DELIVERED;
                }
                else
                    return deliveryRequestStatus;
            }
            else
            {
                if (
                    deliveryRequestStatus == DeliveryRequestStatus.SHIPPING
                    || deliveryRequestStatus == DeliveryRequestStatus.ARRIVED_PICKUP
                )
                {
                    if (deliveryRequestStatus == DeliveryRequestStatus.SHIPPING)
                        return DeliveryRequestStatus.ARRIVED_PICKUP;
                    else
                        return DeliveryRequestStatus.COLLECTED;
                }
                else
                    return deliveryRequestStatus;
            }
        }

        public async Task<CommonResponse> GetSampleGivingItemsToStartScheduledRouteAsync(
            Guid userId,
            Guid scheduledRouteId
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

                ScheduledRoute? scheduledRoute =
                    await _scheduledRouteRepository.FindScheduledRouteByIdForDetailForAdminAsync(
                        scheduledRouteId,
                        true
                    );

                if (
                    scheduledRoute == null
                    || scheduledRoute.Status != ScheduledRouteStatus.PROCESSING
                )
                    return new CommonResponse
                    {
                        Status = 400,
                        Message = _config[
                            "ResponseMessages:ScheduledRouteMsg:ProcessingScheduledRouteNotFound"
                        ]
                    };

                if (
                    GetMainBranchId(
                        scheduledRoute.ScheduledRouteDeliveryRequests[0].DeliveryRequest
                    ) != branch.Id
                )
                    return new CommonResponse
                    {
                        Status = 400,
                        Message = _config[
                            "ResponseMessages:ScheduledRouteMsg:ScheduledRouteNotBelongToYourBranch"
                        ]
                    };

                if (
                    GetStockUpdatedHistoryTypeOfScheduledRoute(scheduledRoute)
                    != StockUpdatedHistoryType.EXPORT
                )
                    return new CommonResponse
                    {
                        Status = 400,
                        Message = _config[
                            "ResponseMessages:ScheduledRouteMsg:ScheduledRouteIsNotBranchToAidRequestTypeTypeMsg"
                        ]
                    };

                DeliveryRequest deliveryRequest = scheduledRoute.ScheduledRouteDeliveryRequests[
                    0
                ].DeliveryRequest;

                //if (deliveryRequest.Status != DeliveryRequestStatus.ARRIVED_PICKUP)
                //    return new CommonResponse
                //    {
                //        Status = 400,
                //        Message = _config[
                //            "ResponseMessages:DeliveryRequestMsg:DeliveryRequestMustBeArrivedPickupToGiveItemsMsg"
                //        ]
                //    };

                List<StockUpdatedHistoryDetail> oldStockUpdatedHistoryDetails =
                    new List<StockUpdatedHistoryDetail>();

                foreach (DeliveryItem deliveryItem in deliveryRequest.DeliveryItems)
                {
                    oldStockUpdatedHistoryDetails.Add(deliveryItem.StockUpdatedHistoryDetails[0]);
                    deliveryItem.AidItem = await _aidItemRepository.FindAidItemByIdAsync(
                        (Guid)deliveryItem.AidItemId!
                    );
                }

                if (oldStockUpdatedHistoryDetails.Any(suhd => suhd.StockId != null))
                    return new CommonResponse
                    {
                        Status = 400,
                        Message = _config[
                            "ResponseMessages:ScheduledRouteMsg:ScheduledRouteAlreadyReceivedItemsMsg"
                        ]
                    };

                DateTime tmpDate = GetEndDateTimeFromScheduledTime(
                    GetLastScheduledTime(
                        JsonConvert.DeserializeObject<List<ScheduledTime>>(
                            deliveryRequest.ScheduledTimes
                        )!
                    )!
                );

                DateTime endOfLastScheduledTime = new DateTime(
                    tmpDate.Year,
                    tmpDate.Month,
                    tmpDate.Day
                );

                Dictionary<Guid, List<Stock>> currentStocks = new Dictionary<Guid, List<Stock>>();
                Guid stockUpdatedHistoryId = oldStockUpdatedHistoryDetails[0].StockUpdatedHistoryId;

                foreach (
                    Guid itemId in deliveryRequest.DeliveryItems.Select(di => di.AidItem!.ItemId)
                )
                {
                    List<Stock> stocks =
                        await _stockRepository.GetCurrentValidStocksByItemIdAndBranchId(
                            itemId,
                            branch.Id
                        );
                    currentStocks[itemId] = stocks
                        .Where(
                            s =>
                                new DateTime(
                                    s.ExpirationDate.Year,
                                    s.ExpirationDate.Month,
                                    s.ExpirationDate.Day
                                ) >= endOfLastScheduledTime.AddDays(1)
                        )
                        .OrderBy(s => s.ExpirationDate)
                        .ToList();
                }

                using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
                {
                    List<Stock> newStocks = new List<Stock>();

                    foreach (DeliveryItem deliveryItem in deliveryRequest.DeliveryItems)
                    {
                        List<StockUpdatedHistoryDetail> tmp = new List<StockUpdatedHistoryDetail>();

                        List<Stock> stocks = currentStocks[deliveryItem.AidItem!.ItemId];

                        double deliveryItemQuantityLeft = deliveryItem.Quantity;

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

                            tmp.Add(
                                new StockUpdatedHistoryDetail
                                {
                                    Quantity = consumedStock,
                                    StockUpdatedHistoryId = stockUpdatedHistoryId,
                                    DeliveryItemId = deliveryItem.Id,
                                    StockId = stock.Id,
                                    Stock = stock
                                }
                            );

                            if (deliveryItemQuantityLeft == 0)
                                break;
                        }

                        deliveryItem.StockUpdatedHistoryDetails = tmp;
                    }

                    scheduledRoute.ScheduledRouteDeliveryRequests[0].DeliveryRequest =
                        deliveryRequest;

                    return new CommonResponse
                    {
                        Status = 200,
                        Data = await GetScheduledRouteDetailForAdminResponse(scheduledRoute),
                        Message = _config[
                            "ResponseMessages:ScheduledRouteMsg:GetScheduledRouteSuccessMsg"
                        ]
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    $"An exception occurred in service {nameof(ScheduledRouteService)}, method {nameof(GetSampleGivingItemsToStartScheduledRouteAsync)}."
                );
            }
            return new CommonResponse
            {
                Status = 500,
                Message = _config["ResponseMessages:CommonMsg:InternalServerErrorMsg"]
            };
        }

        public async Task<CommonResponse> GiveItemsToStartScheduledRouteAsync(
            Guid userId,
            ExportStocksForDeliveryRequestConfirmingRequest exportStocksForDeliveryRequestConfirmingRequest
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

                ScheduledRoute? scheduledRoute =
                    await _scheduledRouteRepository.FindProcessingScheduledRouteByIdAsync(
                        exportStocksForDeliveryRequestConfirmingRequest.ScheduledRouteId
                    );

                if (
                    scheduledRoute == null
                    || scheduledRoute.Status != ScheduledRouteStatus.PROCESSING
                )
                    return new CommonResponse
                    {
                        Status = 400,
                        Message = _config[
                            "ResponseMessages:ScheduledRouteMsg:ProcessingScheduledRouteNotFound"
                        ]
                    };

                if (
                    GetMainBranchId(
                        scheduledRoute.ScheduledRouteDeliveryRequests[0].DeliveryRequest
                    ) != branch.Id
                )
                    return new CommonResponse
                    {
                        Status = 400,
                        Message = _config[
                            "ResponseMessages:ScheduledRouteMsg:ScheduledRouteNotBelongToYourBranch"
                        ]
                    };

                if (
                    GetStockUpdatedHistoryTypeOfScheduledRoute(scheduledRoute)
                    != StockUpdatedHistoryType.EXPORT
                )
                    return new CommonResponse
                    {
                        Status = 400,
                        Message = _config[
                            "ResponseMessages:ScheduledRouteMsg:ScheduledRouteIsNotBranchToAidRequestTypeTypeMsg"
                        ]
                    };

                DeliveryRequest deliveryRequest = scheduledRoute.ScheduledRouteDeliveryRequests[
                    0
                ].DeliveryRequest;

                if (deliveryRequest.Status != DeliveryRequestStatus.ARRIVED_PICKUP)
                    return new CommonResponse
                    {
                        Status = 400,
                        Message = _config[
                            "ResponseMessages:DeliveryRequestMsg:DeliveryRequestMustBeArrivedPickupToGiveItemsMsg"
                        ]
                    };

                List<StockUpdatedHistoryDetail> oldStockUpdatedHistoryDetails =
                    new List<StockUpdatedHistoryDetail>();

                DateTime tmpDate = GetEndDateTimeFromScheduledTime(
                    GetLastScheduledTime(
                        JsonConvert.DeserializeObject<List<ScheduledTime>>(
                            deliveryRequest.ScheduledTimes
                        )!
                    )!
                );

                DateTime endOfLastScheduledTime = new DateTime(
                    tmpDate.Year,
                    tmpDate.Month,
                    tmpDate.Day
                );

                Dictionary<Guid, List<Stock>> currentStocks = new Dictionary<Guid, List<Stock>>();

                foreach (DeliveryItem deliveryItem in deliveryRequest.DeliveryItems)
                {
                    oldStockUpdatedHistoryDetails.Add(deliveryItem.StockUpdatedHistoryDetails[0]);
                    deliveryItem.AidItem = await _aidItemRepository.FindAidItemByIdAsync(
                        (Guid)deliveryItem.AidItemId!
                    );

                    Guid itemId = deliveryItem.AidItem!.ItemId;

                    List<Stock> stocks =
                        await _stockRepository.GetCurrentValidStocksByItemIdAndBranchId(
                            itemId,
                            branch.Id
                        );
                    currentStocks[itemId] = stocks
                        .Where(
                            s =>
                                new DateTime(
                                    s.ExpirationDate.Year,
                                    s.ExpirationDate.Month,
                                    s.ExpirationDate.Day
                                ) >= endOfLastScheduledTime.AddDays(1)
                        )
                        .OrderBy(s => s.ExpirationDate)
                        .ToList();
                }

                if (oldStockUpdatedHistoryDetails.Any(suhd => suhd.StockId != null))
                    return new CommonResponse
                    {
                        Status = 400,
                        Message = _config[
                            "ResponseMessages:ScheduledRouteMsg:ScheduledRouteAlreadyReceivedItemsMsg"
                        ]
                    };

                Guid stockUpdatedHistoryId = oldStockUpdatedHistoryDetails[0].StockUpdatedHistoryId;
                StockUpdatedHistory stockUpdatedHistory = (
                    await _stockUpdatedHistoryRepository.FindStockUpdatedHistoryByIdAsync(
                        stockUpdatedHistoryId
                    )
                )!;

                stockUpdatedHistory.Note = exportStocksForDeliveryRequestConfirmingRequest.Note;
                stockUpdatedHistory.CreatedDate = SettedUpDateTime.GetCurrentVietNamTime();

                using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
                {
                    List<Stock> newStocks = new List<Stock>();
                    List<StockUpdatedHistoryDetail> newStockUpdatedHistoryDetails =
                        new List<StockUpdatedHistoryDetail>();

                    foreach (DeliveryItem deliveryItem in deliveryRequest.DeliveryItems)
                    {
                        List<Stock> stocks = currentStocks[deliveryItem.AidItem!.ItemId];

                        double deliveryItemQuantityLeft = deliveryItem.Quantity;

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

                            StockNoteRequest? exportedStocksRequest =
                                exportStocksForDeliveryRequestConfirmingRequest.NotesOfStockUpdatedHistoryDetails
                                == null
                                    ? null
                                    : exportStocksForDeliveryRequestConfirmingRequest.NotesOfStockUpdatedHistoryDetails.FirstOrDefault(
                                        s => s.StockId == stock.Id
                                    );

                            newStockUpdatedHistoryDetails.Add(
                                new StockUpdatedHistoryDetail
                                {
                                    Quantity = consumedStock,
                                    StockUpdatedHistoryId = stockUpdatedHistoryId,
                                    DeliveryItemId = deliveryItem.Id,
                                    StockId = stock.Id,
                                    Note =
                                        exportedStocksRequest == null
                                            ? null
                                            : exportedStocksRequest.Note,
                                    AidRequestId = oldStockUpdatedHistoryDetails[0].AidRequestId
                                }
                            );

                            if (deliveryItemQuantityLeft == 0)
                                break;
                        }
                    }

                    if (
                        await _stockUpdatedHistoryRepository.UpdateStockUpdatedHistoryAsync(
                            stockUpdatedHistory
                        ) != 1
                    )
                        return new CommonResponse
                        {
                            Status = 500,
                            Message = _config["ResponseMessages:CommonMsg:InternalServerErrorMsg"]
                        };

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

                    if (
                        await _stockUpdatedHistoryDetailRepository.DeleteStockUpdatedHistoryDetailsAsync(
                            oldStockUpdatedHistoryDetails
                        ) != oldStockUpdatedHistoryDetails.Count
                    )
                        return new CommonResponse
                        {
                            Status = 500,
                            Message = _config["ResponseMessages:CommonMsg:InternalServerErrorMsg"]
                        };

                    //await SendNotificationBaseOnDeliveryRequestStatusOfDeliveryRequest(
                    //    deliveryRequest
                    //);

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
                    $"An exception occurred in service {nameof(ScheduledRouteService)}, method {nameof(GiveItemsToStartScheduledRouteAsync)}."
                );
            }
            return new CommonResponse
            {
                Status = 500,
                Message = _config["ResponseMessages:CommonMsg:InternalServerErrorMsg"]
            };
        }

        private async Task<ScheduledRouteDetailForAdminResponse> GetScheduledRouteDetailForAdminResponse(
            ScheduledRoute scheduledRoute
        )
        {
            try
            {
                StockUpdatedHistoryType type = GetStockUpdatedHistoryTypeOfScheduledRoute(
                    scheduledRoute
                );

                foreach (
                    DeliveryRequest deliveryRequest in scheduledRoute.ScheduledRouteDeliveryRequests.Select(
                        srdr => srdr.DeliveryRequest
                    )
                )
                {
                    foreach (DeliveryItem deliveryItem in deliveryRequest.DeliveryItems)
                    {
                        if (deliveryItem.DonatedItem != null)

                            deliveryItem.DonatedItem.Item = (
                                await _itemRepository.FindItemByIdAsync(
                                    deliveryItem.DonatedItem.ItemId
                                )
                            )!;
                        else
                            deliveryItem.AidItem!.Item = (
                                await _itemRepository.FindItemByIdAsync(deliveryItem.AidItem.ItemId)
                            )!;
                    }
                }

                scheduledRoute.ScheduledRouteDeliveryRequests =
                    scheduledRoute.ScheduledRouteDeliveryRequests
                        .Where(
                            srdr =>
                                srdr.Status == ScheduledRouteDeliveryRequestStatus.SCHEDULED
                                || srdr.Status
                                    == ScheduledRouteDeliveryRequestStatus.CANCELED_BY_CONTRIBUTOR
                        )
                        .OrderBy(srdr => srdr.Order)
                        .ToList();

                List<SimpleDeliveryRequestResponse> orderedDeliveryRequests =
                    new List<SimpleDeliveryRequestResponse>();

                foreach (
                    ScheduledRouteDeliveryRequest scheduledRouteDeliveryRequest in scheduledRoute.ScheduledRouteDeliveryRequests
                )
                {
                    if (type == StockUpdatedHistoryType.IMPORT)
                    {
                        SimpleDeliveryRequestResponse simpleDeliveryRequestResponse =
                            await GetPickupSimpleDeliveryRequestResponse(
                                scheduledRouteDeliveryRequest.DeliveryRequest,
                                true
                            );

                        orderedDeliveryRequests.Add(simpleDeliveryRequestResponse);
                    }
                    else
                    {
                        SimpleDeliveryRequestResponse simpleDeliveryRequestResponse =
                            await GetDeliverySimpleDeliveryRequestResponse(
                                scheduledRouteDeliveryRequest.DeliveryRequest,
                                true
                            );

                        orderedDeliveryRequests.Add(simpleDeliveryRequestResponse);
                    }
                }

                if (type == StockUpdatedHistoryType.IMPORT)
                {
                    orderedDeliveryRequests.Add(
                        GetMainBranchSimpleDeliveryRequestResponse(
                            scheduledRoute.ScheduledRouteDeliveryRequests[0].DeliveryRequest
                        )
                    );
                }
                else
                {
                    orderedDeliveryRequests.Insert(
                        0,
                        GetMainBranchSimpleDeliveryRequestResponse(
                            scheduledRoute.ScheduledRouteDeliveryRequests[0].DeliveryRequest
                        )
                    );
                }

                scheduledRoute.User =
                    scheduledRoute.UserId == null
                        ? null
                        : await _userRepository.FindUserByIdAsync((Guid)scheduledRoute.UserId);

                return new ScheduledRouteDetailForAdminResponse
                {
                    Id = scheduledRoute.Id,
                    NumberOfDeliveryRequests = scheduledRoute.ScheduledRouteDeliveryRequests.Count,
                    ScheduledTime = GetCurrentScheduledTimeOfScheduledRoute(scheduledRoute),
                    OrderedDeliveryRequests = orderedDeliveryRequests,
                    TotalDistanceAsMeters = scheduledRoute.ScheduledRouteDeliveryRequests.Sum(
                        srdr => srdr.DistanceToReachThisOrNextAsMeters
                    ),
                    TotalTimeAsSeconds = scheduledRoute.ScheduledRouteDeliveryRequests.Sum(
                        srdr => srdr.TimeToReachThisOrNextAsSeconds
                    ),
                    BulkyLevel = GetBulkyLevel(
                            scheduledRoute.ScheduledRouteDeliveryRequests.Sum(
                                srdr =>
                                    GetTotalTransportVolumnOfDeliveryRequest(srdr.DeliveryRequest)
                            ) * 100
                        )
                        .ToString(),
                    Type = type.ToString(),
                    Status = scheduledRoute.Status.ToString(),
                    AcceptedUser =
                        scheduledRoute.User == null
                            ? null
                            : new SimpleUserResponse
                            {
                                Id = scheduledRoute.User.Id,
                                FullName = scheduledRoute.User.Name!,
                                Avatar = scheduledRoute.User.Avatar,
                                Phone = scheduledRoute.User.Phone,
                                Email = scheduledRoute.User.Email
                            },
                    CreatedDate = scheduledRoute.CreatedDate,
                    AcceptedDate = scheduledRoute.AcceptedDate,
                    FinishedDate = scheduledRoute.FinishedDate
                };
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task<CommonResponse> CancelScheduledRouteAsync(
            Guid userId,
            Guid scheduledRouteId
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

                ScheduledRoute? scheduledRoute =
                    await _scheduledRouteRepository.FindAcceptedAndProcessingScheduledRouteByUserIdAsync(
                        scheduledRouteId,
                        userId
                    );

                if (scheduledRoute == null)
                    return new CommonResponse
                    {
                        Status = 400,
                        Message = _config[
                            "ResponseMessages:ScheduledRouteMsg:AcceptedScheduledRouteNotFoundMsg"
                        ]
                    };

                scheduledRoute.ScheduledRouteDeliveryRequests =
                    scheduledRoute.ScheduledRouteDeliveryRequests
                        .Where(srdr => srdr.Status == ScheduledRouteDeliveryRequestStatus.SCHEDULED)
                        .ToList();

                List<ScheduledRouteDeliveryRequest> scheduledRouteDeliveryRequests =
                    scheduledRoute.ScheduledRouteDeliveryRequests;

                if (
                    scheduledRouteDeliveryRequests.Any(
                        d =>
                            d.DeliveryRequest.Status == DeliveryRequestStatus.COLLECTED
                            || d.DeliveryRequest.Status == DeliveryRequestStatus.ARRIVED_DELIVERY
                            || d.DeliveryRequest.Status == DeliveryRequestStatus.DELIVERED
                    )
                )
                    return new CommonResponse
                    {
                        Status = 400,
                        Message = _config[
                            "ResponseMessages:ScheduledRouteMsg:CanNotCancelScheduledRouteIfThereAreCollectedDeliveryRequestsMsg"
                        ]
                    };

                ScheduledTime scheduledTime = GetCurrentScheduledTimeOfScheduledRoute(
                    scheduledRoute
                );

                using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
                {
                    if (
                        SettedUpDateTime.GetCurrentVietNamTime()
                        < GetEndDateTimeFromScheduledTime(scheduledTime)
                    )
                    {
                        //clone scheduled route, scheduled route thành CANCELED, trung gian SCHEDULED thành CANCELED_BY_COLLABORATOR, delivery request thành PENDING

                        ScheduledRoute newScheduledRoute = new ScheduledRoute
                        {
                            Status = ScheduledRouteStatus.PENDING,
                            CreatedDate = SettedUpDateTime.GetCurrentVietNamTime(),
                            StartDate = scheduledRoute.StartDate,
                        };

                        if (
                            await _scheduledRouteRepository.AddScheduledRouteAsync(
                                newScheduledRoute
                            ) != 1
                        )
                            return new CommonResponse
                            {
                                Status = 500,
                                Message = _config[
                                    "ResponseMessages:CommonMsg:InternalServerErrorMsg"
                                ]
                            };

                        List<ScheduledRouteDeliveryRequest> newScheduledRouteDeliveryRequests =
                            scheduledRoute.ScheduledRouteDeliveryRequests
                                .Select(
                                    srdr =>
                                        new ScheduledRouteDeliveryRequest
                                        {
                                            ScheduledRouteId = newScheduledRoute.Id,
                                            DeliveryRequestId = srdr.DeliveryRequestId,
                                            Status = ScheduledRouteDeliveryRequestStatus.SCHEDULED,
                                            Order = srdr.Order,
                                            TimeToReachThisOrNextAsSeconds =
                                                srdr.TimeToReachThisOrNextAsSeconds,
                                            DistanceToReachThisOrNextAsMeters =
                                                srdr.DistanceToReachThisOrNextAsMeters
                                        }
                                )
                                .ToList();

                        if (
                            await _scheduledRouteDeliveryRequestRepository.AddScheduledRouteDeliveryRequestsAsync(
                                newScheduledRouteDeliveryRequests
                            ) != newScheduledRouteDeliveryRequests.Count
                        )
                            return new CommonResponse
                            {
                                Status = 500,
                                Message = _config[
                                    "ResponseMessages:CommonMsg:InternalServerErrorMsg"
                                ]
                            };

                        scheduledRoute.Status = ScheduledRouteStatus.CANCELED;

                        scheduledRouteDeliveryRequests.ForEach(s =>
                        {
                            s.Status = ScheduledRouteDeliveryRequestStatus.CANCELED_BY_CONTRIBUTOR;
                            s.DeliveryRequest.Status = DeliveryRequestStatus.PENDING;
                        });

                        if (
                            await _scheduledRouteRepository.UpdateScheduledRouteAsync(
                                scheduledRoute
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
                            await _scheduledRouteDeliveryRequestRepository.UpdateScheduledRouteDeliveryRequestsAsync(
                                scheduledRouteDeliveryRequests
                            ) != scheduledRouteDeliveryRequests.Count
                        )
                            return new CommonResponse
                            {
                                Status = 500,
                                Message = _config[
                                    "ResponseMessages:CommonMsg:InternalServerErrorMsg"
                                ]
                            };

                        if (
                            await _deliveryRequestRepository.UpdateDeliveryRequestsAsync(
                                scheduledRouteDeliveryRequests
                                    .Select(srdr => srdr.DeliveryRequest)
                                    .ToList()
                            ) != scheduledRouteDeliveryRequests.Count
                        )
                            return new CommonResponse
                            {
                                Status = 500,
                                Message = _config[
                                    "ResponseMessages:CommonMsg:InternalServerErrorMsg"
                                ]
                            };
                    }
                    else
                    {
                        scheduledRoute.Status = ScheduledRouteStatus.LATE;

                        scheduledRouteDeliveryRequests.ForEach(s =>
                        {
                            s.Status = ScheduledRouteDeliveryRequestStatus.CANCELED_BY_CONTRIBUTOR;
                            s.DeliveryRequest.Status = DeliveryRequestStatus.EXPIRED;
                        });

                        if (
                            await _scheduledRouteRepository.UpdateScheduledRouteAsync(
                                scheduledRoute
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
                            await _scheduledRouteDeliveryRequestRepository.UpdateScheduledRouteDeliveryRequestsAsync(
                                scheduledRouteDeliveryRequests
                            ) != scheduledRouteDeliveryRequests.Count
                        )
                            return new CommonResponse
                            {
                                Status = 500,
                                Message = _config[
                                    "ResponseMessages:CommonMsg:InternalServerErrorMsg"
                                ]
                            };

                        if (
                            await _deliveryRequestRepository.UpdateDeliveryRequestsAsync(
                                scheduledRouteDeliveryRequests
                                    .Select(srdr => srdr.DeliveryRequest)
                                    .ToList()
                            ) != scheduledRouteDeliveryRequests.Count
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
                            "ResponseMessages:DeliveryRequestMsg:UpdateDeliverySuccessMsg"
                        ]
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    $"An exception occurred in service {nameof(ScheduledRouteService)}, method {nameof(CancelScheduledRouteAsync)}."
                );
                return new CommonResponse
                {
                    Status = 500,
                    Message = _config["ResponseMessages:CommonMsg:InternalServerErrorMsg"]
                };
            }
        }

        public async Task AutoCheckLateScheduleRoute()
        {
            try
            {
                List<ScheduledRoute>? scheduledRoutes =
                    await _scheduledRouteRepository.FindAcceptedAndProcessingScheduledRoutedAsync();
                if (scheduledRoutes != null && scheduledRoutes.Count > 0)
                {
                    foreach (ScheduledRoute scheduledRoute in scheduledRoutes)
                    {
                        if (DateTime.Now > scheduledRoute.StartDate.Date.AddDays(1).AddSeconds(-1))
                        {
                            scheduledRoute.Status = ScheduledRouteStatus.LATE;

                            List<ScheduledRouteDeliveryRequest> neededToUpdateScheduledRouteDeliveryRequests =
                                new List<ScheduledRouteDeliveryRequest>();

                            List<DeliveryRequest> neededToUpdateDeliveryRequests =
                                new List<DeliveryRequest>();

                            foreach (
                                ScheduledRouteDeliveryRequest scheduledRouteDeliveryRequest in scheduledRoute.ScheduledRouteDeliveryRequests.Where(
                                    srdr =>
                                        srdr.Status == ScheduledRouteDeliveryRequestStatus.SCHEDULED
                                )
                            )
                            {
                                scheduledRouteDeliveryRequest.Status =
                                    ScheduledRouteDeliveryRequestStatus.CANCELED_BY_CONTRIBUTOR;

                                scheduledRouteDeliveryRequest.DeliveryRequest.Status =
                                    DeliveryRequestStatus.PENDING;

                                neededToUpdateScheduledRouteDeliveryRequests.Add(
                                    scheduledRouteDeliveryRequest
                                );

                                neededToUpdateDeliveryRequests.Add(
                                    scheduledRouteDeliveryRequest.DeliveryRequest
                                );
                            }

                            await _scheduledRouteRepository.UpdateScheduledRouteAsync(
                                scheduledRoute
                            );

                            await _scheduledRouteDeliveryRequestRepository.UpdateScheduledRouteDeliveryRequestsAsync(
                                neededToUpdateScheduledRouteDeliveryRequests
                            );

                            await _deliveryRequestRepository.UpdateDeliveryRequestsAsync(
                                neededToUpdateDeliveryRequests
                            );
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    $"An exception occurred in service {nameof(ScheduledRouteService)}, method {nameof(CancelScheduledRouteAsync)}."
                );
            }
        }
    }

    public class GroupOfDeliveryRequestWithSamePartOfScheduledTime
    {
        public ScheduledTime ScheduledTime { get; set; }

        public List<DeliveryRequest> DeliveryRequests { get; set; }

        public int NumberOfVehicles { get; set; }
    }

    public class GroupOfDeliveryRequestOfRouteWithTimeAndDistance
    {
        public List<DeliveryRequest> DeliveryRequests { get; set; }

        public List<double> TimesAsSeconds { get; set; }

        public List<double> DistancesAsMeters { get; set; }
    }
}
