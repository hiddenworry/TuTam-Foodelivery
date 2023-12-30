using BusinessLogic.Utils.FirebaseService;
using BusinessLogic.Utils.Notification.Implements;
using DataAccess.Entities;
using DataAccess.EntityEnums;
using DataAccess.Models.Requests;
using DataAccess.Models.Requests.ModelBinders;
using DataAccess.Models.Responses;
using DataAccess.ModelsEnum;
using DataAccess.Repositories;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Transactions;
using Notification = DataAccess.Entities.Notification;

namespace BusinessLogic.Services.Implements
{
    public class CollaboratorService : ICollaboratorService
    {
        private readonly ICollaboratorRepository _collaboratorRepository;
        private readonly IUserRepository _userRepository;
        private readonly IConfiguration _config;
        private readonly IFirebaseStorageService _firebaseStorageService;
        private readonly ILogger<CollaboratorService> _logger;
        private readonly IHubContext<NotificationSignalSender> _hubContext;
        private readonly INotificationRepository _notificationRepository;
        private readonly IDeliveryRequestRepository _deliveryRequestRepository;
        private readonly IRoleRepository _roleRepository;
        private readonly IUserPermissionRepository _userPermissionRepository;
        private readonly IPermissionRepository _permissionRepository;
        private readonly IRolePermissionRepository _rolePermissionRepository;
        private readonly IFirebaseNotificationService _firebaseNotificationService;

        public CollaboratorService(
            ICollaboratorRepository collaboratorRepository,
            IConfiguration configuration,
            IUserRepository userRepository,
            IFirebaseStorageService firebaseStorageService,
            ILogger<CollaboratorService> logger,
            IHubContext<NotificationSignalSender> hubContext,
            INotificationRepository notificationRepository,
            IDeliveryRequestRepository deliveryRequestRepository,
            IRoleRepository roleRepository,
            IUserPermissionRepository userPermissionRepository,
            IPermissionRepository permissionRepository,
            IRolePermissionRepository rolePermissionRepository,
            IFirebaseNotificationService firebaseNotificationService
        )
        {
            _collaboratorRepository = collaboratorRepository;
            _config = configuration;
            _userRepository = userRepository;
            _firebaseStorageService = firebaseStorageService;
            _logger = logger;
            _hubContext = hubContext;
            _notificationRepository = notificationRepository;
            _deliveryRequestRepository = deliveryRequestRepository;
            _roleRepository = roleRepository;
            _permissionRepository = permissionRepository;
            _userPermissionRepository = userPermissionRepository;
            _rolePermissionRepository = rolePermissionRepository;
            _firebaseNotificationService = firebaseNotificationService;
        }

