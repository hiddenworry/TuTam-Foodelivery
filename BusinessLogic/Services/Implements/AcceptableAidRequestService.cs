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
    public class AcceptableAidRequestService : IAcceptableAidRequestService
    {
        private readonly IAcceptableAidRequestRepository _acceptableAidRequestRepository;
        private readonly IConfiguration _config;
        private readonly IBranchRepository _branchRepository;
        private readonly IAidRequestRepository _aidRequestRepository;
        private readonly ILogger<AcceptableAidRequestService> _logger;
        private readonly IAidItemRepository _aidItemRepository;
        private readonly INotificationRepository _notificationRepository;
        private readonly IHubContext<NotificationSignalSender> _hubContext;

        public AcceptableAidRequestService(
            IAcceptableAidRequestRepository acceptableAidRequestRepository,
            IConfiguration config,
            IBranchRepository branchRepository,
            IAidRequestRepository aidRequestRepository,
            ILogger<AcceptableAidRequestService> logger,
            IAidItemRepository aidItemRepository,
            INotificationRepository notificationRepository,
            IHubContext<NotificationSignalSender> hubContext
        )
        {
            _acceptableAidRequestRepository = acceptableAidRequestRepository;
            _config = config;
            _branchRepository = branchRepository;
            _aidRequestRepository = aidRequestRepository;
            _logger = logger;
            _aidItemRepository = aidItemRepository;
            _notificationRepository = notificationRepository;
            _hubContext = hubContext;
        }

        public async Task<CommonResponse> ConfirmAidRequestAsync(
            AidRequestComfirmingRequest aidRequestComfirmingRequest,
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

                AidRequest? pendingAidRequest =
                    await _aidRequestRepository.FindPendingAidRequestByIdAsync(
                        aidRequestComfirmingRequest.Id
                    );

                if (pendingAidRequest == null)
                {
                    return new CommonResponse
                    {
                        Status = 400,
                        Message = _config[
                            "ResponseMessages:AidRequest:PendingAcceptableAidRequestNotFound"
                        ]
                    };
                }

                AcceptableAidRequest? pendingAcceptableAidRequest =
                    await _acceptableAidRequestRepository.FindPendingAcceptableAidRequestByAidRequestIdAndBranchIdAsync(
                        pendingAidRequest.Id,
                        branch.Id
                    );
                if (
                    pendingAcceptableAidRequest == null
                    || pendingAcceptableAidRequest.Status != AcceptableAidRequestStatus.PENDING
                    || pendingAidRequest.Status != AidRequestStatus.PENDING
                )
                {
                    return new CommonResponse
                    {
                        Status = 400,
                        Message = _config[
                            "ResponseMessages:AidRequest:PendingAcceptableAidRequestNotFound"
                        ]
                    };
                }

                if (
                    aidRequestComfirmingRequest.AidItemIds != null
                    && aidRequestComfirmingRequest.AidItemIds.Count > 0
                )
                {
                    if (
                        aidRequestComfirmingRequest.AidItemIds.All(
                            id => pendingAidRequest.AidItems.Select(ai => ai.Id).Contains(id)
                        )
                    )
                    {
                        if (
                            aidRequestComfirmingRequest.AidItemIds.Count
                                < pendingAidRequest.AidItems.Count
                            && aidRequestComfirmingRequest.RejectingReason == null
                        )
                        {
                            return new CommonResponse
                            {
                                Status = 400,
                                Message = _config[
                                    "ResponseMessages:AidRequest:AcceptAidRequestPartiallyMustHasRejectingReasonMsg"
                                ]
                            };
                        }
                        pendingAidRequest.ConfirmedDate = SettedUpDateTime.GetCurrentVietNamTime();
                        pendingAcceptableAidRequest.Status = AcceptableAidRequestStatus.ACCEPTED;
                        pendingAcceptableAidRequest.RejectingReason =
                            aidRequestComfirmingRequest.RejectingReason;
                        pendingAidRequest.Status = AidRequestStatus.ACCEPTED;
                    }
                    else
                    {
                        return new CommonResponse
                        {
                            Status = 400,
                            Message = _config[
                                "ResponseMessages:AidRequest:AcceptedAidItemNotFoundInListMsg"
                            ]
                        };
                    }
                }
                else
                {
                    pendingAcceptableAidRequest.Status = AcceptableAidRequestStatus.REJECTED;
                    pendingAcceptableAidRequest.RejectingReason =
                        aidRequestComfirmingRequest.RejectingReason;
                }
                pendingAcceptableAidRequest.ConfirmedDate =
                    SettedUpDateTime.GetCurrentVietNamTime();

                using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
                {
                    if (
                        await _acceptableAidRequestRepository.UpdateAcceptableAidRequestAsync(
                            pendingAcceptableAidRequest
                        ) != 1
                    )
                        return new CommonResponse
                        {
                            Status = 500,
                            Message = _config["ResponseMessages:CommonMsg:InternalServerErrorMsg"]
                        };

                    if (pendingAcceptableAidRequest.Status == AcceptableAidRequestStatus.ACCEPTED)
                    {
                        foreach (AidItem aidItem in pendingAidRequest.AidItems)
                        {
                            if (
                                aidRequestComfirmingRequest.AidItemIds != null
                                && aidRequestComfirmingRequest.AidItemIds.Contains(aidItem.Id)
                            )
                                aidItem.Status = AidItemStatus.ACCEPTED;
                            else
                                aidItem.Status = AidItemStatus.REJECTED;
                        }

                        if (
                            await _aidItemRepository.UpdateAidItemsAsync(pendingAidRequest.AidItems)
                            != pendingAidRequest.AidItems.Count
                        )
                            return new CommonResponse
                            {
                                Status = 500,
                                Message = _config[
                                    "ResponseMessages:CommonMsg:InternalServerErrorMsg"
                                ]
                            };

                        if (
                            await _aidRequestRepository.UpdateAidRequestAsync(pendingAidRequest)
                            != 1
                        )
                            return new CommonResponse
                            {
                                Status = 500,
                                Message = _config[
                                    "ResponseMessages:CommonMsg:InternalServerErrorMsg"
                                ]
                            };

                        Notification notification = new Notification
                        {
                            Name = "Yêu cầu hỗ trợ vật phẩm đã được chấp nhận.",
                            CreatedDate = SettedUpDateTime.GetCurrentVietNamTime(),
                            Image = _config["Notification:Image"],
                            Content =
                                $"Yêu cầu hỗ trợ vật phẩm của bạn đã được chấp nhận và sẽ được xử lý ở chi nhánh {branch.Name}.",
                            ReceiverId = pendingAidRequest.CharityUnit!.UserId.ToString(),
                            Status = NotificationStatus.NEW,
                            Type = NotificationType.NOTIFYING,
                            DataType = DataNotificationType.AID_REQUEST,
                            DataId = pendingAidRequest.Id
                        };
                        await _notificationRepository.CreateNotificationAsync(notification);
                        await _hubContext.Clients.All.SendAsync(
                            pendingAidRequest.CharityUnit!.UserId.ToString(),
                            notification
                        );

                        scope.Complete();
                        return new CommonResponse
                        {
                            Status = 200,
                            Data = pendingAidRequest.Id,
                            Message = _config[
                                "ResponseMessages:AcceptableAidRequestMsg:AcceptAidRequestSuccessMsg"
                            ]
                        };
                    }
                    else
                    {
                        List<AcceptableAidRequest> pendingAcceptableAidRequests =
                            await _acceptableAidRequestRepository.FindPendingAcceptableAidRequestsByAidRequestIdAsync(
                                pendingAidRequest.Id
                            );
                        if (pendingAcceptableAidRequests.Count == 0)
                        {
                            foreach (AidItem aidItem in pendingAidRequest.AidItems)
                            {
                                aidItem.Status = AidItemStatus.REJECTED;
                            }

                            if (
                                await _aidItemRepository.UpdateAidItemsAsync(
                                    pendingAidRequest.AidItems
                                ) != pendingAidRequest.AidItems.Count
                            )
                                return new CommonResponse
                                {
                                    Status = 500,
                                    Message = _config[
                                        "ResponseMessages:CommonMsg:InternalServerErrorMsg"
                                    ]
                                };

                            pendingAidRequest.Status = AidRequestStatus.REJECTED;
                            pendingAidRequest.ConfirmedDate =
                                SettedUpDateTime.GetCurrentVietNamTime();
                            if (
                                await _aidRequestRepository.UpdateAidRequestAsync(pendingAidRequest)
                                != 1
                            )
                                return new CommonResponse
                                {
                                    Status = 500,
                                    Message = _config[
                                        "ResponseMessages:CommonMsg:InternalServerErrorMsg"
                                    ]
                                };

                            Notification notification = new Notification
                            {
                                Name = "Yêu cầu hỗ trợ vật phẩm không được chấp nhận.",
                                CreatedDate = SettedUpDateTime.GetCurrentVietNamTime(),
                                Image = _config["Notification:Image"],
                                Content =
                                    "Yêu cầu hỗ trợ vật phẩm của bạn không được chấp nhận bới các chi nhánh gần đó.",
                                ReceiverId = pendingAidRequest.CharityUnit!.UserId.ToString(),
                                Status = NotificationStatus.NEW,
                                Type = NotificationType.NOTIFYING,
                                DataType = DataNotificationType.AID_REQUEST,
                                DataId = pendingAidRequest.Id
                            };
                            await _notificationRepository.CreateNotificationAsync(notification);
                            await _hubContext.Clients.All.SendAsync(
                                pendingAidRequest.CharityUnit!.UserId.ToString(),
                                notification
                            );
                        }

                        scope.Complete();
                        return new CommonResponse
                        {
                            Status = 200,
                            Data = pendingAidRequest.Id,
                            Message = _config[
                                "ResponseMessages:AcceptableAidRequestMsg:RejectAidRequestSuccessMsg"
                            ]
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "An exception occurred in service AcceptableAidRequestService, method ConfirmAidRequestAsync."
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
