using DataAccess.DbContextData;
using DataAccess.Entities;
using DataAccess.Models.Requests;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Repositories.Implements
{
    public class ItemTemplateRepository : IItemTemplateRepository
    {
        private readonly FoodDonationDeliveryDbContext _context;

        public ItemTemplateRepository(FoodDonationDeliveryDbContext context)
        {
            _context = context;
        }

        public async Task<ItemTemplate?> CreateItemTemplateAsync(ItemTemplate item)
        {
            try
            {
                var rs = _context.ItemTemplates.Add(item);
                await _context.SaveChangesAsync();
                return rs.Entity;
            }
            catch
            {
                return null;
            }
        }

        public async Task<ItemTemplate?> UpdateItemTemplateAsync(ItemTemplate item)
        {
            try
            {
                var rs = _context.ItemTemplates.Update(item);
                await _context.SaveChangesAsync();
                return rs.Entity;
            }
            catch
            {
                return null;
            }
        }

        public async Task<ItemTemplate?> FindItemTemplateByIdAsync(Guid itemId)
        {
            return await _context.ItemTemplates.FindAsync(itemId);
        }

        public async Task<List<ItemTemplate>?> FindItemTemplatesAsync(
            ItemFilterRequest itemFilterRequest
        )
        {
            try
            {
                var query = _context.ItemTemplates
                    .Include(i => i.ItemCategory)
                    .Include(i => i.Unit)
                    .Include(i => i.Items)
                    .ThenInclude(i => i.ItemAttributeValues)
                    .Include(i => i.ItemTemplateAttributes)
                    .ThenInclude(i => i.AttributeValues)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(itemFilterRequest.name))
                {
                    query = query.Where(i => i.Name.Trim().Contains(itemFilterRequest.name));
                }
                if (!string.IsNullOrEmpty(itemFilterRequest.itemCategoryId.ToString()))
                {
                    query = query.Where(i => i.ItemcategoryId == itemFilterRequest.itemCategoryId);
                }
                if (itemFilterRequest.categoryType != null)
                {
                    query = query.Where(i => i.ItemCategory.Type == itemFilterRequest.categoryType);
                }
                if (itemFilterRequest.itemStatus != null)
                {
                    query = query.Where(i => i.Status == itemFilterRequest.itemStatus);
                }
                var items = await query.ToListAsync();
                return items;
            }
            catch
            {
                return null;
            }
        }

        public async Task<ItemTemplate?> GetItemTemplateByIdAsync(Guid itemId)
        {
            return await _context.ItemTemplates
                .Include(i => i.ItemCategory)
                .Include(i => i.Unit)
                .Include(i => i.Items)
                .ThenInclude(i => i.ItemAttributeValues)
                .Include(i => i.ItemTemplateAttributes)
                .ThenInclude(i => i.AttributeValues)
                .FirstOrDefaultAsync(p => p.Id == itemId);
        }

        public async Task<List<ItemTemplate?>?> SearchItemTemplate(
            string searchTerm,
            Guid? categoryId
        )
        {
            try
            {
                var query = _context.ItemTemplates
                    .Include(i => i.ItemCategory)
                    .Include(i => i.Unit)
                    .Include(i => i.Items)
                    .ThenInclude(it => it.ItemAttributeValues)
                    .Include(i => i.ItemTemplateAttributes)
                    .ThenInclude(a => a.AttributeValues)
                    .Where(
                        i =>
                            EF.Functions.Like(i.Name, $"%{searchTerm}%")
                            || i.Items.Any(
                                it =>
                                    it.ItemAttributeValues.Any(
                                        atv =>
                                            EF.Functions.Like(
                                                atv.AttributeValue.Value,
                                                $"%{searchTerm}%"
                                            )
                                    )
                            )
                    )
                    .AsQueryable();
                if (categoryId != null)
                {
                    query = query.Where(i => i.ItemCategory.Id == categoryId);
                }

                var items = await query.ToListAsync();
                return items!;
            }
            catch
            {
                return null;
            }
        }

        public async Task<ItemTemplate?> CheckDuplicatedName(string name)
        {
            return await _context.ItemTemplates.FirstOrDefaultAsync(
                a => a.Name.ToLower() == name.ToLower()
            );
        }
    }
}
