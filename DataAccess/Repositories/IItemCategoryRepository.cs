using DataAccess.Entities;

namespace DataAccess.Repositories
{
    public interface IItemCategoryRepository
    {
        Task<ItemCategory?> CreateItemCategoryAsync(ItemCategory itemCategory);
        Task<ItemCategory?> FindItemCategoryByIdAsync(Guid categoryId);
        Task<List<ItemCategory>?> GetListItemCategoryAsync();
    }
}
