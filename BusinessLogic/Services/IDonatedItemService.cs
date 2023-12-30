using DataAccess.EntityEnums;
using DataAccess.Models.Responses;

namespace BusinessLogic.Services
{
    public interface IDonatedItemService
    {
        Task<CommonResponse> GetDonatedItemAsync(
            int? page,
            int? pageSize,
            Guid userId,
            string? keyWord,
            DonatedItemStatus? status,
            DateTime? startDate,
            DateTime? endDate
        );
    }
}
