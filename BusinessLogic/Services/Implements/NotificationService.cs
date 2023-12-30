using DataAccess.Entities;
using DataAccess.EntityEnums;
using DataAccess.Models.Requests;
using DataAccess.Models.Responses;
using DataAccess.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BusinessLogic.Services.Implements
{
    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository _notificationRepository;
        private readonly ILogger<NotificationService> _logger;
        private readonly IConfiguration _config;

        public NotificationService(
            INotificationRepository notificationRepository,
            ILogger<NotificationService> logger,
            IConfiguration configuration
        )
        {
            _notificationRepository = notificationRepository;
            _logger = logger;
            _config = configuration;
        }

        public async Task<CommonResponse> UpdateNotification(
            NotificationUpdatingRequest request,
            Guid userId
        )
        {
            CommonResponse commonResponse = new CommonResponse();
            string internalServerErrorMsg = _config[
                "ResponseMessages:CommonMsg:InternalServerErrorMsg"
            ];
            string updateSuccessMsg = _config["ResponseMessages:NotificationMsg:UpdateSuccessMsg"];
            try
            {
                foreach (var notificationId in request.NotificationIds)
                {
                    Notification? rs =
                        await _notificationRepository.FindNotificationByIdAndUserIdAsync(
                            notificationId,
                            userId
                        );
                    if (rs != null)
                    {
                        rs.Status = request.Status;
                        await _notificationRepository.UpdateNotificationAsync(rs);
                    }
                }
                commonResponse.Status = 200;
                commonResponse.Message = updateSuccessMsg;
            }
            catch (Exception ex)
            {
                string className = nameof(NotificationService);
                string methodName = nameof(UpdateNotification);
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

        public async Task<CommonResponse> GetNotification(
            Guid userId,
            NotificationStatus? status,
            int? page,
            int? pageSize
        )
        {
            CommonResponse commonResponse = new CommonResponse();
            string internalServerErrorMsg = _config[
                "ResponseMessages:CommonMsg:InternalServerErrorMsg"
            ];
            string updateSuccessMsg = _config["ResponseMessages:NotificationMsg:UpdateSuccessMsg"];
            try
            {
                List<Notification>? rs =
                    await _notificationRepository.FindNotificationByUserIdAsync(userId);
                if (rs != null && rs.Count > 0)
                {
                    int notSeen = rs.Count(n => n.Status == NotificationStatus.NEW);

                    rs = rs.Where(n => status == null ? true : n.Status == status)
                        .OrderByDescending(n => n.CreatedDate)
                        .ToList();

                    Pagination pagination = new Pagination();
                    pagination.PageSize = pageSize == null ? 10 : pageSize.Value;
                    pagination.CurrentPage = page == null ? 1 : page.Value;
                    pagination.Total = rs.Count;
                    rs = rs.Skip((pagination.CurrentPage - 1) * pagination.PageSize)
                        .Take(pagination.PageSize)
                        .ToList();
                    List<NotificationResponse> collectedData = rs.Select(
                            n =>
                                new NotificationResponse
                                {
                                    Id = n.Id,
                                    Name = n.Name,
                                    Image = n.Image,
                                    Content = n.Content,
                                    CreatedDate = n.CreatedDate,
                                    ReceiverId = n.ReceiverId,
                                    Status = n.Status.ToString(),
                                    Type = n.Type.ToString(),
                                    DataType = n.DataType.ToString(),
                                    DataId = n.DataId
                                }
                        )
                        .ToList();
                    NotificationListResponse notificationListResponse =
                        new NotificationListResponse();
                    notificationListResponse.NotSeen = notSeen;
                    notificationListResponse.NotificationResponses = collectedData;
                    commonResponse.Data = notificationListResponse;
                    commonResponse.Pagination = pagination;
                }
                else
                {
                    NotificationListResponse notificationListResponse =
                        new NotificationListResponse();
                    notificationListResponse.NotificationResponses =
                        new List<NotificationResponse>();
                    commonResponse.Data = notificationListResponse;
                }

                commonResponse.Status = 200;
            }
            catch (Exception ex)
            {
                string className = nameof(NotificationService);
                string methodName = nameof(UpdateNotification);
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
