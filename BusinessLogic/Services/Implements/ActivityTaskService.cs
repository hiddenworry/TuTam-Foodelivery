using DataAccess.Entities;
using DataAccess.EntityEnums;
using DataAccess.Models.Requests;
using DataAccess.Models.Requests.ModelBinders;
using DataAccess.Models.Responses;
using DataAccess.ModelsEnum;
using DataAccess.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Transactions;

namespace BusinessLogic.Services.Implements
{
    public class ActivityTaskService : IActivityTaskService
    {
        private readonly IActivityTaskRepository _activityTaskRepository;
        private readonly ILogger<ActivityTaskService> _logger;
        private readonly IConfiguration _config;
        private readonly IRoleTaskRepository _roleTaskRepository;
        private readonly IRoleMemberRepository _roleMemberRepository;
        private readonly IActivityRoleRepository _activityRoleRepository;
        private readonly IUserRepository _userRepository;
        private readonly IActivityBranchRepository _activityBranchRepository;
        private readonly IPhaseRepository _phaseRepository;
        private readonly IActivityMemberRepository _activityMemberRepository;

        public ActivityTaskService(
            IActivityTaskRepository activityTaskRepository,
            ILogger<ActivityTaskService> logger,
            IConfiguration configuration,
            IRoleTaskRepository roleTaskRepository,
            IRoleMemberRepository roleMemberRepository,
            IActivityRoleRepository activityRoleRepository,
            IUserRepository userRepository,
            IActivityBranchRepository activityBranchRepository,
            IPhaseRepository phaseRepository,
            IActivityMemberRepository activityMemberRepository
        )
        {
            _activityTaskRepository = activityTaskRepository;
            _logger = logger;
            _config = configuration;
            _roleTaskRepository = roleTaskRepository;
            _roleMemberRepository = roleMemberRepository;
            _activityRoleRepository = activityRoleRepository;
            _userRepository = userRepository;
            _activityBranchRepository = activityBranchRepository;
            _phaseRepository = phaseRepository;
            _activityMemberRepository = activityMemberRepository;
        }

