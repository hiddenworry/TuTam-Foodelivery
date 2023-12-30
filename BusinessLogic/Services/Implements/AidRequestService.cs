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
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Transactions;

namespace BusinessLogic.Services.Implements
{
    public class AidRequestService : IAidRequestService
    {
        private readonly IAidRequestRepository _aidRequestRepository;
        private readonly IUserRepository _userRepository;
        private readonly ICharityUnitRepository _charityUnitRepository;
        private readonly IConfiguration _config;
        private readonly IItemRepository _itemRepository;
        private readonly IAidItemRepository _aidItemRepository;
        private readonly IOpenRouteService _openRouteService;
        private readonly IAcceptableAidRequestRepository _acceptableAidRequestRepository;
        private readonly ILogger<AidRequestService> _logger;
        private readonly IBranchRepository _branchRepository;
        private readonly INotificationRepository _notificationRepository;
        private readonly IHubContext<NotificationSignalSender> _hubContext;
        private readonly IFirebaseNotificationService _firebaseNotificationService;

        public AidRequestService(
            IAidRequestRepository aidRequestRepository,
            IUserRepository userRepository,
            ICharityUnitRepository charityUnitRepository,
            IConfiguration config,
            IItemRepository itemRepository,
            IAidItemRepository aidItemRepository,
            IOpenRouteService openRouteService,
            IAcceptableAidRequestRepository acceptableAidRequestRepository,
            ILogger<AidRequestService> logger,
            IBranchRepository branchRepository,
            INotificationRepository notificationRepository,
            IHubContext<NotificationSignalSender> hubContext,
            IFirebaseNotificationService firebaseNotificationService
        )
        {
            _aidRequestRepository = aidRequestRepository;
            _userRepository = userRepository;
            _charityUnitRepository = charityUnitRepository;
            _config = config;
            _itemRepository = itemRepository;
            _aidItemRepository = aidItemRepository;
            _openRouteService = openRouteService;
            _acceptableAidRequestRepository = acceptableAidRequestRepository;
            _logger = logger;
            _branchRepository = branchRepository;
            _notificationRepository = notificationRepository;
            _hubContext = hubContext;
            _firebaseNotificationService = firebaseNotificationService;
        }

        public async Task<CommonResponse> CreateAidRequestAsync(
            AidRequestCreatingRequest aidRequestCreatingRequest,
            Guid userId,
            string userRoleName
        )
        {
            if (userRoleName == RoleEnum.CHARITY.ToString())
                return await CreateAidRequestByCharityUnitAsync(aidRequestCreatingRequest, userId);
            else if (userRoleName == RoleEnum.BRANCH_ADMIN.ToString())
                return await CreateAidRequestByBranchAsync(aidRequestCreatingRequest, userId);
            else
                return new CommonResponse
                {
                    Status = 403,
                    Message = _config["ResponseMessages:AuthenticationMsg:UnauthenticationMsg"]
                };
        }

