using DataAccess.DbContextData;
using DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Repositories.Implements
{
    public class AttributeValueRepository : IAttributeValueRepository
    {
        private readonly FoodDonationDeliveryDbContext _context;

        public AttributeValueRepository(FoodDonationDeliveryDbContext context)
        {
            _context = context;
        }

        public async Task<AttributeValue?> CreateAttributeValueAsync(AttributeValue attributeValue)
        {
            try
            {
                var rs = _context.AttributeValues.Add(attributeValue);
                await _context.SaveChangesAsync();
                return rs.Entity;
            }
            catch
            {
                return null;
            }
        }

        public async Task<AttributeValue?> FindAttributeValueByNameAndAttributeIdAsync(
            Guid id,
            string name
        )
        {
            return await _context.AttributeValues
                .Where(av => av.Value == name && av.ItemTemplateAttributeId == id)
                .FirstOrDefaultAsync();
        }

        public async Task<AttributeValue?> FindAttributeValueByIdAsync(Guid id)
        {
            return await _context.AttributeValues.Where(av => av.Id == id).FirstOrDefaultAsync();
        }
    }
}
