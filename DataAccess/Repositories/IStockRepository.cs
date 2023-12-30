using DataAccess.Entities;

namespace DataAccess.Repositories
{
    public interface IStockRepository
    {
        Task<int> AddStockAsync(Stock stock);
        Task<Stock?> FindStockByItemIdAndExpirationDateAndBranchIdAndUserIdAndActivityId(
            Guid itemId,
            DateTime expirationDate,
            Guid branchId,
            Guid? userId,
            Guid? activityId
        );

        //Task<Stock?> GetCurrentValidStocksById(Guid Id);
        Task<Stock?> GetExpiredStocksByIdAndBranchId(Guid stockId, Guid id);
        Task<List<Stock>> GetCurrentValidStocksByItemIdAndBranchId(Guid itemId, Guid branchId);
        Task<List<Stock>?> GetStocksAsync(Guid? itemId, Guid? branchId, DateTime? expirationDate);
        Task<List<Stock>> GetStocksByItemIdAndBranchId(Guid itemId, Guid branchId);
        Task<Stock?> GetStocksByItemIdAndBranchIdAndExpirationDate(
            Guid itemId,
            Guid branchId,
            DateTime expirationDate
        );
        Task<int> UpdateStockAsync(Stock stock);
        Task<int> UpdateStocksAsync(List<Stock> newStocks);
    }
}
