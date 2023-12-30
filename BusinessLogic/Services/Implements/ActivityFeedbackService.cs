using DataAccess.Entities;
using DataAccess.EntityEnums;
using DataAccess.Models.Requests;
using DataAccess.Models.Requests.ModelBinders;
using DataAccess.Models.Responses;
using DataAccess.ModelsEnum;
using DataAccess.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Transactions;

namespace BusinessLogic.Services.Implements
{
    public class ActivityFeedbackService : IActivityFeedbackService
    {
        private readonly IActivityFeedbackRepository _activityFeedbackRepository;
        private readonly IConfiguration _config;
        private readonly ILogger<ActivityFeedbackService> _logger;
        private readonly IActivityMemberRepository _activityMemberRepository;
        private readonly IActivityRepository _activityRepository;
        private readonly IUserRepository _userRepository;

        public ActivityFeedbackService(
            IActivityFeedbackRepository activityFeedbackRepository,
            IConfiguration configuration,
            IUserRepository userRepository,
            ILogger<ActivityFeedbackService> logger,
            IActivityMemberRepository activityMemberRepository,
            IActivityRepository activityRepository
        )
        {
            _activityFeedbackRepository = activityFeedbackRepository;
            _config = configuration;
            _logger = logger;
            _activityMemberRepository = activityMemberRepository;
            _activityRepository = activityRepository;
            _userRepository = userRepository;
        }

