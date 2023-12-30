using DataAccess.DbContextData;
using DataAccess.Entities;
using DataAccess.EntityEnums;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Repositories.Implements
{
    public class DonatedItemRepository : IDonatedItemRepository
    {
        private readonly FoodDonationDeliveryDbContext _context;

        public DonatedItemRepository(FoodDonationDeliveryDbContext context)
        {
            _context = context;
        }

        public async Task<int> CreateDonatedItemsAsync(List<DonatedItem> donatedItems)
        {
            int rs = 0;
            foreach (DonatedItem item in donatedItems)
            {
                rs += await CreateDonatedItemAsync(item);
            }
            return rs;
        }

        public async Task<int> CreateDonatedItemAsync(DonatedItem donatedItem)
        {
            await _context.DonatedItems.AddAsync(donatedItem);
            return await _context.SaveChangesAsync() > 0 ? 1 : 0;
        }

        public async Task<int> UpdateDonatedItemsAsync(List<DonatedItem> donatedItems)
        {
            int rs = 0;
            foreach (DonatedItem item in donatedItems)
            {
                rs += await UpdateDonatedItemAsync(item);
            }
            return rs;
        }

        public async Task<int> UpdateDonatedItemAsync(DonatedItem item)
        {
            _context.DonatedItems.Update(item);
            return await _context.SaveChangesAsync() > 0 ? 1 : 0;
        }

        public async Task<DonatedItem?> FindDonatedItemByIdAsync(Guid id)
        {
            return await _context.DonatedItems
                .Include(di => di.Item)
                .Include(di => di.DonatedRequest)
                .FirstOrDefaultAsync(di => di.Id == id);
        }

        public async Task<List<DonatedItem>?> GetHistoryDonatedItemOfUserAsync(
            Guid userId,
            DateTime? startDate,
            DateTime? endDate,
            string? keyWord,
            DonatedItemStatus? status
        )
        {
            var query = _context.DonatedItems
                .Include(a => a.DonatedRequest)
                .Include(a => a.Item)
                .ThenInclude(a => a.ItemTemplate)
                .ThenInclude(a => a.ItemCategory)
                .Include(a => a.Item)
                .ThenInclude(a => a.ItemTemplate)
                .ThenInclude(a => a.Unit)
                .Include(a => a.Item)
                .ThenInclude(a => a.ItemAttributeValues)
                .ThenInclude(a => a.AttributeValue)
                .AsQueryable();

            query = query.Where(a => a.DonatedRequest.UserId == userId);

            if (startDate != null && endDate == null)
            {
                query = query.Where(a => a.DonatedRequest.CreatedDate >= startDate);
            }
            if (endDate != null && startDate == null)
            {
                query = query.Where(a => a.DonatedRequest.CreatedDate <= endDate);
            }
            if (endDate != null && startDate != null)
            {
                query = query.Where(
                    a =>
                        a.DonatedRequest.CreatedDate <= endDate
                        && a.DonatedRequest.CreatedDate >= startDate
                );
            }
            if (!string.IsNullOrEmpty(keyWord))
            {
                query = query.Where(a => a.Item.ItemTemplate.Name.ToLower().Contains(keyWord));
            }
            if (status != null)
            {
                query = query.Where(a => a.Status == status);
            }
            return await query.ToListAsync();
        }
    }
}
