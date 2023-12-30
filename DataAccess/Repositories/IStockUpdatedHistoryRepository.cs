using DataAccess.Entities;

namespace DataAccess.Repositories
{
    public interface IStockUpdatedHistoryRepository
    {
        Task<int> AddStockUpdatedHistoryAsync(StockUpdatedHistory stockUpdatedHistory);

        //int CountDirectDonationAsync(DateTime? startDate, DateTime? endDate);
        Task<List<StockUpdatedHistory>> FindStockUpdatedHistoriesByAidRequestIdAsync(
            Guid aidRequestId
        );
        Task<List<StockUpdatedHistory>> FindStockUpdatedHistoriesByDonatedRequestIdAsync(
            Guid donatedRequestId
        );
        Task<StockUpdatedHistory?> FindStockUpdatedHistoryByIdAsync(Guid stockUpdatedHistoryId);
        Task<List<StockUpdatedHistory>> FindStockUpdatedHistoriesBySelfShippingAidRequestIdAsync(
            Guid aidRequestId
        );
        Task<int> UpdateStockUpdatedHistoryAsync(StockUpdatedHistory stockUpdatedHistory);
        Task<StockUpdatedHistory?> FindStockUpdatedHistoryByIdAndCharityUnitUserIdAsync(
            Guid stockUpdatedHistoryId,
            Guid? userId
        );
    }
}
