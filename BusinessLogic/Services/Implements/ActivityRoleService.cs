using DataAccess.Entities;
using DataAccess.EntityEnums;
using DataAccess.Models.Requests;
using DataAccess.Models.Responses;
using DataAccess.ModelsEnum;
using DataAccess.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BusinessLogic.Services.Implements
{
    public class ActivityRoleService : IActivityRoleService
    {
        private readonly IActivityRoleRepository _activityRoleRepository;
        private readonly ILogger<ActivityRoleService> _logger;
        private readonly IConfiguration _config;
        private readonly IActivityRepository _activityRepository;
        private readonly IActivityBranchRepository _activityBranchRepository;
        private readonly IUserRepository _userRepository;
        private readonly IActivityTypeRepository _activityTypeRepository;

        public ActivityRoleService(
            IActivityRoleRepository activityRoleRepository,
            ILogger<ActivityRoleService> logger,
            IConfiguration configuration,
            IActivityRepository activityRepository,
            IActivityBranchRepository activityBranchRepository,
            IUserRepository userRepository,
            IActivityTypeRepository activityTypeRepository
        )
        {
            _activityRoleRepository = activityRoleRepository;
            _logger = logger;
            _config = configuration;
            _activityRepository = activityRepository;
            _activityBranchRepository = activityBranchRepository;
            _userRepository = userRepository;
            _activityTypeRepository = activityTypeRepository;
        }

        public async Task<CommonResponse> GetActivityRoleByActivtyId(Guid activityId)
        {
            CommonResponse commonResponse = new CommonResponse();
            string internalServerErrorMsg = _config[
                "ResponseMessages:CommonMsg:InternalServerErrorMsg"
            ];
            try
            {
                var rs = await _activityRoleRepository.GetListActivityRole(activityId);
                if (rs != null && rs.Count > 0)
                {
                    commonResponse.Data = rs.Select(
                            a =>
                                new
                                {
                                    a.Id,
                                    a.Name,
                                    Status = a.Status.ToString(),
                                    a.Description,
                                    a.IsDefault
                                }
                        )
                        .ToList();
                }

                commonResponse.Status = 200;
            }
            catch (Exception ex)
            {
                string className = nameof(ActivityRoleService);
                string methodName = nameof(GetActivityRoleByActivtyId);
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

        public async Task<CommonResponse> CreateActivityRoleByActivtyId(
            ActivityRoleCreatingRequest request,
            Guid userId
        )
        {
            CommonResponse commonResponse = new CommonResponse();
            string internalServerErrorMsg = _config[
                "ResponseMessages:CommonMsg:InternalServerErrorMsg"
            ];
            try
            {
                Activity? activity = await _activityRepository.FindActivityByIdAsync(
                    request.ActivityId
                );
                if (activity == null || activity.Status == ActivityStatus.ENDED)
                {
                    commonResponse.Status = 400;
                    commonResponse.Message =
                        "Không tìm thấy hoạt động tương ứng, hoặc hoạt động đã kết thúc.";
                }
                User? user = await _userRepository.FindUserByIdInclueBranchAsync(userId);

                if (activity != null && user != null && user.Branch != null)
                {
                    ActivityBranch? activityBranch =
                        await _activityBranchRepository.FindActivityBranchByActivityIdAndBranchIdAsync(
                            request.ActivityId,
                            user.Branch.Id
                        );
                    if (
                        activityBranch == null && user.Role.Name != RoleEnum.SYSTEM_ADMIN.ToString()
                    )
                    {
                        commonResponse.Status = 400;
                        commonResponse.Message = "Bạn không có quyền thực hiện hành động này.";
                        return commonResponse;
                    }
                }
                foreach (var r in request.ActivityRoleRequests)
                {
                    ActivityRole activityRole = new ActivityRole
                    {
                        Name = r.Name,
                        ActivityId = request.ActivityId,
                        Description = r.Description,
                        IsDefault = r.IsDefault,
                        Status = ActivityRoleStatus.ACTIVE
                    };
                    var rs = await _activityRoleRepository.CreateActivityRole(activityRole);
                }

                commonResponse.Status = 200;
                commonResponse.Message = "Tạo thành công";
            }
            catch (Exception ex)
            {
                string className = nameof(ActivityRoleService);
                string methodName = nameof(GetActivityRoleByActivtyId);
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

        public async Task<CommonResponse> UpdateActivityRoleById(
            List<ActivityRoleUpdatingRequest> request,
            Guid userId,
            Guid activityId
        )
        {
            CommonResponse commonResponse = new CommonResponse();
            string internalServerErrorMsg = _config[
                "ResponseMessages:CommonMsg:InternalServerErrorMsg"
            ];
            try
            {
                User? user = await _userRepository.FindUserByIdInclueBranchAsync(userId);
                Activity? activity = await _activityRepository.FindActivityByIdAsync(activityId);
                if (activity != null && user != null && user.Branch != null)
                {
                    ActivityBranch? activityBranch =
                        await _activityBranchRepository.FindActivityBranchByActivityIdAndBranchIdAsync(
                            activity.Id,
                            user.Branch.Id
                        );
                    if (
                        activityBranch == null && user.Role.Name != RoleEnum.SYSTEM_ADMIN.ToString()
                    )
                    {
                        commonResponse.Status = 400;
                        commonResponse.Message = "Bạn không có quyền thực hiện hành động này.";
                        return commonResponse;
                    }
                }
                foreach (var r in request)
                {
                    ActivityRole? activityRole = await _activityRoleRepository.GetActivityRoleById(
                        r.Id
                    );
                    if (activityRole != null)
                    {
                        activityRole.Name = r.Name;
                        activityRole.Description = r.Description;
                        activityRole.IsDefault = r.IsDefault;

                        var rs = await _activityRoleRepository.UpdateActivityRole(activityRole);
                    }

                    commonResponse.Status = 200;
                    commonResponse.Message = "Cập nhật thành công";
                }
            }
            catch (Exception ex)
            {
                string className = nameof(ActivityRoleService);
                string methodName = nameof(GetActivityRoleByActivtyId);
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

        public async Task<CommonResponse> GetListOfActivityRole(Guid activityId)
        {
            string internalServerErrorMsg = _config[
                "ResponseMessages:CommonMsg:InternalServerErrorMsg"
            ];
            CommonResponse commonResponse = new CommonResponse();
            try
            {
                List<ActivityRole>? activityRoles =
                    await _activityRoleRepository.GetListActivityRole(activityId);
                if (activityRoles != null && activityRoles.Count > 0)
                {
                    var rs = activityRoles.Select(
                        a =>
                            new
                            {
                                a.Id,
                                a.Name,
                                a.Description,
                                a.IsDefault,
                                Status = a.Status.ToString(),
                                a.ActivityId,
                            }
                    );
                    commonResponse.Data = rs;
                }
                commonResponse.Status = 200;
            }
            catch (Exception ex)
            {
                string className = nameof(ActivityRoleService);
                string methodName = nameof(GetActivityRoleByActivtyId);
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
