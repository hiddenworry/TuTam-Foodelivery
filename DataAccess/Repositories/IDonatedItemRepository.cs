using DataAccess.Entities;
using DataAccess.EntityEnums;

namespace DataAccess.Repositories
{
    public interface IDonatedItemRepository
    {
        Task<int> CreateDonatedItemsAsync(List<DonatedItem> donatedItems);
        Task<DonatedItem?> FindDonatedItemByIdAsync(Guid id);
        Task<List<DonatedItem>?> GetHistoryDonatedItemOfUserAsync(
            Guid userId,
            DateTime? startDate,
            DateTime? endDate,
            string? keyWord,
            DonatedItemStatus? status
        );
        Task<int> UpdateDonatedItemsAsync(List<DonatedItem> donatedItems);
    }
}