        public async Task<CommonResponse> CreateAidRequestByCharityUnitAsync(
            AidRequestCreatingRequest aidRequestCreatingRequest,
            Guid userId
        )
        {
            try
            {
                CharityUnit? charityUnit =
                    await _charityUnitRepository.FindCharityUnitByUserIdAsync(userId);

                if (
                    charityUnit == null
                    || charityUnit.Status != CharityUnitStatus.ACTIVE
                    || charityUnit.Charity == null
                    || charityUnit.Charity.Status != CharityStatus.ACTIVE
                )
                {
                    return new CommonResponse
                    {
                        Status = 403,
                        Message = _config[
                            "ResponseMessages:CharityUnitMsg:CharityOrItsUnitNotFoundOrInactiveMsg"
                        ]
                    };
                }

                using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
                {
                    AidRequest aidRequest =
                        new()
                        {
                            CreatedDate = SettedUpDateTime.GetCurrentVietNamTime(),
                            Address = charityUnit.Address,
                            Location = charityUnit.Location,
                            ScheduledTimes = JsonConvert.SerializeObject(
                                aidRequestCreatingRequest.ScheduledTimes
                            ),
                            Status = AidRequestStatus.PENDING,
                            IsSelfShipping = aidRequestCreatingRequest.IsSelfShipping,
                            Note = aidRequestCreatingRequest.Note,
                            CharityUnitId = charityUnit.Id
                        };

                    if (await _aidRequestRepository.CreateAidRequestAsync(aidRequest) != 1)
                        return new CommonResponse
                        {
                            Status = 500,
                            Message = _config["ResponseMessages:CommonMsg:InternalServerErrorMsg"]
                        };

                    List<AidItem> aidItems = new();
                    foreach (
                        AidItemRequest aidItemRequest in aidRequestCreatingRequest.AidItemRequests
                    )
                    {
                        Item? itemTemplate = await _itemRepository.FindItemByIdAsync(
                            aidItemRequest.ItemTemplateId
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
                        aidItems.Add(
                            new AidItem
                            {
                                AidRequestId = aidRequest.Id,
                                ItemId = aidItemRequest.ItemTemplateId,
                                Quantity = aidItemRequest.Quantity,
                                Status = AidItemStatus.WAITING
                            }
                        );
                    }

                    if (
                        await _aidItemRepository.CreateAidItemsAsync(aidItems)
                        != aidRequestCreatingRequest.AidItemRequests.Count
                    )
                        return new CommonResponse
                        {
                            Status = 500,
                            Message = _config["ResponseMessages:CommonMsg:InternalServerErrorMsg"]
                        };

                    DeliverableBranches deliverableBranches =
                        await _openRouteService.GetDeliverableBranchesByCharityUnitLocation(
                            charityUnit.Location
                        );

                    if (deliverableBranches.NearestBranch == null)
                        return new CommonResponse
                        {
                            Status = 400,
                            Message =
                                _config["ResponseMessages:BranchMsg:NearbyBranchesNotFound"]
                                + $"({double.Parse(_config["OpenRoute:MaxRoadLengthAsMeters"]) / 1000} km)"
                        };

                    List<Branch> nearbyBranches =
                        deliverableBranches.NearbyBranches.Count > 0
                            ? deliverableBranches.NearbyBranches
                            : new List<Branch> { deliverableBranches.NearestBranch };

                    List<AcceptableAidRequest> acceptableAidRequests = nearbyBranches
                        .Select(
                            branch =>
                                new AcceptableAidRequest
                                {
                                    AidRequestId = aidRequest.Id,
                                    BranchId = branch.Id,
                                    CreatedDate = SettedUpDateTime.GetCurrentVietNamTime()
                                }
                        )
                        .ToList();

                    if (
                        await _acceptableAidRequestRepository.CreateAcceptableAidRequestsAsync(
                            acceptableAidRequests
                        ) != acceptableAidRequests.Count
                    )
                        return new CommonResponse
                        {
                            Status = 500,
                            Message = _config["ResponseMessages:CommonMsg:InternalServerErrorMsg"]
                        };

                    foreach (Branch branch in nearbyBranches)
                    {
                        Notification notification =
                            new()
                            {
                                Name = "Có 1 yêu cầu hỗ trợ vật phẩm gần chi nhánh.",
                                CreatedDate = SettedUpDateTime.GetCurrentVietNamTime(),
                                Image = _config["Notification:Image"],
                                Content =
                                    "Có 1 yêu cầu hỗ trợ vật phẩm mới gần chi nhánh mà bạn có thể chấp nhận.",
                                ReceiverId = branch.BranchAdminId!.ToString(),
                                Status = NotificationStatus.NEW,
                                Type = NotificationType.NOTIFYING,
                                DataType = DataNotificationType.AID_REQUEST,
                                DataId = aidRequest.Id
                            };
                        await _notificationRepository.CreateNotificationAsync(notification);
                        await _hubContext.Clients.All.SendAsync(
                            branch.BranchAdminId!.ToString(),
                            notification
                        );
                    }

                    scope.Complete();
                    return new CommonResponse
                    {
                        Status = 200,
                        Message = _config["ResponseMessages:AidRequest:CreateAidRequestSuccess"]
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    $"An exception occurred in service {nameof(AidRequestService)}, method {nameof(CreateAidRequestByCharityUnitAsync)}."
                );
            }
            return new CommonResponse
            {
                Status = 500,
                Message = _config["ResponseMessages:CommonMsg:InternalServerErrorMsg"]
            };
        }

        private async Task<CommonResponse> CreateAidRequestByBranchAsync(
            AidRequestCreatingRequest aidRequestCreatingRequest,
            Guid userId
        )
        {
            try
            {
                Branch? branch = await _branchRepository.FindBranchByBranchAdminIdAsync(userId);

                if (branch == null || branch.Status == BranchStatus.INACTIVE)
                {
                    return new CommonResponse
                    {
                        Status = 403,
                        Message = _config["ResponseMessages:BranchMsg:BranchNotFoundOrInactiveMsg"]
                    };
                }

                using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
                {
                    AidRequest aidRequest =
                        new()
                        {
                            CreatedDate = SettedUpDateTime.GetCurrentVietNamTime(),
                            Address = branch.Address,
                            Location = branch.Location,
                            ScheduledTimes = JsonConvert.SerializeObject(
                                aidRequestCreatingRequest.ScheduledTimes
                            ),
                            Status = AidRequestStatus.PENDING,
                            IsSelfShipping = aidRequestCreatingRequest.IsSelfShipping,
                            Note = aidRequestCreatingRequest.Note,
                            CharityUnitId = branch.Id
                        };

                    if (await _aidRequestRepository.CreateAidRequestAsync(aidRequest) == 1)
                    {
                        List<AidItem> aidItems = new();
                        foreach (
                            AidItemRequest aidItemRequest in aidRequestCreatingRequest.AidItemRequests
                        )
                        {
                            Item? itemTemplate = await _itemRepository.FindItemByIdAsync(
                                aidItemRequest.ItemTemplateId
                            );
                            if (
                                itemTemplate == null
                                || itemTemplate.Status == ItemStatus.INACTIVE
                                || itemTemplate.ItemTemplate == null
                                || itemTemplate.ItemTemplate.Status == ItemTemplateStatus.INACTIVE
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
                            aidItems.Add(
                                new AidItem
                                {
                                    AidRequestId = aidRequest.Id,
                                    ItemId = aidItemRequest.ItemTemplateId,
                                    Quantity = aidItemRequest.Quantity,
                                    Status = AidItemStatus.WAITING
                                }
                            );
                        }
                        if (
                            await _aidItemRepository.CreateAidItemsAsync(aidItems)
                            == aidRequestCreatingRequest.AidItemRequests.Count
                        )
                        {
                            try
                            {
                                DeliverableBranches deliverableBranches =
                                    await _openRouteService.GetDeliverableBranchesByBranchLocation(
                                        branch.Location,
                                        branch.Id
                                    );

                                if (deliverableBranches.NearestBranch == null)
                                    return new CommonResponse
                                    {
                                        Status = 400,
                                        Message =
                                            _config[
                                                "ResponseMessages:BranchMsg:NearbyBranchesNotFound"
                                            ]
                                            + $"({double.Parse(_config["OpenRoute:MaxRoadLengthAsMeters"]) / 1000} km)"
                                    };

                                List<Branch> nearbyBranches =
                                    deliverableBranches.NearbyBranches.Count > 0
                                        ? deliverableBranches.NearbyBranches
                                        : new List<Branch> { deliverableBranches.NearestBranch };

                                List<AcceptableAidRequest> acceptableAidRequests = nearbyBranches
                                    .Select(
                                        b =>
                                            new AcceptableAidRequest
                                            {
                                                AidRequestId = aidRequest.Id,
                                                BranchId = b.Id,
                                                CreatedDate =
                                                    SettedUpDateTime.GetCurrentVietNamTime()
                                            }
                                    )
                                    .ToList();
                                if (
                                    await _acceptableAidRequestRepository.CreateAcceptableAidRequestsAsync(
                                        acceptableAidRequests
                                    ) == acceptableAidRequests.Count
                                )
                                {
                                    scope.Complete();
                                    return new CommonResponse
                                    {
                                        Status = 200,
                                        Message = _config[
                                            "ResponseMessages:AidRequest:CreateAidRequestSuccess"
                                        ]
                                    };
                                }
                                else
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
                                    $"An exception occurred in service {nameof(AidRequestService)}, method {nameof(CreateAidRequestAsync)}."
                                );
                                return new CommonResponse
                                {
                                    Status = 400,
                                    Message = _config["ResponseMessages:CommonMsg:LocationNotValid"]
                                };
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    $"An exception occurred in service {nameof(AidRequestService)}, method {nameof(CreateAidRequestAsync)}."
                );
            }
            return new CommonResponse
            {
                Status = 500,
                Message = _config["ResponseMessages:CommonMsg:InternalServerErrorMsg"]
            };
        }

        public async Task<CommonResponse> GetAidRequestsAsync(
            AidRequestStatus? status,
            DateTime? startDate,
            DateTime? endDate,
            Guid? callerId,
            Guid? branchId,
            Guid? charityUnitId,
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
                if (branchId != null)
                    return new CommonResponse { Status = 403, Message = UnauthenticationMsg };
                Branch? branch = await _branchRepository.FindBranchByBranchAdminIdAsync(
                    (Guid)callerId
                );
                if (branch == null)
                {
                    return new CommonResponse { Status = 403, Message = UnauthenticationMsg };
                }
                branchId = branch.Id;
            }
            else if (userRoleName == RoleEnum.CHARITY.ToString())
            {
                if (charityUnitId != null)
                    return new CommonResponse { Status = 403, Message = UnauthenticationMsg };

                CharityUnit? charityUnit =
                    await _charityUnitRepository.FindCharityUnitByUserIdAsync((Guid)callerId);
                if (charityUnit == null)
                    return new CommonResponse { Status = 403, Message = UnauthenticationMsg };

                charityUnitId = charityUnit.Id;
            }
            else if (userRoleName != RoleEnum.SYSTEM_ADMIN.ToString())
                return new CommonResponse { Status = 403, Message = UnauthenticationMsg };

            List<AidRequest> aidRequests =
                await _aidRequestRepository.GetAidRequestsOfCharityUnitAsync(
                    status,
                    startDate,
                    endDate,
                    branchId,
                    charityUnitId
                );

            if (
                orderBy != null
                && sortType != null
                && (sortType == SortType.ASC || sortType == SortType.DES)
            )
            {
                if (sortType == SortType.ASC)
                    aidRequests = aidRequests
                        .OrderBy(aidRequest => GetPropertyValue(aidRequest, orderBy))
                        .ToList();
                else
                    aidRequests = aidRequests
                        .OrderByDescending(aidRequest => GetPropertyValue(aidRequest, orderBy))
                        .ToList();
            }

            Pagination pagination = new();
            pagination.PageSize = pageSize == null ? 10 : pageSize.Value;
            pagination.CurrentPage = page == null ? 1 : page.Value;
            pagination.Total = aidRequests.Count;
            aidRequests = aidRequests
                .Skip((pagination.CurrentPage - 1) * pagination.PageSize)
                .Take(pagination.PageSize)
                .ToList();

            if (branchId != null)
            {
                foreach (AidRequest aidRequest in aidRequests)
                {
                    if (
                        aidRequest.AcceptableAidRequests.Any(
                            aar =>
                                aar.BranchId == branchId
                                && aar.Status == AcceptableAidRequestStatus.REJECTED
                        )
                    )
                    {
                        aidRequest.Status = AidRequestStatus.REJECTED;
                    }
                }
            }

            return new CommonResponse
            {
                Status = 200,
                Data =
                    userRoleName == RoleEnum.SYSTEM_ADMIN.ToString()
                    || userRoleName == RoleEnum.BRANCH_ADMIN.ToString()
                        ? aidRequests.Select(
                            ar =>
                                new AidRequestForAdminResponse
                                {
                                    Id = ar.Id,
                                    Address = ar.Address,
                                    Location = _openRouteService.GetCoordinatesByLocation(
                                        ar.Location
                                    ),
                                    CreatedDate = ar.CreatedDate,
                                    AcceptedDate = ar.ConfirmedDate,
                                    ScheduledTimes = JsonConvert.DeserializeObject<
                                        List<ScheduledTime>
                                    >(ar.ScheduledTimes),
                                    Status = ar.Status.ToString(),
                                    IsSelfShipping = ar.IsSelfShipping,
                                    SimpleBranchResponses = ar.AcceptableAidRequests
                                        .Where(
                                            aar => aar.Status == AcceptableAidRequestStatus.ACCEPTED
                                        )
                                        .Select(
                                            bar =>
                                                new SimpleBranchResponse
                                                {
                                                    Id = bar.Branch.Id,
                                                    Name = bar.Branch.Name,
                                                    Image = bar.Branch.Image
                                                }
                                        )
                                        .ToList(),
                                    SimpleCharityUnitResponse = new SimpleCharityUnitResponse
                                    {
                                        Id = ar.CharityUnit!.Id,
                                        Name = ar.CharityUnit.Name,
                                        Image = ar.CharityUnit.Image
                                    }
                                }
                        )
                        : aidRequests.Select(
                            ar =>
                                new AidRequestForCharityUnitResponse
                                {
                                    Id = ar.Id,
                                    Address = ar.Address,
                                    Location = _openRouteService.GetCoordinatesByLocation(
                                        ar.Location
                                    ),
                                    CreatedDate = ar.CreatedDate,
                                    AcceptedDate = ar.ConfirmedDate,
                                    ScheduledTimes = JsonConvert.DeserializeObject<
                                        List<ScheduledTime>
                                    >(ar.ScheduledTimes),
                                    Status = ar.Status.ToString(),
                                    IsSelfShipping = ar.IsSelfShipping,
                                    SimpleBranchResponses = ar.AcceptableAidRequests
                                        .Where(
                                            aar => aar.Status == AcceptableAidRequestStatus.ACCEPTED
                                        )
                                        .Select(
                                            bar =>
                                                new SimpleBranchResponse
                                                {
                                                    Id = bar.Branch.Id,
                                                    Name = bar.Branch.Name,
                                                    Image = bar.Branch.Image
                                                }
                                        )
                                        .ToList(),
                                }
                        ),
                Pagination = pagination,
                Message = _config["ResponseMessages:ActivityMsg:GetActivitiesSuccessMsg"]
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

        public async Task<CommonResponse> GetAidRequestAsync(
            Guid id,
            Guid? userId,
            string? userRoleName
        )
        {
            string UnauthenticationMsg = _config[
                "ResponseMessages:AuthenticationMsg:UnauthenticationMsg"
            ];
            bool isConfirmable = false;
            AidRequest? aidRequest =
                await _aidRequestRepository.FindAidRequestOfCharityUnitByIdForDetailAsync(id);
            if (aidRequest == null)
                return new CommonResponse
                {
                    Status = 400,
                    Message = _config["ResponseMessages:AidRequest:AidRequestNotFound"]
                };

            Branch? branch = null;

            if (
                userId == null
                || userRoleName != RoleEnum.SYSTEM_ADMIN.ToString()
                    && userRoleName != RoleEnum.BRANCH_ADMIN.ToString()
                    && userRoleName != RoleEnum.CHARITY.ToString()
            )
                return new CommonResponse { Status = 403, Message = UnauthenticationMsg };
            else if (userRoleName == RoleEnum.BRANCH_ADMIN.ToString())
            {
                branch = await _branchRepository.FindBranchByBranchAdminIdAsync((Guid)userId);

                if (branch == null)
                    return new CommonResponse { Status = 403, Message = UnauthenticationMsg };
                AcceptableAidRequest? acceptableAidRequest =
                    await _acceptableAidRequestRepository.FindPendingAcceptableAidRequestByAidRequestIdAndBranchIdAsync(
                        aidRequest.Id,
                        branch.Id
                    );

                if (acceptableAidRequest != null)
                    isConfirmable = true;

                if (
                    aidRequest.AcceptableAidRequests.Any(
                        aar =>
                            aar.BranchId == branch.Id
                            && aar.Status == AcceptableAidRequestStatus.REJECTED
                    )
                )
                {
                    aidRequest.Status = AidRequestStatus.REJECTED;
                }
            }
            else if (userRoleName == RoleEnum.CHARITY.ToString())
            {
                CharityUnit? charityUnit =
                    await _charityUnitRepository.FindCharityUnitByUserIdAsync((Guid)userId);
                if (charityUnit == null)
                    return new CommonResponse { Status = 403, Message = UnauthenticationMsg };

                List<CharityUnit> charityUnits =
                    await _charityUnitRepository.FindCharityUnitsByCharityIdAsync(
                        charityUnit.CharityId
                    );
                if (!charityUnits.Select(cu => cu.Id).Contains((Guid)aidRequest.CharityUnitId!))
                    return new CommonResponse { Status = 403, Message = UnauthenticationMsg };
            }
            else if (userRoleName != RoleEnum.SYSTEM_ADMIN.ToString())
                return new CommonResponse { Status = 403, Message = UnauthenticationMsg };

            foreach (var ai in aidRequest.AidItems)
            {
                var item = await _itemRepository.FindItemByIdAsync(ai.ItemId);
                ai.Item = item!;
            }

            AcceptableAidRequest? tmp = aidRequest.AcceptableAidRequests.FirstOrDefault(
                aar => aar.Status == AcceptableAidRequestStatus.ACCEPTED
            );

            return new CommonResponse
            {
                Status = 200,
                Data = new AidRequestDetailResponse
                {
                    Id = aidRequest.Id,
                    Address = aidRequest.Address,
                    Location = _openRouteService.GetCoordinatesByLocation(aidRequest.Location),
                    IsConfirmable = isConfirmable,
                    CreatedDate = aidRequest.CreatedDate,
                    AcceptedDate = aidRequest.ConfirmedDate,
                    ScheduledTimes = JsonConvert.DeserializeObject<List<ScheduledTime>>(
                        aidRequest.ScheduledTimes
                    ),
                    Status = aidRequest.Status.ToString(),
                    IsSelfShipping = aidRequest.IsSelfShipping,
                    Note = aidRequest.Note,
                    AcceptedBranches = aidRequest.AcceptableAidRequests
                        .Where(aar => aar.Status == AcceptableAidRequestStatus.ACCEPTED)
                        .Select(
                            aar =>
                                new BranchResponse
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
                    AidItemResponses = aidRequest.AidItems
                        .Select(
                            ai =>
                                new AidItemResponse
                                {
                                    Id = ai.Id,
                                    Quantity = ai.Quantity,
                                    //ExportedQuantity = ai.DeliveryItems
                                    //    .Select(
                                    //        di =>
                                    //            di.StockUpdatedHistoryDetails
                                    //                .Select(sud => sud.Quantity)
                                    //                .Sum()
                                    //    )
                                    //    .Sum(),
                                    ExportedQuantity = GetRealExportedQuantity(
                                        ai.Item.Id,
                                        aidRequest.StockUpdatedHistoryDetails
                                    ),
                                    RealExportedQuantity = GetRealExportedQuantity(
                                        ai.Item.Id,
                                        aidRequest.StockUpdatedHistoryDetails
                                    ),
                                    Status = ai.Status.ToString(),
                                    ItemResponse = new ItemResponse
                                    {
                                        Id = ai.Item.Id,
                                        Name = ai.Item.ItemTemplate.Name,
                                        AttributeValues = ai.Item.ItemAttributeValues
                                            .Select(itav => itav.AttributeValue.Value)
                                            .ToList(),
                                        Image = ai.Item.Image,
                                        MaximumTransportVolume = ai.Item.MaximumTransportVolume,
                                        Unit = ai.Item.ItemTemplate.Unit.Name
                                    }
                                }
                        )
                        .ToList(),
                    StartingBranch =
                        tmp != null
                            ? new BranchResponse
                            {
                                Id = tmp.Branch.Id,
                                Name = tmp.Branch.Name,
                                Address = tmp.Branch.Address,
                                Location = _openRouteService.GetCoordinatesByLocation(
                                    tmp.Branch.Location
                                ),
                                Image = tmp.Branch.Image,
                                CreatedDate = tmp.Branch.CreatedDate,
                                Status = tmp.Branch.Status.ToString()
                            }
                            : null,
                    //aidRequest.Status == AidRequestStatus.ACCEPTED
                    //    ? (
                    //        aidRequest.AcceptableAidRequests
                    //            .Where(aar => aar.Status == AcceptableAidRequestStatus.ACCEPTED)
                    //            .Select(aar => aar.Branch.Id)
                    //            .Any(
                    //                id =>
                    //                    branch != null
                    //                    && id == branch.Id
                    //                    && branch.Status == BranchStatus.ACTIVE
                    //            )
                    //            ? new BranchResponse
                    //            {
                    //                Id = branch!.Id,
                    //                Name = branch!.Name,
                    //                Address = branch!.Address,
                    //                Location = _openRouteService.GetCoordinatesByLocation(
                    //                    branch.Location
                    //                ),
                    //                Image = branch!.Image,
                    //                CreatedDate = branch!.CreatedDate,
                    //                Status = branch!.Status.ToString()
                    //            }
                    //            : null
                    //    )
                    //    : null,
                    CharityUnitResponse = new CharityUnitResponse
                    {
                        Id = aidRequest.CharityUnit!.Id,
                        Name = aidRequest.CharityUnit.Name,
                        Phone = aidRequest.CharityUnit.User.Phone,
                        Email = aidRequest.CharityUnit.User.Email,
                        Image = aidRequest.CharityUnit.Image,
                        Address = aidRequest.CharityUnit.Address,
                        Status = aidRequest.CharityUnit.Status.ToString(),
                        CharityName = aidRequest.CharityUnit.Charity.Name,
                        CharityLogo = aidRequest.CharityUnit.Charity.Logo
                    },
                    RejectingBranchResponses =
                        userRoleName == RoleEnum.CHARITY.ToString()
                            ? aidRequest.Status == AidRequestStatus.REJECTED
                                ? aidRequest.AcceptableAidRequests
                                    .Where(aar => aar.Status == AcceptableAidRequestStatus.REJECTED)
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
                            : aidRequest.AcceptableAidRequests
                                .Where(aar => aar.Status == AcceptableAidRequestStatus.REJECTED)
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
                },
                Message = _config["ResponseMessages:AidRequest:GetAidRequestSuccessMsg"]
            };
        }

        private double GetRealExportedQuantity(
            Guid itemId,
            List<StockUpdatedHistoryDetail> stockUpdatedHistoryDetails
        )
        {
            return stockUpdatedHistoryDetails
                .Where(
                    suhd =>
                        suhd.StockId != null
                        && suhd.Stock!.ItemId == itemId
                        && suhd.StockUpdatedHistory.IsPrivate == false
                )
                .Select(suhd => suhd.Quantity)
                .Sum();
        }

        public async Task<CommonResponse> CountAidRequestByAllStatus(
            DateTime? startDate,
            DateTime? endDate,
            Guid? branchId,
            Guid? charityUnitId,
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
            CommonResponse commonResponse = new();
            try
            {
                int total = await _aidRequestRepository.CountAidRequestByStatus(
                    null,
                    startDate,
                    endDate,
                    branchId,
                    charityUnitId
                );
                int numberOfPending = await _aidRequestRepository.CountAidRequestByStatus(
                    AidRequestStatus.PENDING,
                    startDate,
                    endDate,
                    branchId,
                    charityUnitId
                );
                int numberOfAccepted = await _aidRequestRepository.CountAidRequestByStatus(
                    AidRequestStatus.ACCEPTED,
                    startDate,
                    endDate,
                    branchId,
                    charityUnitId
                );
                int numberOfRejected = await _aidRequestRepository.CountAidRequestByStatus(
                    AidRequestStatus.REJECTED,
                    startDate,
                    endDate,
                    branchId,
                    charityUnitId
                );
                int numberOfCanceled = await _aidRequestRepository.CountAidRequestByStatus(
                    AidRequestStatus.CANCELED,
                    startDate,
                    endDate,
                    branchId,
                    charityUnitId
                );
                int numberOfExpried = await _aidRequestRepository.CountAidRequestByStatus(
                    AidRequestStatus.EXPIRED,
                    startDate,
                    endDate,
                    branchId,
                    charityUnitId
                );
                int numberOfProcessing = await _aidRequestRepository.CountAidRequestByStatus(
                    AidRequestStatus.PROCESSING,
                    startDate,
                    endDate,
                    branchId,
                    charityUnitId
                );
                int numberOfSelfShipping =
                    await _aidRequestRepository.CountAidRequestBySelfShippingFlag(
                        true,
                        startDate,
                        endDate
                    );
                //int numberOfReported = await _aidRequestRepository.CountAidRequestByStatus(
                //    AidRequestStatus.REPORTED,
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
                    NumberOfSelfShipping = numberOfSelfShipping,
                    NumberOfExpried = numberOfExpried,
                    Total = total
                };
                commonResponse.Data = rs;
                commonResponse.Status = 200;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    $"An exception occurred in service {nameof(DonatedRequestService)}, method {nameof(CountAidRequestByAllStatus)}."
                );
                return new CommonResponse
                {
                    Status = 500,
                    Message = _config["ResponseMessages:CommonMsg:InternalServerErrorMsg"]
                };
            }
            return commonResponse;
        }

        public async Task<CommonResponse> CountAidRequestByStatus(
            DateTime startDate,
            DateTime endDate,
            AidRequestStatus? status,
            TimeFrame timeFrame,
            Guid? branchId,
            Guid? charityUnitId,
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
            CommonResponse commonResponse = new();
            try
            {
                int total = await _aidRequestRepository.CountAidRequestByStatus(
                    status,
                    startDate,
                    endDate,
                    branchId,
                    charityUnitId
                );
                List<StatisticObjectByTimeRangeResponse> responses = new();
                switch (timeFrame)
                {
                    case TimeFrame.DAY:
                        for (
                            DateTime currentDate = startDate;
                            currentDate <= endDate;
                            currentDate = currentDate.AddDays(1)
                        )
                        {
                            StatisticObjectByTimeRangeResponse tmp = new();
                            tmp.From = currentDate;
                            tmp.To = currentDate.AddDays(1);
                            tmp.Quantity = await _aidRequestRepository.CountAidRequestByStatus(
                                status,
                                tmp.From,
                                tmp.To,
                                branchId,
                                charityUnitId
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
                            StatisticObjectByTimeRangeResponse tmp = new();
                            tmp.From = currentDate;
                            tmp.To = currentDate.AddMonths(1);
                            tmp.Quantity = await _aidRequestRepository.CountAidRequestByStatus(
                                status,
                                tmp.From,
                                tmp.To,
                                branchId,
                                charityUnitId
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
                            StatisticObjectByTimeRangeResponse tmp = new();
                            tmp.From = currentDate;
                            tmp.To = currentDate.AddYears(1);
                            tmp.Quantity = await _aidRequestRepository.CountAidRequestByStatus(
                                status,
                                tmp.From,
                                tmp.To,
                                branchId,
                                charityUnitId
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
                            StatisticObjectByTimeRangeResponse tmp = new();
                            tmp.From = currentDate;
                            tmp.To = currentDate.AddDays(7);
                            tmp.Quantity = await _aidRequestRepository.CountAidRequestByStatus(
                                status,
                                tmp.From,
                                tmp.To,
                                branchId,
                                charityUnitId
                            );
                            responses.Add(tmp);
                        }
                        break;
                    default:
                        break;
                }
                var rs = new { Total = total, AidRequestByTimeRangeResponse = responses };
                commonResponse.Data = rs;
                commonResponse.Status = 200;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    $"An exception occurred in service {nameof(ScheduledRouteService)}, method {nameof(CountAidRequestByStatus)}."
                );
                return new CommonResponse
                {
                    Status = 500,
                    Message = _config["ResponseMessages:CommonMsg:InternalServerErrorMsg"]
                };
            }
            return commonResponse;
        }

        public async Task<CommonResponse> CancelAidRequest(Guid donatedRequestId, Guid userId)
        {
            CommonResponse commonResponse = new();
            string UnauthenticationMsg = _config[
                "ResponseMessages:AuthenticationMsg:UnauthenticationMsg"
            ];
            try
            {
                CharityUnit? checkCharityUnit =
                    await _charityUnitRepository.FindActiveCharityUnitsByUserIdAsync(userId);
                AidRequest? aidRequest = await _aidRequestRepository.FindAidRequestByIdAsync(
                    donatedRequestId
                );

                if (aidRequest == null)
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
                    aidRequest.Status != AidRequestStatus.PENDING
                    && aidRequest.Status != AidRequestStatus.ACCEPTED
                )
                {
                    return new CommonResponse
                    {
                        Status = 400,
                        Message = "Bạn chỉ có thể hủy yêu cầu quyên góp khi ở trạng thái đang chờ."
                    };
                }
                else if (
                    checkCharityUnit == null || aidRequest.CharityUnitId != checkCharityUnit.Id
                )
                {
                    return new CommonResponse { Status = 403, Message = UnauthenticationMsg };
                }
                else
                {
                    aidRequest.Status = AidRequestStatus.CANCELED;
                    int rs = await _aidRequestRepository.UpdateAidRequestAsync(aidRequest);
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
                    $"An exception occurred in service {nameof(ScheduledRouteService)}, method {nameof(CancelAidRequest)}."
                );
                return new CommonResponse
                {
                    Status = 500,
                    Message = _config["ResponseMessages:CommonMsg:InternalServerErrorMsg"]
                };
            }
            return commonResponse;
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

        public async Task UpdateOutDateAidRequestsAsync()
        {
            try
            {
                List<AidRequest> aidRequests =
                    await _aidRequestRepository.FindPendingAndAcceptedAndProcessingAidRequestsAsync();

                List<AidRequest> aboutToOutDateAidRequests = aidRequests
                    .Where(
                        dr =>
                            dr.AcceptableAidRequests.Any(
                                adr => adr.Status == AcceptableAidRequestStatus.ACCEPTED
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

                List<AidRequest> outDateAidRequests = aidRequests
                    .Where(
                        dr =>
                            dr.AcceptableAidRequests.Any(
                                adr => adr.Status == AcceptableAidRequestStatus.ACCEPTED
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

                outDateAidRequests.ForEach(dr => dr.Status = AidRequestStatus.EXPIRED);

                using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
                {
                    foreach (AidRequest aidRequest in aboutToOutDateAidRequests)
                    {
                        Notification notification =
                            new()
                            {
                                Name = "Yêu cầu hỗ trợ sắp hết hạn.",
                                CreatedDate = SettedUpDateTime.GetCurrentVietNamTime(),
                                Image = _config["Notification:Image"],
                                Content = "Chi nhánh có yêu cầu hỗ trợ sắp hết hạn cần được xử lý.",
                                ReceiverId = aidRequest.AcceptableAidRequests[
                                    0
                                ].Branch.BranchAdminId.ToString(),
                                Status = NotificationStatus.NEW,
                                Type = NotificationType.NOTIFYING,
                                DataType = DataNotificationType.AID_REQUEST,
                                DataId = aidRequest.Id
                            };
                        await _notificationRepository.CreateNotificationAsync(notification);
                        await _hubContext.Clients.All.SendAsync(
                            aidRequest.AcceptableAidRequests[0].Branch.BranchAdminId.ToString(),
                            notification
                        );
                    }
                    foreach (AidRequest request in outDateAidRequests)
                    {
                        if (await _aidRequestRepository.UpdateAidRequestAsync(request) == 1)
                        {
                            if (request.CharityUnit != null)
                            {
                                Notification notificationForUser =
                                    new()
                                    {
                                        Name = "Yêu cầu hỗ trợ đã hết hạn.",
                                        CreatedDate = SettedUpDateTime.GetCurrentVietNamTime(),
                                        Image = _config["Notification:Image"],
                                        Content =
                                            "Yêu cầu hỗ trợ của bạn đã hết hạn vì không có tình nguyện viên nào có thể vận chuyển.Bạn vui lòng liên hệ với chúng tôi để biết thêm thông tin chi tiết.",
                                        ReceiverId = request.CharityUnit.UserId.ToString(),
                                        Status = NotificationStatus.NEW,
                                        Type = NotificationType.NOTIFYING,
                                        DataType = DataNotificationType.AID_REQUEST,
                                        DataId = request.Id
                                    };
                                await _notificationRepository.CreateNotificationAsync(
                                    notificationForUser
                                );
                                await _hubContext.Clients.All.SendAsync(
                                    request.CharityUnit.UserId.ToString(),
                                    notificationForUser
                                );
                            }
                            else if (request.Branch != null)
                            {
                                Notification notificationForUser =
                                    new()
                                    {
                                        Name = "Yêu cầu hỗ trợ đã hết hạn.",
                                        CreatedDate = SettedUpDateTime.GetCurrentVietNamTime(),
                                        Image = _config["Notification:Image"],
                                        Content =
                                            "Yêu cầu hỗ trợ của bạn đã hết hạn vì không có tình nguyện viên nào có thể vận chuyển.",
                                        ReceiverId = request.Branch.BranchAdminId.ToString(),
                                        Status = NotificationStatus.NEW,
                                        Type = NotificationType.NOTIFYING,
                                        DataType = DataNotificationType.AID_REQUEST,
                                        DataId = request.Id
                                    };
                                await _notificationRepository.CreateNotificationAsync(
                                    notificationForUser
                                );
                                await _hubContext.Clients.All.SendAsync(
                                    request.Branch.BranchAdminId.ToString(),
                                    notificationForUser
                                );
                            }

                            if (request.AcceptableAidRequests.Count() > 0)
                            {
                                Notification notificationForAdmin =
                                    new()
                                    {
                                        Name = "Yêu cầu hỗ trợ đã hết hạn.",
                                        CreatedDate = SettedUpDateTime.GetCurrentVietNamTime(),
                                        Image = _config["Notification:Image"],
                                        Content =
                                            "Yêu cầu hỗ trợ mà chi nhánh đảm nhận đã hết hạn vì không có tình nguyện viên nào có thể vận chuyển.",
                                        ReceiverId = request.AcceptableAidRequests[
                                            0
                                        ].Branch.BranchAdminId.ToString(),
                                        Status = NotificationStatus.NEW,
                                        Type = NotificationType.NOTIFYING,
                                        DataType = DataNotificationType.AID_REQUEST,
                                        DataId = request.Id
                                    };
                                await _notificationRepository.CreateNotificationAsync(
                                    notificationForAdmin
                                );
                                await _hubContext.Clients.All.SendAsync(
                                    request.AcceptableAidRequests[
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
                    $"An exception occurred in service {nameof(DonatedRequestService)}, method {nameof(UpdateOutDateAidRequestsAsync)}."
                );
            }
        }

        public async Task<CommonResponse> FinishAidRequestAsync(Guid aidRequestId, Guid userId)
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
                    await _aidRequestRepository.FindAcceptedAndProcessingAidRequestByIdAndBranchIdToFinishAsync(
                        aidRequestId,
                        branch.Id
                    );
                if (aidRequest == null)
                    return new CommonResponse
                    {
                        Status = 400,
                        Message = _config[
                            "ResponseMessages:AidRequest:ProcessingAidRequestAcceptedByBranchNotFoundMsg"
                        ]
                    };

                aidRequest.Status = AidRequestStatus.FINISHED;

                Notification notification =
                    new()
                    {
                        Name = "Yêu cầu hỗ trợ đã hoàn thành.",
                        CreatedDate = SettedUpDateTime.GetCurrentVietNamTime(),
                        Image = _config["Notification:Image"],
                        Content = "Yêu cầu hỗ trợ đã thành thành bởi chi nhánh đảm nhận.",
                        ReceiverId =
                            aidRequest.CharityUnitId != null
                                ? aidRequest.CharityUnit!.UserId.ToString()
                                : aidRequest.Branch!.BranchAdminId.ToString(),
                        Status = NotificationStatus.NEW,
                        Type = NotificationType.NOTIFYING,
                        DataType = DataNotificationType.AID_REQUEST,
                        DataId = aidRequest.Id
                    };

                using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
                {
                    if (await _aidRequestRepository.UpdateAidRequestAsync(aidRequest) != 1)
                        return new CommonResponse
                        {
                            Status = 500,
                            Message = _config["ResponseMessages:CommonMsg:InternalServerErrorMsg"]
                        };

                    await _notificationRepository.CreateNotificationAsync(notification);
                    await _hubContext.Clients.All.SendAsync(
                        aidRequest.CharityUnitId != null
                            ? aidRequest.CharityUnit!.UserId.ToString()
                            : aidRequest.Branch!.BranchAdminId.ToString(),
                        notification
                    );

                    scope.Complete();
                    return new CommonResponse
                    {
                        Status = 200,
                        Message = _config["ResponseMessages:AidRequest:FinishAidRequestSuccessMsg"]
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    $"An exception occurred in service {nameof(AidRequestService)}, method {nameof(FinishAidRequestAsync)}."
                );
            }
            return new CommonResponse
            {
                Status = 500,
                Message = _config["ResponseMessages:CommonMsg:InternalServerErrorMsg"]
            };
        }
    }
}
