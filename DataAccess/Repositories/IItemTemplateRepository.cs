using DataAccess.Entities;
using DataAccess.Models.Requests;

namespace DataAccess.Repositories
{
    public interface IItemTemplateRepository
    {
        Task<ItemTemplate?> CheckDuplicatedName(string name);
        Task<ItemTemplate?> CreateItemTemplateAsync(ItemTemplate item);
        Task<ItemTemplate?> FindItemTemplateByIdAsync(Guid itemId);
        Task<List<ItemTemplate>?> FindItemTemplatesAsync(ItemFilterRequest itemFilterRequest);
        Task<ItemTemplate?> GetItemTemplateByIdAsync(Guid itemId);
        Task<List<ItemTemplate?>?> SearchItemTemplate(string searchTerm, Guid? categoryId);
        Task<ItemTemplate?> UpdateItemTemplateAsync(ItemTemplate item);
    }
}
