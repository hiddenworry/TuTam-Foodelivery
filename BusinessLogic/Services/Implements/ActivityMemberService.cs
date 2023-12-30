using DataAccess.Repositories;
using System.Transactions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using DataAccess.Entities;
using DataAccess.EntityEnums;
using BusinessLogic.Utils.Notification.Implements;
using DataAccess.Models.Responses;
using DataAccess.Models.Requests.ModelBinders;
using DataAccess.Models.Requests;
using DataAccess.ModelsEnum;

namespace BusinessLogic.Services.Implements
{
    public class ActivityMemberService : IActivityMemberService
    {
        private readonly ILogger<ActivityMemberService> _logger;
        private readonly IConfiguration _config;
        private readonly IActivityMemberRepository _memberRepository;
        private readonly IHubContext<NotificationSignalSender> _hubContext;
        private readonly INotificationRepository _notificationRepository;
        private readonly IActivityRoleRepository _activityRoleRepository;
        private readonly IActivityRepository _activityRepository;
        private readonly IActivityBranchRepository _activityBranchRepository;
        private readonly IUserRepository _userRepository;

        public ActivityMemberService(
            ILogger<ActivityMemberService> logger,
            IConfiguration config,
            IActivityMemberRepository memberRepository,
            IHubContext<NotificationSignalSender> hubContext,
            INotificationRepository notificationRepository,
            IActivityRoleRepository activityRoleRepository,
            IActivityRepository activityRepository,
            IActivityBranchRepository activityBranchRepository,
            IUserRepository userRepository
        )
        {
            _logger = logger;
            _config = config;
            _memberRepository = memberRepository;
            _hubContext = hubContext;
            _notificationRepository = notificationRepository;
            _activityRoleRepository = activityRoleRepository;
            _activityRepository = activityRepository;
            _activityBranchRepository = activityBranchRepository;
            _userRepository = userRepository;
        }

