using DataAccess.Entities;

namespace DataAccess.Repositories
{
    public interface IAidItemRepository
    {
        Task<int> CreateAidItemAsync(AidItem aidItem);
        Task<int> CreateAidItemsAsync(List<AidItem> aidItems);
        Task<AidItem?> FindAidItemByIdAsync(Guid guid);
        Task<AidItem?> GetAidItemForActivityByIdAsync(Guid aidItemId);
        Task<int> UpdateAidItemsAsync(List<AidItem> aidItems);
    }
}
