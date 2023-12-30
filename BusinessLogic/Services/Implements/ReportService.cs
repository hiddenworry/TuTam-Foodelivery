using DataAccess.Entities;
using DataAccess.EntityEnums;
using DataAccess.Models.Responses;
using DataAccess.ModelsEnum;
using DataAccess.Repositories;
using Microsoft.Extensions.Configuration;
using System.ComponentModel.DataAnnotations;

namespace BusinessLogic.Services.Implements
{
    public class ReportService : IReportService
    {
        private readonly IReportRepository _reportRepository;
        private readonly IConfiguration _config;
        private readonly IUserRepository _userRepository;

        public ReportService(
            IReportRepository reportRepository,
            IConfiguration configuration,
            IUserRepository userRepository
        )
        {
            _reportRepository = reportRepository;
            _config = configuration;
            _userRepository = userRepository;
        }

        public async Task<CommonResponse> GetReportAsync(
            int? page,
            int? pageSize,
            Guid? userId,
            string? keyWord,
            ReportType? reportType
        )
        {
            string internalServerErrorMsg = _config[
                "ResponseMessages:AuthenticationMsg:InternalServerErrorMsg"
            ];
            CommonResponse commonResponse = new CommonResponse();
            try
            {
                User? user = null;
                List<Report>? reports = new List<Report>();
                if (userId != null)
                {
                    user = await _userRepository.FindUserByIdAsync(userId.Value);

                    if (user != null && user.Role.Name == RoleEnum.BRANCH_ADMIN.ToString())
                    {
                        reports = await _reportRepository.GetReportsByBranchAsync(
                            userId,
                            reportType
                        );
                    }
                    else
                    {
                        reports = await _reportRepository.GetReportsAsync(
                            userId,
                            keyWord,
                            reportType
                        );
                    }
                }
                else
                {
                    reports = await _reportRepository.GetReportsAsync(userId, keyWord, reportType);
                }

                if (reports != null && reports.Count > 0)
                {
                    Pagination pagination = new Pagination();
                    pagination.PageSize = pageSize == null ? 10 : pageSize.Value;
                    pagination.CurrentPage = page == null ? 1 : page.Value;
                    pagination.Total = reports.Count;
                    reports = reports
                        .Skip((pagination.CurrentPage - 1) * pagination.PageSize)
                        .Take(pagination.PageSize)
                        .ToList();

                    var rs = reports
                        .Select(
                            a =>
                                new
                                {
                                    a.Id,
                                    a.CreatedDate,
                                    a.Content,
                                    Type = a.Type.ToString(),
                                    a.UserId,
                                    UserName = a.User?.Name ?? "",
                                    Email = a.User?.Email ?? "",
                                    Phone = a.User?.Phone ?? "",
                                    Avatar = a.User?.Avatar ?? "",
                                    DeliveredUser = new
                                    {
                                        UserId = a.ScheduledRouteDeliveryRequest.ScheduledRoute.User
                                        != null
                                            ? (Guid?)
                                                a.ScheduledRouteDeliveryRequest
                                                    .ScheduledRoute
                                                    .User
                                                    .Id
                                            : null,
                                        UserName = a.ScheduledRouteDeliveryRequest
                                            .ScheduledRoute
                                            .User != null
                                            ? a.ScheduledRouteDeliveryRequest
                                                .ScheduledRoute
                                                .User
                                                .Name
                                            : "",
                                        Avatar = a.ScheduledRouteDeliveryRequest.ScheduledRoute.User
                                        != null
                                            ? a.ScheduledRouteDeliveryRequest
                                                .ScheduledRoute
                                                .User
                                                .Avatar
                                            : "",
                                        Phone = a.ScheduledRouteDeliveryRequest.ScheduledRoute.User
                                        != null
                                            ? a.ScheduledRouteDeliveryRequest
                                                .ScheduledRoute
                                                .User
                                                .Phone
                                            : "",
                                        Email = a.ScheduledRouteDeliveryRequest.ScheduledRoute.User
                                        != null
                                            ? a.ScheduledRouteDeliveryRequest
                                                .ScheduledRoute
                                                .User
                                                .Email
                                            : ""
                                    },
                                    a.Title,
                                    DeliveryRequestId = a.ScheduledRouteDeliveryRequest?.DeliveryRequestId
                                        ?? null
                                }
                        )
                        .ToList();

                    commonResponse.Data = rs;
                    commonResponse.Pagination = pagination;
                }
                else
                {
                    commonResponse.Data = new List<Report>();
                }
                commonResponse.Status = 200;
            }
            catch
            {
                commonResponse.Message = internalServerErrorMsg;
                commonResponse.Status = 500;
            }
            return commonResponse;
        }

        public async Task<CommonResponse> GetReportByDeliveryRequestIdAsync(
            int? page,
            int? pageSize,
            Guid? deliveryRequestId,
            ReportType? reportType
        )
        {
            string internalServerErrorMsg = _config[
                "ResponseMessages:AuthenticationMsg:InternalServerErrorMsg"
            ];
            CommonResponse commonResponse = new CommonResponse();
            try
            {
                List<Report>? reports = await _reportRepository.GetReportsByDeliveryRequestIdAsync(
                    deliveryRequestId,
                    reportType
                );

                if (reports != null && reports.Count > 0)
                {
                    Pagination pagination = new Pagination();
                    pagination.PageSize = pageSize == null ? 10 : pageSize.Value;
                    pagination.CurrentPage = page == null ? 1 : page.Value;
                    pagination.Total = reports.Count;
                    reports = reports
                        .Skip((pagination.CurrentPage - 1) * pagination.PageSize)
                        .Take(pagination.PageSize)
                        .ToList();
                    var rs = reports
                        .Select(
                            a =>
                                new
                                {
                                    a.Id,
                                    a.UserId,
                                    UserName = a.User?.Name ?? "",
                                    a.CreatedDate,
                                    a.Content,
                                    Type = a.Type.ToString(),
                                    Email = a.User?.Email ?? "",
                                    Phone = a.User?.Phone ?? "",
                                    Avatar = a.User?.Avatar ?? "",
                                    a.Title,
                                    DeliveryRequestId = a.ScheduledRouteDeliveryRequest?.DeliveryRequestId
                                        ?? null
                                }
                        )
                        .ToList();

                    commonResponse.Data = rs;
                    commonResponse.Pagination = pagination;
                }
                else
                {
                    commonResponse.Data = new List<Report>();
                }
                commonResponse.Status = 200;
            }
            catch
            {
                commonResponse.Message = internalServerErrorMsg;
                commonResponse.Status = 500;
            }
            return commonResponse;
        }
    }
}
