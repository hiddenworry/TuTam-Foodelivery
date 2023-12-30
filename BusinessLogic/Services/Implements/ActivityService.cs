using BusinessLogic.Utils.FirebaseService;
using BusinessLogic.Utils.OpenRouteService;
using DataAccess.Entities;
using DataAccess.EntityEnums;
using DataAccess.Models.Requests;
using DataAccess.Models.Requests.Enum;
using DataAccess.Models.Requests.ModelBinders;
using DataAccess.Models.Responses;
using DataAccess.ModelsEnum;
using DataAccess.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.Transactions;

namespace BusinessLogic.Services.Implements
{
    public class ActivityService : IActivityService
    {
        private readonly IActivityRepository _activityRepository;
        private readonly IBranchRepository _branchRepository;
        private readonly IActivityTypeComponentRepository _activityTypeComponentRepository;
        private readonly IActivityBranchRepository _activityBranchRepository;
        private readonly IConfiguration _config;
        private readonly IActivityTypeRepository _activityTypeRepository;
        private readonly IFirebaseStorageService _firebaseStorageService;
        private readonly ILogger<ActivityService> _logger;
        private readonly IUserRepository _userRepository;
        private readonly ICharityUnitRepository _charityUnitRepository;
        private readonly ITargetProcessRepository _targetProcessRepository;
        private readonly IItemRepository _itemRepository;
        private readonly IActivityMemberRepository _activityMemberRepository;
        private readonly IRoleRepository _roleRepository;
        private readonly IAidItemRepository _aidItemRepository;
        private readonly IOpenRouteService _openRouteService;

        public ActivityService(
            IActivityRepository activityRepository,
            IBranchRepository branchRepository,
            IActivityTypeComponentRepository activityTypeComponentRepository,
            IActivityBranchRepository activityBranchRepository,
            IConfiguration config,
            IActivityTypeRepository activityTypeRepository,
            IFirebaseStorageService firebaseStorageService,
            ILogger<ActivityService> logger,
            IUserRepository userRepository,
            ICharityUnitRepository charityUnitRepository,
            ITargetProcessRepository targetProcessRepository,
            IItemRepository itemRepository,
            IActivityMemberRepository activityMemberRepository,
            IRoleRepository roleRepository,
            IAidItemRepository aidItemRepository,
            IOpenRouteService openRouteService
        )
        {
            _activityRepository = activityRepository;
            _branchRepository = branchRepository;
            _activityTypeComponentRepository = activityTypeComponentRepository;
            _activityBranchRepository = activityBranchRepository;
            _config = config;
            _activityTypeRepository = activityTypeRepository;
            _firebaseStorageService = firebaseStorageService;
            _logger = logger;
            _userRepository = userRepository;
            _charityUnitRepository = charityUnitRepository;
            _targetProcessRepository = targetProcessRepository;
            _itemRepository = itemRepository;
            _activityMemberRepository = activityMemberRepository;
            _roleRepository = roleRepository;
            _aidItemRepository = aidItemRepository;
            _openRouteService = openRouteService;
        }