        public async Task<CommonResponse> CreateActivityMemberApplication(
            Guid userId,
            ActivityApplicationRequest request
        )
        {
            string internalServerErrorMsg = _config[
                "ResponseMessages:CommonMsg:InternalServerErrorMsg"
            ];
            string createSuccessMsg = _config[
                "ResponseMessages:ActivityApplicationMsg:CreateSuccessMsg"
            ];
            string doNotAllowToSpam = _config[
                "ResponseMessages:ActivityApplicationMsg:DoNotAllowToSpam"
            ];
            CommonResponse commonResponse = new();
            try
            {
                List<ActivityMember>? listApplications =
                    await _memberRepository.FindMemberByUserIdAndActivityIdAsync(
                        userId,
                        request.ActivityId,
                        ActivityMemberStatus.UNVERIFIED
                    );

                if (listApplications != null && listApplications.Count > 0)
                {
                    commonResponse.Status = 400;
                    commonResponse.Message =
                        "Bạn đã gửi đơn tham gia hoạt động trước đó vui lòng chờ đợi sự kiểm duyệt từ admin.";
                    return commonResponse;
                }

                List<ActivityMember>? activeApplication =
                    await _memberRepository.FindMemberByUserIdAndActivityIdAsync(
                        userId,
                        request.ActivityId,
                        ActivityMemberStatus.ACTIVE
                    );

                if (activeApplication != null && activeApplication.Count > 0)
                {
                    commonResponse.Status = 400;
                    commonResponse.Message = "Bạn đã tham gia hoạt động này với 1 vai trò trước đó";
                    return commonResponse;
                }

                using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
                {
                    ActivityMember activityMember =
                        new()
                        {
                            ActivityId = request.ActivityId,
                            Status = ActivityMemberStatus.UNVERIFIED,
                            UserId = userId,
                            CreatedDate = SettedUpDateTime.GetCurrentVietNamTime(),
                            Description = request.Description,
                        };
                    ActivityMember member =
                        await _memberRepository.CreateActivityMemberReturnObjectAsync(
                            activityMember
                        );

                    List<RoleMember> roleMembers = new();

                    ActivityRole? activityRole = await _activityRoleRepository.GetActivityRoleById(
                        request.RoleMemberId
                    );
                    if (activityRole != null)
                    {
                        RoleMember roleMember =
                            new()
                            {
                                ActivityMemberId = member.Id,
                                ActivityRoleId = activityRole.Id,
                            };
                        roleMembers.Add(roleMember);
                    }

                    activityMember.RoleMembers = roleMembers;

                    await _memberRepository.UpdateActivityMemberAsync(member);
                    scope.Complete();
                    commonResponse.Status = 200;
                    commonResponse.Message = createSuccessMsg;
                }
            }
            catch (Exception ex)
            {
                string className = nameof(ActivityMemberService);
                string methodName = nameof(CreateActivityMemberApplication);
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

        public async Task<CommonResponse> ConfirmMemberApplication(
            Guid memberId,
            Guid userId,
            ConfirmActivityApplicationRequest request
        )
        {
            string confirmSuccessMsg = _config[
                "ResponseMessages:ActivityApplicationMsg:ConfirmSuccessMsg"
            ];

            string internalServerErrorMsg = _config[
                "ResponseMessages:CommonMsg:InternalServerErrorMsg"
            ];
            string notificationImage = _config["Notification:Image"];
            CommonResponse commonResponse = new();
            try
            {
                ActivityMember? activityApplication =
                    await _memberRepository.FindActivityMemberByIdAsync(memberId);

                if (
                    activityApplication != null
                    && activityApplication.Status == ActivityMemberStatus.UNVERIFIED
                )
                {
                    User? user = await _userRepository.FindUserByIdInclueBranchAsync(userId);
                    ActivityBranch? activityBranch =
                        await _activityBranchRepository.FindActivityBranchByActivityIdAndBranchIdAsync(
                            activityApplication.ActivityId,
                            user!.Branch!.Id
                        );
                    if (activityBranch == null)
                    {
                        commonResponse.Status = 400;
                        commonResponse.Message = "Bạn không có quyền thực hiện hành động này.";
                        return commonResponse;
                    }
                    using (
                        var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled)
                    )
                    {
                        int rs = 0;
                        if (request.isAccept)
                        {
                            activityApplication.Status = ActivityMemberStatus.ACTIVE;
                            activityApplication.ConfirmedDate =
                                SettedUpDateTime.GetCurrentVietNamTime();
                            rs = await _memberRepository.UpdateActivityMemberAsync(
                                activityApplication
                            );

                            Notification notification =
                                new()
                                {
                                    Name = "Yêu cầu tham gia hoạt động đã được chấp thuận.",
                                    Content =
                                        "Bạn đã được chấp thuận tham gia vào hoạt động "
                                        + activityApplication.Activity.Name,
                                    CreatedDate = SettedUpDateTime.GetCurrentVietNamTime(),
                                    Image = _config["Notification:Image"],
                                    ReceiverId = activityApplication.UserId.ToString(),
                                    Status = NotificationStatus.NEW,
                                    Type = NotificationType.NOTIFYING,
                                    DataType = DataNotificationType.ACTIVITY,
                                    DataId = activityApplication.ActivityId
                                };
                            await _notificationRepository.CreateNotificationAsync(notification);
                            await _hubContext.Clients.All.SendAsync(
                                activityApplication.UserId.ToString(),
                                notification
                            );
                        }
                        else
                        {
                            activityApplication.ConfirmedDate =
                                SettedUpDateTime.GetCurrentVietNamTime();

                            ActivityMember? activityMember =
                                await _memberRepository.FindActivityMemberByActivityIdAndUserIdAsync(
                                    activityApplication.ActivityId,
                                    activityApplication.UserId
                                );
                            if (
                                activityMember != null
                                && activityMember.Status == ActivityMemberStatus.UNVERIFIED
                            )
                            {
                                await _memberRepository.DeleteActivityMemberAsync(activityMember);
                            }

                            Notification notification =
                                new()
                                {
                                    Name = "Yêu cầu tham gia hoạt động không được chấp thuận.",
                                    Content =
                                        "Rất tiếc yêu cầu tham gia hoạt động "
                                        + activityApplication.Activity.Name
                                        + " của bạn đã không được chấp thuận vì lí do: "
                                        + request.reason,
                                    CreatedDate = SettedUpDateTime.GetCurrentVietNamTime(),
                                    ReceiverId = activityApplication.UserId.ToString(),
                                    Status = NotificationStatus.NEW,
                                    Type = NotificationType.NOTIFYING,
                                    Image = notificationImage,
                                    DataType = DataNotificationType.ACTIVITY,
                                    DataId = activityApplication.ActivityId
                                };

                            await _notificationRepository.CreateNotificationAsync(notification);
                            await _hubContext.Clients.All.SendAsync(
                                activityApplication.UserId.ToString(),
                                notification
                            );
                        }
                        scope.Complete();
                        commonResponse.Status = 200;
                        commonResponse.Message = confirmSuccessMsg;
                    }
                }
                else
                {
                    commonResponse.Status = 400;
                    commonResponse.Message = "Không tìm thấy đơn theo yêu cầu";
                }
            }
            catch (Exception ex)
            {
                string className = nameof(ActivityMemberService);
                string methodName = nameof(ConfirmMemberApplication);
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

        public async Task<CommonResponse> GetActivityMemberApplication(
            Guid? activityId,
            Guid onwerId,
            ActivityMemberStatus? status,
            int? page,
            int? pageSize,
            SortType? sortType,
            string? role
        )
        {
            string internalServerErrorMsg = _config[
                "ResponseMessages:CommonMsg:InternalServerErrorMsg"
            ];
            CommonResponse commonResponse = new();
            try
            {
                List<ActivityMember>? activityApplications = new List<ActivityMember>();
                if (role == RoleEnum.BRANCH_ADMIN.ToString())
                {
                    activityApplications = await _memberRepository.FindMemberApplicationAsync(
                        onwerId,
                        activityId,
                        status
                    );
                }
                else if (role == RoleEnum.SYSTEM_ADMIN.ToString())
                {
                    activityApplications = await _memberRepository.FindMemberApplicationAsync(
                        null,
                        activityId,
                        status
                    );
                }

                if (activityApplications != null && activityApplications.Count > 0)
                {
                    Pagination pagination = new();
                    pagination.PageSize = pageSize == null ? 10 : pageSize.Value;
                    pagination.CurrentPage = page == null ? 1 : page.Value;
                    pagination.Total = activityApplications.Count;
                    activityApplications = activityApplications
                        .Skip((pagination.CurrentPage - 1) * pagination.PageSize)
                        .Take(pagination.PageSize)
                        .ToList();
                    var rs = activityApplications.Select(
                        a =>
                            new
                            {
                                a.Id,
                                a.CreatedDate,
                                a.ConfirmedDate,
                                a.Description,
                                Status = a.Status.ToString(),
                                a.ActivityId,
                                User = new
                                {
                                    a.User.Phone,
                                    Email = a.User.Email,
                                    Name = a.User.Name
                                }
                            }
                    );
                    if (sortType == SortType.ASC)
                    {
                        rs = rs.OrderBy(u => u.CreatedDate).ToList();
                    }
                    else
                    {
                        rs = rs.OrderByDescending(u => u.CreatedDate).ToList();
                    }
                    commonResponse.Data = rs;
                }
                commonResponse.Status = 200;
            }
            catch (Exception ex)
            {
                string className = nameof(ActivityMemberService);
                string methodName = nameof(GetActivityMemberApplication);
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

        public async Task<CommonResponse> CheckMemberOfActivity(Guid userId, Guid activityId)
        {
            CommonResponse commonResponse = new();
            string internalServerErrorMsg = _config[
                "ResponseMessages:CommonMsg:InternalServerErrorMsg"
            ];
            try
            {
                ActivityMember? activityMember =
                    await _memberRepository.FindActivityMemberByActivityIdAndUserIdAsync(
                        activityId,
                        userId
                    );
                if (activityMember != null)
                {
                    commonResponse.Data = true;
                    commonResponse.Status = 200;
                }
                else
                {
                    commonResponse.Data = false;
                    commonResponse.Status = 200;
                }
                return commonResponse;
            }
            catch (Exception ex)
            {
                string className = nameof(ActivityMemberService);
                string methodName = nameof(CheckMemberOfActivity);
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
    }
}
