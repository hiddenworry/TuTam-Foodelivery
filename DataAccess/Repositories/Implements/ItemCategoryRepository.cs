using DataAccess.DbContextData;
using DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Repositories.Implements
{
    public class ItemCategoryRepository : IItemCategoryRepository
    {
        private readonly FoodDonationDeliveryDbContext _context;

        public ItemCategoryRepository(FoodDonationDeliveryDbContext context)
        {
            _context = context;
        }

        public async Task<ItemCategory?> CreateItemCategoryAsync(ItemCategory itemCategory)
        {
            try
            {
                var rs = _context.ItemCategories.Add(itemCategory);
                await _context.SaveChangesAsync();
                return rs.Entity;
            }
            catch
            {
                return null;
            }
        }

        public async Task<ItemCategory?> FindItemCategoryByIdAsync(Guid categoryId)
        {
            return await _context.ItemCategories.FindAsync(categoryId);
        }

        public async Task<List<ItemCategory>?> GetListItemCategoryAsync()
        {
            return await _context.ItemCategories.ToListAsync();
        }
    }
}