        public async Task<CommonResponse> CreateActivityAsync(
            ActivityCreatingRequest activityCreatingRequest,
            Guid userId,
            string userRoleName
        )
        {
            if (
                await _activityRepository.FindActivityByNameIgnoreCaseAsync(
                    activityCreatingRequest.Name
                ) != null
            )
            {
                return new CommonResponse
                {
                    Status = 400,
                    Message = _config["ResponseMessages:ActivityMsg:ActivityNameDuplicatedMsg"]
                };
            }

            Branch? branch = null;
            if (userRoleName == RoleEnum.BRANCH_ADMIN.ToString())
            {
                branch = await _branchRepository.FindBranchByBranchAdminIdAsync(userId);
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
                if (
                    activityCreatingRequest.BranchIds != null
                    && activityCreatingRequest.BranchIds.Count > 0
                )
                {
                    return new CommonResponse
                    {
                        Status = 403,
                        Message = _config[
                            "ResponseMessages:ActivityMsg:UserNotAllowToAssignBranchesMsg"
                        ]
                    };
                }
                else
                    activityCreatingRequest.BranchIds = new List<Guid> { branch!.Id };
            }
            else
            {
                if (
                    !(
                        activityCreatingRequest.BranchIds != null
                        && activityCreatingRequest.BranchIds.Count > 0
                    )
                )
                {
                    return new CommonResponse
                    {
                        Status = 400,
                        Message = _config["ResponseMessages:ActivityMsg:JoinedBranchsListEmptyMsg"]
                    };
                }
            }

            using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                List<string> imageUrls = new();
                Activity activity =
                    new()
                    {
                        Name = activityCreatingRequest.Name,
                        Address = null,
                        Location = null,
                        CreatedDate = SettedUpDateTime.GetCurrentVietNamTime(),
                        Images = "",
                        EstimatedStartDate = activityCreatingRequest.EstimatedStartDate,
                        EstimatedEndDate = activityCreatingRequest.EstimatedEndDate,
                        DeliveringDate = activityCreatingRequest.DeliveringDate,
                        Description = activityCreatingRequest.Description,
                        Scope = activityCreatingRequest.Scope ?? ActivityScope.PUBLIC,
                        CreatedBy = userId
                    };

                List<TargetProcess> targetProcesses = new();
                List<AidItem> aidItems = new();

                if (await _activityRepository.CreateActivityAsync(activity) != 1)
                    return new CommonResponse
                    {
                        Status = 500,
                        Message = _config["ResponseMessages:CommonMsg:InternalServerErrorMsg"]
                    };

                if (
                    activityCreatingRequest.TargetProcessRequests != null
                    && activityCreatingRequest.TargetProcessRequests.Count > 0
                )
                {
                    foreach (
                        TargetProcessRequest targetProcessRequest in activityCreatingRequest.TargetProcessRequests
                    )
                    {
                        Item? itemTemplate = await _itemRepository.FindItemByIdAsync(
                            targetProcessRequest.ItemId
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

                        if (
                            targetProcesses.FirstOrDefault(
                                tp => tp.ItemId == targetProcessRequest.ItemId
                            ) != null
                        )
                        {
                            targetProcesses.ForEach(
                                (tp) =>
                                {
                                    if (tp.ItemId == targetProcessRequest.ItemId)
                                    {
                                        tp.Target += targetProcessRequest.Quantity;
                                    }
                                }
                            );
                        }
                        else
                        {
                            targetProcesses.Add(
                                new TargetProcess
                                {
                                    ActivityId = activity.Id,
                                    ItemId = targetProcessRequest.ItemId,
                                    Target = targetProcessRequest.Quantity,
                                    Process = 0
                                }
                            );
                        }

                        //targetProcesses.Add(
                        //    new TargetProcess
                        //    {
                        //        ActivityId = activity.Id,
                        //        ItemId = targetProcessRequest.ItemId,
                        //        Target = targetProcessRequest.Quantity,
                        //        Process = 0
                        //    }
                        //);
                    }
                }
                if (
                    activityCreatingRequest.AidItemForActivityRequests != null
                    && activityCreatingRequest.AidItemForActivityRequests.Count > 0
                )
                {
                    foreach (
                        AidItemForActivityRequest aidItemForActivityRequest in activityCreatingRequest.AidItemForActivityRequests
                    )
                    {
                        AidItem? aidItem = await _aidItemRepository.GetAidItemForActivityByIdAsync(
                            aidItemForActivityRequest.AidItemId
                        );

                        if (aidItem == null)
                            return new CommonResponse
                            {
                                Status = 400,
                                Message = _config[
                                    "ResponseMessages:AidItemMsg:AcceptedAidItemForActivityOfYourBranchNotFoundMsg"
                                ]
                            };

                        if (
                            !(
                                userRoleName == RoleEnum.BRANCH_ADMIN.ToString()
                                && aidItem.AidRequest.AcceptableAidRequests.Any(
                                    aar =>
                                        aar.BranchId == branch!.Id
                                        && aar.Status == AcceptableAidRequestStatus.ACCEPTED
                                )
                            )
                        )
                            return new CommonResponse
                            {
                                Status = 400,
                                Message = _config[
                                    "ResponseMessages:AidItemMsg:AcceptedAidItemForActivityOfYourBranchNotFoundMsg"
                                ]
                            };

                        aidItems.Add(aidItem);

                        if (
                            targetProcesses.FirstOrDefault(tp => tp.ItemId == aidItem.ItemId)
                            != null
                        )
                        {
                            targetProcesses.ForEach(
                                (tp) =>
                                {
                                    if (tp.ItemId == aidItem.ItemId)
                                    {
                                        tp.Target += aidItemForActivityRequest.Quantity;
                                    }
                                }
                            );
                        }
                        else
                        {
                            targetProcesses.Add(
                                new TargetProcess
                                {
                                    ActivityId = activity.Id,
                                    ItemId = aidItem.ItemId,
                                    Target = aidItemForActivityRequest.Quantity,
                                    Process = 0
                                }
                            );
                        }
                    }

                    aidItems.ForEach(
                        (ai) =>
                        {
                            ai.Status = AidItemStatus.APPLIED_TO_ACTIVITY;
                        }
                    );
                    if (await _aidItemRepository.UpdateAidItemsAsync(aidItems) != aidItems.Count)
                    {
                        return new CommonResponse
                        {
                            Status = 500,
                            Message = _config["ResponseMessages:CommonMsg:InternalServerErrorMsg"]
                        };
                    }
                }
                if (
                    await _targetProcessRepository.CreateTargetProcessesAsync(targetProcesses)
                    != targetProcesses.Count
                )
                {
                    return new CommonResponse
                    {
                        Status = 500,
                        Message = _config["ResponseMessages:CommonMsg:InternalServerErrorMsg"]
                    };
                }
                List<ActivityBranch> activityBranches = new();
                Branch? organizeBranch = null;
                foreach (Guid branchId in activityCreatingRequest.BranchIds)
                {
                    Branch? joinedBranch = await _branchRepository.FindBranchByIdAsync(branchId);
                    if (joinedBranch == null)
                        return new CommonResponse
                        {
                            Status = 400,
                            Message = _config["ResponseMessages:BranchMsg:BranchNotFoundInListMsg"]
                        };
                    else if (joinedBranch.Status == BranchStatus.INACTIVE)
                        return new CommonResponse
                        {
                            Status = 400,
                            Message = _config["ResponseMessages:BranchMsg:InactiveBranchInListMsg"]
                        };
                    if (activityCreatingRequest.BranchIds.Count == 1)
                        organizeBranch = joinedBranch;
                    activityBranches.Add(
                        new ActivityBranch { ActivityId = activity.Id, BranchId = branchId }
                    );
                }

                if (
                    !activityCreatingRequest.Address.IsNullOrEmpty()
                    && !activityCreatingRequest.Location.IsNullOrEmpty()
                )
                {
                    activity.Address = activityCreatingRequest.Address;
                    activity.Location = string.Join(",", activityCreatingRequest.Location!);
                }
                else
                {
                    if (activityCreatingRequest.BranchIds.Count == 1)
                    {
                        activity.Address = organizeBranch!.Address;
                        activity.Location = organizeBranch!.Location;
                    }
                }

                if (
                    await _activityBranchRepository.CreateActivityBranchesAsync(activityBranches)
                    != activityCreatingRequest.BranchIds.Count
                )
                    return new CommonResponse
                    {
                        Status = 500,
                        Message = _config["ResponseMessages:CommonMsg:InternalServerErrorMsg"]
                    };

                List<ActivityTypeComponent> activityTypeComponents = new();
                foreach (Guid activityTypeId in activityCreatingRequest.ActivityTypeIds)
                {
                    ActivityType? activityType =
                        await _activityTypeRepository.FindActivityTypeByIdAsync(activityTypeId);
                    if (activityType == null)
                        return new CommonResponse
                        {
                            Status = 400,
                            Message = _config[
                                "ResponseMessages:ActivityTypeMsg:ActivityTypeNotFoundMsg"
                            ]
                        };
                    activityTypeComponents.Add(
                        new ActivityTypeComponent
                        {
                            ActivityId = activity.Id,
                            ActivityTypeId = activityTypeId
                        }
                    );
                }

                if (
                    await _activityTypeComponentRepository.CreateActivityTypeComponentsAsync(
                        activityTypeComponents
                    ) != activityCreatingRequest.ActivityTypeIds.Count
                )
                    return new CommonResponse
                    {
                        Status = 500,
                        Message = _config["ResponseMessages:CommonMsg:InternalServerErrorMsg"]
                    };

                try
                {
                    foreach (IFormFile image in activityCreatingRequest.Images)
                    {
                        using (var stream = image.OpenReadStream())
                        {
                            string imageName =
                                Guid.NewGuid().ToString() + Path.GetExtension(image.FileName);
                            string imageUrl = await _firebaseStorageService.UploadImageToFirebase(
                                stream,
                                imageName
                            );
                            imageUrls.Add(imageUrl);
                        }
                    }

                    activity.Images = string.Join(",", imageUrls);
                    if (await _activityRepository.UpdateActivityAsync(activity) != 1)
                        return new CommonResponse
                        {
                            Status = 500,
                            Message = _config["ResponseMessages:CommonMsg:InternalServerErrorMsg"]
                        };

                    scope.Complete();
                    return new CommonResponse
                    {
                        Status = 200,
                        Data = activity.Id,
                        Message = _config["ResponseMessages:ActivityMsg:CreateActivitySuccessMsg"]
                    };
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "An exception occurred in service ActivityService, method CreateActivityAsync."
                    );
                    imageUrls.ForEach(url => _firebaseStorageService.DeleteImageAsync(url));
                    return new CommonResponse
                    {
                        Status = 500,
                        Message = _config["ResponseMessages:CommonMsg:UploadImageFailedMsg"]
                    };
                }
            }
        }

