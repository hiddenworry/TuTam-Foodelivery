using BusinessLogic.Utils.FirebaseService;
using BusinessLogic.Utils.Notification.Implements;
using BusinessLogic.Utils.OpenRouteService;
using DataAccess.Entities;
using DataAccess.EntityEnums;
using DataAccess.Models.Requests;
using DataAccess.Models.Requests.Enum;
using DataAccess.Models.Requests.ModelBinders;
using DataAccess.Models.Requests.OpenRouteService.Response;
using DataAccess.Models.Responses;
using DataAccess.ModelsEnum;
using DataAccess.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Transactions;

namespace BusinessLogic.Services.Implements
{
    public class DonatedRequestService : IDonatedRequestService
    {
        private readonly IDonatedRequestRepository _donatedRequestRepository;
        private readonly IActivityRepository _activityRepository;
        private readonly ITargetProcessRepository _targetProcessRepository;
        private readonly IConfiguration _config;
        private readonly IItemRepository _itemRepository;
        private readonly IDonatedItemRepository _donatedItemRepository;
        private readonly IOpenRouteService _openRouteService;
        private readonly IAcceptableDonatedRequestRepository _acceptableDonatedRequestRepository;
        private readonly IBranchRepository _branchRepository;
        private readonly ILogger<DonatedRequestService> _logger;
        private readonly IFirebaseStorageService _firebaseStorageService;
        private readonly IStockUpdatedHistoryRepository _stockUpdatedHistoryRepository;
        private readonly INotificationRepository _notificationRepository;
        private readonly IHubContext<NotificationSignalSender> _hubContext;
        private readonly IFirebaseNotificationService _firebaseNotificationService;
        private readonly IUserRepository _userRepository;

        public DonatedRequestService(
            IDonatedRequestRepository donatedRequestRepository,
            IActivityRepository activityRepository,
            ITargetProcessRepository targetProcessRepository,
            IConfiguration config,
            IItemRepository itemRepository,
            IDonatedItemRepository donatedItemRepository,
            IOpenRouteService openRouteService,
            IAcceptableDonatedRequestRepository acceptableDonatedRequestRepository,
            IBranchRepository branchRepository,
            ILogger<DonatedRequestService> logger,
            IFirebaseStorageService firebaseStorageService,
            IStockUpdatedHistoryRepository stockUpdatedHistoryRepository,
            INotificationRepository notificationRepository,
            IHubContext<NotificationSignalSender> hubContext,
            IFirebaseNotificationService firebaseNotificationService,
            IUserRepository userRepository
        )
        {
            _donatedRequestRepository = donatedRequestRepository;
            _activityRepository = activityRepository;
            _targetProcessRepository = targetProcessRepository;
            _config = config;
            _itemRepository = itemRepository;
            _donatedItemRepository = donatedItemRepository;
            _openRouteService = openRouteService;
            _acceptableDonatedRequestRepository = acceptableDonatedRequestRepository;
            _branchRepository = branchRepository;
            _logger = logger;
            _firebaseStorageService = firebaseStorageService;
            _stockUpdatedHistoryRepository = stockUpdatedHistoryRepository;
            _notificationRepository = notificationRepository;
            _hubContext = hubContext;
            _firebaseNotificationService = firebaseNotificationService;
            _userRepository = userRepository;
        }

