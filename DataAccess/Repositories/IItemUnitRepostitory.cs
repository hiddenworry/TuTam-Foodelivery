using DataAccess.Entities;

namespace DataAccess.Repositories
{
    public interface IItemUnitRepostitory
    {
        Task<ItemUnit?> FindItemUnitByIdAsync(Guid itemUnitId);
        Task<List<ItemUnit>?> GetListItemUnitAsync();
    }
}
