using DataAccess.DbContextData;
using DataAccess.Entities;
using DataAccess.EntityEnums;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Repositories.Implements
{
    public class ItemRepository : IItemRepository
    {
        private readonly FoodDonationDeliveryDbContext _context;

        public ItemRepository(FoodDonationDeliveryDbContext context)
        {
            _context = context;
        }

        public async Task<Item?> CreateItemAsync(Item item)
        {
            try
            {
                var rs = _context.Items.Add(item);
                await _context.SaveChangesAsync();
                return rs.Entity;
            }
            catch
            {
                return null;
            }
        }

        public async Task<Item?> FindItemByIdAsync(Guid itemId)
        {
            return await _context.Items
                .Include(it => it.ItemTemplate)
                .ThenInclude(it => it.ItemCategory)
                .Include(it => it.ItemTemplate.Unit)
                .Include(it => it.ItemAttributeValues)
                .ThenInclude(itav => itav.AttributeValue)
                .FirstOrDefaultAsync(it => it.Id == itemId);
        }

        public async Task<Item?> UpdateItemAsync(Item item)
        {
            try
            {
                var rs = _context.Items.Update(item);
                await _context.SaveChangesAsync();
                return rs.Entity;
            }
            catch
            {
                return null;
            }
        }

        public async Task<List<Item?>?> FindItemAsync(string searchTerm)
        {
            try
            {
                var query = _context.Items
                    .Include(it => it.ItemTemplate)
                    .ThenInclude(it => it.Unit)
                    .Include(it => it.ItemTemplate)
                    .Include(it => it.ItemAttributeValues)
                    .ThenInclude(itav => itav.AttributeValue)
                    .Where(
                        i =>
                            i.ItemTemplate.Name.ToLower().Contains(searchTerm.ToLower())
                            || i.ItemAttributeValues.Any(
                                it =>
                                    it.AttributeValue.Value.ToLower().Contains(searchTerm.ToLower())
                            )
                    );

                var items = await query.ToListAsync();
                return items!;
            }
            catch
            {
                return null;
            }
        }

        public async Task<List<Item>?> SelectRelevanceByKeyWordAsync(
            string? searchStr,
            int? take,
            ItemCategoryType? itemCategory,
            Guid? itemCategoryId
        )
        {
            var query = await FindItemAsync(itemCategory, itemCategoryId);
            List<Item>? rs = null;

            if (!string.IsNullOrEmpty(searchStr) && query != null)
            {
                List<string> keyWords = searchStr.Split(' ').ToList();

                List<Item> results = new();
                Dictionary<Item, int> Points = new();

                foreach (string keyWord in keyWords)
                {
                    var matchingItems = query
                        .Where(
                            i =>
                                i.ItemTemplate.Name.ToLower().Contains(keyWord.ToLower())
                                || i.ItemAttributeValues.Any(
                                    atv =>
                                        atv.AttributeValue.Value
                                            .ToLower()
                                            .Contains(keyWord.ToLower()) //
                                )
                        )
                        .ToList();

                    foreach (var itemTemplate in matchingItems)
                    {
                        if (Points.ContainsKey(itemTemplate))
                        {
                            Points[itemTemplate] += CalculateScore(itemTemplate, keyWord);
                        }
                        else
                        {
                            Points[itemTemplate] = CalculateScore(itemTemplate, keyWord);
                        }
                    }
                }
                query = Points
                    .OrderByDescending(kv => kv.Value)
                    .Select(kv => kv.Key)
                    .Take(take ?? 2)
                    .ToList();
            }
            rs = query;

            return rs;
        }

        private int CalculateScore(Item itemTemplate, string keyWord)
        {
            int score = 0;

            if (itemTemplate.ItemTemplate.Name.ToLower().Contains(keyWord.ToLower()))
            {
                score += 5;
            }

            if (
                itemTemplate.ItemAttributeValues.Any(
                    atv => atv.AttributeValue.Value.ToLower().Contains(keyWord.ToLower())
                )
            )
            {
                score += 2;
            }

            return score;
        }

        public async Task<Item?> FindItemByIdForAidItemAsync(Guid itemId)
        {
            return await _context.Items
                .Include(i => i.ItemTemplate)
                .ThenInclude(i => i.ItemTemplateAttributes)
                .Include(i => i.ItemTemplate.ItemCategory)
                .Include(i => i.ItemTemplate.Unit)
                .Include(it => it.ItemAttributeValues)
                .ThenInclude(a => a.AttributeValue)
                .FirstOrDefaultAsync(i => i.Id == itemId);
        }

        private async Task<List<Item>?> FindItemAsync(
            ItemCategoryType? itemCategory,
            Guid? itemCategoryId
        )
        {
            try
            {
                var query = _context.Items
                    .Include(i => i.ItemTemplate)
                    .ThenInclude(i => i.ItemTemplateAttributes)
                    .Include(i => i.ItemTemplate.Unit)
                    .Include(it => it.ItemAttributeValues)
                    .ThenInclude(a => a.AttributeValue)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(itemCategoryId.ToString()))
                {
                    query = query.Where(i => i.ItemTemplate.ItemcategoryId == itemCategoryId);
                }
                if (itemCategory != null)
                {
                    query = query.Where(i => i.ItemTemplate.ItemCategory.Type == itemCategory);
                }

                query = query.Where(i => i.Status == ItemStatus.ACTIVE);

                var items = await query.ToListAsync();
                return items;
            }
            catch
            {
                return null;
            }
        }
    }
}