        public async Task<CommonResponse> CreateDonatedRequestAsync(
            DonatedRequestCreatingRequest donatedRequestCreatingRequest,
            Guid userId
        )
        {
            try
            {
                List<Branch> joinedBranches = new List<Branch>();
                Activity? activity = null;

                if (donatedRequestCreatingRequest.ActivityId != null)
                {
                    activity = await _activityRepository.FindActivityByIdAsync(
                        donatedRequestCreatingRequest.ActivityId.Value
                    );
                    if (activity == null || activity.Scope == ActivityScope.INTERNAL)
                        return new CommonResponse
                        {
                            Status = 400,
                            Message = _config["ResponseMessages:ActivityMsg:ActivityNotFoundMsg"]
                        };

                    //foreach (
                    //    ScheduledTime scheduledTime in donatedRequestCreatingRequest.ScheduledTimes
                    //)
                    //{
                    //    DateOnly day = DateOnly.Parse(scheduledTime.Day);
                    //    TimeOnly endTime = TimeOnly.Parse(scheduledTime.EndTime);
                    //    if (
                    //        activity.Status == ActivityStatus.ENDED
                    //        && day.ToDateTime(endTime) > activity.EndDate
                    //    )
                    //        return new CommonResponse
                    //        {
                    //            Status = 400,
                    //            Message = _config[
                    //                "ResponseMessages:DonatedRequestMsg:DonatedTimeMustBeforeEstimatedEndDateOfActivityMsg"
                    //            ]
                    //        };
                    //}

                    if (activity.Status != ActivityStatus.STARTED)
                        return new CommonResponse
                        {
                            Status = 400,
                            Message = _config[
                                "ResponseMessages:ActivityMsg:ActivityIsNotStartedMsg"
                            ]
                        };

                    List<TargetProcess> targetProcesses = activity.TargetProcesses;

                    if (
                        !donatedRequestCreatingRequest.DonatedItemRequests
                            .Select(dir => dir.ItemTemplateId)
                            .All(id => targetProcesses.Select(tp => tp.ItemId).Contains(id))
                    )
                        return new CommonResponse
                        {
                            Status = 400,
                            Message = _config[
                                "ResponseMessages:DonatedRequestMsg:NeededItemNotFoundInActivityMsg"
                            ]
                        };
                    joinedBranches = activity.ActivityBranches.Select(ab => ab.Branch).ToList();
                }

                DonatedRequest donatedRequest = new DonatedRequest
                {
                    CreatedDate = SettedUpDateTime.GetCurrentVietNamTime(),
                    Images = "",
                    Address = donatedRequestCreatingRequest.Address,
                    ConfirmedDate =
                        donatedRequestCreatingRequest.ActivityId != null
                            ? SettedUpDateTime.GetCurrentVietNamTime()
                            : null,
                    Location = string.Join(",", donatedRequestCreatingRequest.Location),
                    ScheduledTimes = JsonConvert.SerializeObject(
                        donatedRequestCreatingRequest.ScheduledTimes
                    ),
                    Status =
                        donatedRequestCreatingRequest.ActivityId != null
                            ? DonatedRequestStatus.ACCEPTED
                            : DonatedRequestStatus.PENDING,
                    Note = donatedRequestCreatingRequest.Note,
                    UserId = userId,
                    ActivityId = donatedRequestCreatingRequest.ActivityId
                };
                using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
                {
                    if (
                        await _donatedRequestRepository.CreateDonatedRequestAsync(donatedRequest)
                        != 1
                    )
                        return new CommonResponse
                        {
                            Status = 500,
                            Message = _config["ResponseMessages:CommonMsg:InternalServerErrorMsg"]
                        };

                    double totalVolume = 0;
                    List<DonatedItem> donatedItems = new List<DonatedItem>();
                    foreach (
                        DonatedItemRequest donatedItemRequest in donatedRequestCreatingRequest.DonatedItemRequests
                    )
                    {
                        Item? itemTemplate = await _itemRepository.FindItemByIdAsync(
                            donatedItemRequest.ItemTemplateId
                        );
                        if (itemTemplate == null
                        //|| itemTemplate.Status == ItemStatus.INACTIVE
                        //|| itemTemplate.ItemTemplate == null
                        //|| itemTemplate.ItemTemplate.Status == ItemTemplateStatus.INACTIVE
                        )
                        {
                            return new CommonResponse
                            {
                                Status = 400,
                                Message = _config[
                                    "ResponseMessages:ItemTemplateMsg:ItemTemplateNotFoundInListMsg"
                                ]
                            };
                        }

                        //if (
                        //    donatedItemRequest.InitialExpirationDate
                        //    < GetValidExpirationDate(itemTemplate.EstimatedExpirationDays)
                        //)
                        //{
                        //    string itemTemplateName =
                        //        itemTemplate.ItemTemplate.Name
                        //        + $" ({string.Join(", ", itemTemplate.ItemAttributeValues.Select(itav => itav.AttributeValue.Value))})";
                        //    return new CommonResponse
                        //    {
                        //        Status = 400,
                        //        Message =
                        //            $"Vật phẩm {itemTemplateName} phải có hạn sử dụng ước tính ít nhất từ {(int)Math.Ceiling((double)itemTemplate.EstimatedExpirationDays / 2)} ngày sau tính từ thời điểm hiện tại."
                        //    };
                        //}

                        totalVolume +=
                            (donatedItemRequest.Quantity / itemTemplate.MaximumTransportVolume)
                            * 100;

                        donatedItems.Add(
                            new DonatedItem
                            {
                                Quantity = donatedItemRequest.Quantity,
                                InitialExpirationDate = donatedItemRequest.InitialExpirationDate,
                                Status =
                                    donatedRequestCreatingRequest.ActivityId != null
                                        ? DonatedItemStatus.ACCEPTED
                                        : DonatedItemStatus.WAITING,
                                DonatedRequestId = donatedRequest.Id,
                                ItemId = donatedItemRequest.ItemTemplateId
                            }
                        );
                    }

                    if (totalVolume < double.Parse(_config["DonatedRequest:MinVolumeToDonate"]))
                        return new CommonResponse
                        {
                            Status = 400,
                            Message = _config[
                                "ResponseMessages:DonatedRequestMsg:ItemVolumeBelowLimitsMsg"
                            ]
                        };

                    if (totalVolume > double.Parse(_config["DonatedRequest:MaxVolumeToDonate"]))
                        return new CommonResponse
                        {
                            Status = 400,
                            Message = _config[
                                "ResponseMessages:DonatedRequestMsg:ItemVolumeExceedLimitsMsg"
                            ]
                        };

                    if (
                        await _donatedItemRepository.CreateDonatedItemsAsync(donatedItems)
                        != donatedItems.Count
                    )
                        return new CommonResponse
                        {
                            Status = 500,
                            Message = _config["ResponseMessages:CommonMsg:InternalServerErrorMsg"]
                        };

                    try
                    {
                        DeliverableBranches deliverableBranches =
                            await _openRouteService.GetDeliverableBranchesByUserLocation(
                                donatedRequest.Location,
                                donatedRequestCreatingRequest.ActivityId == null
                                    ? null
                                    : joinedBranches,
                                null
                            );

                        if (deliverableBranches.NearestBranch == null)
                            return new CommonResponse
                            {
                                Status = 400,
                                Message =
                                    _config["ResponseMessages:BranchMsg:NearbyBranchesNotFound"]
                                    + $"({double.Parse(_config["OpenRoute:MaxRoadLengthAsMeters"]) / 1000} km)"
                            };

                        if (donatedRequestCreatingRequest.ActivityId == null)
                        {
                            List<Branch> nearbyBranches =
                                deliverableBranches.NearbyBranches.Count > 0
                                    ? deliverableBranches.NearbyBranches
                                    : new List<Branch> { deliverableBranches.NearestBranch };

                            List<AcceptableDonatedRequest> acceptableDonatedRequests =
                                nearbyBranches
                                    .Select(
                                        branch =>
                                            new AcceptableDonatedRequest
                                            {
                                                DonatedRequestId = donatedRequest.Id,
                                                BranchId = branch.Id,
                                                CreatedDate =
                                                    SettedUpDateTime.GetCurrentVietNamTime(),
                                                Status = AcceptableDonatedRequestStatus.PENDING
                                            }
                                    )
                                    .ToList();

                            if (
                                await _acceptableDonatedRequestRepository.CreateAcceptableDonatedRequestsAsync(
                                    acceptableDonatedRequests
                                ) != acceptableDonatedRequests.Count
                            )
                            {
                                return new CommonResponse
                                {
                                    Status = 500,
                                    Message = _config[
                                        "ResponseMessages:CommonMsg:InternalServerErrorMsg"
                                    ]
                                };
                            }

                            foreach (Branch branch in nearbyBranches)
                            {
                                Notification notification = new Notification
                                {
                                    Name = _config[
                                        "ResponseMessages:NotificationMsg:NotificationTitleForNewDonatedRequestMsg"
                                    ],
                                    CreatedDate = SettedUpDateTime.GetCurrentVietNamTime(),
                                    Image = _config["Notification:Image"],
                                    Content = _config[
                                        "ResponseMessages:NotificationMsg:NotificationContentForNewDonatedRequestMsg"
                                    ],
                                    ReceiverId = branch.BranchAdminId!.ToString(),
                                    Status = NotificationStatus.NEW,
                                    Type = NotificationType.NOTIFYING,
                                    DataType = DataNotificationType.DONATED_REQUEST,
                                    DataId = donatedRequest.Id
                                };
                                await _notificationRepository.CreateNotificationAsync(notification);
                                await _hubContext.Clients.All.SendAsync(
                                    branch.BranchAdminId!.ToString(),
                                    notification
                                );
                            }
                        }
                        else
                        {
                            AcceptableDonatedRequest acceptableDonatedRequest =
                                new AcceptableDonatedRequest
                                {
                                    DonatedRequestId = donatedRequest.Id,
                                    BranchId = deliverableBranches.NearestBranch.Id,
                                    CreatedDate = SettedUpDateTime.GetCurrentVietNamTime(),
                                    ConfirmedDate = SettedUpDateTime.GetCurrentVietNamTime(),
                                    Status = AcceptableDonatedRequestStatus.ACCEPTED
                                };

                            if (
                                await _acceptableDonatedRequestRepository.CreateAcceptableDonatedRequestAsync(
                                    acceptableDonatedRequest
                                ) != 1
                            )
                            {
                                return new CommonResponse
                                {
                                    Status = 500,
                                    Message = _config[
                                        "ResponseMessages:CommonMsg:InternalServerErrorMsg"
                                    ]
                                };
                            }

                            Notification notificationForAdmin = new Notification
                            {
                                Name = "Có 1 yêu cầu quyên góp mới.",
                                CreatedDate = SettedUpDateTime.GetCurrentVietNamTime(),
                                Image = _config["Notification:Image"],
                                Content =
                                    $"Có 1 yêu cầu quyên góp mới cho hoạt động {activity!.Name}.",
                                ReceiverId =
                                    deliverableBranches.NearestBranch.BranchAdminId.ToString(),
                                Status = NotificationStatus.NEW,
                                Type = NotificationType.NOTIFYING,
                                DataType = DataNotificationType.DONATED_REQUEST,
                                DataId = donatedRequest.Id
                            };
                            await _notificationRepository.CreateNotificationAsync(
                                notificationForAdmin
                            );
                            await _hubContext.Clients.All.SendAsync(
                                deliverableBranches.NearestBranch.BranchAdminId!.ToString(),
                                notificationForAdmin
                            );

                            User user = (await _userRepository.FindUserByIdAsync(userId))!;

                            Notification notificationForUser = new Notification
                            {
                                Name = "Quyên góp thành công.",
                                CreatedDate = SettedUpDateTime.GetCurrentVietNamTime(),
                                Image = _config["Notification:Image"],
                                Content =
                                    $"Bạn đã quyên góp thành công cho hoạt động {activity!.Name}",
                                ReceiverId = userId.ToString(),
                                Status = NotificationStatus.NEW,
                                Type = NotificationType.NOTIFYING,
                                DataType = DataNotificationType.DONATED_REQUEST,
                                DataId = donatedRequest.Id
                            };
                            await _notificationRepository.CreateNotificationAsync(
                                notificationForUser
                            );
                            await _hubContext.Clients.All.SendAsync(
                                userId.ToString(),
                                notificationForUser
                            );
                            if (user.DeviceToken != null)
                            {
                                PushNotificationRequest pushNotificationRequest =
                                    new PushNotificationRequest
                                    {
                                        DeviceToken = user.DeviceToken,
                                        Message = "Quyên góp thành công.",
                                        Title = _config[
                                            "ResponseMessages:NotificationMsg:NotificationTitleForAcceptedDonatedRequestMsg"
                                        ]
                                    };
                                await _firebaseNotificationService.PushNotification(
                                    pushNotificationRequest
                                );
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(
                            ex,
                            "An exception occurred in service DonatedRequestService, method CreateDonatedRequestAsync."
                        );
                        return new CommonResponse
                        {
                            Status = 400,
                            Message = _config["ResponseMessages:CommonMsg:LocationNotValid"]
                        };
                    }

                    List<string> imageUrls = new List<string>();
                    try
                    {
                        foreach (IFormFile image in donatedRequestCreatingRequest.Images)
                        {
                            using (var stream = image.OpenReadStream())
                            {
                                string imageName =
                                    Guid.NewGuid().ToString() + Path.GetExtension(image.FileName);
                                string imageUrl =
                                    await _firebaseStorageService.UploadImageToFirebase(
                                        stream,
                                        imageName
                                    );
                                imageUrls.Add(imageUrl);
                            }
                        }
                        donatedRequest.Images = string.Join(",", imageUrls);
                        if (
                            await _donatedRequestRepository.UpdateDonatedRequestAsync(
                                donatedRequest
                            ) != 1
                        )
                        {
                            return new CommonResponse
                            {
                                Status = 500,
                                Message = _config[
                                    "ResponseMessages:CommonMsg:InternalServerErrorMsg"
                                ]
                            };
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(
                            ex,
                            "An exception occurred in service DonatedRequestService, method CreateDonatedRequestAsync."
                        );
                        imageUrls.ForEach(url => _firebaseStorageService.DeleteImageAsync(url));
                        return new CommonResponse
                        {
                            Status = 500,
                            Message = _config["ResponseMessages:CommonMsg:UploadImageFailedMsg"]
                        };
                    }
                    scope.Complete();
                    return new CommonResponse
                    {
                        Status = 200,
                        Message =
                            donatedRequestCreatingRequest.ActivityId != null
                                ? _config[
                                    "ResponseMessages:DonatedRequestMsg:CreateAndAcceptDonatedRequestSuccess"
                                ]
                                : _config[
                                    "ResponseMessages:DonatedRequestMsg:CreateDonatedRequestSuccess"
                                ]
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "An exception occurred in service DonatedRequestService, method CreateDonatedRequestAsync."
                );
                return new CommonResponse
                {
                    Status = 500,
                    Message =
                        _config["ResponseMessages:CommonMsg:InternalServerErrorMsg"]
                        + $" {ex.Message}"
                };
            }
        }

        public async Task<CommonResponse> GetDonatedRequestsAsync(
            DonatedRequestStatus? status,
            DateTime? startDate,
            DateTime? endDate,
            Guid? callerId,
            Guid? branchId,
            Guid? userId,
            Guid? activityId,
            int? pageSize,
            int? page,
            string? orderBy,
            SortType? sortType,
            string? userRoleName
        )
        {
            string UnauthenticationMsg = _config[
                "ResponseMessages:AuthenticationMsg:UnauthenticationMsg"
            ];
            if (callerId == null)
                return new CommonResponse { Status = 403, Message = UnauthenticationMsg };
            if (userRoleName == RoleEnum.BRANCH_ADMIN.ToString())
            {
                Branch? branch = await _branchRepository.FindBranchByBranchAdminIdAsync(
                    (Guid)callerId
                );
                if (branch == null)
                {
                    return new CommonResponse { Status = 403, Message = UnauthenticationMsg };
                }
                branchId = branch.Id;
            }
            else if (userRoleName == RoleEnum.CONTRIBUTOR.ToString())
            {
                if (userId != null)
                    return new CommonResponse { Status = 403, Message = UnauthenticationMsg };

                userId = callerId;
            }
            else if (userRoleName != RoleEnum.SYSTEM_ADMIN.ToString())
                return new CommonResponse { Status = 403, Message = UnauthenticationMsg };

            List<DonatedRequest> donatedRequests =
                await _donatedRequestRepository.GetDonatedRequestsAsync(
                    status,
                    startDate,
                    endDate,
                    branchId,
                    userId,
                    activityId
                );

            if (
                orderBy != null
                && sortType != null
                && (sortType == SortType.ASC || sortType == SortType.DES)
            )
            {
                if (sortType == SortType.ASC)
                    donatedRequests = donatedRequests
                        .OrderBy(donatedRequest => GetPropertyValue(donatedRequest, orderBy))
                        .ToList();
                else
                    donatedRequests = donatedRequests
                        .OrderByDescending(
                            donatedRequest => GetPropertyValue(donatedRequest, orderBy)
                        )
                        .ToList();
            }

            Pagination pagination = new Pagination();
            pagination.PageSize = pageSize == null ? 10 : pageSize.Value;
            pagination.CurrentPage = page == null ? 1 : page.Value;
            pagination.Total = donatedRequests.Count;
            donatedRequests = donatedRequests
                .Skip((pagination.CurrentPage - 1) * pagination.PageSize)
                .Take(pagination.PageSize)
                .ToList();

            if (branchId != null)
            {
                foreach (DonatedRequest donatedRequest in donatedRequests)
                {
                    if (
                        donatedRequest.AcceptableDonatedRequests.Any(
                            adr =>
                                adr.BranchId == branchId
                                && adr.Status == AcceptableDonatedRequestStatus.REJECTED
                        )
                    )
                    {
                        donatedRequest.Status = DonatedRequestStatus.REJECTED;
                    }
                }
            }

            return new CommonResponse
            {
                Status = 200,
                Data =
                    userRoleName == RoleEnum.SYSTEM_ADMIN.ToString()
                    || userRoleName == RoleEnum.BRANCH_ADMIN.ToString()
                        ? donatedRequests.Select(
                            dr =>
                                new DonatedRequestForAdminResponse
                                {
                                    Id = dr.Id,
                                    Address = dr.Address,
                                    Location = _openRouteService.GetCoordinatesByLocation(
                                        dr.Location
                                    ),
                                    CreatedDate = dr.CreatedDate,
                                    AcceptedDate = dr.ConfirmedDate,
                                    ScheduledTimes = JsonConvert.DeserializeObject<
                                        List<ScheduledTime>
                                    >(dr.ScheduledTimes),
                                    Status = dr.Status.ToString(),
                                    SimpleBranchResponse = GetAcceptingSimpleBranchResponse(
                                        dr.AcceptableDonatedRequests
                                    ),
                                    SimpleUserResponse = new SimpleUserResponse
                                    {
                                        Id = dr.User.Id,
                                        FullName = dr.User.Name!,
                                        Avatar = dr.User.Avatar,
                                        Role = dr.User.Role.DisplayName
                                    },
                                    SimpleActivityResponse =
                                        dr.Activity != null
                                            ? new SimpleActivityResponse
                                            {
                                                Id = dr.Activity.Id,
                                                Name = dr.Activity.Name
                                            }
                                            : null
                                }
                        )
                        : donatedRequests.Select(
                            dr =>
                                new DonatedRequestForUserResponse
                                {
                                    Id = dr.Id,
                                    Address = dr.Address,
                                    Images = dr.Images.Split(",").ToList(),
                                    Location = _openRouteService.GetCoordinatesByLocation(
                                        dr.Location
                                    ),
                                    CreatedDate = dr.CreatedDate,
                                    AcceptedDate = dr.ConfirmedDate,
                                    ScheduledTimes = JsonConvert.DeserializeObject<
                                        List<ScheduledTime>
                                    >(dr.ScheduledTimes),
                                    Status = dr.Status.ToString(),
                                    SimpleBranchResponse = GetAcceptingSimpleBranchResponse(
                                        dr.AcceptableDonatedRequests
                                    ),
                                    SimpleActivityResponse =
                                        dr.Activity != null
                                            ? new SimpleActivityResponse
                                            {
                                                Id = dr.Activity.Id,
                                                Name = dr.Activity.Name
                                            }
                                            : null,
                                    DonatedItemResponses = dr.DonatedItems
                                        .Select(
                                            di =>
                                                new DonatedItemResponse
                                                {
                                                    Id = di.Id,
                                                    Quantity = di.Quantity,
                                                    ImportedQuantity = di.DeliveryItems
                                                        .Select(
                                                            di =>
                                                                di.StockUpdatedHistoryDetails
                                                                    .Select(sud => sud.Quantity)
                                                                    .Sum()
                                                        )
                                                        .Sum(),
                                                    Status = di.Status.ToString(),
                                                    InitialExpirationDate =
                                                        di.InitialExpirationDate,
                                                    ItemTemplateResponse = new ItemResponse
                                                    {
                                                        Id = di.Item.Id,
                                                        Name = di.Item.ItemTemplate.Name,
                                                        AttributeValues =
                                                            di.Item.ItemAttributeValues
                                                                .Select(
                                                                    itav =>
                                                                        itav.AttributeValue.Value
                                                                )
                                                                .ToList(),
                                                        Image = di.Item.Image,
                                                        MaximumTransportVolume =
                                                            di.Item.MaximumTransportVolume,
                                                        Unit = di.Item.ItemTemplate.Unit.Name
                                                    }
                                                }
                                        )
                                        .ToList()
                                }
                        ),
                Pagination = pagination,
                Message = _config["ResponseMessages:DonatedRequestMsg:GetDonatedRequestsSuccessMsg"]
            };
        }

        //private string GetFullNameOfItem(Item item)
        //{
        //    return $"{item.ItemTemplate.Name}"
        //        + (
        //            item.ItemAttributeValues.Count > 0
        //                ? $" {string.Join(", ", item.ItemAttributeValues.Select(iav => iav.AttributeValue.Value))}"
        //                : ""
        //        );
        //}

        private SimpleBranchResponse? GetAcceptingSimpleBranchResponse(
            List<AcceptableDonatedRequest> acceptableDonatedRequests
        )
        {
            AcceptableDonatedRequest? acceptableDonatedRequest =
                acceptableDonatedRequests.FirstOrDefault(
                    adr => adr.Status == AcceptableDonatedRequestStatus.ACCEPTED
                );

            if (acceptableDonatedRequest == null)
                return null;

            return new SimpleBranchResponse
            {
                Id = acceptableDonatedRequest.Branch.Id,
                Name = acceptableDonatedRequest.Branch.Name,
                Image = acceptableDonatedRequest.Branch.Image
            };
        }

        private BranchResponse? GetAcceptingBranchResponse(
            List<AcceptableDonatedRequest> acceptableDonatedRequests
        )
        {
            AcceptableDonatedRequest? acceptableDonatedRequest =
                acceptableDonatedRequests.FirstOrDefault(
                    adr => adr.Status == AcceptableDonatedRequestStatus.ACCEPTED
                );

            if (acceptableDonatedRequest == null)
                return null;

            return new BranchResponse
            {
                Id = acceptableDonatedRequest.Branch.Id,
                Name = acceptableDonatedRequest.Branch.Name,
                Address = acceptableDonatedRequest.Branch.Address,
                Image = acceptableDonatedRequest.Branch.Image,
                CreatedDate = acceptableDonatedRequest.Branch.CreatedDate,
                Status = acceptableDonatedRequest.Branch.Status.ToString(),
                RejectingReason = acceptableDonatedRequest.RejectingReason
            };
        }

        static object? GetPropertyValue(object obj, string propertyName)
        {
            var propertyInfo = obj.GetType().GetProperty(propertyName);
            if (propertyInfo != null)
                return propertyInfo.GetValue(obj);
            else
                throw new Exception(
                    $"Property {propertyName} not found in object type {obj.GetType}."
                );
        }

        public async Task<CommonResponse> GetDonatedRequestAsync(
            Guid id,
            Guid? userId,
            string? userRoleName
        )
        {
            string UnauthenticationMsg = _config[
                "ResponseMessages:AuthenticationMsg:UnauthenticationMsg"
            ];
            bool isConfirmable = false;
            DonatedRequest? donatedRequest =
                await _donatedRequestRepository.FindDonatedRequestByIdForDetailAsync(id);
            if (donatedRequest == null)
                return new CommonResponse
                {
                    Status = 400,
                    Message = _config["ResponseMessages:DonatedRequestMsg:DonatedRequestNotFound"]
                };

            if (
                userId == null
                || userRoleName != RoleEnum.SYSTEM_ADMIN.ToString()
                    && userRoleName != RoleEnum.BRANCH_ADMIN.ToString()
                    && userRoleName != RoleEnum.CONTRIBUTOR.ToString()
            )
                return new CommonResponse { Status = 403, Message = UnauthenticationMsg };
            else if (userRoleName == RoleEnum.BRANCH_ADMIN.ToString())
            {
                Branch? branch = await _branchRepository.FindBranchByBranchAdminIdAsync(
                    (Guid)userId
                );
                if (branch == null)
                    return new CommonResponse { Status = 403, Message = UnauthenticationMsg };
                AcceptableDonatedRequest? acceptableDonatedRequest =
                    await _acceptableDonatedRequestRepository.FindPendingAcceptableDonatedRequestByDonatedRequestIdAndBranchIdAsync(
                        donatedRequest.Id,
                        branch.Id
                    );
                if (acceptableDonatedRequest != null)
                    isConfirmable = true;

                if (
                    donatedRequest.AcceptableDonatedRequests.Any(
                        adr =>
                            adr.BranchId == branch.Id
                            && adr.Status == AcceptableDonatedRequestStatus.REJECTED
                    )
                )
                {
                    donatedRequest.Status = DonatedRequestStatus.REJECTED;
                }
            }
            else if (userRoleName == RoleEnum.CONTRIBUTOR.ToString())
            {
                if (userId != donatedRequest.UserId)
                    return new CommonResponse { Status = 403, Message = UnauthenticationMsg };
            }
            else if (userRoleName != RoleEnum.SYSTEM_ADMIN.ToString())
                return new CommonResponse { Status = 403, Message = UnauthenticationMsg };

            return new CommonResponse
            {
                Status = 200,
                Data = new DonatedRequestDetailResponse
                {
                    Id = donatedRequest.Id,
                    Address = donatedRequest.Address,
                    Location = _openRouteService.GetCoordinatesByLocation(donatedRequest.Location),
                    IsConfirmable = isConfirmable,
                    CreatedDate = donatedRequest.CreatedDate,
                    AcceptedDate = donatedRequest.ConfirmedDate,
                    Images = donatedRequest.Images.Split(",").ToList(),
                    ScheduledTimes = JsonConvert.DeserializeObject<List<ScheduledTime>>(
                        donatedRequest.ScheduledTimes
                    ),
                    Status = donatedRequest.Status.ToString(),
                    Note = donatedRequest.Note,
                    AcceptedBranch = GetAcceptingBranchResponse(
                        donatedRequest.AcceptableDonatedRequests
                    ),
                    DonatedItemResponses = donatedRequest.DonatedItems
                        .Select(
                            di =>
                                new DonatedItemResponse
                                {
                                    Id = di.Id,
                                    Quantity = di.Quantity,
                                    ImportedQuantity = di.DeliveryItems
                                        .Select(
                                            di =>
                                                di.StockUpdatedHistoryDetails
                                                    .Select(sud => sud.Quantity)
                                                    .Sum()
                                        )
                                        .Sum(),
                                    Status = di.Status.ToString(),
                                    InitialExpirationDate = di.InitialExpirationDate,
                                    ItemTemplateResponse = new ItemResponse
                                    {
                                        Id = di.Item.Id,
                                        Name = di.Item.ItemTemplate.Name,
                                        AttributeValues = di.Item.ItemAttributeValues
                                            .Select(itav => itav.AttributeValue.Value)
                                            .ToList(),
                                        Image = di.Item.Image,
                                        MaximumTransportVolume = di.Item.MaximumTransportVolume,
                                        Unit = di.Item.ItemTemplate.Unit.Name
                                    }
                                }
                        )
                        .ToList(),
                    SimpleUserResponse = new SimpleUserResponse
                    {
                        FullName = donatedRequest.User.Name!,
                        Avatar = donatedRequest.User.Avatar,
                        Id = donatedRequest.User.Id,
                        Role = donatedRequest.User.Role.DisplayName,
                        Phone = donatedRequest.User.Phone,
                        Email = donatedRequest.User.Email,
                        Status = donatedRequest.User.Status.ToString()
                    },
                    RejectingBranchResponses =
                        userRoleName == RoleEnum.CONTRIBUTOR.ToString()
                            ? donatedRequest.Status == DonatedRequestStatus.REJECTED
                                ? donatedRequest.AcceptableDonatedRequests
                                    .Where(
                                        adr => adr.Status == AcceptableDonatedRequestStatus.REJECTED
                                    )
                                    .Select(
                                        aar =>
                                            new RejectingBranchResponse
                                            {
                                                Id = aar.Branch.Id,
                                                Name = aar.Branch.Name,
                                                Address = aar.Branch.Address,
                                                Image = aar.Branch.Image,
                                                CreatedDate = aar.Branch.CreatedDate,
                                                Status = aar.Branch.Status.ToString(),
                                                RejectingReason = aar.RejectingReason
                                            }
                                    )
                                    .ToList()
                                : null
                            : donatedRequest.AcceptableDonatedRequests
                                .Where(adr => adr.Status == AcceptableDonatedRequestStatus.REJECTED)
                                .Select(
                                    aar =>
                                        new RejectingBranchResponse
                                        {
                                            Id = aar.Branch.Id,
                                            Name = aar.Branch.Name,
                                            Address = aar.Branch.Address,
                                            Image = aar.Branch.Image,
                                            CreatedDate = aar.Branch.CreatedDate,
                                            Status = aar.Branch.Status.ToString(),
                                            RejectingReason = aar.RejectingReason
                                        }
                                )
                                .ToList(),
                    SimpleActivityResponse =
                        donatedRequest.Activity != null
                            ? new SimpleActivityResponse
                            {
                                Id = donatedRequest.Activity.Id,
                                Name = donatedRequest.Activity.Name
                            }
                            : null
                },
                Message = _config["ResponseMessages:DonatedRequestMsg:GetDonatedRequestSuccessMsg"]
            };
        }

        private DateTime GetValidExpirationDate(int estimatedExpirationDays)
        {
            DateTime current = SettedUpDateTime.GetCurrentVietNamTime();
            return new DateTime(current.Year, current.Month, current.Day).AddDays(
                (int)Math.Ceiling((double)estimatedExpirationDays / 2)
            );
        }

        public async Task<CommonResponse> CountDonatedRequestByAllStatus(
            DateTime? startDate,
            DateTime? endDate,
            Guid? branchId,
            Guid? userId,
            Guid? activtyId,
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
                int total = await _donatedRequestRepository.CountDonatedRequestByStatus(
                    null,
                    startDate,
                    endDate,
                    branchId,
                    userId,
                    activtyId
                );
                int numberOfPending = await _donatedRequestRepository.CountDonatedRequestByStatus(
                    DonatedRequestStatus.PENDING,
                    startDate,
                    endDate,
                    branchId,
                    userId,
                    activtyId
                );
                int numberOfAccepted = await _donatedRequestRepository.CountDonatedRequestByStatus(
                    DonatedRequestStatus.ACCEPTED,
                    startDate,
                    endDate,
                    branchId,
                    userId,
                    activtyId
                );
                int numberOfRejected = await _donatedRequestRepository.CountDonatedRequestByStatus(
                    DonatedRequestStatus.REJECTED,
                    startDate,
                    endDate,
                    branchId,
                    userId,
                    activtyId
                );
                int numberOfCanceled = await _donatedRequestRepository.CountDonatedRequestByStatus(
                    DonatedRequestStatus.CANCELED,
                    startDate,
                    endDate,
                    branchId,
                    userId,
                    activtyId
                );
                int numberOfExpried = await _donatedRequestRepository.CountDonatedRequestByStatus(
                    DonatedRequestStatus.EXPIRED,
                    startDate,
                    endDate,
                    branchId,
                    userId,
                    activtyId
                );
                int numberOfProcessing =
                    await _donatedRequestRepository.CountDonatedRequestByStatus(
                        DonatedRequestStatus.PROCESSING,
                        startDate,
                        endDate,
                        branchId,
                        userId,
                        activtyId
                    );
                int numberOfFinished = await _donatedRequestRepository.CountDonatedRequestByStatus(
                    DonatedRequestStatus.FINISHED,
                    startDate,
                    endDate,
                    branchId,
                    userId,
                    activtyId
                );
                //int numberOfSelfShipping = _stockUpdatedHistoryRepository.CountDirectDonationAsync(
                //    startDate,
                //    endDate
                //);
                //int numberOfReported = await _donatedRequestRepository.CountDonatedRequestByStatus(
                //    DonatedRequestStatus.REPORTED,
                //    startDate,
                //    endDate
                //);

                var rs = new
                {
                    NumberOfPending = numberOfPending,
                    //NumberOfReported = numberOfReported,
                    NumberOfAccepted = numberOfAccepted,
                    NumberOfCanceled = numberOfCanceled,
                    NumberOfProcessing = numberOfProcessing,
                    NumberOfRejected = numberOfRejected,
                    //NumberOfSelfShipping = numberOfSelfShipping,
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
                    $"An exception occurred in service {nameof(DonatedRequestService)}, method {nameof(CountDonatedRequestByAllStatus)}."
                );
                return new CommonResponse
                {
                    Status = 500,
                    Message = _config["ResponseMessages:CommonMsg:InternalServerErrorMsg"]
                };
            }
            return commonResponse;
        }

        public async Task<CommonResponse> CountDonatedRequestByStatus(
            DateTime startDate,
            DateTime endDate,
            DonatedRequestStatus? status,
            TimeFrame timeFrame,
            Guid? branchId,
            Guid? userId,
            Guid? activityId,
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
                int total = await _donatedRequestRepository.CountDonatedRequestByStatus(
                    status,
                    startDate,
                    endDate,
                    branchId,
                    userId,
                    activityId
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
                            tmp.Quantity =
                                await _donatedRequestRepository.CountDonatedRequestByStatus(
                                    status,
                                    tmp.From,
                                    tmp.To,
                                    branchId,
                                    userId,
                                    activityId
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
                            tmp.Quantity =
                                await _donatedRequestRepository.CountDonatedRequestByStatus(
                                    status,
                                    tmp.From,
                                    tmp.To,
                                    branchId,
                                    userId,
                                    activityId
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
                            tmp.Quantity =
                                await _donatedRequestRepository.CountDonatedRequestByStatus(
                                    status,
                                    tmp.From,
                                    tmp.To,
                                    branchId,
                                    userId,
                                    activityId
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
                            tmp.Quantity =
                                await _donatedRequestRepository.CountDonatedRequestByStatus(
                                    status,
                                    tmp.From,
                                    tmp.To,
                                    branchId,
                                    userId,
                                    activityId
                                );
                            responses.Add(tmp);
                        }
                        break;
                    default:
                        break;
                }
                var rs = new { Total = total, DonatedRequestByTimeRangeResponse = responses };
                commonResponse.Data = rs;
                commonResponse.Status = 200;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    $"An exception occurred in service {nameof(ScheduledRouteService)}, method {nameof(CountDonatedRequestByStatus)}."
                );
                return new CommonResponse
                {
                    Status = 500,
                    Message = _config["ResponseMessages:CommonMsg:InternalServerErrorMsg"]
                };
            }
            return commonResponse;
        }

        public async Task<CommonResponse> CancelDonatedRequest(Guid donatedRequestId, Guid userId)
        {
            CommonResponse commonResponse = new CommonResponse();
            string UnauthenticationMsg = _config[
                "ResponseMessages:AuthenticationMsg:UnauthenticationMsg"
            ];
            try
            {
                DonatedRequest? donatedRequest =
                    await _donatedRequestRepository.FindDonatedRequestByIdAsync(donatedRequestId);

                if (donatedRequest == null)
                {
                    return new CommonResponse
                    {
                        Status = 400,
                        Message = _config[
                            "ResponseMessages:DonatedRequestMsg:DonatedRequestNotFound"
                        ]
                    };
                }
                else if (
                    donatedRequest.Status != DonatedRequestStatus.PENDING
                    && donatedRequest.Status != DonatedRequestStatus.ACCEPTED
                )
                {
                    return new CommonResponse
                    {
                        Status = 400,
                        Message = "Bạn chỉ có thể hủy yêu cầu quyên góp khi ở trạng thái đang chờ."
                    };
                }
                else if (donatedRequest.UserId != userId)
                {
                    return new CommonResponse { Status = 403, Message = UnauthenticationMsg };
                }
                else
                {
                    donatedRequest.Status = DonatedRequestStatus.CANCELED;
                    int rs = await _donatedRequestRepository.UpdateDonatedRequestAsync(
                        donatedRequest
                    );
                    if (rs < 0)
                        throw new Exception();
                    commonResponse.Status = 200;
                    commonResponse.Message = "Cập nhật thành công";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    $"An exception occurred in service {nameof(ScheduledRouteService)}, method {nameof(CancelDonatedRequest)}."
                );
                return new CommonResponse
                {
                    Status = 500,
                    Message = _config["ResponseMessages:CommonMsg:InternalServerErrorMsg"]
                };
            }
            return commonResponse;
        }

        public async Task UpdateOutDateDonatedRequestsAsync()
        {
            try
            {
                List<DonatedRequest> donatedRequests =
                    await _donatedRequestRepository.FindPendingAndAcceptedAndProcessingDonatedRequestsAsync();

                List<DonatedRequest> aboutToOutDateDonatedRequests = donatedRequests
                    .Where(
                        dr =>
                            dr.AcceptableDonatedRequests.Any(
                                adr => adr.Status == AcceptableDonatedRequestStatus.ACCEPTED
                            )
                    )
                    .Where(dr =>
                    {
                        ScheduledTime? scheduledTime = GetLastAvailabeScheduledTime(
                            JsonConvert.DeserializeObject<List<ScheduledTime>>(dr.ScheduledTimes)!
                        );

                        if (scheduledTime == null)
                            return false;

                        double daysLeft = (
                            GetEndDateTimeFromScheduledTime(scheduledTime)
                            - SettedUpDateTime.GetCurrentVietNamTime()
                        ).TotalDays;

                        return daysLeft <= 1;
                    })
                    .ToList();

                List<DonatedRequest> outDateDonatedRequests = donatedRequests
                    .Where(
                        dr =>
                            dr.AcceptableDonatedRequests.Any(
                                adr => adr.Status == AcceptableDonatedRequestStatus.ACCEPTED
                            )
                    )
                    .Where(
                        dr =>
                            GetLastAvailabeScheduledTime(
                                JsonConvert.DeserializeObject<List<ScheduledTime>>(
                                    dr.ScheduledTimes
                                )!
                            ) == null
                    )
                    .ToList();

                outDateDonatedRequests.ForEach(dr => dr.Status = DonatedRequestStatus.EXPIRED);

                using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
                {
                    foreach (DonatedRequest donatedRequest in aboutToOutDateDonatedRequests)
                    {
                        Notification notification = new Notification
                        {
                            Name = "Yêu cầu quyên góp sắp hết hạn.",
                            CreatedDate = SettedUpDateTime.GetCurrentVietNamTime(),
                            Image = _config["Notification:Image"],
                            Content = "Chi nhánh có yêu cầu quyên góp sắp hết hạn cần được xử lý.",
                            ReceiverId = donatedRequest.AcceptableDonatedRequests[
                                0
                            ].Branch.BranchAdminId.ToString(),
                            Status = NotificationStatus.NEW,
                            Type = NotificationType.NOTIFYING,
                            DataType = DataNotificationType.DONATED_REQUEST,
                            DataId = donatedRequest.Id
                        };
                        await _notificationRepository.CreateNotificationAsync(notification);
                        await _hubContext.Clients.All.SendAsync(
                            donatedRequest.AcceptableDonatedRequests[
                                0
                            ].Branch.BranchAdminId.ToString(),
                            notification
                        );
                    }
                    foreach (DonatedRequest donatedRequest in outDateDonatedRequests)
                    {
                        if (
                            await _donatedRequestRepository.UpdateDonatedRequestAsync(
                                donatedRequest
                            ) == 1
                        )
                        {
                            Notification notificationForUser = new Notification
                            {
                                Name = "Yêu cầu quyên góp đã hết hạn.",
                                CreatedDate = SettedUpDateTime.GetCurrentVietNamTime(),
                                Image = _config["Notification:Image"],
                                Content =
                                    "Yêu cầu quyên góp của bạn đã hết hạn vì không có tình nguyện viên nào có thể vận chuyển.",
                                ReceiverId = donatedRequest.UserId.ToString(),
                                Status = NotificationStatus.NEW,
                                Type = NotificationType.NOTIFYING,
                                DataType = DataNotificationType.DONATED_REQUEST,
                                DataId = donatedRequest.Id
                            };
                            await _notificationRepository.CreateNotificationAsync(
                                notificationForUser
                            );
                            await _hubContext.Clients.All.SendAsync(
                                donatedRequest.UserId.ToString(),
                                notificationForUser
                            );
                            if (donatedRequest.User.DeviceToken != null)
                            {
                                PushNotificationRequest pushNotificationRequest =
                                    new PushNotificationRequest
                                    {
                                        DeviceToken = donatedRequest.User.DeviceToken,
                                        Message =
                                            "Yêu cầu quyên góp của bạn đã hết hạn vì không có tình nguyện viên nào có thể vận chuyển.",
                                        Title = "Yêu cầu quyên góp đã hết hạn."
                                    };
                                await _firebaseNotificationService.PushNotification(
                                    pushNotificationRequest
                                );
                            }

                            if (donatedRequest.AcceptableDonatedRequests.Count() > 0)
                            {
                                Notification notificationForAdmin = new Notification
                                {
                                    Name = "Yêu cầu quyên góp đã hết hạn.",
                                    CreatedDate = SettedUpDateTime.GetCurrentVietNamTime(),
                                    Image = _config["Notification:Image"],
                                    Content =
                                        "Yêu cầu quyên góp mà chi nhánh đảm nhận đã hết hạn vì không có tình nguyện viên nào có thể vận chuyển.",
                                    ReceiverId = donatedRequest.AcceptableDonatedRequests[
                                        0
                                    ].Branch.BranchAdminId.ToString(),
                                    Status = NotificationStatus.NEW,
                                    Type = NotificationType.NOTIFYING,
                                    DataType = DataNotificationType.DONATED_REQUEST,
                                    DataId = donatedRequest.Id
                                };
                                await _notificationRepository.CreateNotificationAsync(
                                    notificationForAdmin
                                );
                                await _hubContext.Clients.All.SendAsync(
                                    donatedRequest.AcceptableDonatedRequests[
                                        0
                                    ].Branch.BranchAdminId.ToString(),
                                    notificationForAdmin
                                );
                            }
                        }
                    }
                    scope.Complete();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    $"An exception occurred in service {nameof(DonatedRequestService)}, method {nameof(UpdateOutDateDonatedRequestsAsync)}."
                );
            }
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
    }
}