        public async Task<CommonResponse> GetActivitiesAsync(
            string? name,
            ActivityStatus? status,
            ActivityScope? scope,
            List<Guid>? activityTypeIds,
            DateTime? startDate,
            DateTime? endDate,
            bool? isJoined,
            Guid? userId,
            Guid? callerId,
            Guid? branchId,
            string? address,
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
            List<Activity> activities = new();
            List<TargetProcess> targetProcesses = new();
            Branch? branch = null;
            if (callerId == null)
            {
                if (
                    isJoined == true
                    || scope == ActivityScope.INTERNAL
                    || status == ActivityStatus.INACTIVE
                    || userId != null
                )
                {
                    return new CommonResponse { Status = 403, Message = UnauthenticationMsg };
                }
                else
                {
                    scope = ActivityScope.PUBLIC;
                }
            }
            else
            {
                if (userRoleName == RoleEnum.SYSTEM_ADMIN.ToString())
                {
                    if (isJoined == true)
                    {
                        return new CommonResponse { Status = 403, Message = UnauthenticationMsg };
                    }
                }
                else if (userRoleName == RoleEnum.BRANCH_ADMIN.ToString())
                {
                    branch = await _branchRepository.FindBranchByBranchAdminIdAsync((Guid)callerId);
                    if (isJoined == true)
                    {
                        if (branch == null)
                        {
                            return new CommonResponse
                            {
                                Status = 403,
                                Message = UnauthenticationMsg
                            };
                        }
                        branchId = branch.Id;
                    }
                }
                else if (userRoleName == RoleEnum.CONTRIBUTOR.ToString())
                {
                    if (scope == ActivityScope.INTERNAL || userId != null)
                    {
                        return new CommonResponse { Status = 403, Message = UnauthenticationMsg };
                    }
                    if (isJoined == true)
                    {
                        userId = callerId;
                    }
                }
                else if (userRoleName == RoleEnum.CHARITY.ToString())
                {
                    if (isJoined == true)
                    {
                        return new CommonResponse { Status = 403, Message = UnauthenticationMsg };
                    }
                }
            }
            activities = await _activityRepository.GetActivitiesAsync(
                name,
                status,
                scope,
                activityTypeIds,
                startDate,
                endDate,
                userId,
                branchId,
                address,
                userRoleName
            );
            if (
                orderBy != null
                && sortType != null
                && (sortType == SortType.ASC || sortType == SortType.DES)
            )
            {
                try
                {
                    if (orderBy == "StartDate")
                    {
                        if (sortType == SortType.ASC)
                            activities = activities
                                .OrderByDescending(activity => activity.StartDate.HasValue)
                                .ThenBy(activity => activity.StartDate)
                                .ThenBy(activity => activity.EstimatedStartDate)
                                .ToList();
                        else
                            activities = activities
                                .OrderByDescending(activity => activity.StartDate)
                                .ThenByDescending(activity => activity.EstimatedStartDate)
                                .ToList();
                    }
                    else if (orderBy == "EndDate")
                    {
                        if (sortType == SortType.ASC)
                            activities = activities
                                .OrderByDescending(activity => activity.EndDate.HasValue)
                                .ThenBy(activity => activity.EndDate)
                                .ThenBy(activity => activity.EstimatedEndDate)
                                .ToList();
                        else
                            activities = activities
                                .OrderByDescending(activity => activity.EndDate)
                                .ThenByDescending(activity => activity.EstimatedEndDate)
                                .ToList();
                    }
                    else
                    {
                        if (sortType == SortType.ASC)
                            activities = activities
                                .OrderBy(activity => GetPropertyValue(activity, orderBy))
                                .ToList();
                        else
                            activities = activities
                                .OrderByDescending(activity => GetPropertyValue(activity, orderBy))
                                .ToList();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "An exception occurred in service method GetActivitiesAsync."
                    );
                    return new CommonResponse
                    {
                        Status = 400,
                        Message = _config["ResponseMessages:CommonMsg:SortedFieldNotFoundMsg"]
                    };
                }
            }

            Pagination pagination = new();
            pagination.PageSize = pageSize == null ? 10 : pageSize.Value;
            pagination.CurrentPage = page == null ? 1 : page.Value;
            pagination.Total = activities.Count;
            activities = activities
                .Skip((pagination.CurrentPage - 1) * pagination.PageSize)
                .Take(pagination.PageSize)
                .ToList();
            if (
                userRoleName != RoleEnum.SYSTEM_ADMIN.ToString()
                && userRoleName != RoleEnum.BRANCH_ADMIN.ToString()
            )
            {
                foreach (var a in activities)
                {
                    a.TargetProcesses =
                        await _targetProcessRepository.FindTargetProcessesByActivityIdAsync(a.Id);

                    foreach (var tp in a.TargetProcesses)
                    {
                        var item = await _itemRepository.FindItemByIdAsync(tp.ItemId);
                        tp.Item = item!;
                    }
                }
            }

            List<ActivityForAdminResponse> activityForAdminResponses = new();
            List<ActivityForUserResponse> activityForUserResponses = new();
            if (
                userRoleName == RoleEnum.SYSTEM_ADMIN.ToString()
                || userRoleName == RoleEnum.BRANCH_ADMIN.ToString()
            )
            {
                foreach (Activity a in activities)
                {
                    activityForAdminResponses.Add(
                        new ActivityForAdminResponse
                        {
                            Id = a.Id,
                            Name = a.Name,
                            Address = a.Address,
                            CreatedDate = a.CreatedDate,
                            StartDate = a.StartDate,
                            EndDate = a.EndDate,
                            EstimatedStartDate = a.EstimatedStartDate,
                            EstimatedEndDate = a.EstimatedEndDate,
                            Status = a.Status.ToString(),
                            Scope = a.Scope.ToString(),
                            IsJoined =
                                branch == null
                                    ? false
                                    : a.ActivityBranches.Any(ab => ab.BranchId == branch.Id),
                            ActivityTypeComponents = a.ActivityTypeComponents
                                .Select(atc => atc.ActivityType.Name)
                                .ToList(),
                            BranchResponses = a.ActivityBranches
                                .Select(
                                    ab =>
                                        new BranchResponse
                                        {
                                            Id = ab.Branch.Id,
                                            Name = ab.Branch.Name,
                                            Address = ab.Branch.Address,
                                            Image = ab.Branch.Image,
                                            CreatedDate = ab.Branch.CreatedDate,
                                            Status = ab.Branch.Status.ToString()
                                        }
                                )
                                .ToList()
                        }
                    );
                }
            }
            else
            {
                foreach (Activity a in activities)
                {
                    activityForUserResponses.Add(
                        new ActivityForUserResponse
                        {
                            Id = a.Id,
                            Name = a.Name,
                            Address = a.Address,
                            StartDate = a.StartDate,
                            EndDate = a.EndDate,
                            EstimatedStartDate = a.EstimatedStartDate,
                            EstimatedEndDate = a.EstimatedEndDate,
                            Status = a.Status.ToString(),
                            Description = a.Description,
                            Images = a.Images.Split(",").ToList(),
                            Scope = a.Scope.ToString(),
                            IsNearby = false,
                            IsJoined = a.ActivityMembers.Any(
                                am =>
                                    am.UserId == callerId
                                    && am.Status == ActivityMemberStatus.ACTIVE
                            ),
                            TotalTargetProcessPercentage = GetTotalTargetProcessPercentage(
                                a.TargetProcesses
                            ),
                            ActivityTypeComponents = a.ActivityTypeComponents
                                .Select(atc => atc.ActivityType.Name)
                                .ToList(),
                            TargetProcessResponses = a.TargetProcesses
                                .Select(
                                    tp =>
                                        new TargetProcessResponse
                                        {
                                            Target = tp.Target,
                                            Process = tp.Process,
                                            ItemTemplateResponse = new ItemResponse
                                            {
                                                Id = tp.Item.Id,
                                                Name = tp.Item.ItemTemplate.Name,
                                                AttributeValues = tp.Item.ItemAttributeValues
                                                    .Select(itav => itav.AttributeValue.Value)
                                                    .ToList(),
                                                Unit = tp.Item.ItemTemplate.Unit.Name
                                            }
                                        }
                                )
                                .ToList(),
                            BranchResponses = a.ActivityBranches
                                .Select(
                                    ab =>
                                        new BranchResponse
                                        {
                                            Id = ab.Branch.Id,
                                            Name = ab.Branch.Name,
                                            Address = ab.Branch.Address,
                                            Image = ab.Branch.Image,
                                            CreatedDate = ab.Branch.CreatedDate,
                                            Status = ab.Branch.Status.ToString()
                                        }
                                )
                                .ToList()
                        }
                    );
                }
            }
            return new CommonResponse
            {
                Status = 200,
                Data =
                    userRoleName == RoleEnum.SYSTEM_ADMIN.ToString()
                    || userRoleName == RoleEnum.BRANCH_ADMIN.ToString()
                        ? activityForAdminResponses
                        : activityForUserResponses,
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

        public async Task<CommonResponse> GetActivityAsync(
            Guid id,
            Guid? userId,
            string? userRoleName
        )
        {
            string UnauthenticationMsg = _config[
                "ResponseMessages:AuthenticationMsg:UnauthenticationMsg"
            ];
            Activity? activity = await _activityRepository.FindActivityByIdForDetailAsync(id);
            bool isJoined = false;
            if (activity == null)
                return new CommonResponse
                {
                    Status = 400,
                    Message = _config["ResponseMessages:ActivityMsg:ActivityNotFoundMsg"]
                };
            if (userId != null)
            {
                if (userRoleName == RoleEnum.BRANCH_ADMIN.ToString())
                {
                    isJoined = activity.ActivityBranches.Any(
                        ab => ab.Branch.BranchAdminId == userId
                    );
                }
                else if (userRoleName == RoleEnum.CONTRIBUTOR.ToString())
                {
                    if (
                        activity.Scope == ActivityScope.INTERNAL
                        || activity.Status == ActivityStatus.INACTIVE
                    )
                        return new CommonResponse { Status = 403, Message = UnauthenticationMsg };

                    isJoined = activity.ActivityMembers.Any(
                        am => am.Status == ActivityMemberStatus.ACTIVE && am.UserId == userId
                    );
                }
                else if (userRoleName != RoleEnum.SYSTEM_ADMIN.ToString())
                {
                    if (activity.Scope == ActivityScope.INTERNAL)
                        return new CommonResponse { Status = 403, Message = UnauthenticationMsg };
                }
            }
            else
            {
                if (
                    activity.Scope == ActivityScope.INTERNAL
                    || activity.Status == ActivityStatus.INACTIVE
                )
                    return new CommonResponse { Status = 403, Message = UnauthenticationMsg };
            }
            User? creater = await _userRepository.FindUserByIdAsync(activity.CreatedBy);
            activity.TargetProcesses =
                await _targetProcessRepository.FindTargetProcessesByActivityIdAsync(activity.Id);
            foreach (var tp in activity.TargetProcesses)
            {
                var itemTemplate = await _itemRepository.FindItemByIdAsync(tp.ItemId);
                tp.Item = itemTemplate!;
            }
            return new CommonResponse
            {
                Status = 200,
                Data = new ActivityDetailResponse
                {
                    Id = activity.Id,
                    Name = activity.Name,
                    Location = activity.Location,
                    Address = activity.Address,
                    StartDate = activity.StartDate,
                    EndDate = activity.EndDate,
                    EstimatedStartDate = activity.EstimatedStartDate,
                    EstimatedEndDate = activity.EstimatedEndDate,
                    DeliveringDate = activity.DeliveringDate,
                    Status = activity.Status.ToString(),
                    Description = activity.Description,
                    Images = activity.Images.Split(",").ToList(),
                    Scope = activity.Scope.ToString(),
                    IsNearby = false,
                    NumberOfParticipants = activity.ActivityMembers
                        .Where(am => am.Status == ActivityMemberStatus.ACTIVE)
                        .Count(),
                    TotalTargetProcessPercentage = GetTotalTargetProcessPercentage(
                        activity.TargetProcesses
                    ),
                    ActivityTypeComponents = activity.ActivityTypeComponents
                        .Select(atc => atc.ActivityType.Name)
                        .ToList(),
                    TargetProcessResponses = activity.TargetProcesses
                        .Select(
                            tp =>
                                new TargetProcessResponse
                                {
                                    Target = tp.Target,
                                    Process = tp.Process,
                                    ItemTemplateResponse = new ItemResponse
                                    {
                                        Id = tp.Item.Id,
                                        Name = tp.Item.ItemTemplate.Name,
                                        Image = tp.Item.Image,
                                        AttributeValues = tp.Item.ItemAttributeValues
                                            .Select(itav => itav.AttributeValue.Value)
                                            .ToList(),
                                        Unit = tp.Item.ItemTemplate.Unit.Name
                                    }
                                }
                        )
                        .ToList(),
                    IsJoined = isJoined,
                    BranchResponses = activity.ActivityBranches
                        .Select(
                            ab =>
                                new BranchResponse
                                {
                                    Id = ab.Branch.Id,
                                    Name = ab.Branch.Name,
                                    Address = ab.Branch.Address,
                                    Location = _openRouteService.GetCoordinatesByLocation(
                                        ab.Branch.Location
                                    ),
                                    Image = ab.Branch.Image,
                                    CreatedDate = ab.Branch.CreatedDate,
                                    Status = ab.Branch.Status.ToString()
                                }
                        )
                        .ToList(),
                    Creater =
                        creater != null
                            ? new SimpleUserResponse
                            {
                                Id = creater.Id,
                                Phone =
                                    (
                                        userRoleName == RoleEnum.BRANCH_ADMIN.ToString()
                                        || userRoleName == RoleEnum.SYSTEM_ADMIN.ToString()
                                    )
                                        ? creater.Phone
                                        : null,
                                Email =
                                    (
                                        userRoleName == RoleEnum.BRANCH_ADMIN.ToString()
                                        || userRoleName == RoleEnum.SYSTEM_ADMIN.ToString()
                                    )
                                        ? creater.Email
                                        : null,
                                FullName = creater.Name!,
                                Avatar = creater.Avatar,
                                Role = creater.Role.Name
                            }
                            : new SimpleUserResponse { }
                },
                Message = _config["ResponseMessages:ActivityMsg:GetActivitySuccessMsg"]
            };
        }

        public async Task<CommonResponse> UpdateActivityAsync(
            ActivityUpdatingRequest activityUpdatingRequest,
            Guid userId,
            string userRoleName
        )
        {
            List<string> newImageUrls = new();
            Activity? activity = await _activityRepository.FindActivityByIdAsync(
                activityUpdatingRequest.Id
            );
            if (activity == null)
                return new CommonResponse
                {
                    Status = 400,
                    Message = _config["ResponseMessages:ActivityMsg:ActivityNotFoundMsg"]
                };

            User? creater = await _userRepository.FindUserByIdAsync(activity.CreatedBy);
            if (creater!.Role.Name == RoleEnum.SYSTEM_ADMIN.ToString())
            {
                if (userRoleName == RoleEnum.BRANCH_ADMIN.ToString())
                {
                    return new CommonResponse
                    {
                        Status = 403,
                        Message = _config[
                            "ResponseMessages:ActivityMsg:OnlySystemAdminCanUpdateMsg"
                        ]
                    };
                }
            }
            else
            {
                if (userRoleName == RoleEnum.BRANCH_ADMIN.ToString())
                {
                    if (
                        !activity.ActivityBranches.Any(
                            ab =>
                                ab.Branch.BranchAdminId == userId
                                && ab.Branch.Status == BranchStatus.ACTIVE
                        )
                    )
                        return new CommonResponse
                        {
                            Status = 403,
                            Message = _config[
                                "ResponseMessages:ActivityMsg:OnlyBranchAdminFromTheSameBranchOrSystemAdminCanUpdateMsg"
                            ]
                        };
                }
            }

            if (activity.EstimatedStartDate != activityUpdatingRequest.EstimatedStartDate)
            {
                if (activityUpdatingRequest.Status != ActivityStatus.NOT_STARTED)
                {
                    return new CommonResponse
                    {
                        Status = 400,
                        Message = _config[
                            "ResponseMessages:ActivityMsg:CanNotUpdateEstimatedStartDate"
                        ]
                    };
                }
                else if (
                    !(
                        activityUpdatingRequest.EstimatedStartDate >= activity.CreatedDate
                        && activityUpdatingRequest.EstimatedStartDate
                            >= SettedUpDateTime.GetCurrentVietNamTimeWithDateOnly()
                        && activityUpdatingRequest.EstimatedStartDate
                            <= activity.CreatedDate.AddMonths(3)
                    )
                )
                {
                    return new CommonResponse
                    {
                        Status = 400,
                        Message = _config["ResponseMessages:ActivityMsg:EstimatedStartDateLimitMsg"]
                    };
                }
                else
                {
                    activity.EstimatedStartDate = activityUpdatingRequest.EstimatedStartDate;
                }
            }
            if (activity.EstimatedEndDate != activityUpdatingRequest.EstimatedEndDate)
            {
                if (activityUpdatingRequest.Status == ActivityStatus.ENDED)
                {
                    return new CommonResponse
                    {
                        Status = 400,
                        Message = _config[
                            "ResponseMessages:ActivityMsg:CanNotUpdateEstimatedEndDate"
                        ]
                    };
                }
                else
                {
                    activity.EstimatedEndDate = activityUpdatingRequest.EstimatedEndDate;
                }
            }
            if (activity.DeliveringDate != activityUpdatingRequest.DeliveringDate)
            {
                activity.DeliveringDate = activityUpdatingRequest.DeliveringDate;
            }
            switch (activityUpdatingRequest.Status)
            {
                case ActivityStatus.NOT_STARTED:
                {
                    if (activity.Status != ActivityStatus.NOT_STARTED)
                    {
                        activity.Status = ActivityStatus.NOT_STARTED;
                        activity.StartDate = null;
                        activity.EndDate = null;
                    }
                    break;
                }
                case ActivityStatus.STARTED:
                {
                    if (activity.Status != ActivityStatus.STARTED)
                    {
                        activity.Status = ActivityStatus.STARTED;
                        activity.StartDate = SettedUpDateTime.GetCurrentVietNamTime();
                        activity.EndDate = null;
                    }
                    break;
                }
                case ActivityStatus.ENDED:
                {
                    if (activity.Status != ActivityStatus.ENDED)
                    {
                        activity.Status = ActivityStatus.ENDED;
                        activity.EndDate = SettedUpDateTime.GetCurrentVietNamTime();
                    }
                    break;
                }
            }

            activity.Name = activityUpdatingRequest.Name;
            activity.Description = activityUpdatingRequest.Description;

            using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                CommonResponse? updatedJoinedBranchesRs = await UpdateJoinedBranchesOfActivity(
                    activityUpdatingRequest,
                    userRoleName,
                    activity
                );
                if (updatedJoinedBranchesRs == null)
                {
                    if (userRoleName == RoleEnum.SYSTEM_ADMIN.ToString())
                    {
                        if (
                            !activityUpdatingRequest.Address.IsNullOrEmpty()
                            && !activityUpdatingRequest.Location.IsNullOrEmpty()
                        )
                        {
                            activity.Address = activityUpdatingRequest.Address;
                            activity.Location = string.Join(",", activityUpdatingRequest.Location!);
                        }
                        else
                        {
                            if (activityUpdatingRequest.BranchIds!.Count == 1)
                            {
                                Branch? organizeBranch =
                                    await _branchRepository.FindActiveBranchByIdAsync(
                                        activityUpdatingRequest.BranchIds[0]
                                    );
                                activity.Address = organizeBranch!.Address;
                                activity.Location = organizeBranch!.Location;
                            }
                        }
                    }
                    else
                    {
                        if (
                            !activityUpdatingRequest.Address.IsNullOrEmpty()
                            && !activityUpdatingRequest.Location.IsNullOrEmpty()
                        )
                        {
                            activity.Address = activityUpdatingRequest.Address;
                            activity.Location = string.Join(",", activityUpdatingRequest.Location!);
                        }
                    }
                    CommonResponse? updatedActivityTypesRs = await UpdateActivityTypesOfActivity(
                        activityUpdatingRequest,
                        activity
                    );
                    if (updatedActivityTypesRs == null)
                    {
                        try
                        {
                            if (activityUpdatingRequest.Images != null)
                            {
                                foreach (IFormFile image in activityUpdatingRequest.Images)
                                {
                                    using (var stream = image.OpenReadStream())
                                    {
                                        string imageName =
                                            Guid.NewGuid().ToString()
                                            + Path.GetExtension(image.FileName);
                                        string imageUrl =
                                            await _firebaseStorageService.UploadImageToFirebase(
                                                stream,
                                                imageName
                                            );
                                        newImageUrls.Add(imageUrl);
                                    }
                                }
                                activity.Images
                                    .Split(",")
                                    .ToList()
                                    .ForEach(url => _firebaseStorageService.DeleteImageAsync(url));
                                activity.Images = string.Join(",", newImageUrls);
                            }
                            if (await _activityRepository.UpdateActivityAsync(activity) == 1)
                            {
                                scope.Complete();
                                return new CommonResponse
                                {
                                    Status = 200,
                                    Message = _config[
                                        "ResponseMessages:ActivityMsg:UpdateActivitySuccessMsg"
                                    ]
                                };
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(
                                ex,
                                "An exception occurred in service ActivityService, method UpdateActivityAsync."
                            );
                            newImageUrls.ForEach(
                                url => _firebaseStorageService.DeleteImageAsync(url)
                            );
                            return new CommonResponse
                            {
                                Status = 500,
                                Message = _config["ResponseMessages:CommonMsg:UploadImageFailedMsg"]
                            };
                        }
                    }
                    else
                        return updatedActivityTypesRs;
                }
                else
                    return updatedJoinedBranchesRs;
                return new CommonResponse
                {
                    Status = 500,
                    Message = _config["ResponseMessages:CommonMsg:InternalServerErrorMsg"]
                };
            }
        }

        private async Task<CommonResponse?> UpdateJoinedBranchesOfActivity(
            ActivityUpdatingRequest activityUpdatingRequest,
            string userRoleName,
            Activity activity
        )
        {
            List<Guid> currentJoinedBranchIds = new();
            List<Guid> lostJoinedBranchIds = new();
            List<Guid> newJoinedBranchIds = new();

            if (userRoleName == RoleEnum.BRANCH_ADMIN.ToString())
            {
                if (
                    activityUpdatingRequest.BranchIds != null
                    && activityUpdatingRequest.BranchIds.Count > 0
                )
                {
                    return new CommonResponse
                    {
                        Status = 403,
                        Message = _config[
                            "ResponseMessages:ActivityMsg:UserNotAllowToAssignBranchesMsg"
                        ]
                    };
                }
            }
            else
            {
                if (
                    !(
                        activityUpdatingRequest.BranchIds != null
                        && activityUpdatingRequest.BranchIds.Count > 0
                    )
                )
                {
                    return new CommonResponse
                    {
                        Status = 400,
                        Message = _config["ResponseMessages:ActivityMsg:JoinedBranchsListEmptyMsg"]
                    };
                }
                else
                {
                    foreach (Guid branchId in activityUpdatingRequest.BranchIds)
                    {
                        Branch? joinedBranch = await _branchRepository.FindBranchByIdAsync(
                            branchId
                        );
                        if (joinedBranch == null)
                            return new CommonResponse
                            {
                                Status = 400,
                                Message = _config[
                                    "ResponseMessages:BranchMsg:BranchNotFoundInListMsg"
                                ]
                            };
                        else if (joinedBranch.Status == BranchStatus.INACTIVE)
                            return new CommonResponse
                            {
                                Status = 400,
                                Message = _config[
                                    "ResponseMessages:BranchMsg:InactiveBranchInListMsg"
                                ]
                            };
                    }

                    currentJoinedBranchIds = activity.ActivityBranches
                        .Select(ab => ab.BranchId)
                        .ToList();
                    lostJoinedBranchIds = currentJoinedBranchIds
                        .Except(activityUpdatingRequest.BranchIds)
                        .ToList();
                    newJoinedBranchIds = activityUpdatingRequest.BranchIds
                        .Except(activity.ActivityBranches.Select(ab => ab.BranchId))
                        .ToList();
                }
            }

            int addRs =
                newJoinedBranchIds.Count > 0
                    ? await _activityBranchRepository.CreateActivityBranchesAsync(
                        newJoinedBranchIds
                            .Select(
                                branchId =>
                                    new ActivityBranch
                                    {
                                        ActivityId = activity.Id,
                                        BranchId = branchId
                                    }
                            )
                            .ToList()
                    )
                    : 0;
            if (addRs == newJoinedBranchIds.Count)
            {
                int removeRs = 0;
                if (lostJoinedBranchIds.Count > 0)
                {
                    List<ActivityBranch> lostActivityBranches = new();
                    foreach (Guid branchId in lostJoinedBranchIds)
                    {
                        ActivityBranch? activityBranch =
                            await _activityBranchRepository.FindActivityBranchByActivityIdAndBranchIdAsync(
                                activity.Id,
                                branchId
                            );
                        lostActivityBranches.Add(activityBranch!);
                    }
                    removeRs = await _activityBranchRepository.DeleteActivityBranchesAsync(
                        lostActivityBranches
                    );
                }
                if (removeRs == lostJoinedBranchIds.Count)
                {
                    return null;
                }
            }
            return new CommonResponse
            {
                Status = 500,
                Message = _config["ResponseMessages:CommonMsg:InternalServerErrorMsg"]
            };
        }

        private async Task<CommonResponse?> UpdateActivityTypesOfActivity(
            ActivityUpdatingRequest activityUpdatingRequest,
            Activity activity
        )
        {
            foreach (Guid activityTypeId in activityUpdatingRequest.ActivityTypeIds)
            {
                ActivityType? activityType =
                    await _activityTypeRepository.FindActivityTypeByIdAsync(activityTypeId);
                if (activityType == null)
                    return new CommonResponse
                    {
                        Status = 400,
                        Message = _config[
                            "ResponseMessages:ActivityTypeMsg:ActivityTypeNotFoundMsg"
                        ]
                    };
            }

            List<Guid> currentActivityTypeIds = activity.ActivityTypeComponents
                .Select(ab => ab.ActivityTypeId)
                .ToList();
            List<Guid> lostActivityTypeIds = currentActivityTypeIds
                .Except(activityUpdatingRequest.ActivityTypeIds)
                .ToList();
            List<Guid> newActivityTypeIds = activityUpdatingRequest.ActivityTypeIds
                .Except(activity.ActivityTypeComponents.Select(ab => ab.ActivityTypeId))
                .ToList();

            int addRs =
                newActivityTypeIds.Count > 0
                    ? await _activityTypeComponentRepository.CreateActivityTypeComponentsAsync(
                        newActivityTypeIds
                            .Select(
                                activityTypeId =>
                                    new ActivityTypeComponent
                                    {
                                        ActivityId = activity.Id,
                                        ActivityTypeId = activityTypeId
                                    }
                            )
                            .ToList()
                    )
                    : 0;
            if (addRs == newActivityTypeIds.Count)
            {
                int removeRs = 0;
                if (lostActivityTypeIds.Count > 0)
                {
                    List<ActivityTypeComponent> lostActivityTypeComponents = new();
                    foreach (Guid activityTypeId in lostActivityTypeIds)
                    {
                        ActivityTypeComponent? activityTypeComponent =
                            await _activityTypeComponentRepository.FindActivityComponentByActivityIdAndActivityTypeIdAsync(
                                activity.Id,
                                activityTypeId
                            );
                        lostActivityTypeComponents.Add(activityTypeComponent!);
                    }
                    removeRs =
                        await _activityTypeComponentRepository.DeleteActivityTypeComponentsAsync(
                            lostActivityTypeComponents
                        );
                }
                if (removeRs == lostActivityTypeIds.Count)
                {
                    return null;
                }
            }
            return new CommonResponse
            {
                Status = 500,
                Message = _config["ResponseMessages:CommonMsg:InternalServerErrorMsg"]
            };
        }

        public async Task<CommonResponse> DeactivateActivityAsync(
            Guid activityId,
            Guid userId,
            string userRoleName
        )
        {
            Activity? activity = await _activityRepository.FindActivityByIdAsync(activityId);
            if (activity == null)
                return new CommonResponse
                {
                    Status = 400,
                    Message = _config["ResponseMessages:ActivityMsg:ActivityNotFoundMsg"]
                };

            User? creater = await _userRepository.FindUserByIdAsync(activity.CreatedBy);
            if (creater!.Role.Name == RoleEnum.SYSTEM_ADMIN.ToString())
            {
                if (userRoleName == RoleEnum.BRANCH_ADMIN.ToString())
                {
                    return new CommonResponse
                    {
                        Status = 403,
                        Message = _config[
                            "ResponseMessages:ActivityMsg:OnlySystemAdminCanUpdateMsg"
                        ]
                    };
                }
            }
            else
            {
                if (userRoleName == RoleEnum.BRANCH_ADMIN.ToString())
                {
                    if (
                        !activity.ActivityBranches.Any(
                            ab =>
                                ab.Branch.BranchAdminId == userId
                                && ab.Branch.Status == BranchStatus.ACTIVE
                        )
                    )
                        return new CommonResponse
                        {
                            Status = 403,
                            Message = _config[
                                "ResponseMessages:ActivityMsg:OnlyBranchAdminFromTheSameBranchOrSystemAdminCanUpdateMsg"
                            ]
                        };
                }
            }

            if (activity.Status == ActivityStatus.INACTIVE)
            {
                return new CommonResponse
                {
                    Status = 400,
                    Message = _config[
                        "ResponseMessages:ActivityMsg:ActivityIsAlreadyDeactivatedMsg"
                    ]
                };
            }
            else
                activity.Status = ActivityStatus.INACTIVE;

            using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                if (await _activityRepository.UpdateActivityAsync(activity) == 1)
                {
                    scope.Complete();
                    return new CommonResponse
                    {
                        Status = 200,
                        Message = _config[
                            "ResponseMessages:ActivityMsg:DeactivateActivitySuccessMsg"
                        ]
                    };
                }
                return new CommonResponse
                {
                    Status = 500,
                    Message = _config["ResponseMessages:CommonMsg:InternalServerErrorMsg"]
                };
            }
        }

        public async Task<CommonResponse> CountActivityByAllStatus(
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
            CommonResponse commonResponse = new();
            try
            {
                int total = await _activityRepository.CountActivityByStatus(
                    null,
                    startDate,
                    endDate,
                    branchId
                );
                int numberOfNotStated = await _activityRepository.CountActivityByStatus(
                    ActivityStatus.NOT_STARTED,
                    startDate,
                    endDate,
                    branchId
                );
                int numberOfStarted = await _activityRepository.CountActivityByStatus(
                    ActivityStatus.STARTED,
                    startDate,
                    endDate,
                    branchId
                );
                int numberOfEnded = await _activityRepository.CountActivityByStatus(
                    ActivityStatus.ENDED,
                    startDate,
                    endDate,
                    branchId
                );
                int numberOfInactive = await _activityRepository.CountActivityByStatus(
                    ActivityStatus.INACTIVE,
                    startDate,
                    endDate,
                    branchId
                );

                var rs = new
                {
                    NumberOfNotStated = numberOfNotStated,
                    NumberOfStarted = numberOfStarted,
                    NumberOfInactive = numberOfInactive,
                    NumberOfEnded = numberOfEnded,
                    Total = total
                };
                commonResponse.Data = rs;
                commonResponse.Status = 200;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    $"An exception occurred in service {nameof(ActivityService)}, method {nameof(CountActivityByAllStatus)}."
                );
                return new CommonResponse
                {
                    Status = 500,
                    Message = _config["ResponseMessages:CommonMsg:InternalServerErrorMsg"]
                };
            }
            return commonResponse;
        }

