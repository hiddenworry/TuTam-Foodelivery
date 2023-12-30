using DataAccess.Entities;
using DataAccess.EntityEnums;

namespace DataAccess.Repositories
{
    public interface IStockUpdatedHistoryDetailRepository
    {
        Task<int> AddStockUpdatedHistoryDetailAsync(
            StockUpdatedHistoryDetail stockUpdatedHistoryDetail
        );
        Task<int> AddStockUpdatedHistoryDetailsAsync(
            List<StockUpdatedHistoryDetail> stockUpdatedHistoryDetails
        );
        Task<int> DeleteStockUpdatedHistoryDetailsAsync(
            List<StockUpdatedHistoryDetail> oldStockUpdatedHistoryDetails
        );
        Task<List<StockUpdatedHistoryDetail>> GetStockUpdatedHistoryDetailsByDeliveryItemIdAsync(
            Guid deliveryItemId
        );
        Task<List<StockUpdatedHistoryDetail>> GetStockUpdateHistoryDetailsByActivityId(
            Guid activityId,
            DateTime? startDate,
            DateTime? endDate
        );
        Task<List<StockUpdatedHistoryDetail>> GetStockUpdateHistoryDetailsByBranchId(
            Guid branchId,
            DateTime? startDate,
            DateTime? endDate
        );

        Task<List<StockUpdatedHistoryDetail>> GetStockUpdateHistoryByCharityUnitId(
            Guid charityUnitId,
            DateTime? startDate,
            DateTime? endDate
        );
        Task<List<StockUpdatedHistoryDetail>> GetStockUpdateHistoryDetailsForAdmin(
            Guid? charityUnitId,
            Guid? branchId,
            StockUpdatedHistoryType? type,
            DateTime? startDate,
            DateTime? endDate
        );
        Task<List<StockUpdatedHistoryDetail>> GetStockUpdateHistoryOfContributor(
            Guid userId,
            DateTime? startDate,
            DateTime? endDate
        );
    }
}