        public async Task<CommonResponse> CreateTask(TaskCreaingRequest request, Guid onwerId)
        {
            string internalServerErrorMsg = _config[
                "ResponseMessages:CommonMsg:InternalServerErrorMsg"
            ];
            CommonResponse commonResponse = new CommonResponse();
            try
            {
                Phase? phase = await _phaseRepository.GetPhaseByIdAsync(request.PhaseId);
                User? user = await _userRepository.FindUserByIdInclueBranchAsync(onwerId);
                if (phase == null)
                {
                    commonResponse.Status = 400;
                    commonResponse.Message = "Không tìm thấy phase tương ứng.";
                    return commonResponse;
                }
                ActivityBranch? activityBranch = null;
                if (user != null && user.Branch != null)
                {
                    activityBranch =
                        await _activityBranchRepository.FindActivityBranchByActivityIdAndBranchIdAsync(
                            phase.ActivityId,
                            user!.Branch!.Id
                        );
                }

                if (
                    user != null
                    && activityBranch == null
                    && user.Role.Name != RoleEnum.SYSTEM_ADMIN.ToString()
                )
                {
                    commonResponse.Status = 400;
                    commonResponse.Message = "Bạn không có quyền thực hiện hành động này.";
                    return commonResponse;
                }
                foreach (var t in request.TaskRequests)
                {
                    ActivityTask task = new ActivityTask();
                    task.Name = t.Name;

                    task.Status = ActivityTaskStatus.NOT_STARTED;
                    task.Description = t.Description;
                    task.PhaseId = request.PhaseId;
                    using (
                        var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled)
                    )
                    {
                        ActivityTask? rs =
                            await _activityTaskRepository.CreateTaskReturnObjectAsync(task);
                        if (rs == null)
                            throw new Exception();
                        if (t.ActivityRoleIds != null && t.ActivityRoleIds.Count > 0)
                        {
                            List<RoleTask> RoleTasks = new List<RoleTask>();
                            foreach (var a in t.ActivityRoleIds)
                            {
                                ActivityRole? activityRole =
                                    await _activityRoleRepository.GetActivityRoleById(a);
                                if (activityRole != null)
                                {
                                    RoleTasks.Add(
                                        new RoleTask { ActivityRoleId = a, ActivityTaskId = rs.Id }
                                    );
                                }
                            }
                            task.RoleTasks = RoleTasks;
                            int updatedResult = await _activityTaskRepository.UpdateTaskAsync(task);
                            if (updatedResult > 0)
                            {
                                commonResponse.Message = "Tạo Thành Công.";
                                commonResponse.Status = 200;
                            }
                        }
                        scope.Complete();
                    }
                }
            }
            catch (Exception ex)
            {
                string className = nameof(ActivityTaskService);
                string methodName = nameof(CreateTask);
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

        public async Task<CommonResponse> GetTask(
            int? page,
            int? pageSize,
            Guid ownerId,
            Guid? activityId,
            Guid? phaseId,
            string? Name,
            DateTime? StartDate,
            DateTime? EndDate
        )
        {
            string internalServerErrorMsg = _config[
                "ResponseMessages:CommonMsg:InternalServerErrorMsg"
            ];
            CommonResponse commonResponse = new CommonResponse();
            try
            {
                List<ActivityTask>? activityTasks = await _activityTaskRepository.GetTaskAsync(
                    ownerId,
                    activityId,
                    phaseId,
                    Name,
                    StartDate,
                    EndDate
                );
                if (activityTasks != null && activityTasks.Count > 0)
                {
                    Pagination pagination = new Pagination();
                    pagination.PageSize = pageSize == null ? 10 : pageSize.Value;
                    pagination.CurrentPage = page == null ? 1 : page.Value;
                    pagination.Total = activityTasks.Count;

                    activityTasks = activityTasks
                        .Skip((pagination.CurrentPage - 1) * pagination.PageSize)
                        .Take(pagination.PageSize)
                        .ToList();

                    var rs = activityTasks
                        .Select(
                            t =>
                                new
                                {
                                    t.Id,
                                    t.Name,
                                    t.StartDate,
                                    t.EndDate,
                                    Status = t.Status.ToString(),
                                    t.Description,
                                    t.PhaseId,
                                    ActivityRole = t.RoleTasks
                                        .Where(
                                            a =>
                                                a.ActivityRole != null
                                                && // Check if ActivityRole is not null
                                                a.ActivityRole.Status != ActivityRoleStatus.INACTIVE
                                        )
                                        .Select(
                                            a =>
                                                new
                                                {
                                                    a.ActivityRole.Id,
                                                    a.ActivityRole.Name,
                                                    a.ActivityRole.Description,
                                                    Status = a.ActivityRole.Status.ToString()
                                                }
                                        )
                                        .ToList()
                                }
                        )
                        .ToList();

                    commonResponse.Pagination = pagination;
                    commonResponse.Data = rs;
                }
                else
                {
                    commonResponse.Data = new List<ActivityTask>();
                }

                commonResponse.Status = 200;
            }
            catch (Exception ex)
            {
                string className = nameof(ActivityTaskService);
                string methodName = nameof(GetTask);
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

        public async Task<CommonResponse> UpdateTask(
            Guid onwerId,
            List<TaskUpdatingRequest> taskUpdatingRequest
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
                    foreach (var request in taskUpdatingRequest)
                    {
                        User? user = await _userRepository.FindUserByIdInclueBranchAsync(onwerId);

                        ActivityTask? task = await _activityTaskRepository.GetTaskByIdAsync(
                            request.Id
                        );
                        if (task != null)
                        {
                            task.Name = request.Name != null ? request.Name : task.Name;

                            task.Description =
                                request.Description != null
                                    ? request.Description
                                    : task.Description;

                            if (
                                request.ActivityRoleIds != null && request.ActivityRoleIds.Count > 0
                            )
                            {
                                var oldListRoleTask = task.RoleTasks;
                                foreach (var a in request.ActivityRoleIds)
                                {
                                    ActivityRole? activityRole =
                                        await _activityRoleRepository.GetActivityRoleById(a);
                                    if (activityRole != null)
                                    {
                                        if (
                                            !oldListRoleTask.Any(
                                                rt =>
                                                    rt.ActivityRoleId == a
                                                    && rt.ActivityTaskId == task.Id
                                            )
                                        )
                                        {
                                            task.RoleTasks.Add(
                                                new RoleTask
                                                {
                                                    ActivityRoleId = a,
                                                    ActivityTaskId = task.Id
                                                }
                                            );
                                        }
                                    }
                                }
                            }
                            if (request.PhaseId != null)
                            {
                                task.PhaseId = request.PhaseId.Value;
                            }
                            Phase? phase = await _phaseRepository.GetPhaseByIdAsync(task.PhaseId);
                            if (phase == null)
                            {
                                commonResponse.Status = 400;
                                commonResponse.Message = "Không tìm thấy phase tương ứng.";
                                return commonResponse;
                            }
                            ActivityBranch? activityBranch = null;
                            if (user != null && user.Branch != null)
                            {
                                activityBranch =
                                    await _activityBranchRepository.FindActivityBranchByActivityIdAndBranchIdAsync(
                                        phase.ActivityId,
                                        user!.Branch!.Id
                                    );
                            }

                            if (
                                user != null
                                && activityBranch == null
                                && user.Role.Name != RoleEnum.SYSTEM_ADMIN.ToString()
                            )
                            {
                                commonResponse.Status = 400;
                                commonResponse.Message =
                                    "Bạn không có quyền thực hiện hành động này.";
                                return commonResponse;
                            }
                            if (request.Status != null)
                            {
                                if (phase.Status != PhaseStatus.STARTED)
                                {
                                    commonResponse.Status = 400;
                                    commonResponse.Message =
                                        "Bạn cần phải bắt đầu 1 phase trước khi bắt đầu nhiệm vụ.";
                                    return commonResponse;
                                }
                                if (
                                    request.Status != null
                                    && request.Status == 0
                                    && task.Status == ActivityTaskStatus.NOT_STARTED
                                )
                                {
                                    task.StartDate = SettedUpDateTime.GetCurrentVietNamTime();
                                    task.Status = ActivityTaskStatus.ACTIVE;
                                }
                                else if (
                                    request.Status != null
                                    && request.Status == 1
                                    && task.Status == ActivityTaskStatus.ACTIVE
                                )
                                {
                                    task.EndDate = SettedUpDateTime.GetCurrentVietNamTime();
                                    task.Status = ActivityTaskStatus.DONE;
                                }
                            }
                            int updatedResult = await _activityTaskRepository.UpdateTaskAsync(task);
                            if (updatedResult <= 0)
                                throw new Exception();
                        }
                        else
                        {
                            commonResponse.Status = 400;
                            commonResponse.Message = "Không tìm thấy nhiệm vụ tương ứng.";
                            return commonResponse;
                        }
                    }
                    commonResponse.Message = "Cập nhật thành Công.";
                    commonResponse.Status = 200;
                    scope.Complete();
                }
            }
            catch (Exception ex)
            {
                string className = nameof(ActivityTaskService);
                string methodName = nameof(CreateTask);
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

        public async Task<CommonResponse> DeleteTask(Guid taskId, Guid ownerId)
        {
            CommonResponse commonResponse = new CommonResponse();
            string internalServerErrorMsg = _config[
                "ResponseMessages:CommonMsg:InternalServerErrorMsg"
            ];
            try
            {
                ActivityTask? activityTask = await _activityTaskRepository.GetTaskByIdAsync(taskId);
                User? user = await _userRepository.FindUserByIdInclueBranchAsync(ownerId);

                if (activityTask != null)
                {
                    ActivityBranch? activityBranch =
                        await _activityBranchRepository.FindActivityBranchByActivityIdAndBranchIdAsync(
                            activityTask.Phase.ActivityId,
                            user!.Branch!.Id
                        );
                    if (activityBranch == null)
                    {
                        commonResponse.Status = 400;
                        commonResponse.Message = "Bạn không có quyền thực hiện hành động này.";
                        return commonResponse;
                    }
                    int rs = await _activityTaskRepository.DeleteTaskAsync(activityTask);
                    if (rs < 0)
                        throw new Exception();
                    commonResponse.Status = 200;
                    commonResponse.Message = "Cập nhật thành công.";
                }
                else
                {
                    commonResponse.Status = 400;
                    commonResponse.Message = "Không tìm thấy nhiệm vụ này.";
                }
            }
            catch (Exception ex)
            {
                string className = nameof(ActivityTaskService);
                string methodName = nameof(DeleteTask);
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

        public async Task<CommonResponse> GetTaskDetail(Guid? taskId, Guid onwerId)
        {
            string internalServerErrorMsg = _config[
                "ResponseMessages:CommonMsg:InternalServerErrorMsg"
            ];
            CommonResponse commonResponse = new CommonResponse();
            try
            {
                ActivityTask? activityTask = await _activityTaskRepository.GetTaskDetailAsync(
                    onwerId,
                    taskId
                );
                if (activityTask != null)
                {
                    List<ActivityMember>? members =
                        await _activityMemberRepository.FindMemberByActivityIdAsync(
                            activityTask.Phase.ActivityId,
                            ActivityMemberStatus.ACTIVE
                        );
                    var activityMembers = members
                        ?.Select(
                            m =>
                                new
                                {
                                    m.User.Name,
                                    m.User.Id,
                                    m.User.Phone,
                                    CreateDate = m.CreatedDate,
                                    Status = m.Status.ToString()
                                }
                        )
                        .ToList();

                    var activeRoles = activityTask.RoleTasks
                        .Where(
                            a =>
                                a.ActivityRole != null
                                && a.ActivityRole.Status != ActivityRoleStatus.INACTIVE
                        )
                        .Select(
                            a =>
                                new
                                {
                                    a.ActivityRole.Id,
                                    a.ActivityRole.Name,
                                    a.ActivityRole.Description,
                                    Status = a.ActivityRole.Status.ToString()
                                }
                        )
                        .ToList();

                    var result = new
                    {
                        activityTask.Id,
                        activityTask.Name,
                        activityTask.StartDate,
                        activityTask.EndDate,
                        Status = activityTask.Status.ToString(),
                        activityTask.PhaseId,
                        ActivityRole = activeRoles ?? null,
                        ActivityMember = activityMembers ?? null
                    };

                    commonResponse.Data = result;
                }
                commonResponse.Status = 200;
            }
            catch (Exception ex)
            {
                string className = nameof(ActivityTaskService);
                string methodName = nameof(GetTask);
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