        public async Task<CommonResponse> CountActivityByStatus(
            DateTime startDate,
            DateTime endDate,
            ActivityStatus? status,
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
            CommonResponse commonResponse = new();
            try
            {
                int total = await _activityRepository.CountActivityByStatus(
                    status,
                    startDate,
                    endDate,
                    branchId
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
                            tmp.Quantity = await _activityRepository.CountActivityByStatus(
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
                            StatisticObjectByTimeRangeResponse tmp = new();
                            tmp.From = currentDate;
                            tmp.To = currentDate.AddMonths(1);
                            tmp.Quantity = await _activityRepository.CountActivityByStatus(
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
                            StatisticObjectByTimeRangeResponse tmp = new();
                            tmp.From = currentDate;
                            tmp.To = currentDate.AddYears(1);
                            tmp.Quantity = await _activityRepository.CountActivityByStatus(
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
                            StatisticObjectByTimeRangeResponse tmp = new();
                            tmp.From = currentDate;
                            tmp.To = currentDate.AddDays(7);
                            tmp.Quantity = await _activityRepository.CountActivityByStatus(
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
                var rs = new { Total = total, ActivityByTimeRangeResponse = responses };
                commonResponse.Data = rs;
                commonResponse.Status = 200;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    $"An exception occurred in service {nameof(ActivityService)}, method {nameof(CountActivityByStatus)}."
                );
                return new CommonResponse
                {
                    Status = 500,
                    Message = _config["ResponseMessages:CommonMsg:InternalServerErrorMsg"]
                };
            }
            return commonResponse;
        }

        public async Task<CommonResponse> SearchActivityByItemId(Guid itemId, Guid userId)
        {
            CommonResponse commonResponse = new();
            try
            {
                var activities = await _activityRepository.FindActivityByItemId(itemId, userId);
                if (activities != null && activities.Count > 0)
                {
                    var rs = activities.Select(
                        a =>
                            new
                            {
                                a.Name,
                                a.Description,
                                a.Id
                            }
                    );

                    commonResponse.Data = rs.ToList();
                }
                commonResponse.Status = 200;
            }
            catch { }
            return commonResponse;
        }

        private double GetTotalTargetProcessPercentage(List<TargetProcess> targetProcesses)
        {
            if (targetProcesses.Count == 0)
                return 0;
            double maxComponentPercentage = 100 / targetProcesses.Count;
            double count = targetProcesses.Count;
            double rs = 0;

            foreach (TargetProcess tp in targetProcesses)
            {
                if (100 * tp.Process / tp.Target > 100)
                    rs += maxComponentPercentage;
                else
                    rs += 100 * tp.Process / tp.Target / count;
            }

            return Math.Round(rs, 2);
        }
    }
}
