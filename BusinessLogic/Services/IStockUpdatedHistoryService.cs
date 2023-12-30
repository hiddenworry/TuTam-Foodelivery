using DataAccess.Models.Requests;
using DataAccess.Models.Responses;

namespace BusinessLogic.Services
{
    public interface IStockUpdatedHistoryService
    {
        Task<CommonResponse> CreateStockUpdatedHistoryTypeExportByItemsAsync(
            Guid userId,
            StockUpdatedHistoryTypeExportByItemsCreatingRequest updatedHistoryTypeExportByItemsCreatingRequest
        );

        Task<CommonResponse> CreateStockUpdatedHistoryTypeExportByStocksAsync(
            Guid userId,
            StockUpdatedHistoryTypeExportByStocksCreatingRequest updatedHistoryTypeExportByStocksCreatingRequest
        );
        Task<CommonResponse> CreateStockUpdateHistoryWhenBranchAdminImport(
            StockUpdateForImportingByBranchAdmin request,
            Guid branchAdminId
        );
        Task<CommonResponse> CreateStockUpdateHistoryWhenUserDirectlyDonate(
            StockUpdateForUserDirectDonateRequest request,
            Guid branchAdminId
        );
        Task<CommonResponse> GetStockUpdatedHistoryOfSelfShippingAidRequestForCharityUnit(
            Guid stockUpdatedHistoryId,
            Guid userId,
            string userRoleName
        );
    }
}
