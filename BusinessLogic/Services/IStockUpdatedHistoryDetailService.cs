using DataAccess.EntityEnums;
using DataAccess.Models.Responses;

namespace BusinessLogic.Services
{
    public interface IStockUpdatedHistoryDetailService
    {
        Task<CommonResponse> ExportStockUpdateHistoryDetailsForAdmin(
            Guid? branchId,
            Guid? branchAdminId,
            Guid? charityUnitId,
            StockUpdatedHistoryType? type,
            DateTime? startDate,
            DateTime? endDate
        );
        Task<CommonResponse> ExportStockUpdateHistoryDetailsOfActivity(
            Guid branchId,
            DateTime startDate,
            DateTime endDate
        );
        Task<CommonResponse> ExportStockUpdateHistoryDetailsOfBranch(
            Guid branchId,
            DateTime startDate,
            DateTime endDate
        );
        Task<CommonResponse> GetStockUpdateHistoryByCharityUnit(
            int? page,
            int? pageSize,
            Guid charityUnitId,
            DateTime? startDate,
            DateTime? endDate
        );
        Task<CommonResponse> GetStockUpdateHistoryDetailsForAdmin(
            int? page,
            int? pageSize,
            Guid? branchId,
            Guid? branchAdminId,
            Guid? charityUnitId,
            StockUpdatedHistoryType? type,
            DateTime? startDate,
            DateTime? endDate
        );
        Task<CommonResponse> GetStockUpdateHistoryDetailsOfActivity(
            int? page,
            int? pageSize,
            Guid activityId,
            DateTime? startDate,
            DateTime? endDate
        );
        Task<CommonResponse> GetStockUpdateHistoryDetailsOfBranch(
            int? page,
            int? pageSize,
            Guid branchId,
            DateTime? startDate,
            DateTime? endDate
        );
        Task<CommonResponse> GetStockUpdateHistoryOfContributor(
            int? page,
            int? pageSize,
            Guid userId,
            DateTime? startDate,
            DateTime? endDate
        );
    }
}
