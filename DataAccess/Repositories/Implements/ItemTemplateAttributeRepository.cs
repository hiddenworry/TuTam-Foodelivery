using DataAccess.DbContextData;
using DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Repositories.Implements
{
    public class ItemTemplateAttributeRepository : IItemTemplateAttributeRepository
    {
        private readonly FoodDonationDeliveryDbContext _context;

        public ItemTemplateAttributeRepository(FoodDonationDeliveryDbContext context)
        {
            _context = context;
        }

        public async Task<ItemTemplateAttribute?> CreateItemTemplateAttributeAsync(
            ItemTemplateAttribute attribute
        )
        {
            try
            {
                var rs = _context.ItemTemplateAttributes.Add(attribute);
                await _context.SaveChangesAsync();
                return rs.Entity;
            }
            catch
            {
                return null;
            }
        }

        public async Task<ItemTemplateAttribute?> UpdateItemTemplateAttributeAsync(
            ItemTemplateAttribute attribute
        )
        {
            try
            {
                var rs = _context.ItemTemplateAttributes.Update(attribute);
                await _context.SaveChangesAsync();
                return rs.Entity;
            }
            catch
            {
                return null;
            }
        }

        public async Task<ItemTemplateAttribute?> FindItemTemplateAttributeByNameAndItemTempateIdAsync(
            string name,
            Guid itemId
        )
        {
            return await _context.ItemTemplateAttributes
                .Include(a => a.AttributeValues)
                .Where(a => a.Name == name.Trim() && a.ItemTemplateId == itemId)
                .FirstOrDefaultAsync();
        }

        public async Task<ItemTemplateAttribute?> FindItemTemplateAttributeByIdAsync(Guid Id)
        {
            return await _context.ItemTemplateAttributes
                .Include(a => a.AttributeValues)
                .Where(a => a.Id == Id)
                .FirstOrDefaultAsync();
        }

        public async Task<
            List<ItemTemplateAttribute>
        >? FindItemTemplateAttributeByItemTemplateIdAsync(Guid itemTemplateId)
        {
            return await _context.ItemTemplateAttributes
                .Include(a => a.AttributeValues)
                .Where(a => a.ItemTemplateId == itemTemplateId)
                .ToListAsync();
        }
    }
}
