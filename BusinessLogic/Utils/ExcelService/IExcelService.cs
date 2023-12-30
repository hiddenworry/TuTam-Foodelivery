using DataAccess.Models.Responses;

namespace BusinessLogic.Utils.ExcelService
{
    public interface IExcelService
    {
        Task<string> CreateExcelFile(List<StockUpdateHistoryDetailResponse> items);
    }
}
