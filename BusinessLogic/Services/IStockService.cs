using DataAccess.Models.Requests;
using DataAccess.Models.Responses;

namespace BusinessLogic.Services
{
    public interface IStockService
    {
        Task<CommonResponse> GetListAvalableByItemsId(
            NumberOfAvalibleItemRequest request,
            Guid? branchAdminId
        );

        Task<CommonResponse> GetStockByItemIdAndBranchId(
            Guid itemId,
            Guid branchId,
            int? page,
            int? pageSize,
            Guid? branchAdminId
        );
        Task<CommonResponse> GetStockByItemIdAndScheduledTimesAsync(
            Guid userId,
            StockGettingRequest stockGettingRequest
        );
        Task UpdateStockWhenOutDate();
    }
}
