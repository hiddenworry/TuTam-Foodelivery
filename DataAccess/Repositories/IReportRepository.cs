using DataAccess.Entities;
using DataAccess.EntityEnums;

namespace DataAccess.Repositories
{
    public interface IReportRepository
    {
        Task<int> CreateReportAsync(Report report);
        Task<Report?> FindReportByUserIdAndDeliveryRequestIdAsync(
            Guid userId,
            Guid deliveryRequestId,
            ReportType? reportType
        );
        Task<List<Report>?> GetReportsAsync(Guid? userId, string? keyWord, ReportType? reportType);
        Task<List<Report>?> GetReportsByBranchAsync(Guid? branchAdminId, ReportType? reportType);
        Task<List<Report>?> GetReportsByDeliveryRequestIdAsync(
            Guid? deliveryRequestId,
            ReportType? reportType
        );
    }
}
