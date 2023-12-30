using BusinessLogic.Utils.FirebaseService;
using BusinessLogic.Utils.Notification.Implements;
using DataAccess.Entities;
using DataAccess.EntityEnums;
using DataAccess.Models.Requests;
using DataAccess.Models.Requests.ModelBinders;
using DataAccess.Models.Responses;
using DataAccess.Repositories;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Transactions;

namespace BusinessLogic.Services.Implements
{
    public class AcceptableDonatedRequestService : IAcceptableDonatedRequestService
    {
        private readonly IAcceptableDonatedRequestRepository _acceptableDonatedRequestRepository;
        private readonly IConfiguration _config;
        private readonly IBranchRepository _branchRepository;
        private readonly IDonatedRequestRepository _donatedRequestRepository;
        private readonly ILogger<AcceptableDonatedRequestService> _logger;
        private readonly IDonatedItemRepository _donatedItemRepository;
        private readonly INotificationRepository _notificationRepository;
        private readonly IHubContext<NotificationSignalSender> _hubContext;
        private readonly IFirebaseNotificationService _firebaseNotificationService;

        public AcceptableDonatedRequestService(
            IAcceptableDonatedRequestRepository acceptableDonatedRequestRepository,
            IConfiguration config,
            IBranchRepository branchRepository,
            IDonatedRequestRepository donatedRequestRepository,
            ILogger<AcceptableDonatedRequestService> logger,
            IDonatedItemRepository donatedItemRepository,
            INotificationRepository notificationRepository,
            IHubContext<NotificationSignalSender> hubContext,
            IFirebaseNotificationService firebaseNotificationService
        )
        {
            _acceptableDonatedRequestRepository = acceptableDonatedRequestRepository;
            _config = config;
            _branchRepository = branchRepository;
            _donatedRequestRepository = donatedRequestRepository;
            _logger = logger;
            _donatedItemRepository = donatedItemRepository;
            _notificationRepository = notificationRepository;
            _hubContext = hubContext;
            _firebaseNotificationService = firebaseNotificationService;
        }

