using DataAccess.DbContextData;
using DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Repositories.Implements
{
    public class ItemUnitRepository : IItemUnitRepostitory
    {
        private readonly FoodDonationDeliveryDbContext _context;

        public ItemUnitRepository(FoodDonationDeliveryDbContext context)
        {
            _context = context;
        }

        public async Task<ItemUnit?> FindItemUnitByIdAsync(Guid itemUnitId)
        {
            return await _context.ItemUnits.FindAsync(itemUnitId);
        }

        public async Task<List<ItemUnit>?> GetListItemUnitAsync()
        {
            return await _context.ItemUnits.ToListAsync();
        }
    }
}
