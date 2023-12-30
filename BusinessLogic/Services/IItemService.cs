using DataAccess.EntityEnums;
using DataAccess.Models.Responses;

namespace BusinessLogic.Services
{
    public interface IItemService
    {
        Task<CommonResponse> GetItemById(Guid itemId);
        Task<CommonResponse> SearchItemForUser(
            string? searchStr,
            ItemCategoryType? itemCategory,
            Guid? itemCategoryId,
            int? page,
            int? pageSize
        );
    }
}
