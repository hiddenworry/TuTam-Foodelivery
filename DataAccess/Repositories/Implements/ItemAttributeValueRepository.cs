using DataAccess.DbContextData;
using DataAccess.Entities;

namespace DataAccess.Repositories.Implements
{
    public class ItemAttributeValueRepository : IItemAttributeValueRepository
    {
        private readonly FoodDonationDeliveryDbContext _context;

        public ItemAttributeValueRepository(FoodDonationDeliveryDbContext context)
        {
            _context = context;
        }

        public async Task<ItemAttributeValue?> CreateItemAttributeValueAsync(
            ItemAttributeValue itemAttributeValue
        )
        {
            try
            {
                var rs = _context.ItemAttributeValues.Add(itemAttributeValue);
                await _context.SaveChangesAsync();
                return rs.Entity;
            }
            catch
            {
                return null;
            }
        }
    }
}
