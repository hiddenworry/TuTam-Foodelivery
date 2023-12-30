using DataAccess.EntityEnums;
using DataAccess.Models.Responses;

namespace BusinessLogic.Services
{
    public interface IReportService
    {
        Task<CommonResponse> GetReportAsync(
            int? page,
            int? pageSize,
            Guid? userId,
            string? keyWord,
            ReportType? reportType
        );
        Task<CommonResponse> GetReportByDeliveryRequestIdAsync(
            int? page,
            int? pageSize,
            Guid? deliveryRequestId,
            ReportType? reportType
        );
    }
}
