using DataAccess.Models.Requests.Enum;
using DataAccess.Models.Responses;
using DataAccess.ModelsEnum;

namespace BusinessLogic.Services
{
    public interface IAidItemService
    {
        Task<CommonResponse> GetAidItemsForBranchAdminAsync(
            string? keyWord,
            UrgencyLevel? urgencyLevel,
            DateTime? startDate,
            DateTime? endDate,
            Guid? charityUnitId,
            int? pageSize,
            int? page,
            string? orderBy,
            SortType? sortType,
            Guid userId
        );
    }
}