        public async Task<CommonResponse> SendFeedback(
            Guid userId,
            ActivityFeedbackCreatingRequest request
        )
        {
            CommonResponse commonResponse = new CommonResponse();
            string internalServerErrorMsg = _config[
                "ResponseMessages:CommonMsg:InternalServerErrorMsg"
            ];

            try
            {
                ActivityFeedback? feedback =
                    await _activityFeedbackRepository.FindActivityFeedbackAsync(
                        userId,
                        request.ActivityId,
                        ActivityFeedbackStatus.NOT_FEEDBACK_PROVIDED
                    );
                if (feedback != null)
                {
                    feedback.ActivityId = request.ActivityId;
                    feedback.Content = request.Content;
                    feedback.CreatedDate = SettedUpDateTime.GetCurrentVietNamTime();
                    feedback.UserId = userId;
                    feedback.Status = ActivityFeedbackStatus.FEEDBACK_PROVIDED;
                    feedback.Rating = request.Rating;
                    int rs = await _activityFeedbackRepository.UpdateFeedbackAsync(feedback);
                    if (rs < 0)
                        throw new Exception();
                }
                else
                {
                    commonResponse.Status = 400;
                    commonResponse.Message = "Bạn không thể đưa ra đánh giá với hoạt động này.";
                    return commonResponse;
                }
                commonResponse.Status = 200;
                commonResponse.Message = "Đã cập nhật thành công.";
            }
            catch (Exception ex)
            {
                string className = nameof(ActivityFeedbackService);
                string methodName = nameof(SendFeedback);
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

        public async Task<CommonResponse> CreatFeedback(Guid activityId, Guid userId, string role)
        {
            CommonResponse commonResponse = new CommonResponse();
            string internalServerErrorMsg = _config[
                "ResponseMessages:CommonMsg:InternalServerErrorMsg"
            ];

            try
            {
                Activity? activity = await _activityRepository.FindActivityByIdAsync(activityId);
                if (activity == null)
                {
                    commonResponse.Status = 400;
                    commonResponse.Message = "Không tìm thấy hoạt động tương ứng.";
                    return commonResponse;
                }
                else
                {
                    if (role != RoleEnum.SYSTEM_ADMIN.ToString())
                    {
                        if (activity.CreatedBy != userId)
                        {
                            commonResponse.Status = 400;
                            commonResponse.Message = "Bạn không có quyền thực hiên hành động này.";
                            return commonResponse;
                        }
                    }
                    var check = await _activityFeedbackRepository.GetListActivityFeedbackAsync(
                        activityId,
                        null
                    );
                    if (check != null && check.Count > 0)
                    {
                        commonResponse.Status = 400;
                        commonResponse.Message = "Đã lấy feedback của hoạt đông này rồi.";
                        return commonResponse;
                    }
                    List<ActivityMember>? activityMembers =
                        await _activityMemberRepository.FindMemberByActivityIdAsync(
                            activityId,
                            ActivityMemberStatus.ACTIVE
                        );
                    int rs = 0;
                    if (activityMembers != null && activityMembers.Count > 0)
                    {
                        using (
                            var scope = new TransactionScope(
                                TransactionScopeAsyncFlowOption.Enabled
                            )
                        )
                        {
                            foreach (var m in activityMembers)
                            {
                                ActivityFeedback? feedback = new ActivityFeedback();

                                feedback.ActivityId = activityId;
                                feedback.CreatedDate = SettedUpDateTime.GetCurrentVietNamTime();
                                feedback.UserId = m.UserId;
                                feedback.Content = "";
                                feedback.Status = ActivityFeedbackStatus.NOT_FEEDBACK_PROVIDED;
                                rs += await _activityFeedbackRepository.CreateFeedbackAsync(
                                    feedback
                                );
                            }
                            if (rs != activityMembers.Count)
                                throw new Exception();
                            scope.Complete();
                        }
                    }
                }

                commonResponse.Status = 200;
                commonResponse.Message = "Đã cập nhật thành công.";
            }
            catch (Exception ex)
            {
                string className = nameof(ActivityFeedbackService);
                string methodName = nameof(SendFeedback);
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

        public async Task<CommonResponse> CheckUserIsFeedbacked(Guid userId, Guid activityId)
        {
            string internalServerErrorMsg = _config[
                "ResponseMessages:CommonMsg:InternalServerErrorMsg"
            ];
            CommonResponse commonResponse = new CommonResponse();
            try
            {
                ActivityFeedback? feedbacks =
                    await _activityFeedbackRepository.FindActivityFeedbackAsync(
                        userId,
                        activityId,
                        ActivityFeedbackStatus.NOT_FEEDBACK_PROVIDED
                    );
                if (feedbacks != null)
                {
                    var rs = new FeedbackResponse
                    {
                        Content = feedbacks.Content,
                        CreatedDate = feedbacks.CreatedDate,
                        Id = feedbacks.Id,
                        Rating = feedbacks.Rating,
                        Status = feedbacks.Status.ToString()
                    };
                    commonResponse.Data = rs;
                }
                commonResponse.Status = 200;
            }
            catch (Exception ex)
            {
                string className = nameof(ActivityFeedbackService);
                string methodName = nameof(SendFeedback);
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

        public async Task<CommonResponse> CheckActivityIsFeedbacked(Guid activityId)
        {
            string internalServerErrorMsg = _config[
                "ResponseMessages:CommonMsg:InternalServerErrorMsg"
            ];
            CommonResponse commonResponse = new CommonResponse();
            try
            {
                var feedbacks = await _activityFeedbackRepository.GetListActivityFeedbackAsync(
                    activityId,
                    null
                );
                if (feedbacks != null && feedbacks.Count > 0)
                {
                    commonResponse.Data = false;
                }
                commonResponse.Status = 200;
            }
            catch (Exception ex)
            {
                string className = nameof(ActivityFeedbackService);
                string methodName = nameof(SendFeedback);
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

        public async Task<CommonResponse> GetFeedback(
            int? page,
            int? pageSize,
            Guid activityId,
            ActivityFeedbackStatus? status,
            Guid userId,
            string role
        )
        {
            CommonResponse commonResponse = new CommonResponse();
            string internalServerErrorMsg = _config[
                "ResponseMessages:CommonMsg:InternalServerErrorMsg"
            ];

            try
            {
                Activity? activity = await _activityRepository.FindActivityByIdAsync(activityId);
                if (activity == null)
                {
                    commonResponse.Status = 400;
                    commonResponse.Message = "Không tìm thấy hoạt động tương ứng.";
                    return commonResponse;
                }
                else
                {
                    if (role != RoleEnum.SYSTEM_ADMIN.ToString())
                    {
                        if (activity.CreatedBy != userId)
                        {
                            commonResponse.Status = 400;
                            commonResponse.Message = "Bạn không có quyền thực hiên hành động này.";
                            return commonResponse;
                        }
                    }
                }
                List<ActivityFeedback>? feedbacks =
                    await _activityFeedbackRepository.GetListActivityFeedbackAsync(
                        activityId,
                        status
                    );
                if (feedbacks != null && feedbacks.Count > 0)
                {
                    double avarage = feedbacks.Sum(a => a.Rating) / feedbacks.Count();
                    Pagination pagination = new Pagination();
                    pagination.PageSize = pageSize == null ? 10 : pageSize.Value;
                    pagination.CurrentPage = page == null ? 1 : page.Value;
                    pagination.Total = feedbacks.Count;
                    feedbacks = feedbacks
                        .Skip((pagination.CurrentPage - 1) * pagination.PageSize)
                        .Take(pagination.PageSize)
                        .ToList();

                    ActivityFeedbackResponse response = new ActivityFeedbackResponse();
                    response.ActivityId = activityId;
                    response.AverageStar = avarage;
                    response.FeedbackResponses = feedbacks
                        .Select(
                            a =>
                                new FeedbackResponse
                                {
                                    Content = a.Content,
                                    CreatedDate = a.CreatedDate,
                                    Id = a.Id,
                                    Rating = a.Rating,
                                    Status = a.Status.ToString()
                                }
                        )
                        .ToList();
                    commonResponse.Data = response;
                    commonResponse.Pagination = pagination;
                }
                else
                {
                    commonResponse.Data = new ActivityFeedbackResponse();
                }
                commonResponse.Status = 200;
            }
            catch (Exception ex)
            {
                string className = nameof(ActivityFeedbackService);
                string methodName = nameof(SendFeedback);
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
