using DataAccess.Models.Requests;
using DataAccess.Models.Responses;

namespace BusinessLogic.Services
{
    public interface IItemCategoryService
    {
        Task<CommonResponse> CreateItemsCategoryAsync(ItemsCategoryRequest request);
        Task<CommonResponse> GetItemCategoriesListAsync();
    }
}