        public async Task<CommonResponse> RegisterToBecomeCollaborator(
            Guid userId,
            CollaboratorCreatingRequest request
        )
        {
            CommonResponse commonResponse = new();
            string internalServerErrorMsg = _config[
                "ResponseMessages:CommonMsg:InternalServerErrorMsg"
            ];
            string createSuccessMsg = _config["ResponseMessages:CollaboratorMsg:CreateSuccessMsg"];
            string notAllowToSpamMsg = _config[
                "ResponseMessages:CollaboratorMsg:NotAllowToSpamMsg"
            ];
            try
            {
                User? user = await _userRepository.FindUserByIdAsync(userId);

                if (user != null)
                {
                    if (user.IsCollaborator == true)
                    {
                        commonResponse.Status = 400;
                        commonResponse.Message = "Bạn đã có thể thực hiện yêu cầu vận chuyển rồi.";
                        return commonResponse;
                    }

                    CollaboratorApplication? check =
                        await _collaboratorRepository.FindCollaboratorByUserIdAsync(userId);
                    if (
                        check != null
                        && check.CreatedDate.AddHours(24) > SettedUpDateTime.GetCurrentVietNamTime()
                        && check.Status == CollaboratorStatus.UNVERIFIED
                    )
                    {
                        commonResponse.Status = 400;
                        commonResponse.Message = notAllowToSpamMsg;
                        return commonResponse;
                    }

                    if (check != null && check.Status == CollaboratorStatus.ACTIVE)
                    {
                        commonResponse.Status = 400;
                        commonResponse.Message = "Bạn đã có thể thực hiện yêu cầu vận chuyển rồi.";
                        return commonResponse;
                    }
                    using (
                        var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled)
                    )
                    {
                        CollaboratorApplication collaborator = new();
                        collaborator.Note = request.Note;
                        using (var stream = request.FrontOfIdCard.OpenReadStream())
                        {
                            string imageName =
                                Guid.NewGuid().ToString()
                                + Path.GetExtension(request.FrontOfIdCard.FileName);
                            string imageUrl = await _firebaseStorageService.UploadImageToFirebase(
                                stream,
                                imageName
                            );
                            collaborator.FrontOfIdCard = imageUrl;
                        }
                        using (var stream = request.BackOfIdCard.OpenReadStream())
                        {
                            string imageName =
                                Guid.NewGuid().ToString()
                                + Path.GetExtension(request.BackOfIdCard.FileName);
                            string imageUrl = await _firebaseStorageService.UploadImageToFirebase(
                                stream,
                                imageName
                            );
                            collaborator.BackOfIdCard = imageUrl;
                        }

                        using (var stream = request.Avatar.OpenReadStream())
                        {
                            string imageName =
                                Guid.NewGuid().ToString()
                                + Path.GetExtension(request.Avatar.FileName);
                            string imageUrl = await _firebaseStorageService.UploadImageToFirebase(
                                stream,
                                imageName
                            );
                            collaborator.Avatar = imageUrl;
                        }
                        collaborator.Note = request.Note;
                        collaborator.CreatedDate = SettedUpDateTime.GetCurrentVietNamTime();
                        collaborator.FullName = request.FullName;
                        collaborator.Gender = request.Gender;
                        collaborator.DateOfBirth = request.DateOfBirth;
                        collaborator.Status = CollaboratorStatus.UNVERIFIED;
                        collaborator.UserId = userId;

                        int rs = await _collaboratorRepository.CreateCollaboratorAsync(
                            collaborator
                        )!;
                        if (rs > 0)
                        {
                            commonResponse.Status = 200;
                            commonResponse.Message = createSuccessMsg;
                            scope.Complete();
                        }
                        else
                        {
                            _firebaseStorageService.DeleteImage(collaborator.FrontOfIdCard);
                            _firebaseStorageService.DeleteImage(collaborator.BackOfIdCard);
                            throw new Exception();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                string className = nameof(CollaboratorService);
                string methodName = nameof(RegisterToBecomeCollaborator);
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

        public async Task<CommonResponse> ConfirmCollaborator(
            Guid collaboratorId,
            ConfirmCollaboratorRequest request
        )
        {
            CommonResponse commonResponse = new CommonResponse();
            string internalServerErrorMsg = _config[
                "ResponseMessages:CommonMsg:InternalServerErrorMsg"
            ];
            string notificationImage = _config["Notification:Image"];
            string updateSuccessMsg = _config["ResponseMessages:CollaboratorMsg:UpdateSuccessMsg"];
            string confirmUserToBecomeCollaborator = _config[
                "Notification:ConfirmUserToBecomeCollaborator"
            ];
            string denyUserToBecomeCollaborator = _config[
                "Notification:DenyUserToBecomeCollaborator"
            ];
            try
            {
                CollaboratorApplication? collaborator =
                    await _collaboratorRepository.FindCollaboratorByIdAsync(collaboratorId);
                if (collaborator != null)
                {
                    using (
                        var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled)
                    )
                    {
                        if (
                            !request.isAccept
                            && request.reason != null
                            && collaborator.Status == CollaboratorStatus.UNVERIFIED
                        )
                        {
                            _firebaseStorageService.DeleteImage(collaborator.FrontOfIdCard);
                            _firebaseStorageService.DeleteImage(collaborator.BackOfIdCard);
                            int rs = await _collaboratorRepository.DeleteCollaboratorAsync(
                                collaborator
                            );
                            if (rs > 0)
                            {
                                Notification notification = new Notification
                                {
                                    Name =
                                        "Thông báo về tình trạng đơn xin thực hiện yêu cầu vận chuyển.",
                                    CreatedDate = SettedUpDateTime.GetCurrentVietNamTime(),
                                    Image = notificationImage,
                                    Content = denyUserToBecomeCollaborator + request.reason,
                                    ReceiverId = collaborator.UserId.ToString(),
                                    Status = NotificationStatus.NEW,
                                    Type = NotificationType.NOTIFYING,
                                    DataType = DataNotificationType.CONTRIBUTOR,
                                    DataId = collaborator.UserId
                                };
                                await _notificationRepository.CreateNotificationAsync(notification);
                                await _hubContext.Clients.All.SendAsync(
                                    collaborator.UserId.ToString(),
                                    notification
                                );
                                if (collaborator.User.DeviceToken != null)
                                {
                                    PushNotificationRequest pushNotificationRequest =
                                        new PushNotificationRequest
                                        {
                                            DeviceToken = collaborator.User.DeviceToken,
                                            Message = denyUserToBecomeCollaborator + request.reason,
                                            Title =
                                                "Thông báo về tình trạng đơn xin thực hiện yêu cầu vận chuyển."
                                        };
                                    await _firebaseNotificationService.PushNotification(
                                        pushNotificationRequest
                                    );
                                }
                                scope.Complete();
                                commonResponse.Message = updateSuccessMsg;
                                commonResponse.Status = 200;
                            }
                            else
                                throw new Exception();
                        }
                        else if (
                            request.isAccept && collaborator.Status == CollaboratorStatus.UNVERIFIED
                        )
                        {
                            User? user = await _userRepository.FindUserByIdAsync(
                                collaborator.UserId
                            );

                            if (user != null)
                            {
                                user.IsCollaborator = true;
                                user = await _userRepository.UpdateUserAsync(user);
                            }
                            else
                            {
                                throw new Exception("User not found.");
                            }
                            collaborator.Status = CollaboratorStatus.ACTIVE;
                            int rs = await _collaboratorRepository.UpdateCollaboratorAsync(
                                collaborator
                            );

                            if (rs > 0)
                            {
                                Notification notification = new Notification
                                {
                                    Name =
                                        "Thông báo về tình trạng đơn xin thực hiện yêu cầu vận chuyển.",
                                    CreatedDate = SettedUpDateTime.GetCurrentVietNamTime(),
                                    Image = notificationImage,
                                    Content = confirmUserToBecomeCollaborator,
                                    ReceiverId = collaborator.UserId.ToString(),
                                    Status = NotificationStatus.NEW,
                                    Type = NotificationType.NOTIFYING,
                                    DataType = DataNotificationType.CONTRIBUTOR,
                                    DataId = collaborator.UserId
                                };
                                await _notificationRepository.CreateNotificationAsync(notification);
                                await _hubContext.Clients.All.SendAsync(
                                    collaborator.UserId.ToString(),
                                    notification
                                );
                                if (collaborator.User.DeviceToken != null)
                                {
                                    PushNotificationRequest pushNotificationRequest =
                                        new PushNotificationRequest
                                        {
                                            DeviceToken = collaborator.User.DeviceToken,
                                            Message = confirmUserToBecomeCollaborator,
                                            Title =
                                                "Thông báo về tình trạng đơn xin thực hiện yêu cầu vận chuyển."
                                        };
                                    await _firebaseNotificationService.PushNotification(
                                        pushNotificationRequest
                                    );
                                }
                                scope.Complete();
                                commonResponse.Message = updateSuccessMsg;
                                commonResponse.Status = 200;
                            }
                            else
                                throw new Exception("Excution sql failed");
                        }
                        else
                        {
                            commonResponse.Message = "Bạn không được phép thực hiện hành động này";
                            commonResponse.Status = 400;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                string className = nameof(CollaboratorService);
                string methodName = nameof(ConfirmCollaborator);
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

        //public async Task<CommonResponse> UpdateCollaboratorForUserAsync(
        //    Guid userId,
        //    CollaboratorUpdatingRequest request
        //)
        //{
        //    CommonResponse commonResponse = new CommonResponse();
        //    string internalServerErrorMsg = _config[
        //        "ResponseMessages:CommonMsg:InternalServerErrorMsg"
        //    ];
        //    string updateSuccessMsg = _config["ResponseMessages:CollaboratorMsg:UpdateSuccessMsg"];

        //    string notAllowToCancelCollaboratorMsg = _config[
        //        "CollaboratorMsg:NotAllowToCancelCollaboratorMsg"
        //    ];
        //    string collaboratorNotFoundMsg = _config[
        //        "ResponseMessages:CollaboratorMsg:CollaboratorNotFoundMsg"
        //    ];
        //    try
        //    {
        //        User? collaborator = await _userRepository.FindUserByIdAsync(userId);
        //        if (
        //            collaborator != null
        //            && collaborator.Status != UserStatus.INACTIVE
        //            && collaborator.Status != UserStatus.UNVERIFIED
        //        )
        //        {
        //            using (
        //                var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled)
        //            )
        //            {
        //                if (request.IsActive)
        //                {
        //                    collaborator.IsCollaborator = true;
        //                }
        //                else
        //                {
        //                    collaborator.IsCollaborator = false;
        //                    bool checkDeliveryRequest =
        //                        await _deliveryRequestRepository.CheckCollaboratorAvailabileToCancelCollaborator(
        //                            collaborator.Id
        //                        );
        //                    if (!checkDeliveryRequest)
        //                    {
        //                        commonResponse.Message = notAllowToCancelCollaboratorMsg;
        //                        commonResponse.Status = 400;
        //                        return commonResponse;
        //                    }
        //                }
        //                User? rs = await _userRepository.UpdateUserAsync(collaborator);
        //                if (rs != null)
        //                {
        //                    scope.Complete();
        //                }
        //                else
        //                {
        //                    throw new Exception("Excution sql falied");
        //                }

        //                commonResponse.Message = updateSuccessMsg;
        //                commonResponse.Status = 200;
        //            }
        //        }
        //        else
        //        {
        //            commonResponse.Message = collaboratorNotFoundMsg;
        //            commonResponse.Status = 400;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        string className = nameof(CollaboratorService);
        //        string methodName = nameof(UpdateCollaboratorForUserAsync);
        //        _logger.LogError(
        //            ex,
        //            "An error occurred in {ClassName}.{MethodName}: {ErrorMessage}",
        //            className,
        //            methodName,
        //            ex.Message
        //        );
        //        commonResponse.Message = internalServerErrorMsg;
        //        commonResponse.Status = 500;
        //    }
        //    return commonResponse;
        //}

        private async Task UpdatePermission(Guid userId, bool isActive)
        {
            //UserPermissionStatus status = UserPermissionStatus.PERMITTED;
            //if (!isActive)
            //{
            //    status = UserPermissionStatus.BANNED;
            //}
            User? user = await _userRepository.FindUserByIdAsync(userId);

            if (!user!.IsCollaborator)
                throw new Exception("User or permission not found.");
        }

        public async Task<CommonResponse> DeleteCollaborator(Guid collaboratorId)
        {
            CommonResponse commonResponse = new CommonResponse();
            string internalServerErrorMsg = _config[
                "ResponseMessages:CommonMsg:InternalServerErrorMsg"
            ];
            string updateSuccessMsg = _config["ResponseMessages:CollaboratorMsg:UpdateSuccessMsg"];
            string collaboratorNotFoundMsg = _config[
                "ResponseMessages:CollaboratorMsg:CollaboratorNotFoundMsg"
            ];

            try
            {
                CollaboratorApplication? collaborator =
                    await _collaboratorRepository.FindCollaboratorByIdAsync(collaboratorId);
                if (collaborator != null)
                {
                    using (
                        var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled)
                    )
                    {
                        User? user = await _userRepository.FindUserByIdAsync(collaborator.UserId);

                        if (user != null)
                        {
                            user.IsCollaborator = false;
                            await _userRepository.UpdateUserAsync(user);
                            collaborator.Status = CollaboratorStatus.DELETED;
                            int rs = await _collaboratorRepository.UpdateCollaboratorAsync(
                                collaborator
                            );
                            if (rs > 0)
                            {
                                scope.Complete();
                            }
                            else
                            {
                                throw new Exception();
                            }
                        }
                        else
                        {
                            throw new Exception("User not found.");
                        }

                        commonResponse.Message = updateSuccessMsg;
                        commonResponse.Status = 200;
                    }
                }
                else
                {
                    commonResponse.Message = collaboratorNotFoundMsg;
                    commonResponse.Status = 400;
                }
            }
            catch (Exception ex)
            {
                string className = nameof(CollaboratorService);
                string methodName = nameof(DeleteCollaborator);
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

        public async Task<CommonResponse> GetListUnVerifyCollaborator(
            CollaboratorStatus? status,
            int? page,
            int? pageSize,
            SortType? sortType = SortType.ASC
        )
        {
            CommonResponse commonResponse = new CommonResponse();
            string internalServerErrorMsg = _config[
                "ResponseMessages:CommonMsg:InternalServerErrorMsg"
            ];
            string updateSuccessMsg = _config["ResponseMessages:CollaboratorMsg:UpdateSuccessMsg"];
            string collaboratorNotFoundMsg = _config[
                "ResponseMessages:CollaboratorMsg:CollaboratorNotFoundMsg"
            ];

            try
            {
                List<CollaboratorApplication>? collaborators =
                    await _collaboratorRepository.GetCollaboratorByStatusAsync(status);
                if (collaborators != null && collaborators.Count > 0)
                {
                    if (sortType == SortType.ASC)
                    {
                        collaborators = collaborators.OrderBy(u => u.CreatedDate).ToList();
                    }
                    else
                    {
                        collaborators = collaborators
                            .OrderByDescending(u => u.CreatedDate)
                            .ToList();
                    }
                    Pagination pagination = new Pagination();
                    pagination.PageSize = pageSize == null ? 10 : pageSize.Value;
                    pagination.CurrentPage = page == null ? 1 : page.Value;
                    pagination.Total = collaborators.Count;
                    collaborators = collaborators
                        .Skip((pagination.CurrentPage - 1) * pagination.PageSize)
                        .Take(pagination.PageSize)
                        .ToList();
                    var responseData = collaborators
                        .Select(
                            c =>
                                new
                                {
                                    c.Id,
                                    c.FullName,
                                    CreateDate = c.CreatedDate,
                                    Status = c.Status.ToString(),
                                    c.User.Email,
                                    c.User.Phone,
                                    c.Avatar
                                }
                        )
                        .ToList();
                    commonResponse.Data = responseData;
                    commonResponse.Pagination = pagination;
                }
                commonResponse.Status = 200;
            }
            catch (Exception ex)
            {
                string className = nameof(CollaboratorService);
                string methodName = nameof(GetListUnVerifyCollaborator);
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

        public async Task<CommonResponse> GetDetailsCollaborator(Guid collaboratorId)
        {
            CommonResponse commonResponse = new CommonResponse();
            string internalServerErrorMsg = _config[
                "ResponseMessages:CommonMsg:InternalServerErrorMsg"
            ];
            string updateSuccessMsg = _config["ResponseMessages:CollaboratorMsg:UpdateSuccessMsg"];
            string collaboratorNotFoundMsg = _config[
                "ResponseMessages:CollaboratorMsg:CollaboratorNotFoundMsg"
            ];

            try
            {
                CollaboratorApplication? collaborator =
                    await _collaboratorRepository.FindCollaboratorByIdAsync(collaboratorId);
                if (collaborator != null)
                {
                    CollaboratorDetailsResponse collaboratorDetailsResponse =
                        new CollaboratorDetailsResponse
                        {
                            FullName = collaborator.FullName,
                            Email = collaborator.User.Email,
                            Avatar = collaborator.Avatar,
                            BackOfIdCard = collaborator.BackOfIdCard,
                            FrontOfIdCard = collaborator.FrontOfIdCard,
                            DateOfBirth = collaborator.DateOfBirth,
                            Gender = collaborator.Gender.ToString(),
                            Note = collaborator.Note,
                            Phone = collaborator.User.Phone,
                            status = collaborator.Status.ToString()
                        };
                    commonResponse.Data = collaboratorDetailsResponse;
                }
                commonResponse.Status = 200;
            }
            catch (Exception ex)
            {
                string className = nameof(CollaboratorService);
                string methodName = nameof(GetListUnVerifyCollaborator);
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

        public async Task<CommonResponse> checkCollaborator(Guid userId)
        {
            CommonResponse commonResponse = new CommonResponse();
            string internalServerErrorMsg = _config[
                "ResponseMessages:CommonMsg:InternalServerErrorMsg"
            ];

            try
            {
                CollaboratorApplication? collaborator =
                    await _collaboratorRepository.FindCollaboratorByUserIdAsync(userId);
                if (collaborator != null && collaborator.Status != CollaboratorStatus.DELETED)
                {
                    commonResponse.Data = true;
                }
                else
                {
                    commonResponse.Data = false;
                }
                commonResponse.Status = 200;
            }
            catch (Exception ex)
            {
                string className = nameof(CollaboratorService);
                string methodName = nameof(GetListUnVerifyCollaborator);
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