        public async Task<CommonResponse> ConfirmDonatedRequestAsync(
            DonatedRequestConfirmingRequest donatedRequestConfirmingRequest,
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

                DonatedRequest? pendingDonatedRequest =
                    await _donatedRequestRepository.FindPendingDonatedRequestByIdAsync(
                        donatedRequestConfirmingRequest.Id
                    );

                if (pendingDonatedRequest == null)
                {
                    return new CommonResponse
                    {
                        Status = 400,
                        Message = _config[
                            "ResponseMessages:DonatedRequestMsg:PendingAcceptableDonatedRequestNotFound"
                        ]
                    };
                }

                AcceptableDonatedRequest? pendingAcceptableDonatedRequest =
                    await _acceptableDonatedRequestRepository.FindPendingAcceptableDonatedRequestByDonatedRequestIdAndBranchIdAsync(
                        pendingDonatedRequest.Id,
                        branch.Id
                    );
                if (
                    pendingAcceptableDonatedRequest == null
                    || pendingAcceptableDonatedRequest.Status
                        != AcceptableDonatedRequestStatus.PENDING
                    || pendingDonatedRequest.Status != DonatedRequestStatus.PENDING
                )
                {
                    return new CommonResponse
                    {
                        Status = 400,
                        Message = _config[
                            "ResponseMessages:DonatedRequestMsg:PendingAcceptableDonatedRequestNotFound"
                        ]
                    };
                }

                if (
                    donatedRequestConfirmingRequest.DonatedItemIds != null
                    && donatedRequestConfirmingRequest.DonatedItemIds.Count > 0
                )
                {
                    if (
                        donatedRequestConfirmingRequest.DonatedItemIds.All(
                            id =>
                                pendingDonatedRequest.DonatedItems.Select(di => di.Id).Contains(id)
                        )
                    )
                    {
                        if (
                            donatedRequestConfirmingRequest.DonatedItemIds.Count
                                < pendingDonatedRequest.DonatedItems.Count
                            && donatedRequestConfirmingRequest.RejectingReason == null
                        )
                        {
                            return new CommonResponse
                            {
                                Status = 400,
                                Message = _config[
                                    "ResponseMessages:DonatedRequestMsg:AcceptDonatedRequestPartiallyMustHasRejectingReasonMsg"
                                ]
                            };
                        }
                        pendingDonatedRequest.ConfirmedDate =
                            SettedUpDateTime.GetCurrentVietNamTime();
                        pendingAcceptableDonatedRequest.Status =
                            AcceptableDonatedRequestStatus.ACCEPTED;
                        pendingAcceptableDonatedRequest.RejectingReason =
                            donatedRequestConfirmingRequest.RejectingReason;
                        pendingDonatedRequest.Status = DonatedRequestStatus.ACCEPTED;
                    }
                    else
                    {
                        return new CommonResponse
                        {
                            Status = 400,
                            Message = _config[
                                "ResponseMessages:DonatedRequestMsg:AcceptedDonatedItemNotFoundInListMsg"
                            ]
                        };
                    }
                }
                else
                {
                    pendingAcceptableDonatedRequest.Status =
                        AcceptableDonatedRequestStatus.REJECTED;
                    pendingAcceptableDonatedRequest.RejectingReason =
                        donatedRequestConfirmingRequest.RejectingReason;
                }
                pendingAcceptableDonatedRequest.ConfirmedDate =
                    SettedUpDateTime.GetCurrentVietNamTime();

                using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
                {
                    if (
                        await _acceptableDonatedRequestRepository.UpdateAcceptableDonatedRequestAsync(
                            pendingAcceptableDonatedRequest
                        ) != 1
                    )
                        return new CommonResponse
                        {
                            Status = 500,
                            Message = _config["ResponseMessages:CommonMsg:InternalServerErrorMsg"]
                        };

                    if (
                        pendingAcceptableDonatedRequest.Status
                        == AcceptableDonatedRequestStatus.ACCEPTED
                    )
                    {
                        foreach (DonatedItem donatedItem in pendingDonatedRequest.DonatedItems)
                        {
                            if (
                                donatedRequestConfirmingRequest.DonatedItemIds != null
                                && donatedRequestConfirmingRequest.DonatedItemIds.Contains(
                                    donatedItem.Id
                                )
                            )
                                donatedItem.Status = DonatedItemStatus.ACCEPTED;
                            else
                                donatedItem.Status = DonatedItemStatus.REJECTED;
                        }

                        if (
                            await _donatedItemRepository.UpdateDonatedItemsAsync(
                                pendingDonatedRequest.DonatedItems
                            ) != pendingDonatedRequest.DonatedItems.Count
                        )
                            return new CommonResponse
                            {
                                Status = 500,
                                Message = _config[
                                    "ResponseMessages:CommonMsg:InternalServerErrorMsg"
                                ]
                            };

                        if (
                            await _donatedRequestRepository.UpdateDonatedRequestAsync(
                                pendingDonatedRequest
                            ) != 1
                        )
                            return new CommonResponse
                            {
                                Status = 500,
                                Message = _config[
                                    "ResponseMessages:CommonMsg:InternalServerErrorMsg"
                                ]
                            };

                        string tmpNotification = _config[
                            "ResponseMessages:NotificationMsg:NotificationContentForAcceptedDonatedRequestMsg"
                        ];

                        Notification notification = new Notification
                        {
                            Name = _config[
                                "ResponseMessages:NotificationMsg:NotificationTitleForAcceptedDonatedRequestMsg"
                            ],
                            CreatedDate = SettedUpDateTime.GetCurrentVietNamTime(),
                            Image = _config["Notification:Image"],
                            Content = $"{tmpNotification} {branch.Name}.",
                            ReceiverId = pendingDonatedRequest.UserId.ToString(),
                            Status = NotificationStatus.NEW,
                            Type = NotificationType.NOTIFYING,
                            DataType = DataNotificationType.DONATED_REQUEST,
                            DataId = pendingDonatedRequest.Id
                        };
                        await _notificationRepository.CreateNotificationAsync(notification);
                        await _hubContext.Clients.All.SendAsync(
                            pendingDonatedRequest.UserId.ToString(),
                            notification
                        );
                        if (pendingDonatedRequest.User.DeviceToken != null)
                        {
                            PushNotificationRequest pushNotificationRequest =
                                new PushNotificationRequest
                                {
                                    DeviceToken = pendingDonatedRequest.User.DeviceToken,
                                    Message = $"{tmpNotification} {branch.Name}.",
                                    Title = _config[
                                        "ResponseMessages:NotificationMsg:NotificationTitleForAcceptedDonatedRequestMsg"
                                    ]
                                };
                            await _firebaseNotificationService.PushNotification(
                                pushNotificationRequest
                            );
                        }

                        scope.Complete();
                        return new CommonResponse
                        {
                            Status = 200,
                            Data = pendingDonatedRequest.Id,
                            Message = _config[
                                "ResponseMessages:AcceptableDonatedRequest:AcceptDonatedRequestSuccessMsg"
                            ]
                        };
                    }
                    else
                    {
                        List<AcceptableDonatedRequest> pendingAcceptableDonatedRequests =
                            await _acceptableDonatedRequestRepository.FindPendingAcceptableDonatedRequestsByDonatedRequestIdAsync(
                                pendingDonatedRequest.Id
                            );
                        if (pendingAcceptableDonatedRequests.Count == 0)
                        {
                            foreach (DonatedItem donatedItem in pendingDonatedRequest.DonatedItems)
                            {
                                donatedItem.Status = DonatedItemStatus.REJECTED;
                            }

                            if (
                                await _donatedItemRepository.UpdateDonatedItemsAsync(
                                    pendingDonatedRequest.DonatedItems
                                ) != pendingDonatedRequest.DonatedItems.Count
                            )
                                return new CommonResponse
                                {
                                    Status = 500,
                                    Message = _config[
                                        "ResponseMessages:CommonMsg:InternalServerErrorMsg"
                                    ]
                                };

                            pendingDonatedRequest.Status = DonatedRequestStatus.REJECTED;
                            pendingDonatedRequest.ConfirmedDate =
                                SettedUpDateTime.GetCurrentVietNamTime();
                            if (
                                await _donatedRequestRepository.UpdateDonatedRequestAsync(
                                    pendingDonatedRequest
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
                            Notification notification = new Notification
                            {
                                Name = _config[
                                    "ResponseMessages:NotificationMsg:NotificationTitleForRejectedDonatedRequestMsg"
                                ],
                                CreatedDate = SettedUpDateTime.GetCurrentVietNamTime(),
                                Image = _config["Notification:Image"],
                                Content = _config[
                                    "ResponseMessages:NotificationMsg:NotificationContentForRejectedDonatedRequestMsg"
                                ],
                                ReceiverId = pendingDonatedRequest.UserId.ToString(),
                                Status = NotificationStatus.NEW,
                                Type = NotificationType.NOTIFYING,
                                DataType = DataNotificationType.DONATED_REQUEST,
                                DataId = pendingDonatedRequest.Id
                            };
                            await _notificationRepository.CreateNotificationAsync(notification);
                            await _hubContext.Clients.All.SendAsync(
                                pendingDonatedRequest.UserId.ToString(),
                                notification
                            );
                            if (pendingDonatedRequest.User.DeviceToken != null)
                            {
                                PushNotificationRequest pushNotificationRequest =
                                    new PushNotificationRequest
                                    {
                                        DeviceToken = pendingDonatedRequest.User.DeviceToken,
                                        Message = _config[
                                            "ResponseMessages:NotificationMsg:NotificationContentForRejectedDonatedRequestMsg"
                                        ],
                                        Title = _config[
                                            "ResponseMessages:NotificationMsg:NotificationTitleForRejectedDonatedRequestMsg"
                                        ]
                                    };
                                await _firebaseNotificationService.PushNotification(
                                    pushNotificationRequest
                                );
                            }
                        }

                        scope.Complete();
                        return new CommonResponse
                        {
                            Status = 200,
                            Data = pendingDonatedRequest.Id,
                            Message = _config[
                                "ResponseMessages:AcceptableDonatedRequest:RejectDonatedRequestSuccessMsg"
                            ]
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "An exception occurred in service AcceptableDonatedRequestService, method ConfirmDonatedRequestAsync."
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
