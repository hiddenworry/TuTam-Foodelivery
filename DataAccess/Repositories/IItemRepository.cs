using DataAccess.Entities;
using DataAccess.EntityEnums;

namespace DataAccess.Repositories
{
    public interface IItemRepository
    {
        Task<Item?> CreateItemAsync(Item item);
        Task<List<Item?>?> FindItemAsync(string searchTerm);
        Task<Item?> FindItemByIdAsync(Guid id);
        Task<Item?> UpdateItemAsync(Item item);
        Task<Item?> FindItemByIdForAidItemAsync(Guid itemId);
        Task<List<Item>?> SelectRelevanceByKeyWordAsync(
            string? searchStr,
            int? take,
            ItemCategoryType? itemCategory,
            Guid? itemCategoryId
        );
    }
}
